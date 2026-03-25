namespace PhysicsSandbox.Mcp

open System
open System.Threading.Tasks
open ModelContextProtocol.Server

[<McpServerToolType; Class>]
type ComparisonTools =
    [<McpServerTool>]
    static member start_comparison_test : conn: GrpcConnection.GrpcConnection * body_count: Nullable<int> * step_count: Nullable<int> -> Task<string>
