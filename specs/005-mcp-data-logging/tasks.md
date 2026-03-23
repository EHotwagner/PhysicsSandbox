# Tasks: MCP Data Logging for Analysis

**Input**: Design documents from `/specs/005-mcp-data-logging/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization — create the Recording module structure and wire into the build

- [x] T001 Create Recording/ directory and type definition files: `src/PhysicsSandbox.Mcp/Recording/Types.fsi` and `src/PhysicsSandbox.Mcp/Recording/Types.fs` with LogEntry discriminated union (`StateSnapshot of DateTimeOffset * SimulationState | CommandEvent of DateTimeOffset * CommandEvent`), EntryType byte enum, wire format constants, and PaginationCursor record type per data-model.md
- [x] T002 Add all new `.fs`/`.fsi` files to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` in correct compilation order: Types.fsi/.fs, SessionStore.fsi/.fs, ChunkWriter.fsi/.fs, ChunkReader.fsi/.fs, RecordingEngine.fsi/.fs, RecordingTools.fsi/.fs, RecordingQueryTools.fsi/.fs (before Program.fs, after existing tool files)
- [x] T003 Create test project `tests/PhysicsSandbox.Mcp.Tests/PhysicsSandbox.Mcp.Tests.fsproj` with xUnit 2.x dependencies and project reference to PhysicsSandbox.Mcp, add to `PhysicsSandbox.slnx`
- [x] T004 Verify build passes with empty/stub module files: `dotnet build src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` and `dotnet build tests/PhysicsSandbox.Mcp.Tests/PhysicsSandbox.Mcp.Tests.fsproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T005 Implement `src/PhysicsSandbox.Mcp/Recording/SessionStore.fsi` — public API surface: `createSession`, `loadSession`, `updateSession`, `deleteSession`, `listSessions`, `getActiveSession`, `getRecordingsDir` functions operating on RecordingSession records with JSON persistence at `~/.config/PhysicsSandbox/recordings/`
- [x] T006 Implement `src/PhysicsSandbox.Mcp/Recording/SessionStore.fs` — session.json read/write using System.Text.Json (following ViewerSettings pattern), directory creation/deletion, session enumeration by scanning subdirectories, active session tracking, validation rules from data-model.md (label max 200 chars, time limit 1-1440 min, size limit 1MB-10GB)
- [x] T007 Implement `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fsi` — public API surface: `create` (returns ChunkWriter with Channel and config), `start` (begins background consumer task), `stop` (flushes and closes), `enqueue` (non-blocking write to Channel), `getCurrentChunkInfo` (active chunk metadata)
- [x] T008 Implement `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs` — bounded Channel<LogEntry> consumer that writes length-prefixed protobuf binary to chunk files (`chunk-{timestamp}.bin`), rotates to new chunk file on 1-minute boundaries, tracks cumulative size via `CalculateSize()`, flushes on stop. Wire format per data-model.md: `[uint32 totalSize | int64 timestamp | byte entryType | byte[] payload]`

**Checkpoint**: Foundation ready — SessionStore can create/persist sessions, ChunkWriter can write binary chunk files

---

## Phase 3: User Story 1 — Continuous Physics Data Recording (Priority: P1) — MVP

**Goal**: Auto-start recording on simulation connection, capture all state updates and command events to disk at full fidelity

**Independent Test**: Start MCP server with simulation, verify chunk files appear on disk with valid protobuf entries and timestamps

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T009 [P] [US1] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/ChunkWriterTests.fs` — test binary write format (verify length prefix + timestamp + type byte + payload), chunk rotation on minute boundary, multiple entries per chunk, flush on stop, CalculateSize tracking accuracy
- [x] T010 [P] [US1] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/SessionStoreTests.fs` — test create/load/update/delete session, JSON round-trip fidelity, directory cleanup on delete, active session tracking, validation rejection of invalid limits
- [x] T011 [P] [US1] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/RecordingEngineTests.fs` — test auto-start on first state callback, state and command entries enqueued correctly, stop finalizes session to Completed status, only one active session at a time

### Implementation for User Story 1

- [x] T012 [US1] Implement `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fsi` — public API surface: `create` (with SessionStore + config), `start` (begins recording, creates session, starts ChunkWriter), `stop` (stops ChunkWriter, finalizes session), `onStateReceived` (callback for state stream), `onCommandReceived` (callback for command stream), `isRecording`, `activeSession`
- [x] T013 [US1] Implement `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — orchestrates recording lifecycle: creates session via SessionStore, instantiates ChunkWriter, provides `onStateReceived`/`onCommandReceived` callbacks that wrap proto messages as LogEntry and enqueue to ChunkWriter's Channel. Tracks snapshot/event counts. Handles stop by flushing writer and updating session metadata to Completed
- [x] T014 [US1] Add recording callback hooks to `src/PhysicsSandbox.Mcp/GrpcConnection.fsi` and `src/PhysicsSandbox.Mcp/GrpcConnection.fs` — add mutable `OnStateReceived: (SimulationState -> unit) option` and `OnCommandReceived: (CommandEvent -> unit) option` callback fields. Invoke callbacks in `startStateStream` (after updating latestState) and `startCommandAuditStream` (after adding to commandLog). Callbacks must not block stream processing
- [x] T015 [US1] Wire RecordingEngine into `src/PhysicsSandbox.Mcp/Program.fs` — register RecordingEngine as singleton in DI, after GrpcConnection starts set the OnStateReceived/OnCommandReceived callbacks to RecordingEngine's handlers, call `engine.start()` to auto-start recording on first state message (FR-016)

**Checkpoint**: Recording pipeline works end-to-end. All US1 tests pass. MCP server auto-records to disk. Verify with `ls ~/.config/PhysicsSandbox/recordings/*/chunk-*.bin`

---

## Phase 4: User Story 2 — Dual-Limit Storage Management (Priority: P1)

**Goal**: Automatic pruning enforces both 10-minute time window and 500 MB size cap, whichever is hit first

**Independent Test**: Configure small limits (e.g., 2 minutes, 5 MB), run recording, verify oldest chunks are deleted while newest are preserved and total stays within limits

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T016 [US2] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/ChunkWriterTests.fs` — test time-based pruning (chunks older than limit are deleted), size-based pruning (total stays within cap), whichever-first behavior (time limit reached before size, and vice versa), session metadata updated after pruning, post-prune total size within 5% of configured limit (SC-003)

### Implementation for User Story 2

- [x] T017 [US2] Add pruning logic to `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs` — after each chunk rotation, check: (1) delete chunk files with StartTime older than `now - timeLimitMinutes`, (2) while total size exceeds sizeLimitBytes delete oldest chunk file. Update session metadata (CurrentSizeBytes, ChunkCount) via SessionStore after each prune cycle. Log pruning events with structured diagnostics
- [x] T018 [US2] Add `recording_status` tool to expose storage state — implement in `src/PhysicsSandbox.Mcp/RecordingTools.fsi` and `src/PhysicsSandbox.Mcp/RecordingTools.fs` with `[<McpServerToolType>]` class. Tool returns: session status, current size, size limit, time limit, actual time window covered, chunk count, whether pruning has occurred. Inject RecordingEngine via constructor per existing tool pattern

**Checkpoint**: Pruning verified — all US2 tests pass. Recording stays within configured limits indefinitely. `recording_status` tool reports accurate storage state

---

## Phase 5: User Story 3 — Querying Recorded Data via MCP Tools (Priority: P1)

**Goal**: AI assistants can query recorded data — body trajectories, state snapshots, command events — with paginated results

**Independent Test**: Record a known simulation, query body trajectory and event history, verify results match recorded data

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T019 [P] [US3] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/ChunkReaderTests.fs` — test reading binary format written by ChunkWriter (round-trip), time-range filtering across multiple chunks, pagination cursor encode/decode fidelity, resume from cursor produces correct next page, empty results for out-of-range queries

### Implementation for User Story 3

- [x] T020 [P] [US3] Implement `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fsi` — public API surface: `readEntries` (session dir + time range → seq of LogEntry), `readPage` (session dir + time range + cursor + pageSize → entries + nextCursor option), `encodeCursor`/`decodeCursor` (PaginationCursor ↔ base64 string)
- [x] T021 [US3] Implement `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fs` — open chunk files matching time range (by parsing timestamp from filename), read length-prefixed entries sequentially, deserialize protobuf payloads (SimulationState via `SimulationState.Parser.ParseFrom`, CommandEvent via `CommandEvent.Parser.ParseFrom`), filter by time range, support pagination via chunk filename + byte offset cursor per research.md R6
- [x] T022 [US3] Implement `src/PhysicsSandbox.Mcp/RecordingQueryTools.fsi` — public API for `[<McpServerToolType>]` class with tools: `query_body_trajectory`, `query_snapshots`, `query_events`, `query_summary` per contracts/mcp-tools.md
- [x] T023 [US3] Implement `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs` — `query_body_trajectory`: read state snapshots, extract body by ID, return timestamped position/velocity/rotation series. `query_snapshots`: return state summaries (body count, sim time, tick_ms) in time window. `query_events`: filter CommandEvent entries by type string match and time range. `query_summary`: read session.json metadata without scanning chunk files. All tools support pagination via cursor parameter (FR-015). Indicate pruned data gaps in responses

**Checkpoint**: Full read path working. All US3 tests pass. Query tools return correct, paginated data from recorded sessions

---

## Phase 6: User Story 4 — Session Management (Priority: P2)

**Goal**: Users can start/stop/list/delete recording sessions via MCP tools for organizing experiments

**Independent Test**: Create multiple sessions, list them, delete old ones, verify storage freed

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T024 [US4] Unit tests in `tests/PhysicsSandbox.Mcp.Tests/RecordingToolsTests.fs` — test start new session (auto-stops previous with warning), stop recording, list multiple sessions, delete session frees storage, delete active session rejected, restart recovery marks interrupted sessions as Completed

### Implementation for User Story 4

- [x] T025 [US4] Complete `src/PhysicsSandbox.Mcp/RecordingTools.fsi` and `src/PhysicsSandbox.Mcp/RecordingTools.fs` — add remaining session management tools: `start_recording` (stop current if active + warning, create new session with configurable limits per contracts/mcp-tools.md), `stop_recording` (finalize active session), `list_sessions` (enumerate all sessions with metadata), `delete_session` (remove session directory and storage, reject if active)
- [x] T026 [US4] Handle restart recovery in `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — on startup, scan for sessions with Status=Recording, mark them as Completed (they were interrupted by restart), preserve their data for querying. Log recovery events

**Checkpoint**: Full session lifecycle management working via MCP tools. All US4 tests pass

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, integration testing, performance verification, and build verification

- [x] T027 Add edge case handling across recording modules — disk full detection (catch IOException, set session to Failed, log error) in `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs`; simulation disconnect handling (pause/resume recording on stream reconnect) in `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs`; pruned data gap indicators in query responses in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs`
- [ ] T028 Performance verification in `tests/PhysicsSandbox.Integration.Tests/RecordingIntegrationTests.cs` — measure MCP tool response time baseline (without recording active) and with active recording by calling `get_state` tool 100 times in each condition, assert p95 latency increase is ≤10% (SC-002)
- [ ] T029 Integration test in `tests/PhysicsSandbox.Integration.Tests/RecordingIntegrationTests.cs` — end-to-end test via Aspire testing: start MCP server + simulation, verify recording auto-starts, add bodies and apply forces, query body trajectory and events through MCP tools, verify results match commands sent
- [ ] T030 [P] Surface area baseline tests for new public modules — add baseline assertions for RecordingEngine, ChunkWriter, ChunkReader, SessionStore, RecordingTools, RecordingQueryTools in appropriate test projects per Constitution Principle V
- [x] T031 Build and test verification — run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`, verify all tests pass including new recording tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 Recording (Phase 3)**: Depends on Foundational (Phase 2) — this is the MVP
- **US2 Pruning (Phase 4)**: Depends on US1 (Phase 3) — extends ChunkWriter with pruning
- **US3 Queries (Phase 5)**: Depends on Foundational (Phase 2) — ChunkReader is independent of recording pipeline; can start after Phase 2 if chunk file format is stable
- **US4 Session Mgmt (Phase 6)**: Depends on US1 (Phase 3) — needs RecordingEngine for start/stop
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only — core MVP, no other story dependencies
- **US2 (P1)**: Depends on US1 — extends the recording pipeline with pruning
- **US3 (P1)**: Depends on Phase 2 only — reads chunk files written by US1 but ChunkReader is structurally independent (reads finalized files). Can be developed in parallel with US1/US2 once wire format is locked (T001)
- **US4 (P2)**: Depends on US1 — wraps RecordingEngine lifecycle in MCP tools

### Within Each User Story

- Tests FIRST — write failing tests before implementation (TDD per constitution)
- Signature files (.fsi) before implementation files (.fs)
- Core modules before MCP tool wrappers

### Parallel Opportunities

- T005 + T007 can run in parallel (SessionStore and ChunkWriter signatures are independent files)
- T006 + T008 can run in parallel (SessionStore and ChunkWriter implementations are independent)
- T009 + T010 + T011 can run in parallel (different test files)
- T019 + T020 can run in parallel (ChunkReader tests + .fsi are independent files)
- T020 can start as soon as Phase 2 is complete (ChunkReader is independent of recording pipeline)
- T030 can run in parallel with T029 (different test files)

---

## Parallel Example: User Story 1

```text
# After Phase 2 foundational is complete:

# Write failing tests first (in parallel):
Task T009: "Unit tests for ChunkWriter"
Task T010: "Unit tests for SessionStore"
Task T011: "Unit tests for RecordingEngine"

# Then implement to make tests pass:
Task T012: "Implement RecordingEngine.fsi"
Task T013: "Implement RecordingEngine.fs"  (depends on T012)

# In parallel, prepare GrpcConnection hooks:
Task T014: "Add recording callback hooks to GrpcConnection"

# Then wire together:
Task T015: "Wire RecordingEngine into Program.fs"  (depends on T013, T014)
```

---

## Parallel Example: User Story 3 (can overlap with US1/US2)

```text
# Write failing tests first:
Task T019: "Unit tests for ChunkReader"

# Then implement:
Task T020: "Implement ChunkReader.fsi"
Task T021: "Implement ChunkReader.fs"  (depends on T020)

# In parallel with reader implementation:
Task T022: "Implement RecordingQueryTools.fsi"

# Then wire query tools:
Task T023: "Implement RecordingQueryTools.fs"  (depends on T021, T022)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T008)
3. Complete Phase 3: User Story 1 — Recording (T009-T015)
4. **STOP and VALIDATE**: All US1 tests pass, chunk files on disk with correct binary format, auto-start works, session.json persists
5. Proceed to US2 (pruning) and US3 (queries) — these complete the core value loop

### Incremental Delivery

1. Setup + Foundational → Wire format and session persistence ready
2. US1 Recording → Data flows to disk automatically (MVP!)
3. US2 Pruning → Storage stays bounded (production-safe)
4. US3 Queries → AI assistants can analyze recorded data (full value delivered)
5. US4 Session Management → Multi-experiment workflow support
6. Polish → Edge cases, performance verification, integration tests, surface area baselines

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All new F# modules require `.fsi` signature files (Constitution Principle V)
- TDD: Write failing tests before implementation code (Constitution Workflow §4)
- No new NuGet dependencies — uses existing Google.Protobuf, System.Text.Json, System.Threading.Channels
- Recording directory: `~/.config/PhysicsSandbox/recordings/`
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
