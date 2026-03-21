namespace PhysicsSandbox.Mcp

open System.Threading.Tasks
open ModelContextProtocol.Server

[<McpServerToolType; Class>]
type ComparisonTools =
    [<McpServerTool>]
    static member start_comparison_test : conn: GrpcConnection.GrpcConnection * ?body_count: int * ?step_count: int -> Task<string>
