# Feature Specification: Stress Test Demos

**Feature Branch**: `002-stress-test-demos`
**Created**: 2026-03-21
**Status**: Draft
**Input**: User description: "Add more complex demos that stress test the system."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Body Avalanche Demo (Priority: P1)

A developer wants to see how the physics sandbox handles large numbers of objects interacting simultaneously. This demo progressively adds waves of bodies — starting small (50), then scaling up (100, 200, 300+) — while the simulation runs, creating a visual avalanche of objects cascading and colliding. After each wave, the demo pauses briefly to let the user observe the scene before the next wave arrives.

**Why this priority**: This is the most visually impactful stress test and directly demonstrates the system's body-scaling capacity. Current demos max out at ~40 bodies; this demo should push past 200+ to show the system's true capability.

**Independent Test**: Run the demo against a running server and verify that bodies are added in escalating waves, the simulation remains responsive throughout, and the final body count exceeds 200.

**Acceptance Scenarios**:

1. **Given** a clean simulation, **When** the avalanche demo runs, **Then** bodies are added in at least 4 progressively larger waves with the simulation running between waves.
2. **Given** hundreds of bodies actively simulating, **When** the demo reaches peak body count, **Then** the simulation continues to run (may slow down but does not crash or freeze).
3. **Given** the avalanche demo, **When** it completes, **Then** it reports the total body count and approximate performance observed.

---

### User Story 2 - Wrecking Ball Stress Demo (Priority: P1)

A developer wants a visually dramatic demo that creates a large, dense structure (tall tower, wide wall, or multi-layered pyramid) and then demolishes it with a high-velocity projectile. The demo should showcase mass collision resolution — dozens or hundreds of bodies scattering from a single impact event. This tests the system's ability to handle sudden, complex collision cascades.

**Why this priority**: Collision cascades are the most demanding physics workload. A large structure hit by a fast projectile generates hundreds of simultaneous contact points, making this the hardest test for the physics engine. It's also the most entertaining demo to watch.

**Independent Test**: Run the demo and verify the structure is built (50+ bodies), the projectile strikes it, and the resulting collision cascade resolves without simulation errors.

**Acceptance Scenarios**:

1. **Given** a clean simulation, **When** the wrecking ball demo runs, **Then** a structure of at least 50 bodies is constructed before the impact occurs.
2. **Given** a large structure, **When** a high-velocity projectile strikes it, **Then** the collision cascade produces visible scattering of bodies across the scene.
3. **Given** the collision cascade, **When** bodies settle after impact, **Then** the simulation reports all bodies accounted for (none lost or stuck in invalid states).

---

### User Story 3 - Rapid-Fire Command Throughput Demo (Priority: P2)

A developer wants to see how fast the system processes commands under sustained load. This demo creates a moderate scene (50 bodies) and then issues a rapid sequence of force/impulse commands to all bodies simultaneously — pushing, spinning, and launching them repeatedly for a sustained period. This tests command processing throughput rather than body count.

**Why this priority**: Complements the body-scaling demos by testing a different axis of performance. High command throughput is critical for interactive use cases where an AI assistant issues many commands in quick succession.

**Independent Test**: Run the demo and verify that sustained rapid commands are processed without command queue overflow or dropped messages.

**Acceptance Scenarios**:

1. **Given** a scene with 50 bodies, **When** rapid commands are issued to all bodies, **Then** all commands are processed and their effects are visible in the simulation.
2. **Given** sustained command throughput, **When** the demo runs for at least 10 seconds, **Then** no commands are silently dropped or cause errors.
3. **Given** the throughput demo, **When** it completes, **Then** it reports the total commands issued and approximate commands-per-second achieved.

---

### User Story 4 - Gravity Storm Demo (Priority: P2)

A developer wants a visually chaotic demo that combines body quantity with dynamic environmental changes. This demo fills the scene with 100+ mixed objects (spheres, boxes, various presets) and then rapidly cycles gravity direction — up, down, sideways, diagonal — causing all objects to tumble and fly in shifting directions. Camera sweeps follow the action automatically.

**Why this priority**: Tests the system's handling of global state changes (gravity) affecting many bodies simultaneously, combined with view command throughput (camera updates). Creates a visually spectacular result that demonstrates the sandbox's full capability.

**Independent Test**: Run the demo and verify gravity changes are applied to all bodies, camera tracks the action, and the simulation handles rapid environmental changes without errors.

**Acceptance Scenarios**:

1. **Given** a scene with 100+ mixed bodies, **When** gravity direction changes rapidly, **Then** all bodies respond to the new gravity within one simulation step.
2. **Given** multiple gravity changes in succession, **When** the demo cycles through at least 6 different gravity directions, **Then** the simulation remains stable throughout.
3. **Given** gravity storm with camera sweeps, **When** the demo completes, **Then** it reports total body count, gravity changes applied, and any performance observations.

---

### User Story 5 - Endurance Run Demo (Priority: P3)

A developer wants to verify long-running stability by running a demo that gradually builds complexity over an extended period (60+ seconds). It starts with a small scene, periodically adds new bodies, applies random forces, removes some bodies, and repeats — simulating a sustained interactive session. This tests for memory leaks, accumulating errors, or gradual performance degradation over time.

**Why this priority**: Long-running stability is important but less visually dramatic. This is a soak test that validates the system doesn't degrade over sustained use — important for real interactive sessions but lower priority than the visually impressive demos.

**Independent Test**: Run the demo for its full duration and verify the simulation remains responsive from start to finish, with no errors or crashes.

**Acceptance Scenarios**:

1. **Given** a long-running demo, **When** it runs for at least 60 seconds with continuous body churn, **Then** the simulation remains responsive throughout.
2. **Given** ongoing body creation and removal, **When** the demo completes, **Then** the final body count is within expected bounds (not accumulating unboundedly).
3. **Given** the endurance run, **When** it completes, **Then** it reports start/end performance metrics to show whether degradation occurred.

---

### Edge Cases

- What happens when body count approaches the system's degradation threshold (~500 bodies)? The demo should observe and report slowdown rather than crash.
- What happens if the demo's command rate exceeds the server's processing capacity (1024-command queue)? Commands should queue gracefully, not be silently dropped.
- What happens when a collision cascade causes physics instability (bodies tunneling through each other at high speed)? The demo should continue and report anomalies.
- What happens if the server becomes unresponsive during a stress demo? The script should detect timeouts and terminate gracefully rather than hang.
- What happens when running stress demos back-to-back? Each demo must fully clean up before the next starts.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The demo suite MUST include at least 4 new demos that each push the system beyond the scale of existing demos (which max at ~40 bodies).
- **FR-002**: At least one demo MUST scale body count to 200+ simultaneously active bodies.
- **FR-003**: At least one demo MUST generate a mass collision event involving 50+ bodies colliding in a short time window.
- **FR-004**: At least one demo MUST sustain rapid command throughput (forces/impulses to many bodies) for at least 10 seconds.
- **FR-005**: At least one demo MUST run for 60+ seconds to test long-running stability.
- **FR-006**: Every stress demo MUST report performance observations upon completion (body count, duration, command count, or throughput rate as appropriate).
- **FR-007**: Every stress demo MUST clean up fully after completion so subsequent demos start from a clean state.
- **FR-008**: Stress demos MUST handle server slowdowns gracefully — they should observe and report degradation, not crash or hang.
- **FR-009**: Stress demos MUST be runnable both individually and as part of the full demo suite (RunAll and AutoRun).
- **FR-010**: Stress demos MUST use batch commands where appropriate to maximize setup efficiency.
- **FR-011**: Each stress demo MUST include camera positioning that provides the best visual perspective for the scenario.
- **FR-012**: The demo numbering MUST continue from the existing sequence (Demo 11, 12, etc.) and integrate into AllDemos, RunAll, and AutoRun.

### Key Entities

- **Stress Demo**: A demo script that intentionally pushes system capacity beyond normal usage to observe performance characteristics and limits.
- **Performance Report**: A summary printed at the end of each stress demo showing relevant metrics (body count, duration, command count, throughput, degradation observations).
- **Degradation Threshold**: The point at which the system noticeably slows down under load, to be discovered and reported by the demos.

## Assumptions

- The system can handle 200+ bodies before significant degradation based on the existing stress test framework's default of 500 max bodies.
- The batch command limit of 100 commands per batch is sufficient — demos needing more will issue multiple batches.
- The existing Prelude helpers (reset, runFor, sleep) are adequate or will be extended by the companion feature (001-demo-script-modernization) before this feature is implemented.
- Stress demos are expected to take longer than standard demos (30-90 seconds each vs 3-10 seconds for current demos).
- The 3D viewer may drop frames or lag during peak stress — this is expected and should be reported, not prevented.
- Body creation via generators (randomBodies, pyramid, grid) is the most efficient way to reach high body counts quickly.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 4 new stress demos are added and all run to completion without crashes in both RunAll and AutoRun modes.
- **SC-002**: The body avalanche demo reaches 200+ simultaneously active bodies and the simulation continues running (does not freeze or crash).
- **SC-003**: The wrecking ball demo resolves a collision cascade of 50+ bodies without simulation errors or lost bodies.
- **SC-004**: The command throughput demo sustains at least 10 seconds of rapid commands and reports a measurable commands-per-second rate.
- **SC-005**: The endurance demo runs for 60+ seconds and end-of-run performance is within 50% of start-of-run performance (no catastrophic degradation).
- **SC-006**: Every stress demo prints a human-readable performance summary at completion.
- **SC-007**: The total demo count (existing + new) is at least 14, with all demos passing in the AutoRun suite.
