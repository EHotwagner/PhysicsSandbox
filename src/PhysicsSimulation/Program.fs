open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

let builder = Host.CreateApplicationBuilder()
builder.AddServiceDefaults() |> ignore

let host = builder.Build()

let logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PhysicsSimulation")
let lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>()

let serverAddress =
    let envUrl = Environment.GetEnvironmentVariable("services__server__https__0")
    let envUrlHttp = Environment.GetEnvironmentVariable("services__server__http__0")
    match envUrl, envUrlHttp with
    | url, _ when not (String.IsNullOrEmpty url) -> url
    | _, url when not (String.IsNullOrEmpty url) -> url
    | _ -> "https+http://server"

logger.LogInformation("PhysicsSimulation starting, server address: {Address}", serverAddress)

let cts = new CancellationTokenSource()
lifetime.ApplicationStopping.Register(fun () -> cts.Cancel()) |> ignore

let task =
    async {
        try
            do! PhysicsSimulation.SimulationClient.run serverAddress cts.Token
        with ex ->
            logger.LogError(ex, "Simulation client failed")
    }
    |> Async.StartAsTask

host.Run()
cts.Cancel()
task.Wait()
