open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open PhysicsServer.Hub
open PhysicsServer.Services

let builder = WebApplication.CreateBuilder()
builder.AddServiceDefaults() |> ignore

// Register domain services as singletons
let router = MessageRouter.create ()
builder.Services.AddSingleton<MessageRouter.MessageRouter>(router) |> ignore

// Register gRPC
builder.Services.AddGrpc() |> ignore

let app = builder.Build()

app.MapGrpcService<PhysicsHubService>() |> ignore
app.MapGrpcService<SimulationLinkService>() |> ignore
app.MapDefaultEndpoints() |> ignore

app.Run()
