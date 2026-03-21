namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

[<McpServerToolType>]
type ComparisonTools() =

    [<McpServerTool; Description("Run an MCP vs direct scripting performance comparison. Runs the same scenario (add N bodies, step M times) via individual gRPC calls and batched calls, comparing timing and overhead. Returns a test ID — poll with get_stress_test_status.")>]
    static member start_comparison_test(
        conn: GrpcConnection,
        [<Description("Number of bodies to add in each path (default 100)")>] ?body_count: int,
        [<Description("Number of simulation steps in each path (default 60)")>] ?step_count: int
    ) : Task<string> =
        task {
            try
                let bodies = defaultArg body_count 100
                let steps = defaultArg step_count 60
                let testId = StressTestRunner.startTest conn "mcp-vs-script" bodies steps
                return $"Comparison test started: {testId}. Poll with get_stress_test_status."
            with ex ->
                return $"Error starting comparison test: {ex.Message}"
        }
