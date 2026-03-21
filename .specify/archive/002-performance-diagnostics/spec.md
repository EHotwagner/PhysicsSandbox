# Feature Specification: Performance Diagnostics & Stress Testing

**Feature Branch**: `002-performance-diagnostics`
**Created**: 2026-03-21
**Status**: Completed
**Input**: User description: "this feature is performance focused. show and log fps in the viewer. measure and log how many messages and traffic each service handles. create batch commands for simulation/ui. create restart simulation command. all static bodies can collide. diagnose where problems arise. stress test. compare mcp vs scripting performance of the same tests..."

## Clarifications

### Session 2026-03-21

- Q: How should diagnostics and stress test results be accessed? → A: Both — logged to structured logs for historical review AND queryable via dedicated MCP tools for live interaction.
- Q: Should stress tests run as blocking operations or background jobs? → A: Background — MCP tool starts the test and returns immediately; a separate tool polls progress and retrieves results.
- Q: Should the restart command also reset accumulated performance metrics? → A: Simulation only — restart clears bodies and physics state, but performance metrics and diagnostics counters persist across restarts.
- Q: Should batch commands be exposed at the gRPC level, MCP level, or both? → A: Both — batch RPC at the gRPC protocol level, plus an MCP batch tool that wraps the gRPC batch call. Enables fair MCP-vs-scripting comparison.
- Q: Should service metrics be queryable on-demand or only passively logged? → A: Both — periodic logging for historical review, plus on-demand MCP tool to query current metric counters live.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Viewer FPS Display & Logging (Priority: P1)

As a developer running the physics sandbox, I want to see real-time frames-per-second in the viewer window and have FPS data logged over time, so I can immediately identify rendering performance issues and track trends across sessions.

**Why this priority**: FPS is the most visible and immediate performance indicator. Without it, the developer has no baseline for understanding whether the system is performing well or degrading.

**Independent Test**: Can be fully tested by launching the viewer, observing the FPS overlay on screen, and verifying that FPS values are written to logs. Delivers immediate visibility into rendering performance.

**Acceptance Scenarios**:

1. **Given** the viewer is running, **When** the simulation is active, **Then** the current FPS is displayed as an overlay in the viewer window, updating at least once per second.
2. **Given** the viewer is running, **When** 10 seconds have elapsed, **Then** FPS samples have been recorded to the logging system with timestamps.
3. **Given** the viewer is running under heavy load, **When** FPS drops below a configurable threshold, **Then** a warning is emitted in the logs.

---

### User Story 2 - Service Message & Traffic Metrics (Priority: P1)

As a developer, I want each service (PhysicsServer, PhysicsSimulation, PhysicsViewer, MCP server) to measure and log the number of messages processed and the volume of data transferred, so I can identify which service is a bottleneck or consuming excessive bandwidth.

**Why this priority**: Understanding inter-service communication volume is essential for diagnosing performance issues in a distributed system. This is foundational data needed before stress testing can be meaningful.

**Independent Test**: Can be tested by running the system, performing operations, and verifying that each service emits periodic metric summaries showing message counts and data volumes.

**Acceptance Scenarios**:

1. **Given** the system is running with all services active, **When** messages are exchanged between services, **Then** each service logs the count of messages sent and received per reporting interval.
2. **Given** the system is running, **When** a reporting interval elapses, **Then** each service logs the total bytes sent and received during that interval.
3. **Given** the system has been running for multiple intervals, **When** a developer reviews the logs, **Then** they can see trends in message volume and traffic over time.

---

### User Story 3 - Batch Commands for Simulation & UI (Priority: P1)

As a developer or AI assistant, I want to send multiple simulation commands (add bodies, apply forces, etc.) and UI commands (camera moves, wireframe toggles) in a single batch request, so I can reduce round-trip overhead and execute complex scenarios efficiently.

**Why this priority**: Individual command round-trips are a major source of latency, especially when setting up complex scenes. Batch commands directly address performance and are a prerequisite for meaningful stress testing.

**Independent Test**: Can be tested by submitting a batch of 10+ commands in a single request and verifying all execute correctly, compared against sending them individually.

**Acceptance Scenarios**:

1. **Given** a batch of simulation commands is submitted, **When** the batch is processed, **Then** all commands in the batch execute in order and the results are returned together.
2. **Given** a batch contains an invalid command among valid ones, **When** the batch is processed, **Then** valid commands still execute and the invalid command's error is reported in the batch response.
3. **Given** a batch of 50 commands is submitted, **When** compared to sending 50 individual commands, **Then** the batch completes in significantly less total time.

---

### User Story 4 - Restart Simulation Command (Priority: P2)

As a developer or AI assistant, I want a single command to reset the simulation to its initial empty state (clearing all bodies, resetting physics time), so I can quickly start fresh without restarting the entire application.

**Why this priority**: Essential for repeatable stress testing and benchmarking. Without a restart command, each test run requires manual cleanup or full application restart.

**Independent Test**: Can be tested by adding several bodies to the simulation, issuing the restart command, and verifying the simulation returns to an empty, clean state.

**Acceptance Scenarios**:

1. **Given** a simulation with multiple bodies in motion, **When** the restart command is issued, **Then** all bodies are removed and the simulation state is reset to empty.
2. **Given** a simulation that has been running for some time, **When** the restart command is issued, **Then** the simulation clock resets and all accumulated state is cleared.
3. **Given** the viewer is connected, **When** the simulation restarts, **Then** the viewer reflects the cleared state without requiring reconnection.

---

### User Story 5 - Static Body Collision (Priority: P2)

As a developer, I want all static bodies (planes, static boxes, etc.) to participate in collision detection with dynamic bodies, so that the simulation accurately represents physical boundaries and surfaces.

**Why this priority**: Correct collision behavior is a prerequisite for valid stress test results. If static bodies don't collide properly, benchmark results are unreliable.

**Independent Test**: Can be tested by placing a dynamic body above a static body and verifying it collides and rests on the surface rather than passing through.

**Acceptance Scenarios**:

1. **Given** a static body exists in the simulation, **When** a dynamic body moves toward it, **Then** a collision occurs and the dynamic body responds physically.
2. **Given** multiple static bodies exist at different positions, **When** dynamic bodies interact with them, **Then** all static bodies act as solid collision surfaces.

---

### User Story 6 - Performance Diagnostics & Bottleneck Detection (Priority: P2)

As a developer, I want the system to identify and report where performance bottlenecks occur across the service pipeline (simulation tick time, serialization, network transfer, viewer rendering), so I can focus optimization efforts on the right component.

**Why this priority**: Raw metrics are only useful if they can be correlated to identify the actual source of slowdowns. This story turns data into actionable diagnostics.

**Independent Test**: Can be tested by artificially loading one service and verifying the diagnostics correctly identify which stage of the pipeline is the bottleneck.

**Acceptance Scenarios**:

1. **Given** the system is under load, **When** a diagnostics report is requested, **Then** a breakdown of time spent in each pipeline stage is provided.
2. **Given** the simulation is the bottleneck, **When** diagnostics are reviewed, **Then** the simulation stage shows disproportionately high latency compared to other stages.
3. **Given** the system is running normally, **When** diagnostics are reviewed, **Then** all stages show healthy performance within expected ranges.

---

### User Story 7 - Stress Testing (Priority: P2)

As a developer, I want predefined stress test scenarios that push the system to its limits (large body counts, rapid command throughput, sustained load), so I can measure maximum capacity and identify failure modes.

**Why this priority**: Stress testing validates that the system can handle demanding workloads and helps establish performance baselines and limits.

**Independent Test**: Can be tested by running a stress test scenario and observing the system's behavior, metrics, and any degradation or failures.

**Acceptance Scenarios**:

1. **Given** a stress test scenario is initiated, **When** the start command is issued, **Then** the test begins running in the background and the command returns immediately with a test identifier. **When** the test runs to completion, **Then** a summary report is available via a progress/results query showing peak metrics, degradation points, and any failures.
2. **Given** a stress test adds bodies incrementally, **When** the system reaches its capacity, **Then** the test records the body count at which performance degraded below acceptable thresholds.
3. **Given** a stress test sends rapid-fire commands, **When** the throughput limit is reached, **Then** the test records the maximum sustainable command rate.

---

### User Story 8 - MCP vs Scripting Performance Comparison (Priority: P3)

As a developer, I want to run identical test scenarios through both the MCP interface and direct scripting (gRPC client), measuring and comparing the performance of each approach, so I can understand the overhead introduced by the MCP layer.

**Why this priority**: Understanding MCP overhead informs architectural decisions about when to use MCP vs direct scripting for performance-critical operations. Lower priority because it depends on the other metrics and stress testing infrastructure being in place.

**Independent Test**: Can be tested by running the same predefined scenario (e.g., add 100 bodies, apply forces, step N times) via both MCP and direct script, then comparing timing results.

**Acceptance Scenarios**:

1. **Given** identical test scenarios exist for both MCP and scripting, **When** both are executed, **Then** timing results for each are recorded and a comparison summary is produced.
2. **Given** a comparison test has completed, **When** the results are reviewed, **Then** the overhead of the MCP layer (in time and message count) is clearly quantified.
3. **Given** batch commands are available, **When** the comparison includes batched vs unbatched MCP commands, **Then** the impact of batching on MCP performance is measured.

---

### Edge Cases

- What happens when FPS drops to zero or the viewer window is minimized — are metrics still logged?
- How does the system behave when a batch command exceeds the maximum size limit?
- What happens if a restart command is issued while a stress test is actively running?
- How does the system handle metric logging when a service disconnects and reconnects?
- What happens when a stress test causes an out-of-memory condition?
- How are metrics handled when services start at different times (partial system)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Viewer MUST display current FPS as an on-screen overlay, updated at least once per second.
- **FR-002**: Viewer MUST log FPS samples periodically with timestamps to the structured logging system.
- **FR-003**: Each service MUST track and log message counts (sent/received) per configurable reporting interval, and MUST expose current counters via an on-demand MCP query tool.
- **FR-004**: Each service MUST track and log data volume (bytes sent/received) per reporting interval, and MUST expose current values via the same on-demand MCP query tool.
- **FR-005**: System MUST support batch submission of multiple simulation commands in a single request, executing them in order. Batching MUST be available at both the gRPC protocol level (batch RPC) and the MCP tool level (batch tool wrapping the gRPC batch call).
- **FR-006**: System MUST support batch submission of multiple UI/viewer commands in a single request, at both gRPC and MCP levels.
- **FR-007**: Batch responses MUST include per-command results, including errors for individual failed commands.
- **FR-008**: System MUST provide a restart command that clears all bodies and resets simulation state to empty.
- **FR-009**: Restart command MUST NOT require service restarts or reconnections. Restart MUST only clear simulation state (bodies, physics time); performance metrics and diagnostics counters MUST persist across restarts.
- **FR-010**: All static bodies MUST participate in collision detection with dynamic bodies.
- **FR-011**: System MUST provide pipeline diagnostics showing time breakdown across simulation, serialization, transfer, and rendering stages, accessible both via structured logs and on-demand MCP tools.
- **FR-012**: System MUST provide predefined stress test scenarios for body count scaling and command throughput, invocable via MCP tools. Stress tests MUST run as background jobs that return immediately, with a separate MCP tool to poll progress and retrieve results.
- **FR-013**: Stress tests MUST produce summary reports with peak metrics, degradation thresholds, and failure points, returned via the MCP tool response and logged to structured logs for historical review.
- **FR-014**: System MUST support running identical scenarios via MCP and direct scripting for performance comparison.
- **FR-015**: Comparison results MUST quantify the overhead of MCP in terms of time and message count.
- **FR-016**: FPS logging MUST emit a warning when FPS falls below a configurable threshold.

### Key Entities

- **PerformanceMetrics**: Represents a snapshot of performance data for a service at a point in time — includes message counts, data volumes, timing breakdowns.
- **BatchCommand**: A collection of ordered commands submitted as a single unit — contains individual commands and collects per-command results.
- **StressTestScenario**: A predefined test configuration — specifies body counts, command rates, duration, and success thresholds.
- **ComparisonReport**: Results of running the same scenario via different interfaces — captures timing, overhead, and throughput for each approach.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can see live FPS in the viewer at all times during a running simulation.
- **SC-002**: Per-service message count and traffic metrics are available in logs within 30 seconds of system startup.
- **SC-003**: A batch of 50 commands completes at least 2x faster than sending 50 individual commands sequentially.
- **SC-004**: Simulation can be fully restarted to a clean state in under 2 seconds via a single command.
- **SC-005**: All static bodies correctly block dynamic bodies — zero pass-through incidents during normal operation.
- **SC-006**: Diagnostics correctly identify the slowest pipeline stage within 10% accuracy of actual measured time.
- **SC-007**: Stress tests can scale to at least 500 simultaneous bodies while measuring degradation points.
- **SC-008**: MCP vs scripting comparison quantifies overhead with reproducible results (less than 15% variance between runs).

## Assumptions

- Reporting intervals for service metrics default to 10 seconds but are configurable.
- FPS warning threshold defaults to 30 FPS but is configurable.
- Batch command size is limited to 100 commands per batch (reasonable default to prevent resource exhaustion).
- Stress test scenarios are predefined but parameterizable (body count, duration, command rate).
- "Direct scripting" for MCP comparison refers to using the gRPC client library directly (PhysicsClient).
- Static body collision fix applies to all body types that can be created as static (planes approximated as large boxes are already expected to collide; this ensures no static body is excluded from collision detection).
- Metrics are logged via the existing structured logging/telemetry infrastructure visible in the Aspire dashboard.
