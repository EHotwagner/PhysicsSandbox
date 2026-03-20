# Tasks: Add MCP Server to Aspire AppHost Orchestration

**Input**: Design documents from `/specs/006-mcp-aspire-orchestration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: Integration tests are included per Constitution Principle VI (test evidence required for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new projects needed. Verify the existing MCP project builds and is referenceable from AppHost.

- [x] T001 Verify PhysicsSandbox.Mcp builds cleanly with `dotnet build src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`

**Checkpoint**: MCP project compiles without errors

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Register the MCP server in the AppHost orchestration graph. This MUST complete before user story work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Add MCP server as project resource in `src/PhysicsSandbox.AppHost/AppHost.cs` — add `builder.AddProject<Projects.PhysicsSandboxMcp>("mcp").WithReference(server).WaitFor(server)` following the existing pattern for simulation/viewer/client

**Checkpoint**: Foundation ready — AppHost references MCP project, solution builds

---

## Phase 3: User Story 1 + 2 — MCP Server Starts with Aspire & Connects via Service Discovery (Priority: P1) 🎯 MVP

**Goal**: MCP server starts/stops with Aspire and resolves PhysicsServer address via environment variables instead of hardcoded default.

**Independent Test**: Start AppHost, verify MCP resource appears in dashboard with "Running" state and connects to PhysicsServer.

> Note: US1 and US2 are combined into a single phase because they are tightly coupled — registering the MCP server in Aspire (US1) is meaningless without service discovery (US2), and the code changes overlap in Program.fs.

### Implementation for User Story 1 + 2

- [x] T003 [P] [US1] [US2] Update server address resolution in `src/PhysicsSandbox.Mcp/Program.fs` — replace hardcoded `"https://localhost:7180"` fallback with environment variable lookup: check CLI args first, then `services__server__https__0`, then `services__server__http__0`, then fall back to `"https://localhost:7180"` for standalone use
- [x] T004 [P] [US1] [US2] Write integration test in `tests/PhysicsSandbox.Integration.Tests/McpOrchestrationTests.cs` — verify MCP resource starts in Aspire using `DistributedApplicationTestingBuilder`, assert resource reaches "Running" state, assert graceful shutdown on disposal
- [x] T005 [US1] [US2] Run full test suite with `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — verify new test passes and all existing tests remain green (SC-003)

**Checkpoint**: MCP server starts with Aspire, discovers PhysicsServer via env vars, appears in dashboard, shuts down gracefully. US1 + US2 acceptance scenarios satisfied.

---

## Phase 4: User Story 3 — MCP Server Logs Visible in Aspire Dashboard (Priority: P2)

**Goal**: MCP server logs appear in Aspire dashboard's structured logging view for observability.

**Independent Test**: Invoke an MCP tool and verify log entries appear in the Aspire dashboard under the MCP resource.

### Implementation for User Story 3

- [x] T006 [US3] Verify MCP server stdout/stderr logs are captured by Aspire dashboard — Aspire automatically captures child process output for project resources, so this should work without code changes. If logs are not appearing, investigate whether `LogToStandardErrorThreshold` in `src/PhysicsSandbox.Mcp/Program.fs` needs adjustment for Aspire log capture.
- [x] T007 [US3] Add integration test assertion in `tests/PhysicsSandbox.Integration.Tests/McpOrchestrationTests.cs` (depends on T004) — verified: Aspire automatically captures stdout/stderr for AddProject resources; no programmatic log API available in Aspire.Hosting.Testing 13.x, verified by dashboard observation

**Checkpoint**: MCP server logs visible in Aspire dashboard. US3 acceptance scenario satisfied.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and cleanup

- [x] T008 Update `CLAUDE.md` Recent Changes section with 006 feature summary
- [x] T009 Verify quickstart.md scenarios work end-to-end (start AppHost, confirm 5 resources in dashboard, confirm standalone mode still works)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Story 1+2 (Phase 3)**: Depends on Foundational phase completion
- **User Story 3 (Phase 4)**: Can start after Phase 3 (depends on MCP being orchestrated)
- **Polish (Phase 5)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1+2 (P1)**: Combined — starts after Foundational (Phase 2)
- **User Story 3 (P2)**: Depends on US1+2 (MCP must be orchestrated before verifying log capture)

### Within Each Phase

- T003 (Program.fs change) and T004 (test file) can run in parallel since they modify different files
- T005 (test run) depends on T002, T003, and T004

### Parallel Opportunities

- T003 and T004 can be written in parallel (different files)
- T006 and T007 can be investigated/written in parallel

---

## Parallel Example: Phase 3

```bash
# These can run in parallel (different files):
Task T003: "Update server address resolution in src/PhysicsSandbox.Mcp/Program.fs"
Task T004: "Write integration test in tests/PhysicsSandbox.Integration.Tests/McpOrchestrationTests.cs"

# Then run sequentially:
Task T005: "Run full test suite" (depends on T003, T004)
```

---

## Implementation Strategy

### MVP First (User Story 1+2)

1. Complete Phase 1: Verify MCP builds
2. Complete Phase 2: Register MCP in AppHost
3. Complete Phase 3: Update Program.fs + integration test
4. **STOP and VALIDATE**: Start AppHost, confirm 5 resources in dashboard
5. Ready for use

### Incremental Delivery

1. Phase 1+2 → Foundation ready
2. Phase 3 (US1+2) → MCP orchestrated + service discovery → MVP!
3. Phase 4 (US3) → Log observability confirmed
4. Phase 5 → Documentation updated

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US2 are combined because they share code changes and are not independently useful
- This is a very small feature — 9 tasks total, ~3 files modified/created
- Commit after each phase completion
- The MCP server's stdio transport does not conflict with Aspire orchestration (validated in research.md)
