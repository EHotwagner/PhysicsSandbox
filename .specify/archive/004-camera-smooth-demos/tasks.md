# Tasks: Smooth Camera Controls and Demo Narration

**Input**: Design documents from `/specs/004-camera-smooth-demos/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/view-commands-proto.md, quickstart.md

**Tests**: Included — spec mentions xUnit unit tests and integration tests.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Proto contract changes and build validation — everything depends on the new messages.

- [X] T001 Add 9 new ViewCommand proto messages (SmoothCamera, CameraLookAt, CameraFollow, CameraOrbit, CameraChase, CameraFrameBodies, CameraShake, CameraStop, SetNarration) to src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto per contracts/view-commands-proto.md
- [X] T002 Build solution (`dotnet build PhysicsSandbox.slnx`) and verify proto codegen succeeds with new message types

**Checkpoint**: Proto contracts compiled — all downstream projects can reference the new types.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core camera interpolation math and CameraMode state machine — MUST be complete before any camera mode or demo work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Extend CameraState with `ActiveMode: CameraMode option` discriminated union (Transitioning, LookingAt, Following, Orbiting, Chasing, Framing, Shaking variants) in src/PhysicsViewer/Rendering/CameraController.fs per data-model.md
- [X] T004 Implement smoothstep easing function (`3t² - 2t³`) and Vector3/float interpolation helpers in src/PhysicsViewer/Rendering/CameraController.fs per research.md R1
- [X] T005 Implement `updateCameraMode` function that advances ActiveMode each frame using delta time, updates Position/Target/Zoom, and transitions to None on completion in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T006 Implement mode cancellation logic — any new command, mouse input, or CameraStop sets ActiveMode to None from current interpolated position in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T007 Update CameraController.fsi signature file to expose new types (CameraMode DU, ActiveMode field) and new functions (smoothstep, updateCameraMode, cancellation) in src/PhysicsViewer/Rendering/CameraController.fsi
- [X] T008 [P] Write unit tests for smoothstep easing (t=0→0, t=0.5→0.5, t=1→1, monotonic) in tests/PhysicsViewer.Tests/
- [X] T009 [P] Write unit tests for CameraMode state transitions (start→complete, start→cancel, mode replacement) in tests/PhysicsViewer.Tests/

**Checkpoint**: Foundation ready — camera state machine and interpolation math validated.

---

## Phase 3: User Story 1 — Smooth Camera Transitions (Priority: P1) 🎯 MVP

**Goal**: Smooth camera position/target/zoom interpolation over a specified duration, replacing instant snaps.

**Independent Test**: Issue a SmoothCamera command with duration > 0 and confirm the camera glides from current position to target over that time.

### Tests for User Story 1

- [X] T010 [P] [US1] Write unit tests for Transitioning mode: interpolation from start→end state, zero duration = instant snap, mid-transition cancellation in tests/PhysicsViewer.Tests/
- [X] T011 [P] [US1] Write unit tests for smoothCameraWithZoom client function: correct proto message construction, zoom_level=0 keeps current in tests/PhysicsClient.Tests/

### Implementation for User Story 1

- [X] T012 [US1] Add SmoothCamera ViewCommand handler in src/PhysicsViewer/Program.fs — create Transitioning mode from current camera state to command target with duration
- [X] T013 [US1] Wire `updateCameraMode` call into the viewer's per-frame update loop (using `float32 time.Elapsed.TotalSeconds` as dt) in src/PhysicsViewer/Program.fs
- [X] T014 [US1] Wire mouse input detection to cancel ActiveMode (set to None, keep current interpolated position) in src/PhysicsViewer/Program.fs
- [X] T015 [P] [US1] Add `smoothCamera` and `smoothCameraWithZoom` client functions in src/PhysicsClient/Commands/ViewCommands.fs per contracts/view-commands-proto.md
- [X] T016 [P] [US1] Update src/PhysicsClient/Commands/ViewCommands.fsi with smoothCamera and smoothCameraWithZoom signatures
- [X] T017 [US1] Write integration test: send SmoothCamera command via gRPC, verify viewer processes it without error in tests/PhysicsSandbox.Integration.Tests/

**Checkpoint**: Smooth camera transitions work end-to-end. Demos can use smooth moves.

---

## Phase 4: User Story 2 — Body-Relative Camera Modes (Priority: P1)

**Goal**: lookAt, follow, orbit, chase, frameBodies, and shake camera modes that track physics bodies by ID.

**Independent Test**: Create a body, issue lookAt with its ID, confirm camera orients toward body. Drop the body and issue follow to confirm continuous tracking.

### Tests for User Story 2

- [X] T018 [P] [US2] Write unit tests for LookingAt mode: smooth orient toward body position, completion after duration in tests/PhysicsViewer.Tests/
- [X] T019 [P] [US2] Write unit tests for Following mode: target updates each frame to body position, cancellation on new command in tests/PhysicsViewer.Tests/
- [X] T020 [P] [US2] Write unit tests for Orbiting mode: angle progress over duration, partial arc (180°), full revolution (360°) in tests/PhysicsViewer.Tests/
- [X] T021 [P] [US2] Write unit tests for Chasing mode: position = body position + offset each frame in tests/PhysicsViewer.Tests/
- [X] T022 [P] [US2] Write unit tests for Framing mode: camera positions to keep all bodies visible, single body = lookAt behavior in tests/PhysicsViewer.Tests/
- [X] T023 [P] [US2] Write unit tests for Shaking mode: additive random offset, duration expiry restores base position in tests/PhysicsViewer.Tests/
- [X] T024 [P] [US2] Write unit tests for invalid body ID handling: non-existent body → mode cancelled, destroyed body mid-follow → mode cancelled in tests/PhysicsViewer.Tests/

### Implementation for User Story 2

- [X] T025 [US2] Build `Map<string, Vector3>` body position lookup from `latestSimState.Bodies` each frame in src/PhysicsViewer/Program.fs per research.md R2
- [X] T026 [US2] Implement LookingAt mode update logic (smooth orient camera target toward body position over duration) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T027 [US2] Implement Following mode update logic (set camera target to body position each frame) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T028 [US2] Implement Orbiting mode update logic (revolve camera around body, angle = startAngle + progress * totalDegrees) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T029 [US2] Implement Chasing mode update logic (set camera position to body position + offset each frame) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T030 [US2] Implement Framing mode update logic (compute bounding center + distance to keep all bodies visible) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T031 [US2] Implement Shaking mode update logic (additive random offset from base, restore on completion) in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T032 [US2] Add body ID validation — if body not found in position map, cancel mode and hold current position in src/PhysicsViewer/Rendering/CameraController.fs
- [X] T033 [US2] Update CameraController.fsi with new mode update function signatures in src/PhysicsViewer/Rendering/CameraController.fsi
- [X] T034 [US2] Add ViewCommand handlers for CameraLookAt, CameraFollow, CameraOrbit, CameraChase, CameraFrameBodies, CameraShake, CameraStop in src/PhysicsViewer/Program.fs
- [X] T035 [P] [US2] Add cameraLookAt, cameraFollow, cameraOrbit, cameraChase, cameraFrameBodies, cameraShake, cameraStop client functions in src/PhysicsClient/Commands/ViewCommands.fs
- [X] T036 [P] [US2] Update src/PhysicsClient/Commands/ViewCommands.fsi with all body-relative mode signatures
- [X] T037 [US2] Write integration test: send body-relative camera commands, verify viewer processes them in tests/PhysicsSandbox.Integration.Tests/

**Checkpoint**: All 6 body-relative camera modes work. Camera tracks physics bodies by ID.

---

## Phase 5: User Story 3 — Demo Narration Labels (Priority: P2)

**Goal**: Text label on the left side of the viewer describing current demo phase, set/cleared via SetNarration command.

**Independent Test**: Send a SetNarration command with text and confirm label appears at (10, 50), then send empty text to confirm it clears.

### Tests for User Story 3

- [X] T038 [P] [US3] Write unit test for SceneManager: applyNarration sets/clears NarrationText on SceneState in tests/PhysicsViewer.Tests/
- [X] T039 [P] [US3] Write unit test for setNarration client function: correct proto message construction in tests/PhysicsClient.Tests/

### Implementation for User Story 3

- [X] T040 [US3] Add `NarrationText: string option` field to SceneState and `applyNarration` function in src/PhysicsViewer/Rendering/SceneManager.fs
- [X] T041 [US3] Update src/PhysicsViewer/Rendering/SceneManager.fsi with NarrationText field and applyNarration signature
- [X] T042 [US3] Add SetNarration ViewCommand handler in src/PhysicsViewer/Program.fs — call applyNarration on SceneState
- [X] T043 [US3] Render narration label via `DebugTextSystem.Print()` at position (10, 50) each frame when NarrationText is Some in src/PhysicsViewer/Program.fs — use a distinct color (e.g., Color.Yellow) for readability against varying backgrounds per FR-010
- [X] T044 [P] [US3] Add setNarration client function in src/PhysicsClient/Commands/ViewCommands.fs
- [X] T045 [P] [US3] Update src/PhysicsClient/Commands/ViewCommands.fsi with setNarration signature

**Checkpoint**: Narration labels display and clear correctly in the viewer.

---

## Phase 6: User Story 4 — Smooth Camera in Scripting Library (Priority: P2)

**Goal**: Convenient helper functions in F# Prelude.fsx and Python prelude.py for all new camera and narration commands.

**Independent Test**: Write a short script calling smoothCamera, lookAtBody, and setNarration helpers — confirm all work end-to-end.

### Implementation for User Story 4

- [X] T046 [P] [US4] Add F# scripting helpers (smoothCamera, lookAtBody, followBody, orbitBody, chaseBody, frameBodies, shakeCamera, stopCamera, setNarration, clearNarration) to Scripting/demos/Prelude.fsx per contracts/view-commands-proto.md scripting API
- [X] T047 [P] [US4] Add Python scripting helpers (smooth_camera, look_at_body, follow_body, orbit_body, chase_body, frame_bodies, shake_camera, stop_camera, set_narration, clear_narration) to Scripting/demos_py/prelude.py per contracts/view-commands-proto.md scripting API
- [X] T048 [US4] Verify helpers work end-to-end by running a quick test script that calls each new helper function

**Checkpoint**: Script authors can use all new camera/narration capabilities with single function calls.

---

## Phase 7: User Story 5 — Camera Showcase Demo (Priority: P3)

**Goal**: Dedicated ~40-second demo showcasing smooth moves, body-relative modes, and narration labels.

**Independent Test**: Run the showcase demo — confirm it plays ~40 seconds, shows 8+ distinct camera movements with narration labels.

### Implementation for User Story 5

- [X] T049 [P] [US5] Create F# camera showcase demo (Demo22_CameraShowcase.fsx) in Scripting/demos/ — scene setup with multiple bodies, 8+ camera moves (smooth, lookAt, follow, orbit, chase, frameBodies, shake), narration for each phase, ~40 seconds total
- [X] T050 [P] [US5] Create Python camera showcase demo (demo22_camera_showcase.py) in Scripting/demos_py/ — matching F# counterpart with equivalent camera sequences and narration
- [X] T051 [US5] Update F# demo runner (run_all.fsx or equivalent) to include Demo22 in Scripting/demos/
- [X] T052 [US5] Update Python demo runner (run_all.py or equivalent) to include demo22 in Scripting/demos_py/

**Checkpoint**: Camera showcase demo plays ~40 seconds with full cinematic camera work and narration.

---

## Phase 8: User Story 6 — Enhance All Existing Demos (Priority: P3)

**Goal**: All 42 demo scripts (21 F# + 21 Python) enhanced with cinematic camera sequences (3-6 moves each) and narration labels.

**Independent Test**: Run any enhanced demo — confirm narration labels describe each phase, 3-6 camera moves, no legacy instant camera calls.

### Implementation for User Story 6 — F# Demos

- [X] T053 [P] [US6] Enhance Demo01_HelloDrop.fsx with cinematic camera + narration in Scripting/demos/
- [X] T054 [P] [US6] Enhance Demo02_TwoSpheres.fsx with cinematic camera + narration in Scripting/demos/
- [X] T055 [P] [US6] Enhance Demo03_CrateStack.fsx with cinematic camera + narration in Scripting/demos/
- [X] T056 [P] [US6] Enhance Demo04_Pendulum.fsx with cinematic camera + narration in Scripting/demos/
- [X] T057 [P] [US6] Enhance Demo05_DominoChain.fsx with cinematic camera + narration in Scripting/demos/
- [X] T058 [P] [US6] Enhance Demo06_Ramp.fsx with cinematic camera + narration in Scripting/demos/
- [X] T059 [P] [US6] Enhance Demo07_NewtonCradle.fsx with cinematic camera + narration in Scripting/demos/
- [X] T060 [P] [US6] Enhance Demo08_ClothDrop.fsx with cinematic camera + narration in Scripting/demos/
- [X] T061 [P] [US6] Enhance Demo09_Billiards.fsx with cinematic camera + narration in Scripting/demos/
- [X] T062 [P] [US6] Enhance Demo10_Chaos.fsx with cinematic camera + narration (migrate legacy instant camera calls) in Scripting/demos/
- [X] T063 [P] [US6] Enhance Demo11_Catapult.fsx with cinematic camera + narration in Scripting/demos/
- [X] T064 [P] [US6] Enhance Demo12_Constraints.fsx with cinematic camera + narration in Scripting/demos/
- [X] T065 [P] [US6] Enhance Demo13_Ragdoll.fsx with cinematic camera + narration in Scripting/demos/
- [X] T066 [P] [US6] Enhance Demo14_StressTest.fsx with cinematic camera + narration in Scripting/demos/
- [X] T067 [P] [US6] Enhance Demo15_Explosion.fsx with cinematic camera + narration in Scripting/demos/
- [X] T068 [P] [US6] Enhance Demo16_Gyroscope.fsx with cinematic camera + narration in Scripting/demos/
- [X] T069 [P] [US6] Enhance Demo17_Gears.fsx with cinematic camera + narration in Scripting/demos/
- [X] T070 [P] [US6] Enhance Demo18_Trebuchet.fsx with cinematic camera + narration in Scripting/demos/
- [X] T071 [P] [US6] Enhance Demo19_ShapeGallery.fsx with cinematic camera + narration in Scripting/demos/
- [X] T072 [P] [US6] Enhance Demo20_CompoundConstructions.fsx with cinematic camera + narration in Scripting/demos/
- [X] T073 [P] [US6] Enhance Demo21_MeshHullPlayground.fsx with cinematic camera + narration in Scripting/demos/

### Implementation for User Story 6 — Python Demos

- [X] T074 [P] [US6] Enhance demo01_hello_drop.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T075 [P] [US6] Enhance demo02_two_spheres.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T076 [P] [US6] Enhance demo03_crate_stack.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T077 [P] [US6] Enhance demo04_pendulum.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T078 [P] [US6] Enhance demo05_domino_chain.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T079 [P] [US6] Enhance demo06_ramp.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T080 [P] [US6] Enhance demo07_newton_cradle.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T081 [P] [US6] Enhance demo08_cloth_drop.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T082 [P] [US6] Enhance demo09_billiards.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T083 [P] [US6] Enhance demo10_chaos.py with cinematic camera + narration (migrate legacy instant camera calls) matching F# counterpart in Scripting/demos_py/
- [X] T084 [P] [US6] Enhance demo11_catapult.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T085 [P] [US6] Enhance demo12_constraints.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T086 [P] [US6] Enhance demo13_ragdoll.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T087 [P] [US6] Enhance demo14_stress_test.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T088 [P] [US6] Enhance demo15_explosion.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T089 [P] [US6] Enhance demo16_gyroscope.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T090 [P] [US6] Enhance demo17_gears.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T091 [P] [US6] Enhance demo18_trebuchet.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T092 [P] [US6] Enhance demo19_shape_gallery.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T093 [P] [US6] Enhance demo20_compound_constructions.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/
- [X] T094 [P] [US6] Enhance demo21_mesh_hull_playground.py with cinematic camera + narration matching F# counterpart in Scripting/demos_py/

**Checkpoint**: All 42 demo scripts enhanced with cinematic camera and narration. No legacy instant camera calls remain.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Validation, surface area updates, and final verification.

- [X] T095 Update PhysicsViewer.Tests surface area baseline for CameraController.fsi (new CameraMode DU, ActiveMode field, updateCameraMode, mode update functions) in tests/PhysicsViewer.Tests/
- [X] T096 Update PhysicsClient.Tests surface area baseline for ViewCommands.fsi (smoothCamera, smoothCameraWithZoom, cameraLookAt, cameraFollow, cameraOrbit, cameraChase, cameraFrameBodies, cameraShake, cameraStop, setNarration) in tests/PhysicsClient.Tests/
- [X] T097 Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and fix any failures
- [X] T098 Verify smooth camera timing accuracy (SC-001: ±0.1s of specified duration at 60 FPS) and no frame drops (SC-002) by running Demo22 and observing transitions
- [X] T099 Run quickstart.md verification — build, start system, run Demo01 and Demo22, confirm camera + narration work

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (proto codegen)
- **US1 Smooth Camera (Phase 3)**: Depends on Phase 2 (CameraMode state machine)
- **US2 Body-Relative (Phase 4)**: Depends on Phase 2 (CameraMode state machine) + Phase 3 (viewer command wiring)
- **US3 Narration Labels (Phase 5)**: Depends on Phase 1 (SetNarration proto) only — can run in parallel with US1/US2
- **US4 Scripting Helpers (Phase 6)**: Depends on Phase 3 + Phase 4 + Phase 5 (all client functions must exist)
- **US5 Camera Showcase (Phase 7)**: Depends on Phase 6 (scripting helpers)
- **US6 Enhance All Demos (Phase 8)**: Depends on Phase 6 (scripting helpers)
- **Polish (Phase 9)**: Depends on all previous phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only — **MVP target**
- **US2 (P1)**: Depends on Foundational + US1 viewer wiring
- **US3 (P2)**: Independent of US1/US2 — can parallel after Phase 1
- **US4 (P2)**: Depends on US1 + US2 + US3 (wraps all client functions)
- **US5 (P3)**: Depends on US4 (uses scripting helpers)
- **US6 (P3)**: Depends on US4 (uses scripting helpers)

### Within Each User Story

- Tests written first (where included)
- Core logic before viewer integration
- Client functions before scripting helpers
- F# and Python helpers can run in parallel

### Parallel Opportunities

- T008 + T009 (foundational tests) can run in parallel
- T010 + T011 (US1 tests) can run in parallel
- T015 + T016 (US1 client functions) can run in parallel with T012-T014 (viewer)
- T018-T024 (US2 tests) can ALL run in parallel
- T035 + T036 (US2 client functions) can run in parallel
- T038 + T039 (US3 tests) can run in parallel
- T044 + T045 (US3 client functions) can run in parallel
- T046 + T047 (US4 F# + Python helpers) can run in parallel
- T049 + T050 (US5 F# + Python showcase) can run in parallel
- T053-T073 (all F# demo enhancements) can ALL run in parallel
- T074-T094 (all Python demo enhancements) can ALL run in parallel
- US3 (narration) can run in parallel with US1/US2 (camera modes)

---

## Parallel Example: User Story 1

```bash
# Launch US1 tests in parallel:
Task: T010 "Unit tests for Transitioning mode in tests/PhysicsViewer.Tests/"
Task: T011 "Unit tests for smoothCamera client function in tests/PhysicsClient.Tests/"

# Launch client functions in parallel with viewer implementation:
Task: T015 "smoothCamera client functions in src/PhysicsClient/Commands/ViewCommands.fs"
Task: T016 "ViewCommands.fsi update"
# (parallel with T012-T014 viewer work)
```

## Parallel Example: User Story 6

```bash
# All 21 F# demo enhancements can launch in parallel:
Task: T053-T073 (all touch different files)

# All 21 Python demo enhancements can launch in parallel:
Task: T074-T094 (all touch different files)

# F# and Python batches can also run in parallel with each other
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Proto contracts
2. Complete Phase 2: CameraMode state machine + interpolation
3. Complete Phase 3: US1 — Smooth camera transitions
4. **STOP and VALIDATE**: Send a SmoothCamera command, confirm smooth glide

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 (US1) → Smooth moves work → **MVP!**
3. Phase 4 (US2) → Body-relative modes work
4. Phase 5 (US3) → Narration labels work (can parallel with 3+4)
5. Phase 6 (US4) → Script helpers ready
6. Phase 7 (US5) → Camera showcase demo
7. Phase 8 (US6) → All 42 demos enhanced
8. Phase 9 → Polish and validation
