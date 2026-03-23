# Tasks: Enhance Demos with New Body Types and Fix Impacts

**Input**: Design documents from `/specs/005-enhance-demos/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Extend Prelude helpers and color palette so all demos can use new shape types, constraints, queries, and consistent colors.

- [x] T001 Add color palette constants (8 named colors: projectileColor, targetColor, structureColor, accentYellow, accentGreen, accentPurple, accentOrange, kinematicColor) to Scripting/demos/Prelude.fsx per research R8 color definitions
- [x] T002 Add shape builder helpers (makeTriangleCmd, makeConvexHullCmd, makeCompoundCmd) to Scripting/demos/Prelude.fsx per research R4 signatures, following existing makeSphereCmd/makeBoxCmd patterns
- [x] T003 Add kinematic and filter helpers (makeKinematicCmd, withMotionType, withCollisionFilter) to Scripting/demos/Prelude.fsx per research R4 signatures, using BodyMotionType.Kinematic and AddBody collision_group/collision_mask fields
- [x] T004 Add matching color palette constants and shape/kinematic helpers to Scripting/demos_py/prelude.py mirroring T001-T003 F# additions with Python naming conventions (snake_case)

**Checkpoint**: Prelude extensions ready — all downstream demo tasks can use new helpers.

---

## Phase 2: US1 — Fix Broken Impact Demos (Priority: P1) 🎯 MVP

**Goal**: Demo 03 (Crate Stack) boulder strikes tower centrally with dramatic force. Demo 04 (Bowling Alley) ball approaches pyramid head-on and scatters bricks.

**Independent Test**: `dotnet fsi Scripting/demos/03_CrateStack.fsx` — boulder hits tower center, topples majority. `dotnet fsi Scripting/demos/04_BowlingAlley.fsx` — ball approaches frontally, scatters majority.

### Implementation for US1

- [x] T005 [US1] Fix Demo 03 (Crate Stack) in Scripting/demos/AllDemos.fsx: move boulder spawn to (-4, 0.5, 0), use `launch` helper aimed at tower center of mass (~Y=3), increase speed for dramatic impact, add projectileColor to boulder and targetColor to crates per research R1
- [x] T006 [US1] Fix Demo 04 (Bowling Alley) in Scripting/demos/AllDemos.fsx: place pyramid at (0, 0, 5) instead of (5, 0, 0), place ball at (0, 0.5, -2) with Z-axis impulse (0, 0, 300+), add projectileColor to ball and targetColor to bricks per research R2
- [x] T007 [P] [US1] Update standalone Scripting/demos/03_CrateStack.fsx to match AllDemos fix from T005
- [x] T008 [P] [US1] Update standalone Scripting/demos/04_BowlingAlley.fsx to match AllDemos fix from T006
- [ ] T009 [P] [US1] Mirror Demo 03 and Demo 04 fixes to Scripting/demos_py/all_demos.py and standalone Scripting/demos_py/demo_03_crate_stack.py, Scripting/demos_py/demo_04_bowling_alley.py

**Checkpoint**: Impact demos fixed — run AutoRun.fsx and verify demos 03+04 show central, dramatic destruction.

---

## Phase 3: US2 — Showcase Constraint Physics (Priority: P1)

**Goal**: New Demo 16 demonstrates 4 constraint types (ball socket, hinge, weld, distance limit) in three acts: pendulum chain, hinged bridge, weld cluster.

**Independent Test**: `dotnet fsi Scripting/demos/16_Constraints.fsx` — pendulum swings, bridge flexes under load, weld cluster tumbles as single rigid body.

### Implementation for US2

- [x] T010 [US2] Create Scripting/demos/16_Constraints.fsx with three acts per research R5: Act 1 — 5 spheres linked by ball-socket + distance-limit constraints hanging from static anchor, disturb first sphere to create wave; Act 2 — 6 box planks linked by hinge constraints between static pillars, drop heavy spheres on bridge; Act 3 — 4 boxes welded into cross shape and dropped onto a pile. Use accentYellow for pendulum, accentOrange for bridge planks, accentPurple for weld cluster, bouncy material on pendulum end per R9
- [x] T011 [US2] Add Demo 16 entry to Scripting/demos/AllDemos.fsx — register as { Name = "Constraints"; Description = "Pendulum chain, hinged bridge, and weld cluster — four constraint types in action"; Run = run16 } where run16 contains the same logic as 16_Constraints.fsx
- [ ] T012 [P] [US2] Create Scripting/demos_py/demo_16_constraints.py mirroring F# Demo 16 logic using prelude.py helpers
- [ ] T013 [P] [US2] Add Demo 16 entry to Scripting/demos_py/all_demos.py matching F# registration

**Checkpoint**: Constraint demo working — 4 constraint types demonstrated (ball socket, hinge, weld, distance limit) satisfying SC-003.

---

## Phase 4: US3 — Showcase Advanced Shape Types (Priority: P1)

**Goal**: Distribute capsule, cylinder, triangle, convex hull, and compound shapes across existing demos so at least 7 of 10 shape types are used (SC-002).

**Independent Test**: Run full AutoRun.fsx, grep output for shape names — confirm capsule, cylinder, triangle, convex hull, compound each appear in at least one demo.

### Implementation for US3

- [x] T014 [US3] Enhance Demo 05 (Marble Rain) in Scripting/demos/AllDemos.fsx: replace some crates in wave 2 with capsules (makeC capsuleCmd, "log" shapes) and cylinders (makeCylinderCmd, "barrel" shapes), add accentGreen for capsules and accentOrange for cylinders
- [x] T015 [US3] Enhance Demo 07 (Spinning Tops) in Scripting/demos/AllDemos.fsx: replace 1 sphere with a capsule and 1 box with a cylinder in the spinning ring, add per-shape colors
- [x] T016 [US3] Enhance Demo 08 (Gravity Flip) in Scripting/demos/AllDemos.fsx: add 2-3 triangle "ramp" shapes (makeTriangleCmd) and 2-3 convex hull "octahedron" shapes (makeConvexHullCmd with 6-point octahedron), add per-shape-type colors
- [x] T017 [US3] Enhance Demo 11 (Body Scaling) in Scripting/demos/AllDemos.fsx: add capsules and cylinders to the alternating shape mix, add 2-3 compound "dumbbell" bodies (makeCompoundCmd with two spheres offset along X) at 200+ body tier
- [x] T018 [US3] Enhance Demo 12 (Collision Pit) in Scripting/demos/AllDemos.fsx: add convex hull tetrahedra (4-point) to wave 2 and compound bodies to wave 3, add colors per wave (accentYellow wave 1, accentGreen wave 2, accentPurple wave 3)
- [ ] T019 [P] [US3] Update standalone files Scripting/demos/05_MarbleRain.fsx, 07_SpinningTops.fsx, 08_GravityFlip.fsx, 11_BodyScaling.fsx, 12_CollisionPit.fsx to match AllDemos enhancements from T014-T018
- [ ] T020 [P] [US3] Mirror all shape enhancements to Python: update demo_05, demo_07, demo_08, demo_11, demo_12 in Scripting/demos_py/ and Scripting/demos_py/all_demos.py

**Checkpoint**: Shape coverage expanded — 8 of 10 shape types used across suite (sphere, box, capsule, cylinder, triangle, convex hull, compound, plane) satisfying SC-002.

---

## Phase 5: US4 — Showcase Physics Queries (Priority: P2)

**Goal**: New Demo 17 demonstrates raycast, sweep cast, and overlap queries with printed results.

**Independent Test**: `dotnet fsi Scripting/demos/17_QueryRange.fsx` — console prints hit body IDs, positions, distances for raycasts; overlap count for sphere test; sweep hit for sphere sweep.

### Implementation for US4

- [x] T021 [US4] Create Scripting/demos/17_QueryRange.fsx per research R6: drop 20 random colored bodies into a walled pit, settle 3s, then fire 5 downward raycasts from different X positions printing hit results (bodyId, position, distance), perform overlapSphere at pit center printing body count, sweep a sphere across the pit printing first hit. Handle empty results gracefully (print "no hit")
- [x] T022 [US4] Add Demo 17 entry to Scripting/demos/AllDemos.fsx — register as { Name = "Query Range"; Description = "Raycasts, overlap tests, and sweep casts — physics queries in action"; Run = run17 }
- [ ] T023 [P] [US4] Create Scripting/demos_py/demo_17_query_range.py mirroring F# Demo 17
- [ ] T024 [P] [US4] Add Demo 17 entry to Scripting/demos_py/all_demos.py

**Checkpoint**: Query demo working — raycast, overlapSphere, sweepSphere all demonstrated with printed output satisfying SC-004.

---

## Phase 6: US5 — Expand Colors and Materials (Priority: P2)

**Goal**: At least 8 demos use custom colors (SC-006) and at least 3 use material presets with visible contrast (SC-007). Apply to demos not already enhanced in earlier phases.

**Independent Test**: Run AutoRun.fsx and visually confirm color variety across demos. For material demos, observe behavioral differences (bouncy vs. sticky vs. slippery).

### Implementation for US5

- [x] T025 [US5] Enhance Demo 01 (Hello Drop) in Scripting/demos/AllDemos.fsx: ensure existing colors are using named palette constants, apply bouncy material to beach ball and sticky material to bowling ball per R9 for visible bounce contrast, add 1 capsule and 1 cylinder body to the drop lineup with accentGreen/accentOrange colors per plan and research R10
- [x] T026 [US5] Enhance Demo 02 (Bouncing Marbles) in Scripting/demos/AllDemos.fsx: add accentYellow to wave 1 marbles, accentGreen to wave 2 marbles
- [x] T027 [US5] Enhance Demo 06 (Domino Row) in Scripting/demos/AllDemos.fsx: apply color gradient along the 20 dominoes (interpolate from targetColor to accentPurple) using makeColor with computed R/G/B values
- [x] T028 [US5] Enhance Demo 09 (Billiards) in Scripting/demos/AllDemos.fsx: add per-ball colors (alternate accentYellow, accentGreen, accentOrange, accentPurple for formation; projectileColor for cue ball), apply slippery material to all balls per R9
- [x] T029 [US5] Enhance Demo 10 (Chaos Scene) in Scripting/demos/AllDemos.fsx: add targetColor to pyramid bricks, accentOrange to stack crates, accentGreen to row spheres, projectileColor to boulder and projectiles; fix boulder targeting to use `launch` aimed at pyramid center; add cylinder "pillars" to the stage setup per plan and research R10
- [x] T030 [US5] Enhance Demo 13 (Force Frenzy) in Scripting/demos/AllDemos.fsx: apply bouncy material + accentYellow to first 40 bodies, sticky material + accentPurple to remaining 40 bodies per R9 for visible behavioral contrast
- [x] T031 [US5] Enhance Demo 14 (Domino Cascade) in Scripting/demos/AllDemos.fsx: apply color gradient along the 120 semicircular dominoes (interpolate from targetColor to projectileColor)
- [x] T032 [US5] Enhance Demo 15 (Overload) in Scripting/demos/AllDemos.fsx: add targetColor to pyramid, accentOrange to stack, accentGreen to row, accentYellow to rain spheres, mix in capsules + cylinders in the 100-sphere rain
- [ ] T033 [P] [US5] Update standalone files Scripting/demos/01_HelloDrop.fsx, 02_BouncingMarbles.fsx, 06_DominoRow.fsx, 09_Billiards.fsx, 10_ChaosScene.fsx, 13_ForceFrenzy.fsx, 14_DominoCascade.fsx, 15_Overload.fsx to match AllDemos enhancements from T025-T032
- [ ] T034 [P] [US5] Mirror all color/material enhancements to Python: update demo_01, demo_02, demo_06, demo_09, demo_10, demo_13, demo_14, demo_15 in Scripting/demos_py/ and Scripting/demos_py/all_demos.py

**Checkpoint**: Color and material coverage complete — 12+ demos use custom colors (SC-006 ≥8), 4 demos use material presets with visible contrast (SC-007 ≥3).

---

## Phase 7: US6 — Demonstrate Kinematic Bodies (Priority: P3)

**Goal**: New Demo 18 demonstrates a kinematic body plowing through dynamic bodies.

**Independent Test**: `dotnet fsi Scripting/demos/18_KinematicSweep.fsx` — cyan kinematic box moves through scene, dynamic bodies are pushed aside, kinematic path unaffected.

### Implementation for US6

- [x] T035 [US6] Create Scripting/demos/18_KinematicSweep.fsx per research R7: place 30 small dynamic spheres in a grid with accentYellow color, create kinematic box "bulldozer" at one end with kinematicColor using makeKinematicCmd, animate by looping (pause → makeSetBodyPoseCmd to incrementally advance X position → play → sleep 100ms) for 20 steps, camera follows bulldozer
- [x] T036 [US6] Add Demo 18 entry to Scripting/demos/AllDemos.fsx — register as { Name = "Kinematic Sweep"; Description = "A kinematic bulldozer plows through dynamic bodies — scripted path meets physics"; Run = run18 }
- [ ] T037 [P] [US6] Create Scripting/demos_py/demo_18_kinematic_sweep.py mirroring F# Demo 18
- [ ] T038 [P] [US6] Add Demo 18 entry to Scripting/demos_py/all_demos.py

**Checkpoint**: Kinematic demo working — kinematic body moves on scripted path pushing dynamic bodies satisfying FR-009.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final registry updates, full suite validation, success criteria verification.

- [x] T039 Verify AllDemos.fsx in Scripting/demos/AllDemos.fsx has all 18 demos registered in order (01-18) with updated names and descriptions reflecting enhancements
- [ ] T040 Verify all_demos.py in Scripting/demos_py/all_demos.py has all 18 Python demos registered matching F# order and descriptions
- [x] T041 Run full F# suite: `dotnet fsi Scripting/demos/AutoRun.fsx` — verify "Results: 18 passed, 0 failed" (SC-001)
- [ ] T042 Run full Python suite: `python Scripting/demos_py/auto_run.py` — verify "Results: 18 passed, 0 failed" (SC-008 parity)
- [ ] T043 Validate success criteria: SC-002 (count shape types ≥7), SC-003 (count constraint types ≥3), SC-004 (query demo prints results), SC-005 (Demo 03+04 impact alignment), SC-006 (count colored demos ≥8), SC-007 (count material demos ≥3)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **US1 (Phase 2)**: Depends on Phase 1 (needs color palette from Prelude)
- **US2 (Phase 3)**: Depends on Phase 1 (needs constraint helpers already in Prelude)
- **US3 (Phase 4)**: Depends on Phase 1 (needs new shape helpers)
- **US4 (Phase 5)**: Depends on Phase 1 (needs query helpers already in Prelude)
- **US5 (Phase 6)**: Depends on Phase 1 (needs color palette)
- **US6 (Phase 7)**: Depends on Phase 1 (needs kinematic helper)
- **Polish (Phase 8)**: Depends on all user story phases complete

### User Story Independence

- **US1 (Fix impacts)**: Independent — only touches Demo 03 + 04
- **US2 (Constraints)**: Independent — new Demo 16 file
- **US3 (Advanced shapes)**: Independent — touches Demo 05, 07, 08, 11, 12
- **US4 (Queries)**: Independent — new Demo 17 file
- **US5 (Colors/materials)**: Independent — touches Demo 01, 02, 06, 09, 10, 13, 14, 15
- **US6 (Kinematic)**: Independent — new Demo 18 file

**No file conflicts**: US3 and US5 touch different demo files (no overlap). US1 touches 03+04, US5 touches 10 (Chaos Scene), so Demo 10's boulder fix (T029) is in US5 not US1.

### Within Each User Story

1. AllDemos.fsx modifications first (primary source)
2. Standalone .fsx files updated to match (can parallel)
3. Python files mirrored (can parallel with standalone)

### Parallel Opportunities

- After Phase 1 completes: US1, US2, US3, US4, US5, US6 can ALL start in parallel
- Within each US: AllDemos tasks sequential, standalone + Python tasks parallel
- New demo creation (US2, US4, US6): F# standalone + AllDemos entry, then Python in parallel

---

## Parallel Example: After Phase 1

```text
# All user stories can launch simultaneously:
US1: T005 → T006 → [T007, T008, T009 in parallel]
US2: T010 → T011 → [T012, T013 in parallel]
US3: T014 → T015 → T016 → T017 → T018 → [T019, T020 in parallel]
US4: T021 → T022 → [T023, T024 in parallel]
US5: T025 → ... → T032 → [T033, T034 in parallel]
US6: T035 → T036 → [T037, T038 in parallel]
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Prelude extensions
2. Complete Phase 2: Fix Demo 03 + 04
3. **STOP and VALIDATE**: Run AutoRun.fsx — 15 pass, 0 fail, impacts fixed
4. Proceed to remaining stories

### Incremental Delivery

1. Setup → US1 (fix impacts) → Validate → Working base
2. Add US2 (constraints) → Validate Demo 16 → 16 demos passing
3. Add US3 (shapes) → Validate enhanced demos → Shape coverage met
4. Add US4 (queries) → Validate Demo 17 → 17 demos passing
5. Add US5 (colors/materials) → Validate visual enhancements → Color coverage met
6. Add US6 (kinematic) → Validate Demo 18 → 18 demos passing
7. Polish → Full suite validation → All SC met

---

## Notes

- AllDemos.fsx contains all demos INLINE — each demo is a function in a single array. Modifications to different demos in this file are sequential (same file).
- Standalone .fsx files duplicate AllDemos logic for individual execution. Keep in sync.
- Python demos mirror F# exactly — same scene, same timing, same parameters.
- Color constants use (R, G, B, A) floats 0.0-1.0. Material presets: bouncyMaterial, stickyMaterial, slipperyMaterial already in Prelude.
- Convex hull points: tetrahedron = 4 vertices, octahedron = 6 vertices. Keep simple.
- Kinematic animation: pause → setBodyPose → play → sleep loop. ~20 discrete steps.
