# Feature Specification: State Stream Bandwidth Optimization

**Feature Branch**: `004-state-stream-optimization`
**Created**: 2026-03-23
**Status**: Draft
**Input**: User description: "Optimize state streaming bandwidth by leveraging protobuf zero-cost default fields to avoid transmitting unchanged body properties (shape, color, mass, etc.) every tick, sending only pose data (position + orientation) for moving bodies."

## Data Taxonomy

Body properties are classified into two categories based on update frequency:

**Continuous data** — changes every simulation tick (60 Hz):
- `id` (body identifier, always present)
- `position` (Vec3)
- `orientation` (Vec4)
- `velocity` (Vec3) — per-client opt-in
- `angular_velocity` (Vec3) — per-client opt-in

**Semi-static data** — set at creation, rarely changes:
- `shape` (Shape)
- `color` (Color)
- `mass` (double)
- `is_static` (bool)
- `motion_type` (BodyMotionType)
- `collision_group` (uint32)
- `collision_mask` (uint32)
- `material` (MaterialProperties)

**Transport separation**: Continuous data flows through the existing `StreamState` RPC at 60 Hz for dynamic bodies only. Static bodies are excluded from the tick stream entirely — their pose is semi-static. Semi-static data is delivered via a new **property event stream** (`StreamProperties` RPC, server → client) — sent on body creation, on property change, and on late-joiner connect. This keeps the hot-path tick stream as lean as possible. The property event stream follows the same patterns (caching, backfill, on-demand delivery) already proven by the mesh transport infrastructure.

**Constraints and registered shapes** are also semi-static and flow through the property event stream — only transmitted on add/remove/modify, not per-tick.

## Clarifications

### Session 2026-03-23

- Q: How should semi-static body properties be delivered to clients? → A: New property event stream (`StreamProperties` RPC), following the patterns proven by the mesh bidirectional channel. The 60 Hz stream sends only continuous data; semi-static properties are delivered via the property event stream on creation and on change.
- Q: Should static bodies be included in the 60 Hz tick stream? → A: No. Static bodies are excluded from the tick stream entirely. Their pose (position + orientation) is delivered once via the property event stream alongside other semi-static data. Only dynamic bodies appear in the tick stream.
- Q: How should body removal be signaled to clients? → A: Explicit removal event on the property event stream. Server pushes a removal notification (body id) when a body is removed. Clients delete it from local state.
- Q: How are dynamic-to-static (and reverse) transitions handled across channels? → A: The motion type change on the property event stream is authoritative. The message includes the body's current pose. Clients stop expecting the body in the tick stream (dynamic→static) or start expecting it (static→dynamic). No separate migration event needed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reduced Bandwidth for Steady-State Scenes (Priority: P1)

When a simulation is running with many objects (100-500+), each client currently receives the full state of every body on every tick at 60 Hz — including properties like shape, color, mass, and material that never change after creation. The system should only transmit properties that have actually changed since a body was last fully sent to a given client, dramatically reducing per-tick message sizes.

**Why this priority**: This is the core bandwidth optimization. At 200 bodies, current per-tick payload is ~50 KB; with pose-only updates it drops to ~11 KB — a ~78% reduction per client, per tick.

**Independent Test**: Run a scene with 200 dynamic bodies, measure serialized message size per tick after the initial state has been sent. Compare against baseline (current full-state messages).

**Acceptance Scenarios**:

1. **Given** a scene with 200 moving bodies has been streaming for at least 2 seconds, **When** no semi-static properties have changed, **Then** the 60 Hz tick stream contains only continuous data (id, position, orientation, and optionally velocity/angular velocity) — no shape, color, mass, material, or other semi-static fields.
2. **Given** a body's color is changed mid-simulation via a command, **When** the change is applied, **Then** the updated semi-static properties are pushed to all clients via the property event stream. The tick stream is unaffected.
3. **Given** a new client connects mid-simulation, **When** it joins the property event stream, **Then** it receives the full semi-static state of all bodies (shape, color, mass, material, etc.) as a backfill before receiving tick stream updates.

---

### User Story 2 - Viewer Gets Minimal Pose-Only Updates (Priority: P2)

The 3D viewer only needs position, orientation, shape, and color to render. It does not use velocity, angular velocity, mass, collision filters, or material properties. The viewer should receive even less data than other clients by excluding fields it never reads.

**Why this priority**: The viewer is the most performance-sensitive client — it processes 60 updates/second for rendering. Eliminating unused fields reduces both serialization and deserialization overhead.

**Independent Test**: Connect only the viewer to the server with a 200-body scene. Verify that velocity and angular velocity fields are absent from messages received by the viewer after initial state.

**Acceptance Scenarios**:

1. **Given** the viewer has received a body's full initial state, **When** subsequent ticks are received, **Then** velocity and angular velocity are never included (since the viewer does not need them).
2. **Given** the viewer is the only connected client, **When** measuring server CPU and network throughput, **Then** serialization cost per tick is measurably lower than the current baseline.

---

### User Story 3 - Client and MCP Receive Velocity Data (Priority: P2)

The REPL client and MCP server use velocity and angular velocity for display, filtering (min-velocity watch), steering (stop command), and trajectory recording. These clients must continue to receive velocity data on every tick for moving bodies.

**Why this priority**: Preserving existing functionality for velocity-dependent features is essential — the optimization must not break the live watch, steering, or recording capabilities.

**Independent Test**: Run the client's live watch with a min-velocity filter and the MCP's trajectory recording tool against a 200-body scene. Verify velocity data is present and filtering/recording work correctly.

**Acceptance Scenarios**:

1. **Given** the client is running live watch with a min-velocity filter, **When** bodies are moving, **Then** velocity data is available for filtering and the watch displays correctly.
2. **Given** the MCP recording system is active, **When** querying a body's trajectory, **Then** velocity data is present in all recorded snapshots.

---

### User Story 4 - Constraints and Registered Shapes Use Same Optimization (Priority: P3)

Constraint and registered shape data is currently sent on every tick even though it only changes when constraints/shapes are added or removed. These should follow the same pattern — only transmit when changed.

**Why this priority**: Lower impact than body optimization (fewer constraints than bodies in typical scenes), but follows the same principle and further reduces payload.

**Independent Test**: Create a scene with 50 constraints, verify that after initial state, tick messages do not include constraint data unless constraints are added/removed.

**Acceptance Scenarios**:

1. **Given** a scene with 50 constraints has been streaming for 2+ seconds with no constraint changes, **When** a tick message is received, **Then** the constraints list is empty (no constraint data transmitted).
2. **Given** a constraint is removed mid-simulation, **When** the next state update is sent, **Then** the updated constraint set is included. Subsequent ticks omit it again.

---

### Edge Cases

- What happens when a body is removed? The server pushes an explicit removal event on the property event stream. Clients delete the body from their local state.
- What happens when a body transitions from dynamic to static (or vice versa)? The motion type change is delivered via the property event stream with the body's current pose. For dynamic→static: the body stops appearing in the tick stream; clients use the pose from the bidirectional message as the final pose. For static→dynamic: the body starts appearing in the tick stream on the next tick.
- What happens if a client misses a tick (e.g., slow consumer on a bounded channel)? Tick data is always-latest (next tick overwrites), so missing a tick is harmless for pose. For property events (body_created, body_removed), the property event stream uses a reliable delivery channel; if a client disconnects and reconnects, it receives a full PropertySnapshot backfill.
- What happens when the simulation is paused and resumed? No special handling needed — pose data stops changing when paused, so ticks naturally become smaller.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The 60 Hz tick stream MUST contain only continuous data for dynamic bodies: body id, position, and orientation, plus velocity and angular velocity for clients that have opted in. Static bodies MUST NOT appear in the tick stream.
- **FR-002**: Semi-static body properties (shape, color, mass, material, collision filters, motion type, is_static) MUST be delivered via the property event stream, not the tick stream.
- **FR-003**: The bidirectional channel MUST deliver a body's full semi-static properties when the body is first created. For static bodies, this includes position and orientation (since they do not appear in the tick stream).
- **FR-004**: The bidirectional channel MUST push updated semi-static properties to all connected clients when any semi-static property changes for a body.
- **FR-005**: When a new client connects to the property event stream, it MUST receive the full semi-static state of all existing bodies as a backfill.
- **FR-006**: The system MUST allow different clients to declare which continuous fields they need per tick (e.g., viewer opts out of velocity; client/MCP opt in).
- **FR-007**: The system MUST push an explicit removal event (body id) on the property event stream when a body is removed. Clients MUST delete the body from local state upon receiving it.
- **FR-008**: Constraints and registered shapes MUST be delivered via the property event stream on add/remove/modify — not on every tick.
- **FR-009**: All clients MUST be updated to merge continuous tick data with semi-static data from the property event stream to reconstruct full body state locally.
- **FR-010**: The system MUST ensure that a slow consumer who drops ticks eventually converges to correct state without requiring a manual reconnect.

### Key Entities

- **Continuous Data**: Per-tick body state (id, position, orientation, and optionally velocity/angular velocity) delivered via the 60 Hz tick stream. Always fresh — no client-side caching needed.
- **Semi-Static Data**: Body properties that rarely change (shape, color, mass, material, collision filters, motion type, is_static) delivered via the property event stream on creation, on change, and on late-joiner backfill. Clients cache this locally and merge with continuous data for full state.
- **Client Field Profile**: A declaration of which continuous fields a given client needs per tick beyond the mandatory pose data (id + position + orientation). Examples: viewer opts out of velocity; client/MCP opt in.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a scene with 200 moving bodies in steady state (no property changes), per-tick message size is reduced by at least 70% compared to current full-state messages.
- **SC-002**: For the viewer specifically, per-tick message size is reduced by at least 80% (no velocity, no unchanged properties).
- **SC-003**: All existing tests continue to pass — no functional regression in client display, live watch, steering, MCP tools, or recording.
- **SC-004**: New clients joining a running simulation see correct, complete state within 1 tick of connecting.
- **SC-005**: Body property changes (color, shape, material) are visible to all clients within 1 tick of the change being applied.

## Assumptions

- Protobuf 3 does not transmit fields set to their default values (zero, empty string, false, null for messages). This is a property of the wire format that the optimization can leverage — fields simply not set on the message object will not consume bandwidth.
- The current simulation tick rate (60 Hz) remains unchanged.
- The number of clients is small (4-6) and all are local or on a fast network, so per-client tracking overhead is negligible.
- Static bodies that never move can be sent once and omitted from subsequent ticks entirely, since their pose never changes.
