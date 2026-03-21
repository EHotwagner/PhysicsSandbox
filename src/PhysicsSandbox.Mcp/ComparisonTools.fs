namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

/// <summary>MCP server tool type for running performance comparisons between direct gRPC scripting, individual MCP commands, and batched MCP commands.</summary>
[<McpServerToolType>]
type ComparisonTools() =

    /// <summary>Starts a three-phase comparison test that runs the same workload (add N bodies, step M times) via direct gRPC, individual MCP calls, and batched MCP calls, measuring timing differences and overhead.</summary>
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
