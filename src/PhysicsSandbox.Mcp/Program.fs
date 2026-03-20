open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

[<EntryPoint>]
let main args =
    let serverAddress =
        if args.Length > 0 then args.[0]
        else "https://localhost:7180"

    let builder = Host.CreateApplicationBuilder(args)

    builder.Logging.AddConsole(fun opts ->
        opts.LogToStandardErrorThreshold <- LogLevel.Trace) |> ignore

    // Register GrpcConnection as singleton
    builder.Services.AddSingleton<GrpcConnection>(fun _ ->
        let conn = new GrpcConnection(serverAddress)
        conn.Start()
        conn) |> ignore

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly() |> ignore

    builder.Build().RunAsync().GetAwaiter().GetResult()
    0
