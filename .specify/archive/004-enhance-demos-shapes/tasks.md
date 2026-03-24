# Tasks: Enhance Demos with New Shapes and Viewer Labels

**Input**: Design documents from `/specs/004-enhance-demos-shapes/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Test tasks included only for metadata transport infrastructure (constitution Principle VI requires test evidence for behavior-changing code).

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Proto Contract & Build)

**Purpose**: Establish the SetDemoMetadata proto message and regenerate all language bindings

- [x] T001 Add SetDemoMetadata message and ViewCommand field 4 to src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto
- [x] T002 Build contracts project to regenerate C# types: `dotnet build src/PhysicsSandbox.Shared.Contracts/`
- [x] T003 Regenerate Python proto stubs in Scripting/demos_py/generated/

**Checkpoint**: Proto contract established, all language bindings available

---

## Phase 2: Foundational (Client & Viewer Infrastructure)

**Purpose**: Core infrastructure that MUST be complete before any user story demo work

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Add `setDemoMetadata` function to src/PhysicsClient/Commands/ViewCommands.fs that constructs and sends SetDemoMetadata ViewCommand
- [x] T005 Update src/PhysicsClient/Commands/ViewCommands.fsi signature file with setDemoMetadata declaration
- [x] T006 [P] Add DemoName and DemoDescription fields (string option) to SceneState type in src/PhysicsViewer/Rendering/SceneManager.fs
- [x] T007 [P] Update src/PhysicsViewer/Rendering/SceneManager.fsi with new SceneState fields and any accessor functions
- [x] T008 [P] Add `makeMeshCmd` helper function to Scripting/demos/Prelude.fsx (creates Mesh shape from list of triangle vertices)
- [x] T009 [P] Add `make_mesh_cmd` helper function to Scripting/demos_py/prelude.py (equivalent Python mesh builder)
- [x] T010 [P] Add `setDemoInfo` helper to Scripting/demos/Prelude.fsx that wraps SendViewCommand(SetDemoMetadata(...))
- [x] T011 [P] Add `set_demo_info` helper to Scripting/demos_py/prelude.py (equivalent Python wrapper)
- [x] T012 Verify build succeeds: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Foundation ready — all helpers, proto types, and viewer state fields available for user story work

---

## Phase 3: User Story 1 — Viewer Window Title and Demo Label (Priority: P1) MVP

**Goal**: Viewer shows a static window title in the OS title bar and a dynamic demo name/description overlay in the top-left corner

**Independent Test**: Launch viewer, run any demo, verify window title shows "PhysicsSandbox Viewer" and top-left displays demo name + description. Toggle between demos to confirm label updates.

### Implementation for User Story 1

- [x] T013 [US1] Set viewer window title to "PhysicsSandbox Viewer" via game.Window.Title in src/PhysicsViewer/Program.fs (during game initialization)
- [x] T014 [US1] Handle SetDemoMetadata ViewCommand in viewer's view command processing block in src/PhysicsViewer/Program.fs — extract name/description and store in mutable state (or update SceneState)
- [x] T015 [US1] Render demo label overlay at top-left (10, 10) using DebugTextSystem.Print() in src/PhysicsViewer/Program.fs update function — show "Demo: {name}" and description; move existing FPS/status bar down to (10, 30)
- [x] T016 [US1] Handle default state when no demo metadata is set — show "Free Mode" or empty label in src/PhysicsViewer/Program.fs
- [ ] T017 [US1] Add unit test for SceneState metadata field initialization and update in tests/PhysicsViewer.Tests/

**Checkpoint**: Viewer window title visible, demo label renders with metadata from ViewCommand stream. MVP complete.

---

## Phase 4: User Story 4 — Demo Metadata for Labels (Priority: P3)

**Goal**: Every demo (existing and new) sends its name and description via setDemoInfo so the viewer label displays correct info

**Independent Test**: Run any demo script and confirm the viewer's top-left label shows that demo's name and description

### Implementation for User Story 4

- [x] T018 [US4] Add setDemoInfo calls to all 15 inline demos in Scripting/demos/AllDemos.fsx — each demo calls setDemoInfo after resetSimulation/setCamera with its name and description
- [x] T019 [P] [US4] Add setDemoInfo calls to standalone F# demo files: Scripting/demos/Demo01_HelloDrop.fsx through Demo15_Overload.fsx
- [x] T020 [P] [US4] Add setDemoInfo calls to standalone F# demos: Scripting/demos/16_Constraints.fsx, 17_QueryRange.fsx, 18_KinematicSweep.fsx
- [x] T021 [P] [US4] Add set_demo_info calls to all Python demos in Scripting/demos_py/ (demo01_hello_drop.py through demo15_overload.py)
- [x] T022 [P] [US4] Update Scripting/demos_py/all_demos.py to include set_demo_info calls in all demo functions
- [ ] T023 [US4] Add integration test verifying SetDemoMetadata ViewCommand is received by viewer stream in tests/PhysicsSandbox.Integration.Tests/

**Checkpoint**: All 18 existing demos send metadata. Viewer displays correct label for every demo.

---

## Phase 5: User Story 2 — Existing Demos Use New Shape Types (Priority: P2)

**Goal**: At least 8 existing demos incorporate Triangle, ConvexHull, Mesh, or Compound shapes where thematically appropriate

**Independent Test**: Run each enhanced demo, verify new shape types appear, collide correctly, and render in both solid and wireframe modes

### Implementation for User Story 2

- [x] T024 [P] [US2] Enhance Demo 01 (Hello Drop) in Scripting/demos/AllDemos.fsx — add triangle and convex hull bodies alongside existing primitives
- [x] T025 [P] [US2] Enhance Demo 03 (Crate Stack) in Scripting/demos/AllDemos.fsx — add compound crates (multi-part stacking objects)
- [x] T026 [P] [US2] Enhance Demo 04 (Bowling Alley) in Scripting/demos/AllDemos.fsx — add convex hull bowling ball or compound pins
- [x] T027 [P] [US2] Enhance Demo 06 (Domino Row) in Scripting/demos/AllDemos.fsx — add compound L-shaped or T-shaped domino pieces
- [x] T028 [P] [US2] Enhance Demo 09 (Billiards) in Scripting/demos/AllDemos.fsx — add convex hull or mesh table bumpers
- [x] T029 [P] [US2] Enhance Demo 10 (Chaos) in Scripting/demos/AllDemos.fsx — add mesh and triangle shapes for visual variety
- [x] T030 [P] [US2] Enhance Demo 13 (Force Frenzy) in Scripting/demos/AllDemos.fsx — add triangle and convex hull projectiles
- [x] T031 [P] [US2] Enhance Demo 14 (Domino Cascade) in Scripting/demos/AllDemos.fsx — add compound domino pieces
- [x] T032 [US2] Update corresponding standalone F# demo files (Demo01_HelloDrop.fsx, Demo03_CrateStack.fsx, Demo04_BowlingAlley.fsx, Demo06_DominoRow.fsx, Demo09_Billiards.fsx, Demo10_Chaos.fsx, Demo13_ForceFrenzy.fsx, Demo14_DominoCascade.fsx) with matching shape enhancements
- [x] T033 [US2] Update corresponding Python demo files (demo01_hello_drop.py, demo03_crate_stack.py, demo04_bowling_alley.py, demo06_domino_row.py, demo09_billiards.py, demo10_chaos.py, demo13_force_frenzy.py, demo14_domino_cascade.py) with matching shape enhancements

### Verification for User Story 2

- [ ] T033a [US2] Run each enhanced F# demo via `dotnet fsi` and verify new shapes appear, collide correctly, and render in both solid and wireframe modes — document results as test evidence per constitution Principle VI

**Checkpoint**: 8+ existing demos now use new shape types. Existing demos still function correctly with enhanced variety.

---

## Phase 6: User Story 3 — Three New Demos Showcasing All New Shapes (Priority: P2)

**Goal**: Create Demo 19 (Shape Gallery), Demo 20 (Compound Constructions), Demo 21 (Mesh & Hull Playground) — each with F# and Python versions

**Independent Test**: Run each new demo, verify all advertised shape types present, render correctly, and interact physically. Verify demo label displays correct metadata.

### Implementation for User Story 3

- [x] T034 [P] [US3] Create Scripting/demos/19_ShapeGallery.fsx — all 10 shape types (Sphere, Box, Capsule, Cylinder, Triangle, ConvexHull, Mesh, Compound, Plane as ground) displayed side-by-side with distinct colors, sizes, and materials; slow drop to showcase rendering
- [x] T035 [P] [US3] Create Scripting/demos_py/demo19_shape_gallery.py — Python equivalent of Demo 19 with matching behavior
- [x] T036 [P] [US3] Create Scripting/demos/20_CompoundConstructions.fsx — compound shapes in interesting configurations (T-shapes, L-shapes, dumbbell, multi-sphere clusters) with varied masses and materials; demonstrate nesting and composite collision
- [x] T037 [P] [US3] Create Scripting/demos_py/demo20_compound_constructions.py — Python equivalent of Demo 20
- [x] T038 [P] [US3] Create Scripting/demos/21_MeshHullPlayground.fsx — varied convex hulls (tetrahedra, octahedra, icosahedra, random point clouds) and triangle meshes tumbling through obstacles; stress-test custom geometry rendering
- [x] T039 [P] [US3] Create Scripting/demos_py/demo21_mesh_hull_playground.py — Python equivalent of Demo 21
- [x] T040 [US3] Add Demo 19, 20, 21 to Scripting/demos/AllDemos.fsx (inline demo functions with setDemoInfo calls)
- [x] T041 [P] [US3] Add demo19, demo20, demo21 to Scripting/demos_py/all_demos.py
- [x] T042 [US3] Update Scripting/demos/RunAll.fsx and AutoRun.fsx to include demos 19-21
- [x] T043 [P] [US3] Update Scripting/demos_py/auto_run.py to include demos 19-21

### Verification for User Story 3

- [ ] T043a [US3] Run each new F# demo (19, 20, 21) via `dotnet fsi` and verify all 4 newer shape types (Triangle, ConvexHull, Mesh, Compound) are present, interact physically, and render correctly — document results as test evidence per constitution Principle VI
- [ ] T043b [P] [US3] Run each new Python demo (19, 20, 21) and verify equivalent behavior to F# versions

**Checkpoint**: All 3 new demos functional in F# and Python, registered in all runners, label metadata working.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across all stories

- [x] T044 Verify all 21 demos build and run without errors: test each F# demo with `dotnet fsi` — confirm new shape types interact physically (collide, bounce, stack) with other body types (FR-010)
- [x] T045 [P] Verify all Python demos run without import/syntax errors
- [ ] T046 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [ ] T047 Update surface area baselines if public API surface changed (ViewCommands, SceneManager) in tests/PhysicsClient.Tests/ and tests/PhysicsViewer.Tests/

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (proto types must exist)
- **US1 (Phase 3)**: Depends on Phase 2 (SceneState fields, no helpers needed yet)
- **US4 (Phase 4)**: Depends on Phase 2 (setDemoInfo helpers) + Phase 3 (viewer must render labels)
- **US2 (Phase 5)**: Depends on Phase 2 (makeMeshCmd helper) + Phase 4 (metadata calls pattern established)
- **US3 (Phase 6)**: Depends on Phase 2 (all helpers) + Phase 4 (metadata pattern)
- **Polish (Phase 7)**: Depends on all previous phases

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational — no other story dependencies
- **US4 (P3)**: Depends on US1 being complete (viewer must render labels before metadata is meaningful to verify)
- **US2 (P2)**: Can start after US4 pattern is established (metadata + shapes in same pass)
- **US3 (P2)**: Can start after US4 pattern is established. Independent of US2.

### Within Each Phase

- Tasks marked [P] can run in parallel within their phase
- Non-[P] tasks have implicit sequential ordering within their phase

### Parallel Opportunities

- Phase 2: T006-T011 are all [P] (different files: SceneManager, Prelude.fsx, prelude.py)
- Phase 4: T019-T022 are all [P] (standalone files vs AllDemos vs Python)
- Phase 5: T024-T031 are all [P] (different demo functions within AllDemos.fsx — but since they're in the same file, true parallelism requires careful merging)
- Phase 6: T034-T039 are all [P] (each demo is a separate file in F#/Python)

---

## Parallel Example: Phase 2 (Foundational)

```text
# These can all run in parallel (different files):
Task T006: Add DemoName/DemoDescription to SceneManager.fs
Task T007: Update SceneManager.fsi
Task T008: Add makeMeshCmd to Prelude.fsx
Task T009: Add make_mesh_cmd to prelude.py
Task T010: Add setDemoInfo to Prelude.fsx
Task T011: Add set_demo_info to prelude.py

# These must be sequential (same module):
Task T004: Add setDemoMetadata to ViewCommands.fs
Task T005: Update ViewCommands.fsi (depends on T004 for exact signature)
```

## Parallel Example: Phase 6 (New Demos)

```text
# All new demo files can be created in parallel:
Task T034: 19_ShapeGallery.fsx
Task T035: demo19_shape_gallery.py
Task T036: 20_CompoundConstructions.fsx
Task T037: demo20_compound_constructions.py
Task T038: 21_MeshHullPlayground.fsx
Task T039: demo21_mesh_hull_playground.py

# Then register all demos (sequential, same files):
Task T040: AllDemos.fsx
Task T041: all_demos.py
Task T042: RunAll.fsx + AutoRun.fsx
Task T043: auto_run.py
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (proto contract)
2. Complete Phase 2: Foundational (helpers + viewer state)
3. Complete Phase 3: US1 (window title + demo label rendering)
4. **STOP and VALIDATE**: Launch viewer, verify window title and label overlay work
5. Demonstrate with a single demo manually calling setDemoInfo

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. US1 (viewer title + label) → Visual feedback working (MVP!)
3. US4 (metadata in all demos) → Every demo displays its info
4. US2 (enhance existing demos) → Existing demos show new shapes
5. US3 (3 new demos) → Full showcase complete
6. Polish → Final validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- AllDemos.fsx is a single file containing all 15 inline demos — tasks T024-T031 edit different functions within it but cannot truly parallelize
- Standalone F# demo files (Demo01_HelloDrop.fsx etc.) mirror AllDemos.fsx content — keep them in sync
- Python demos mirror F# demos — ensure equivalent behavior
- Demo 08, 11, 12 already use some advanced shapes — no enhancement needed for those
- Surface area baselines may need updating if ViewCommands or SceneManager public API changes
