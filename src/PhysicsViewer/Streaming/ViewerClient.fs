module PhysicsViewer.ViewerClient

open System
open System.Collections.Concurrent
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts

let private createChannel (serverAddress: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    GrpcChannel.ForAddress(
        serverAddress,
        GrpcChannelOptions(HttpHandler = handler))

let private runWithReconnect (action: GrpcChannel -> CancellationToken -> Task<unit>) (serverAddress: string) (ct: CancellationToken) : Task<unit> =
    task {
        let mutable delay = 1000 // ms
        while not ct.IsCancellationRequested do
            try
                use channel = createChannel serverAddress
                do! action channel ct
            with
            | :? OperationCanceledException -> ()
            | :? RpcException when not ct.IsCancellationRequested ->
                eprintfn $"Connection lost to {serverAddress}, reconnecting in {delay}ms..."
                do! Task.Delay(delay, ct) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                delay <- min (delay * 2) 30000
            | ex when not ct.IsCancellationRequested ->
                eprintfn $"Stream error: {ex.Message}, reconnecting in {delay}ms..."
                do! Task.Delay(delay, ct) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                delay <- min (delay * 2) 30000
    }

/// <summary>
/// Opens a gRPC server-streaming call to receive simulation state snapshots, enqueuing each
/// received snapshot into the provided queue. Automatically reconnects with exponential backoff on failure.
/// </summary>
/// <param name="serverAddress">The gRPC server address (e.g., "https://localhost:7180").</param>
/// <param name="stateQueue">The concurrent queue where received SimulationState messages are enqueued.</param>
/// <param name="ct">Cancellation token to stop the streaming loop.</param>
/// <returns>A task that runs until cancellation, continuously streaming state updates.</returns>
let streamState (serverAddress: string) (stateQueue: ConcurrentQueue<SimulationState>) (ct: CancellationToken) : Task<unit> =
    runWithReconnect (fun channel ct ->
        task {
            let client = PhysicsHub.PhysicsHubClient(channel)
            use call = client.StreamState(StateRequest(), cancellationToken = ct)
            let stream = call.ResponseStream

            while not ct.IsCancellationRequested do
                let! hasNext = stream.MoveNext(ct)
                if hasNext then
                    stateQueue.Enqueue(stream.Current)
        }) serverAddress ct

/// <summary>
/// Opens a gRPC server-streaming call to receive view commands (camera, zoom, wireframe), enqueuing each
/// received command into the provided queue. Automatically reconnects with exponential backoff on failure.
/// </summary>
/// <param name="serverAddress">The gRPC server address (e.g., "https://localhost:7180").</param>
/// <param name="commandQueue">The concurrent queue where received ViewCommand messages are enqueued.</param>
/// <param name="ct">Cancellation token to stop the streaming loop.</param>
/// <returns>A task that runs until cancellation, continuously streaming view commands.</returns>
let streamViewCommands (serverAddress: string) (commandQueue: ConcurrentQueue<ViewCommand>) (ct: CancellationToken) : Task<unit> =
    runWithReconnect (fun channel ct ->
        task {
            let client = PhysicsHub.PhysicsHubClient(channel)
            use call = client.StreamViewCommands(StateRequest(), cancellationToken = ct)
            let stream = call.ResponseStream

            while not ct.IsCancellationRequested do
                let! hasNext = stream.MoveNext(ct)
                if hasNext then
                    commandQueue.Enqueue(stream.Current)
        }) serverAddress ct
