# Feature Specification: Stress Test Demos

**Feature Branch**: `003-stress-test-demos`
**Created**: 2026-03-21
**Status**: Draft
**Input**: User description: "create and add more complex demos to stress test the system and see what is possible and where it breaks"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run High-Volume Body Demos (Priority: P1)

A developer or AI assistant runs demo scripts that progressively increase physics body counts to discover where simulation performance degrades. Each demo is a self-contained scenario that pushes a specific axis of complexity (body count, collision density, force interactions) well beyond the current 20-body maximum seen in existing demos.

**Why this priority**: The primary goal is finding system limits. Without high-volume scenarios, there is no way to identify degradation points or breaking thresholds.

**Independent Test**: Can be tested by running any single high-volume demo and observing whether bodies are created, simulation runs, and timing/metrics are reported.

**Acceptance Scenarios**:

1. **Given** a fresh simulation, **When** a body-scaling demo runs with 50, 100, 200, and 500 bodies, **Then** the demo completes each tier and reports observable timing for each tier.
2. **Given** a fresh simulation, **When** a demo creates bodies that exceed the system's capacity, **Then** the demo reports degradation or failure gracefully rather than crashing silently.
3. **Given** any stress demo, **When** the demo finishes, **Then** it resets the simulation cleanly for the next demo.

---

### User Story 2 - Run Collision-Heavy Demos (Priority: P1)

A developer runs demos specifically designed to maximize simultaneous collisions — tightly packed formations, chain reactions, and confined spaces — to stress the collision detection and resolution pipeline.

**Why this priority**: Collision handling is often the first bottleneck in physics engines. Demos that isolate collision stress from body count reveal different failure modes.

**Independent Test**: Can be tested by running a collision demo and verifying that all bodies interact physically (no pass-through, no explosion artifacts) and that timing is reported.

**Acceptance Scenarios**:

1. **Given** a fresh simulation, **When** a demo drops a large number of bodies into a confined space, **Then** all bodies collide and settle without bodies passing through each other or the ground.
2. **Given** a collision-heavy demo, **When** a chain reaction is triggered (e.g., domino cascade at scale), **Then** the reaction propagates visibly through all bodies.

---

### User Story 3 - Run Force & Interaction Demos (Priority: P2)

A developer runs demos that apply many simultaneous forces, impulses, and torques to stress the force-application pipeline and observe how the system handles concurrent interactions.

**Why this priority**: Forces applied in bulk (batch impulses, gravity changes, torque on many bodies) test a different code path than simple body creation and collision.

**Independent Test**: Can be tested by running a force demo and verifying that forces are applied to all target bodies and the simulation responds correctly.

**Acceptance Scenarios**:

1. **Given** a simulation with 100+ bodies, **When** impulses are batch-applied to all bodies simultaneously, **Then** all bodies respond to the applied forces.
2. **Given** a simulation with active bodies, **When** gravity is changed multiple times in rapid succession, **Then** all bodies respond to each gravity change without desynchronization.

---

### User Story 4 - Run Combined Stress Scenarios (Priority: P2)

A developer runs "everything at once" demos that combine high body counts, dense collisions, continuous force application, and camera movement to simulate worst-case real-world usage.

**Why this priority**: Individual axis tests reveal isolated limits, but combined scenarios reveal interaction effects and overall system ceiling.

**Independent Test**: Can be tested by running the combined scenario and observing that it either completes or reports which subsystem degraded first.

**Acceptance Scenarios**:

1. **Given** a fresh simulation, **When** a combined demo creates 200+ bodies, applies forces, and changes camera angles, **Then** the demo runs to completion and reports per-stage timing.
2. **Given** a combined demo running, **When** one subsystem (physics, rendering, gRPC) becomes the bottleneck, **Then** the demo output identifies which stage is slowest.

---

### User Story 5 - Run Demos via MCP Tools (Priority: P3)

An AI assistant runs stress demos through MCP tools (not just F# scripts) to validate that the MCP tool interface handles high-volume operations correctly and to compare MCP performance against direct scripting.

**Why this priority**: MCP is the AI assistant's interface to the sandbox. If MCP has different limits or overhead, that needs to be discovered.

**Independent Test**: Can be tested by invoking stress scenarios through MCP batch commands and comparing results with script-based execution.

**Acceptance Scenarios**:

1. **Given** the MCP server is running, **When** stress demos are invoked through MCP batch tools, **Then** the results are comparable to script-based execution.
2. **Given** a stress scenario, **When** run via MCP vs. script, **Then** the overhead percentage and any MCP-specific failures are reported.

---

### Edge Cases

- What happens when body creation exceeds available memory or physics engine limits?
- How does the system behave when batch commands exceed the 100-command split threshold repeatedly in rapid succession?
- What happens if a demo applies forces to bodies that have already been removed or settled below the simulation floor?
- How does the viewer handle rendering 500+ bodies simultaneously?
- What happens if the simulation step time exceeds the state broadcast interval?
- What happens if a demo is interrupted mid-execution (e.g., server restart)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The demo suite MUST include at least 5 new stress test demos beyond the existing 10 demos.
- **FR-002**: Each stress demo MUST follow the existing demo conventions: use Prelude.fsx helpers, reset simulation at start, use batchAdd for bulk operations.
- **FR-003**: At least one demo MUST progressively scale body count in tiers (e.g., 50 → 100 → 200 → 500) and report observable results at each tier.
- **FR-004**: At least one demo MUST focus on collision density by creating tightly packed formations in a confined space.
- **FR-005**: At least one demo MUST apply bulk forces (impulses, torques, gravity changes) to 100+ bodies simultaneously.
- **FR-006**: At least one demo MUST combine multiple stress axes (body count + collisions + forces + camera movement) in a single scenario.
- **FR-007**: Each demo MUST print timing or observation markers so the operator can identify when performance degrades.
- **FR-008**: All new demos MUST be integrated into AllDemos.fsx, RunAll.fsx, and AutoRun.fsx following the existing pattern.
- **FR-009**: Demos MUST handle failures gracefully — if a batch command fails or the simulation becomes unresponsive, the demo should report the failure and attempt to continue or exit cleanly.
- **FR-010**: The demo suite MUST work through both direct F# scripting and MCP tool invocation (at least the body-scaling and combined scenarios).

### Key Entities

- **Stress Demo**: A self-contained scenario script that pushes one or more system axes beyond normal usage, follows Prelude.fsx conventions, and reports observable results.
- **Performance Tier**: A discrete body count or complexity level within a progressive demo (e.g., 50/100/200/500 bodies).
- **Degradation Point**: The tier at which observable performance drops below acceptable thresholds (e.g., step time > 100ms, visible stutter, command failures).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 5 new stress demos are runnable end-to-end without unhandled crashes.
- **SC-002**: The body-scaling demo identifies a concrete degradation point (the body count at which step time exceeds 100ms or commands start failing).
- **SC-003**: The collision-density demo creates at least 100 simultaneously interacting bodies in a confined space.
- **SC-004**: The combined scenario demo runs with 200+ bodies, forces, and camera movement, and reports per-stage timing.
- **SC-005**: All new demos complete within 5 minutes each when run individually.
- **SC-006**: The full demo suite (existing + new) runs end-to-end via AutoRun without manual intervention.
- **SC-007**: At least one scenario is executable via MCP batch tools with comparable results to scripting.

## Assumptions

- The existing Prelude.fsx helpers (batchAdd, resetSimulation, nextId, etc.) are sufficient for the new demos; no new client library features are needed.
- The 100-command batch limit is a fixed constraint that demos must work within (auto-split via batchAdd).
- The existing stress test infrastructure (StressTestRunner) is complementary to but separate from these demos — the demos are user-observable scenarios, not automated test harnesses.
- Performance degradation thresholds (100ms step time) from the existing stress test infrastructure are reasonable benchmarks for demo observation.
- The viewer can render scenes with hundreds of bodies, though frame rate may degrade — this degradation is itself a useful observation.
