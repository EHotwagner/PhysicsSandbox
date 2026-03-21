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

    override StreamViewCommands:
        request: StateRequest *
        responseStream: IServerStreamWriter<ViewCommand> *
        context: ServerCallContext ->
            System.Threading.Tasks.Task

    override SendBatchCommand:
        request: BatchSimulationRequest * context: ServerCallContext ->
            System.Threading.Tasks.Task<BatchResponse>

    override SendBatchViewCommand:
        request: BatchViewRequest * context: ServerCallContext ->
            System.Threading.Tasks.Task<BatchResponse>

    override GetMetrics:
        request: MetricsRequest * context: ServerCallContext ->
            System.Threading.Tasks.Task<MetricsResponse>
