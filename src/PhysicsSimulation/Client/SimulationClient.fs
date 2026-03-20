module PhysicsSimulation.SimulationClient

open System
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld
open Microsoft.Extensions.Logging

let private stepIntervalMs = int (1000.0f / 60.0f)

let run (serverAddress: string) (ct: CancellationToken) =
    async {
        let loggerFactory = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore)
        let logger = loggerFactory.CreateLogger("SimulationClient")

        let world = create ()
        try
            let channel = GrpcChannel.ForAddress(serverAddress)
            let client = SimulationLink.SimulationLinkClient(channel)

            logger.LogInformation("Connecting to server at {Address}", serverAddress)

            use call = client.ConnectSimulation(cancellationToken = ct)
            let requestStream = call.RequestStream
            let responseStream = call.ResponseStream

            logger.LogInformation("Connected to server")

            // Send initial state
            do! requestStream.WriteAsync(currentState world, ct) |> Async.AwaitTask

            let mutable running = true
            while running && not ct.IsCancellationRequested do
                try
                    // Check for commands (non-blocking with short timeout)
                    let commandTask = responseStream.MoveNext(ct)

                    let! hasCommandResult =
                        async {
                            try
                                let! completed = Task.WhenAny(commandTask, Task.Delay(TimeSpan.FromMilliseconds(1.0), ct)) |> Async.AwaitTask
                                return Object.ReferenceEquals(completed, commandTask) && commandTask.Result
                            with
                            | _ -> return false
                        }

                    if hasCommandResult then
                        let command = responseStream.Current
                        let _ack = CommandHandler.handle world command
                        logger.LogDebug("Command received: {Command}", command.CommandCase)

                        // If it was a step command while paused, send state
                        if command.CommandCase = SimulationCommand.CommandOneofCase.Step && not (isRunning world) then
                            let state = currentState world
                            do! requestStream.WriteAsync(state, ct) |> Async.AwaitTask

                    // If playing, run simulation loop
                    if isRunning world then
                        let state = step world
                        do! requestStream.WriteAsync(state, ct) |> Async.AwaitTask
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

            logger.LogInformation("Simulation client shutting down")
            try
                do! requestStream.CompleteAsync() |> Async.AwaitTask
            with _ -> ()
        finally
            destroy world
            loggerFactory.Dispose()
    }
