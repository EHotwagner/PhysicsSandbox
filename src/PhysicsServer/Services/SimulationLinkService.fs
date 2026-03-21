namespace PhysicsServer.Services

open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

[<RequireQualifiedAccess>]
module SimulationLinkDiagnostics =
    let mutable internal transferMs = 0.0
    let getLastTransferMs () = transferMs

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
                            let sw = Stopwatch()
                            let mutable keepReading = true
                            while not context.CancellationToken.IsCancellationRequested && keepReading do
                                sw.Restart()
                                let! hasNext = requestStream.MoveNext(context.CancellationToken)
                                if hasNext then
                                    sw.Stop()
                                    SimulationLinkDiagnostics.transferMs <- sw.Elapsed.TotalMilliseconds
                                    do! publishState router requestStream.Current
                                else
                                    keepReading <- false
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
