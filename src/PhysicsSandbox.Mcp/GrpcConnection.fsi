module PhysicsSandbox.Mcp.GrpcConnection

open System
open PhysicsSandbox.Shared.Contracts

[<Class>]
type GrpcConnection =
    new : serverAddress: string -> GrpcConnection
    member Client : PhysicsHub.PhysicsHubClient
    member LatestState : SimulationState option
    member LastUpdateTime : DateTimeOffset
    member StreamConnected : bool
    member ViewStreamConnected : bool
    member AuditStreamConnected : bool
    member ServerAddress : string
    member LatestViewCommand : ViewCommand option
    member CommandLog : CommandEvent list
    member SendBatchCommand : batch: BatchSimulationRequest -> System.Threading.Tasks.Task<BatchResponse>
    member SendBatchViewCommand : batch: BatchViewRequest -> System.Threading.Tasks.Task<BatchResponse>
    member IncrementSent : bytes: int64 -> unit
    member LocalMetrics : ServiceMetricsReport
    member OnStateReceived : (SimulationState -> unit) option with get, set
    member OnCommandReceived : (CommandEvent -> unit) option with get, set
    member Start : unit -> unit
    interface IDisposable
