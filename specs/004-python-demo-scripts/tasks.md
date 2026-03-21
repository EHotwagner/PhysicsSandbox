# Tasks: Python Demo Scripts

**Input**: Design documents from `/specs/004-python-demo-scripts/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: Not required per spec — demos are informal end-to-end smoke tests.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, dependencies, and proto stub generation

- [x] T001 Create demos_py/ directory structure with demos_py/__init__.py and demos_py/generated/__init__.py
- [x] T002 Create Python dependencies file in demos_py/requirements.txt (grpcio, grpcio-tools, protobuf)
- [x] T003 Create proto stub generation script in demos_py/generate_stubs.sh that runs grpc_tools.protoc against src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto and outputs to demos_py/generated/
- [x] T004 Run demos_py/generate_stubs.sh to generate demos_py/generated/physics_hub_pb2.py and demos_py/generated/physics_hub_pb2_grpc.py, verify stubs compile

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared prelude module that ALL demos and runners depend on

**CRITICAL**: No demo or runner work can begin until this phase is complete

- [x] T005 Implement prelude.py — Session dataclass, connect(addr)/disconnect(session), Direction enum, ID generation (next_id/reset_ids), utility helpers (sleep, run_for, to_vec3, timed context manager) in demos_py/prelude.py
- [x] T006 Implement prelude.py — simulation commands (play, pause, step, reset, set_gravity, add_sphere, add_box, add_plane, remove_body, clear_all, apply_force, apply_impulse, apply_torque, clear_forces), view commands (set_camera, wireframe, set_zoom), message construction helpers (make_sphere_cmd, make_box_cmd, make_impulse_cmd, make_torque_cmd), batch helpers (batch_add with auto-chunking at 100, batch_commands, batch_view_commands) in demos_py/prelude.py
- [x] T007 Implement prelude.py — body presets (bowling_ball, boulder, marble, beach_ball, crate, brick with F#-matching defaults), generators (stack, pyramid, row, grid, random_spheres), steering (push with Direction enum, launch with trajectory calculation) in demos_py/prelude.py
- [x] T008 Implement prelude.py — display helpers (list_bodies prints body table, status prints simulation summary, get_state fetches StreamState snapshot), reset_simulation (pause → reset → add_plane → set_gravity → sleep) in demos_py/prelude.py

**Checkpoint**: Prelude ready — demo implementation can now begin in parallel

---

## Phase 3: User Story 1 — Run Full Demo Suite Automatically (Priority: P1) MVP

**Goal**: All 15 demos execute via `python demos_py/auto_run.py` with pass/fail summary

**Independent Test**: Start Aspire stack, run `python demos_py/auto_run.py`, verify all 15 demos complete with pass/fail count

### Basic Demos (01–10)

- [x] T009 [P] [US1] Create Demo 01 Hello Drop — bowling ball falls from 10m, side-view camera in demos_py/demo01_hello_drop.py
- [x] T010 [P] [US1] Create Demo 02 Bouncing Marbles — 5 marbles from different heights using batch_add in demos_py/demo02_bouncing_marbles.py
- [x] T011 [P] [US1] Create Demo 03 Crate Stack — tower of 8 crates, push top one east using stack generator in demos_py/demo03_crate_stack.py
- [x] T012 [P] [US1] Create Demo 04 Bowling Alley — pyramid of bricks hit by launched bowling ball in demos_py/demo04_bowling_alley.py
- [x] T013 [P] [US1] Create Demo 05 Marble Rain — 20 random spheres with ground-level camera change in demos_py/demo05_marble_rain.py
- [x] T014 [P] [US1] Create Demo 06 Domino Row — 12 box dominoes toppled by push using batch_add in demos_py/demo06_domino_row.py
- [x] T015 [P] [US1] Create Demo 07 Spinning Tops — 4 bodies with torques and wireframe toggle in demos_py/demo07_spinning_tops.py
- [x] T016 [P] [US1] Create Demo 08 Gravity Flip — grid + balls, gravity reversed then sideways then restored in demos_py/demo08_gravity_flip.py
- [x] T017 [P] [US1] Create Demo 09 Billiards — 15-ball triangle + cue ball break shot in demos_py/demo09_billiards.py
- [x] T018 [P] [US1] Create Demo 10 Chaos Scene — 5-act scene: formations, bombardment, boulder, gravity chaos, camera sweep in demos_py/demo10_chaos.py

### Stress Demos (11–15)

- [x] T019 [P] [US1] Create Demo 11 Body Scaling — 4 tiers (50/100/200/500 bodies) with timed markers in demos_py/demo11_body_scaling.py
- [x] T020 [P] [US1] Create Demo 12 Collision Pit — 4 static walls + 120 spheres dropped in, timed sections in demos_py/demo12_collision_pit.py
- [x] T021 [P] [US1] Create Demo 13 Force Frenzy — 100 bodies, 3 rounds of impulses/torques/gravity shifts with timed markers in demos_py/demo13_force_frenzy.py
- [x] T022 [P] [US1] Create Demo 14 Domino Cascade — 120 semicircular dominoes with cascade propagation timing in demos_py/demo14_domino_cascade.py
- [x] T023 [P] [US1] Create Demo 15 Overload — 5-act stress test: formations + 100 spheres + impulse storm + gravity chaos + camera sweep, total timing in demos_py/demo15_overload.py

### Runners

- [x] T024 [US1] Create demo registry importing all 15 demos as (name, description, run) tuples in demos_py/all_demos.py
- [x] T025 [US1] Create automated runner — sequential execution, per-demo try/except, pass/fail counters, summary banner, optional server address arg, cleanup on exit in demos_py/auto_run.py

**Checkpoint**: `python demos_py/auto_run.py` runs all 15 demos with pass/fail reporting. US1 complete.

---

## Phase 4: User Story 2 — Run Individual Demo Script (Priority: P2)

**Goal**: Each demo runs standalone via `python demos_py/demoNN_name.py [server-address]`

**Independent Test**: Run `python demos_py/demo01_hello_drop.py` — it connects, runs the demo, disconnects

**Note**: US2 is largely satisfied by the demo script design in Phase 3. Each demo created in T009–T023 MUST include a module-level `name`, `description`, and `run(session)` function, plus an `if __name__ == "__main__"` block that handles connect/run/disconnect with optional server address argument. This task verifies and fixes any gaps.

- [x] T026 [US2] Verify all 15 demo scripts have working standalone execution — each must have `if __name__ == "__main__"` block with sys.argv server address parsing, connect, run, disconnect, and error handling. Fix any missing or broken standalone blocks.

**Checkpoint**: Any single demo can be run independently. US2 complete.

---

## Phase 5: User Story 3 — Interactive Demo Runner (Priority: P3)

**Goal**: Step-through runner with keypress advancement via `python demos_py/run_all.py`

**Independent Test**: Run `python demos_py/run_all.py`, press Enter to step through demos

- [x] T027 [US3] Create interactive runner — demo header display, keypress wait (Enter/Space), per-demo try/except, cleanup on exit, optional server address arg in demos_py/run_all.py

**Checkpoint**: `python demos_py/run_all.py` steps through all 15 demos interactively. US3 complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [x] T028 Commit generated proto stubs in demos_py/generated/ to git for convenience (per research.md R2 — avoids requiring grpcio-tools at runtime); ensure generate_stubs.sh remains as the regeneration source-of-truth
- [x] T029 Validate quickstart.md by running setup steps end-to-end: pip install, generate stubs, run auto_run.py against live Aspire stack

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 (T004 stubs must exist for prelude imports)
- **Phase 3 (US1)**: Depends on Phase 2 (all demos import from prelude.py)
- **Phase 4 (US2)**: Depends on Phase 3 (verifies demos created in Phase 3)
- **Phase 5 (US3)**: Depends on Phase 3 (imports from all_demos.py)
- **Phase 6 (Polish)**: Depends on all prior phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational phase only. No dependency on other stories.
- **US2 (P2)**: Logically depends on US1 (demos must exist to verify standalone execution). Could be done simultaneously if __main__ blocks are included during US1 demo creation.
- **US3 (P3)**: Depends on US1 (needs all_demos.py registry). Independent of US2.

### Within Phase 3 (US1)

- T009–T023 (all 15 demos): Fully parallel — each is a separate file importing only from prelude.py
- T024 (all_demos.py): Depends on T009–T023 (imports all demo modules)
- T025 (auto_run.py): Depends on T024 (imports demo registry)

### Parallel Opportunities

```
Phase 1: T001 → T002, T003 (parallel) → T004
Phase 2: T005 → T006 → T007 → T008 (sequential, same file)
Phase 3: T009–T023 (15 demos, ALL parallel) → T024 → T025
Phase 4: T026 (single verification pass)
Phase 5: T027 (single task, parallel with Phase 4)
Phase 6: T028, T029 (parallel)
```

---

## Parallel Example: Phase 3 Demos

```bash
# Launch all 15 demo scripts in parallel (all independent files):
Task: "Create Demo 01 Hello Drop in demos_py/demo01_hello_drop.py"
Task: "Create Demo 02 Bouncing Marbles in demos_py/demo02_bouncing_marbles.py"
Task: "Create Demo 03 Crate Stack in demos_py/demo03_crate_stack.py"
# ... all 15 can be written simultaneously
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (4 tasks)
2. Complete Phase 2: Foundational prelude.py (4 tasks)
3. Complete Phase 3: All 15 demos + auto_run.py (17 tasks)
4. **STOP and VALIDATE**: Run `python demos_py/auto_run.py` against live Aspire stack
5. MVP delivered — full automated demo suite working

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. US1 (Phase 3) → `auto_run.py` works → MVP!
3. US2 (Phase 4) → Individual demo execution verified
4. US3 (Phase 5) → Interactive runner added
5. Polish (Phase 6) → Final validation

---

## Notes

- All 15 demo scripts should closely mirror their F# equivalents — same body positions, camera angles, forces, timing
- Each demo's `run(session)` function should follow the F# pattern: reset_simulation → set_camera → create bodies → run simulation → display results
- The `timed` helper should be a Python context manager (`with timed("label"):`) rather than a higher-order function, for more Pythonic usage
- Proto stubs are generated from the same `physics_hub.proto` used by all .NET services — contract alignment is automatic
- Total: 29 tasks across 6 phases
