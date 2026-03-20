namespace PhysicsServer.Services

open System.Threading.Tasks
open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

type SimulationLinkService(router: MessageRouter) =
    inherit SimulationLink.SimulationLinkBase()

    override _.ConnectSimulation
        (requestStream: IAsyncStreamReader<SimulationState>,
         responseStream: IServerStreamWriter<SimulationCommand>,
         context: ServerCallContext)
        =
        task {
            if not (tryConnectSimulation router) then
                raise (RpcException(Status(StatusCode.AlreadyExists, "A simulation is already connected")))

            try
                // Task 1: Read incoming state from simulation and publish
                let readTask =
                    task {
                        try
                            while not context.CancellationToken.IsCancellationRequested
                                  && requestStream.MoveNext(context.CancellationToken).Result do
                                do! publishState router requestStream.Current
                        with
                        | :? RpcException -> ()
                        | _ -> ()
                    }

                // Task 2: Forward pending commands to simulation
                let writeTask =
                    task {
                        try
                            while not context.CancellationToken.IsCancellationRequested do
                                match! readCommand router context.CancellationToken with
                                | Some cmd -> do! responseStream.WriteAsync(cmd)
                                | None -> ()
                        with
                        | :? RpcException -> ()
                        | _ -> ()
                    }

                do! Task.WhenAny(readTask, writeTask) :> Task
            finally
                disconnectSimulation router
        }
