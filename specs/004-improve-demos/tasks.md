# Tasks: Improve Physics Demos

**Input**: Design documents from `/specs/004-improve-demos/`
**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: Not required — demos are verified by running through AllDemos runners.

**Organization**: Tasks grouped by user story. US1 (demo content) and US3 (Python parity) are paired per-demo since the collaborative workflow is: improve F# → mirror Python → confirm.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Foundational — Structural Fixes (US2)

**Purpose**: Integrate demos 11-15 into AllDemos and eliminate AutoRun code duplication. MUST complete before demo content improvements so all demos can be validated through the unified runner.

**Goal**: All 15 demos runnable through AllDemos/RunAll/AutoRun without standalone execution.

**Independent Test**: Run `dotnet fsi Scripting/demos/RunAll.fsx` and `python Scripting/demos_py/run_all.py` — all 15 demos listed and executable.

### F# Runner Fixes

- [x] T001 [US2] Integrate Demo 11 (Body Scaling) into AllDemos.fsx as a `{ Name; Description; Run }` record with sensible defaults (no CLI args) in `Scripting/demos/AllDemos.fsx` *(already done)*
- [x] T002 [US2] Integrate Demo 12 (Collision Pit) into AllDemos.fsx as a record with sensible defaults in `Scripting/demos/AllDemos.fsx` *(already done)*
- [x] T003 [US2] Integrate Demo 13 (Force Frenzy) into AllDemos.fsx as a record with sensible defaults in `Scripting/demos/AllDemos.fsx` *(already done)*
- [x] T004 [US2] Integrate Demo 14 (Domino Cascade) into AllDemos.fsx as a record with sensible defaults in `Scripting/demos/AllDemos.fsx` *(already done)*
- [x] T005 [US2] Integrate Demo 15 (Overload) into AllDemos.fsx as a record with sensible defaults in `Scripting/demos/AllDemos.fsx` *(already done)*
- [x] T006 [US2] Refactor AutoRun.fsx to load AllDemos.fsx and reuse demo definitions instead of duplicating all helper and demo code in `Scripting/demos/AutoRun.fsx`
- [x] T007 [US2] Update RunAll.fsx if needed to handle all 15 demos from AllDemos in `Scripting/demos/RunAll.fsx` *(already done)*

### Python Runner Fixes

- [x] T008 [P] [US2] Integrate demos 11-15 into all_demos.py as function entries with sensible defaults (no CLI args) in `Scripting/demos_py/all_demos.py` *(already done)*
- [x] T009 [US2] Refactor auto_run.py to reuse all_demos.py definitions instead of duplicating demo code in `Scripting/demos_py/auto_run.py` *(already done)*
- [x] T010 [US2] Update run_all.py if needed to handle all 15 demos from all_demos in `Scripting/demos_py/run_all.py` *(already done)*

**Checkpoint**: All 15 demos run through AllDemos runners in both F# and Python. AutoRun no longer duplicates code.

---

## Phase 2: US1+US3 — Major Demo Improvements (6 demos)

**Purpose**: Improve the thinnest demos that need significant rework. Each task pair = improve F# then mirror Python.

**Goal**: Each demo produces visually interesting, physically rich interactions beyond a minimal smoke test.

**Independent Test**: Run each improved demo individually — it should produce at least 3 distinct visible physics interactions.

### Demo 01: Hello Drop (single ball → multi-object comparison)

- [x] T011 [US1] Improve Demo 01 F# — expand from single ball drop to multiple objects of different shapes and masses showing comparative fall behavior in `Scripting/demos/Demo01_HelloDrop.fsx`
- [x] T012 [US3] Mirror Demo 01 improvements to Python in `Scripting/demos_py/demo01_hello_drop.py`

### Demo 02: Bouncing Marbles (5 vertical → dense spread)

- [x] T013 [US1] Improve Demo 02 F# — more marbles with lateral spread, varied sizes, and marble-marble collisions in `Scripting/demos/Demo02_BouncingMarbles.fsx`
- [x] T014 [US3] Mirror Demo 02 improvements to Python in `Scripting/demos_py/demo02_bouncing_marbles.py`

### Demo 07: Spinning Tops (isolated rotation → collision dynamics)

- [x] T015 [US1] Improve Demo 07 F# — add body-body collisions between spinning objects, show gyroscopic interaction effects in `Scripting/demos/Demo07_SpinningTops.fsx`
- [x] T016 [US3] Mirror Demo 07 improvements to Python in `Scripting/demos_py/demo07_spinning_tops.py`

### Demo 08: Gravity Flip (heavy crates → dramatic mixed bodies)

- [x] T017 [US1] Improve Demo 08 F# — replace heavy crates with lighter mixed bodies (beach balls, marbles, dice) for more dramatic gravity transitions in `Scripting/demos/Demo08_GravityFlip.fsx`
- [x] T018 [US3] Mirror Demo 08 improvements to Python in `Scripting/demos_py/demo08_gravity_flip.py`

### Demo 11: Body Scaling (sparse grid → collision-dense stress test)

- [x] T019 [US1] Improve Demo 11 F# — tighter body packing for collision density during scaling tiers, add visual interest beyond settling in `Scripting/demos/Demo11_BodyScaling.fsx`
- [x] T020 [US3] Mirror Demo 11 improvements to Python in `Scripting/demos_py/demo11_body_scaling.py`

### Demo 13: Force Frenzy (wide spacing → interactive collisions)

- [x] T021 [US1] Improve Demo 13 F# — tighter grid spacing so bodies collide with each other during force rounds, not just respond to external forces in `Scripting/demos/Demo13_ForceFrenzy.fsx`
- [x] T022 [US3] Mirror Demo 13 improvements to Python in `Scripting/demos_py/demo13_force_frenzy.py`

**Checkpoint**: 6 thinnest demos now produce satisfying, visually rich physics scenarios in both F# and Python.

---

## Phase 3: US1+US3 — Moderate Demo Improvements (3 demos)

**Purpose**: Enrich demos with solid concepts that need more depth.

### Demo 03: Crate Stack (short push → dramatic collapse)

- [x] T023 [US1] Improve Demo 03 F# — taller stack, more dramatic collapse with multiple impact angles or projectile strike in `Scripting/demos/Demo03_CrateStack.fsx`
- [x] T024 [US3] Mirror Demo 03 improvements to Python in `Scripting/demos_py/demo03_crate_stack.py`

### Demo 05: Marble Rain (vertical spheres → mixed shape density)

- [x] T025 [US1] Improve Demo 05 F# — horizontal spread, mixed shapes (not just spheres), higher density for more collisions in `Scripting/demos/Demo05_MarbleRain.fsx`
- [x] T026 [US3] Mirror Demo 05 improvements to Python in `Scripting/demos_py/demo05_marble_rain.py`

### Demo 12: Collision Pit (uniform drop → staged drama)

- [x] T027 [US1] Improve Demo 12 F# — varied sphere sizes, staged drop waves for visual drama, camera repositioning in `Scripting/demos/Demo12_CollisionPit.fsx`
- [x] T028 [US3] Mirror Demo 12 improvements to Python in `Scripting/demos_py/demo12_collision_pit.py`

**Checkpoint**: 9 demos improved. Moderate demos now feel rich and complete.

---

## Phase 4: US1+US3 — Polish Demos (6 demos)

**Purpose**: Minor camera, pacing, and composition tweaks to already-strong demos.

### Demo 04: Bowling Alley

- [x] T029 [P] [US1] Polish Demo 04 F# — camera angles, pacing, possibly a second throw in `Scripting/demos/Demo04_BowlingAlley.fsx`
- [x] T030 [P] [US3] Mirror Demo 04 polish to Python in `Scripting/demos_py/demo04_bowling_alley.py`

### Demo 06: Domino Row

- [x] T031 [P] [US1] Polish Demo 06 F# — longer row, camera tracking along cascade in `Scripting/demos/Demo06_DominoRow.fsx`
- [x] T032 [P] [US3] Mirror Demo 06 polish to Python in `Scripting/demos_py/demo06_domino_row.py`

### Demo 09: Billiards

- [x] T033 [P] [US1] Polish Demo 09 F# — camera positioning, pacing between setup and break in `Scripting/demos/Demo09_Billiards.fsx`
- [x] T034 [P] [US3] Mirror Demo 09 polish to Python in `Scripting/demos_py/demo09_billiards.py`

### Demo 10: Chaos Scene

- [x] T035 [P] [US1] Polish Demo 10 F# — minor pacing or camera adjustments in `Scripting/demos/Demo10_Chaos.fsx`
- [x] T036 [P] [US3] Mirror Demo 10 polish to Python in `Scripting/demos_py/demo10_chaos.py`

### Demo 14: Domino Cascade

- [x] T037 [P] [US1] Polish Demo 14 F# — minor camera sweep or pacing adjustments in `Scripting/demos/Demo14_DominoCascade.fsx`
- [x] T038 [P] [US3] Mirror Demo 14 polish to Python in `Scripting/demos_py/demo14_domino_cascade.py`

### Demo 15: Overload

- [x] T039 [P] [US1] Polish Demo 15 F# — minor pacing or visual improvements in `Scripting/demos/Demo15_Overload.fsx`
- [x] T040 [P] [US3] Mirror Demo 15 polish to Python in `Scripting/demos_py/demo15_overload.py`

**Checkpoint**: All 15 demos improved or polished in both F# and Python.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across the full suite.

- [x] T041 Update AllDemos.fsx demo descriptions/names if any changed during improvements in `Scripting/demos/AllDemos.fsx`
- [x] T042 Update all_demos.py demo descriptions/names if any changed during improvements in `Scripting/demos_py/all_demos.py`
- [x] T043 Validate all 15 F# demos run through AutoRun.fsx without errors
- [x] T044 Validate all 15 Python demos run through auto_run.py without errors
- [x] T045 Run quickstart.md validation — verify all documented commands work

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Structural)**: No dependencies — start immediately. BLOCKS demo content work (need unified runner for validation)
- **Phase 2 (Major)**: Depends on Phase 1. Work through demos collaboratively one at a time.
- **Phase 3 (Moderate)**: Depends on Phase 1. Can start after or in parallel with Phase 2.
- **Phase 4 (Polish)**: Depends on Phase 1. All polish demos are independent — [P] marked tasks can run in parallel.
- **Phase 5 (Validation)**: Depends on all prior phases.

### User Story Dependencies

- **US2 (Suite Integration)**: Phase 1 — no dependencies on other stories
- **US1 (Demo Content)**: Phases 2-4 — depends on US2 (needs AllDemos integration for validation)
- **US3 (Python Parity)**: Interleaved with US1 — each F# improvement is immediately mirrored to Python

### Within Each Demo Pair

1. Improve F# version (US1 task)
2. Mirror to Python version (US3 task)
3. User confirms satisfaction before moving to next demo

### Parallel Opportunities

Within Phase 4 (Polish), all 6 demo pairs are independent and can run in parallel:
```
Task T029+T030 (Demo 04) | Task T031+T032 (Demo 06) | Task T033+T034 (Demo 09)
Task T035+T036 (Demo 10) | Task T037+T038 (Demo 14) | Task T039+T040 (Demo 15)
```

---

## Implementation Strategy

### MVP First (Phase 1 + Phase 2)

1. Complete Phase 1: Structural fixes (AllDemos integration, AutoRun dedup)
2. Complete Phase 2: Major improvements to 6 thinnest demos
3. **STOP and VALIDATE**: Run full suite, confirm 6 major demos are satisfying
4. This alone transforms the weakest demos into strong showcases

### Incremental Delivery

1. Phase 1 → Unified runner ✅
2. Phase 2 → 6 thinnest demos transformed ✅
3. Phase 3 → 3 moderate demos enriched ✅
4. Phase 4 → 6 strong demos polished ✅
5. Phase 5 → Full suite validated ✅

### Collaborative Workflow

Each demo follows: **Review → Discuss → Improve F# → Mirror Python → Confirm**

The improvement directions in the spec are starting points. Actual changes are determined collaboratively during implementation.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each F#/Python demo pair should be done together in the collaborative workflow
- Commit after each demo pair is confirmed satisfying
- Demo improvement directions are guidelines — actual changes determined collaboratively
- Body count limit: 500 per demo. Runtime limit: 30 seconds per demo.
