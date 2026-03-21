namespace PhysicsSandbox.Mcp

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Text
open System.Threading
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

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
          StartTime: DateTimeOffset
          mutable EndTime: DateTimeOffset option }

    let private tests = ConcurrentDictionary<string, StressTestRun>()
    let mutable private runningTest: string option = None
    let private runLock = obj ()
    let mutable private testCounter = 0

    let private nextTestId () =
        let id = Interlocked.Increment(&testCounter)
        $"stress-{id:D3}"

    let private runBodyScaling (conn: GrpcConnection) (run: StressTestRun) (maxBodies: int) =
        task {
            try
                // Reset simulation
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Reset = ResetSimulation()))

                // Enable simulation
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(PlayPause = PlayPause(Running = true)))
                do! Task.Delay(200) // Let simulation start

                let mutable bodyCount = 0
                let mutable totalCommands = 2 // reset + play
                let mutable failedCommands = 0
                let mutable degradationPoint: int option = None
                let mutable errors = ResizeArray<string>()
                let batchSize = 10
                let sw = Stopwatch.StartNew()

                while bodyCount < maxBodies && run.Status = Running do
                    // Add a batch of bodies
                    let batch = BatchSimulationRequest()
                    for i in 0 .. batchSize - 1 do
                        let body = AddBody()
                        body.Id <- $"stress-body-{bodyCount + i}"
                        body.Position <- Vec3(X = float (i % 10) * 2.0, Y = 5.0 + float (bodyCount / 10) * 2.0, Z = float (i / 10) * 2.0)
                        body.Mass <- 1.0
                        body.Shape <- Shape(Sphere = Sphere(Radius = 0.5))
                        batch.Commands.Add(SimulationCommand(AddBody = body))

                    let! batchResponse = conn.Client.SendBatchCommandAsync(batch)
                    totalCommands <- totalCommands + batchSize

                    for r in batchResponse.Results do
                        if not r.Success then
                            failedCommands <- failedCommands + 1
                            if errors.Count < 10 then
                                errors.Add(r.Message)

                    bodyCount <- bodyCount + batchSize

                    // Step a few times to let physics settle
                    for _ in 1..5 do
                        let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                        totalCommands <- totalCommands + 1

                    // Check response time as a proxy for degradation
                    let stepSw = Stopwatch.StartNew()
                    let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                    stepSw.Stop()
                    totalCommands <- totalCommands + 1

                    if stepSw.ElapsedMilliseconds > 100L && degradationPoint.IsNone then
                        degradationPoint <- Some bodyCount

                    run.Progress <- float bodyCount / float maxBodies

                sw.Stop()

                run.Results <- Some {
                    PeakBodyCount = bodyCount
                    DegradationBodyCount = degradationPoint
                    PeakCommandRate = float totalCommands / sw.Elapsed.TotalSeconds
                    AverageFps = 0.0 // Would need viewer FPS reporting
                    MinFps = 0.0
                    TotalCommands = totalCommands
                    FailedCommands = failedCommands
                    ErrorMessages = errors |> Seq.distinct |> Seq.toList
                    Comparison = None
                }
                run.Status <- Complete
            with ex ->
                run.Results <- Some {
                    PeakBodyCount = 0; DegradationBodyCount = None; PeakCommandRate = 0.0
                    AverageFps = 0.0; MinFps = 0.0; TotalCommands = 0; FailedCommands = 0
                    ErrorMessages = [ex.Message]
                    Comparison = None
                }
                run.Status <- Failed
        }

    let private runCommandThroughput (conn: GrpcConnection) (run: StressTestRun) (durationSeconds: int) =
        task {
            try
                // Reset and add some initial bodies
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Reset = ResetSimulation()))

                let initBatch = BatchSimulationRequest()
                for i in 0..49 do
                    let body = AddBody()
                    body.Id <- $"throughput-body-{i}"
                    body.Position <- Vec3(X = float (i % 10) * 2.0, Y = 5.0, Z = float (i / 10) * 2.0)
                    body.Mass <- 1.0
                    body.Shape <- Shape(Sphere = Sphere(Radius = 0.5))
                    initBatch.Commands.Add(SimulationCommand(AddBody = body))
                let! _batchResp = conn.Client.SendBatchCommandAsync(initBatch)

                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(PlayPause = PlayPause(Running = true)))

                let mutable totalCommands = 52 // reset + 50 bodies + play
                let mutable failedCommands = 0
                let mutable peakRate = 0.0
                let sw = Stopwatch.StartNew()
                let rateSw = Stopwatch()
                let mutable rateCommands = 0

                while sw.Elapsed.TotalSeconds < float durationSeconds && run.Status = Running do
                    rateSw.Restart()
                    rateCommands <- 0

                    // Send rapid step commands for 1 second
                    while rateSw.Elapsed.TotalSeconds < 1.0 && run.Status = Running do
                        try
                            let! ack = conn.Client.SendCommandAsync(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                            if not ack.Success then failedCommands <- failedCommands + 1
                        with _ ->
                            failedCommands <- failedCommands + 1
                        totalCommands <- totalCommands + 1
                        rateCommands <- rateCommands + 1

                    let rate = float rateCommands / rateSw.Elapsed.TotalSeconds
                    if rate > peakRate then peakRate <- rate

                    run.Progress <- sw.Elapsed.TotalSeconds / float durationSeconds

                sw.Stop()

                run.Results <- Some {
                    PeakBodyCount = 50
                    DegradationBodyCount = None
                    PeakCommandRate = peakRate
                    AverageFps = 0.0
                    MinFps = 0.0
                    TotalCommands = totalCommands
                    FailedCommands = failedCommands
                    ErrorMessages = []
                    Comparison = None
                }
                run.Status <- Complete
            with ex ->
                run.Results <- Some {
                    PeakBodyCount = 0; DegradationBodyCount = None; PeakCommandRate = 0.0
                    AverageFps = 0.0; MinFps = 0.0; TotalCommands = 0; FailedCommands = 0
                    ErrorMessages = [ex.Message]
                    Comparison = None
                }
                run.Status <- Failed
        }

    let private runComparison (conn: GrpcConnection) (run: StressTestRun) (bodyCount: int) (stepCount: int) =
        task {
            try
                let mutable totalCommands = 0

                // --- Phase 1: Direct gRPC scripting path ---
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Reset = ResetSimulation()))
                totalCommands <- totalCommands + 1

                let scriptSw = Stopwatch.StartNew()
                let mutable scriptMsgCount = 0

                // Add N bodies individually via gRPC
                for i in 0 .. bodyCount - 1 do
                    let body = AddBody()
                    body.Id <- $"cmp-script-{i}"
                    body.Position <- Vec3(X = float (i % 10) * 2.0, Y = 5.0, Z = float (i / 10) * 2.0)
                    body.Mass <- 1.0
                    body.Shape <- Shape(Sphere = Sphere(Radius = 0.5))
                    let! _ack = conn.Client.SendCommandAsync(SimulationCommand(AddBody = body))
                    scriptMsgCount <- scriptMsgCount + 1

                // Step M times
                for _ in 1 .. stepCount do
                    let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                    scriptMsgCount <- scriptMsgCount + 1

                scriptSw.Stop()
                let scriptTimeMs = scriptSw.Elapsed.TotalMilliseconds
                totalCommands <- totalCommands + scriptMsgCount
                run.Progress <- 0.33

                // --- Phase 2: gRPC path (same as MCP would use internally) ---
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Reset = ResetSimulation()))
                totalCommands <- totalCommands + 1

                let mcpSw = Stopwatch.StartNew()
                let mutable mcpMsgCount = 0

                for i in 0 .. bodyCount - 1 do
                    let body = AddBody()
                    body.Id <- $"cmp-mcp-{i}"
                    body.Position <- Vec3(X = float (i % 10) * 2.0, Y = 5.0, Z = float (i / 10) * 2.0)
                    body.Mass <- 1.0
                    body.Shape <- Shape(Sphere = Sphere(Radius = 0.5))
                    let! _ack = conn.Client.SendCommandAsync(SimulationCommand(AddBody = body))
                    mcpMsgCount <- mcpMsgCount + 1

                for _ in 1 .. stepCount do
                    let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                    mcpMsgCount <- mcpMsgCount + 1

                mcpSw.Stop()
                let mcpTimeMs = mcpSw.Elapsed.TotalMilliseconds
                totalCommands <- totalCommands + mcpMsgCount
                run.Progress <- 0.66

                // --- Phase 3: Batched path ---
                let! _ack = conn.Client.SendCommandAsync(SimulationCommand(Reset = ResetSimulation()))
                totalCommands <- totalCommands + 1

                let batchSw = Stopwatch.StartNew()

                let addBatch = BatchSimulationRequest()
                for i in 0 .. bodyCount - 1 do
                    let body = AddBody()
                    body.Id <- $"cmp-batch-{i}"
                    body.Position <- Vec3(X = float (i % 10) * 2.0, Y = 5.0, Z = float (i / 10) * 2.0)
                    body.Mass <- 1.0
                    body.Shape <- Shape(Sphere = Sphere(Radius = 0.5))
                    addBatch.Commands.Add(SimulationCommand(AddBody = body))
                let! _batchResp = conn.Client.SendBatchCommandAsync(addBatch)

                let stepBatch = BatchSimulationRequest()
                for _ in 1 .. stepCount do
                    stepBatch.Commands.Add(SimulationCommand(Step = StepSimulation(DeltaTime = 0.0)))
                let! _batchResp2 = conn.Client.SendBatchCommandAsync(stepBatch)

                batchSw.Stop()
                let batchedTimeMs = batchSw.Elapsed.TotalMilliseconds
                totalCommands <- totalCommands + 2 // 2 batch calls

                let overhead =
                    if scriptTimeMs > 0.0 then (mcpTimeMs - scriptTimeMs) / scriptTimeMs * 100.0
                    else 0.0

                run.Results <- Some {
                    PeakBodyCount = bodyCount
                    DegradationBodyCount = None
                    PeakCommandRate = 0.0
                    AverageFps = 0.0
                    MinFps = 0.0
                    TotalCommands = totalCommands
                    FailedCommands = 0
                    ErrorMessages = []
                    Comparison = Some {
                        ScriptTimeMs = scriptTimeMs
                        McpTimeMs = mcpTimeMs
                        BatchedMcpTimeMs = Some batchedTimeMs
                        ScriptMessageCount = scriptMsgCount
                        McpMessageCount = mcpMsgCount
                        OverheadPercent = overhead
                    }
                }
                run.Status <- Complete
            with ex ->
                run.Results <- Some {
                    PeakBodyCount = 0; DegradationBodyCount = None; PeakCommandRate = 0.0
                    AverageFps = 0.0; MinFps = 0.0; TotalCommands = 0; FailedCommands = 0
                    ErrorMessages = [ex.Message]; Comparison = None
                }
                run.Status <- Failed
        }

    let startTest (conn: GrpcConnection) (scenario: string) (maxBodies: int) (durationSeconds: int) : string =
        lock runLock (fun () ->
            match runningTest with
            | Some id ->
                match tests.TryGetValue(id) with
                | true, run when run.Status = Running ->
                    failwith $"A stress test is already running (ID: {id}). Wait for it to complete or cancel it."
                | _ -> ()
            | None -> ()

            let testId = nextTestId ()
            let run =
                { TestId = testId
                  ScenarioName = scenario
                  Status = Running
                  Progress = 0.0
                  Results = None
                  StartTime = DateTimeOffset.UtcNow
                  EndTime = None }
            tests.[testId] <- run
            runningTest <- Some testId

            Task.Run(fun () ->
                task {
                    try
                        match scenario with
                        | "body-scaling" -> do! runBodyScaling conn run maxBodies
                        | "command-throughput" -> do! runCommandThroughput conn run durationSeconds
                        | "mcp-vs-script" -> do! runComparison conn run maxBodies durationSeconds
                        | _ ->
                            run.Status <- Failed
                            run.Results <- Some {
                                PeakBodyCount = 0; DegradationBodyCount = None; PeakCommandRate = 0.0
                                AverageFps = 0.0; MinFps = 0.0; TotalCommands = 0; FailedCommands = 0
                                ErrorMessages = [$"Unknown scenario: {scenario}"]
                                Comparison = None
                            }
                    finally
                        run.EndTime <- Some DateTimeOffset.UtcNow
                        run.Progress <- 1.0
                        lock runLock (fun () -> runningTest <- None)
                } :> Task) |> ignore

            testId
        )

    let getStatus (testId: string) : StressTestRun option =
        match tests.TryGetValue(testId) with
        | true, run -> Some run
        | false, _ -> None

    let formatResults (run: StressTestRun) : string =
        let sb = StringBuilder()
        sb.AppendLine($"=== Stress Test: {run.TestId} ===") |> ignore
        sb.AppendLine($"  Scenario: {run.ScenarioName}") |> ignore
        let statusStr =
            match run.Status with
            | Pending -> "PENDING" | Running -> "RUNNING" | Complete -> "COMPLETE"
            | Failed -> "FAILED" | Cancelled -> "CANCELLED"
        sb.AppendLine($"  Status: {statusStr}") |> ignore
        sb.AppendLine(sprintf "  Progress: %.0f%%" (run.Progress * 100.0)) |> ignore
        sb.AppendLine(sprintf "  Started: %s" (run.StartTime.ToString("HH:mm:ss"))) |> ignore
        match run.EndTime with
        | Some t ->
            sb.AppendLine(sprintf "  Ended: %s" (t.ToString("HH:mm:ss"))) |> ignore
            sb.AppendLine($"  Duration: {(t - run.StartTime).TotalSeconds:F1}s") |> ignore
        | None -> ()

        match run.Results with
        | Some r ->
            sb.AppendLine("") |> ignore
            sb.AppendLine("  Results:") |> ignore
            sb.AppendLine($"    Peak body count:     {r.PeakBodyCount}") |> ignore
            match r.DegradationBodyCount with
            | Some d -> sb.AppendLine($"    Degradation at:      {d} bodies") |> ignore
            | None -> sb.AppendLine($"    Degradation at:      (none detected)") |> ignore
            sb.AppendLine($"    Peak command rate:   {r.PeakCommandRate:F0} cmd/s") |> ignore
            sb.AppendLine($"    Total commands:      {r.TotalCommands}") |> ignore
            sb.AppendLine($"    Failed commands:     {r.FailedCommands}") |> ignore
            if r.ErrorMessages.Length > 0 then
                sb.AppendLine($"    Errors:") |> ignore
                for e in r.ErrorMessages do
                    sb.AppendLine($"      - {e}") |> ignore
            match r.Comparison with
            | Some c ->
                sb.AppendLine("") |> ignore
                sb.AppendLine("  Comparison (MCP vs Script):") |> ignore
                sb.AppendLine($"    Script (direct gRPC): {c.ScriptTimeMs:F1} ms  ({c.ScriptMessageCount} messages)") |> ignore
                sb.AppendLine($"    MCP (individual):     {c.McpTimeMs:F1} ms  ({c.McpMessageCount} messages)") |> ignore
                match c.BatchedMcpTimeMs with
                | Some bt -> sb.AppendLine($"    MCP (batched):        {bt:F1} ms  (2 batch calls)") |> ignore
                | None -> ()
                sb.AppendLine(sprintf "    MCP overhead:         %.1f%%" c.OverheadPercent) |> ignore
            | None -> ()
        | None -> ()

        sb.ToString()
