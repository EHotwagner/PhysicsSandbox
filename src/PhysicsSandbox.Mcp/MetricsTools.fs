namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Text
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

[<McpServerToolType>]
type MetricsTools() =

    [<McpServerTool; Description("Get performance metrics from all services: message counts, data volumes, and pipeline timings")>]
    static member get_metrics(conn: GrpcConnection) : Task<string> =
        task {
            try
                let! response = conn.Client.GetMetricsAsync(MetricsRequest())
                let sb = StringBuilder()
                sb.AppendLine("=== Service Metrics ===") |> ignore

                for svc in response.Services do
                    sb.AppendLine($"  [{svc.ServiceName}]") |> ignore
                    sb.AppendLine($"    Messages: sent={svc.MessagesSent} recv={svc.MessagesReceived}") |> ignore
                    sb.AppendLine($"    Bytes:    sent={svc.BytesSent} recv={svc.BytesReceived}") |> ignore

                // Append MCP server's own metrics
                let mcpMetrics = conn.LocalMetrics
                sb.AppendLine($"  [{mcpMetrics.ServiceName}]") |> ignore
                sb.AppendLine($"    Messages: sent={mcpMetrics.MessagesSent} recv={mcpMetrics.MessagesReceived}") |> ignore
                sb.AppendLine($"    Bytes:    sent={mcpMetrics.BytesSent} recv={mcpMetrics.BytesReceived}") |> ignore

                if not (isNull response.Pipeline) then
                    sb.AppendLine("") |> ignore
                    sb.AppendLine("=== Pipeline Timings ===") |> ignore
                    sb.AppendLine($"  Simulation tick:   {response.Pipeline.SimulationTickMs:F2} ms") |> ignore
                    sb.AppendLine($"  Serialization:     {response.Pipeline.StateSerializationMs:F2} ms") |> ignore
                    sb.AppendLine($"  gRPC transfer:     {response.Pipeline.GrpcTransferMs:F2} ms") |> ignore
                    sb.AppendLine($"  Viewer render:     {response.Pipeline.ViewerRenderMs:F2} ms") |> ignore
                    sb.AppendLine($"  Total pipeline:    {response.Pipeline.TotalPipelineMs:F2} ms") |> ignore

                return sb.ToString()
            with ex ->
                return $"Error fetching metrics: {ex.Message}"
        }

    [<McpServerTool; Description("Get pipeline diagnostics: timing breakdown across simulation tick, serialization, gRPC transfer, and rendering stages. Highlights the slowest stage.")>]
    static member get_diagnostics(conn: GrpcConnection) : Task<string> =
        task {
            try
                let! response = conn.Client.GetMetricsAsync(MetricsRequest())
                let p = response.Pipeline
                if isNull p then
                    return "No pipeline timings available yet (simulation may not have stepped)"
                else
                    let sb = StringBuilder()
                    sb.AppendLine("=== Pipeline Diagnostics ===") |> ignore
                    sb.AppendLine("") |> ignore

                    let stages = [
                        ("Simulation tick", p.SimulationTickMs)
                        ("Serialization", p.StateSerializationMs)
                        ("gRPC transfer", p.GrpcTransferMs)
                        ("Viewer render", p.ViewerRenderMs)
                    ]

                    let maxStage = stages |> List.maxBy snd
                    let total = p.TotalPipelineMs

                    for (name, ms) in stages do
                        let pct = if total > 0.0 then ms / total * 100.0 else 0.0
                        let marker = if name = fst maxStage && ms > 0.0 then " << SLOWEST" else ""
                        let line = sprintf "  %-20s %8.2f ms  (%.0f%%)%s" name ms pct marker
                        sb.AppendLine(line) |> ignore

                    let totalLine = sprintf "  %-20s %8.2f ms" "Total" total
                    sb.AppendLine(totalLine) |> ignore
                    return sb.ToString()
            with ex ->
                return $"Error fetching diagnostics: {ex.Message}"
        }
