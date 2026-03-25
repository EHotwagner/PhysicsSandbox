# Tasks: MCP Tool Schema Fix & Aspire MCP Configuration

**Input**: Design documents from `/specs/004-mcp-fix-aspire-config/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — FR-008 explicitly requires automated regression test in the integration test suite.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Validate the technical approach before applying it across all 17 tools.

- [x] T001 Validate `Nullable<T>` approach by converting one simple tool (`start_stress_test`) in `src/PhysicsSandbox.Mcp/StressTestTools.fs` — change `?max_bodies: int` and `?duration_seconds: int` to `Nullable<int>`, update method body to use `.HasValue`/`.Value`, update `src/PhysicsSandbox.Mcp/StressTestTools.fsi`
- [x] T002 Build solution (`dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and verify the Nullable approach compiles, then start the stack and test `start_stress_test` with the Python test runner to confirm the framework now treats those params as optional

**Checkpoint**: Nullable<T> approach validated. If it fails, fall back to wrapper DTOs (see research.md R2).

**TDD Evidence (Constitution VI)**: The existing Python test runner (`reports/mcp_test_runner.py`) demonstrates 17/59 failures on pre-fix code, satisfying the "tests fail before the fix" requirement. The new C# regression test (T032–T034) provides the "tests pass after implementation" evidence and continuous enforcement.

---

## Phase 2: User Story 1 — AI Assistant Calls Any MCP Tool Successfully (Priority: P1) MVP

**Goal**: Fix all 17 failing MCP tools so 59/59 accept requests with only relevant parameters. Improve tool descriptions. Add regression test.

**Independent Test**: Run Python test runner (`python3 reports/mcp_test_runner.py`) — all 59 tools should return OK (was 42/59).

### Fix Tool Signatures (FR-001, FR-002, FR-003)

- [x] T003 [US1] Fix SimulationTools `add_body` (38 params, 35 optional) — convert all `?param: float`/`?param: int`/`?param: bool` to `Nullable<T>`, convert `?param: string` to nullable string, update method body defaults in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T004 [US1] Fix SimulationTools `register_shape` (13 params, 10 optional) — convert optional params to `Nullable<T>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T005 [US1] Fix SimulationTools `add_constraint` (25 params, 22 optional) — convert optional params to `Nullable<T>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T006 [US1] Fix SimulationTools `set_body_pose` (6 params, 3 optional vx/vy/vz) — convert to `Nullable<float>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T007 [US1] Fix SimulationTools `raycast` (9 params, 3 optional) — convert max_distance, collision_mask, all_hits to `Nullable<T>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T008 [US1] Fix SimulationTools `sweep_cast` (14 params, 6 optional shape-specific) — convert to `Nullable<T>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T009 [US1] Fix SimulationTools `overlap` (11 params, 5 optional shape-specific) — convert to `Nullable<T>` in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T010 [US1] Fix SimulationTools `set_collision_filter` — verify all params are truly required; if test failure was due to missing body state, no signature change needed; ensure test creates body first in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T011 [US1] Update `src/PhysicsSandbox.Mcp/SimulationTools.fsi` to match all signature changes from T003–T010
- [x] T012 [US1] Fix GeneratorTools `generate_random_bodies` — make `seed: int` → `seed: Nullable<int>` (default 0) in `src/PhysicsSandbox.Mcp/GeneratorTools.fs`
- [x] T013 [US1] Fix GeneratorTools `generate_row` — make `spacing: float` → `spacing: Nullable<float>` (default 0.5) in `src/PhysicsSandbox.Mcp/GeneratorTools.fs`
- [x] T014 [US1] Update `src/PhysicsSandbox.Mcp/GeneratorTools.fsi` to match T012–T013 changes
- [x] T015 [US1] Fix RecordingTools `start_recording` — convert `?label: string`, `?time_limit_minutes: int`, `?size_limit_mb: int` to Nullable/nullable in `src/PhysicsSandbox.Mcp/RecordingTools.fs`
- [x] T016 [US1] Update `src/PhysicsSandbox.Mcp/RecordingTools.fsi` to match T015 changes
- [x] T017 [US1] Fix RecordingQueryTools `query_snapshots` — make `start_time`, `end_time`, `page_size`, `cursor` optional with `Nullable<T>` and defaults in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs`
- [x] T018 [US1] Fix RecordingQueryTools `query_events` — make `start_time`, `end_time`, `event_type`, `page_size`, `cursor` optional in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs`
- [x] T019 [US1] Fix RecordingQueryTools `query_body_trajectory` — make `start_time`, `end_time`, `page_size`, `cursor` optional in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs`
- [x] T020 [US1] Update `src/PhysicsSandbox.Mcp/RecordingQueryTools.fsi` to match T017–T019 changes
- [x] T021 [US1] Fix MeshFetchQueryTools `query_mesh_fetches` — make `minutes_ago`, `mesh_id`, `page_size`, `cursor` optional in `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fs`
- [x] T022 [US1] Update `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fsi` to match T021 changes
- [x] T023 [US1] Build solution and run existing unit tests (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) to verify no regressions (FR-007)

### Improve Tool Descriptions (FR-009)

- [x] T024 [US1] Improve `[<Description>]` attributes on SimulationTools — add shape-specific applicability ("Required when shape='sphere'"), defaults, parameter grouping to tool-level and param-level descriptions in `src/PhysicsSandbox.Mcp/SimulationTools.fs`
- [x] T025 [P] [US1] Improve `[<Description>]` attributes on GeneratorTools — add defaults and constraints in `src/PhysicsSandbox.Mcp/GeneratorTools.fs`
- [x] T026 [P] [US1] Improve `[<Description>]` attributes on RecordingTools — add defaults and optional behavior in `src/PhysicsSandbox.Mcp/RecordingTools.fs`
- [x] T027 [P] [US1] Improve `[<Description>]` attributes on RecordingQueryTools — add pagination defaults, time range defaults in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs`
- [x] T028 [P] [US1] Improve `[<Description>]` attributes on MeshFetchQueryTools — add filter and pagination defaults in `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fs`
- [x] T029 [P] [US1] Improve `[<Description>]` attributes on StressTestTools — add scenario-specific param descriptions in `src/PhysicsSandbox.Mcp/StressTestTools.fs`
- [x] T030 [P] [US1] Improve `[<Description>]` attributes on remaining passing tool modules (PresetTools, SteeringTools, ViewCommandTools, MetricsTools, BatchTools) — add clarity on defaults and parameter usage in their respective `.fs` files under `src/PhysicsSandbox.Mcp/`

### Update Surface Area Baselines

- [x] T031 [US1] Update surface-area baseline tests in `tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs` to reflect new Nullable parameter types across all modified tool modules

### Automated Regression Test (FR-008)

- [x] T032 [US1] Create MCP HTTP/SSE client helper in `tests/PhysicsSandbox.Integration.Tests/McpTestClient.cs` — implement JSON-RPC 2.0 client over HTTP/SSE (initialize handshake, tool call method, response parsing) based on pattern from `reports/mcp_test_runner.py`
- [x] T033 [US1] Create `tests/PhysicsSandbox.Integration.Tests/McpToolRegressionTests.cs` — Aspire integration test class using `DistributedApplicationTestingBuilder`, start AppHost, wait for MCP resource, connect via McpTestClient. Three test categories: (1) call all 59 tools with minimal relevant params, assert no RPC_ERROR (TOOL_ERROR acceptable for stateful tools); (2) call `add_body` without required `shape` param, assert clear error message not deserialization crash [FR-004]; (3) call `add_body` with sphere params both as omitted and as explicit null, assert identical success responses [edge case: null vs omit]
- [x] T034 [US1] Run full integration test suite (`dotnet test tests/PhysicsSandbox.Integration.Tests -p:StrideCompilerSkipBuild=true`) and verify all 59 MCP tools pass the regression test

**Checkpoint**: User Story 1 complete — 59/59 tools accept minimal params, descriptions improved, regression test passing.

---

## Phase 3: User Story 2 — Developer Uses Aspire Dashboard Tools in AI Workflow (Priority: P2)

**Goal**: Configure Aspire Dashboard MCP in `.mcp.json` so Claude Code gets resource monitoring, logs, diagnostics, and docs search.

**Independent Test**: Start Aspire stack, launch Claude Code, verify Aspire Dashboard tools appear in tool list.

### Implementation

- [x] T035 [US2] Add `aspire-dashboard` stdio transport entry to `.mcp.json` — add `{"type": "stdio", "command": "aspire", "args": ["agent", "mcp", "--nologo", "--non-interactive"]}` alongside existing `physics-sandbox` SSE entry

**Checkpoint**: User Story 2 complete — Aspire Dashboard tools available in Claude Code when stack is running.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation, and cleanup.

- [x] T036 Update `reports/NetworkProblems.md` — mark MCP tool deserialization entry as resolved with fix details, mark Aspire Dashboard 403 entry as resolved with stdio config
- [x] T037 Run quickstart.md validation — follow `specs/004-mcp-fix-aspire-config/quickstart.md` end-to-end: build, test, start stack, run Python test runner (59/59 OK), verify Aspire tools in Claude Code
- [x] T038 Update `CLAUDE.md` — add MCP tool Nullable pattern to Known Issues & Gotchas (document that MCP tool parameters changed from F# `Option<T>` to `Nullable<T>` for framework compatibility; any code directly calling tool methods must update from `defaultArg`/Option patterns to `Nullable.HasValue`/`.Value` patterns), update MCP tool count if changed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — validates technical approach
- **US1 (Phase 2)**: Depends on Setup — proof-of-concept must pass
- **US2 (Phase 3)**: No dependency on US1 — can run in parallel
- **Polish (Phase 4)**: Depends on US1 and US2 completion

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 1 (Nullable validation). Self-contained — all 6 tool modules + tests.
- **User Story 2 (P2)**: Independent — only touches `.mcp.json`. Can start immediately after Phase 1.

### Within User Story 1

- Signature fixes (T003–T022) are sequential within each module but modules can overlap
- Build verification (T023) must follow all signature fixes
- Description improvements (T024–T030) can run in parallel after signatures are fixed
- Surface area baselines (T031) must follow all signature + description changes
- Regression test (T032–T034) should follow all fixes to validate the complete set

### Parallel Opportunities

- T025, T026, T027, T028, T029, T030 — description improvements across different modules
- US1 and US2 can proceed in parallel after Phase 1 validation
- T032 (MCP test client) can be written in parallel with description improvements

---

## Parallel Example: User Story 1 Description Improvements

```bash
# Launch all description improvement tasks together (different files):
Task T025: "Improve descriptions in GeneratorTools.fs"
Task T026: "Improve descriptions in RecordingTools.fs"
Task T027: "Improve descriptions in RecordingQueryTools.fs"
Task T028: "Improve descriptions in MeshFetchQueryTools.fs"
Task T029: "Improve descriptions in StressTestTools.fs"
Task T030: "Improve descriptions in remaining passing tool modules"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Validate Nullable approach (T001–T002)
2. Complete Phase 2: Fix all 17 tools + descriptions + regression test (T003–T034)
3. **STOP and VALIDATE**: Run Python test runner — expect 59/59 OK
4. Run integration tests — expect all MCP regression tests pass

### Incremental Delivery

1. Phase 1 → Nullable proof-of-concept validated
2. US1 signature fixes → Build passes, existing tests green
3. US1 description improvements → Better AI assistant experience
4. US1 regression test → Continuous enforcement of SC-001/SC-004
5. US2 Aspire config → Developer productivity boost
6. Polish → Documentation and verification complete

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- The proof-of-concept (T001–T002) is critical — if Nullable<T> doesn't work with the MCP framework, the entire approach needs revision per research.md R2 fallback
- set_collision_filter (T010) may not need a signature change — investigate whether the test failure is due to missing body state rather than schema issues
- Commit after each module is complete (signatures + .fsi) to maintain buildable state
