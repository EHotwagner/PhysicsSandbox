/// <summary>Manages the persistent gRPC connection to the PhysicsServer, including background streaming of simulation state, view commands, and command audit events.</summary>
module PhysicsSandbox.Mcp.GrpcConnection

open System
open System.Collections.Generic
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

/// <summary>Maintains a gRPC channel to the PhysicsServer and runs background streams for simulation state, view commands, and command audit events. Reconnects automatically with exponential backoff on stream failures.</summary>
/// <param name="serverAddress">The server URL (http:// or https://) to connect to.</param>
type GrpcConnection(serverAddress: string) =
    let channel = createChannel serverAddress
    let client = PhysicsHub.PhysicsHubClient(channel)
    let cts = new CancellationTokenSource()
    let mutable latestState: SimulationState option = None
    let mutable lastUpdateTime = DateTimeOffset.UtcNow
    let mutable streamConnected = false
    let mutable viewStreamConnected = false
    let mutable auditStreamConnected = false
    let mutable latestViewCommand: ViewCommand option = None
    let commandLog = LinkedList<CommandEvent>()
    let commandLogLock = obj ()
    let commandLogMax = 100
    let mutable mcpMsgSent = 0L
    let mutable mcpMsgRecv = 0L
    let mutable mcpBytesSent = 0L
    let mutable mcpBytesRecv = 0L

    let addToCommandLog (evt: CommandEvent) =
        lock commandLogLock (fun () ->
            commandLog.AddLast(evt) |> ignore
            while commandLog.Count > commandLogMax do
                commandLog.RemoveFirst())

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
                                Threading.Interlocked.Increment(&mcpMsgRecv) |> ignore
                                Threading.Interlocked.Add(&mcpBytesRecv, int64 (stream.Current.CalculateSize())) |> ignore
                    with
                    | :? OperationCanceledException -> ()
                    | :? RpcException when cts.Token.IsCancellationRequested -> ()
                    | :? RpcException as ex ->
                        streamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            logger.LogWarning(ex, "State stream disconnected (RpcException), retrying in {Delay}ms", delay)
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
                    | ex ->
                        streamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            logger.LogWarning(ex, "State stream disconnected (unexpected), retrying in {Delay}ms", delay)
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
            } :> Task) |> ignore

    let startViewCommandStream (logger: ILogger) =
        Task.Run(fun () ->
            task {
                let mutable delay = 1000
                while not cts.Token.IsCancellationRequested do
                    try
                        use call = client.StreamViewCommands(StateRequest(), cancellationToken = cts.Token)
                        let stream = call.ResponseStream
                        viewStreamConnected <- true
                        delay <- 1000
                        logger.LogInformation("View command stream connected to {Address}", serverAddress)
                        while not cts.Token.IsCancellationRequested do
                            let! hasNext = stream.MoveNext(cts.Token)
                            if hasNext then
                                latestViewCommand <- Some stream.Current
                    with
                    | :? OperationCanceledException -> ()
                    | :? RpcException when cts.Token.IsCancellationRequested -> ()
                    | :? RpcException ->
                        viewStreamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            logger.LogWarning("View command stream disconnected, retrying in {Delay}ms", delay)
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
                    | _ ->
                        viewStreamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
            } :> Task) |> ignore

    let startCommandAuditStream (logger: ILogger) =
        Task.Run(fun () ->
            task {
                let mutable delay = 1000
                while not cts.Token.IsCancellationRequested do
                    try
                        use call = client.StreamCommands(StateRequest(), cancellationToken = cts.Token)
                        let stream = call.ResponseStream
                        auditStreamConnected <- true
                        delay <- 1000
                        logger.LogInformation("Command audit stream connected to {Address}", serverAddress)
                        while not cts.Token.IsCancellationRequested do
                            let! hasNext = stream.MoveNext(cts.Token)
                            if hasNext then
                                addToCommandLog stream.Current
                                Threading.Interlocked.Increment(&mcpMsgRecv) |> ignore
                                Threading.Interlocked.Add(&mcpBytesRecv, int64 (stream.Current.CalculateSize())) |> ignore
                    with
                    | :? OperationCanceledException -> ()
                    | :? RpcException when cts.Token.IsCancellationRequested -> ()
                    | :? RpcException ->
                        auditStreamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            logger.LogWarning("Command audit stream disconnected, retrying in {Delay}ms", delay)
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
                    | _ ->
                        auditStreamConnected <- false
                        if not cts.Token.IsCancellationRequested then
                            do! Task.Delay(delay, cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                            delay <- min (delay * 2) 10000
            } :> Task) |> ignore

    /// <summary>The underlying PhysicsHub gRPC client for sending commands and queries.</summary>
    member _.Client = client
    /// <summary>The most recently received simulation state from the background stream, or None if no state has arrived yet.</summary>
    member _.LatestState = latestState
    /// <summary>Timestamp of the last simulation state update received from the server.</summary>
    member _.LastUpdateTime = lastUpdateTime
    /// <summary>Whether the simulation state stream is currently connected to the server.</summary>
    member _.StreamConnected = streamConnected
    /// <summary>Whether the view command stream is currently connected to the server.</summary>
    member _.ViewStreamConnected = viewStreamConnected
    /// <summary>Whether the command audit stream is currently connected to the server.</summary>
    member _.AuditStreamConnected = auditStreamConnected
    /// <summary>The server address this connection was created with.</summary>
    member _.ServerAddress = serverAddress
    /// <summary>The most recently received view command from the background stream, or None if none has arrived.</summary>
    member _.LatestViewCommand = latestViewCommand

    /// <summary>Returns a snapshot of the most recent command events (up to 100) received from the audit stream.</summary>
    member _.CommandLog =
        lock commandLogLock (fun () ->
            commandLog |> Seq.toList)

    /// <summary>Sends a batch of simulation commands to the server in a single gRPC call and tracks message/byte counts.</summary>
    /// <param name="batch">The batch request containing multiple simulation commands.</param>
    /// <returns>The server's batch response with per-command results.</returns>
    member _.SendBatchCommand(batch: BatchSimulationRequest) =
        task {
            let! response = client.SendBatchCommandAsync(batch)
            Threading.Interlocked.Increment(&mcpMsgSent) |> ignore
            Threading.Interlocked.Add(&mcpBytesSent, int64 (batch.CalculateSize())) |> ignore
            return response
        }

    /// <summary>Sends a batch of view commands to the server in a single gRPC call and tracks message/byte counts.</summary>
    /// <param name="batch">The batch request containing multiple view commands.</param>
    /// <returns>The server's batch response with per-command results.</returns>
    member _.SendBatchViewCommand(batch: BatchViewRequest) =
        task {
            let! response = client.SendBatchViewCommandAsync(batch)
            Threading.Interlocked.Increment(&mcpMsgSent) |> ignore
            Threading.Interlocked.Add(&mcpBytesSent, int64 (batch.CalculateSize())) |> ignore
            return response
        }

    /// <summary>Increments the local sent-message counter and adds to the sent-bytes total. Used by tools that send commands outside of SendBatchCommand.</summary>
    /// <param name="bytes">Number of bytes sent in this message.</param>
    member _.IncrementSent(bytes: int64) =
        Threading.Interlocked.Increment(&mcpMsgSent) |> ignore
        Threading.Interlocked.Add(&mcpBytesSent, bytes) |> ignore

    /// <summary>Returns a ServiceMetricsReport for the MCP server's own message and byte counters.</summary>
    member _.LocalMetrics =
        let report = ServiceMetricsReport()
        report.ServiceName <- "McpServer"
        report.MessagesSent <- Threading.Interlocked.Read(&mcpMsgSent)
        report.MessagesReceived <- Threading.Interlocked.Read(&mcpMsgRecv)
        report.BytesSent <- Threading.Interlocked.Read(&mcpBytesSent)
        report.BytesReceived <- Threading.Interlocked.Read(&mcpBytesRecv)
        report

    /// <summary>Starts background streams for simulation state, view commands, and command audit events. Each stream reconnects automatically on failure.</summary>
    member this.Start() =
        let loggerFactory = LoggerFactory.Create(fun b -> b.AddConsole(fun opts -> opts.LogToStandardErrorThreshold <- LogLevel.Trace) |> ignore)
        let logger = loggerFactory.CreateLogger("GrpcConnection")
        startStateStream logger
        startViewCommandStream logger
        startCommandAuditStream logger

    interface IDisposable with
        member _.Dispose() =
            try cts.Cancel() with _ -> ()
            try cts.Dispose() with _ -> ()
            try channel.Dispose() with _ -> ()
