# Feature Specification: Python Demo Scripts

**Feature Branch**: `004-python-demo-scripts`
**Created**: 2026-03-21
**Status**: Completed
**Input**: User description: "create an equivalent python scripting feature. same as the fsharp one."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run Full Demo Suite Automatically (Priority: P1)

A developer runs the complete Python demo suite to verify the PhysicsSandbox system works end-to-end. They execute `python demos_py/auto_run.py` against a running Aspire stack and see all 15 demos execute sequentially — each exercising different physics scenarios (dropping, bouncing, stacking, bowling, gravity manipulation, stress tests, etc.) — with a pass/fail summary at the end.

**Why this priority**: The automated runner is the primary way to validate the full system stack. It also serves as the entry point for Python users unfamiliar with the project, demonstrating all capabilities in one command.

**Independent Test**: Can be tested by starting the Aspire stack, running `python demos_py/auto_run.py`, and verifying all 15 demos complete with a pass/fail summary.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running, **When** a developer runs `python demos_py/auto_run.py`, **Then** all 15 demos execute sequentially and a pass/fail summary is displayed.
2. **Given** a demo fails during execution, **When** the runner catches the error, **Then** it reports the failure with error details and continues to the next demo.
3. **Given** all demos complete, **When** the summary is displayed, **Then** it shows how many passed and failed out of 15.
4. **Given** the Aspire stack is running on a non-default address, **When** a developer runs `python demos_py/auto_run.py http://custom:5000`, **Then** the runner connects to the specified address.

---

### User Story 2 - Run Individual Demo Script (Priority: P2)

A developer runs a single Python demo script to explore or test a specific physics scenario. They can run any demo independently (e.g., `python demos_py/demo01_hello_drop.py`) and observe the result in the 3D viewer.

**Why this priority**: Individual demo execution enables focused experimentation and debugging. A developer may want to iterate on a single scenario without running the full suite.

**Independent Test**: Can be tested by running any single demo script with `python demos_py/demo01_hello_drop.py` and confirming it executes the scenario end-to-end.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running, **When** a developer runs a single demo script, **Then** the demo resets the scene, creates its physics scenario, runs the simulation, and displays results.
2. **Given** a demo script is run standalone, **When** it completes, **Then** it disconnects from the server cleanly.
3. **Given** a developer wants to use a non-default server address, **When** they pass it as a command-line argument, **Then** the demo connects to the specified address.

---

### User Story 3 - Interactive Demo Runner (Priority: P3)

A developer uses the interactive runner to step through demos one at a time, pressing a key to advance between demos. This allows them to observe each scenario in the 3D viewer before moving to the next.

**Why this priority**: The interactive runner is a presentation and learning tool. It depends on the individual demos and shared infrastructure already working.

**Independent Test**: Can be tested by running `python demos_py/run_all.py` and pressing Enter to step through demos, confirming each one executes before the next prompt.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running, **When** a developer runs `python demos_py/run_all.py`, **Then** the runner displays a demo header and waits for a keypress before executing each demo.
2. **Given** a demo has completed, **When** the developer presses Enter, **Then** the next demo begins.
3. **Given** a demo fails, **When** the error is caught, **Then** the runner displays the error and continues to the next demo on keypress.

---

### Edge Cases

- What happens when the Aspire stack is not running? Demo scripts fail at the connection step with a clear gRPC connection error message.
- What happens when the proto stubs are not generated? An import error is raised with a clear message indicating the stubs need to be generated from the `.proto` files.
- What happens when a demo creates bodies that persist between runs? Each demo calls `reset_simulation` which pauses the simulation, clears all bodies, resets ID counters, adds a ground plane, and restores standard gravity.
- What happens when the batch command limit (100) is exceeded? The `batch_add` helper automatically chunks commands into groups of 100, matching the F# implementation behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide 15 Python demo scripts covering the same scenarios as the existing F# demos (Hello Drop, Bouncing Marbles, Crate Stack, Bowling Alley, Marble Rain, Domino Row, Spinning Tops, Gravity Flip, Billiards, Chaos Scene, Body Scaling, Collision Pit, Force Frenzy, Domino Cascade, Overload).
- **FR-002**: Each demo MUST be self-contained: reset the scene, configure camera, create bodies, run simulation for a set duration, and display results.
- **FR-003**: System MUST provide an automated runner (`auto_run.py`) that executes all demos sequentially and reports pass/fail results with error details.
- **FR-004**: System MUST provide an interactive runner (`run_all.py`) that lets the user step through demos with keypress advancement.
- **FR-005**: Demos MUST communicate with the PhysicsServer via gRPC using Python-generated protobuf stubs from the existing `.proto` contract files.
- **FR-006**: System MUST provide a shared prelude module (`prelude.py`) with common helpers equivalent to the F# Prelude: `reset_simulation`, `run_for`, `sleep`, `to_vec3`, `make_sphere_cmd`, `make_box_cmd`, `make_impulse_cmd`, `make_torque_cmd`, `timed`, `batch_add`, `next_id`.
- **FR-007**: Demos MUST be runnable via `python <script>.py` without requiring any compilation step beyond building the .NET solution (for the running server).
- **FR-008**: System MUST provide Python equivalents of PhysicsClient library functions used by demos: session connect/disconnect, play, pause, simulation reset, body listing, status display, camera control, gravity control, wireframe toggle, preset bodies (bowling ball, boulder), generators (stack, pyramid, row, grid, random spheres), steering (push, launch), and batch commands.
- **FR-009**: Each demo script MUST accept an optional server address argument, defaulting to `http://localhost:5000`.
- **FR-010**: The `batch_add` helper MUST automatically split command lists exceeding 100 items into multiple batches, matching the F# behavior.

### Key Entities

- **Demo**: A self-contained physics scenario with a name, description, and run function that operates on a gRPC channel/session.
- **Prelude**: Shared module providing gRPC channel setup, protobuf message construction helpers, and common utility functions used by all demos.
- **Runner**: An orchestrator (`auto_run.py` or `run_all.py`) that executes multiple demos sequentially with error handling and reporting.
- **Session**: A gRPC channel wrapper providing connection management and method calls to the PhysicsServer.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 15 Python demos execute successfully against a running Aspire stack, producing the same visual physics behavior as the equivalent F# demos.
- **SC-002**: The automated runner completes all 15 demos and reports results within 3 minutes.
- **SC-003**: Each Python demo produces visible physics behavior in the 3D viewer that matches the F# equivalent (same body types, positions, forces, camera angles).
- **SC-004**: A Python developer unfamiliar with F# can run and understand any demo from its script alone, without needing to reference the F# version.
- **SC-005**: The Python demos require no .NET tooling to run — only Python and its dependencies.

## Assumptions

- The Aspire stack is running and accessible at the default address before demos are executed.
- Python 3.10+ is available in the development environment.
- The gRPC proto stubs can be generated from the existing `.proto` files in `PhysicsSandbox.Shared.Contracts` using standard Python protobuf/gRPC tooling.
- Demo timing (e.g., `run_for(session, 3.0)`) assumes simulation runs at approximately real-time speed, matching the F# behavior.
- The Python demo directory (`demos_py/`) is separate from the F# demo directory (`demos/`) to avoid confusion.
- Demos are informal — they do not have their own unit tests. The demos themselves serve as end-to-end smoke tests, matching the F# approach.
