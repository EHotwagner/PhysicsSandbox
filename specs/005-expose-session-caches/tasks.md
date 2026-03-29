# Tasks: Expose Session Caches

**Input**: Design documents from `/specs/005-expose-session-caches/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Surface area baseline test update included (required by constitution Principle V).

**Organization**: Tasks are grouped by user story. All four stories modify the same two source files (Session.fsi, Session.fs), so they are sequential within those files but logically grouped by story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup needed — all target files already exist. This feature modifies existing files only.

*No tasks in this phase.*

---

## Phase 2: Foundational

**Purpose**: No foundational/blocking prerequisites — all infrastructure is in place from the existing PhysicsClient project.

*No tasks in this phase.*

---

## Phase 3: User Story 1 — Inspect Body Properties (Priority: P1) 🎯 MVP

**Goal**: Expose `bodyPropertiesCache` as a public accessor so consumers outside PhysicsClient can read body property metadata.

**Independent Test**: Call `Session.bodyPropertiesCache` from outside the PhysicsClient assembly (surface area test confirms public visibility).

### Implementation for User Story 1

- [x] T001 [US1] Add `bodyPropertiesCache` accessor function in `src/PhysicsClient/Connection/Session.fs` — add `let bodyPropertiesCache (session: Session) = session.BodyPropertiesCache` after the existing `lastStateUpdate` accessor (line ~266)
- [x] T002 [US1] Add `val bodyPropertiesCache` declaration in `src/PhysicsClient/Connection/Session.fsi` — add `val bodyPropertiesCache : session: Session -> System.Collections.Concurrent.ConcurrentDictionary<string, PhysicsSandbox.Shared.Contracts.BodyProperties>` after the `lastStateUpdate` declaration (line ~28)

**Checkpoint**: `bodyPropertiesCache` is now publicly accessible. Build should succeed.

---

## Phase 4: User Story 2 — View Active Constraints (Priority: P1)

**Goal**: Expose `cachedConstraints` as a public accessor so consumers can inspect active constraint relationships.

**Independent Test**: Call `Session.cachedConstraints` from outside the PhysicsClient assembly.

### Implementation for User Story 2

- [x] T003 [US2] Add `cachedConstraints` accessor function in `src/PhysicsClient/Connection/Session.fs` — add `let cachedConstraints (session: Session) = session.CachedConstraints` after the `bodyPropertiesCache` accessor
- [x] T004 [US2] Add `val cachedConstraints` declaration in `src/PhysicsClient/Connection/Session.fsi` — add `val cachedConstraints : session: Session -> PhysicsSandbox.Shared.Contracts.ConstraintState list` after the `bodyPropertiesCache` declaration

**Checkpoint**: `cachedConstraints` is now publicly accessible. Build should succeed.

---

## Phase 5: User Story 3 — View Registered Shapes (Priority: P1)

**Goal**: Expose `cachedRegisteredShapes` as a public accessor so consumers can see which custom shapes are registered.

**Independent Test**: Call `Session.cachedRegisteredShapes` from outside the PhysicsClient assembly.

### Implementation for User Story 3

- [x] T005 [US3] Add `cachedRegisteredShapes` accessor function in `src/PhysicsClient/Connection/Session.fs` — add `let cachedRegisteredShapes (session: Session) = session.CachedRegisteredShapes` after the `cachedConstraints` accessor
- [x] T006 [US3] Add `val cachedRegisteredShapes` declaration in `src/PhysicsClient/Connection/Session.fsi` — add `val cachedRegisteredShapes : session: Session -> PhysicsSandbox.Shared.Contracts.RegisteredShapeState list` after the `cachedConstraints` declaration

**Checkpoint**: `cachedRegisteredShapes` is now publicly accessible. Build should succeed.

---

## Phase 6: User Story 4 — Check Server Address (Priority: P2)

**Goal**: Expose `serverAddress` as a public accessor for diagnostics and multi-server tooling.

**Independent Test**: Call `Session.serverAddress` from outside the PhysicsClient assembly.

### Implementation for User Story 4

- [x] T007 [US4] Add `serverAddress` accessor function in `src/PhysicsClient/Connection/Session.fs` — add `let serverAddress (session: Session) = session.ServerAddress` after the `cachedRegisteredShapes` accessor
- [x] T008 [US4] Add `val serverAddress` declaration in `src/PhysicsClient/Connection/Session.fsi` — add `val serverAddress : session: Session -> string` after the `cachedRegisteredShapes` declaration

**Checkpoint**: `serverAddress` is now publicly accessible. Build should succeed.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Surface area baseline validation and build verification.

- [x] T009 Update Session surface area baseline test in `tests/PhysicsClient.Tests/SurfaceAreaTests.fs` — add `"bodyPropertiesCache"; "cachedConstraints"; "cachedRegisteredShapes"; "serverAddress"` to the Session baseline list (line ~17)
- [x] T010 Run full build: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — verify no compilation errors
- [x] T011 Run tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — verify all existing tests pass plus updated surface area test

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phases 3–6** (User Stories): No setup or foundational prerequisites — can start immediately
- **Phase 7** (Polish): Depends on all user story phases (T001–T008) being complete
- User stories are sequential because they modify the same two files (Session.fs, Session.fsi)

### User Story Dependencies

- **US1 (T001–T002)**: No dependencies — start immediately
- **US2 (T003–T004)**: Depends on US1 (same files, insertion order matters)
- **US3 (T005–T006)**: Depends on US2 (same files, insertion order matters)
- **US4 (T007–T008)**: Depends on US3 (same files, insertion order matters)

### Within Each User Story

- `.fs` implementation (odd tasks) before `.fsi` declaration (even tasks) — or vice versa; order within a story doesn't matter as long as both are done before build
- In practice, do both together since they're single-line additions

### Parallel Opportunities

- T001+T002 can be done together (same story, different files)
- T003+T004 can be done together
- T005+T006 can be done together
- T007+T008 can be done together
- All four stories could theoretically be batched into a single edit session since the changes are non-conflicting additions

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001–T002: `bodyPropertiesCache` accessor
2. Complete T009–T011: Update baseline test, build, run tests
3. **STOP and VALIDATE**: Verify build + tests pass

### Incremental Delivery

1. Add all 4 accessors (T001–T008) — they're trivial one-liners
2. Update surface area test (T009)
3. Build + test (T010–T011)
4. Done — single commit for the entire feature

### Recommended Approach

Given the trivial nature of these changes (4 one-line accessor functions + 4 one-line .fsi declarations + 1 test update), implement all tasks in a single pass rather than story-by-story. The story decomposition exists for traceability, not because incremental delivery adds value here.

---

## Notes

- Pattern follows 005-expose-session-state exactly (commit 2eb9d05)
- No new files created — all modifications to existing files
- No behavior changes — read-only accessors only
- No downstream (Scripting library) changes required
- PhysicsClient NuGet version will need bumping post-merge (handled by merge-feature skill)
