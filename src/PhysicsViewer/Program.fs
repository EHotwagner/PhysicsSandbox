module PhysicsViewer.Program

open System
open System.Threading
open System.Threading.Tasks
open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Games
open Stride.CommunityToolkit.Skyboxes
open Stride.CommunityToolkit.Rendering.Compositing
open Stride.CommunityToolkit.Rendering.ProceduralModels
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.SceneManager
open PhysicsViewer.CameraController
open PhysicsViewer.Rendering
open Stride.Rendering.Lights
open PhysicsViewer.Settings
open PhysicsViewer.Streaming

let game = new Game()

let fpsState = FpsCounter.create 30.0f
let mutable sceneState = SceneManager.create ()
let mutable debugState = DebugRenderer.create ()
let mutable cameraState = CameraController.defaultCamera ()
let mutable cameraEntity: Entity option = None

// Display settings state
let mutable viewerSettings = ViewerSettings.defaultSettings ()
let mutable displayState: DisplayManager.DisplayState option = None
let mutable overlayState = SettingsOverlay.create (ViewerSettings.defaultSettings ())

// Shared state: written by background stream, read by game update loop
let mutable latestSimState: SimulationState = null
let viewCmdQueue = System.Collections.Concurrent.ConcurrentQueue<ViewCommand>()
let mutable stateVersion = 0
let mutable lastAppliedVersion = 0

// Mesh resolver for on-demand geometry fetch
let mutable meshResolverState: MeshResolver.MeshResolverState option = None

// Body properties cache for split-channel reconstruction
let bodyPropsCache = System.Collections.Concurrent.ConcurrentDictionary<string, BodyProperties>()
let mutable cachedConstraints: ConstraintState list = []
let mutable cachedRegisteredShapes: RegisteredShapeState list = []

// Viewer metrics counters
let mutable viewerMsgRecv = 0L
let mutable viewerBytesRecv = 0L

let cts = new CancellationTokenSource()

// ServiceDefaults host for observability + service discovery (FR-013, constitution VII)
let hostBuilder = Host.CreateApplicationBuilder()
do hostBuilder.AddServiceDefaults() |> ignore
let host = hostBuilder.Build()
let logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PhysicsViewer")
let lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>()
do lifetime.ApplicationStopping.Register(fun () -> cts.Cancel()) |> ignore

let resolveServerAddress () =
    match Environment.GetEnvironmentVariable("services__server__https__0") with
    | null | "" ->
        match Environment.GetEnvironmentVariable("services__server__http__0") with
        | null | "" -> "http://localhost:5180"
        | addr -> addr
    | addr -> addr

let createGrpcChannel (serverAddress: string) =
    let handler = new System.Net.Http.SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    Grpc.Net.Client.GrpcChannel.ForAddress(serverAddress, Grpc.Net.Client.GrpcChannelOptions(HttpHandler = handler))

let private reconstructSimState (tick: TickState) =
    let state = SimulationState()
    state.Time <- tick.Time
    state.Running <- tick.Running
    state.TickMs <- tick.TickMs
    state.SerializeMs <- tick.SerializeMs
    let dynamicIds = System.Collections.Generic.HashSet<string>()
    for pose in tick.Bodies do
        dynamicIds.Add(pose.Id) |> ignore
        let b = Body()
        b.Id <- pose.Id
        b.Position <- pose.Position
        b.Orientation <- pose.Orientation
        b.Velocity <- pose.Velocity
        b.AngularVelocity <- pose.AngularVelocity
        match bodyPropsCache.TryGetValue(pose.Id) with
        | true, p ->
            b.Shape <- p.Shape
            b.Color <- p.Color
            b.Mass <- p.Mass
            b.IsStatic <- p.IsStatic
            b.MotionType <- p.MotionType
            b.CollisionGroup <- p.CollisionGroup
            b.CollisionMask <- p.CollisionMask
            b.Material <- p.Material
        | _ -> ()
        state.Bodies.Add(b)
    for kvp in bodyPropsCache do
        if kvp.Value.IsStatic && not (dynamicIds.Contains(kvp.Key)) then
            let b = Body()
            b.Id <- kvp.Value.Id
            b.Shape <- kvp.Value.Shape
            b.Color <- kvp.Value.Color
            b.Mass <- kvp.Value.Mass
            b.IsStatic <- kvp.Value.IsStatic
            b.MotionType <- kvp.Value.MotionType
            b.CollisionGroup <- kvp.Value.CollisionGroup
            b.CollisionMask <- kvp.Value.CollisionMask
            b.Material <- kvp.Value.Material
            b.Position <- kvp.Value.Position
            b.Orientation <- kvp.Value.Orientation
            b.Velocity <- Vec3()
            b.AngularVelocity <- Vec3()
            state.Bodies.Add(b)
    for cs in cachedConstraints do
        state.Constraints.Add(cs)
    for rs in cachedRegisteredShapes do
        state.RegisteredShapes.Add(rs)
    for qr in tick.QueryResponses do
        state.QueryResponses.Add(qr)
    state

let private processViewerPropertyEvent (evt: PropertyEvent) =
    match evt.EventCase with
    | PropertyEvent.EventOneofCase.BodyCreated ->
        bodyPropsCache.[evt.BodyCreated.Id] <- evt.BodyCreated
    | PropertyEvent.EventOneofCase.BodyUpdated ->
        bodyPropsCache.[evt.BodyUpdated.Id] <- evt.BodyUpdated
    | PropertyEvent.EventOneofCase.BodyRemoved ->
        bodyPropsCache.TryRemove(evt.BodyRemoved) |> ignore
    | PropertyEvent.EventOneofCase.Snapshot ->
        bodyPropsCache.Clear()
        for bp in evt.Snapshot.Bodies do
            bodyPropsCache.[bp.Id] <- bp
        cachedConstraints <- evt.Snapshot.Constraints |> Seq.toList
        cachedRegisteredShapes <- evt.Snapshot.RegisteredShapes |> Seq.toList
    | PropertyEvent.EventOneofCase.ConstraintsSnapshot ->
        cachedConstraints <- evt.ConstraintsSnapshot.Constraints |> Seq.toList
    | PropertyEvent.EventOneofCase.RegisteredShapesSnapshot ->
        cachedRegisteredShapes <- evt.RegisteredShapesSnapshot.RegisteredShapes |> Seq.toList
    | _ -> ()
    // Process new mesh definitions piggyback
    if evt.NewMeshes.Count > 0 then
        match meshResolverState with
        | Some resolver -> MeshResolver.processNewMeshes evt.NewMeshes resolver
        | None -> ()

let startPropertyStream (serverAddress: string) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not cts.Token.IsCancellationRequested do
                try
                    use channel = createGrpcChannel serverAddress
                    let client = PhysicsHub.PhysicsHubClient(channel)
                    // Viewer uses exclude_velocity=true for leaner tick stream
                    use call = client.StreamProperties(StateRequest(ExcludeVelocity = true), cancellationToken = cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    logger.LogInformation("Property stream connected to {Address}", serverAddress)
                    while not cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(cts.Token)
                        if hasNext then
                            processViewerPropertyEvent stream.Current
                with
                | :? OperationCanceledException -> ()
                | ex when not cts.Token.IsCancellationRequested ->
                    logger.LogWarning("Property stream error: {Message}, reconnecting in {Delay}ms", ex.Message, delay)
                    do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                    delay <- min (delay * 2) 30000
        } :> Task) |> ignore

let startStateStream (serverAddress: string) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not cts.Token.IsCancellationRequested do
                try
                    use channel = createGrpcChannel serverAddress
                    let client = PhysicsHub.PhysicsHubClient(channel)
                    // Viewer uses exclude_velocity=true for leaner tick stream
                    use call = client.StreamState(StateRequest(ExcludeVelocity = true), cancellationToken = cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    logger.LogInformation("State stream connected to {Address}", serverAddress)
                    if meshResolverState.IsNone then
                        meshResolverState <- Some (MeshResolver.create client)

                    while not cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(cts.Token)
                        if hasNext then
                            let tick = stream.Current
                            let state = reconstructSimState tick
                            // Fetch any unresolved CachedShapeRef mesh IDs
                            match meshResolverState with
                            | Some resolver ->
                                let missingIds =
                                    state.Bodies
                                    |> Seq.choose (fun b ->
                                        if not (isNull b.Shape) && b.Shape.ShapeCase = Shape.ShapeOneofCase.CachedRef then
                                            let id = b.Shape.CachedRef.MeshId
                                            match MeshResolver.resolve id resolver with
                                            | None -> Some id
                                            | Some _ -> None
                                        else None)
                                    |> Seq.distinct |> Seq.toList
                                if not missingIds.IsEmpty then
                                    Async.Start(MeshResolver.fetchMissing missingIds resolver)
                            | None -> ()
                            Volatile.Write(&latestSimState, state)
                            Interlocked.Increment(&stateVersion) |> ignore
                            Interlocked.Increment(&viewerMsgRecv) |> ignore
                            Interlocked.Add(&viewerBytesRecv, int64 (tick.CalculateSize())) |> ignore
                with
                | :? OperationCanceledException -> ()
                | ex when not cts.Token.IsCancellationRequested ->
                    logger.LogWarning("State stream error: {Message}, reconnecting in {Delay}ms", ex.Message, delay)
                    do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                    delay <- min (delay * 2) 30000
        } :> Task) |> ignore

let startViewCommandStream (serverAddress: string) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not cts.Token.IsCancellationRequested do
                try
                    use channel = createGrpcChannel serverAddress
                    let client = PhysicsHub.PhysicsHubClient(channel)
                    use call = client.StreamViewCommands(StateRequest(), cancellationToken = cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    while not cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(cts.Token)
                        if hasNext then
                            let cmd = stream.Current
                            logger.LogInformation("ViewCmd RECV: {Case}", cmd.CommandCase)
                            viewCmdQueue.Enqueue(cmd)
                            Interlocked.Increment(&viewerMsgRecv) |> ignore
                            Interlocked.Add(&viewerBytesRecv, int64 (stream.Current.CalculateSize())) |> ignore
                with
                | :? OperationCanceledException -> ()
                | ex when not cts.Token.IsCancellationRequested ->
                    do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                    delay <- min (delay * 2) 30000
        } :> Task) |> ignore

let start (scene: Scene) =
    game.AddGraphicsCompositor().AddCleanUIStage() |> ignore
    let camEntity = game.Add3DCamera()
    cameraEntity <- Some camEntity
    game.AddDirectionalLight() |> ignore
    // Add ambient light so objects are visible from all angles
    let ambientEntity = Entity("AmbientLight")
    let ambientLight = LightComponent()
    let ambient = LightAmbient()
    ambient.Color <- Stride.Rendering.Colors.ColorRgbProvider(Color3(0.3f, 0.3f, 0.3f))
    ambientLight.Type <- ambient
    ambientLight.Intensity <- 0.5f
    ambientEntity.Add(ambientLight)
    ambientEntity.Scene <- game.SceneSystem.SceneInstance.RootScene
    // Create ground plane without Bepu physics (visual-only viewer)
    let groundMaterial = game.CreateMaterial(Color.DarkGray, 0.0f, 0.1f)
    let groundOpts = Primitive3DEntityOptions(Material = groundMaterial, EntityName = "Ground")
    let ground = game.Create3DPrimitive(PrimitiveModelType.Plane, groundOpts)
    ground.Scene <- game.SceneSystem.SceneInstance.RootScene
    game.AddSkybox() |> ignore

    GameExtensions.AddGroundGizmo(
        game,
        System.Nullable<Vector3>(Vector3(-5f, 0.1f, -5f)),
        showAxisName = true)

    CameraController.applyToCamera cameraState camEntity

    // Load and apply persisted viewer settings (fullscreen, resolution, quality)
    viewerSettings <- ViewerSettings.load ()
    displayState <- Some (DisplayManager.create game viewerSettings)
    overlayState <- SettingsOverlay.create viewerSettings
    logger.LogInformation("Viewer settings loaded: {Width}x{Height}, fullscreen={Fs}",
        viewerSettings.ResolutionWidth, viewerSettings.ResolutionHeight, viewerSettings.IsFullscreen)

    game.Window.Title <- "PhysicsSandbox Viewer"

    let addr = resolveServerAddress ()
    logger.LogInformation("Viewer starting, server address: {Address}", addr)
    startPropertyStream addr
    startStateStream addr
    startViewCommandStream addr

    // Start periodic viewer metrics logging
    new Timer(
        (fun _ ->
            logger.LogInformation(
                "Metrics [PhysicsViewer] recv={MsgRecv} bytesRecv={BytesRecv}",
                Interlocked.Read(&viewerMsgRecv),
                Interlocked.Read(&viewerBytesRecv))),
        null,
        TimeSpan.FromSeconds(10.0),
        TimeSpan.FromSeconds(10.0))
    |> ignore

let update (scene: Scene) (time: GameTime) =
    let dt = float32 time.Elapsed.TotalSeconds

    // Check for new simulation state (lock-free: just compare version counter)
    let currentVersion = Volatile.Read(&stateVersion)
    if currentVersion <> lastAppliedVersion then
        let simState = Volatile.Read(&latestSimState)
        if not (isNull simState) then
            sceneState <- SceneManager.applyState game scene sceneState simState meshResolverState
            debugState <- DebugRenderer.updateShapes game scene debugState simState
            debugState <- DebugRenderer.updateConstraints game scene debugState simState
            lastAppliedVersion <- currentVersion

    // Build body position map for camera modes
    let bodyPositions =
        let sim = Volatile.Read(&latestSimState)
        if isNull sim || isNull sim.Bodies then Map.empty
        else
            sim.Bodies
            |> Seq.map (fun b ->
                b.Id,
                if isNull b.Position then Vector3.Zero
                else Vector3(float32 b.Position.X, float32 b.Position.Y, float32 b.Position.Z))
            |> Map.ofSeq

    // Process all queued view commands
    let mutable viewCmd = Unchecked.defaultof<ViewCommand>
    while viewCmdQueue.TryDequeue(&viewCmd) do
        match viewCmd.CommandCase with
        | ViewCommand.CommandOneofCase.SetCamera ->
            cameraState <- CameraController.cancelMode cameraState
            cameraState <- CameraController.applySetCamera viewCmd.SetCamera cameraState
        | ViewCommand.CommandOneofCase.SetZoom ->
            cameraState <- CameraController.applySetZoom viewCmd.SetZoom cameraState
        | ViewCommand.CommandOneofCase.ToggleWireframe ->
            sceneState <- SceneManager.applyWireframe game viewCmd.ToggleWireframe sceneState
        | ViewCommand.CommandOneofCase.SetDemoMetadata ->
            sceneState <- SceneManager.applyDemoMetadata viewCmd.SetDemoMetadata sceneState
        | ViewCommand.CommandOneofCase.SmoothCamera ->
            logger.LogInformation("ViewCommand: SmoothCamera dur={Dur:F2}s", viewCmd.SmoothCamera.DurationSeconds)
            let cmd = viewCmd.SmoothCamera
            let endPos = if isNull cmd.Position then CameraController.position cameraState else Vector3(float32 cmd.Position.X, float32 cmd.Position.Y, float32 cmd.Position.Z)
            let endTarget = if isNull cmd.Target then CameraController.target cameraState else Vector3(float32 cmd.Target.X, float32 cmd.Target.Y, float32 cmd.Target.Z)
            let endZoom = if cmd.ZoomLevel = 0.0 then CameraController.zoomLevel cameraState else cmd.ZoomLevel
            let mode = CameraController.Transitioning (
                CameraController.position cameraState,
                CameraController.target cameraState,
                CameraController.zoomLevel cameraState,
                endPos, endTarget, endZoom, 0f, float32 cmd.DurationSeconds)
            cameraState <- CameraController.setMode mode cameraState
        | ViewCommand.CommandOneofCase.CameraLookAt ->
            logger.LogInformation("ViewCommand: CameraLookAt body={BodyId} dur={Dur:F2}s", viewCmd.CameraLookAt.BodyId, viewCmd.CameraLookAt.DurationSeconds)
            let cmd = viewCmd.CameraLookAt
            let mode = CameraController.LookingAt (cmd.BodyId, CameraController.target cameraState, 0f, float32 cmd.DurationSeconds)
            cameraState <- CameraController.setMode mode cameraState
        | ViewCommand.CommandOneofCase.CameraFollow ->
            logger.LogInformation("ViewCommand: CameraFollow body={BodyId}", viewCmd.CameraFollow.BodyId)
            let cmd = viewCmd.CameraFollow
            cameraState <- CameraController.setMode (CameraController.Following cmd.BodyId) cameraState
        | ViewCommand.CommandOneofCase.CameraOrbit ->
            let cmd = viewCmd.CameraOrbit
            let deg = if cmd.Degrees = 0.0 then 360.0 else cmd.Degrees
            // Compute current angle relative to body
            let bodyPos = Map.tryFind cmd.BodyId bodyPositions |> Option.defaultValue Vector3.Zero
            let pos = CameraController.position cameraState
            let dx = pos.X - bodyPos.X
            let dz = pos.Z - bodyPos.Z
            let startAngle = atan2 dz dx
            let radius = sqrt (dx * dx + dz * dz)
            let height = pos.Y - bodyPos.Y
            logger.LogInformation("CameraOrbit: bodyId={BodyId} bodyPos={BodyPos} camPos={CamPos} radius={Radius:F2} height={Height:F2} angle={Angle:F2} deg={Deg:F0}",
                cmd.BodyId, bodyPos, pos, radius, height, startAngle, deg)
            let mode = CameraController.Orbiting (cmd.BodyId, startAngle, float32 deg, max radius 2f, height, 0f, float32 cmd.DurationSeconds)
            cameraState <- CameraController.setMode mode cameraState
        | ViewCommand.CommandOneofCase.CameraChase ->
            logger.LogInformation("ViewCommand: CameraChase body={BodyId}", viewCmd.CameraChase.BodyId)
            let cmd = viewCmd.CameraChase
            let offset =
                if isNull cmd.Offset then Vector3(0f, 5f, 10f)
                else Vector3(float32 cmd.Offset.X, float32 cmd.Offset.Y, float32 cmd.Offset.Z)
            cameraState <- CameraController.setMode (CameraController.Chasing (cmd.BodyId, offset)) cameraState
        | ViewCommand.CommandOneofCase.CameraFrameBodies ->
            let cmd = viewCmd.CameraFrameBodies
            let ids = cmd.BodyIds |> Seq.toList
            if not ids.IsEmpty then
                cameraState <- CameraController.setMode (CameraController.Framing ids) cameraState
        | ViewCommand.CommandOneofCase.CameraShake ->
            let cmd = viewCmd.CameraShake
            let mode = CameraController.Shaking (
                CameraController.position cameraState,
                CameraController.target cameraState,
                float32 cmd.Intensity, 0f, float32 cmd.DurationSeconds)
            cameraState <- CameraController.setMode mode cameraState
        | ViewCommand.CommandOneofCase.CameraStop ->
            logger.LogInformation("ViewCommand: CameraStop (was active={Active})", CameraController.isActive cameraState)
            cameraState <- CameraController.cancelMode cameraState
        | ViewCommand.CommandOneofCase.SetNarration ->
            sceneState <- SceneManager.applyNarration viewCmd.SetNarration.Text sceneState
        | _ -> ()

    // Update camera mode each frame
    cameraState <- CameraController.updateCameraMode dt bodyPositions cameraState

    // Toggle debug visualization with F3
    if game.Input.IsKeyPressed(Stride.Input.Keys.F3) then
        let newEnabled = not (DebugRenderer.isEnabled debugState)
        debugState <- DebugRenderer.setEnabled newEnabled debugState
        logger.LogInformation("Debug visualization {State}", if newEnabled then "enabled" else "disabled")

    // F11: Toggle borderless windowed fullscreen
    if game.Input.IsKeyPressed(Stride.Input.Keys.F11) then
        match displayState with
        | Some ds ->
            let newDs = DisplayManager.toggleFullscreen ds
            displayState <- Some newDs
            viewerSettings <- DisplayManager.currentSettings newDs
            ViewerSettings.save viewerSettings
            logger.LogInformation("Fullscreen toggled: {Fs}", viewerSettings.IsFullscreen)
        | None -> ()

    // Escape: exit fullscreen
    if game.Input.IsKeyPressed(Stride.Input.Keys.Escape) && viewerSettings.IsFullscreen then
        match displayState with
        | Some ds ->
            let newDs = DisplayManager.toggleFullscreen ds
            displayState <- Some newDs
            viewerSettings <- DisplayManager.currentSettings newDs
            ViewerSettings.save viewerSettings
            logger.LogInformation("Exited fullscreen via Escape")
        | None -> ()

    // F2: Toggle settings overlay
    if game.Input.IsKeyPressed(Stride.Input.Keys.F2) then
        overlayState <- SettingsOverlay.toggle overlayState

    // Settings overlay input handling (when visible, consumes input)
    if SettingsOverlay.isVisible overlayState then
        let newOverlay, changedSettings = SettingsOverlay.handleInput game.Input overlayState
        overlayState <- newOverlay
        match changedSettings with
        | Some newSettings ->
            match displayState with
            | Some ds ->
                let newDs = DisplayManager.applySettings ds newSettings
                displayState <- Some newDs
                viewerSettings <- newSettings
                ViewerSettings.save viewerSettings
                logger.LogInformation("Settings changed: {Width}x{Height}, AA={AA}, Shadows={Sh}",
                    newSettings.ResolutionWidth, newSettings.ResolutionHeight,
                    newSettings.AntiAliasing, newSettings.ShadowQuality)
            | None -> ()
        | None -> ()

    // Camera (skip when overlay is visible to avoid camera movement)
    if not (SettingsOverlay.isVisible overlayState) then
        // Cancel active camera mode on mouse input
        if CameraController.isActive cameraState then
            if game.Input.IsMouseButtonDown(Stride.Input.MouseButton.Left) ||
               game.Input.IsMouseButtonDown(Stride.Input.MouseButton.Middle) ||
               abs game.Input.MouseWheelDelta > 0.001f then
                cameraState <- CameraController.cancelMode cameraState
        cameraState <- CameraController.applyInput game.Input dt cameraState
    match cameraEntity with
    | Some entity -> CameraController.applyToCamera cameraState entity
    | None -> ()

    // FPS tracking
    let fps = FpsCounter.update dt fpsState
    if FpsCounter.shouldLog 10.0f fpsState then
        if FpsCounter.isBelowThreshold fpsState then
            logger.LogWarning("FPS below threshold: {Fps:F1}", fps)
        else
            logger.LogInformation("FPS: {Fps:F1}", fps)

    // Demo label overlay
    let demoLabel =
        match SceneManager.demoName sceneState with
        | Some name ->
            let desc = SceneManager.demoDescription sceneState |> Option.defaultValue ""
            if desc.Length > 0 then $"{name} — {desc}" else name
        | None -> "Free Mode"
    game.DebugTextSystem.Print(demoLabel, Int2(10, 10))

    // Narration label overlay
    match SceneManager.narrationText sceneState with
    | Some text -> game.DebugTextSystem.Print(text, Int2(10, 50), Color.Yellow)
    | None -> ()

    // Status overlay
    let simTime = SceneManager.simulationTime sceneState
    let running = SceneManager.isRunning sceneState
    let statusText =
        let runLabel = if running then "RUNNING" else "PAUSED"
        $"FPS: {fps:F0} | Time: {simTime:F2}s | {runLabel}"
    game.DebugTextSystem.Print(statusText, Int2(10, 30))

    // Render settings overlay (if visible)
    SettingsOverlay.render game.DebugTextSystem overlayState

[<EntryPoint>]
let main _ =
    host.StartAsync() |> Async.AwaitTask |> Async.RunSynchronously
    logger.LogInformation("Viewer host started")

    game.Run(
        start = Action<Scene>(start),
        update = Action<Scene, GameTime>(update))

    logger.LogInformation("Viewer shutting down")
    cts.Cancel()
    host.StopAsync() |> Async.AwaitTask |> Async.RunSynchronously
    cts.Dispose()
    0
