namespace PhysicsServer.Services

open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

[<RequireQualifiedAccess>]
module SimulationLinkDiagnostics =
    val getLastTransferMs : unit -> double

/// gRPC service implementation for the simulation-facing SimulationLink.
type SimulationLinkService =
    inherit SimulationLink.SimulationLinkBase

    new: router: MessageRouter -> SimulationLinkService

    override ConnectSimulation:
        requestStream: IAsyncStreamReader<SimulationState> *
        responseStream: IServerStreamWriter<SimulationCommand> *
        context: ServerCallContext ->
            System.Threading.Tasks.Task
