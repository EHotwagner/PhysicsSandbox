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
    member ServerAddress : string
    member Start : unit -> unit
    interface IDisposable
