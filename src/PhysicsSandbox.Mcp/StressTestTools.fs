namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection

[<McpServerToolType>]
type StressTestTools() =

    [<McpServerTool; Description("Start a stress test scenario in the background. Available scenarios: 'body-scaling' (adds bodies until degradation), 'command-throughput' (measures max command rate). Returns a test ID for polling.")>]
    static member start_stress_test(
        conn: GrpcConnection,
        [<Description("Scenario name: 'body-scaling' or 'command-throughput'")>] scenario: string,
        [<Description("Maximum bodies for body-scaling scenario (default 500)")>] ?max_bodies: int,
        [<Description("Duration in seconds for command-throughput scenario (default 30)")>] ?duration_seconds: int
    ) : Task<string> =
        task {
            try
                let maxBodies = defaultArg max_bodies 500
                let duration = defaultArg duration_seconds 30
                let testId = StressTestRunner.startTest conn scenario maxBodies duration
                return $"Stress test started: {testId} (scenario: {scenario}). Poll with get_stress_test_status."
            with ex ->
                return $"Error starting stress test: {ex.Message}"
        }

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
