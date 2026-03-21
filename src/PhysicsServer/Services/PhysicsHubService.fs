namespace PhysicsServer.Services

open System.Threading.Tasks
open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

type PhysicsHubService(router: MessageRouter) =
    inherit PhysicsHub.PhysicsHubBase()

    override _.SendCommand(request: SimulationCommand, context: ServerCallContext) =
        let ack = submitCommand router request
        Task.FromResult(ack)

    override _.SendViewCommand(request: ViewCommand, context: ServerCallContext) =
        let ack = submitViewCommand router request
        Task.FromResult(ack)

    override _.StreamState
        (request: StateRequest, responseStream: IServerStreamWriter<SimulationState>, context: ServerCallContext)
        =
        task {
            // Send cached state immediately for late joiners
            match getLatestState router with
            | Some state -> do! responseStream.WriteAsync(state)
            | None -> ()

            // Subscribe and stream live updates
            let tcs = TaskCompletionSource()

            let subId =
                subscribe router (fun state ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            do! responseStream.WriteAsync(state)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribe router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }

    override _.StreamViewCommands
        (request: StateRequest, responseStream: IServerStreamWriter<ViewCommand>, context: ServerCallContext)
        =
        task {
            while not context.CancellationToken.IsCancellationRequested do
                match! readViewCommand router context.CancellationToken with
                | Some cmd -> do! responseStream.WriteAsync(cmd)
                | None -> ()
        }

    override _.StreamCommands
        (request: StateRequest, responseStream: IServerStreamWriter<CommandEvent>, context: ServerCallContext)
        =
        task {
            let tcs = TaskCompletionSource()

            let subId =
                subscribeCommands router (fun evt ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            do! responseStream.WriteAsync(evt)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribeCommands router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }
