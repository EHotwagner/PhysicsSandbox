namespace PhysicsSandbox.Mcp

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module StressTestRunner =

    type TestStatus =
        | Pending
        | Running
        | Complete
        | Failed
        | Cancelled

    type ComparisonData =
        { ScriptTimeMs: float
          McpTimeMs: float
          BatchedMcpTimeMs: float option
          ScriptMessageCount: int
          McpMessageCount: int
          OverheadPercent: float }

    type StressTestResults =
        { PeakBodyCount: int
          DegradationBodyCount: int option
          PeakCommandRate: float
          AverageFps: float
          MinFps: float
          TotalCommands: int
          FailedCommands: int
          ErrorMessages: string list
          Comparison: ComparisonData option }

    type StressTestRun =
        { TestId: string
          ScenarioName: string
          mutable Status: TestStatus
          mutable Progress: float
          mutable Results: StressTestResults option
          StartTime: System.DateTimeOffset
          mutable EndTime: System.DateTimeOffset option }

    val startTest : conn: GrpcConnection.GrpcConnection -> scenario: string -> maxBodies: int -> durationSeconds: int -> string
    val getStatus : testId: string -> StressTestRun option
    val formatResults : run: StressTestRun -> string
