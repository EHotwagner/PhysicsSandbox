# Drift Resolution Proposals

Generated: 2026-03-20
Based on: drift-report from 2026-03-20

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 4 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 001-server-hub/FR-002

**Direction**: BACKFILL

**Current State**:
- Spec says: "The shared contracts project MUST define a `PhysicsHub` service with methods for sending simulation commands, sending view commands, and streaming simulation state."
- Code does: Defines both `PhysicsHub` (client/viewer-facing) AND `SimulationLink` (simulation-facing) services.

**Proposed Resolution**:

Update FR-002 to:

> **FR-002**: The shared contracts project MUST define a `PhysicsHub` service with methods for sending simulation commands, sending view commands, and streaming simulation state; and a `SimulationLink` service with a bidirectional streaming method for the simulation to push state and receive commands.

**Rationale**: SimulationLink was a deliberate planning-phase addition documented in research.md. It cleanly separates client-facing and simulation-facing interfaces. Code is authoritative.

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
- Code does: Health endpoints only mapped in Development environment (standard Aspire template behavior).

**Proposed Resolution**:

Update SC-002 to:

> **SC-002**: The orchestration dashboard is accessible and displays the server hub as a healthy, running resource in Development mode.

**Rationale**: Development-only health endpoint mapping is intentional Aspire security practice. Sandbox always runs in Development mode.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: 002-physics-simulation/FR-013

**Direction**: BACKFILL

**Current State**:
- Spec says: "The streamed state MUST include each body's position, velocity, angular velocity, mass, shape, and identifier."
- Code does: Dynamic bodies (sphere, box) are fully tracked with all 7 fields. Plane bodies are created as BepuPhysics2 statics but NOT tracked in the Bodies map — they are invisible in the state stream.

**Proposed Resolution**:

Update FR-013 to:

> **FR-013**: The streamed state MUST include each dynamic body's position, velocity, angular velocity, mass, shape, and identifier. Static bodies (planes) are not included in the state stream as they have no dynamics.

Add to Assumptions:

> - Plane bodies are approximated as large static boxes in the physics engine. Since collisions are out of scope and statics cannot receive forces, they are not tracked in the state stream. Future features (collision, rendering) may require adding static body tracking.

**Rationale**: This is a deliberate design trade-off, not a bug. BepuPhysics2 statics have no `BodyId` (they use `StaticId`), making them fundamentally different from dynamic bodies. The spec's FR-005 lists plane as a supported shape for *adding* bodies, which works — the drift is only about state *reporting*. Since collisions are explicitly out of scope and planes can't receive forces, including them in state would add complexity for zero observable benefit in the current feature set.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 4: 002-physics-simulation/SC-003

**Direction**: BACKFILL

**Current State**:
- Spec says: "State updates are streamed to the server after every simulation step with zero missed steps."
- Code does: State is sent via `requestStream.WriteAsync(state)` after every `step` call. Under gRPC backpressure (slow server, network congestion), the write may block, delaying the next step. No steps are skipped or dropped — all are delivered in order.

**Proposed Resolution**:

Update SC-003 to:

> **SC-003**: State updates are streamed to the server after every simulation step with zero skipped steps. Under backpressure, the simulation paces itself to the server's consumption rate rather than dropping updates.

**Rationale**: "Zero missed steps" is ambiguous — it could mean "zero latency" or "no drops." The implementation guarantees no drops (every step's state is sent) but doesn't guarantee timing. This is the correct behavior for a physics simulation: you want deterministic, ordered state delivery rather than lossy streaming. The spec should clarify this is about completeness, not timing.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 5: Unspecced — Kestrel HTTP/2 Configuration

**Direction**: BACKFILL (no spec change needed)

**Current State**:
- Code: AppHost sets `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` on the server resource.
- Spec: No mention of protocol configuration.

**Proposed Resolution**:

No spec update needed. Infrastructure implementation detail required for gRPC, not a functional requirement.

**Confidence**: HIGH

**Action**:
- [ ] Approve (no change)
- [ ] Reject (add to spec)
- [ ] Modify

---

### Proposal 6: 002-physics-simulation/Assumptions — Stale Physics Model

**Direction**: BACKFILL

**Current State**:
- Spec Assumptions says: "The simulation uses a simple physics model (Euler or semi-implicit Euler integration). A production-grade physics engine is not required for this sandbox."
- Code does: Uses BepuFSharp (BepuPhysics2 wrapper), a full rigid body physics engine with constraint solver, substeps, and contact events.

**Proposed Resolution**:

Update Assumptions to:

> - The simulation uses BepuFSharp, an idiomatic F# wrapper for the BepuPhysics2 rigid body engine, consumed via local NuGet package. While this is a production-grade engine, only basic features (body creation, force/impulse application, gravity) are used. Advanced features (constraints, contact events, raycasting) are available for future specs.

**Rationale**: The assumption was written before the plan phase chose BepuFSharp. The plan's research.md R1 documents this decision and its rationale. The spec assumption is stale and should reflect the actual technology.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
