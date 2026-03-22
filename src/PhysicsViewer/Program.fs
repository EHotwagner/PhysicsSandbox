module PhysicsViewer.Program

open System
open System.Threading
open System.Threading.Tasks
open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Bepu
open Stride.CommunityToolkit.Skyboxes
open Stride.CommunityToolkit.Rendering.Compositing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.SceneManager
open PhysicsViewer.CameraController
open PhysicsViewer.Rendering

let game = new Game()

let fpsState = FpsCounter.create 30.0f
let mutable sceneState = SceneManager.create ()
let mutable debugState = DebugRenderer.create ()
let mutable cameraState = CameraController.defaultCamera ()
let mutable cameraEntity: Entity option = None

// Shared state: written by background stream, read by game update loop
let mutable latestSimState: SimulationState = null
let mutable latestViewCmd: ViewCommand = null
let mutable stateVersion = 0
let mutable lastAppliedVersion = 0

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

let startStateStream (serverAddress: string) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not cts.Token.IsCancellationRequested do
                try
                    use channel = createGrpcChannel serverAddress
                    let client = PhysicsHub.PhysicsHubClient(channel)
                    use call = client.StreamState(StateRequest(), cancellationToken = cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000
                    logger.LogInformation("State stream connected to {Address}", serverAddress)
                    while not cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(cts.Token)
                        if hasNext then
                            Volatile.Write(&latestSimState, stream.Current)
                            Interlocked.Increment(&stateVersion) |> ignore
                            Interlocked.Increment(&viewerMsgRecv) |> ignore
                            Interlocked.Add(&viewerBytesRecv, int64 (stream.Current.CalculateSize())) |> ignore
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
                            Volatile.Write(&latestViewCmd, stream.Current)
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
    game.Add3DGround() |> ignore
    game.AddSkybox() |> ignore

    GameExtensions.AddGroundGizmo(
        game,
        System.Nullable<Vector3>(Vector3(-5f, 0.1f, -5f)),
        showAxisName = true)

    CameraController.applyToCamera cameraState camEntity

    let addr = resolveServerAddress ()
    logger.LogInformation("Viewer starting, server address: {Address}", addr)
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
            sceneState <- SceneManager.applyState game scene sceneState simState
            debugState <- DebugRenderer.updateShapes game scene debugState simState
            debugState <- DebugRenderer.updateConstraints game scene debugState simState
            lastAppliedVersion <- currentVersion

    // Check for view commands
    let viewCmd = Interlocked.Exchange(&latestViewCmd, null)
    if not (isNull viewCmd) then
        match viewCmd.CommandCase with
        | ViewCommand.CommandOneofCase.SetCamera ->
            cameraState <- CameraController.applySetCamera viewCmd.SetCamera cameraState
        | ViewCommand.CommandOneofCase.SetZoom ->
            cameraState <- CameraController.applySetZoom viewCmd.SetZoom cameraState
        | ViewCommand.CommandOneofCase.ToggleWireframe ->
            sceneState <- SceneManager.applyWireframe game viewCmd.ToggleWireframe sceneState
        | _ -> ()

    // Toggle debug visualization with F3
    if game.Input.IsKeyPressed(Stride.Input.Keys.F3) then
        let newEnabled = not (DebugRenderer.isEnabled debugState)
        debugState <- DebugRenderer.setEnabled newEnabled debugState
        logger.LogInformation("Debug visualization {State}", if newEnabled then "enabled" else "disabled")

    // Camera
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

    // Status overlay
    let simTime = SceneManager.simulationTime sceneState
    let running = SceneManager.isRunning sceneState
    let statusText =
        let runLabel = if running then "RUNNING" else "PAUSED"
        $"FPS: {fps:F0} | Time: {simTime:F2}s | {runLabel}"
    game.DebugTextSystem.Print(statusText, Int2(10, 10))

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
