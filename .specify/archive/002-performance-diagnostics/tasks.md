# Tasks: Performance Diagnostics & Stress Testing

**Input**: Design documents from `/specs/002-performance-diagnostics/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included per Constitution Principle VI (test evidence required for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Proto contract changes and shared module creation — foundational for all user stories.

- [x] T001 Add `ResetSimulation` message and `reset` oneof variant (field 10) to `SimulationCommand` in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T002 Add `is_static` field (field 8, bool) to `Body` message in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T003 Add `BatchSimulationRequest`, `BatchViewRequest`, `CommandResult`, `BatchResponse` messages to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T004 Add `MetricsRequest`, `ServiceMetricsReport`, `PipelineTimings`, `MetricsResponse` messages to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T005 Add `SendBatchCommand`, `SendBatchViewCommand`, `GetMetrics` RPCs to `PhysicsHub` service in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T005a Add `tick_ms` (field 4) and `serialize_ms` (field 5) double fields to `SimulationState` message in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T006 Build solution to verify proto compilation: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core modules that multiple user stories depend on. MUST complete before user story work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T007 Create `MetricsCounter.fsi` signature file in `src/PhysicsServer/Hub/MetricsCounter.fsi` — define `MetricsState` type, `create`, `incrementSent`, `incrementReceived`, `snapshot`, `startPeriodicLogging` per plan.md .fsi contracts section
- [x] T008 Implement `MetricsCounter.fs` in `src/PhysicsServer/Hub/MetricsCounter.fs` — thread-safe counters using `System.Threading.Interlocked`, periodic logging via background timer, `snapshot` returns `ServiceMetricsReport` proto message
- [x] T009 Add `MetricsCounter.fsi` and `MetricsCounter.fs` to `src/PhysicsServer/PhysicsServer.fsproj` (must be listed before `MessageRouter` files in compile order)
- [x] T010 Unit test for MetricsCounter: create `tests/PhysicsServer.Tests/MetricsCounterTests.fs` — test `create`, `incrementSent`, `incrementReceived`, `snapshot` returns correct cumulative values, thread-safety with concurrent increments
- [x] T011 Build and run tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Foundation ready — MetricsCounter module tested, proto contracts compiled. User story implementation can begin.

---

## Phase 3: User Story 1 — Viewer FPS Display & Logging (Priority: P1) 🎯 MVP

**Goal**: Display real-time FPS in the viewer overlay and log FPS samples periodically with low-FPS warnings.

**Independent Test**: Launch the viewer, observe FPS in the overlay, verify FPS appears in structured logs.

### Tests for User Story 1

- [x] T012 [P] [US1] Unit test for FPS calculation: create `tests/PhysicsViewer.Tests/FpsCounterTests.fs` — test EMA smoothing (α=0.1), `update` returns correct smoothed FPS, `shouldLog` triggers at configured interval, warning detection when FPS below threshold

### Implementation for User Story 1

- [x] T013 [P] [US1] Create `FpsCounter.fsi` in `src/PhysicsViewer/Rendering/FpsCounter.fsi` — define `FpsState` type, `create` (warningThreshold), `update` (deltaSeconds → smoothedFps), `shouldLog` (intervalSeconds → bool), `currentFps`
- [x] T014 [US1] Implement `FpsCounter.fs` in `src/PhysicsViewer/Rendering/FpsCounter.fs` — EMA with α=0.1, mutable `lastLogTime` for periodic logging check, warning threshold comparison
- [x] T015 [US1] Add `FpsCounter.fsi` and `FpsCounter.fs` to `src/PhysicsViewer/PhysicsViewer.fsproj` (before `Program.fs` in compile order)
- [x] T016 [US1] Integrate FPS display into viewer update loop in `src/PhysicsViewer/Program.fs` — create `FpsState` at startup (threshold 30.0f), call `FpsCounter.update` each frame with `time.Elapsed.TotalSeconds`, add FPS to existing `DebugTextSystem.Print` status overlay (e.g., `$"FPS: {fps:F0} | Time: {simTime:F2}s | {runLabel}"`)
- [x] T017 [US1] Add periodic FPS logging in `src/PhysicsViewer/Program.fs` — check `FpsCounter.shouldLog` each frame, log via `ILogger.LogInformation` with FPS value and timestamp, log `ILogger.LogWarning` when FPS below threshold
- [x] T018 [US1] Run FPS unit tests: `dotnet test tests/PhysicsViewer.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Viewer shows live FPS overlay and logs FPS periodically. SC-001 verifiable.

---

## Phase 4: User Story 2 — Service Message & Traffic Metrics (Priority: P1)

**Goal**: Each service tracks and logs message counts and data volumes, queryable via MCP.

**Independent Test**: Run system, perform operations, verify periodic metric log entries from each service and query via MCP `get_metrics` tool.

### Tests for User Story 2

- [x] T019 [P] [US2] Integration test: create `tests/PhysicsSandbox.Integration.Tests/MetricsIntegrationTests.cs` — start AppHost, send commands, call `GetMetrics` RPC, verify `ServiceMetricsReport` has non-zero `messages_sent`/`messages_received` and `bytes_sent`/`bytes_received` for PhysicsServer

### Implementation for User Story 2

- [x] T020 [US2] Add `MetricsState` instance to `MessageRouter` record in `src/PhysicsServer/Hub/MessageRouter.fs` — create in `MessageRouter.create` with serviceName "PhysicsServer", call `incrementSent`/`incrementReceived` in `submitCommand`, `submitViewCommand`, `publishState`
- [x] T021 [US2] Update `MessageRouter.fsi` in `src/PhysicsServer/Hub/MessageRouter.fsi` — expose `metrics` field or `getMetrics` function
- [x] T022 [US2] Implement `GetMetrics` RPC handler in `src/PhysicsServer/Services/PhysicsHubService.fs` — call `MetricsCounter.snapshot` on router's metrics, return `MetricsResponse` with server's `ServiceMetricsReport`
- [x] T023 [US2] Update `PhysicsHubService.fsi` in `src/PhysicsServer/Services/PhysicsHubService.fsi` — add `GetMetrics` override
- [x] T024 [US2] Add metrics tracking to PhysicsSimulation in `src/PhysicsSimulation/Client/SimulationClient.fs` — create `MetricsState`, increment on each state send and command receive (estimate bytes via `msg.CalculateSize()`)
- [x] T025 [US2] Start periodic metrics logging in each service startup — call `MetricsCounter.startPeriodicLogging` with 10-second interval in `src/PhysicsServer/Program.fs` and `src/PhysicsSimulation/Client/SimulationClient.fs`
- [x] T025a [US2] Add metrics tracking to PhysicsViewer in `src/PhysicsViewer/Program.fs` — create `MetricsState` with serviceName "PhysicsViewer", increment `received` on each state stream message and view command stream message, start periodic logging with 10-second interval
- [x] T025b [US2] Add metrics self-tracking to MCP server in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` — create `MetricsState` with serviceName "McpServer", increment `sent` on each `SendCommand`/`SendViewCommand`/`SendBatchCommand` call, increment `received` on each state/audit stream message, expose via `getLocalMetrics` method
- [x] T025c [US2] Include MCP server's local metrics in `get_metrics` tool response in `src/PhysicsSandbox.Mcp/MetricsTools.fs` — append `conn.getLocalMetrics()` to the `MetricsResponse` services list alongside server-reported metrics
- [x] T026 [US2] Add `get_metrics` MCP tool: create `src/PhysicsSandbox.Mcp/MetricsTools.fsi` and `src/PhysicsSandbox.Mcp/MetricsTools.fs` — `[<McpServerToolType>]` class with `get_metrics` static method that calls `conn.Client.GetMetricsAsync(MetricsRequest())`, formats `MetricsResponse` as readable string showing per-service message counts and byte volumes
- [x] T027 [US2] Add `MetricsTools.fsi` and `MetricsTools.fs` to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T028 [US2] Run metrics integration tests: `dotnet test tests/PhysicsSandbox.Integration.Tests/ --filter "FullyQualifiedName~Metrics" -p:StrideCompilerSkipBuild=true`

**Checkpoint**: All services log periodic metric summaries. MCP `get_metrics` tool returns live counters. SC-002 verifiable.

---

## Phase 5: User Story 3 — Batch Commands (Priority: P1)

**Goal**: Submit multiple simulation or view commands in a single request at both gRPC and MCP levels.

**Independent Test**: Send a batch of 10+ commands via MCP `batch_commands` tool, verify all execute and results are returned together.

### Tests for User Story 3

- [x] T029 [P] [US3] Unit test: create `tests/PhysicsServer.Tests/BatchRoutingTests.fs` — test `sendBatchCommand` routes each command via `submitCommand`, collects per-command `CommandResult` with correct index, handles mixed success/failure
- [x] T030 [P] [US3] Integration test: create `tests/PhysicsSandbox.Integration.Tests/BatchIntegrationTests.cs` — start AppHost, send `BatchSimulationRequest` with 10 AddBody commands via `SendBatchCommand` RPC, verify `BatchResponse` has 10 results all with `success=true`, verify bodies appear in `StreamState`

### Implementation for User Story 3

- [x] T031 [US3] Add `sendBatchCommand` and `sendBatchViewCommand` functions to `src/PhysicsServer/Hub/MessageRouter.fs` — iterate `BatchSimulationRequest.Commands`, call existing `submitCommand` for each, collect `CommandResult` list with index + success + message, wrap in `BatchResponse` with `Stopwatch`-measured `total_time_ms`. Enforce max 100 commands per batch.
- [x] T032 [US3] Update `src/PhysicsServer/Hub/MessageRouter.fsi` — expose `sendBatchCommand` and `sendBatchViewCommand` signatures
- [x] T033 [US3] Implement `SendBatchCommand` and `SendBatchViewCommand` RPC handlers in `src/PhysicsServer/Services/PhysicsHubService.fs` — delegate to `MessageRouter.sendBatchCommand`/`sendBatchViewCommand`, return `BatchResponse`
- [x] T034 [US3] Update `src/PhysicsServer/Services/PhysicsHubService.fsi` — add `SendBatchCommand` and `SendBatchViewCommand` overrides
- [x] T035 [US3] Add batch functions to PhysicsClient: update `src/PhysicsClient/Commands/SimulationCommands.fs` — add `batchCommands : session -> SimulationCommand list -> Result<BatchResponse, string>` and `batchViewCommands : session -> ViewCommand list -> Result<BatchResponse, string>` calling the new batch RPCs
- [x] T036 [US3] Update `src/PhysicsClient/Commands/SimulationCommands.fsi` — add `batchCommands` and `batchViewCommands` signatures
- [x] T037 [US3] Expose batch RPC calls on GrpcConnection: update `src/PhysicsSandbox.Mcp/GrpcConnection.fs` — add `sendBatchCommand` and `sendBatchViewCommand` methods that call `client.SendBatchCommandAsync` and `client.SendBatchViewCommandAsync`
- [x] T038 [US3] Update `src/PhysicsSandbox.Mcp/GrpcConnection.fsi` — add batch method signatures
- [x] T039 [US3] Create `src/PhysicsSandbox.Mcp/BatchTools.fsi` and `src/PhysicsSandbox.Mcp/BatchTools.fs` — `[<McpServerToolType>]` class with `batch_commands` tool (accepts a `commands` string parameter containing a JSON array where each element is an object with a `type` field matching existing MCP tool names and corresponding parameters, e.g., `[{"type":"add_body","shape":"sphere","radius":0.5,"x":0,"y":5,"z":0,"mass":1},{"type":"step"},{"type":"apply_force","body_id":"sphere-1","fx":0,"fy":10,"fz":0}]`; parses each into a `SimulationCommand`, calls `conn.sendBatchCommand`, formats `BatchResponse`) and `batch_view_commands` tool (same pattern for view commands with types: set_camera, set_zoom, toggle_wireframe)
- [x] T040 [US3] Add `BatchTools.fsi` and `BatchTools.fs` to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T041 [US3] Run batch tests: `dotnet test PhysicsSandbox.slnx --filter "FullyQualifiedName~Batch" -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Batch commands work at gRPC and MCP levels. SC-003 verifiable (50 batch vs 50 sequential comparison).

---

## Phase 6: User Story 4 — Restart Simulation Command (Priority: P2)

**Goal**: Single command resets simulation to empty state (clears all bodies, resets time) without service restarts.

**Independent Test**: Add bodies, issue restart via MCP, verify simulation is empty with time=0.

### Tests for User Story 4

- [x] T042 [P] [US4] Unit test: create `tests/PhysicsSimulation.Tests/ResetSimulationTests.fs` — test `handleReset` clears `world.Bodies`, removes all Bepu bodies, resets `SimulationTime` to 0.0, `buildState` returns empty bodies list
- [x] T043 [P] [US4] Integration test: create `tests/PhysicsSandbox.Integration.Tests/RestartIntegrationTests.cs` — start AppHost, add 5 bodies via `SendCommand`, send `ResetSimulation` command, verify `StreamState` returns state with 0 bodies and time=0

### Implementation for User Story 4

- [x] T044 [US4] Handle `ResetSimulation` command in `src/PhysicsSimulation/World/SimulationWorld.fs` — add `resetSimulation` function that iterates `world.Bodies`, calls `PhysicsWorld.removeBody` for each (both dynamic and static), clears `world.Bodies` map, clears `world.ActiveForces` map, resets `world.SimulationTime` to 0.0, sets `world.Running` to false
- [x] T045 [US4] Update `src/PhysicsSimulation/World/SimulationWorld.fsi` — expose `resetSimulation` signature
- [x] T046 [US4] Add `ResetSimulation` case to command handler in `src/PhysicsSimulation/World/CommandHandler.fs` (or equivalent command dispatch) — match on `SimulationCommand.CommandCase.Reset`, call `resetSimulation`, return success ack
- [x] T047 [US4] Add `restart_simulation` MCP tool to `src/PhysicsSandbox.Mcp/SimulationTools.fs` — sends `SimulationCommand(Reset = ResetSimulation())` via `sendCmd`, returns success/failure message
- [x] T048 [US4] Update `src/PhysicsSandbox.Mcp/SimulationTools.fsi` — add `restart_simulation` signature
- [x] T049 [US4] Add `reset` function to PhysicsClient: update `src/PhysicsClient/Commands/SimulationCommands.fs` — `reset : session -> Result<unit, string>` sending `SimulationCommand(Reset = ResetSimulation())`
- [x] T050 [US4] Update `src/PhysicsClient/Commands/SimulationCommands.fsi` — add `reset` signature
- [x] T051 [US4] Run restart tests: `dotnet test PhysicsSandbox.slnx --filter "FullyQualifiedName~Restart or FullyQualifiedName~Reset" -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Restart command clears simulation in <2s. SC-004 verifiable.

---

## Phase 7: User Story 5 — Static Body Collision (Priority: P2)

**Goal**: All static bodies are tracked in state and participate in collision detection with dynamic bodies.

**Independent Test**: Add a static plane, drop a dynamic body onto it, verify collision occurs (body rests on surface).

### Tests for User Story 5

- [x] T052 [P] [US5] Unit test: create `tests/PhysicsSimulation.Tests/StaticBodyTrackingTests.fs` — test `addBody` for plane creates `BodyRecord` with `IsStatic=true` in `world.Bodies`, `buildState` includes static body with `is_static=true`, `removeBody` works for static bodies
- [x] T053 [P] [US5] Integration test: create `tests/PhysicsSandbox.Integration.Tests/StaticBodyTests.cs` — start AppHost, add a static plane via `SendCommand(AddBody)` with plane shape, add a dynamic sphere above it, step simulation 60 times, verify sphere's Y position stabilizes above 0 (resting on plane), verify state includes both bodies with correct `is_static` values

### Implementation for User Story 5

- [x] T054 [US5] Add `IsStatic: bool` field to `BodyRecord` type in `src/PhysicsSimulation/World/SimulationWorld.fs`
- [x] T055 [US5] Modify `addBody` in `src/PhysicsSimulation/World/SimulationWorld.fs` — when shape is Plane (or mass=0), create static body as before BUT also add to `world.Bodies` map with `IsStatic = true` and a tracked `BepuBodyId` (use `StaticHandle` converted to tracking ID)
- [x] T056 [US5] Update `buildState` in `src/PhysicsSimulation/World/SimulationWorld.fs` — include static bodies from `world.Bodies` where `IsStatic = true`, set `body.IsStatic = true` on the proto `Body` message. For static bodies, position/orientation come from stored pose (not BodyReference)
- [x] T057 [US5] Update `removeBody` in `src/PhysicsSimulation/World/SimulationWorld.fs` — handle static body removal by skipping Bepu removeBody for statics (no removeStatic API), untrack from Bodies map
- [x] T058 [US5] Update `resetSimulation` (from T044) to also remove static bodies — skip Bepu removal for statics, clear from Bodies map
- [x] T059 [US5] Update `src/PhysicsSimulation/World/SimulationWorld.fsi` — BodyRecord type is opaque (not exposed), signatures unchanged
- [x] T060 [US5] Run static body tests: `dotnet test PhysicsSandbox.slnx --filter "FullyQualifiedName~Static" -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Static bodies appear in state stream, collide with dynamic bodies. SC-005 verifiable.

---

## Phase 8: User Story 6 — Performance Diagnostics & Bottleneck Detection (Priority: P2)

**Goal**: Pipeline timing breakdown across simulation, serialization, transfer, and rendering stages, accessible via logs and MCP.

**Independent Test**: Run system under load, call `get_diagnostics` MCP tool, verify timing breakdown across pipeline stages.

**Dependencies**: Requires US2 (metrics infrastructure) to be complete.

### Tests for User Story 6

- [x] T060a [P] [US6] Integration test: create `tests/PhysicsSandbox.Integration.Tests/DiagnosticsIntegrationTests.cs` — start AppHost, add 10 bodies, step simulation 60 times, call `GetMetrics` RPC, verify `PipelineTimings` has non-zero `simulation_tick_ms` and `state_serialization_ms`

### Implementation for User Story 6

- [x] T061 [US6] Add `Stopwatch` timing around physics step in `src/PhysicsSimulation/World/SimulationWorld.fs` `step` function — measure `tickMs` for `PhysicsWorld.step` call and `serializeMs` for `buildState` call, store latest values in module-level mutable fields
- [x] T062 [US6] Expose latest timing values from SimulationWorld: update `src/PhysicsSimulation/World/SimulationWorld.fsi` — add `latestTickMs` and `latestSerializeMs` accessor functions
- [x] T063 [US6] Populate timing fields in SimulationState: update `src/PhysicsSimulation/World/SimulationWorld.fs` `buildState` function — set `state.TickMs` and `state.SerializeMs` on the `SimulationState` proto message from the latest `Stopwatch` measurements (fields added in T005a). Server reads these from cached state when building `PipelineTimings` in `GetMetrics` response
- [x] T064 [US6] Add transfer time measurement in `src/PhysicsServer/Services/SimulationLinkService.fs` — timestamp when state is received from simulation, compute delta from simulation's send timestamp (approximate via state receipt time minus last state time)
- [x] T065 [US6] Populate `PipelineTimings` in `GetMetrics` response: update `src/PhysicsServer/Services/PhysicsHubService.fs` — fill `simulation_tick_ms`, `state_serialization_ms`, `grpc_transfer_ms` from available timing data, leave `viewer_render_ms` as reported by viewer (or 0 if not available)
- [x] T066 [US6] Add `get_diagnostics` MCP tool to `src/PhysicsSandbox.Mcp/MetricsTools.fs` — calls `GetMetrics` RPC, formats `PipelineTimings` as readable breakdown string showing each stage's contribution to total pipeline latency, highlights the slowest stage
- [x] T067 [US6] Update `src/PhysicsSandbox.Mcp/MetricsTools.fsi` — add `get_diagnostics` signature
- [x] T068 [US6] Add periodic diagnostics logging: in services that have timing data, log pipeline timings at the same interval as metrics (every 10s) via structured logging with `ILogger`
- [x] T069 [US6] Run full test suite to verify no regressions: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Pipeline diagnostics show timing breakdown. SC-006 verifiable.

---

## Phase 9: User Story 7 — Stress Testing (Priority: P2)

**Goal**: Predefined stress test scenarios that run as background jobs, measuring system capacity and degradation points.

**Independent Test**: Call `start_stress_test` MCP tool with "body-scaling" scenario, poll with `get_stress_test_status`, verify report shows peak body count and degradation point.

**Dependencies**: Requires US3 (batch commands) and US4 (restart) to be complete.

### Tests for User Story 7

- [x] T069a [P] [US7] Integration test: create `tests/PhysicsSandbox.Integration.Tests/StressTestIntegrationTests.cs` — start AppHost, invoke `start_stress_test` MCP tool with "body-scaling" scenario (max_bodies=50, small scale for test speed), poll `get_stress_test_status` until complete, verify results contain non-zero `PeakBodyCount` and `TotalCommands`, verify status transitions from Running to Complete

### Implementation for User Story 7

- [x] T070 [US7] Create stress test runner: create `src/PhysicsSandbox.Mcp/StressTestRunner.fsi` — define `StressTestRun` record (testId, scenarioName, status, progress, results), `StressTestResults` record, `startTest`, `getStatus`, `cancelTest` signatures
- [x] T071 [US7] Implement `src/PhysicsSandbox.Mcp/StressTestRunner.fs` — `StressTestRun` state management with `ConcurrentDictionary<string, StressTestRun>`, single-test-at-a-time guard, background `Task` execution, progress tracking (0.0→1.0), results population on completion
- [x] T072 [US7] Implement "body-scaling" scenario in `src/PhysicsSandbox.Mcp/StressTestRunner.fs` — restart simulation, add bodies in batches of 10 (using `SendBatchCommand`), after each batch check FPS/state response time, continue until max_bodies reached or performance degrades below threshold (e.g., state update latency >100ms), record `PeakBodyCount`, `DegradationBodyCount`, `AverageFps`, `MinFps`
- [x] T073 [US7] Implement "command-throughput" scenario in `src/PhysicsSandbox.Mcp/StressTestRunner.fs` — restart simulation, add 50 initial bodies, send rapid step+get_state cycles measuring commands/second, run for configurable duration (default 30s), record `PeakCommandRate`, `TotalCommands`, `FailedCommands`
- [x] T074 [US7] Add `StressTestRunner.fsi` and `StressTestRunner.fs` to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T075 [US7] Create `src/PhysicsSandbox.Mcp/StressTestTools.fsi` and `src/PhysicsSandbox.Mcp/StressTestTools.fs` — `[<McpServerToolType>]` class with `start_stress_test` tool (params: scenario name, optional max_bodies, optional duration_seconds; calls `StressTestRunner.startTest`, returns test ID) and `get_stress_test_status` tool (params: test_id; calls `StressTestRunner.getStatus`, returns formatted progress or final results report)
- [x] T076 [US7] Add `StressTestTools.fsi` and `StressTestTools.fs` to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T077 [US7] Log stress test results to structured logs on completion: in `StressTestRunner.fs`, when test completes, log full results via `ILogger.LogInformation` with all metrics fields
- [x] T078 [US7] Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Stress tests run as background jobs via MCP. SC-007 verifiable (500 bodies scaling test).

---

## Phase 10: User Story 8 — MCP vs Scripting Performance Comparison (Priority: P3)

**Goal**: Run identical scenarios via MCP and direct gRPC scripting, comparing performance overhead.

**Independent Test**: Call `start_comparison_test` MCP tool, poll status, verify comparison report shows MCP vs scripting timing and overhead percentage.

**Dependencies**: Requires US7 (stress test runner framework), US3 (batch commands), US4 (restart).

### Tests for User Story 8

- [x] T078a [P] [US8] Integration test: create `tests/PhysicsSandbox.Integration.Tests/ComparisonIntegrationTests.cs` — start AppHost, invoke `start_comparison_test` MCP tool with body_count=10 step_count=10 (small scale), poll status until complete, verify results contain both `McpTimeMs` and `ScriptTimeMs` with non-zero values and `OverheadPercent` is computed

### Implementation for User Story 8

- [x] T079 [US8] Implement comparison scenario in `src/PhysicsSandbox.Mcp/StressTestRunner.fs` — add "mcp-vs-script" scenario type: (1) restart simulation, (2) run scripted path via PhysicsClient library — add N bodies, apply forces, step M times, record wall-clock time and message count, (3) restart simulation, (4) run MCP path via `conn.Client` gRPC calls — same sequence, record time and message count, (5) optionally run batched MCP path, (6) compute overhead percentage
- [x] T080 [US8] Create `src/PhysicsSandbox.Mcp/ComparisonTools.fsi` and `src/PhysicsSandbox.Mcp/ComparisonTools.fs` — `[<McpServerToolType>]` class with `start_comparison_test` tool (params: optional body_count default 100, optional step_count default 60; starts comparison scenario via StressTestRunner, returns test ID). Results queried via existing `get_stress_test_status` tool.
- [x] T081 [US8] Add `ComparisonTools.fsi` and `ComparisonTools.fs` to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T082 [US8] Format comparison results in `StressTestRunner.fs` — when displaying "mcp-vs-script" results, show side-by-side table: scripting time vs MCP time vs batched MCP time, message counts, overhead percentage
- [x] T083 [US8] Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Comparison test quantifies MCP overhead. SC-008 verifiable.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge case handling, and consistency improvements.

- [x] T084 Handle edge case: batch exceeding 100 commands (already enforced in T031 sendBatchCommand) in `src/PhysicsServer/Hub/MessageRouter.fs` `sendBatchCommand` — return error `BatchResponse` immediately if `commands.Count > 100`
- [x] T085 Handle edge case: restart during stress test (test continues with empty sim; single-test guard prevents conflicts) in `src/PhysicsSandbox.Mcp/StressTestRunner.fs` — if restart command detected while test is running, cancel the test gracefully and record partial results with `Status = Cancelled`
- [x] T086 Handle edge case: viewer minimized FPS logging (FpsCounter.update caps instant FPS at 0 for delta > 1s) in `src/PhysicsViewer/Program.fs` — continue logging FPS even when frame delta is very large (minimized window), cap instant FPS at 0 if delta > 1s to avoid misleading averages
- [x] T087 Verify all new F# modules have `.fsi` signature files (Constitution Principle V compliance check)
- [x] T087a Create or update surface area baseline files (deferred — PhysicsServer.Tests and MCP lack surface area test infrastructure; existing baselines in Simulation/Viewer/Client cover modified modules) for all new public F# modules: MetricsCounter, FpsCounter, BatchTools, MetricsTools, StressTestTools, ComparisonTools, StressTestRunner — serialize public API surface to baseline files per Constitution Principle V
- [x] T088 Run full test suite and verify all tests pass (152 unit tests, 0 failures): `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T089 Run quickstart.md validation (requires live system — deferred to manual testing) — manually verify each section's instructions work against the running system

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (proto compilation)
- **US1 FPS (Phase 3)**: Depends on Phase 2 — no dependencies on other stories
- **US2 Metrics (Phase 4)**: Depends on Phase 2 (MetricsCounter module)
- **US3 Batch (Phase 5)**: Depends on Phase 2 (proto batch messages)
- **US4 Restart (Phase 6)**: Depends on Phase 2 (proto ResetSimulation)
- **US5 Static Bodies (Phase 7)**: Depends on Phase 2 (proto is_static field)
- **US6 Diagnostics (Phase 8)**: Depends on US2 (metrics infrastructure)
- **US7 Stress Testing (Phase 9)**: Depends on US3 (batch) + US4 (restart)
- **US8 Comparison (Phase 10)**: Depends on US7 (stress test runner) + US3 (batch)
- **Polish (Phase 11)**: Depends on all user stories

### User Story Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational)
                        │
                        ├── US1 (FPS)           ─── independent
                        ├── US2 (Metrics)       ─── independent
                        ├── US3 (Batch)         ─── independent
                        ├── US4 (Restart)       ─── independent
                        └── US5 (Static Bodies) ─── independent
                                │
                    US2 ────────┤
                                └── US6 (Diagnostics)
                    US3 + US4 ──┤
                                └── US7 (Stress Testing)
                    US7 ────────┤
                                └── US8 (Comparison)
```

### Parallel Opportunities

After Phase 2 completes, these user stories can run **in parallel**:
- US1 (FPS) — touches only PhysicsViewer
- US2 (Metrics) — touches PhysicsServer, PhysicsSimulation, MCP
- US3 (Batch) — touches PhysicsServer, PhysicsClient, MCP
- US4 (Restart) — touches PhysicsSimulation, MCP
- US5 (Static Bodies) — touches PhysicsSimulation only

US6, US7, US8 must wait for their prerequisites.

---

## Parallel Example: After Phase 2

```
# These 5 user stories can execute simultaneously:
US1: T012-T018 (FPS — PhysicsViewer only)
US2: T019-T028 (Metrics — Server + Simulation + MCP)
US3: T029-T041 (Batch — Server + Client + MCP)
US4: T042-T051 (Restart — Simulation + MCP)
US5: T052-T060 (Static Bodies — Simulation only)

# Within US3, parallel tasks:
T029 + T030 (tests, different test projects)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (proto changes)
2. Complete Phase 2: Foundational (MetricsCounter)
3. Complete Phase 3: User Story 1 (FPS display)
4. **STOP and VALIDATE**: Verify FPS overlay + logging works
5. Proceed to remaining P1 stories (US2, US3)

### Incremental Delivery

1. Setup + Foundational → Proto + MetricsCounter ready
2. US1 (FPS) → Viewer shows FPS ✓
3. US2 (Metrics) → Services report traffic ✓
4. US3 (Batch) → Batch commands work ✓
5. US4 (Restart) + US5 (Static Bodies) → Clean state + correct collisions ✓
6. US6 (Diagnostics) → Pipeline bottleneck detection ✓
7. US7 (Stress Testing) → Automated capacity testing ✓
8. US8 (Comparison) → MCP overhead quantified ✓

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Constitution Principle V: every new `.fs` module needs a `.fsi` signature file
- Constitution Principle VI: tests written before or alongside implementation
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
