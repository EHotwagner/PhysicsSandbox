/// <summary>
/// Central message routing hub that connects clients, the simulation, and the viewer.
/// Commands flow from clients through bounded channels to the simulation/viewer,
/// while state updates are broadcast from the simulation to all subscribers.
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
      Subscribers: ConcurrentDictionary<Guid, SimulationState -> Task>
      CommandSubscribers: ConcurrentDictionary<Guid, CommandEvent -> Task>
      CommandChannel: Channel<SimulationCommand>
      ViewCommandChannel: Channel<ViewCommand>
      mutable SimulationConnected: bool
      SimulationLock: obj
      Metrics: MetricsCounter.MetricsState }

/// <summary>Pending query completions, keyed by correlation ID.</summary>
let internal pendingQueries = ConcurrentDictionary<string, TaskCompletionSource<QueryResponse>>()

/// <summary>Create a new message router with empty subscriber lists, bounded command channels (1024), and fresh metrics.</summary>
/// <returns>A fully initialized MessageRouter ready to accept connections.</returns>
let create () =
    { StateCache = StateCache.create ()
      Subscribers = ConcurrentDictionary<Guid, SimulationState -> Task>()
      CommandSubscribers = ConcurrentDictionary<Guid, CommandEvent -> Task>()
      CommandChannel = Channel.CreateBounded<SimulationCommand>(1024)
      ViewCommandChannel = Channel.CreateBounded<ViewCommand>(1024)
      SimulationConnected = false
      SimulationLock = obj ()
      Metrics = MetricsCounter.create "PhysicsServer" }

/// <summary>Register a callback to receive command audit events (both simulation and view commands).</summary>
/// <param name="router">The message router to subscribe to.</param>
/// <param name="callback">Async callback invoked for each command event.</param>
/// <returns>A Guid that can be used to unsubscribe later.</returns>
let subscribeCommands (router: MessageRouter) (callback: CommandEvent -> Task) =
    let id = Guid.NewGuid()
    router.CommandSubscribers.TryAdd(id, callback) |> ignore
    id

/// <summary>Remove a command audit subscription by its id.</summary>
/// <param name="router">The message router to unsubscribe from.</param>
/// <param name="id">The subscription Guid returned by subscribeCommands.</param>
let unsubscribeCommands (router: MessageRouter) (id: Guid) =
    router.CommandSubscribers.TryRemove(id) |> ignore

/// <summary>Broadcast a command event to all registered command audit subscribers. Errors in individual callbacks are silently ignored.</summary>
/// <param name="router">The message router containing the subscriber registry.</param>
/// <param name="evt">The command event to publish.</param>
let publishCommandEvent (router: MessageRouter) (evt: CommandEvent) =
    task {
        for kvp in router.CommandSubscribers do
            try
                do! kvp.Value evt
            with
            | _ -> ()
    }

/// <summary>
/// Submit a simulation command to the router. The command is forwarded to the connected simulation
/// via a bounded channel; if no simulation is connected or the channel is full, the command is dropped.
/// The command is also published to audit subscribers regardless.
/// </summary>
/// <param name="router">The message router to submit through.</param>
/// <param name="cmd">The simulation command to forward.</param>
/// <returns>A CommandAck indicating whether the command was forwarded or dropped.</returns>
let submitCommand (router: MessageRouter) (cmd: SimulationCommand) =
    // Track incoming command
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics

    // Try to write to the command channel; drop if full or no simulation connected
    let written =
        lock router.SimulationLock (fun () ->
            if router.SimulationConnected then
                router.CommandChannel.Writer.TryWrite(cmd)
            else
                false)

    if written then
        MetricsCounter.incrementSent 1 (int64 (cmd.CalculateSize())) router.Metrics

    // Publish to command audit subscribers
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

/// <summary>
/// Submit a view command to the router. The command is written to the view command channel
/// for the connected viewer and published to audit subscribers.
/// </summary>
/// <param name="router">The message router to submit through.</param>
/// <param name="cmd">The view command to forward.</param>
/// <returns>A CommandAck confirming acceptance.</returns>
let submitViewCommand (router: MessageRouter) (cmd: ViewCommand) =
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics
    // Try to write to the view command channel; drop if full or no viewer connected
    let _written = router.ViewCommandChannel.Writer.TryWrite(cmd)

    // Publish to command audit subscribers
    let evt = CommandEvent()
    evt.ViewCommand <- cmd
    publishCommandEvent router evt |> ignore

    CommandAck(
        Success = true,
        Message = "View command accepted"
    )

/// <summary>Subscribe to live simulation state updates. The callback is invoked each time a new state is published.</summary>
/// <param name="router">The message router to subscribe to.</param>
/// <param name="callback">Async callback invoked with each new simulation state.</param>
/// <returns>A SubscriptionId for later unsubscription.</returns>
let subscribe (router: MessageRouter) (callback: SimulationState -> Task) =
    let id = Guid.NewGuid()
    router.Subscribers.TryAdd(id, callback) |> ignore
    SubscriptionId id

/// <summary>Remove a state stream subscription so the callback is no longer invoked.</summary>
/// <param name="router">The message router to unsubscribe from.</param>
let unsubscribe (router: MessageRouter) (SubscriptionId id) =
    router.Subscribers.TryRemove(id) |> ignore

/// <summary>Process query responses from a simulation state update.</summary>
/// <param name="state">The simulation state that may contain query responses.</param>
let processQueryResponses (state: SimulationState) =
    if not (isNull state) && state.QueryResponses.Count > 0 then
        for qr in state.QueryResponses do
            match pendingQueries.TryRemove(qr.CorrelationId) with
            | true, tcs -> tcs.TrySetResult(qr) |> ignore
            | _ -> ()

/// <summary>Publish a simulation state to all subscribers and update the state cache. Errors in individual callbacks are silently ignored.</summary>
/// <param name="router">The message router containing subscribers and the state cache.</param>
/// <param name="state">The simulation state snapshot to broadcast.</param>
let publishState (router: MessageRouter) (state: SimulationState) =
    task {
        MetricsCounter.incrementReceived 1 (int64 (state.CalculateSize())) router.Metrics

        // Process query responses before caching/broadcasting
        processQueryResponses state
        StateCache.update router.StateCache state

        for kvp in router.Subscribers do
            try
                do! kvp.Value state
            with
            | _ -> ()
    }

/// <summary>Retrieve the latest cached simulation state for late-joining clients.</summary>
/// <param name="router">The message router to query.</param>
/// <returns>The most recent SimulationState if available, otherwise None.</returns>
let getLatestState (router: MessageRouter) =
    StateCache.get router.StateCache

/// <summary>Attempt to register as the active simulation. Only one simulation may be connected at a time.</summary>
/// <param name="router">The message router to register with.</param>
/// <returns>True if registration succeeded; false if a simulation is already connected.</returns>
let tryConnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        if router.SimulationConnected then
            false
        else
            router.SimulationConnected <- true
            true)

/// <summary>Unregister the active simulation, allowing a new one to connect.</summary>
/// <param name="router">The message router to unregister from.</param>
let disconnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        router.SimulationConnected <- false)

/// <summary>Capture a point-in-time snapshot of the server's throughput metrics.</summary>
/// <param name="router">The message router to query.</param>
/// <returns>A ServiceMetricsReport with current counter values.</returns>
let getMetrics (router: MessageRouter) =
    MetricsCounter.snapshot router.Metrics

/// <summary>Access the raw metrics state, typically used to set up periodic logging.</summary>
/// <param name="router">The message router to query.</param>
/// <returns>The underlying MetricsState for direct use with MetricsCounter functions.</returns>
let metricsState (router: MessageRouter) =
    router.Metrics

/// <summary>Submit a batch of simulation commands (max 100). Each command is individually forwarded and its result recorded.</summary>
/// <param name="router">The message router to submit through.</param>
/// <param name="batch">The batch request containing up to 100 simulation commands.</param>
/// <returns>A BatchResponse with per-command results and total execution time.</returns>
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

/// <summary>Submit a batch of view commands (max 100). Each command is individually forwarded and its result recorded.</summary>
/// <param name="router">The message router to submit through.</param>
/// <param name="batch">The batch request containing up to 100 view commands.</param>
/// <returns>A BatchResponse with per-command results and total execution time.</returns>
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

/// <summary>Read a pending simulation command from the channel. Blocks asynchronously until a command is available or cancellation occurs.</summary>
/// <param name="router">The message router to read from.</param>
/// <param name="ct">Cancellation token to abort the wait.</param>
/// <returns>Some command if one was read, or None on cancellation or channel closure.</returns>
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
/// <param name="router">The message router to submit through.</param>
/// <param name="queryRequest">The query request to forward.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The query response from the simulation.</returns>
let submitQuery (router: MessageRouter) (queryRequest: QueryRequest) (ct: CancellationToken) =
    task {
        let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
        use _reg = ct.Register(fun () -> tcs.TrySetCanceled() |> ignore)
        pendingQueries.TryAdd(queryRequest.CorrelationId, tcs) |> ignore
        try
            // Send as a command
            let cmd = SimulationCommand(QueryRequest = queryRequest)
            let _ack = submitCommand router cmd
            // Wait for the response (completed when state with matching correlation arrives)
            let! response = tcs.Task
            return response
        finally
            pendingQueries.TryRemove(queryRequest.CorrelationId) |> ignore
    }

/// <summary>Read a pending view command from the channel. Blocks asynchronously until a command is available or cancellation occurs.</summary>
/// <param name="router">The message router to read from.</param>
/// <param name="ct">Cancellation token to abort the wait.</param>
/// <returns>Some command if one was read, or None on cancellation or channel closure.</returns>
let readViewCommand (router: MessageRouter) (ct: CancellationToken) =
    task {
        try
            let! cmd = router.ViewCommandChannel.Reader.ReadAsync(ct).AsTask()
            return Some cmd
        with
        | :? OperationCanceledException -> return None
        | :? ChannelClosedException -> return None
    }
