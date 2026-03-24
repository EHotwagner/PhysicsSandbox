# Feature Specification: Smooth Camera Controls and Demo Narration

**Feature Branch**: `004-camera-smooth-demos`
**Created**: 2026-03-24
**Status**: Completed
**Input**: User description: "improve the camera capabilities. smooth moving, look at. research native stride3ds capabilities and add/wrap them. add a longer 40sec demo showing them off. also add a ui text label on the left describing what is currently going on. add that as general demo feature."

## Clarifications

### Session 2026-03-24

- Q: Which body-relative camera modes should be supported? → A: All three (lookAt, follow, orbit) plus chase(bodyId, offset), frameBodies(bodyIds), and shake(intensity, duration).
- Q: For follow and chase modes, should the camera continuously track the body each frame or just move once? → A: True continuous tracking — camera updates target/position every frame while mode is active.
- Q: How should continuous modes (follow, chase, orbit) be cancelled? → A: By any new camera command, manual mouse input, or explicit stop command.
- Q: For frameBodies, should it support more than 2 bodies? → A: Variable list of 1 or more body IDs.
- Q: Should orbit use fixed speed or duration? → A: Duration-based by default, with optional partial arc (degrees + duration).

### Session 2026-03-24 (existing demo enhancements)

- Q: Should all 21 existing demos be enhanced, or a prioritized subset? → A: All 21 demos get camera moves + narration labels.
- Q: Should the Python demos mirror the F# demo enhancements? → A: Yes, all 21 Python demos updated to match their F# counterparts (42 total scripts).
- Q: What depth of camera work per demo? → A: Full cinematic — each demo gets a unique multi-shot camera sequence (3-6 moves per demo) with narration.
- Q: Should existing camera logic in demos be replaced or kept alongside new system? → A: Replace — migrate all existing camera calls to use new smooth/body-relative system.
- Q: Should demo durations be extended to accommodate camera sequences? → A: No constraint — demos can be as long as their cinematic sequences require.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Smooth Camera Transitions (Priority: P1)

As a demo author or MCP user, I want to command the camera to smoothly move to a new position/target over a specified duration, so that camera transitions feel cinematic rather than jarring instant teleports.

**Why this priority**: Smooth camera movement is the core capability that all other stories depend on. Without interpolation, the viewer feels broken during scripted sequences. This is the foundational building block.

**Independent Test**: Can be fully tested by issuing a camera command with a duration and visually confirming the camera glides from its current position to the new one over the specified time. Delivers immediate visual polish to every existing demo.

**Acceptance Scenarios**:

1. **Given** the camera is at position A looking at target A, **When** a smooth camera command is issued with position B, target B, and a 2-second duration, **Then** the camera smoothly interpolates from A to B over 2 seconds with no visible stuttering or jumps.
2. **Given** a smooth camera transition is in progress, **When** a new smooth camera command is issued, **Then** the previous transition is cancelled and the new one begins from the camera's current interpolated position.
3. **Given** a smooth camera command is issued with zero or no duration, **When** the viewer processes it, **Then** the camera snaps instantly (preserving current behavior).
4. **Given** a smooth camera transition is in progress, **When** the user interacts with mouse orbit/pan/zoom, **Then** the smooth transition is cancelled and manual control takes over immediately.

---

### User Story 2 - Body-Relative Camera Modes (Priority: P1)

As a demo author, I want to command the camera to look at, follow, orbit, or chase a specific body by its ID, so that I can create cinematic sequences that track physics objects without computing positions manually.

**Why this priority**: Body-relative commands are the primary way demo authors will use the camera. Computing absolute positions for moving bodies is impractical — these modes make the camera system usable for real demos. Co-equal with smooth transitions as a foundational capability.

**Independent Test**: Can be fully tested by creating a body, issuing a lookAt command with its ID, and confirming the camera orients toward the body. Then drop the body and issue a follow command to confirm continuous tracking.

**Acceptance Scenarios**:

1. **Given** a body exists in the scene, **When** a lookAt(bodyId) command is issued, **Then** the camera smoothly reorients to face the body's current position.
2. **Given** a body is moving, **When** a follow(bodyId) command is issued, **Then** the camera continuously tracks the body each frame, keeping it centered in view.
3. **Given** a body exists, **When** an orbit(bodyId, duration) command is issued, **Then** the camera revolves around the body completing the arc over the specified duration.
4. **Given** a body is moving, **When** a chase(bodyId, offset) command is issued, **Then** the camera follows the body maintaining the specified relative offset, updating every frame.
5. **Given** multiple bodies exist, **When** a frameBodies(bodyId1, bodyId2, ...) command is issued, **Then** the camera positions itself to keep all specified bodies in view.
6. **Given** a collision occurs, **When** a shake(intensity, duration) command is issued, **Then** the camera shakes with the specified intensity for the specified duration.
7. **Given** follow mode is active, **When** the user moves the mouse to orbit/pan, **Then** follow mode is cancelled and manual control takes over.
8. **Given** follow mode is active, **When** any new camera command is issued, **Then** follow mode is cancelled and the new command takes effect.
9. **Given** follow mode is active, **When** an explicit stop command is issued, **Then** follow mode is cancelled and the camera holds its current position.
10. **Given** an orbit command specifies 180 degrees over 3 seconds, **When** executed, **Then** the camera completes a half-orbit (not full revolution) in 3 seconds.

---

### User Story 3 - Demo Narration Labels (Priority: P2)

As a demo viewer, I want to see a text label on the left side of the screen that describes what is currently happening in the demo, so that I can understand each phase of a demo without reading source code.

**Why this priority**: Narration labels make demos self-explanatory and are a general-purpose enhancement benefiting all demos. This is independent of camera smoothing and delivers standalone value.

**Independent Test**: Can be fully tested by running any demo that sends narration text and confirming the label appears, updates, and clears at appropriate times. Delivers immediate comprehension improvement.

**Acceptance Scenarios**:

1. **Given** a demo is running, **When** a narration label command is sent with text "Dropping a sphere from 10m", **Then** the text appears on the left side of the viewer screen, clearly readable against any background.
2. **Given** a narration label is currently displayed, **When** a new narration label command is sent, **Then** the previous text is replaced with the new text.
3. **Given** a narration label is displayed, **When** a clear narration command is sent (empty text), **Then** the label disappears.
4. **Given** no demo is running, **When** the viewer is in free mode, **Then** no narration label is shown.

---

### User Story 4 - Smooth Camera in Scripting Library (Priority: P2)

As a demo script author (F# or Python), I want convenient helper functions for smooth camera moves, body-relative modes, and narration labels, so that I can easily create polished demos without low-level gRPC calls.

**Why this priority**: Equal priority to narration labels — script authors need ergonomic wrappers to actually use the new capabilities. Without helpers, adoption in demos will be slow.

**Independent Test**: Can be fully tested by writing a short script that calls the smooth camera helper, a body-relative mode, and the narration helper, confirming all work end-to-end.

**Acceptance Scenarios**:

1. **Given** a demo script imports the standard prelude, **When** the author calls a smooth camera function with position, target, and duration, **Then** the viewer camera smoothly transitions as expected.
2. **Given** a demo script imports the standard prelude, **When** the author calls lookAt/follow/orbit/chase/frameBodies/shake with body IDs, **Then** the viewer executes the corresponding camera mode.
3. **Given** a demo script imports the standard prelude, **When** the author calls a narration function with display text, **Then** the viewer shows the narration label.
4. **Given** existing demo helper patterns (F# Prelude.fsx, Python prelude.py), **When** new camera/narration helpers are added, **Then** they follow the same naming conventions and ergonomic patterns as existing helpers.

---

### User Story 5 - Camera Showcase Demo (Priority: P3)

As a user exploring the sandbox, I want a dedicated ~40-second demo that showcases smooth camera movements, body-relative modes, and narration labels explaining each transition, so I can see the camera system's full capabilities.

**Why this priority**: This demo serves as both a showcase and integration test for all other stories. It depends on them being implemented first but provides the "wow factor" that demonstrates the system's polish.

**Independent Test**: Can be fully tested by running the demo and confirming it plays for approximately 40 seconds, shows multiple distinct camera movements (including body-relative modes), and displays narration labels describing each phase.

**Acceptance Scenarios**:

1. **Given** the physics server is running, **When** the camera showcase demo is launched, **Then** it creates a scene with multiple bodies and runs for approximately 40 seconds with at least 8 distinct camera movements.
2. **Given** the camera showcase demo is playing, **When** each camera transition begins, **Then** a narration label appears describing the current movement (e.g., "Following the falling sphere", "Orbiting the tower").
3. **Given** the demo includes body-relative modes, **When** follow/orbit/chase/frameBodies are demonstrated, **Then** each mode is showcased with at least one body-relative command and a narration label.
4. **Given** the camera showcase demo completes, **When** it finishes, **Then** the narration label is cleared and the camera is left at a reasonable default viewing position.

---

### User Story 6 - Enhance All Existing Demos (Priority: P3)

As a user running any demo, I want every existing demo (1-21) to feature cinematic camera work and narration labels that guide me through the action, so that every demo feels polished and self-explanatory.

**Why this priority**: This is the largest task by volume (42 scripts: 21 F# + 21 Python) but depends on all camera infrastructure (stories 1-4) being complete. It transforms the entire demo suite from functional to cinematic.

**Independent Test**: Can be tested per-demo — run any enhanced demo and confirm it has narration labels describing each phase, 3-6 smooth/body-relative camera moves, and no leftover instant camera calls.

**Acceptance Scenarios**:

1. **Given** any existing demo (1-21), **When** it is run after enhancement, **Then** it includes narration labels describing each phase of the demo.
2. **Given** any existing demo (1-21), **When** it is run after enhancement, **Then** it includes 3-6 unique camera moves using smooth transitions and/or body-relative modes appropriate to the demo's content.
3. **Given** any demo that previously used instant camera calls (e.g., Demo10_Chaos camera sweeps), **When** it is run after enhancement, **Then** all camera calls use the new smooth/body-relative system — no legacy instant calls remain.
4. **Given** the F# version of a demo has been enhanced, **When** the corresponding Python demo is run, **Then** it produces an equivalent cinematic experience with matching camera sequences and narration.
5. **Given** an enhanced demo, **When** it is run, **Then** its duration is unconstrained — the demo runs as long as its cinematic sequence requires.

---

### Edge Cases

- What happens when multiple smooth camera commands arrive within a single frame? The latest command wins; earlier ones are discarded.
- How does the system handle very short durations (e.g., 0.01 seconds)? Treated as near-instant; completes on next frame.
- What happens if the viewer window is resized during a smooth transition? Transition continues unaffected (camera position/target are independent of viewport).
- What if narration text is very long? Text wraps or truncates at a reasonable width to avoid overlapping scene content.
- What happens when a smooth move is issued but the camera is already at the target? No visible transition occurs; command completes immediately.
- What happens when lookAt/follow/orbit/chase references a body ID that doesn't exist? Command is ignored; camera holds current position.
- What happens when a followed body is destroyed mid-follow? Continuous mode is cancelled; camera holds its last position.
- What happens when frameBodies is called with a single body ID? Behaves like lookAt — camera positions to view that one body.
- What happens when orbit is called with 0 degrees? No movement; command completes immediately.
- What happens when shake is issued during a smooth transition? Shake applies on top of the transition (additive effect).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support smooth camera position and target interpolation over a caller-specified duration in seconds.
- **FR-002**: System MUST use easing (not linear interpolation) for camera transitions so movements feel natural — slow start, smooth stop.
- **FR-003**: System MUST cancel any in-progress smooth transition or continuous mode when a new camera command is received, when the user manually interacts with the camera, or when an explicit stop command is sent.
- **FR-004**: System MUST preserve existing instant camera behavior when no duration is specified (backward compatible).
- **FR-005**: System MUST support a narration label displayed on the left side of the viewer screen, separate from the existing demo name/description overlay and status bar.
- **FR-006**: System MUST allow narration labels to be set and cleared via the same command transport used for other viewer commands.
- **FR-007**: System MUST provide scripting helper functions for smooth camera moves in both F# and Python demo preludes.
- **FR-008**: System MUST provide scripting helper functions for setting and clearing narration labels in both F# and Python demo preludes.
- **FR-009**: System MUST include a camera showcase demo of approximately 40 seconds that demonstrates smooth moves, body-relative modes, and narration labels.
- **FR-010**: System MUST ensure narration labels are readable against varying scene backgrounds (e.g., using text shadow, outline, or semi-transparent backdrop).
- **FR-011**: System MUST support smooth zoom transitions (not just position/target) over a specified duration.
- **FR-012**: System MUST support a lookAt(bodyId) command that smoothly orients the camera to face a specified body.
- **FR-013**: System MUST support a follow(bodyId) command that continuously tracks a body each frame, keeping it centered in view.
- **FR-014**: System MUST support an orbit(bodyId, duration, degrees) command that revolves the camera around a body, completing the specified arc over the given duration. Defaults to 360° if degrees is omitted. Orbit radius is the camera's current distance from the body at the time the command is received.
- **FR-015**: System MUST support a chase(bodyId, offset) command that continuously follows a body while maintaining a fixed relative offset from it.
- **FR-016**: System MUST support a frameBodies(bodyIds) command that auto-positions the camera to keep a variable list of one or more bodies in view.
- **FR-017**: System MUST support a shake(intensity, duration) command that applies a camera shake effect for the specified duration.
- **FR-018**: System MUST gracefully handle body-relative commands referencing non-existent or destroyed bodies by cancelling the mode and holding the camera's current position.
- **FR-019**: System MUST provide scripting helper functions for all body-relative camera modes (lookAt, follow, orbit, chase, frameBodies, shake) in both F# and Python demo preludes.
- **FR-020**: All 21 existing F# demos MUST be enhanced with full cinematic camera sequences (3-6 unique camera moves per demo) and narration labels describing each phase.
- **FR-021**: All 21 existing Python demos MUST be enhanced to match their F# counterparts with equivalent camera sequences and narration.
- **FR-022**: All existing instant camera calls in demos (e.g., Demo10_Chaos camera sweeps) MUST be migrated to the new smooth/body-relative camera system — no legacy instant calls may remain.

### Key Entities

- **Camera Transition**: Represents an in-progress smooth camera movement with start state, end state, duration, elapsed time, and easing function.
- **Continuous Camera Mode**: An active body-tracking mode (follow, chase, orbit, frameBodies) that updates the camera each frame until cancelled.
- **Narration Label**: A text string displayed at a fixed screen position during demos, updated via viewer commands.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Smooth camera transitions complete within ±0.1 seconds of the specified duration at 60 FPS.
- **SC-002**: Camera transitions produce no visible stuttering or frame drops during movement (maintains consistent frame rate).
- **SC-003**: Narration labels appear within one frame of the command being received by the viewer.
- **SC-004**: The camera showcase demo runs for 35-45 seconds and includes at least 8 distinct camera movements (including body-relative modes) with accompanying narration.
- **SC-005**: All 42 enhanced demo scripts (21 F# + 21 Python) run successfully with camera moves and narration labels.
- **SC-006**: Demo authors can add a smooth camera move or body-relative mode with a single function call (no multi-step setup required).
- **SC-007**: Continuous camera modes (follow, chase) track moving bodies with no perceptible lag (updates every frame).
- **SC-008**: Each enhanced demo contains 3-6 camera moves and narration labels for every distinct phase.
- **SC-009**: No legacy instant camera calls remain in any demo script after enhancement.

## Assumptions

- The existing ViewCommand transport (proto → server auto-forward → viewer) is the appropriate channel for smooth camera and body-relative commands. New message variants will be added for body-relative modes.
- Easing will use a standard ease-in-out curve (e.g., smoothstep or cubic). No need for configurable easing functions in the initial implementation.
- The narration label position will be on the left side, below the existing demo name overlay and status bar, to avoid overlap.
- The camera showcase demo will create its own scene with multiple bodies rather than depending on a pre-existing scene state.
- The narration label is a single line or short paragraph — rich text formatting is not required.
- Frame-based interpolation using delta time is sufficient for smooth transitions; no separate timer thread is needed.
- Body-relative commands resolve body positions using the same body name/ID scheme already shared between client and viewer.
- The viewer already receives body positions via the state stream, so no additional server queries are needed for body-relative camera modes.
- Demo durations are unconstrained — each demo can run as long as its cinematic sequence requires. No artificial time limits.
- Camera sequences for each demo will be tailored to the demo's specific content (e.g., Demo03_CrateStack gets a tower-tracking orbit; Demo09_Billiards gets a table-level follow shot).
