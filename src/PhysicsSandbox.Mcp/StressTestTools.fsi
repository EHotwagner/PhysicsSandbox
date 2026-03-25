namespace PhysicsSandbox.Mcp

open System
open System.Threading.Tasks
open ModelContextProtocol.Server

[<McpServerToolType; Class>]
type StressTestTools =
    [<McpServerTool>]
    static member start_stress_test : conn: GrpcConnection.GrpcConnection * scenario: string * max_bodies: Nullable<int> * duration_seconds: Nullable<int> -> Task<string>

    [<McpServerTool>]
    static member get_stress_test_status : conn: GrpcConnection.GrpcConnection * test_id: string -> Task<string>
