# Data Model: Performance Diagnostics & Stress Testing

**Date**: 2026-03-21
**Branch**: `002-performance-diagnostics`

## Entities

### ServiceMetrics

Per-service performance counters, maintained in-memory.

| Field | Type | Description |
|-------|------|-------------|
| ServiceName | string | Service identifier (e.g., "PhysicsServer", "PhysicsSimulation") |
| MessagesSent | int64 | Cumulative count of messages sent |
| MessagesReceived | int64 | Cumulative count of messages received |
| BytesSent | int64 | Cumulative bytes sent |
| BytesReceived | int64 | Cumulative bytes received |
| LastReportTime | DateTime | Timestamp of last periodic log report |
| ReportIntervalSeconds | int | Configurable reporting interval (default 10) |

**Lifecycle**: Created on service startup. Counters increment monotonically. Persist across simulation restarts. Reset only on service restart.

### PipelineTimings

Timing breakdown across pipeline stages, captured per measurement cycle.

| Field | Type | Description |
|-------|------|-------------|
| SimulationTickMs | float | Time for one physics step (BepuPhysics2 tick) |
| StateSerializationMs | float | Time to build SimulationState proto from world |
| GrpcTransferMs | float | Estimated transfer time (send-to-receive delta) |
| ViewerRenderMs | float | Frame render time in viewer |
| TotalPipelineMs | float | End-to-end latency |
| Timestamp | DateTime | When this measurement was taken |

**Lifecycle**: Captured continuously during simulation run. Logged periodically. Latest snapshot queryable via MCP.

### BatchRequest / BatchResponse

Batch command submission and results.

| Field | Type | Description |
|-------|------|-------------|
| Commands | SimulationCommand[] | Ordered list of commands to execute |
| Results | CommandResult[] | Per-command results (success/failure + message) |
| TotalTimeMs | float | Wall-clock time for entire batch |

**State transitions**: Submitted → Processing → Complete

### StressTestRun

Tracks a running or completed stress test.

| Field | Type | Description |
|-------|------|-------------|
| TestId | string | Unique identifier (e.g., "stress-001") |
| ScenarioName | string | Which predefined scenario (e.g., "body-scaling", "command-throughput") |
| Status | enum | Pending, Running, Complete, Failed, Cancelled |
| StartTime | DateTime | When test began |
| EndTime | DateTime? | When test completed (null if running) |
| Parameters | record | Scenario-specific params (body count, duration, command rate) |
| Progress | float | 0.0 to 1.0 completion estimate |
| Results | StressTestResults? | Final results (null until complete) |

**State transitions**: Pending → Running → Complete/Failed/Cancelled

### StressTestResults

Summary of a completed stress test.

| Field | Type | Description |
|-------|------|-------------|
| PeakBodyCount | int | Maximum simultaneous bodies during test |
| DegradationBodyCount | int? | Body count where FPS dropped below threshold |
| PeakCommandRate | float | Maximum commands/second sustained |
| AverageFps | float | Mean FPS during test |
| MinFps | float | Lowest FPS recorded |
| TotalCommands | int | Total commands executed |
| FailedCommands | int | Commands that returned errors |
| ErrorMessages | string[] | Distinct error messages encountered |

### ComparisonResult

MCP vs scripting comparison data.

| Field | Type | Description |
|-------|------|-------------|
| ScenarioName | string | What was tested |
| McpTimeMs | float | Wall-clock time via MCP path |
| ScriptTimeMs | float | Wall-clock time via direct gRPC |
| McpMessageCount | int | Total gRPC messages via MCP path |
| ScriptMessageCount | int | Total gRPC messages via direct path |
| OverheadPercent | float | (McpTime - ScriptTime) / ScriptTime * 100 |
| BatchedMcpTimeMs | float? | Time with batch MCP tool (if applicable) |

## Relationships

```
StressTestRun 1──* StressTestResults (has results when complete)
StressTestRun 1──* ComparisonResult (comparison tests produce these)
ServiceMetrics *──1 Service (one per running service)
PipelineTimings *──1 MeasurementCycle (captured each reporting interval)
BatchRequest 1──* SimulationCommand (contains ordered commands)
BatchResponse 1──* CommandResult (per-command outcomes)
```

## Key Constraints

- StressTestRun.TestId must be unique across all tests in a session
- Only one stress test may run at a time (sequential execution)
- BatchRequest limited to 100 commands maximum
- ServiceMetrics counters are monotonically increasing (never decrease)
- PipelineTimings are point-in-time snapshots, not aggregated
