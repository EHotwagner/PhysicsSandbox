module PhysicsSimulation.SimulationClient

open System
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld
open Microsoft.Extensions.Logging

let private stepIntervalMs = int (1000.0f / 60.0f)

// Metrics counters (thread-safe)
let mutable private msgSent = 0L
let mutable private msgRecv = 0L
let mutable private bytesSent = 0L
let mutable private bytesRecv = 0L

let private createChannel (address: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    let options = GrpcChannelOptions(HttpHandler = handler)
    if address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) then
        options.HttpVersion <- System.Net.HttpVersion.Version20
        options.HttpVersionPolicy <- System.Net.Http.HttpVersionPolicy.RequestVersionExact
    GrpcChannel.ForAddress(address, options)

let private runSession (logger: ILogger) (world: World) (client: SimulationLink.SimulationLinkClient) (ct: CancellationToken) =
    async {
        logger.LogInformation("Connecting to server")

        use call = client.ConnectSimulation(cancellationToken = ct)
        let requestStream = call.RequestStream
        let responseStream = call.ResponseStream

        logger.LogInformation("Connected to server")

        // Send initial state
        let initState = currentState world
        do! requestStream.WriteAsync(initState, ct) |> Async.AwaitTask
        Interlocked.Increment(&msgSent) |> ignore
        Interlocked.Add(&bytesSent, int64 (initState.CalculateSize())) |> ignore

        let mutable running = true
        let mutable pendingCommand: Task<bool> option = None
        while running && not ct.IsCancellationRequested do
            try
                // Reuse pending MoveNext task or start a new one
                let commandTask =
                    match pendingCommand with
                    | Some t -> t
                    | None -> responseStream.MoveNext(ct)

                let! hasCommandResult =
                    async {
                        try
                            let! completed = Task.WhenAny(commandTask, Task.Delay(TimeSpan.FromMilliseconds(1.0), ct)) |> Async.AwaitTask
                            if Object.ReferenceEquals(completed, commandTask) then
                                pendingCommand <- None
                                return commandTask.Result
                            else
                                pendingCommand <- Some commandTask
                                return false
                        with
                        | _ ->
                            pendingCommand <- None
                            return false
                    }

                if hasCommandResult then
                    let command = responseStream.Current
                    Interlocked.Increment(&msgRecv) |> ignore
                    Interlocked.Add(&bytesRecv, int64 (command.CalculateSize())) |> ignore
                    let _ack = CommandHandler.handle world command
                    logger.LogDebug("Command received: {Command}", command.CommandCase)

                    // Send state after any command while paused (so state reflects AddBody, RemoveBody, etc.)
                    if not (isRunning world) then
                        let state = currentState world
                        do! requestStream.WriteAsync(state, ct) |> Async.AwaitTask
                        Interlocked.Increment(&msgSent) |> ignore
                        Interlocked.Add(&bytesSent, int64 (state.CalculateSize())) |> ignore

                // If playing, run simulation loop
                if isRunning world then
                    let state = step world
                    do! requestStream.WriteAsync(state, ct) |> Async.AwaitTask
                    Interlocked.Increment(&msgSent) |> ignore
                    Interlocked.Add(&bytesSent, int64 (state.CalculateSize())) |> ignore
                    logger.LogTrace("Step completed, {BodyCount} bodies", state.Bodies.Count)
                    do! Async.Sleep stepIntervalMs
                elif not hasCommandResult then
                    // Paused and no command — wait a bit
                    do! Async.Sleep 16
            with
            | :? RpcException as ex ->
                logger.LogWarning("Server disconnected: {Status}", ex.StatusCode)
                running <- false
            | :? OperationCanceledException ->
                running <- false
            | ex ->
                logger.LogError(ex, "Unexpected error in simulation loop")
                running <- false

        try
            do! requestStream.CompleteAsync() |> Async.AwaitTask
        with _ -> ()
    }

let run (serverAddress: string) (ct: CancellationToken) =
    async {
        let loggerFactory = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore)
        let logger = loggerFactory.CreateLogger("SimulationClient")

        let world = create ()
        let metricsTimer =
            new System.Threading.Timer(
                (fun _ ->
                    logger.LogInformation(
                        "Metrics [PhysicsSimulation] sent={MsgSent} recv={MsgRecv} bytesSent={BytesSent} bytesRecv={BytesRecv} tickMs={TickMs:F2} serializeMs={SerializeMs:F2}",
                        Interlocked.Read(&msgSent),
                        Interlocked.Read(&msgRecv),
                        Interlocked.Read(&bytesSent),
                        Interlocked.Read(&bytesRecv),
                        SimulationWorld.latestTickMs(),
                        SimulationWorld.latestSerializeMs())),
                null,
                TimeSpan.FromSeconds(10.0),
                TimeSpan.FromSeconds(10.0))
        try
            let channel = createChannel serverAddress
            let client = SimulationLink.SimulationLinkClient(channel)

            logger.LogInformation("Simulation client starting, server at {Address}", serverAddress)

            // Reconnection loop with exponential backoff
            let mutable delay = 1000
            while not ct.IsCancellationRequested do
                try
                    do! runSession logger world client ct
                    // If runSession returned normally (not cancelled), reconnect
                    if not ct.IsCancellationRequested then
                        logger.LogWarning("Session ended, reconnecting in {Delay}ms", delay)
                        do! Async.Sleep delay
                        delay <- min (delay * 2) 10000
                with
                | :? OperationCanceledException -> ()
                | :? RpcException as ex ->
                    if not ct.IsCancellationRequested then
                        logger.LogWarning("Connection failed ({Status}), retrying in {Delay}ms", ex.StatusCode, delay)
                        do! Async.Sleep delay
                        delay <- min (delay * 2) 10000
                | ex ->
                    if not ct.IsCancellationRequested then
                        logger.LogError(ex, "Unexpected error, retrying in {Delay}ms", delay)
                        do! Async.Sleep delay
                        delay <- min (delay * 2) 10000

                // Reset backoff on successful connection
                if delay > 1000 then
                    delay <- 1000

            logger.LogInformation("Simulation client shutting down")
        finally
            metricsTimer.Dispose()
            destroy world
            loggerFactory.Dispose()
    }
