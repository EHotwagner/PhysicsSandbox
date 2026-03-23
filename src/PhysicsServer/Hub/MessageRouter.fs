/// <summary>
/// Central message routing hub that connects clients, the simulation, and the viewer.
/// Commands flow from clients through bounded channels to the simulation/viewer,
/// while state updates are broadcast from the simulation to all subscribers.
/// The router decomposes incoming SimulationState into lean TickState (60 Hz)
/// and PropertyEvent (on-change) for downstream clients.
/// </summary>
module PhysicsServer.Hub.MessageRouter

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.StateCache

/// <summary>Opaque handle wrapping a Guid, used to identify state stream subscriptions for later removal.</summary>
type SubscriptionId = SubscriptionId of Guid

/// <summary>
/// Core server state: holds the state cache, subscriber registries, bounded command channels,
/// simulation connection status, and performance metrics.
/// </summary>
type MessageRouter =
    { StateCache: StateCache.StateCache
      PropertyCache: StateCache.PropertyCache
      Subscribers: ConcurrentDictionary<Guid, TickState -> Task>
      PropertySubscribers: ConcurrentDictionary<Guid, PropertyEvent -> Task>
      CommandSubscribers: ConcurrentDictionary<Guid, CommandEvent -> Task>
      CommandChannel: Channel<SimulationCommand>
      ViewCommandChannel: Channel<ViewCommand>
      mutable SimulationConnected: bool
      SimulationLock: obj
      Metrics: MetricsCounter.MetricsState
      MeshCache: MeshCache.MeshCacheState
      /// Tracks the last-known semi-static properties per body for change detection.
      PreviousBodyProps: ConcurrentDictionary<string, BodyProperties>
      /// Tracks which body IDs were present in the last state for removal detection.
      mutable PreviousBodyIds: Set<string>
      /// Tracks the previous constraint count for change detection.
      mutable PreviousConstraintCount: int
      /// Tracks the previous registered shape count for change detection.
      mutable PreviousRegisteredShapeCount: int }

/// <summary>A pending query entry with its TCS and creation timestamp for timeout tracking.</summary>
type PendingQueryEntry =
    { Tcs: TaskCompletionSource<QueryResponse>
      CreatedAt: DateTime }

/// <summary>Pending query completions, keyed by correlation ID.</summary>
let internal pendingQueries = ConcurrentDictionary<string, PendingQueryEntry>()

/// <summary>Create a new message router with empty subscriber lists, bounded command channels (1024), and fresh metrics.</summary>
let create () =
    { StateCache = StateCache.create ()
      PropertyCache = StateCache.createPropertyCache ()
      Subscribers = ConcurrentDictionary<Guid, TickState -> Task>()
      PropertySubscribers = ConcurrentDictionary<Guid, PropertyEvent -> Task>()
      CommandSubscribers = ConcurrentDictionary<Guid, CommandEvent -> Task>()
      CommandChannel = Channel.CreateBounded<SimulationCommand>(1024)
      ViewCommandChannel = Channel.CreateBounded<ViewCommand>(1024)
      SimulationConnected = false
      SimulationLock = obj ()
      Metrics = MetricsCounter.create "PhysicsServer"
      MeshCache = MeshCache.create ()
      PreviousBodyProps = ConcurrentDictionary<string, BodyProperties>()
      PreviousBodyIds = Set.empty
      PreviousConstraintCount = 0
      PreviousRegisteredShapeCount = 0 }

/// <summary>Register a callback to receive command audit events.</summary>
let subscribeCommands (router: MessageRouter) (callback: CommandEvent -> Task) =
    let id = Guid.NewGuid()
    router.CommandSubscribers.TryAdd(id, callback) |> ignore
    id

/// <summary>Remove a command audit subscription by its id.</summary>
let unsubscribeCommands (router: MessageRouter) (id: Guid) =
    router.CommandSubscribers.TryRemove(id) |> ignore

/// <summary>Broadcast a command event to all registered command audit subscribers.</summary>
let publishCommandEvent (router: MessageRouter) (evt: CommandEvent) =
    task {
        for kvp in router.CommandSubscribers do
            try
                do! kvp.Value evt
            with
            | _ -> ()
    }

/// <summary>Submit a simulation command to the router.</summary>
let submitCommand (router: MessageRouter) (cmd: SimulationCommand) =
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics

    let written =
        lock router.SimulationLock (fun () ->
            if router.SimulationConnected then
                router.CommandChannel.Writer.TryWrite(cmd)
            else
                false)

    if written then
        MetricsCounter.incrementSent 1 (int64 (cmd.CalculateSize())) router.Metrics

    if cmd.CommandCase = SimulationCommand.CommandOneofCase.Reset then
        MeshCache.clear router.MeshCache

    let evt = CommandEvent()
    evt.SimulationCommand <- cmd
    publishCommandEvent router evt |> ignore

    CommandAck(
        Success = true,
        Message =
            if written then
                "Command forwarded to simulation"
            else
                "Command accepted (no simulation connected — dropped)"
    )

/// <summary>Submit a view command to the router.</summary>
let submitViewCommand (router: MessageRouter) (cmd: ViewCommand) =
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics
    let _written = router.ViewCommandChannel.Writer.TryWrite(cmd)

    let evt = CommandEvent()
    evt.ViewCommand <- cmd
    publishCommandEvent router evt |> ignore

    CommandAck(
        Success = true,
        Message = "View command accepted"
    )

/// <summary>Subscribe to lean tick state updates (60 Hz, dynamic body poses only).</summary>
let subscribe (router: MessageRouter) (callback: TickState -> Task) =
    let id = Guid.NewGuid()
    router.Subscribers.TryAdd(id, callback) |> ignore
    SubscriptionId id

/// <summary>Remove a tick state subscription.</summary>
let unsubscribe (router: MessageRouter) (SubscriptionId id) =
    router.Subscribers.TryRemove(id) |> ignore

/// <summary>Subscribe to property events (body lifecycle, semi-static changes).</summary>
let subscribeProperties (router: MessageRouter) (callback: PropertyEvent -> Task) =
    let id = Guid.NewGuid()
    router.PropertySubscribers.TryAdd(id, callback) |> ignore
    SubscriptionId id

/// <summary>Remove a property event subscription.</summary>
let unsubscribeProperties (router: MessageRouter) (SubscriptionId id) =
    router.PropertySubscribers.TryRemove(id) |> ignore

/// <summary>Get the latest cached property snapshot for late joiners.</summary>
let getPropertySnapshot (router: MessageRouter) =
    StateCache.getProperties router.PropertyCache

/// <summary>Process query responses from a simulation state update.</summary>
let processQueryResponses (state: SimulationState) =
    if not (isNull state) && state.QueryResponses.Count > 0 then
        for qr in state.QueryResponses do
            match pendingQueries.TryRemove(qr.CorrelationId) with
            | true, entry -> entry.Tcs.TrySetResult(qr) |> ignore
            | _ -> ()

/// <summary>Remove pending queries older than 30 seconds and set TimeoutException on their TCS.</summary>
let expireStaleQueries () =
    let cutoff = TimeSpan.FromSeconds(30.0)
    for kvp in pendingQueries do
        if DateTime.UtcNow - kvp.Value.CreatedAt > cutoff then
            kvp.Value.Tcs.TrySetException(TimeoutException("Query expired after 30s")) |> ignore
            pendingQueries.TryRemove(kvp.Key) |> ignore

/// <summary>Timer that sweeps pending queries every 10 seconds.</summary>
let private queryExpirationTimer =
    new Timer(
        (fun _ -> expireStaleQueries ()),
        null,
        TimeSpan.FromSeconds(10.0),
        TimeSpan.FromSeconds(10.0))

/// <summary>Dispose the query expiration timer.</summary>
let disposeExpirationTimer () =
    queryExpirationTimer.Dispose()

// ─── Internal: State Decomposition ──────────────────────────────────────────

/// <summary>Build BodyProperties from a Body proto message.</summary>
let private bodyToProperties (body: Body) =
    let bp = BodyProperties()
    bp.Id <- body.Id
    bp.Shape <- body.Shape
    bp.Color <- body.Color
    bp.Mass <- body.Mass
    bp.IsStatic <- body.IsStatic
    bp.MotionType <- body.MotionType
    bp.CollisionGroup <- body.CollisionGroup
    bp.CollisionMask <- body.CollisionMask
    bp.Material <- body.Material
    bp.Position <- body.Position
    bp.Orientation <- body.Orientation
    bp

/// <summary>Check if semi-static properties differ between two BodyProperties.</summary>
let private propsChanged (prev: BodyProperties) (curr: BodyProperties) =
    prev.Shape <> curr.Shape
    || prev.Color <> curr.Color
    || prev.Mass <> curr.Mass
    || prev.IsStatic <> curr.IsStatic
    || prev.MotionType <> curr.MotionType
    || prev.CollisionGroup <> curr.CollisionGroup
    || prev.CollisionMask <> curr.CollisionMask
    || prev.Material <> curr.Material

/// <summary>Build a lean TickState from a full SimulationState (dynamic bodies only).</summary>
let private buildTickState (state: SimulationState) =
    let tick = TickState()
    tick.Time <- state.Time
    tick.Running <- state.Running
    tick.TickMs <- state.TickMs
    tick.SerializeMs <- state.SerializeMs
    for body in state.Bodies do
        if not body.IsStatic then
            let pose = BodyPose()
            pose.Id <- body.Id
            pose.Position <- body.Position
            pose.Orientation <- body.Orientation
            pose.Velocity <- body.Velocity
            pose.AngularVelocity <- body.AngularVelocity
            tick.Bodies.Add(pose)
    for qr in state.QueryResponses do
        tick.QueryResponses.Add(qr)
    tick

/// <summary>Detect property changes and emit PropertyEvent messages.</summary>
let private detectPropertyEvents (router: MessageRouter) (state: SimulationState) =
    let events = ResizeArray<PropertyEvent>()
    let currentIds = state.Bodies |> Seq.map (fun b -> b.Id) |> Set.ofSeq

    // Detect new and changed bodies
    for body in state.Bodies do
        let curr = bodyToProperties body
        match router.PreviousBodyProps.TryGetValue(body.Id) with
        | false, _ ->
            // New body — emit body_created
            let evt = PropertyEvent(BodyCreated = curr)
            events.Add(evt)
            router.PreviousBodyProps.[body.Id] <- curr
        | true, prev ->
            if propsChanged prev curr then
                // Changed — emit body_updated
                let evt = PropertyEvent(BodyUpdated = curr)
                events.Add(evt)
                router.PreviousBodyProps.[body.Id] <- curr
            else
                // Update cached pose for static bodies (in case SetBodyPose changed it)
                if body.IsStatic then
                    router.PreviousBodyProps.[body.Id] <- curr

    // Detect removed bodies
    for prevId in router.PreviousBodyIds do
        if not (Set.contains prevId currentIds) then
            let evt = PropertyEvent(BodyRemoved = prevId)
            events.Add(evt)
            router.PreviousBodyProps.TryRemove(prevId) |> ignore

    router.PreviousBodyIds <- currentIds

    // Detect constraint changes
    if state.Constraints.Count <> router.PreviousConstraintCount then
        let snap = ConstraintSnapshot()
        for cs in state.Constraints do
            snap.Constraints.Add(cs)
        let evt = PropertyEvent(ConstraintsSnapshot = snap)
        events.Add(evt)
        router.PreviousConstraintCount <- state.Constraints.Count

    // Detect registered shape changes
    if state.RegisteredShapes.Count <> router.PreviousRegisteredShapeCount then
        let snap = RegisteredShapeSnapshot()
        for rs in state.RegisteredShapes do
            snap.RegisteredShapes.Add(rs)
        let evt = PropertyEvent(RegisteredShapesSnapshot = snap)
        events.Add(evt)
        router.PreviousRegisteredShapeCount <- state.RegisteredShapes.Count

    // Add mesh definitions to the first event (or create one if needed)
    if state.NewMeshes.Count > 0 then
        if events.Count > 0 then
            for mg in state.NewMeshes do
                events.[0].NewMeshes.Add(mg)
        else
            let evt = PropertyEvent()
            for mg in state.NewMeshes do
                evt.NewMeshes.Add(mg)
            events.Add(evt)

    events

/// <summary>Build a PropertySnapshot from current state (for late-joiner backfill).</summary>
let private buildPropertySnapshot (router: MessageRouter) (state: SimulationState) =
    let snapshot = PropertySnapshot()
    for body in state.Bodies do
        snapshot.Bodies.Add(bodyToProperties body)
    for cs in state.Constraints do
        snapshot.Constraints.Add(cs)
    for rs in state.RegisteredShapes do
        snapshot.RegisteredShapes.Add(rs)
    snapshot

/// <summary>Broadcast a PropertyEvent to all property subscribers.</summary>
let private publishPropertyEventInternal (router: MessageRouter) (evt: PropertyEvent) =
    task {
        for kvp in router.PropertySubscribers do
            try
                do! kvp.Value evt
            with
            | _ -> ()
    }

/// <summary>
/// Publish a simulation state from the simulation upstream.
/// Decomposes it into TickState (broadcast to state subscribers) and
/// PropertyEvents (broadcast to property subscribers on change).
/// </summary>
let publishState (router: MessageRouter) (state: SimulationState) =
    task {
        MetricsCounter.incrementReceived 1 (int64 (state.CalculateSize())) router.Metrics

        // Process query responses
        processQueryResponses state

        // Cache new mesh geometries
        let newMeshCount = state.NewMeshes.Count
        for mg in state.NewMeshes do
            MeshCache.add mg.MeshId mg.Shape router.MeshCache
        if newMeshCount > 0 then
            MetricsCounter.incrementMeshesCached newMeshCount router.Metrics

        // Build and broadcast lean TickState to state subscribers
        let tickState = buildTickState state
        StateCache.update router.StateCache tickState
        let tickBytes = int64 (tickState.CalculateSize())
        for kvp in router.Subscribers do
            try
                do! kvp.Value tickState
            with
            | _ -> ()
        MetricsCounter.incrementTickSent router.Subscribers.Count (tickBytes * int64 router.Subscribers.Count) router.Metrics

        // Detect and broadcast property events
        let propEvents = detectPropertyEvents router state
        for evt in propEvents do
            let evtBytes = int64 (evt.CalculateSize())
            do! publishPropertyEventInternal router evt
            MetricsCounter.incrementPropertySent router.PropertySubscribers.Count (evtBytes * int64 router.PropertySubscribers.Count) router.Metrics

        // Update property snapshot cache for late joiners
        let snapshot = buildPropertySnapshot router state
        StateCache.updateProperties router.PropertyCache snapshot
    }

/// <summary>Retrieve the latest cached tick state for late-joining clients.</summary>
let getLatestState (router: MessageRouter) =
    StateCache.get router.StateCache

/// <summary>Attempt to register as the active simulation.</summary>
let tryConnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        if router.SimulationConnected then
            false
        else
            router.SimulationConnected <- true
            true)

/// <summary>Unregister the active simulation.</summary>
let disconnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        router.SimulationConnected <- false)
    MeshCache.clear router.MeshCache
    router.PreviousBodyProps.Clear()
    router.PreviousBodyIds <- Set.empty
    router.PreviousConstraintCount <- 0
    router.PreviousRegisteredShapeCount <- 0

/// <summary>Capture a point-in-time snapshot of the server's throughput metrics.</summary>
let getMetrics (router: MessageRouter) =
    MetricsCounter.snapshot router.Metrics

/// <summary>Access the raw metrics state.</summary>
let metricsState (router: MessageRouter) =
    router.Metrics

/// <summary>Access the mesh cache for FetchMeshes RPC.</summary>
let meshCache (router: MessageRouter) =
    router.MeshCache

/// <summary>Submit a batch of simulation commands (max 100).</summary>
let sendBatchCommand (router: MessageRouter) (batch: BatchSimulationRequest) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let response = BatchResponse()

    if batch.Commands.Count > 100 then
        let result = CommandResult(Success = false, Message = "Batch exceeds maximum of 100 commands", Index = 0)
        response.Results.Add(result)
    else
        for i in 0 .. batch.Commands.Count - 1 do
            let cmd = batch.Commands.[i]
            let ack = submitCommand router cmd
            let result = CommandResult(Success = ack.Success, Message = ack.Message, Index = i)
            response.Results.Add(result)

    sw.Stop()
    response.TotalTimeMs <- sw.Elapsed.TotalMilliseconds
    response

/// <summary>Submit a batch of view commands (max 100).</summary>
let sendBatchViewCommand (router: MessageRouter) (batch: BatchViewRequest) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let response = BatchResponse()

    if batch.Commands.Count > 100 then
        let result = CommandResult(Success = false, Message = "Batch exceeds maximum of 100 commands", Index = 0)
        response.Results.Add(result)
    else
        for i in 0 .. batch.Commands.Count - 1 do
            let cmd = batch.Commands.[i]
            let ack = submitViewCommand router cmd
            let result = CommandResult(Success = ack.Success, Message = ack.Message, Index = i)
            response.Results.Add(result)

    sw.Stop()
    response.TotalTimeMs <- sw.Elapsed.TotalMilliseconds
    response

/// <summary>Read a pending simulation command from the channel.</summary>
let readCommand (router: MessageRouter) (ct: CancellationToken) =
    task {
        try
            let! cmd = router.CommandChannel.Reader.ReadAsync(ct).AsTask()
            return Some cmd
        with
        | :? OperationCanceledException -> return None
        | :? ChannelClosedException -> return None
    }

/// <summary>Submit a query through the command channel and wait for the response.</summary>
let submitQuery (router: MessageRouter) (queryRequest: QueryRequest) (ct: CancellationToken) =
    task {
        let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
        use _reg = ct.Register(fun () -> tcs.TrySetCanceled() |> ignore)
        let entry = { Tcs = tcs; CreatedAt = DateTime.UtcNow }
        pendingQueries.TryAdd(queryRequest.CorrelationId, entry) |> ignore
        try
            let cmd = SimulationCommand(QueryRequest = queryRequest)
            let _ack = submitCommand router cmd
            let! response = tcs.Task
            return response
        finally
            pendingQueries.TryRemove(queryRequest.CorrelationId) |> ignore
    }

/// <summary>Read a pending view command from the channel.</summary>
let readViewCommand (router: MessageRouter) (ct: CancellationToken) =
    task {
        try
            let! cmd = router.ViewCommandChannel.Reader.ReadAsync(ct).AsTask()
            return Some cmd
        with
        | :? OperationCanceledException -> return None
        | :? ChannelClosedException -> return None
    }
