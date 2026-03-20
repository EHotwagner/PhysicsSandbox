# Feature Specification: 3D Viewer

**Feature Branch**: `003-3d-viewer`
**Created**: 2026-03-20
**Status**: Completed
**Input**: User description: "this feature is the viewer. it has to show the simulation state it gets from the server and adjust the ui according to commands from the server (camera...)."

## Clarifications

### Session 2026-03-20

- Q: Should the viewer support interactive mouse/keyboard camera control in its own window, in addition to REPL commands? → A: Yes — viewer accepts mouse/keyboard input (orbit, pan, zoom) AND REPL camera commands.
- Q: How should bodies be visually distinguished in the scene? → A: Color by shape type (e.g., spheres one color, boxes another).
- Q: Should the viewer include a ground plane or reference grid? → A: Ground grid (flat grid lines at Y=0 for spatial reference).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Live Simulation (Priority: P1)

A user launches the viewer alongside the running simulation. The viewer connects to the server and begins receiving a continuous stream of simulation state. Each physics body is rendered as a 3D shape (sphere, box) at its current position and orientation. As the simulation advances, the viewer updates in real time so the user sees bodies moving, colliding, and rotating.

**Why this priority**: Without rendering simulation state, the viewer has no purpose. This is the core value proposition — making the invisible physics world visible.

**Independent Test**: Can be fully tested by starting the simulation with a few bodies and verifying that the viewer window displays them at the correct positions, updating each frame.

**Acceptance Scenarios**:

1. **Given** the server and simulation are running with bodies present, **When** the viewer starts and connects, **Then** all bodies appear as 3D shapes matching their shape type, position, and orientation.
2. **Given** the viewer is connected and displaying bodies, **When** the simulation advances (bodies move, collide), **Then** the viewer updates positions and orientations smoothly each frame.
3. **Given** the viewer is connected, **When** a new body is added to the simulation, **Then** the viewer renders it on the next state update without requiring a restart.
4. **Given** the simulation is paused, **When** the viewer is connected, **Then** the viewer displays the last known state as a static scene.

---

### User Story 2 - Camera Control via Commands (Priority: P2)

A user controls the camera in two ways: (1) directly in the viewer window using mouse and keyboard (click-drag to orbit, scroll to zoom, middle-click to pan), and (2) via precise commands from the REPL client (SetCamera, SetZoom) forwarded through the server. Both input methods update the same camera state. Direct interaction provides fluid exploration; REPL commands enable precise, repeatable positioning.

**Why this priority**: Camera control is essential for inspecting different parts of the simulation. Without it, the user is stuck at a fixed viewpoint and cannot meaningfully explore the 3D scene.

**Independent Test**: Can be tested by (a) using mouse drag in the viewer to orbit the camera and verifying the viewpoint changes, and (b) sending a SetCamera command from the client and verifying the viewer's viewpoint changes to match the requested position and target.

**Acceptance Scenarios**:

1. **Given** the viewer is rendering a scene, **When** a SetCamera command is received with a new position and target, **Then** the viewer updates the camera to look from the new position toward the new target.
2. **Given** the viewer is rendering a scene, **When** a SetZoom command is received, **Then** the viewer adjusts the field of view or camera distance to reflect the requested zoom level.
3. **Given** the viewer is rendering a scene, **When** multiple camera commands arrive in rapid succession, **Then** each command is applied in order without dropped or corrupted state.
4. **Given** the viewer is rendering a scene, **When** the user click-drags in the viewer window, **Then** the camera orbits around the scene's focal point.
5. **Given** the viewer is rendering a scene, **When** the user scrolls the mouse wheel, **Then** the camera zooms in or out.
6. **Given** the user has repositioned the camera via mouse, **When** a SetCamera REPL command arrives, **Then** the REPL command overrides the current camera position.

---

### User Story 3 - Wireframe Toggle (Priority: P3)

A user toggles wireframe rendering mode from the REPL client. When enabled, all bodies are drawn as wireframe outlines instead of solid shapes, making it easier to see overlapping objects and collision boundaries.

**Why this priority**: Wireframe is a useful debugging visualization but not essential for core functionality. The viewer is fully usable without it.

**Independent Test**: Can be tested by sending a ToggleWireframe command and verifying the rendering style changes from solid to wireframe (and back).

**Acceptance Scenarios**:

1. **Given** the viewer is rendering solid shapes, **When** a ToggleWireframe command with enabled=true is received, **Then** all bodies switch to wireframe rendering.
2. **Given** the viewer is in wireframe mode, **When** a ToggleWireframe command with enabled=false is received, **Then** all bodies switch back to solid rendering.

---

### User Story 4 - Simulation Status Display (Priority: P3)

The viewer displays simulation metadata — current simulation time and whether the simulation is running or paused — as an overlay or status indicator. This gives the user at-a-glance context about the simulation state without switching to the REPL.

**Why this priority**: Nice-to-have context display. The REPL client already shows this information, so it is supplementary.

**Independent Test**: Can be tested by checking the viewer displays the simulation time value and running/paused indicator matching the streamed state.

**Acceptance Scenarios**:

1. **Given** the viewer is connected, **When** simulation state is received, **Then** the current simulation time is displayed.
2. **Given** the simulation is running, **When** the user looks at the viewer, **Then** a running indicator is visible. When the simulation is paused, a paused indicator is shown instead.

---

### Edge Cases

- What happens when the viewer starts before the simulation has sent any state? The viewer should show an empty scene and begin rendering once state arrives.
- What happens when the server connection drops? The viewer should display the last known state and indicate the connection is lost. It should attempt to reconnect.
- What happens when a body has an unknown or unset shape type? The viewer should render a default fallback shape (e.g., a small sphere) rather than crash.
- What happens when the viewer receives a state with zero bodies? The viewer should display an empty scene (camera, background, status overlay only).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Viewer MUST connect to the server and subscribe to the simulation state stream on startup.
- **FR-002**: Viewer MUST render each body in the streamed state as a 3D shape corresponding to its shape type (sphere, box).
- **FR-003**: Viewer MUST position and orient each rendered body according to its position and orientation (quaternion) from the state.
- **FR-004**: Viewer MUST update the rendered scene each time a new simulation state is received.
- **FR-005**: Viewer MUST apply SetCamera commands by repositioning the camera to the specified position, target, and up vector.
- **FR-006**: Viewer MUST apply SetZoom commands by adjusting the camera zoom to the specified level.
- **FR-007**: Viewer MUST apply ToggleWireframe commands by switching between solid and wireframe rendering modes.
- **FR-008**: Viewer MUST display the current simulation time from the state stream.
- **FR-009**: Viewer MUST indicate whether the simulation is running or paused.
- **FR-010**: Viewer MUST provide a default camera position on startup so the scene is immediately visible without requiring a camera command.
- **FR-011**: Viewer MUST handle late-join gracefully — when connecting to a simulation already in progress, it should render the first state it receives without errors.
- **FR-012**: Viewer MUST differentiate bodies visually by shape type using distinct colors (e.g., spheres one color, boxes another) in addition to geometric shape and size.
- **FR-013**: Viewer MUST register with the Aspire orchestrator for health checks and service discovery.
- **FR-014**: Viewer MUST support interactive mouse/keyboard camera control: click-drag to orbit, scroll to zoom, middle-click to pan.
- **FR-015**: Viewer MUST allow both interactive and REPL-driven camera control to coexist — REPL commands override the current camera state when received.
- **FR-016**: Viewer MUST display a ground reference grid at Y=0 to provide spatial context for body positions.

### Key Entities

- **Rendered Body**: A visual representation of a physics body. Defined by shape type (sphere, box), position (3D vector), orientation (quaternion), and mass (used for relative sizing or labeling).
- **Camera**: The viewpoint into the 3D scene. Defined by position, look-at target, up vector, and zoom level.
- **Scene**: The collection of all rendered bodies plus the camera, ground grid, background, and any overlays (time, status).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All bodies present in the simulation state are visible in the viewer within 1 second of the state being received.
- **SC-002**: Camera commands sent from the client are reflected in the viewer's viewpoint within 1 second.
- **SC-003**: The viewer maintains a responsive display (no freezing or stalling) while receiving continuous state updates during active simulation.
- **SC-004**: Wireframe toggle commands change the rendering mode without requiring a viewer restart.
- **SC-005**: The viewer starts and displays a scene within 5 seconds of launch when the server is already running.
- **SC-006**: The viewer correctly renders scenes containing up to 100 bodies simultaneously.

## Assumptions

- The server already implements `StreamState` and `SendViewCommand` RPCs (delivered in specs 001 and 002).
- The REPL client (spec 003) sends camera and view commands; the viewer only receives them via the server's state and command forwarding.
- Bodies are colored by shape type (one color per shape kind). No user-specified appearance or per-body custom colors are required.
- The viewer runs as a desktop window application, not a web browser application.
- The "up" direction in the 3D scene is the Y-axis by default, consistent with common 3D conventions.
