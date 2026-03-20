# Feature Specification: Physics Simulation Service

**Feature Branch**: `002-physics-simulation`
**Created**: 2026-03-20
**Status**: Draft
**Input**: User description: "create the physics simulation as second feature and service for the aspire server. it should have lifecycle management (start/stop/step) add/remove bodies, add force/torque/impulse.... a comprehensive way to control the simulation. it also has to send its state every step to the server."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Simulation Lifecycle Control (Priority: P1)

An operator starts the simulation service, which connects to the central server hub. They can play, pause, and single-step the simulation. When playing, the simulation advances continuously at a fixed time step. When paused, only explicit step commands advance the world. The simulation streams its complete world state to the server after every step, whether triggered by play mode or manual stepping.

**Why this priority**: Without lifecycle control, no other feature can function. The simulation must connect, run, and emit state before any interactive use is possible.

**Independent Test**: Can be fully tested by starting the simulation service alongside the server, sending play/pause/step commands via the existing server hub, and verifying that state updates arrive at the server with advancing timestamps.

**Acceptance Scenarios**:

1. **Given** the server is running and the simulation service starts, **When** the simulation initializes, **Then** it connects to the server via the simulation link and reports as connected.
2. **Given** the simulation is connected and paused (default state), **When** a play command is received, **Then** the simulation begins advancing at a fixed time step and streams state after each step.
3. **Given** the simulation is playing, **When** a pause command is received, **Then** the simulation stops advancing but remains connected.
4. **Given** the simulation is paused, **When** a step command is received, **Then** the simulation advances exactly one time step and streams the resulting state.
5. **Given** the simulation is playing or paused, **When** the simulation service shuts down, **Then** it disconnects cleanly from the server.

---

### User Story 2 - Body Management (Priority: P1)

An operator adds rigid bodies to the simulation world, specifying position, velocity, mass, and shape (sphere, box, or plane). Each body receives a unique identifier. Bodies can also be removed from the world by their identifier. The world state streamed to the server reflects all current bodies and their properties.

**Why this priority**: Bodies are the fundamental objects in the simulation. Without them, forces and physics have nothing to act on.

**Independent Test**: Can be tested by adding several bodies, verifying they appear in the streamed state with correct properties, then removing one and confirming it disappears from subsequent state updates.

**Acceptance Scenarios**:

1. **Given** the simulation is connected, **When** an add-body command is received with position, velocity, mass, and shape, **Then** a new body appears in the world with a unique identifier and the specified properties.
2. **Given** bodies exist in the world, **When** a step is performed, **Then** the streamed state includes all bodies with their current positions and velocities.
3. **Given** a body exists with a known identifier, **When** a remove-body command is received for that identifier, **Then** the body is removed and no longer appears in subsequent state updates.
4. **Given** the world is empty, **When** a remove-body command is received for a non-existent identifier, **Then** the command is acknowledged without error (idempotent removal).

---

### User Story 3 - Force, Torque, and Impulse Application (Priority: P2)

An operator applies forces, torques, and impulses to specific bodies. Forces persist across steps (continuous push). Impulses are instantaneous velocity changes applied once. Torques apply rotational force. All of these target a specific body by identifier and specify a 3D vector for direction and magnitude.

**Why this priority**: Applying forces and impulses is the primary way to interact with the physics world, but requires bodies and lifecycle to already work.

**Independent Test**: Can be tested by adding a body at rest, applying a force, stepping, and verifying the body's velocity and position have changed in the expected direction.

**Acceptance Scenarios**:

1. **Given** a body at rest, **When** a force is applied to it and the simulation steps, **Then** the body's velocity changes proportional to the force and inversely proportional to its mass.
2. **Given** a body at rest, **When** an impulse is applied, **Then** the body's velocity changes immediately on the next step, and the impulse does not persist to subsequent steps.
3. **Given** a body, **When** a torque is applied, **Then** the body's angular velocity changes proportional to the torque after the next step.
4. **Given** a force is continuously applied to a body, **When** multiple steps occur, **Then** the body accelerates consistently across steps.
5. **Given** a body with persistent forces applied, **When** a clear-forces command is received for that body, **Then** all forces on the body are removed and the body stops accelerating from those forces on the next step.
6. **Given** a body identifier that does not exist, **When** a force/torque/impulse command targets it, **Then** the command is acknowledged without error (no crash, graceful no-op).

---

### User Story 4 - Gravity Configuration (Priority: P2)

An operator sets the global gravity vector for the simulation world. This affects all bodies on every step. Gravity can be changed at any time, including while the simulation is playing.

**Why this priority**: Gravity is essential for realistic physics but is a single global setting, simpler than per-body forces.

**Independent Test**: Can be tested by adding a body, setting gravity, stepping, and verifying the body falls in the gravity direction.

**Acceptance Scenarios**:

1. **Given** the default simulation (zero gravity), **When** gravity is set to a downward vector and a body exists, **Then** the body accelerates downward on each step.
2. **Given** gravity is set, **When** gravity is changed to a new vector, **Then** subsequent steps use the new gravity.
3. **Given** gravity is set, **When** gravity is set to zero, **Then** bodies in free flight continue at constant velocity (no acceleration).

---

### User Story 5 - Continuous State Streaming (Priority: P1)

Every time the simulation advances one step (whether from play mode or manual step), the complete world state is sent to the server. The state includes all body positions, velocities, angular velocities, the current simulation time, and whether the simulation is running or paused. Late-joining subscribers to the server receive the most recently cached state immediately.

**Why this priority**: State streaming is the simulation's primary output — without it, no downstream service (viewer, client) can observe the physics world.

**Independent Test**: Can be tested by connecting a state subscriber to the server, starting the simulation in play mode, and verifying a continuous stream of state updates arrives with incrementing timestamps.

**Acceptance Scenarios**:

1. **Given** the simulation is playing, **When** each step completes, **Then** the complete world state is sent to the server.
2. **Given** the simulation is paused, **When** a manual step completes, **Then** the state is sent once.
3. **Given** multiple bodies exist, **When** state is streamed, **Then** every body's position, velocity, and angular velocity are included.
4. **Given** the simulation is running, **When** the state includes the `running` flag as true, **Then** subscribers can distinguish playing from paused states.

---

### Edge Cases

- What happens when the server disconnects while the simulation is running? The simulation detects the broken connection, logs the event, and shuts down cleanly. No reconnect attempts — the operator restarts the service explicitly.
- What happens when commands arrive faster than the simulation can process them? Commands should be queued up to a reasonable limit; excess commands are dropped rather than causing unbounded memory growth.
- What happens when a body with mass zero is added? The system should reject or handle zero/negative mass gracefully (treat as invalid input).
- What happens when extremely large forces are applied? The simulation should continue running without crashing, though results may be physically unrealistic.
- What happens when the simulation has no bodies and is set to play? It should continue stepping and streaming empty state (no error).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The simulation service MUST connect to the server hub on startup via the existing simulation link protocol.
- **FR-002**: The simulation MUST start in a paused state by default.
- **FR-003**: The simulation MUST support play, pause, and single-step lifecycle commands.
- **FR-004**: When playing, the simulation MUST advance at a fixed time step and stream state after each step.
- **FR-005**: The simulation MUST support adding rigid bodies with position, velocity, mass, and shape (sphere, box, plane).
- **FR-006**: The simulation MUST assign a unique identifier to each added body.
- **FR-007**: The simulation MUST support removing bodies by identifier.
- **FR-008**: The simulation MUST support applying persistent forces to specific bodies (3D vector, by body ID).
- **FR-009**: The simulation MUST support applying one-shot impulses to specific bodies (3D vector, by body ID).
- **FR-010**: The simulation MUST support applying torques to specific bodies (3D vector, by body ID).
- **FR-020**: The simulation MUST support a clear-forces command that removes all persistent forces on a specific body (by body ID).
- **FR-011**: The simulation MUST support setting a global gravity vector that affects all bodies.
- **FR-012**: The simulation MUST stream the complete world state to the server after every simulation step.
- **FR-013**: The streamed state MUST include each body's position, velocity, angular velocity, mass, shape, and identifier.
- **FR-014**: The streamed state MUST include the current simulation time and running/paused flag.
- **FR-015**: Commands targeting non-existent body identifiers MUST be handled gracefully without errors.
- **FR-016**: The simulation MUST handle server disconnection by logging the event and shutting down cleanly (no reconnect attempts).
- **FR-017**: The simulation MUST reject bodies with zero or negative mass.
- **FR-018**: The simulation MUST be registered with the Aspire orchestrator and discoverable by the server.
- **FR-019**: The simulation MUST extend the existing communication contracts where new command types (remove body, impulse, torque) are needed, preserving backward compatibility with existing message structures.

### Key Entities

- **World**: The simulation environment containing all bodies and global settings (gravity, time, running state). One world per simulation instance.
- **Body**: A rigid object in the world with identity, spatial properties (position, velocity, angular velocity), physical properties (mass, shape), and accumulated forces.
- **Force**: A persistent 3D vector applied to a body each step until explicitly cleared via clear-forces command or the body is removed.
- **Impulse**: A one-shot 3D velocity change applied to a body on the next step only.
- **Torque**: A 3D rotational force vector applied to a body, affecting angular velocity.
- **Simulation State**: A snapshot of the entire world at a point in time — all bodies, simulation clock, and running flag — emitted after every step.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The simulation service connects to the server and is ready to receive commands within 5 seconds of startup.
- **SC-002**: All command types (play, pause, step, add body, remove body, force, impulse, torque, set gravity) produce the expected physical result within one step of processing.
- **SC-003**: State updates are streamed to the server after every simulation step with zero missed steps.
- **SC-004**: The simulation maintains stable operation with at least 100 bodies in the world simultaneously.
- **SC-005**: A newly connected state subscriber receives the current world state within 1 second (via server cache).
- **SC-006**: All edge cases (non-existent body targets, empty world, zero-mass rejection, server disconnection) are handled without crashes or unhandled errors.
- **SC-007**: The simulation service passes both unit tests (physics logic) and integration tests (end-to-end with the server hub).

## Clarifications

### Session 2026-03-20

- Q: How are persistent forces removed from a body? → A: A clear-forces command removes all persistent forces on a specific body (FR-020).
- Q: What happens on server disconnection — reconnect or shutdown? → A: Clean shutdown. Log the event and exit; operator restarts explicitly (FR-016).

## Assumptions

- The fixed time step value is an internal detail; a reasonable default (e.g., 16ms / 60Hz) is acceptable without user configuration for this feature.
- Angular velocity and rotation tracking require extending the existing Body message in the contracts. This is acceptable as part of this feature.
- The simulation uses a simple physics model (Euler or semi-implicit Euler integration). A production-grade physics engine is not required for this sandbox.
- The existing SimulationLink bidirectional streaming contract is the communication mechanism — no new gRPC services are needed, only extensions to existing message types.
- Collision detection and response are out of scope for this feature. Bodies move under forces and gravity but do not collide with each other.
