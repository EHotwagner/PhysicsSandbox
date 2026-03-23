# Tasks: Refactor Evaluation Analysis

**Input**: Design documents from `/specs/005-refactor-evaluation/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Establish report structure and analysis infrastructure

- [x] T001 Create reports/ directory at repository root
- [x] T002 Create report skeleton with all required sections in reports/refactorEvaluation.md

**Checkpoint**: Report file exists with section headings matching spec requirements (FR-001 through FR-008) ✓

---

## Phase 2: Foundational (Analysis Infrastructure)

**Purpose**: Read and analyse all source projects to gather raw findings before writing report sections

- [x] T003 [P] Analyse PhysicsServer project (src/PhysicsServer/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T004 [P] Analyse PhysicsSimulation project (src/PhysicsSimulation/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T005 [P] Analyse PhysicsViewer project (src/PhysicsViewer/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T006 [P] Analyse PhysicsClient project (src/PhysicsClient/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T007 [P] Analyse PhysicsSandbox.Mcp project (src/PhysicsSandbox.Mcp/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T008 [P] Analyse PhysicsSandbox.Scripting project (src/PhysicsSandbox.Scripting/) — read all .fs/.fsi files, count LOC, identify issues
- [x] T009 [P] Analyse AppHost, ServiceDefaults, Shared.Contracts (src/PhysicsSandbox.AppHost/, src/PhysicsSandbox.ServiceDefaults/, src/PhysicsSandbox.Shared.Contracts/) — read all source files, count LOC
- [x] T010 [P] Analyse test projects (tests/*/) and scripting layers (Scripting/demos/, Scripting/demos_py/) — count tests, LOC, identify patterns
- [x] T011 Identify cross-cutting issues: duplication patterns, ID generation inconsistency, error handling inconsistency, proto type conflicts across all projects

**Checkpoint**: Raw analysis data collected for all 9 source projects + tests + scripting. Ready to write report sections. ✓

---

## Phase 3: User Story 1 — Developer Reviews Codebase Health (Priority: P1) 🎯 MVP

**Goal**: Produce per-project quality analysis with ratings, issue tables, and LOC metrics (FR-001, FR-002, FR-003, FR-008)

**Independent Test**: Verify each project in Part 1 of the report has a numeric rating, issue table with severity/location/suggestion, and LOC count

### Implementation for User Story 1

- [x] T012 [US1] Write Executive Summary section with overall rating and project ratings table in reports/refactorEvaluation.md
- [x] T013 [P] [US1] Write Part 1.1 PhysicsServer analysis (rating, issue table, refactor estimate) in reports/refactorEvaluation.md
- [x] T014 [P] [US1] Write Part 1.2 PhysicsSimulation analysis (rating, issue table, refactor estimate) in reports/refactorEvaluation.md
- [x] T015 [P] [US1] Write Part 1.3 PhysicsViewer analysis (rating, issue table, refactor estimate) in reports/refactorEvaluation.md
- [x] T016 [P] [US1] Write Part 1.4 PhysicsClient analysis (rating, issue table, refactor estimate) in reports/refactorEvaluation.md
- [x] T017 [P] [US1] Write Part 1.5 PhysicsSandbox.Mcp analysis (rating, issue table, refactor estimate) in reports/refactorEvaluation.md
- [x] T018 [P] [US1] Write Part 1.6 PhysicsSandbox.Scripting analysis (rating, issue table) in reports/refactorEvaluation.md
- [x] T019 [P] [US1] Write Part 1.7 Infrastructure projects analysis (AppHost, ServiceDefaults, Contracts) in reports/refactorEvaluation.md
- [x] T020 [P] [US1] Write Part 1.8 Tests & Scripting analysis in reports/refactorEvaluation.md
- [x] T021 [US1] Write Part 2 Cross-Cutting Issues (duplication table, ID inconsistency, error handling, proto conflicts) in reports/refactorEvaluation.md
- [x] T022 [US1] Write Appendix LOC Summary table in reports/refactorEvaluation.md
- [x] T023 [US1] Validate FR-001: verify all 9 source projects + tests + scripting are covered in reports/refactorEvaluation.md
- [x] T024 [US1] Validate FR-002: verify every project has a numeric 1-10 rating with justification in reports/refactorEvaluation.md
- [x] T025 [US1] Validate FR-003: verify specific duplication, spaghetti, coupling, and complexity issues per project in reports/refactorEvaluation.md
- [x] T026 [US1] Validate FR-008: verify LOC metrics table is present and complete in reports/refactorEvaluation.md

**Checkpoint**: Part 1 (per-project analysis) and Part 2 (cross-cutting) complete. Each project rated with issues documented. SC-001 satisfied. ✓

---

## Phase 4: User Story 2 — Decision Maker Evaluates Alternatives (Priority: P2)

**Goal**: Present 3+ alternative approaches with trade-off analysis and clear recommendation (FR-005)

**Independent Test**: Verify Part 3 contains at least 3 alternatives, each with scope, effort, risk, pros, and cons

### Implementation for User Story 2

- [x] T027 [P] [US2] Write Alternative A (Targeted Refactoring) with scope, effort, risk, pros, cons in reports/refactorEvaluation.md
- [x] T028 [P] [US2] Write Alternative B (Major Refactoring) with scope, effort, risk, pros, cons in reports/refactorEvaluation.md
- [x] T029 [P] [US2] Write Alternative C (Full Rewrite) with scope, effort, risk, pros, cons in reports/refactorEvaluation.md
- [x] T030 [P] [US2] Write Alternative D (Partial Rewrite) with scope, effort, risk, pros, cons in reports/refactorEvaluation.md
- [x] T031 [US2] Write Part 5 BepuFSharp Wrapper Assessment in reports/refactorEvaluation.md
- [x] T032 [US2] Validate FR-005: verify at least 3 alternatives with trade-offs in reports/refactorEvaluation.md
- [x] T033 [US2] Validate SC-002: verify recommendation is supported by per-project evidence in reports/refactorEvaluation.md

**Checkpoint**: Part 3 (alternatives) and Part 5 (BepuFSharp) complete. Decision maker can compare options. SC-002 satisfied. ✓

---

## Phase 5: User Story 3 — Team Prioritizes Refactoring Work (Priority: P3)

**Goal**: Deliver prioritized refactoring roadmap with effort/impact per item (FR-006)

**Independent Test**: Verify Part 4 contains at least 10 prioritized items, each with impact, effort, and ROI

### Implementation for User Story 3

- [x] T034 [US3] Write Part 4 Recommended Approach section with justification in reports/refactorEvaluation.md
- [x] T035 [US3] Write Prioritized Refactoring Roadmap table (13 items with priority, target, impact, effort, ROI) in reports/refactorEvaluation.md
- [x] T036 [US3] Validate FR-006: verify roadmap has effort/impact assessment per item in reports/refactorEvaluation.md
- [x] T037 [US3] Validate SC-003: verify at least 10 specific actionable items in reports/refactorEvaluation.md

**Checkpoint**: Part 4 (recommendation + roadmap) complete. Team can plan work. SC-003 satisfied. ✓

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and report completeness

- [x] T038 Validate FR-004: verify solution-level architectural analysis covers dependency topology in reports/refactorEvaluation.md
- [x] T039 Validate FR-007: verify report exists at reports/refactorEvaluation.md
- [x] T040 Validate SC-004: verify report is self-contained and readable without additional context in reports/refactorEvaluation.md
- [x] T041 Run quickstart.md validation — verify quickstart instructions accurately describe how to use the report in specs/005-refactor-evaluation/quickstart.md
- [x] T042 Final review: check all tables render correctly, all section links work, no placeholder text remains in reports/refactorEvaluation.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - US1 (Phase 3) can proceed independently
  - US2 (Phase 4) can proceed independently (references US1 data but writes separate sections)
  - US3 (Phase 5) can proceed independently (references US1 data but writes separate sections)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 — No dependencies on other stories
- **User Story 2 (P2)**: Can start after Phase 2 — References US1 ratings but writes to different sections
- **User Story 3 (P3)**: Can start after Phase 2 — References US1 analysis but writes to different sections

### Parallel Opportunities

- T003–T010 (all project analyses) can run fully in parallel
- T013–T020 (per-project report sections) can run in parallel
- T027–T030 (alternative descriptions) can run in parallel
- US1, US2, US3 phases can proceed in parallel (different report sections)

---

## Parallel Example: User Story 1

```bash
# Launch all per-project analyses together (Phase 2):
Task: "Analyse PhysicsServer project (src/PhysicsServer/)"
Task: "Analyse PhysicsSimulation project (src/PhysicsSimulation/)"
Task: "Analyse PhysicsViewer project (src/PhysicsViewer/)"
Task: "Analyse PhysicsClient project (src/PhysicsClient/)"
Task: "Analyse PhysicsSandbox.Mcp project (src/PhysicsSandbox.Mcp/)"
Task: "Analyse PhysicsSandbox.Scripting project (src/PhysicsSandbox.Scripting/)"
Task: "Analyse infrastructure projects"
Task: "Analyse test projects and scripting layers"

# Launch all per-project report sections together (Phase 3):
Task: "Write Part 1.1 PhysicsServer analysis"
Task: "Write Part 1.2 PhysicsSimulation analysis"
Task: "Write Part 1.3 PhysicsViewer analysis"
Task: "Write Part 1.4 PhysicsClient analysis"
Task: "Write Part 1.5 MCP analysis"
Task: "Write Part 1.6 Scripting analysis"
Task: "Write Part 1.7 Infrastructure analysis"
Task: "Write Part 1.8 Tests & Scripting analysis"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (report skeleton)
2. Complete Phase 2: Foundational (analyse all projects)
3. Complete Phase 3: User Story 1 (per-project ratings + issues)
4. **STOP and VALIDATE**: Report has complete per-project analysis with ratings
5. Deliverable: Developer can review codebase health

### Incremental Delivery

1. Complete Setup + Foundational → Raw analysis ready
2. Add User Story 1 → Per-project quality analysis → MVP deliverable
3. Add User Story 2 → Alternative comparisons → Decision-ready
4. Add User Story 3 → Prioritized roadmap → Planning-ready
5. Polish → Final validation → Complete report

---

## Notes

- [P] tasks = different files or report sections, no dependencies
- [Story] label maps task to specific user story for traceability
- The report is a single file (reports/refactorEvaluation.md) — parallel tasks write to different sections
- Validation tasks (T023-T026, T032-T033, T036-T037) verify spec requirements are met
- All analysis is read-only — no source code modifications in this feature
