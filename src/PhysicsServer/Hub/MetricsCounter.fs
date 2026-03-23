namespace PhysicsServer.Hub

open System
open System.Threading
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts

/// <summary>Thread-safe message and byte counters for tracking server throughput metrics.</summary>
[<RequireQualifiedAccess>]
module MetricsCounter =

    /// <summary>Mutable, thread-safe counters tracking messages and bytes sent/received for a named service.</summary>
    type MetricsState =
        { ServiceName: string
          mutable MessagesSent: int64
          mutable MessagesReceived: int64
          mutable BytesSent: int64
          mutable BytesReceived: int64
          mutable MeshesCachedTotal: int64
          mutable FetchRequestsTotal: int64
          mutable FetchHitsTotal: int64
          mutable FetchMissesTotal: int64 }

    /// <summary>Create a new zeroed metrics state for the given service name.</summary>
    /// <param name="serviceName">The display name used when reporting metrics.</param>
    /// <returns>A fresh MetricsState with all counters at zero.</returns>
    let create (serviceName: string) : MetricsState =
        { ServiceName = serviceName
          MessagesSent = 0L
          MessagesReceived = 0L
          BytesSent = 0L
          BytesReceived = 0L
          MeshesCachedTotal = 0L
          FetchRequestsTotal = 0L
          FetchHitsTotal = 0L
          FetchMissesTotal = 0L }

    /// <summary>Atomically increment the sent message count and byte total.</summary>
    /// <param name="count">Number of messages sent.</param>
    /// <param name="bytes">Total bytes sent.</param>
    /// <param name="state">The metrics state to update.</param>
    let incrementSent (count: int) (bytes: int64) (state: MetricsState) : unit =
        Interlocked.Add(&state.MessagesSent, int64 count) |> ignore
        Interlocked.Add(&state.BytesSent, bytes) |> ignore

    /// <summary>Atomically increment the received message count and byte total.</summary>
    /// <param name="count">Number of messages received.</param>
    /// <param name="bytes">Total bytes received.</param>
    /// <param name="state">The metrics state to update.</param>
    let incrementReceived (count: int) (bytes: int64) (state: MetricsState) : unit =
        Interlocked.Add(&state.MessagesReceived, int64 count) |> ignore
        Interlocked.Add(&state.BytesReceived, bytes) |> ignore

    /// <summary>Atomically increment the mesh cache counters.</summary>
    let incrementMeshesCached (count: int) (state: MetricsState) : unit =
        Interlocked.Add(&state.MeshesCachedTotal, int64 count) |> ignore

    /// <summary>Record a FetchMeshes request with hit/miss counts.</summary>
    let incrementFetchRequest (hits: int) (misses: int) (state: MetricsState) : unit =
        Interlocked.Increment(&state.FetchRequestsTotal) |> ignore
        Interlocked.Add(&state.FetchHitsTotal, int64 hits) |> ignore
        Interlocked.Add(&state.FetchMissesTotal, int64 misses) |> ignore

    /// <summary>Capture a point-in-time snapshot of the current metrics as a protobuf report.</summary>
    /// <param name="state">The metrics state to read.</param>
    /// <returns>A ServiceMetricsReport populated with the current counter values.</returns>
    let snapshot (state: MetricsState) : ServiceMetricsReport =
        let report = ServiceMetricsReport()
        report.ServiceName <- state.ServiceName
        report.MessagesSent <- Interlocked.Read(&state.MessagesSent)
        report.MessagesReceived <- Interlocked.Read(&state.MessagesReceived)
        report.BytesSent <- Interlocked.Read(&state.BytesSent)
        report.BytesReceived <- Interlocked.Read(&state.BytesReceived)
        report

    /// <summary>Start a background timer that logs the current metrics at a fixed interval.</summary>
    /// <param name="intervalSeconds">Seconds between each log entry.</param>
    /// <param name="logger">The logger to write metrics summaries to.</param>
    /// <param name="state">The metrics state to snapshot on each tick.</param>
    /// <returns>An IDisposable that stops the timer when disposed.</returns>
    let startPeriodicLogging (intervalSeconds: int) (logger: ILogger) (state: MetricsState) : IDisposable =
        let timer =
            new Timer(
                (fun _ ->
                    let s = snapshot state
                    logger.LogInformation(
                        "Metrics [{ServiceName}] sent={MessagesSent} recv={MessagesReceived} bytesSent={BytesSent} bytesRecv={BytesReceived}",
                        s.ServiceName,
                        s.MessagesSent,
                        s.MessagesReceived,
                        s.BytesSent,
                        s.BytesReceived
                    )),
                null,
                TimeSpan.FromSeconds(float intervalSeconds),
                TimeSpan.FromSeconds(float intervalSeconds)
            )

        timer :> IDisposable
