open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Mcp.Recording.RecordingEngine

let builder = WebApplication.CreateBuilder()
builder.AddServiceDefaults() |> ignore

// Resolve server address from environment or defaults
let serverAddress =
    let envHttp = Environment.GetEnvironmentVariable("services__server__http__0")
    let envHttps = Environment.GetEnvironmentVariable("services__server__https__0")
    match envHttp, envHttps with
    | url, _ when not (String.IsNullOrEmpty url) -> url
    | _, url when not (String.IsNullOrEmpty url) -> url
    | _ -> "http://localhost:5180"

// Create recording engine (auto-starts on first state received)
let engine: RecordingEngine = create ()
let onState = fun (s: PhysicsSandbox.Shared.Contracts.SimulationState) -> engine.OnStateReceived(s)
let onCommand = fun (e: PhysicsSandbox.Shared.Contracts.CommandEvent) -> engine.OnCommandReceived(e)

// MeshResolver state — initialized when GrpcConnection starts
let mutable mcpMeshResolver: PhysicsSandbox.Mcp.MeshResolver.MeshResolverState option = None

// Register GrpcConnection as singleton with recording + mesh resolver callbacks
builder.Services.AddSingleton<GrpcConnection>(fun (_: IServiceProvider) ->
    let conn = new GrpcConnection(serverAddress)
    let resolver = PhysicsSandbox.Mcp.MeshResolver.create conn.Client
    mcpMeshResolver <- Some resolver
    conn.OnStateReceived <- Some (fun s ->
        // Process new meshes from state update
        if s.NewMeshes.Count > 0 then
            PhysicsSandbox.Mcp.MeshResolver.processNewMeshes s.NewMeshes resolver
        onState s)
    conn.OnCommandReceived <- Some onCommand
    conn.Start()
    conn) |> ignore

// Register RecordingEngine as singleton for tool injection
builder.Services.AddSingleton<RecordingEngine>(fun (_: IServiceProvider) -> engine) |> ignore

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly() |> ignore

let app = builder.Build()

// Eagerly resolve GrpcConnection to start streams and recording
app.Services.GetRequiredService<GrpcConnection>() |> ignore

app.MapMcp() |> ignore
app.MapDefaultEndpoints() |> ignore

app.Run()
