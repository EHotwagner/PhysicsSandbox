# Tasks: Stress Test Demos

**Input**: Design documents from `/specs/003-stress-test-demos/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add timing helper to Prelude.fsx and verify build before writing demos

- [x] T001 Add `timed` helper function to demos/Prelude.fsx — wraps an action with Stopwatch and prints `[TIME] label: N ms` format. Uses System.Diagnostics.Stopwatch. Signature: `let timed (label: string) (f: unit -> 'a) : 'a`
- [x] T002 Verify existing demos still run after Prelude.fsx change by running `dotnet fsi demos/AutoRun.fsx` against a live server (or confirm build with `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`)

**Checkpoint**: Prelude.fsx enhanced, existing demos unaffected

---

## Phase 2: User Story 1 — High-Volume Body Demos (Priority: P1) 🎯 MVP

**Goal**: Progressive body-scaling demo that discovers degradation points across 50/100/200/500 body tiers

**Independent Test**: Run Demo 11 standalone and confirm it completes all tiers, prints [TIME] markers per tier, and resets cleanly

### Implementation for User Story 1

- [x] T003 [US1] Add Demo 11 (Body Scaling) to demos/AllDemos.fsx — insert after the Demo 10 (Chaos Scene) entry at the end of the `demos` array (before the closing `|]`). New entry in the `demos` array. Implements tiered body creation: for each tier (50, 100, 200, 500), resetSimulation, create spheres in a grid via batchAdd, runFor 3 seconds, wrap each tier in `timed`. Camera pulls back at higher tiers. Print tier summary with body count and elapsed time. Handle batch failures gracefully with try/with around batchAdd and runFor.
- [x] T004 [P] [US1] Create standalone demos/Demo11_BodyScaling.fsx — loads Prelude.fsx, defines Demo 11 as a standalone module, connects to server (default http://localhost:5180 or CLI arg), runs the demo, disconnects. Follow pattern of existing Demo01_HelloDrop.fsx through Demo10_Chaos.fsx.

**Checkpoint**: Demo 11 runs end-to-end, reports timing per tier, identifies degradation point

---

## Phase 3: User Story 2 — Collision-Heavy Demos (Priority: P1)

**Goal**: Demos that maximize simultaneous collisions to stress collision detection — a pit scenario and a scaled-up domino cascade

**Independent Test**: Run Demo 12 and Demo 14 standalone, confirm 100+ bodies interact physically with timing reported

### Implementation for User Story 2

- [x] T005 [US2] Add Demo 12 (Collision Pit) to demos/AllDemos.fsx — new entry in the `demos` array. Create 4 static box walls (mass=0) forming a 4x4x4m pit using makeBoxCmd with mass 0.0 for static bodies. Then batchAdd 100–150 spheres positioned above the pit. runFor to let them drop and collide. Wrap setup and simulation in `timed`. Print settling observation. Handle failures gracefully.
- [x] T006 [P] [US2] Create standalone demos/Demo12_CollisionPit.fsx — loads Prelude.fsx, defines Demo 12 as standalone module.
- [x] T007 [US2] Add Demo 14 (Domino Cascade) to demos/AllDemos.fsx — new entry in the `demos` array. Create 100+ box dominoes (thin tall boxes, e.g., halfExtents 0.05, 0.3, 0.15) arranged in a curved path. Use trigonometry for curve (semicircle or spiral). Push first domino. Wrap in `timed` to report cascade propagation time. Camera follows along the path with setCamera at intervals.
- [x] T008 [P] [US2] Create standalone demos/Demo14_DominoCascade.fsx — loads Prelude.fsx, defines Demo 14 as standalone module.

**Checkpoint**: Demo 12 and 14 run end-to-end, 100+ bodies collide with timing output

---

## Phase 4: User Story 3 — Force & Interaction Demos (Priority: P2)

**Goal**: Demo that applies bulk forces (impulses, torques, gravity changes) to 100+ bodies simultaneously

**Independent Test**: Run Demo 13 standalone, confirm forces are applied to all bodies across 3 rounds with timing

### Implementation for User Story 3

- [x] T009 [US3] Add Demo 13 (Force Frenzy) to demos/AllDemos.fsx — new entry in the `demos` array. Create 100 spheres in a 10x10 grid via batchAdd. Let settle with runFor 2s. Then 3 rounds: (1) batch impulses to all 100 bodies with makeImpulseCmd, runFor 3s; (2) batch torques to all 100 bodies with makeTorqueCmd at 2x magnitude, change gravity to sideways, runFor 3s; (3) batch impulses at 3x magnitude, gravity reversed, runFor 3s. Wrap each round in `timed`. Restore gravity at end.
- [x] T010 [P] [US3] Create standalone demos/Demo13_ForceFrenzy.fsx — loads Prelude.fsx, defines Demo 13 as standalone module.

**Checkpoint**: Demo 13 runs end-to-end, 100 bodies respond to 3 rounds of forces with timing

---

## Phase 5: User Story 4 — Combined Stress Scenarios (Priority: P2)

**Goal**: "Overload" demo combining body count + collisions + forces + camera movement with per-stage timing

**Independent Test**: Run Demo 15 standalone, confirm 200+ bodies created, forces applied, camera swept, per-stage timing printed

### Implementation for User Story 4

- [x] T011 [US4] Add Demo 15 (Overload) to demos/AllDemos.fsx — new entry in the `demos` array. Multi-act scenario, each act wrapped in `timed`: Act 1 — build pyramid (generator, ~50 bodies) + stack (~10) + row (~10), runFor 2s; Act 2 — batchAdd 100 random spheres from height, runFor 3s; Act 3 — batch impulses to all bodies, runFor 3s; Act 4 — gravity flip up then sideways, runFor 2s each; Act 5 — camera sweep (8 angles), wireframe toggle. Print total elapsed at end. Handle any failures gracefully.
- [x] T012 [P] [US4] Create standalone demos/Demo15_Overload.fsx — loads Prelude.fsx, defines Demo 15 as standalone module.

**Checkpoint**: Demo 15 runs end-to-end with 200+ bodies, per-stage timing, and camera movement

---

## Phase 6: User Story 5 — MCP Tool Invocation (Priority: P3)

**Goal**: Validate that stress scenarios are reproducible via MCP batch tools with comparable results

**Independent Test**: Run body-scaling via MCP `start_stress_test` and `batch_commands`, compare output with script-based Demo 11

### Implementation for User Story 5

- [x] T013 [US5] Document MCP stress testing procedure in specs/003-stress-test-demos/quickstart.md — update the MCP section with step-by-step instructions for replicating Demo 11 (body scaling) and Demo 15 (overload) via MCP tools. For Demo 11: use start_stress_test(scenario: "body-scaling", max_bodies: 500) and get_stress_test_status. For Demo 15 (Overload) via MCP: (1) use generate_pyramid + generate_random_bodies for setup, (2) batch_commands with impulse commands for force application, (3) set_gravity for gravity changes, (4) set_camera for camera sweep, (5) get_diagnostics for per-stage pipeline timing. Include expected output format and how to compare with script results.
- [x] T014 [US5] Run MCP body-scaling scenario via `start_stress_test(scenario: "body-scaling", max_bodies: 500)` and `get_stress_test_status` to validate comparable results. Document observed differences in quickstart.md.

**Checkpoint**: MCP-based stress testing documented and validated

---

## Phase 7: Polish & Integration

**Purpose**: Integrate all demos into runners, validate full suite

- [x] T015 Update demos/AllDemos.fsx — verify all 5 new demo entries (11–15) are in the `demos` array in correct order, demo count is now 15
- [x] T016 Update demos/RunAll.fsx — update the header banner to show correct demo count (15 demos). No other changes needed since it iterates the demos array.
- [x] T017 Update demos/AutoRun.fsx — add all 5 new demos as inline self-contained functions following existing pattern (all helpers inlined, no #load dependencies). Update demo count in banner. Each demo must include full body of the demo function with all helpers (batchAdd, resetSimulation, timed, makeBoxCmd, etc.) already available from the file's preamble. Also add `timed` helper to AutoRun.fsx's preamble section (after `batchAdd` definition): `let timed (label: string) (f: unit -> 'a) = let sw = System.Diagnostics.Stopwatch.StartNew() in let r = f () in sw.Stop(); printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds; r`
- [x] T018 Run full demo suite via `dotnet fsi demos/AutoRun.fsx` to validate all 15 demos pass end-to-end without manual intervention. Confirm pass/fail counts in output. Verify that no demo produces unhandled exceptions — all should show `✓ Complete` or report errors via `[BATCH FAIL]`/`[ERROR]` prefixes without crashing the runner.
- [x] T019 Run full demo suite via `dotnet fsi demos/RunAll.fsx` interactively to validate demo presentation (titles, descriptions, timing output visible).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **US1 (Phase 2)**: Depends on Phase 1 (timed helper)
- **US2 (Phase 3)**: Depends on Phase 1 (timed helper); independent of US1
- **US3 (Phase 4)**: Depends on Phase 1 (timed helper); independent of US1/US2
- **US4 (Phase 5)**: Depends on Phase 1 (timed helper); independent of US1/US2/US3
- **US5 (Phase 6)**: Depends on at least US1 (Demo 11) being complete for comparison
- **Polish (Phase 7)**: Depends on all user stories (phases 2–5) being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 1 — no dependencies on other stories
- **US2 (P1)**: Can start after Phase 1 — no dependencies on other stories
- **US3 (P2)**: Can start after Phase 1 — no dependencies on other stories
- **US4 (P2)**: Can start after Phase 1 — no dependencies on other stories
- **US5 (P3)**: Depends on US1 for comparison baseline

### Within Each User Story

- AllDemos.fsx entry before standalone Demo*.fsx (so standalone can reference the pattern)
- AllDemos.fsx entries are the canonical implementation; standalone files are convenience wrappers

### Parallel Opportunities

- T003 and T005/T007/T009/T011 can all run in parallel (different entries in AllDemos.fsx, but same file — must be serialized)
- T004, T006, T008, T010, T012 are standalone files and CAN run in parallel with each other
- T015, T016, T017 are different files and can run in parallel

---

## Parallel Example: User Stories 1–4

```
# After Phase 1 (timed helper) is complete:

# AllDemos.fsx entries must be serialized (same file):
T003 → T005 → T007 → T009 → T011

# Standalone files can be parallelized:
T004, T006, T008, T010, T012 (all different files, all [P])
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T002)
2. Complete Phase 2: US1 — Demo 11 Body Scaling (T003–T004)
3. **STOP and VALIDATE**: Run Demo 11 to find degradation point
4. This alone delivers the core value: finding where the system breaks

### Incremental Delivery

1. Phase 1 → Setup complete
2. Phase 2 → Demo 11 (body scaling) — primary stress axis discovered
3. Phase 3 → Demos 12+14 (collisions) — collision limits discovered
4. Phase 4 → Demo 13 (forces) — force pipeline limits discovered
5. Phase 5 → Demo 15 (combined) — overall system ceiling discovered
6. Phase 6 → MCP validation — AI assistant workflow confirmed
7. Phase 7 → Full suite integrated and validated

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- No test tasks generated (not requested in spec — demos are self-testing via AutoRun pass/fail)
- All demos use existing Prelude.fsx helpers plus new `timed` helper
- AutoRun.fsx requires inline duplication of all demo code (existing convention)
- Commit after each demo is added and verified
