namespace PhysicsServer.Services

open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

/// gRPC service implementation for the client/viewer-facing PhysicsHub.
type PhysicsHubService =
    inherit PhysicsHub.PhysicsHubBase

    new: router: MessageRouter -> PhysicsHubService

    override SendCommand:
        request: SimulationCommand * context: ServerCallContext -> System.Threading.Tasks.Task<CommandAck>

    override SendViewCommand:
        request: ViewCommand * context: ServerCallContext -> System.Threading.Tasks.Task<CommandAck>

    override StreamState:
        request: StateRequest *
        responseStream: IServerStreamWriter<SimulationState> *
        context: ServerCallContext ->
            System.Threading.Tasks.Task
