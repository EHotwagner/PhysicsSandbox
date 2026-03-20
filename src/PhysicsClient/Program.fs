open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

let builder = Host.CreateApplicationBuilder()
builder.AddServiceDefaults() |> ignore
let host = builder.Build()
let logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PhysicsClient")
let lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>()

let serverAddress =
    match Environment.GetEnvironmentVariable("services__server__https__0") with
    | null | "" ->
        match Environment.GetEnvironmentVariable("services__server__http__0") with
        | null | "" -> "http://localhost:5000"
        | addr -> addr
    | addr -> addr

logger.LogInformation("PhysicsClient starting, server address: {Address}", serverAddress)
// Keep alive for Aspire orchestration
host.Run()
