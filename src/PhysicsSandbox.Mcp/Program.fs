open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

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

// Register GrpcConnection as singleton
builder.Services.AddSingleton<GrpcConnection>(fun _ ->
    let conn = new GrpcConnection(serverAddress)
    conn.Start()
    conn) |> ignore

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly() |> ignore

let app = builder.Build()

app.MapMcp() |> ignore
app.MapDefaultEndpoints() |> ignore

app.Run()
