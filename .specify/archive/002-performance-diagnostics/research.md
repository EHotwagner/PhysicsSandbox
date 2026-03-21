# Research: Performance Diagnostics & Stress Testing

**Date**: 2026-03-21
**Branch**: `002-performance-diagnostics`

## R1: Batch Command Protocol Design

**Decision**: Add `BatchCommand` and `BatchViewCommand` RPCs to the `PhysicsHub` gRPC service, with corresponding MCP batch tools wrapping them.

**Rationale**: The existing proto has no batch support — generator tools in both MCP and PhysicsClient loop individual `SendCommand` calls. A native gRPC batch RPC eliminates per-command round-trip overhead and enables fair MCP-vs-scripting comparison. The batch RPC accepts a repeated list of commands and returns a repeated list of per-command results.

**Alternatives considered**:
- Client-side batching (loop in MCP tool) — already exists in generators, but doesn't reduce network round-trips
- gRPC streaming for batches — more complex, unnecessary since batches are bounded (≤100 commands)
- Generic `oneof` batch message — rejected in favor of separate simulation/view batch RPCs for type safety

## R2: Restart Simulation Command

**Decision**: Add `ResetSimulation` as a new variant in the `SimulationCommand` oneof. The simulation handles it by clearing all bodies from BepuPhysics2, resetting `SimulationTime` to 0, and sending an empty state update.

**Rationale**: Fits naturally into the existing command routing — no new RPC needed. The server already forwards `SimulationCommand` variants to the simulation via `ConnectSimulation` bidirectional stream. Metrics persist because they live in separate modules (not in `SimulationWorld`).

**Alternatives considered**:
- Dedicated `ResetSimulation` unary RPC — unnecessary complexity; command routing already handles all sim commands
- Kill and restart simulation process — violates FR-009 (no service restarts)

## R3: Static Body Tracking

**Decision**: Track static bodies in `world.Bodies` map with a `IsStatic` flag on `BodyRecord`. Include static bodies in `SimulationState` stream. Add `is_static` field to proto `Body` message.

**Rationale**: Currently static bodies (planes) are added to BepuPhysics2 but not tracked in `world.Bodies`, making them invisible in state and non-removable. Tracking them enables: (1) collision verification in tests, (2) removal via restart, (3) visibility in viewer. Static bodies already collide in Bepu — the issue is only state tracking.

**Alternatives considered**:
- Separate `StaticBodies` map — adds complexity without benefit; single map with flag is simpler
- Keep static bodies untracked — prevents restart from clearing them, blocks FR-010 verification

## R4: FPS Display in Viewer

**Decision**: Use Stride's `GameTime.Elapsed` to calculate FPS (1/deltaTime smoothed), display via existing `DebugTextSystem.Print()`, log periodically via `ILogger`.

**Rationale**: The viewer already uses `DebugTextSystem` for the status overlay (sim time + run state). Adding FPS to this overlay is minimal change. `GameTime.Elapsed` is the standard Stride mechanism for frame timing. Smoothed FPS (exponential moving average) avoids jitter.

**Alternatives considered**:
- Stride profiler API — overkill for a simple FPS counter; profiler is GPU-focused
- Custom high-resolution timer — unnecessary; GameTime already provides sub-millisecond precision

## R5: Service Metrics Architecture

**Decision**: Each service maintains thread-safe counters (message count, byte count) incremented at gRPC send/receive points. Counters are logged periodically via a background timer. The MCP server exposes a `get_metrics` tool that queries counters from all services via a new `GetMetrics` unary RPC on PhysicsHub.

**Rationale**: Metrics must be collected at the service level (each service owns its counters) but queryable centrally (via MCP). A new `GetMetrics` RPC lets the MCP server pull metrics from PhysicsServer, which aggregates its own counters plus forwarded metrics from the simulation. The viewer reports FPS separately via its own metrics.

**Alternatives considered**:
- OpenTelemetry custom metrics export — already configured in ServiceDefaults but requires Aspire dashboard for viewing; doesn't support on-demand MCP query
- Metrics via state stream — pollutes state messages with non-physics data
- Each service exposes its own metrics endpoint — MCP would need to connect to every service

## R6: Pipeline Diagnostics

**Decision**: Instrument timing at key pipeline stages: (1) simulation tick time in PhysicsSimulation, (2) state serialization time in SimulationWorld.buildState, (3) gRPC transfer time (measured as delta between send and receive timestamps), (4) viewer render frame time. Report via `get_diagnostics` MCP tool and periodic structured logging.

**Rationale**: The existing pipeline has zero performance instrumentation. Adding `Stopwatch` measurements at each stage boundary provides the time breakdown needed for bottleneck identification. The MCP tool aggregates all stage timings into a single diagnostics report.

**Alternatives considered**:
- Distributed tracing (OpenTelemetry spans) — good complement but requires Aspire dashboard; doesn't support on-demand MCP query
- Profiler-based approach — too invasive for production-like measurement

## R7: Stress Testing Execution Model

**Decision**: Stress tests run as background tasks in the MCP server process. A `start_stress_test` MCP tool launches the test and returns a test ID. A `get_stress_test_status` tool polls progress and retrieves results. Tests use PhysicsClient library for direct gRPC scripting and MCP tools for MCP-path testing.

**Rationale**: The MCP server already has a gRPC connection to the PhysicsServer and a project reference to PhysicsClient. Running stress tests in-process avoids spinning up a separate test runner. Background execution prevents MCP tool timeout. The PhysicsClient reference enables direct scripting comparison.

**Alternatives considered**:
- Separate stress test console app — adds a new project; MCP already has all dependencies
- Integration test project — good for CI but doesn't support interactive MCP-triggered execution
- Blocking MCP tool with long timeout — MCP tools shouldn't block for minutes

## R8: MCP vs Scripting Comparison

**Decision**: Implement comparison as a specialized stress test scenario. The test runs the same sequence (e.g., add N bodies, apply forces, step M times) twice: once via MCP tools (HTTP/SSE → MCP server → gRPC) and once via PhysicsClient library (direct gRPC). Wall-clock time, message count, and bytes transferred are recorded for each path.

**Rationale**: The comparison must use identical scenarios to isolate MCP overhead. PhysicsClient is already referenced by the MCP project. Running both paths from the same process controls for network variability. A restart command between runs ensures clean state.

**Alternatives considered**:
- External benchmark harness (BenchmarkDotNet) — too heavyweight for interactive use; better for microbenchmarks
- Separate processes for each path — introduces network/scheduling variance that obscures MCP overhead measurement
