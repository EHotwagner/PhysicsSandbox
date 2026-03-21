namespace PhysicsServer.Services

open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

/// <summary>Diagnostics for measuring gRPC transfer latency on the simulation link.</summary>
[<RequireQualifiedAccess>]
module SimulationLinkDiagnostics =
    let mutable internal transferMs = 0.0

    /// <summary>Get the most recently measured gRPC transfer time in milliseconds for state streaming.</summary>
    /// <returns>The last observed transfer duration in milliseconds.</returns>
    let getLastTransferMs () = transferMs

/// <summary>
/// gRPC service implementation for the simulation-facing SimulationLink.
/// Manages the bidirectional stream between the server and the physics simulation:
/// incoming state updates are published to subscribers, outgoing commands are forwarded from the channel.
/// Only one simulation may be connected at a time.
/// </summary>
type SimulationLinkService(router: MessageRouter) =
    inherit SimulationLink.SimulationLinkBase()

    /// <summary>
    /// Handle the bidirectional simulation connection. Reads state updates from the simulation
    /// and publishes them to subscribers, while concurrently forwarding queued commands back
    /// to the simulation. Rejects the call if another simulation is already connected.
    /// </summary>
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
