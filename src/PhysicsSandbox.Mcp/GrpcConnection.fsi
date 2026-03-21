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
    member Start : unit -> unit
    interface IDisposable
