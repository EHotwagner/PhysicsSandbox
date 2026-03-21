# Implementation Plan: Performance Diagnostics & Stress Testing

**Branch**: `002-performance-diagnostics` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-performance-diagnostics/spec.md`

## Summary

Add comprehensive performance observability and stress testing to PhysicsSandbox. This includes: FPS display in the viewer, per-service message/traffic metrics, batch command support at both gRPC and MCP levels, simulation restart, static body collision tracking, pipeline diagnostics, stress testing framework, and MCP-vs-scripting performance comparison. All metrics are dual-access: logged to structured logs for historical review AND queryable on-demand via MCP tools.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, MCP, client), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts)
**Primary Dependencies**: ModelContextProtocol.AspNetCore 1.1.*, Grpc.Net.Client 2.*, Google.Protobuf 3.*, BepuFSharp 0.1.0, Stride.CommunityToolkit 1.0.0-preview.62, Spectre.Console
**Storage**: In-memory (counters, stress test state, command logs)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x, dotnet test with StrideCompilerSkipBuild=true
**Target Platform**: Linux with GPU passthrough (Stride3D viewer)
**Project Type**: Distributed microservice system (gRPC + MCP + 3D viewer)
**Performance Goals**: 60 FPS viewer, batch 50 commands ≥2x faster than sequential, restart <2s, 500 simultaneous bodies
**Constraints**: Single simulation connection, bounded channels (100 items), batch ≤100 commands
**Scale/Scope**: 4 services + MCP server, ~15 new/modified files, 6 new MCP tools, 3 new gRPC RPCs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Each service maintains own metrics counters. No shared mutable state. Communication via gRPC only. |
| II. Contract-First | PASS | Proto changes defined in `contracts/proto-changes.md` before implementation. All new RPCs (batch, metrics) specified. |
| III. Shared Nothing | PASS | Only `PhysicsSandbox.Shared.Contracts` shared. MCP references PhysicsClient as library (existing pattern). |
| IV. Spec-First Delivery | PASS | Spec, clarifications, and plan completed before implementation. |
| V. Compiler-Enforced Contracts | PASS | All new F# modules require `.fsi` signature files. New modules: MetricsCounter, StressTestRunner, BatchHandler, DiagnosticsCollector. |
| VI. Test Evidence | PASS | Integration tests for batch RPCs, restart, static collision, metrics. Unit tests for metrics counters, stress test scenarios. |
| VII. Observability | PASS | This feature IS observability. All metrics logged via structured logging + queryable via MCP. |

**Post-Phase 1 Re-check**: All principles remain satisfied. No violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/002-performance-diagnostics/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: research decisions
├── data-model.md        # Phase 1: entity definitions
├── quickstart.md        # Phase 1: getting started guide
├── contracts/
│   └── proto-changes.md # Phase 1: gRPC contract changes
└── tasks.md             # Phase 2: task breakdown (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── Protos/
│       └── physics_hub.proto          # MODIFIED: batch, metrics, reset messages + RPCs
│
├── PhysicsServer/
│   ├── Hub/
│   │   ├── MessageRouter.fs/.fsi      # MODIFIED: batch routing, metrics counters
│   │   └── MetricsCounter.fs/.fsi     # NEW: thread-safe service metrics tracking
│   └── Services/
│       └── PhysicsHubService.fs/.fsi  # MODIFIED: batch + metrics RPC handlers
│
├── PhysicsSimulation/
│   ├── World/
│   │   └── SimulationWorld.fs/.fsi    # MODIFIED: static body tracking, reset, timing
│   └── Client/
│       └── SimulationClient.fs        # MODIFIED: metrics counters, timing instrumentation
│
├── PhysicsViewer/
│   ├── Program.fs                     # MODIFIED: FPS calculation + display
│   └── Rendering/
│       └── FpsCounter.fs/.fsi         # NEW: smoothed FPS calculation + logging
│
├── PhysicsSandbox.Mcp/
│   ├── BatchTools.fs/.fsi             # NEW: batch_commands, batch_view_commands MCP tools
│   ├── MetricsTools.fs/.fsi           # NEW: get_metrics, get_diagnostics MCP tools
│   ├── StressTestTools.fs/.fsi        # NEW: start_stress_test, get_stress_test_status MCP tools
│   ├── ComparisonTools.fs/.fsi        # NEW: start_comparison_test MCP tool
│   ├── SimulationTools.fs             # MODIFIED: add restart_simulation tool
│   └── GrpcConnection.fs/.fsi        # MODIFIED: expose batch + metrics RPC calls
│
└── PhysicsClient/
    └── Commands/
        └── SimulationCommands.fs/.fsi # MODIFIED: add reset, batchCommands, getMetrics

tests/
├── PhysicsServer.Tests/
│   └── BatchAndMetricsTests.fs        # NEW: unit tests for batch routing + metrics
├── PhysicsSimulation.Tests/
│   └── ResetAndStaticTests.fs         # NEW: unit tests for reset + static body tracking
├── PhysicsViewer.Tests/
│   └── FpsCounterTests.fs             # NEW: unit tests for FPS calculation
├── PhysicsClient.Tests/
│   └── BatchCommandTests.fs           # NEW: unit tests for batch client functions
└── PhysicsSandbox.Integration.Tests/
    ├── BatchIntegrationTests.cs       # NEW: batch RPC end-to-end tests
    ├── RestartIntegrationTests.cs     # NEW: restart command end-to-end tests
    ├── MetricsIntegrationTests.cs     # NEW: metrics collection end-to-end tests
    └── StaticBodyTests.cs             # NEW: static body collision verification
```

**Structure Decision**: Extends existing multi-project microservice structure. No new projects needed — all changes are additions to existing projects. New F# modules follow existing patterns (`.fs` + `.fsi` pairs). New MCP tools follow existing tool file convention (`*Tools.fs`).

## Complexity Tracking

> No constitution violations. No complexity justifications needed.

## Design Decisions

### D1: Metrics Counter Architecture

Each service creates a `MetricsCounter` module that wraps `System.Threading.Interlocked` operations for thread-safe counter increments. The counter tracks:
- Messages sent/received (incremented in gRPC send/receive wrappers)
- Bytes sent/received (estimated from proto message serialized size)

PhysicsServer aggregates metrics from connected simulation via the `GetMetrics` RPC response. The MCP server's `get_metrics` tool calls this RPC and appends its own local counters.

### D2: Batch Processing in MessageRouter

`SendBatchCommand` iterates commands in the `BatchSimulationRequest`, calling the existing `submitCommand` logic for each. Results are collected in a `BatchResponse`. This reuses all existing validation and routing — batch is a thin wrapper over sequential command submission on the server side. The performance gain comes from eliminating per-command network round-trips (client sends one request instead of N).

### D3: FPS Counter Design

`FpsCounter` uses exponential moving average (EMA) with α=0.1 for smooth display:
```
smoothedFps = α * instantFps + (1 - α) * smoothedFps
```
Logged every 10 seconds (configurable). Warning emitted when smoothed FPS drops below threshold (default 30).

### D4: Stress Test Scenarios

Two predefined scenarios:
1. **body-scaling**: Incrementally adds bodies (10 per batch) until FPS drops below threshold or max count reached. Measures degradation point.
2. **command-throughput**: Sends rapid-fire commands (step + get_state cycles) measuring max sustainable command rate.

Both use the restart command between runs for clean state. Results stored in-memory in the MCP server process.

### D5: Static Body State Tracking

`BodyRecord` gains `IsStatic: bool` field. Static bodies are added to `world.Bodies` with `IsStatic = true`. `buildState` includes them in `SimulationState` with `is_static = true`. `removeBody` can now remove static bodies. `resetSimulation` iterates all bodies (including static) and removes them from BepuPhysics2.

### D6: Pipeline Diagnostics Flow

```
PhysicsSimulation                  PhysicsServer              PhysicsViewer
┌──────────────────┐              ┌──────────────┐           ┌──────────────┐
│ tick_ms (Stopwatch)│             │              │           │ render_ms    │
│ serialize_ms     │──state──────→│ transfer_ms  │──state──→│ (GameTime)   │
│                  │              │ (delta)      │           │              │
└──────────────────┘              └──────────────┘           └──────────────┘
                                        │
                                        ▼
                                  GetMetrics RPC
                                  returns all timings
```

Simulation reports `tick_ms` and `serialize_ms` via its metrics. Server calculates `transfer_ms` as delta between state receive and simulation send timestamps. Viewer reports `render_ms` (frame time). All aggregated in `GetMetrics` response.

## New .fsi Signature Contracts

### MetricsCounter.fsi (PhysicsServer)
```fsharp
module PhysicsServer.Hub.MetricsCounter
type MetricsState
val create : serviceName:string -> MetricsState
val incrementSent : count:int -> bytes:int64 -> MetricsState -> unit
val incrementReceived : count:int -> bytes:int64 -> MetricsState -> unit
val snapshot : MetricsState -> ServiceMetricsReport
val startPeriodicLogging : intervalSeconds:int -> logger:ILogger -> MetricsState -> IDisposable
```

### FpsCounter.fsi (PhysicsViewer)
```fsharp
module PhysicsViewer.Rendering.FpsCounter
type FpsState
val create : warningThreshold:float32 -> FpsState
val update : deltaSeconds:float32 -> FpsState -> float32  // returns smoothed FPS
val shouldLog : intervalSeconds:float32 -> FpsState -> bool
val currentFps : FpsState -> float32
```

### BatchTools.fsi (MCP)
```fsharp
module PhysicsSandbox.Mcp.BatchTools
open PhysicsSandbox.Mcp.GrpcConnection
[<McpServerToolType>]
type BatchTools =
    [<McpServerTool>] static member batch_commands : conn:GrpcConnection * commands:string -> Task<string>
    [<McpServerTool>] static member batch_view_commands : conn:GrpcConnection * commands:string -> Task<string>
```

### StressTestTools.fsi (MCP)
```fsharp
module PhysicsSandbox.Mcp.StressTestTools
open PhysicsSandbox.Mcp.GrpcConnection
[<McpServerToolType>]
type StressTestTools =
    [<McpServerTool>] static member start_stress_test : conn:GrpcConnection * scenario:string * ?max_bodies:int * ?duration_seconds:int -> Task<string>
    [<McpServerTool>] static member get_stress_test_status : conn:GrpcConnection * test_id:string -> Task<string>
```
