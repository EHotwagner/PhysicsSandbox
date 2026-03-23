# Tasks: MCP Mesh Fetch Logging

**Input**: Design documents from `/specs/004-mcp-mesh-logging/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Proto + Types)

**Purpose**: Define the wire format and data types that all components depend on

- [x] T001 Add `MeshFetchLog` message to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — fields: `repeated string requested_ids = 1`, `int32 hits = 2`, `int32 misses = 3`, `repeated string missed_ids = 4`
- [x] T002 Add `MeshFetchEvent = 3uy` case to `EntryType` in `src/PhysicsSandbox.Mcp/Recording/Types.fs` and `src/PhysicsSandbox.Mcp/Recording/Types.fsi`
- [x] T003 Add `MeshFetchEvent of timestamp: DateTimeOffset * requestedIds: string list * hits: int * misses: int * missedIds: string list` case to `LogEntry` in `src/PhysicsSandbox.Mcp/Recording/Types.fs` and `src/PhysicsSandbox.Mcp/Recording/Types.fsi`
- [x] T004 Build solution to verify proto codegen and type changes compile: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Types compile. MeshFetchLog proto generated. LogEntry has new case.

---

## Phase 2: User Story 1 — Record Mesh Fetch Activity (Priority: P1)

**Goal**: Every FetchMeshes RPC call is recorded to the MCP session with mesh IDs, hits, misses

**Independent Test**: Create mesh bodies, call FetchMeshes, verify recording contains MeshFetchEvent entries with correct hit/miss data

### Implementation for User Story 1

- [x] T005 [US1] Modify `FetchMeshes` in `src/PhysicsServer/Services/PhysicsHubService.fs` — after computing hits/misses, publish a `CommandEvent` to the audit stream via `publishCommandEvent` containing the mesh fetch observation (requested IDs, hits, misses, missed IDs). Use a recognizable wrapper (e.g., serialize a `MeshFetchLog` proto as a marker in the event).
- [x] T006 [P] [US1] Handle `MeshFetchEvent` case in `writeEntry` in `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs` — serialize as `MeshFetchLog` proto, use `EntryType.MeshFetchEvent` byte
- [x] T007 [P] [US1] Handle `MeshFetchEvent` deserialization in `deserializeEntry` in `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fs` — parse `MeshFetchLog.Parser.ParseFrom`, return `LogEntry.MeshFetchEvent`
- [x] T008 [US1] Modify `RecordingEngine.OnCommandReceived` in `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — detect mesh fetch observation events in the command stream and enqueue `LogEntry.MeshFetchEvent` to the current writer (in addition to the normal `LogEntry.CommandEvent`)
- [x] T009 [US1] Add unit test in `tests/PhysicsSandbox.Mcp.Tests/ChunkWriterTests.fs` — write a MeshFetchEvent entry, read it back, verify all fields match
- [x] T010 [US1] Build and run MCP tests: `dotnet test tests/PhysicsSandbox.Mcp.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: FetchMeshes calls are recorded via audit stream. Write/read round-trip verified.

---

## Phase 3: User Story 2 — Query Mesh Fetch History (Priority: P2)

**Goal**: MCP query tool retrieves mesh fetch events with filtering and pagination

**Independent Test**: Record mesh fetch events, call `query_mesh_fetches` tool, verify paginated results with correct data

### Implementation for User Story 2

- [x] T011 [P] [US2] Create `MeshFetchQueryTools.fsi` in `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fsi` — expose static MCP tool type with `query_mesh_fetches` method
- [x] T012 [P] [US2] Create `MeshFetchQueryTools.fs` in `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fs` — implement `query_mesh_fetches` tool: accept session_id, minutes_ago, mesh_id (optional filter), page_size, cursor. Read entries from ChunkReader, filter to MeshFetchEvent type, optionally filter by mesh_id, format and return paginated results
- [x] T013 [US2] Add `MeshFetchQueryTools.fsi` and `MeshFetchQueryTools.fs` to compilation order in `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` (before Program.fs)
- [x] T014 [US2] Update `query_summary` in `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs` to include mesh fetch event count in the summary output
- [x] T015 [US2] Build and verify tool registration: `dotnet build src/PhysicsSandbox.Mcp -p:StrideCompilerSkipBuild=true`

**Checkpoint**: `query_mesh_fetches` tool registered and returns paginated results. Summary includes fetch event count.

---

## Phase 4: Polish & Cross-Cutting

**Purpose**: Surface area, validation, documentation

- [x] T016 [P] Update surface area test in `tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs` if MeshFetchQueryTools module adds new public API
- [x] T017 Run full test suite: `dotnet test tests/PhysicsSandbox.Mcp.Tests tests/PhysicsServer.Tests -p:StrideCompilerSkipBuild=true`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **User Story 1 (Phase 2)**: Depends on Phase 1 (types must compile first)
- **User Story 2 (Phase 3)**: Depends on Phase 2 (needs MeshFetchEvent in recording to query)
- **Polish (Phase 4)**: Depends on all user story phases

### Parallel Opportunities

Within Phase 2:
```
T006 (ChunkWriter) || T007 (ChunkReader)  -- parallel, different files
T005 (PhysicsHubService audit publish) → T008 (RecordingEngine detection)
```

Within Phase 3:
```
T011 (MeshFetchQueryTools.fsi) || T012 (MeshFetchQueryTools.fs)  -- parallel
T014 (query_summary update) — independent of T011/T012
```

---

## Implementation Strategy

### MVP (User Story 1)

1. Complete Phase 1: Proto + Types
2. Complete Phase 2: Recording pipeline
3. **STOP and VALIDATE**: FetchMeshes calls are recorded, write/read round-trip works
4. Build and test: `dotnet test tests/PhysicsSandbox.Mcp.Tests -p:StrideCompilerSkipBuild=true`

### Full Delivery

1. US1 → Core recording (MVP)
2. US2 → Query tool + summary update
3. Phase 4 → Surface area + validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- All new F# modules require .fsi signature files (Constitution Principle V)
- Mesh fetch observations flow through the existing command audit stream (StreamCommands) — no cross-service callback needed
