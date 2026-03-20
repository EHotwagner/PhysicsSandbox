module PhysicsViewer.Program

open System
open System.Collections.Concurrent
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
open PhysicsViewer.ViewerClient

let game = new Game()

let mutable sceneState = SceneManager.create ()
let mutable cameraState = CameraController.defaultCamera ()
let mutable cameraEntity: Entity option = None

let stateQueue = ConcurrentQueue<SimulationState>()
let viewCmdQueue = ConcurrentQueue<ViewCommand>()

let cts = new CancellationTokenSource()

let mutable serverAddress = ""

// ServiceDefaults host for observability + service discovery (FR-013, constitution VII)
let hostBuilder = Host.CreateApplicationBuilder()
do hostBuilder.AddServiceDefaults() |> ignore
let host = hostBuilder.Build()
let logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PhysicsViewer")
let lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>()
do lifetime.ApplicationStopping.Register(fun () -> cts.Cancel()) |> ignore

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

    // Apply default camera position
    CameraController.applyToCamera cameraState camEntity

    // Resolve server address from Aspire service discovery
    serverAddress <-
        match Environment.GetEnvironmentVariable("services__server__https__0") with
        | null | "" ->
            match Environment.GetEnvironmentVariable("services__server__http__0") with
            | null | "" -> "http://localhost:5000"
            | addr -> addr
        | addr -> addr

    logger.LogInformation("Viewer starting, server address: {Address}", serverAddress)

    // Start gRPC state streaming on background task
    Task.Run(fun () ->
        task {
            try
                do! ViewerClient.streamState serverAddress stateQueue cts.Token
            with
            | :? OperationCanceledException -> ()
            | ex -> logger.LogError(ex, "State stream error")
        } :> Task) |> ignore

    // Start gRPC view command streaming on background task
    Task.Run(fun () ->
        task {
            try
                do! ViewerClient.streamViewCommands serverAddress viewCmdQueue cts.Token
            with
            | :? OperationCanceledException -> ()
            | ex -> logger.LogError(ex, "View command stream error")
        } :> Task) |> ignore

let update (scene: Scene) (time: GameTime) =
    let dt = float32 time.Elapsed.TotalSeconds

    // Drain state queue — apply latest state only
    let mutable latestState = Unchecked.defaultof<SimulationState>
    let mutable hasState = false

    while stateQueue.TryDequeue(&latestState) do
        hasState <- true

    if hasState then
        sceneState <- SceneManager.applyState game scene sceneState latestState

    // Apply interactive mouse/keyboard input first
    cameraState <- CameraController.applyInput game.Input dt cameraState

    // Drain view command queue — REPL commands override interactive input (FR-015)
    let mutable viewCmd = Unchecked.defaultof<ViewCommand>

    while viewCmdQueue.TryDequeue(&viewCmd) do
        match viewCmd.CommandCase with
        | ViewCommand.CommandOneofCase.SetCamera ->
            cameraState <- CameraController.applySetCamera viewCmd.SetCamera cameraState
        | ViewCommand.CommandOneofCase.SetZoom ->
            cameraState <- CameraController.applySetZoom viewCmd.SetZoom cameraState
        | ViewCommand.CommandOneofCase.ToggleWireframe ->
            sceneState <- SceneManager.applyWireframe game viewCmd.ToggleWireframe sceneState
        | _ -> ()

    // Sync camera entity transform
    match cameraEntity with
    | Some entity -> CameraController.applyToCamera cameraState entity
    | None -> ()

    // Status overlay — display simulation time and running/paused
    let simTime = SceneManager.simulationTime sceneState
    let running = SceneManager.isRunning sceneState
    let statusText =
        let runLabel = if running then "RUNNING" else "PAUSED"
        $"Time: {simTime:F2}s | {runLabel}"
    game.DebugTextSystem.Print(statusText, Int2(10, 10))

[<EntryPoint>]
let main _ =
    // Start ServiceDefaults host on background thread
    host.StartAsync() |> Async.AwaitTask |> Async.RunSynchronously
    logger.LogInformation("Viewer host started")

    // Run Stride game loop on main thread (blocks until window closes)
    game.Run(
        start = Action<Scene>(start),
        update = Action<Scene, GameTime>(update))

    logger.LogInformation("Viewer shutting down")
    cts.Cancel()
    host.StopAsync() |> Async.AwaitTask |> Async.RunSynchronously
    cts.Dispose()
    0
