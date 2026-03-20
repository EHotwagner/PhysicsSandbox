module PhysicsSandbox.Mcp.GrpcConnection

open System
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts
open Microsoft.Extensions.Logging

let private createChannel (address: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    let options = GrpcChannelOptions(HttpHandler = handler)
    if address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) then
        options.HttpVersion <- System.Net.HttpVersion.Version20
        options.HttpVersionPolicy <- System.Net.Http.HttpVersionPolicy.RequestVersionExact
    GrpcChannel.ForAddress(address, options)

type GrpcConnection(serverAddress: string) =
    let channel = createChannel serverAddress
    let client = PhysicsHub.PhysicsHubClient(channel)
    let cts = new CancellationTokenSource()
    let mutable latestState: SimulationState option = None
    let mutable lastUpdateTime = DateTimeOffset.UtcNow
    let mutable streamConnected = false

    let startStateStream (logger: ILogger) =
        Task.Run(fun () ->
            task {
                let mutable delay = 1000
                while not cts.Token.IsCancellationRequested do
                    try
                        use call = client.StreamState(StateRequest(), cancellationToken = cts.Token)
                        let stream = call.ResponseStream
                        streamConnected <- true
                        delay <- 1000
                        logger.LogInformation("State stream connected to {Address}", serverAddress)
                        while not cts.Token.IsCancellationRequested do
                            let! hasNext = stream.MoveNext(cts.Token)
                            if hasNext then
                                latestState <- Some stream.Current
                                lastUpdateTime <- DateTimeOffset.UtcNow
                    with
                    | :? OperationCanceledException -> ()
                    | :? RpcException when cts.Token.IsCancellationRequested -> ()
                    | :? RpcException ->
                        streamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            logger.LogWarning("State stream disconnected, retrying in {Delay}ms", delay)
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
                    | _ ->
                        streamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
            } :> Task) |> ignore

    member _.Client = client
    member _.LatestState = latestState
    member _.LastUpdateTime = lastUpdateTime
    member _.StreamConnected = streamConnected
    member _.ServerAddress = serverAddress

    member this.Start() =
        let loggerFactory = LoggerFactory.Create(fun b -> b.AddConsole(fun opts -> opts.LogToStandardErrorThreshold <- LogLevel.Trace) |> ignore)
        let logger = loggerFactory.CreateLogger("GrpcConnection")
        startStateStream logger

    interface IDisposable with
        member _.Dispose() =
            try cts.Cancel() with _ -> ()
            try cts.Dispose() with _ -> ()
            try channel.Dispose() with _ -> ()
