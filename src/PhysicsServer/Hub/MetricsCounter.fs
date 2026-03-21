namespace PhysicsServer.Hub

open System
open System.Threading
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts

[<RequireQualifiedAccess>]
module MetricsCounter =

    type MetricsState =
        { ServiceName: string
          mutable MessagesSent: int64
          mutable MessagesReceived: int64
          mutable BytesSent: int64
          mutable BytesReceived: int64 }

    let create (serviceName: string) : MetricsState =
        { ServiceName = serviceName
          MessagesSent = 0L
          MessagesReceived = 0L
          BytesSent = 0L
          BytesReceived = 0L }

    let incrementSent (count: int) (bytes: int64) (state: MetricsState) : unit =
        Interlocked.Add(&state.MessagesSent, int64 count) |> ignore
        Interlocked.Add(&state.BytesSent, bytes) |> ignore

    let incrementReceived (count: int) (bytes: int64) (state: MetricsState) : unit =
        Interlocked.Add(&state.MessagesReceived, int64 count) |> ignore
        Interlocked.Add(&state.BytesReceived, bytes) |> ignore

    let snapshot (state: MetricsState) : ServiceMetricsReport =
        let report = ServiceMetricsReport()
        report.ServiceName <- state.ServiceName
        report.MessagesSent <- Interlocked.Read(&state.MessagesSent)
        report.MessagesReceived <- Interlocked.Read(&state.MessagesReceived)
        report.BytesSent <- Interlocked.Read(&state.BytesSent)
        report.BytesReceived <- Interlocked.Read(&state.BytesReceived)
        report

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
