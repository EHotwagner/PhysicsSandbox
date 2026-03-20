# Drift Resolution Proposals

Generated: 2026-03-20
Based on: drift-report from 2026-03-20

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 2 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 001-server-hub/FR-002

**Direction**: BACKFILL

**Current State**:
- Spec says: "The shared contracts project MUST define a `PhysicsHub` service with methods for sending simulation commands, sending view commands, and streaming simulation state."
- Code does: Defines both `PhysicsHub` (client/viewer-facing) AND `SimulationLink` (simulation-facing) services. SimulationLink provides bidirectional streaming for the simulation to push state and receive commands.

**Proposed Resolution**:

Update FR-002 to:

> **FR-002**: The shared contracts project MUST define a `PhysicsHub` service with methods for sending simulation commands, sending view commands, and streaming simulation state; and a `SimulationLink` service with a bidirectional streaming method for the simulation to push state and receive commands.

**Rationale**: The SimulationLink service was a deliberate design decision made during the planning phase (documented in research.md R-001 and R-002). It cleanly separates client-facing and simulation-facing interfaces. The spec's FR-008 ("accept a simulation state stream") implicitly requires this interface — SimulationLink is how that requirement is fulfilled. The code is authoritative; the spec should be updated to match.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: 001-server-hub/SC-002

**Direction**: BACKFILL

**Current State**:
- Spec says: "The orchestration dashboard is accessible and displays the server hub as a healthy, running resource."
- Code does: Health endpoints (`/health`, `/alive`) are only mapped when `app.Environment.IsDevelopment()` is true. This is standard Aspire ServiceDefaults template behavior.

**Proposed Resolution**:

Update SC-002 to:

> **SC-002**: The orchestration dashboard is accessible and displays the server hub as a healthy, running resource in Development mode.

**Rationale**: The Development-only health endpoint mapping is an intentional security practice from the Aspire template — exposing health check endpoints in non-development environments has security implications (as noted in the template's own comments). Since this is a developer sandbox that always runs in Development mode, the criterion is met in practice. The spec should acknowledge this scope to avoid false drift.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: Unspecced — Kestrel HTTP/2 Configuration

**Direction**: BACKFILL (no spec change needed)

**Current State**:
- Code: AppHost sets `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` on the server resource.
- Spec: No mention of protocol configuration.

**Proposed Resolution**:

No spec update needed. This is an infrastructure detail required for gRPC to function over plain HTTP endpoints. It is an implementation concern, not a functional requirement. The spec correctly focuses on the WHAT (gRPC communication) rather than HOW (HTTP/2 protocol negotiation).

**Rationale**: Adding protocol-level implementation details to a feature specification would violate the spec's purpose of describing user-facing behavior. This configuration is analogous to NuGet package versions or compiler flags — necessary but not specification-worthy.

**Confidence**: HIGH

**Action**:
- [ ] Approve (no change)
- [ ] Reject (add to spec)
- [ ] Modify
