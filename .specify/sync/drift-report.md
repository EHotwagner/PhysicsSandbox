# Spec Drift Report

Generated: 2026-03-21T08:00:00Z
Project: PhysicsSandbox

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 18 (12 FR + 6 SC) |
| Aligned | 17 (94%) |
| Drifted | 1 (6%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 001-mcp-persistent-service - MCP Persistent Service

#### Aligned

- FR-001: Network socket transport → `Program.fs` uses `MapMcp()` with `ModelContextProtocol.AspNetCore` HTTP/SSE transport
- FR-002: Auto-start with AppHost → `AppHost.cs:19-21` registers MCP with `.WithReference(server).WaitFor(server)`
- FR-003: Multiple concurrent connections, shared state → `GrpcConnection` registered as singleton in `Program.fs`
- FR-004: All 3 streams (state + view + audit) → `GrpcConnection.fs` implements `startStateStream`, `startViewCommandStream`, `startCommandAuditStream`
- FR-005: All simulation commands → `SimulationTools.fs` exposes 10 tools covering all 9 proto command types
- FR-006: All view commands → `ViewTools.fs` exposes 3 tools (set_camera, set_zoom, toggle_wireframe)
- FR-007: 7 body presets → `PresetTools.fs` implements marble, bowling_ball, beach_ball, crate, brick, boulder, die
- FR-008: 5 scene generators → `GeneratorTools.fs` implements random_bodies, stack, row, grid, pyramid
- FR-009: 4 steering tools → `SteeringTools.fs` implements push_body, launch_body, spin_body, stop_body
- FR-010: All 12 commands via PhysicsHub only → No SimulationLink usage in MCP codebase
- FR-011: Status & state query tools → `QueryTools.fs` implements get_state, get_status (incl. 3-stream status)
- FR-012: Graceful disconnection + reconnection → Exponential backoff (1s→10s) in all 3 stream handlers
- SC-001: Persistent for AppHost lifetime → HTTP/SSE transport, singleton, no client-bound exit conditions
- SC-002: Connect/disconnect/reconnect → HTTP transport inherently supports this
- SC-003: 100% command coverage → 10 simulation + 3 view = 13 core tools covering all protocol commands
- SC-005: Any legal state reachable → All 12 command types accessible
- SC-006: State staleness < 2s → Background stream updates every physics frame

#### Drifted

- **US2 Acceptance Scenario 2**: Spec says "it sees the raw command that was sent (command type, parameters, **sender**)" but `CommandEvent` proto message has no sender field. Implementation shows command type and parameters only.
  - Location: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto:179-184`
  - Severity: minor
  - Note: Protocol-level omission. Adding sender info would require modifying the proto message and all callers.

#### Not Implemented

(none)

### Unspecced Code

(none — all new code maps to spec requirements)

## Inter-Spec Conflicts

(none — only one active spec)

## Recommendations

1. **Minor**: Consider adding an optional `sender` field to `CommandEvent` proto message to align with US2 acceptance scenario 2. This is a low-priority enhancement that can be addressed in a follow-up.
