namespace PhysicsServer.Hub

open System
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts

[<RequireQualifiedAccess>]
module MetricsCounter =

    type MetricsState

    val create : serviceName: string -> MetricsState
    val incrementSent : count: int -> bytes: int64 -> state: MetricsState -> unit
    val incrementReceived : count: int -> bytes: int64 -> state: MetricsState -> unit
    val incrementMeshesCached : count: int -> state: MetricsState -> unit
    val incrementFetchRequest : hits: int -> misses: int -> state: MetricsState -> unit
    val incrementTickSent : count: int -> bytes: int64 -> state: MetricsState -> unit
    val incrementPropertySent : count: int -> bytes: int64 -> state: MetricsState -> unit
    val snapshot : state: MetricsState -> ServiceMetricsReport
    val startPeriodicLogging : intervalSeconds: int -> logger: ILogger -> state: MetricsState -> IDisposable
