# Feature Specification: Backlog Fixes and Test Progress Reporting

**Feature Branch**: `004-backlog-fix-test-progress`
**Created**: 2026-03-23
**Status**: Complete
**Input**: User description: "search and collect backlog items and fix them. also implement a way to see the progress and time estimates when running a test suite."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Test Suite Progress and Time Estimates (Priority: P1)

A developer runs the full test suite (362+ tests across 7 projects) and wants to see real-time progress — how many tests have passed, how many remain, and an estimated time to completion — instead of watching a silent terminal until it finishes.

**Why this priority**: Running the test suite is the most frequent developer workflow. Blind waiting with no feedback degrades the experience, especially when the suite takes minutes. This delivers immediate, daily-use value.

**Independent Test**: Can be fully tested by running the test suite and observing that progress bars, test counts, and time estimates appear in the terminal output. Delivers value as a standalone improvement to the developer workflow.

**Acceptance Scenarios**:

1. **Given** a developer runs the test suite, **When** a test project completes, **Then** a progress indicator shows the number of projects completed out of the total (e.g., "3/7 projects"), updated after each project finishes.
2. **Given** a developer runs the test suite, **When** at least one test project has completed, **Then** an estimated time to completion is displayed based on elapsed time and remaining projects.
3. **Given** a developer runs the test suite, **When** a test project completes with failures, **Then** the failure summary is immediately surfaced in the progress output without waiting for the full suite to complete.
4. **Given** a developer runs the test suite with headless build flags, **When** tests execute, **Then** progress reporting works identically to a standard run.

---

### User Story 2 - Fix Silent TryAdd/TryRemove Failures (Priority: P2)

A developer using the PhysicsClient API calls methods that internally use `TryAdd` or `TryRemove` on registries. When these operations silently fail (e.g., duplicate key, missing entry), the developer receives no feedback and encounters subtle bugs later.

**Why this priority**: Silent failures are a correctness issue. Developers waste time debugging downstream effects instead of catching the problem at the source. This is a targeted fix with high reliability impact.

**Independent Test**: Can be tested by invoking PhysicsClient operations that trigger duplicate add or missing remove scenarios and verifying that appropriate error information is returned or logged.

**Acceptance Scenarios**:

1. **Given** a developer adds a body with an ID that already exists in the registry, **When** the add operation fails internally, **Then** the failure is returned as a `Result.Error` with a descriptive message.
2. **Given** a developer removes a body that does not exist in the registry, **When** the remove operation fails, **Then** the failure is returned as a `Result.Error` rather than silently ignored.
3. **Given** 10 instances of silent `TryAdd`/`TryRemove` exist across PhysicsClient (7 body registry operations in SimulationCommands, 2 cache operations in MeshResolver, 1 cache operation in Session), **When** all are addressed, **Then** 6 single-body registry operations return a `Result.Error` with a meaningful diagnostic message consistent with the existing `Result<'a, string>` API pattern, the `clearAll` bulk registry cleanup emits `Trace.TraceWarning` for missing entries (benign cache inconsistency), and cache operations emit structured warnings (cache duplicates are expected/benign and do not warrant error-level reporting).

---

### User Story 3 - Add Pending Query Expiration (Priority: P3)

The PhysicsServer MessageRouter holds pending query entries in an unbounded dictionary. If a query response never arrives (e.g., simulation crash, timeout), entries accumulate indefinitely, risking memory exhaustion over long-running sessions.

**Why this priority**: While not immediately user-facing, this is a correctness and stability issue. Long-running sessions (common in physics sandboxes) could degrade over time. The fix is small and prevents an insidious failure mode.

**Independent Test**: Can be tested by submitting queries without providing responses and verifying that stale entries are cleaned up after a reasonable period.

**Acceptance Scenarios**:

1. **Given** a query is submitted to the MessageRouter, **When** no response arrives within a defined timeout, **Then** the pending query entry is removed and the caller receives a timeout indication.
2. **Given** the pending query dictionary contains expired entries, **When** the cleanup mechanism runs, **Then** all entries older than the timeout threshold are removed.
3. **Given** a normal query-response cycle completes within the timeout, **When** the response arrives, **Then** the query is resolved normally and no expiration occurs.

---

### User Story 4 - Complete Missing Constraint Builder Coverage (Priority: P4)

The Scripting library currently provides convenience builders for only 4 of 10 constraint types (BallSocket, Hinge, Weld, DistanceLimit). Developers wanting to script the remaining 6 types must manually construct proto messages, which is error-prone and inconsistent with the library's purpose.

**Why this priority**: This is a completeness gap in a public-facing convenience API. While the workaround exists (manual proto construction), it undermines the library's value proposition. Moderate effort, clear scope.

**Independent Test**: Can be tested by writing scripts that create all 10 constraint types using only Scripting library APIs and verifying they work correctly.

**Acceptance Scenarios**:

1. **Given** the Scripting library, **When** a developer wants to create any of the 10 supported constraint types, **Then** a typed builder function is available for each type.
2. **Given** a developer uses a new constraint builder, **When** the constraint is created, **Then** it behaves identically to the equivalent manually constructed proto message.

---

### User Story 5 - Extract Shared Test Helpers (Priority: P5)

Test utility functions (`getPublicMembers`, `StartAppAndConnect`) are copy-pasted across multiple test projects. When the signature changes, each copy must be updated independently, creating maintenance risk.

**Why this priority**: Low immediate user impact but improves maintainability. Small scope, clear benefit.

**Independent Test**: Can be tested by verifying all test projects compile and pass after shared helpers are consolidated, and that no duplicated helper code remains.

**Acceptance Scenarios**:

1. **Given** test utility functions are duplicated across 4+ test files, **When** they are consolidated into a shared location, **Then** all test projects reference the shared version.
2. **Given** the shared helpers are modified, **When** tests are rebuilt, **Then** all consuming test projects pick up the change without individual updates.

---

### Edge Cases

- What happens when the test suite contains zero tests (empty project)? Progress reporting should handle this gracefully without division-by-zero errors.
- What happens when a test project fails to build? Progress reporting should indicate the build failure without crashing the progress display.
- What happens when pending queries are submitted faster than the expiration cleanup runs? The cleanup mechanism should handle high volumes without blocking query submission.
- What happens when a constraint builder receives invalid parameters (e.g., negative distance limit)? It should return an error consistent with existing builder error handling patterns.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a test runner wrapper or configuration that displays per-project progress (projects completed / total) during test suite execution, updated as each project finishes.
- **FR-002**: System MUST display an estimated time to completion once at least one test project has completed, based on elapsed time and remaining projects.
- **FR-003**: System MUST surface test project failure summaries immediately in the progress output as each project completes.
- **FR-004a**: System MUST return `Result.Error` with a descriptive message when PhysicsClient single-body registry operations (`TryAdd`/`TryRemove` in SimulationCommands) fail, covering the 6 single-body operations (addSphere, addBox, addCapsule, addCylinder, addPlane, removeBody), consistent with the existing `Result<'a, string>` API pattern. The `clearAll` bulk operation MUST emit a structured warning (`Trace.TraceWarning`) when individual registry cleanup entries fail, but still return `Ok` with the removal count, since the server-side removals succeeded and registry cleanup failures are benign cache inconsistencies.
- **FR-004b**: System MUST emit structured warnings (via `Trace.TraceWarning` or equivalent always-on diagnostic) when PhysicsClient cache operations (`TryAdd`/`TryRemove` in MeshResolver and Session) encounter duplicates or missing entries, covering all 3 identified instances. Cache duplicates are expected and do not warrant error-level reporting.
- **FR-005**: System MUST expire pending queries in the MessageRouter after a configurable timeout period.
- **FR-006**: System MUST provide Scripting library builder functions for all 10 constraint types supported by BepuFSharp.
- **FR-007**: System MUST consolidate duplicated test helper functions into a shared location referenced by all test projects.
- **FR-008**: System MUST preserve existing test pass/fail behavior — no test should change result due to these changes.
- **FR-009**: The test progress display MUST work with both standard and headless build configurations.

### Key Entities

- **Test Run**: A single execution of the test suite, with per-project breakdown of test counts, pass/fail status, and timing.
- **Pending Query**: A correlation entry in the MessageRouter tracking an outstanding request-response pair, with a creation timestamp and timeout threshold.
- **Constraint Builder**: A typed function in the Scripting library that constructs a proto-based constraint message from domain parameters.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can see test progress (projects completed/total and percentage) within 2 seconds of the first test project completing.
- **SC-002**: Estimated time to completion is displayed and accurate within 20% of actual remaining time after 25% of tests have completed.
- **SC-003**: All 10 silent `TryAdd`/`TryRemove` failures in PhysicsClient are replaced with explicit reporting — 6 single-body registry operations return `Result.Error`, 1 bulk registry cleanup (`clearAll`) emits `Trace.TraceWarning`, 3 cache operations emit structured warnings.
- **SC-004**: Pending queries in MessageRouter are cleaned up within a defined timeout period, preventing unbounded memory growth.
- **SC-005**: All 10 constraint types have Scripting library builders, up from 4.
- **SC-006**: Duplicated test helpers exist in exactly one shared location, with zero copy-pasted instances remaining.
- **SC-007**: All existing tests continue to pass after all changes are applied.

## Clarifications

### Session 2026-03-23

- Q: Should silent TryAdd/TryRemove failures be surfaced as Result errors, logged warnings, or both? → A: Return `Result.Error` through the existing Result type, consistent with PhysicsClient's `Result<'a, string>` API pattern.
- Q: Should test progress track at per-test or per-project granularity? → A: Per-project granularity — show progress as each test project completes (7 update points).

## Assumptions

- The test suite is run via `dotnet test` and the solution uses xUnit as the test framework. Progress reporting will leverage the test platform's extensibility (custom loggers or output parsing).
- The pending query timeout value will default to 30 seconds, which is generous for local physics simulation queries. This can be made configurable without requiring user input.
- The 6 missing constraint types are to be confirmed from BepuFSharp's supported types during planning.
- Shared test helpers will be placed in an existing shared test infrastructure project or a new minimal shared test utilities project if none exists.
- The test progress feature will be implemented as a shell script wrapper or test logger, not requiring changes to the test framework or individual test code.
