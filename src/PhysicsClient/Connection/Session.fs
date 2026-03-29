/// <summary>Manages gRPC connections to the physics server, including state streaming and automatic reconnection.</summary>
module PhysicsClient.Session

open System
open System.Collections.Concurrent
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts

/// <summary>Opaque session handle that holds the gRPC channel, client, body registry, and cached simulation state.</summary>
type Session =
    { Channel: GrpcChannel
      Client: PhysicsHub.PhysicsHubClient
      ServerAddress: string
      Cts: CancellationTokenSource
      BodyRegistry: ConcurrentDictionary<string, string>
      mutable LatestState: SimulationState option
      mutable LastStateUpdate: DateTime
      mutable IsConnected: bool
      MeshResolver: MeshResolver.MeshResolverState
      /// Cached semi-static body properties from the property event stream.
      BodyPropertiesCache: ConcurrentDictionary<string, BodyProperties>
      /// Cached constraints from the property event stream.
      mutable CachedConstraints: ConstraintState list
      /// Cached registered shapes from the property event stream.
      mutable CachedRegisteredShapes: RegisteredShapeState list }

let private createChannel (address: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    let options = GrpcChannelOptions(HttpHandler = handler)
    // For plain HTTP, force HTTP/2 without TLS upgrade
    if address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) then
        options.HttpVersion <- System.Net.HttpVersion.Version20
        options.HttpVersionPolicy <- System.Net.Http.HttpVersionPolicy.RequestVersionExact
    GrpcChannel.ForAddress(address, options)

/// Reconstruct a full Body from a BodyPose (tick data) + cached BodyProperties.
let private mergeBodyPoseWithProps (pose: BodyPose) (props: BodyProperties option) =
    let b = Body()
    b.Id <- pose.Id
    b.Position <- pose.Position
    b.Orientation <- pose.Orientation
    b.Velocity <- pose.Velocity
    b.AngularVelocity <- pose.AngularVelocity
    match props with
    | Some p ->
        b.Shape <- p.Shape
        b.Color <- p.Color
        b.Mass <- p.Mass
        b.IsStatic <- p.IsStatic
        b.MotionType <- p.MotionType
        b.CollisionGroup <- p.CollisionGroup
        b.CollisionMask <- p.CollisionMask
        b.Material <- p.Material
    | None -> ()
    b

/// Reconstruct a full Body from a cached static BodyProperties (not in tick stream).
let private staticBodyFromProps (props: BodyProperties) =
    let b = Body()
    b.Id <- props.Id
    b.Shape <- props.Shape
    b.Color <- props.Color
    b.Mass <- props.Mass
    b.IsStatic <- props.IsStatic
    b.MotionType <- props.MotionType
    b.CollisionGroup <- props.CollisionGroup
    b.CollisionMask <- props.CollisionMask
    b.Material <- props.Material
    b.Position <- props.Position
    b.Orientation <- props.Orientation
    b.Velocity <- Vec3()
    b.AngularVelocity <- Vec3()
    b

/// Reconstruct a full SimulationState from a lean TickState + cached BodyProperties.
let private reconstructState (session: Session) (tick: TickState) =
    let state = SimulationState()
    state.Time <- tick.Time
    state.Running <- tick.Running
    state.TickMs <- tick.TickMs
    state.SerializeMs <- tick.SerializeMs
    // Dynamic bodies: merge pose from tick + properties from cache
    let dynamicIds = System.Collections.Generic.HashSet<string>()
    for pose in tick.Bodies do
        dynamicIds.Add(pose.Id) |> ignore
        let props =
            match session.BodyPropertiesCache.TryGetValue(pose.Id) with
            | true, p -> Some p
            | _ -> None
        state.Bodies.Add(mergeBodyPoseWithProps pose props)
    // Static bodies: from cache only (not in tick stream)
    for kvp in session.BodyPropertiesCache do
        if kvp.Value.IsStatic && not (dynamicIds.Contains(kvp.Key)) then
            state.Bodies.Add(staticBodyFromProps kvp.Value)
    // Constraints and registered shapes from cache
    for cs in session.CachedConstraints do
        state.Constraints.Add(cs)
    for rs in session.CachedRegisteredShapes do
        state.RegisteredShapes.Add(rs)
    // Query responses
    for qr in tick.QueryResponses do
        state.QueryResponses.Add(qr)
    state

/// Process a PropertyEvent and update the session's caches.
let private processPropertyEvent (session: Session) (evt: PropertyEvent) =
    match evt.EventCase with
    | PropertyEvent.EventOneofCase.BodyCreated ->
        session.BodyPropertiesCache.[evt.BodyCreated.Id] <- evt.BodyCreated
    | PropertyEvent.EventOneofCase.BodyUpdated ->
        session.BodyPropertiesCache.[evt.BodyUpdated.Id] <- evt.BodyUpdated
    | PropertyEvent.EventOneofCase.BodyRemoved ->
        let mutable _removed = Unchecked.defaultof<BodyProperties>
        if not (session.BodyPropertiesCache.TryRemove(evt.BodyRemoved, &_removed)) then
            System.Diagnostics.Trace.TraceWarning($"Session: body {evt.BodyRemoved} not found in properties cache")
    | PropertyEvent.EventOneofCase.Snapshot ->
        session.BodyPropertiesCache.Clear()
        for bp in evt.Snapshot.Bodies do
            session.BodyPropertiesCache.[bp.Id] <- bp
        session.CachedConstraints <- evt.Snapshot.Constraints |> Seq.toList
        session.CachedRegisteredShapes <- evt.Snapshot.RegisteredShapes |> Seq.toList
    | PropertyEvent.EventOneofCase.ConstraintsSnapshot ->
        session.CachedConstraints <- evt.ConstraintsSnapshot.Constraints |> Seq.toList
    | PropertyEvent.EventOneofCase.RegisteredShapesSnapshot ->
        session.CachedRegisteredShapes <- evt.RegisteredShapesSnapshot.RegisteredShapes |> Seq.toList
    | _ -> ()
    // Process new mesh definitions piggyback
    if evt.NewMeshes.Count > 0 then
        MeshResolver.processNewMeshes evt.NewMeshes session.MeshResolver

let private startPropertyStream (session: Session) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not session.Cts.Token.IsCancellationRequested && session.IsConnected do
                try
                    use call = session.Client.StreamProperties(StateRequest(), cancellationToken = session.Cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    while not session.Cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(session.Cts.Token)
                        if hasNext then
                            processPropertyEvent session stream.Current
                with
                | :? OperationCanceledException -> ()
                | :? RpcException when session.Cts.Token.IsCancellationRequested -> ()
                | :? RpcException ->
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
                | _ ->
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
        } :> Task) |> ignore

let private startStateStream (session: Session) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not session.Cts.Token.IsCancellationRequested && session.IsConnected do
                try
                    use call = session.Client.StreamState(StateRequest(), cancellationToken = session.Cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    while not session.Cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(session.Cts.Token)
                        if hasNext then
                            let tick = stream.Current
                            // Reconstruct full SimulationState from lean tick + cached properties
                            let state = reconstructState session tick
                            // Fetch any unresolved CachedShapeRef mesh IDs
                            let missingIds =
                                state.Bodies
                                |> Seq.choose (fun b ->
                                    if not (isNull b.Shape) && b.Shape.ShapeCase = Shape.ShapeOneofCase.CachedRef then
                                        let id = b.Shape.CachedRef.MeshId
                                        match MeshResolver.resolve id session.MeshResolver with
                                        | None -> Some id
                                        | Some _ -> None
                                    else None)
                                |> Seq.distinct |> Seq.toList
                            if not missingIds.IsEmpty then
                                MeshResolver.fetchMissingSync missingIds session.MeshResolver
                            session.LatestState <- Some state
                            session.LastStateUpdate <- DateTime.UtcNow
                with
                | :? OperationCanceledException -> ()
                | :? RpcException when session.Cts.Token.IsCancellationRequested -> ()
                | :? RpcException ->
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
                | _ ->
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
        } :> Task) |> ignore

/// <summary>Connects to the physics server at the given address and starts background state + property streams.</summary>
/// <param name="serverAddress">The server URL (e.g., "http://localhost:5180" or "https://localhost:7180").</param>
/// <returns>Ok with the connected session, or Error with a failure message.</returns>
let connect (serverAddress: string) : Result<Session, string> =
    try
        let channel = createChannel serverAddress
        let grpcClient = PhysicsHub.PhysicsHubClient(channel)
        let session =
            { Channel = channel
              Client = grpcClient
              ServerAddress = serverAddress
              Cts = new CancellationTokenSource()
              BodyRegistry = ConcurrentDictionary<string, string>()
              LatestState = None
              LastStateUpdate = DateTime.UtcNow
              IsConnected = true
              MeshResolver = MeshResolver.create grpcClient
              BodyPropertiesCache = ConcurrentDictionary<string, BodyProperties>()
              CachedConstraints = []
              CachedRegisteredShapes = [] }
        startPropertyStream session
        startStateStream session
        Ok session
    with ex ->
        Error $"Failed to connect to {serverAddress}: {ex.Message}"

/// <summary>Disconnects from the server, cancels the state stream, and disposes the gRPC channel.</summary>
/// <param name="session">The session to disconnect.</param>
let disconnect (session: Session) : unit =
    session.IsConnected <- false
    try session.Cts.Cancel() with _ -> ()
    try session.Cts.Dispose() with _ -> ()
    try session.Channel.Dispose() with _ -> ()

/// <summary>Disconnects the current session and creates a new connection to the same server address.</summary>
/// <param name="session">The session to reconnect.</param>
/// <returns>Ok with a fresh session, or Error with a failure message.</returns>
let reconnect (session: Session) : Result<Session, string> =
    disconnect session
    connect session.ServerAddress

/// <summary>Returns whether the session is currently connected to the server.</summary>
/// <param name="session">The session to check.</param>
let isConnected (session: Session) : bool =
    session.IsConnected

/// <summary>Gets the underlying gRPC client from a session. Used internally by command modules.</summary>
let internal client (session: Session) : PhysicsHub.PhysicsHubClient =
    session.Client

/// <summary>Gets the local body registry that maps body IDs to their shape kinds.</summary>
let internal bodyRegistry (session: Session) : ConcurrentDictionary<string, string> =
    session.BodyRegistry

/// <summary>Gets the most recently received simulation state from the background stream, if any.</summary>
let internal latestState (session: Session) : SimulationState option =
    session.LatestState

/// <summary>Gets the UTC timestamp of the last state update received from the server.</summary>
let internal lastStateUpdate (session: Session) : DateTime =
    session.LastStateUpdate

/// <summary>Gets the mesh resolver state for resolving CachedShapeRef.</summary>
let internal meshResolver (session: Session) : MeshResolver.MeshResolverState =
    session.MeshResolver

/// <summary>Clears all client-side caches to ensure a clean state after a confirmed reset.</summary>
let internal clearCaches (session: Session) : unit =
    session.BodyRegistry.Clear()
    session.BodyPropertiesCache.Clear()
    session.CachedConstraints <- []
    session.CachedRegisteredShapes <- []
    session.LatestState <- None

/// <summary>Sends a simulation command to the server and returns the acknowledgement result. Marks the session as disconnected on gRPC transport errors.</summary>
let internal sendCommand (session: Session) (cmd: SimulationCommand) : Result<unit, string> =
    if not session.IsConnected then
        Error "Not connected to server"
    else
        try
            let ack = session.Client.SendCommand(cmd)
            if ack.Success then Ok ()
            else Error ack.Message
        with
        | :? RpcException as ex ->
            session.IsConnected <- false
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Command failed: {ex.Message}"

/// <summary>Sends a view command to the server and returns the acknowledgement result. Marks the session as disconnected on gRPC transport errors.</summary>
let internal sendViewCommand (session: Session) (cmd: ViewCommand) : Result<unit, string> =
    if not session.IsConnected then
        Error "Not connected to server"
    else
        try
            let ack = session.Client.SendViewCommand(cmd)
            if ack.Success then Ok ()
            else Error ack.Message
        with
        | :? RpcException as ex ->
            session.IsConnected <- false
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Command failed: {ex.Message}"
