namespace PhysicsSandbox.Mcp

open System.Threading.Tasks
open ModelContextProtocol.Server

[<McpServerToolType; Class>]
type MetricsTools =
    [<McpServerTool>]
    static member get_metrics : conn: GrpcConnection.GrpcConnection -> Task<string>

    [<McpServerTool>]
    static member get_diagnostics : conn: GrpcConnection.GrpcConnection -> Task<string>
