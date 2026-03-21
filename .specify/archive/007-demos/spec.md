# Feature Specification: Demo Scripts

**Feature Branch**: `007-demos`
**Created**: 2026-03-21
**Status**: Completed (backfill from existing code)
**Input**: Backfilled from existing `demos/` directory containing 10 interactive FSI physics demos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run Demo Catalogue (Priority: P1)

A developer runs the demo suite to verify the full PhysicsSandbox system works end-to-end. They execute `dotnet fsi demos/AutoRun.fsx` against a running Aspire stack and see 10 demos execute sequentially — each exercising different physics scenarios (dropping, bouncing, stacking, bowling, gravity manipulation, etc.) — with a pass/fail summary at the end.

**Why this priority**: The demo catalogue serves as both a showcase and a smoke test. If all 10 demos pass, the full stack (server, simulation, viewer, client library) is working correctly.

**Independent Test**: Can be tested by starting the Aspire stack, running `dotnet fsi demos/AutoRun.fsx`, and verifying all 10 demos complete without errors.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running, **When** a developer runs `dotnet fsi demos/AutoRun.fsx`, **Then** all 10 demos execute sequentially and a pass/fail summary is displayed.
2. **Given** a demo fails during execution, **When** the runner catches the error, **Then** it reports the failure and continues to the next demo.
3. **Given** all demos complete, **When** the summary is displayed, **Then** it shows how many passed and failed out of 10.

---

### User Story 2 - Run Individual Demo (Priority: P2)

A developer loads a specific demo in FSI to explore or modify a single physics scenario interactively. They can run the demo, observe the result in the 3D viewer, and tweak parameters in real time.

**Why this priority**: Individual demo execution enables experimentation and learning. It depends on the shared infrastructure (Prelude) being in place.

**Independent Test**: Can be tested by loading any single demo script in FSI and calling its `run` function with an active session.

**Acceptance Scenarios**:

1. **Given** an active session, **When** a developer loads a demo script and calls its run function, **Then** the demo executes its physics scenario and the result is visible in the viewer.
2. **Given** a demo has run, **When** the developer modifies parameters and re-runs, **Then** the scene resets and the modified scenario plays.

---

### Edge Cases

- What happens when the Aspire stack is not running? Demo scripts fail at the connection step with a clear error from the Session module.
- What happens when a demo creates bodies that persist between runs? Each demo calls `resetScene` which pauses, clears all bodies, resets ID counters, adds a ground plane, and sets standard gravity.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide at least 10 demo scripts covering distinct physics scenarios (dropping, bouncing, stacking, bowling, random generation, dominos, spinning, gravity manipulation, billiards, chaos).
- **FR-002**: Each demo MUST be self-contained: reset the scene, configure camera, create bodies, run simulation for a set duration, and display results.
- **FR-003**: System MUST provide an automated runner (`AutoRun.fsx`) that executes all demos sequentially and reports pass/fail results with error details.
- **FR-004**: Demos MUST use the PhysicsClient library API (Session, SimulationCommands, ViewCommands, Presets, Generators, Steering, StateDisplay).
- **FR-005**: Demos MUST be runnable via `dotnet fsi` without requiring compilation beyond building the solution.
- **FR-006**: System MUST provide a shared prelude (`Prelude.fsx`) with common helpers (`resetScene`, `runFor`, `ok`, `sleep`) to avoid duplication across demo scripts.

### Key Entities

- **Demo**: A self-contained physics scenario with a name, description, and run function that operates on a Session.
- **Prelude**: Shared infrastructure providing library references, module imports, and helper functions used by all demos.
- **Runner**: An orchestrator (`AutoRun.fsx` or `RunAll.fsx`) that executes multiple demos sequentially with error handling and reporting.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 demos execute successfully against a running Aspire stack without manual intervention.
- **SC-002**: The automated runner completes all demos and reports results within 2 minutes.
- **SC-003**: Each demo produces visible physics behavior in the 3D viewer (bodies move, interact, or are arranged).
- **SC-004**: A developer unfamiliar with the project can understand each demo's scenario from its name, description, and inline comments.

## Assumptions

- The PhysicsClient library (spec 004) is built and available as a compiled DLL before demos run.
- The Aspire stack is running and accessible at the default service discovery address.
- Demos are informal — they do not have their own unit tests. The demos themselves serve as end-to-end smoke tests.
- Demo timing (e.g., `runFor s 3.0`) assumes simulation runs at approximately real-time speed.
