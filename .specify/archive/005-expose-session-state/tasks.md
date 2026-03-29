# Tasks: Expose Session State

**Input**: Design documents from `/specs/005-expose-session-state/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Surface area baseline test update is an implementation task (not TDD).

**Organization**: Tasks grouped by user story. US1 and US2 share a single implementation change (same file, same edit). US3 is the same. All three are delivered atomically.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup needed — existing project structure, no new files or dependencies.

*Phase skipped — nothing to do.*

---

## Phase 2: Foundational

**Purpose**: No foundational work needed — the accessor functions and their types already exist internally.

*Phase skipped — nothing to do.*

---

## Phase 3: User Stories 1, 2, 3 — Expose Session Accessors (Priority: P1/P2) 🎯 MVP

**Goal**: Make `latestState`, `bodyRegistry`, and `lastStateUpdate` publicly accessible from outside the PhysicsClient assembly.

**Independent Test**: Build the solution, then verify the surface area test passes and the three functions appear in the PhysicsClient.Session module's public API via reflection.

> **Note**: All three user stories share the same two files. They are implemented atomically in a single phase rather than split across phases, since splitting would require partial edits to the same lines.

### Implementation

- [x] T001 [US1] [US2] [US3] Remove `internal` keyword from `latestState`, `bodyRegistry`, and `lastStateUpdate` signatures in `src/PhysicsClient/Connection/Session.fsi`
- [x] T002 [US1] [US2] [US3] Update Session surface area baseline to include `bodyRegistry`, `latestState`, `lastStateUpdate` in `tests/PhysicsClient.Tests/SurfaceAreaTests.fs`
- [x] T003 Verify all existing tests pass by running `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: All three accessors are publicly visible. Surface area test validates the new API. Existing tests remain green.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validate end-to-end and ensure downstream consumers can access the new API.

- [x] T004 Run quickstart.md validation — confirm the code example compiles against the updated PhysicsClient API

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 3** (User Stories): No prerequisites — can start immediately
- **Phase 4** (Polish): Depends on Phase 3 completion

### User Story Dependencies

- **User Story 1 (P1)** — Read Current Simulation State: Implemented by T001
- **User Story 2 (P1)** — Look Up Body Names: Implemented by T001
- **User Story 3 (P2)** — Check State Freshness: Implemented by T001
- All three stories share the same code change and are delivered atomically

### Within Phase 3

- T001 (signature change) → T002 (baseline update) → T003 (test verification)
- T001 and T002 edit different files and could technically run in parallel, but T002 only validates correctly after T001

---

## Parallel Example: Phase 3

```text
# T001 and T002 edit different files — can run in parallel:
Task: "Remove internal from 3 signatures in src/PhysicsClient/Connection/Session.fsi"
Task: "Update baseline in tests/PhysicsClient.Tests/SurfaceAreaTests.fs"

# T003 must run after both:
Task: "Run dotnet test to verify all tests pass"
```

---

## Implementation Strategy

### MVP First (All Stories — Single Atomic Change)

1. Complete T001: Edit Session.fsi (the actual feature)
2. Complete T002: Update surface area baseline (constitution compliance)
3. Complete T003: Run tests (verification)
4. **STOP and VALIDATE**: All acceptance scenarios from spec.md are satisfied
5. Complete T004: Quickstart validation (optional polish)

### Summary

This is a 4-task feature with 2 file edits. The entire feature is a single atomic change — there is no meaningful MVP subset smaller than the whole thing.

---

## Notes

- No new files created
- No behavior changes — read-only visibility promotion
- Thread safety already established (research.md)
- Constitution Principle V compliance via surface area baseline update
