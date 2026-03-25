namespace PhysicsSandbox.Mcp

open System
open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

/// <summary>MCP server tool type for launching and monitoring background stress tests that measure simulation performance limits.</summary>
[<McpServerToolType>]
type StressTestTools() =

    /// <summary>Starts a background stress test using the specified scenario. Returns a test ID that can be polled with get_stress_test_status to check progress and results.</summary>
    [<McpServerTool; Description("Start a stress test scenario in the background. Available scenarios: 'body-scaling' (adds bodies until degradation), 'command-throughput' (measures max command rate). Returns a test ID for polling.")>]
    static member start_stress_test(
        conn: GrpcConnection,
        [<Description("Scenario name: 'body-scaling' or 'command-throughput'")>] scenario: string,
        [<Description("Applies to 'body-scaling' scenario. Maximum number of bodies to add before stopping. Default: 500. Ignored for other scenarios.")>] max_bodies: Nullable<int>,
        [<Description("Applies to 'command-throughput' scenario. Test duration in seconds. Default: 30. Ignored for other scenarios.")>] duration_seconds: Nullable<int>
    ) : Task<string> =
        task {
            try
                let maxBodies = if max_bodies.HasValue then max_bodies.Value else 500
                let duration = if duration_seconds.HasValue then duration_seconds.Value else 30
                let testId = StressTestRunner.startTest conn scenario maxBodies duration
                return $"Stress test started: {testId} (scenario: {scenario}). Poll with get_stress_test_status."
            with ex ->
                return $"Error starting stress test: {ex.Message}"
        }

    /// <summary>Retrieves the current status, progress, and results of a stress test identified by the given test ID.</summary>
    [<McpServerTool; Description("Get the status and results of a running or completed stress test.")>]
    static member get_stress_test_status(
        conn: GrpcConnection,
        [<Description("Test ID returned by start_stress_test")>] test_id: string
    ) : Task<string> =
        task {
            match StressTestRunner.getStatus test_id with
            | Some run -> return StressTestRunner.formatResults run
            | None -> return $"No stress test found with ID: {test_id}"
        }
