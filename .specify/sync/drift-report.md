# Spec Drift Report

Generated: 2026-03-20
Project: BPSandbox (PhysicsSandbox)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 14 (FR) + 7 (SC) = 21 |
| ✓ Aligned | 19 (90%) |
| ⚠️ Drifted | 2 (10%) |
| ✗ Not Implemented | 0 (0%) |
| 🆕 Unspecced Code | 1 |

## Detailed Findings

### Spec: 001-server-hub - Contracts and Server Hub

#### Aligned ✓

- FR-001: Solution structure with AppHost, Contracts, ServiceDefaults, and PhysicsServer → `PhysicsSandbox.slnx` with 4 src projects + 2 test projects
- FR-002: PhysicsHub service with SendCommand, SendViewCommand, StreamState → `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto:9-18`
- FR-003: SimulationCommand with all 5 variants → `physics_hub.proto:33-41`
- FR-004: ViewCommand with 3 variants → `physics_hub.proto:68-73`
- FR-005: SimulationState with bodies, time, running → `physics_hub.proto:90-94`
- FR-006: Server accepts simulation commands → `src/PhysicsServer/Services/PhysicsHubService.fs` SendCommand method
- FR-007: Server accepts view commands → `src/PhysicsServer/Services/PhysicsHubService.fs` SendViewCommand method
- FR-008: Server fans out state to subscribers → `src/PhysicsServer/Hub/MessageRouter.fs` publishState function
- FR-009: Graceful handling when no downstream connected → `src/PhysicsServer/Hub/MessageRouter.fs` submitCommand returns success ack even when no simulation
- FR-010: AppHost registers server hub → `src/PhysicsSandbox.AppHost/AppHost.cs:3`
- FR-011: ServiceDefaults provides health, telemetry, discovery, resilience → `src/PhysicsSandbox.ServiceDefaults/Extensions.cs`
- FR-012: Server references ServiceDefaults → `src/PhysicsServer/PhysicsServer.fsproj` (ProjectReference) and `Program.fs` (`AddServiceDefaults()`)
- FR-013: Cache latest state for late joiners → `src/PhysicsServer/Hub/StateCache.fs` + `PhysicsHubService.fs` StreamState sends cached state first
- FR-014: Single simulation enforcement → `src/PhysicsServer/Services/SimulationLinkService.fs` rejects with ALREADY_EXISTS
- SC-001: Build and run with single command → `dotnet build PhysicsSandbox.slnx` succeeds
- SC-003: Command acknowledgment → Integration test `SendCommand_ReturnsSuccessAck` passes
- SC-005: Health check endpoints → ServiceDefaults maps `/health` and `/alive`
- SC-006: Contracts buildable by reference → Contracts project builds standalone, PhysicsServer references it
- SC-007: No errors for commands without downstream → Unit test `SubmitCommand succeeds with no simulation connected` passes

#### Drifted ⚠️

- SC-002: Spec says "orchestration dashboard displays the server hub as a healthy, running resource"
  - Location: `src/PhysicsSandbox.ServiceDefaults/Extensions.cs:113`
  - Actual: Health endpoints only mapped in Development environment (`if (app.Environment.IsDevelopment())`). In non-dev environments, health endpoints are not exposed — dashboard may not show health status.
  - Severity: minor (this is standard Aspire template behavior; tests run in Development)

- FR-002: Spec says "define a PhysicsHub service" only; does not mention SimulationLink service
  - Location: `physics_hub.proto:22-28`
  - Actual: Code defines BOTH PhysicsHub and SimulationLink services. SimulationLink was added during planning to handle simulation-to-server communication (fulfills FR-008).
  - Severity: minor (implementation is correct; spec should be updated to mention SimulationLink explicitly)

#### Not Implemented ✗

(None — all requirements implemented)

### Unspecced Code 🆕

| Feature | Location | Lines | Suggested Spec |
|---------|----------|-------|----------------|
| Kestrel HTTP/2 protocol config | `AppHost.cs:4` (env var) | 1 | 001-server-hub (infrastructure detail, no spec needed) |

## Inter-Spec Conflicts

None — only one spec exists.

## Recommendations

1. **Minor**: Update FR-002 in spec.md to explicitly mention the `SimulationLink` service alongside `PhysicsHub`. The SimulationLink was a planning-phase addition that correctly fulfills FR-008 but is not named in the spec.
2. **No action needed**: SC-002 health check visibility in Development-only is standard Aspire behavior. The dashboard works correctly in the Development profile where testing occurs.
