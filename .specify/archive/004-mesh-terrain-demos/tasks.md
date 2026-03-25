# Tasks: Static Mesh Terrain Demos

**Input**: Design documents from `/specs/004-mesh-terrain-demos/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Not explicitly requested in the feature specification. Demo scripts are self-verifying by execution.

**Organization**: Tasks are grouped by user story. Both stories are P1 and independent — can be implemented in parallel.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No new project setup needed — uses existing demo infrastructure. This phase verifies prerequisites.

- [ ] T001 Verify PhysicsSandbox server starts successfully via `./start.sh`
- [ ] T002 Verify existing Demo 21 (Mesh & Hull Playground) runs without errors to confirm mesh support works

**Checkpoint**: Infrastructure verified — demo scripting can begin

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational work needed. All required helpers (`makeMeshCmd`, camera commands, material presets) already exist in `Scripting/demos/Prelude.fsx` and `Scripting/demos_py/prelude.py`.

**Checkpoint**: Foundation ready — both user stories can proceed in parallel

---

## Phase 3: User Story 1 - Ball Rollercoaster Demo (Priority: P1) 🎯 MVP

**Goal**: Create a rollercoaster-style track from static mesh triangles with drops, hills, and banked curves. Release balls at the top that roll the full track length with cinematic camera and narration.

**Independent Test**: Run `dotnet fsi Scripting/demos/Demo23_BallRollercoaster.fsx` against a live server — balls should traverse the entire track without clipping through.

### Implementation for User Story 1

- [x] T003 [P] [US1] Create F# rollercoaster demo with procedural track mesh generation (parametric path curve with steep drop, hill, and banked curve sections → ~90-120 triangles, mass=0.0 static body, slipperyMaterial), 5-8 balls released at top with staggered timing, cinematic camera (wide opening → follow lead ball → orbit → wide pullback), and narration at each phase in `Scripting/demos/Demo23_BallRollercoaster.fsx`
- [x] T004 [P] [US1] Create Python rollercoaster demo mirroring the F# version — same track geometry, ball placement, camera work, and narration in `Scripting/demos_py/demo23_ball_rollercoaster.py`
- [x] T005 [US1] Add Demo 23 entry to the F# demo registry array in `Scripting/demos/AllDemos.fsx`
- [x] T006 [US1] Add Demo 23 entry to the Python demo registry in `Scripting/demos_py/all_demos.py`

**Checkpoint**: Demo 23 runs standalone in both F# and Python, and appears in suite runners

---

## Phase 4: User Story 2 - Halfpipe Arena Demo (Priority: P1)

**Goal**: Create a halfpipe (U-shaped ramp) from static mesh triangles with smooth curvature. Drop balls and capsules that oscillate back and forth, demonstrating concave mesh terrain interactions with cinematic camera and narration.

**Independent Test**: Run `dotnet fsi Scripting/demos/Demo24_HalfpipeArena.fsx` against a live server — objects should oscillate in the halfpipe for at least 3 visible cycles before settling.

### Implementation for User Story 2

- [x] T007 [P] [US2] Create F# halfpipe demo with semicircular arc cross-section mesh (8-12 arc strips extruded over 10-15 segments → ~160-360 triangles, mass=0.0 static body, custom moderate-friction material), 5-8 balls + 2-3 capsules dropped from varying heights, cinematic camera (wide view → orbit around halfpipe → close-up of oscillation → wide pullback), and narration in `Scripting/demos/Demo24_HalfpipeArena.fsx`
- [x] T008 [P] [US2] Create Python halfpipe demo mirroring the F# version — same halfpipe geometry, object placement, camera work, and narration in `Scripting/demos_py/demo24_halfpipe_arena.py`
- [x] T009 [US2] Add Demo 24 entry to the F# demo registry array in `Scripting/demos/AllDemos.fsx`
- [x] T010 [US2] Add Demo 24 entry to the Python demo registry in `Scripting/demos_py/all_demos.py`

**Checkpoint**: Demo 24 runs standalone in both F# and Python, and appears in suite runners

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Suite integration validation and final cleanup

- [ ] T011 Run full F# demo suite via `dotnet fsi Scripting/demos/AutoRun.fsx` and verify Demos 23 and 24 execute in sequence without errors (requires live server)
- [ ] T012 [P] Run full Python demo suite via `python Scripting/demos_py/auto_run.py` and verify Demos 23 and 24 execute in sequence without errors (requires live server)
- [ ] T013 Verify both demos complete within 30 seconds each (SC-005) (requires live server)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — verify server and mesh support
- **Foundational (Phase 2)**: N/A — no foundational work needed
- **User Story 1 (Phase 3)**: Depends on Phase 1 verification only
- **User Story 2 (Phase 4)**: Depends on Phase 1 verification only — **independent of Phase 3**
- **Polish (Phase 5)**: Depends on Phases 3 and 4 completion

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories — fully independent
- **User Story 2 (P1)**: No dependencies on other stories — fully independent
- Note: T005/T006 and T009/T010 both modify AllDemos.fsx/all_demos.py but at different array positions — no conflict if done sequentially within each story

### Parallel Opportunities

- T003 (F# rollercoaster) and T004 (Python rollercoaster) can run in parallel
- T007 (F# halfpipe) and T008 (Python halfpipe) can run in parallel
- All of Phase 3 and Phase 4 can run in parallel (different files, independent stories)
- T011 and T012 (suite runs in F# and Python) can run in parallel

---

## Parallel Example: Both Stories Simultaneously

```bash
# All four demo files can be created in parallel:
Task T003: "Create F# rollercoaster in Demo23_BallRollercoaster.fsx"
Task T004: "Create Python rollercoaster in demo23_ball_rollercoaster.py"
Task T007: "Create F# halfpipe in Demo24_HalfpipeArena.fsx"
Task T008: "Create Python halfpipe in demo24_halfpipe_arena.py"

# Then registry updates (sequential within each file):
Task T005 + T009: "Add both entries to AllDemos.fsx"
Task T006 + T010: "Add both entries to all_demos.py"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Verify Phase 1 (server + mesh support working)
2. Complete Phase 3: Demo 23 Ball Rollercoaster (F# + Python + registry)
3. **STOP and VALIDATE**: Run Demo 23 standalone, verify balls traverse track
4. Proceed to Phase 4 if satisfied

### Incremental Delivery

1. Phase 1: Verify → Ready
2. Phase 3: Demo 23 → Test standalone → Working rollercoaster demo
3. Phase 4: Demo 24 → Test standalone → Working halfpipe demo
4. Phase 5: Suite validation → Both demos integrated

---

## Notes

- Both stories are P1 (equal priority) and fully independent — implement in either order or parallel
- No Prelude.fsx or prelude.py changes needed — all required helpers exist
- Registry updates (AllDemos.fsx, all_demos.py) are the only shared-file modifications
- The `resetSimulation` call in each demo adds a ground plane — mesh terrain sits above it
- Commit after each demo file is complete and tested standalone
