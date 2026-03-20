# Feature Specification: Client REPL Library

**Feature Branch**: `004-client-repl`
**Created**: 2026-03-20
**Status**: Completed
**Input**: User description: "the next service is the Client. a repl environment to control the simulation and viewer. it should just be a library that can be loaded and have all necessary functions to start the connection.... it should have a comprehensive library to conveniently create bodies, apply forces, torques... ready made bodies that just can be added. convenient steering functionality for bodies. functions with rng to create varied bodies. tui for displaying the state/filtering...."

## User Scenarios & Testing

### User Story 1 - Connect and Control Simulation (Priority: P1)

A user loads the client library in an F# interactive session (FSI) or script. They call a single function to connect to the server. Once connected, they use intuitive functions to add bodies, apply forces, and control simulation playback (play, pause, step). All commands return immediate acknowledgement so the user knows they succeeded.

**Why this priority**: Without connection and basic command-sending, nothing else works. This is the foundation that all other stories depend on.

**Independent Test**: Can be fully tested by loading the library, connecting to the server, adding a sphere, and stepping the simulation — delivers the core value of programmatic simulation control.

**Acceptance Scenarios**:

1. **Given** a running server, **When** the user calls the connect function with the server address, **Then** a connection is established and the user receives a session handle for issuing commands.
2. **Given** an active connection, **When** the user calls an add-body function with shape, position, and mass, **Then** the body is created in the simulation and an acknowledgement is returned.
3. **Given** an active connection, **When** the user calls play, pause, or step, **Then** the simulation responds accordingly and acknowledges the command.
4. **Given** an active connection, **When** the user applies a force, impulse, or torque to a body by ID, **Then** the effect is applied in the simulation.
5. **Given** no running server, **When** the user attempts to connect, **Then** the library returns a clear error indicating the server is unreachable.

---

### User Story 2 - Ready-Made Body Builders (Priority: P2)

The user wants to quickly populate scenes without manually specifying every parameter. The library provides a catalogue of pre-configured body creation functions — common objects with sensible defaults (e.g., a "bowling ball", a "crate", a "marble"). The user can also generate randomized variations to create interesting, varied scenes with a single call.

**Why this priority**: Convenience functions dramatically reduce boilerplate and make the REPL experience enjoyable. Without them, every body requires specifying all parameters manually.

**Independent Test**: Can be tested by calling ready-made body functions and verifying bodies appear in the simulation with the expected properties (shape, mass, position).

**Acceptance Scenarios**:

1. **Given** an active connection, **When** the user calls a ready-made body function (e.g., bowling ball, crate, marble), **Then** a body is added with appropriate pre-configured shape, mass, and size.
2. **Given** an active connection, **When** the user calls a random body generator specifying a count (e.g., 10 random spheres), **Then** that many bodies are created with varied positions, sizes, and masses within reasonable bounds.
3. **Given** an active connection, **When** the user calls a scene builder function (e.g., stack of 5, pyramid of 4 layers, grid), **Then** bodies are arranged in the specified formation.
4. **Given** a ready-made body function, **When** the user provides optional overrides (e.g., position or mass), **Then** those overrides replace the defaults while other parameters retain their pre-configured values.

---

### User Story 3 - Body Steering and Motion Control (Priority: P2)

The user wants convenient functions to "steer" bodies — push them in a direction, launch them at a target, spin them, or stop them. These are higher-level wrappers around raw force/impulse/torque commands that express intent rather than physics vectors.

**Why this priority**: Raw force vectors are unintuitive. Steering functions let users express intent ("push this body north", "launch toward target") without calculating vectors manually.

**Independent Test**: Can be tested by adding a body, calling steering functions, and observing the resulting velocity/position changes in the state stream.

**Acceptance Scenarios**:

1. **Given** a body in the simulation, **When** the user calls a directional push function with a direction and magnitude, **Then** an appropriate force or impulse is applied in that direction.
2. **Given** a body in the simulation, **When** the user calls a launch function specifying a target position, **Then** an impulse is calculated and applied to move the body toward that target.
3. **Given** a body in the simulation, **When** the user calls a spin function, **Then** a torque is applied to rotate the body.
4. **Given** a body in the simulation, **When** the user calls a stop function, **Then** forces are cleared and a counter-impulse is applied to halt the body.

---

### User Story 4 - State Display and Monitoring (Priority: P3)

The user wants to see what's happening in the simulation from the REPL. The library provides functions to query the current state, list bodies, inspect individual body properties, and optionally subscribe to a live text-based state feed with filtering. The display is formatted for easy reading in a terminal.

**Why this priority**: Visibility into simulation state is essential for meaningful interaction, but the 3D viewer already provides visual feedback. The REPL state display complements this with precise numeric data.

**Independent Test**: Can be tested by adding bodies, stepping the simulation, and calling state-query functions to verify the returned data matches expected values.

**Acceptance Scenarios**:

1. **Given** an active connection with bodies in the simulation, **When** the user calls a list-bodies function, **Then** a formatted table of all bodies with their positions, velocities, and shapes is displayed.
2. **Given** an active connection, **When** the user calls an inspect function with a body ID, **Then** detailed properties of that body are displayed (position, velocity, angular velocity, orientation, mass, shape).
3. **Given** an active connection, **When** the user starts a live-watch with an optional filter (e.g., only bodies moving faster than a threshold), **Then** matching state updates are printed to stdout until the user cancels the watch to return to the REPL.
4. **Given** an active connection, **When** the user requests state and no bodies exist, **Then** a clear "no bodies" message is shown rather than an empty or confusing display.

---

### User Story 5 - Viewer Control from REPL (Priority: P3)

The user wants to control the 3D viewer's camera and display settings from the REPL — set camera position and look-at target, adjust zoom, toggle wireframe mode. This enables scripted camera animations and consistent viewpoints for demonstrations.

**Why this priority**: The viewer already supports interactive camera input. REPL-based camera control adds scripted/precise positioning but is not essential for basic operation.

**Independent Test**: Can be tested by calling camera/view functions and verifying the corresponding view commands are sent to the server.

**Acceptance Scenarios**:

1. **Given** an active connection, **When** the user calls a camera function with position and target, **Then** a set-camera command is sent to the viewer via the server.
2. **Given** an active connection, **When** the user calls a wireframe toggle function, **Then** a toggle-wireframe command is sent.
3. **Given** an active connection, **When** the user calls a zoom function with a level, **Then** a set-zoom command is sent.

---

### Edge Cases

- What happens when the user sends commands after the server connection drops? The library detects disconnection and returns clear errors. No auto-reconnect — the user calls a reconnect function explicitly when ready.
- What happens when the user references a body ID that doesn't exist? The command acknowledgement from the server should be surfaced as an error message.
- What happens when the user calls random body generators with zero or negative counts? The library should validate inputs and return helpful error messages.
- What happens when the state stream has no updates for an extended period? The state display should show the last known state with a timestamp indicating staleness.
- What happens when multiple REPL sessions connect to the same server? Each session should work independently — the server already supports multiple clients on StreamState.

## Requirements

### Functional Requirements

- **FR-001**: The library MUST provide a connect function that establishes a connection to the server and returns a session handle used by all other functions.
- **FR-002**: The library MUST provide functions for all simulation commands: add body (sphere, box, plane), remove body, apply force, apply impulse, apply torque, clear forces, set gravity, play, pause, and step.
- **FR-002a**: The library MUST provide a clear-all function that removes all bodies the session has created by sending individual remove commands for each tracked body ID.
- **FR-003**: The library MUST provide ready-made body creation functions with pre-configured defaults for common objects (at least 5 distinct presets). Body IDs are auto-generated with human-readable names (e.g., "sphere-1", "box-3") and can be optionally overridden by the user.
- **FR-004**: The library MUST provide randomized body generation functions that produce varied bodies (randomized position, size, mass) within configurable bounds.
- **FR-005**: The library MUST provide scene-builder functions that create arrangements of bodies (at least 3 patterns such as stacks, rows, and grids).
- **FR-006**: The library MUST provide steering functions that translate user intent (direction, target, spin, stop) into appropriate force/impulse/torque commands.
- **FR-007**: The library MUST provide functions to query the current simulation state: list all bodies, inspect a single body, and get simulation time and running status.
- **FR-008**: The library MUST provide a cancellable live-watch mode that prints state updates to stdout with optional filtering (by body ID, shape type, or velocity threshold), returning control to the REPL when cancelled.
- **FR-009**: The library MUST provide functions for viewer control: set camera, set zoom, and toggle wireframe.
- **FR-010**: The library MUST be loadable in F# Interactive (FSI) and F# scripts without requiring a full application build.
- **FR-011**: The library MUST surface command acknowledgements and errors from the server as return values, not exceptions, for REPL-friendly usage.
- **FR-012**: The library MUST provide formatted text output for state queries suitable for terminal display (aligned columns, readable numbers).

### Key Entities

- **Session**: Represents an active connection to the server. Holds the communication channel, state subscription, and a registry of body IDs created during the session (used by clear-all). All command and query functions operate through a session.
- **BodyPreset**: A named, pre-configured set of body parameters (shape, mass, size) that can be instantiated with optional position and velocity overrides.
- **StateSnapshot**: A point-in-time view of the simulation state — all bodies, simulation time, and running status — used for display and inspection.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A user can go from loading the library to having a body in a running simulation in under 5 function calls.
- **SC-002**: All ready-made body presets produce valid bodies that appear correctly in the simulation and viewer.
- **SC-003**: Randomized body generators produce bodies with visibly different properties (no two identical bodies in a batch of 10+).
- **SC-004**: State queries return complete, accurate data matching what the simulation and viewer show.
- **SC-005**: All 9 simulation commands and 3 view commands are accessible through dedicated library functions.
- **SC-006**: The library loads and connects successfully in an F# Interactive session.
- **SC-007**: Steering functions produce observable motion in the expected direction when viewed in the 3D viewer.

## Clarifications

### Session 2026-03-20

- Q: What is the scope of the state display — full interactive TUI, print functions with live-watch, or snapshots only? → A: Formatted print functions plus a cancellable live-watch mode (prints state updates to stdout, user stops it to return to REPL). No full interactive/ncurses-style TUI.
- Q: Who generates body IDs for ready-made and random body functions? → A: Library auto-generates human-readable IDs (e.g., "sphere-1", "box-3") with optional user override.
- Q: Should the library auto-reconnect on disconnect or require explicit user action? → A: No auto-reconnect. Detect disconnect, return errors on subsequent calls, user calls reconnect explicitly.
- Q: Should the library provide a reset/clear-all function? → A: Yes, client-side clear-all. The library tracks created body IDs and sends RemoveBody for each. No new proto command needed.

## Assumptions

- The library targets F# Interactive (FSI) as its primary usage environment. It is structured as a loadable library, not a standalone executable.
- The server address defaults to the Aspire service discovery URL but can be overridden for standalone usage.
- Random number generation uses a seedable RNG so that users can reproduce specific randomized scenes.
- State display formatting targets 80-column terminals with monospace fonts.
- The library manages its own background state subscription and caches the latest state for synchronous query functions.
- Error handling favors Result types over exceptions to keep the REPL experience smooth (no unhandled exception stack traces cluttering the session).
