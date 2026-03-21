# Spec Drift Report

Generated: 2026-03-21T12:00:00Z
Project: PhysicsSandbox

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 2 |
| Requirements Checked | 28 |
| ✓ Aligned | 27 (96%) |
| ⚠️ Drifted | 1 (4%) |
| ✗ Not Implemented | 0 (0%) |
| 🆕 Unspecced Code | 0 |

## Detailed Findings

### Spec: 001-mcp-persistent-service - MCP Persistent Service

**Status: Completed — All 12 requirements aligned.**

#### Aligned ✓
- FR-001: Network socket transport → `src/PhysicsSandbox.Mcp/Program.fs:29` (HTTP/SSE via ModelContextProtocol.AspNetCore)
- FR-002: Aspire AppHost integration → `src/PhysicsSandbox.AppHost/AppHost.cs:19-21`
- FR-003: Multiple concurrent connections, shared state → `src/PhysicsSandbox.Mcp/GrpcConnection.fs` (singleton DI)
- FR-004: Full message visibility (state, view, audit streams) → `src/PhysicsSandbox.Mcp/GrpcConnection.fs:47-144`
- FR-005: All simulation command types → `src/PhysicsSandbox.Mcp/SimulationTools.fs:29-136` (10 commands including restart)
- FR-006: All view command types → `src/PhysicsSandbox.Mcp/ViewTools.fs:20-47` (camera, zoom, wireframe)
- FR-007: Body presets (7 types) → `src/PhysicsSandbox.Mcp/PresetTools.fs:16-107`
- FR-008: Scene generators (5 types) → `src/PhysicsSandbox.Mcp/GeneratorTools.fs:16-130`
- FR-009: Steering tools (4 types) → `src/PhysicsSandbox.Mcp/SteeringTools.fs:18-88`
- FR-010: All 12 command types (9 sim + 3 view) → SimulationTools.fs + ViewTools.fs
- FR-011: Query tools (state + status) → `src/PhysicsSandbox.Mcp/QueryTools.fs:37-57`
- FR-012: Graceful disconnection handling → `src/PhysicsSandbox.Mcp/GrpcConnection.fs` (exponential backoff reconnection)

#### Drifted ⚠️
None

#### Not Implemented ✗
None

---

### Spec: 002-performance-diagnostics - Performance Diagnostics & Stress Testing

**Status: In Progress — 15 of 16 requirements aligned, 1 drifted.**

#### Aligned ✓
- FR-001: FPS on-screen overlay → `src/PhysicsViewer/Rendering/FpsCounter.fs:18-26`, `src/PhysicsViewer/Program.fs:175,187`
- FR-002: Periodic FPS logging with timestamps → `src/PhysicsViewer/Program.fs:176-180` (10-second interval)
- FR-003: Per-service message count tracking + MCP tool → `src/PhysicsServer/Hub/MetricsCounter.fs:25-31`, `src/PhysicsSandbox.Mcp/MetricsTools.fs:13-44`
- FR-004: Per-service data volume tracking + MCP tool → `src/PhysicsServer/Hub/MetricsCounter.fs:13-16`, bytes tracked via `CalculateSize()`
- FR-005: Batch simulation commands (gRPC + MCP) → `physics_hub.proto:30`, `src/PhysicsSandbox.Mcp/BatchTools.fs:121-136`
- FR-006: Batch view commands (gRPC + MCP) → `physics_hub.proto:31`, `src/PhysicsSandbox.Mcp/BatchTools.fs:138-153`
- FR-007: Per-command batch results with errors → `physics_hub.proto:209-218`, `src/PhysicsServer/Hub/MessageRouter.fs:145-147`
- FR-008: Restart command → `src/PhysicsSimulation/World/SimulationWorld.fs:252-261`
- FR-009: Restart preserves metrics → MetricsCounter module-level, never reset by restart
- FR-010: Static body collision → `src/PhysicsSimulation/World/SimulationWorld.fs:144-190`, static bodies tracked with `is_static=true`
- FR-012: Stress test scenarios as background jobs → `src/PhysicsSandbox.Mcp/StressTestRunner.fs:59-365`, `src/PhysicsSandbox.Mcp/StressTestTools.fs`
- FR-013: Stress test summary reports → `src/PhysicsSandbox.Mcp/StressTestRunner.fs:30-39,372-417`
- FR-014: MCP vs scripting comparison → `src/PhysicsSandbox.Mcp/StressTestRunner.fs:207-319`
- FR-015: Overhead quantification (time + messages) → `src/PhysicsSandbox.Mcp/StressTestRunner.fs:22-28,289-309`
- FR-016: FPS warning below threshold → `src/PhysicsViewer/Rendering/FpsCounter.fs:37-38`, `src/PhysicsViewer/Program.fs:177-178`

#### Drifted ⚠️
- FR-011: Spec says "pipeline diagnostics showing time breakdown across simulation, serialization, transfer, and rendering stages" but viewer render time is always 0.0
  - Location: `src/PhysicsServer/Services/PhysicsHubService.fs:74-75` — `ViewerRenderMs` hardcoded to 0.0
  - Severity: moderate
  - Detail: Simulation tick time, serialization time, and gRPC transfer time are measured correctly. However, the viewer does not report its render time back to the server, so the diagnostics pipeline breakdown is incomplete. The viewer has access to frame timing via `GameTime` but does not send it.

#### Not Implemented ✗
None

---

## Inter-Spec Conflicts

None detected. The two specs are independent — 001 provides the MCP infrastructure that 002 extends with performance tooling.

## Recommendations

1. **FR-011 viewer render timing**: Add a mechanism for the viewer to report frame render time to the server (e.g., a periodic metrics RPC or piggybacked on existing state responses) so pipeline diagnostics can show a complete breakdown across all stages.
