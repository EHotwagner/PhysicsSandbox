# Tasks: Demo Script Modernization

**Input**: Design documents from `/specs/001-demo-script-modernization/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not requested. Verification is via manual `dotnet fsi` execution of AutoRun and individual demo scripts.

**Organization**: Tasks grouped by user story. US3 (Prelude helpers) is foundational — must complete before US1/US2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Foundational — Prelude Helpers (US3, Priority: P2 but blocking)

**Goal**: Add batch command builders, `resetSimulation`, `batchAdd`, and `nextId` helpers to the shared Prelude module. All other phases depend on this.

**Independent Test**: Write a minimal test script that loads Prelude.fsx and calls `resetSimulation`, `makeSphereCmd`, and `batchAdd` to verify the helpers work.

- [x] T001 [US3] Add `open PhysicsSandbox.Shared.Contracts` to module opens in `demos/Prelude.fsx`
- [x] T002 [US3] Add `toVec3` local helper function (Vec3 construction) in `demos/Prelude.fsx`
- [x] T003 [US3] Replace `resetScene` with `resetSimulation` (server-side reset + addPlane + setGravity) in `demos/Prelude.fsx` — on reset failure, print clear error message before re-throwing (FR-010)
- [x] T004 [US3] Add `makeSphereCmd` command builder in `demos/Prelude.fsx` — returns `SimulationCommand` with `AddBody` sphere shape
- [x] T005 [US3] Add `makeBoxCmd` command builder in `demos/Prelude.fsx` — returns `SimulationCommand` with `AddBody` box shape
- [x] T006 [US3] Add `makeImpulseCmd` command builder in `demos/Prelude.fsx` — returns `SimulationCommand` with `ApplyImpulse`
- [x] T007 [US3] Add `makeTorqueCmd` command builder in `demos/Prelude.fsx` — returns `SimulationCommand` with `ApplyTorque`
- [x] T008 [US3] Add `batchAdd` helper in `demos/Prelude.fsx` — auto-splits at 100 commands, calls `batchCommands` per chunk, prints failed command indices on partial failure (FR-010)
- [x] T009 [US3] Add `nextId` re-export in `demos/Prelude.fsx` — wraps `PhysicsClient.IdGenerator.nextId`

**Checkpoint**: Prelude.fsx has all helpers. Individual demos and AllDemos can now be updated.

---

## Phase 2: User Story 2 — Simulation Reset in All Demos (Priority: P1)

**Goal**: Replace `resetScene` with `resetSimulation` in all 10 individual demo scripts. These are independent file changes.

**Independent Test**: Run `dotnet fsi demos/Demo01_HelloDrop.fsx` (or any demo) and verify it starts with a clean simulation state (time reset, no leftover bodies).

- [x] T010 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo01_HelloDrop.fsx`
- [x] T011 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo02_BouncingMarbles.fsx`
- [x] T012 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo03_CrateStack.fsx`
- [x] T013 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo04_BowlingAlley.fsx`
- [x] T014 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo05_MarbleRain.fsx`
- [x] T015 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo06_DominoRow.fsx`
- [x] T016 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo07_SpinningTops.fsx`
- [x] T017 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo08_GravityFlip.fsx`
- [x] T018 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo09_Billiards.fsx`
- [x] T019 [P] [US2] Replace `resetScene` with `resetSimulation` in `demos/Demo10_Chaos.fsx`

**Checkpoint**: All 10 demos use server-side reset. Each demo can be run standalone.

---

## Phase 3: User Story 1 — Batch Commands in Demos (Priority: P1)

**Goal**: Convert sequential body/force creation to batched commands in demos with 3+ sequential calls. Each demo file is independent.

**Independent Test**: Run `dotnet fsi demos/Demo06_DominoRow.fsx` or `demos/Demo09_Billiards.fsx` and verify all bodies appear correctly and setup is visibly faster.

- [x] T020 [P] [US1] Batch 5 marble creates in `demos/Demo02_BouncingMarbles.fsx` — use `makeSphereCmd` (r=0.01, m=0.005) + `batchAdd`
- [x] T021 [P] [US1] Batch 12 box creates in `demos/Demo06_DominoRow.fsx` — use `makeBoxCmd` + `batchAdd`, pre-generate IDs for `push` reference
- [x] T022 [P] [US1] Batch 4 body creates and 4 torque applies in `demos/Demo07_SpinningTops.fsx` — use `makeSphereCmd` (beachBall: r=0.2, m=0.1) + `makeBoxCmd` (crate: half=0.5, m=20.0) + `makeTorqueCmd` + `batchAdd`
- [x] T023 [P] [US1] Batch 5 beach ball creates in `demos/Demo08_GravityFlip.fsx` — use `makeSphereCmd` (r=0.2, m=0.1) + `batchAdd`
- [x] T024 [P] [US1] Batch 16 sphere creates in `demos/Demo09_Billiards.fsx` — use `makeSphereCmd` (r=0.1, m=0.17 for balls + r=0.11 for cue) + `batchAdd`, pre-generate cue ID for `launch`
- [x] T025 [P] [US1] Batch 10 impulse applies in `demos/Demo10_Chaos.fsx` — use `makeImpulseCmd` + `batchAdd` for projectile pushes

**Checkpoint**: All applicable demos use batch commands. Demos 01, 03, 04, 05 unchanged (single body or generator-based).

---

## Phase 4: User Story 4 — AllDemos and AutoRun Sync (Priority: P2)

**Goal**: Mirror all demo changes into AllDemos.fsx (shared via #load) and AutoRun.fsx (self-contained duplicate).

**Independent Test**: Run `dotnet fsi demos/AutoRun.fsx` — all 10 demos pass with 0 failures.

- [x] T026 [US4] Add `open PhysicsSandbox.Shared.Contracts` to `demos/AllDemos.fsx`
- [x] T027 [US4] Update all 10 inline demo functions in `demos/AllDemos.fsx` — mirror resetSimulation + batching changes from Phase 2 and Phase 3
- [x] T028 [US4] Add all Prelude helpers (toVec3, resetSimulation, command builders, batchAdd, nextId) inline in `demos/AutoRun.fsx`
- [x] T029 [US4] Update all 10 inline demo functions in `demos/AutoRun.fsx` — mirror resetSimulation + batching changes from Phase 2 and Phase 3
- [x] T030 [US4] Replace final `resetScene s` cleanup call with `resetSimulation s` in `demos/AutoRun.fsx`
- [x] T031 [US4] Replace `resetScene s` cleanup call with `resetSimulation s` in `demos/RunAll.fsx`

**Checkpoint**: All execution modes (individual, RunAll, AutoRun) produce consistent results.

---

## Phase 5: Polish & Verification

**Purpose**: End-to-end validation across all execution modes.

- [x] T032 Run `dotnet fsi demos/AutoRun.fsx` — verify all 10 demos pass (SC-001)
- [x] T033 Run `dotnet fsi demos/Demo06_DominoRow.fsx` standalone — verify standalone execution works (FR-009)
- [x] T034 Run `dotnet fsi demos/Demo09_Billiards.fsx` standalone — verify batched demo works standalone (FR-009)
- [x] T035 Verify no `resetScene` references remain across all `demos/*.fsx` files
- [x] T036 Time Demo09_Billiards setup phase before and after batching — verify 30%+ speedup (SC-002)
- [x] T037 Compare total line count of `demos/*.fsx` before and after changes — verify net-neutral or net reduction (SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Prelude helpers)**: No dependencies — start immediately. BLOCKS all other phases.
- **Phase 2 (Reset in demos)**: Depends on Phase 1 (T003 specifically). All T010-T019 are parallel.
- **Phase 3 (Batch in demos)**: Depends on Phase 1 (T004-T009). All T020-T025 are parallel.
- **Phase 4 (AllDemos/AutoRun)**: Depends on Phase 2 + Phase 3. Must mirror final demo state.
- **Phase 5 (Verification)**: Depends on all previous phases.

### User Story Dependencies

- **US3 (Prelude)**: Foundational — must complete first, no dependencies on other stories
- **US2 (Reset)**: Depends on US3 for `resetSimulation` helper
- **US1 (Batching)**: Depends on US3 for command builders + `batchAdd`
- **US4 (AutoRun/AllDemos)**: Depends on US1 + US2 for final demo state to mirror
- **US1 and US2 are independent of each other** — can run in parallel after US3

### Parallel Opportunities

- T010-T019 (all reset changes): 10 tasks across 10 files, fully parallel
- T020-T025 (all batching changes): 6 tasks across 6 files, fully parallel
- Phase 2 and Phase 3 can run in parallel (different aspects of same files, but changes are in different code sections)

---

## Parallel Example: Phase 2 + Phase 3

```bash
# After Phase 1 completes, launch all demo updates in parallel:

# Reset changes (Phase 2):
Task: "Replace resetScene with resetSimulation in demos/Demo01_HelloDrop.fsx"
Task: "Replace resetScene with resetSimulation in demos/Demo03_CrateStack.fsx"
Task: "Replace resetScene with resetSimulation in demos/Demo04_BowlingAlley.fsx"
Task: "Replace resetScene with resetSimulation in demos/Demo05_MarbleRain.fsx"

# Batch + Reset changes (Phase 2+3 combined — these demos get both):
Task: "Replace resetScene + batch 5 marbles in demos/Demo02_BouncingMarbles.fsx"
Task: "Replace resetScene + batch 12 boxes in demos/Demo06_DominoRow.fsx"
Task: "Replace resetScene + batch bodies+torques in demos/Demo07_SpinningTops.fsx"
Task: "Replace resetScene + batch 5 beach balls in demos/Demo08_GravityFlip.fsx"
Task: "Replace resetScene + batch 16 spheres in demos/Demo09_Billiards.fsx"
Task: "Replace resetScene + batch 10 impulses in demos/Demo10_Chaos.fsx"
```

---

## Implementation Strategy

### MVP First (US3 + US2 Only)

1. Complete Phase 1: Prelude helpers
2. Complete Phase 2: Reset in all demos
3. **STOP and VALIDATE**: Run AutoRun — all 10 demos should pass with server-side reset
4. This alone delivers clean state management (SC-003)

### Full Delivery

1. Phase 1: Prelude helpers → Foundation ready
2. Phase 2 + 3 in parallel: Reset + Batching in demos
3. Phase 4: Mirror to AllDemos + AutoRun
4. Phase 5: End-to-end verification
5. All success criteria (SC-001 through SC-005) validated

---

## Notes

- Preset dimensions must match exactly: marble (r=0.01, m=0.005), beachBall (r=0.2, m=0.1), crate (half=0.5, m=20.0). Source: `src/PhysicsClient/Bodies/Presets.fs`
- `toVec3` is `internal` in PhysicsClient — must be redefined locally in Prelude
- Demos using generators (03, 04, 05) don't need batching — generators handle their own sequential calls internally
- AutoRun.fsx is self-contained (doesn't #load Prelude) — must duplicate all helpers inline
- 37 total tasks: 9 foundational, 10 reset, 6 batching, 6 sync, 6 verification
