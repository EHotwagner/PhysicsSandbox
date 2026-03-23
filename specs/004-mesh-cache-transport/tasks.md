# Tasks: Mesh Cache and On-Demand Transport

**Input**: Design documents from `/specs/004-mesh-cache-transport/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Proto Contract Changes)

**Purpose**: Define the wire format changes that all components depend on

- [x] T001 Add `CachedShapeRef` message (mesh_id, bbox_min, bbox_max) and `cached_ref` field 11 to `Shape` oneof in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T002 Add `MeshGeometry` message (mesh_id, shape), `MeshRequest` message (repeated mesh_ids), `MeshResponse` message (repeated meshes) in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T003 Add `repeated MeshGeometry new_meshes = 9` field to `SimulationState` message in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T004 Add `rpc FetchMeshes (MeshRequest) returns (MeshResponse)` to `PhysicsHub` service in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T005 Build solution and verify proto codegen succeeds: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core modules that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 [P] Create `MeshIdGenerator.fsi` signature file in `src/PhysicsSimulation/World/MeshIdGenerator.fsi` — expose `computeMeshId: Shape -> string option` (returns Some for ConvexHull/MeshShape/Compound, None for primitives) and `computeBoundingBox: Shape -> (Vec3 * Vec3) option` (returns bbox_min, bbox_max)
- [x] T007 [P] Create `MeshIdGenerator.fs` in `src/PhysicsSimulation/World/MeshIdGenerator.fs` — implement content-addressed SHA-256 hash (truncated to 128 bits, 32 hex chars) of proto-serialized geometry bytes. ConvexHull: hash points. MeshShape: hash triangles. Compound: hash children recursively. Primitives return None. Implement AABB computation: iterate points/vertices/children to find min/max.
- [x] T008 Add `MeshIdGenerator.fsi` and `MeshIdGenerator.fs` to compilation order in `src/PhysicsSimulation/PhysicsSimulation.fsproj` (before SimulationWorld files)
- [x] T009 [P] Create `MeshIdGeneratorTests.fs` in `tests/PhysicsSimulation.Tests/MeshIdGeneratorTests.fs` — test: same ConvexHull points → same ID, different points → different ID, Sphere returns None, Compound with mesh children produces deterministic ID, AABB computation correctness for ConvexHull and MeshShape
- [x] T010 Add `MeshIdGeneratorTests.fs` to compilation order in `tests/PhysicsSimulation.Tests/PhysicsSimulation.Tests.fsproj`
- [x] T011 Build and run PhysicsSimulation.Tests to verify MeshIdGenerator works: `dotnet test tests/PhysicsSimulation.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: MeshIdGenerator available — proto compiles, content-hash IDs work, AABB computation verified

---

## Phase 3: User Story 1 — Bandwidth-Efficient State Streaming (Priority: P1) 🎯 MVP

**Goal**: State updates use CachedShapeRef with mesh IDs instead of inline geometry for complex shapes after their first tick, reducing message size by ≥80%

**Independent Test**: Create a simulation with ConvexHull bodies. After the first tick, verify state update messages contain CachedShapeRef (not full vertex data) and that new_meshes is populated on the first tick only.

### Implementation for User Story 1

- [x] T012 [US1] Extend `BodyRecord` type in `src/PhysicsSimulation/World/SimulationWorld.fs` to include `MeshId: string option` and `BoundingBox: (Vec3 * Vec3) option` fields, computed once on body addition via MeshIdGenerator
- [x] T013 [US1] Add `EmittedMeshIds: Set<string>` to `World` type in `src/PhysicsSimulation/World/SimulationWorld.fs` — tracks which mesh IDs have been sent at least once. Cleared on simulation reset.
- [x] T014 [US1] Modify `buildBodyProto` in `src/PhysicsSimulation/World/SimulationWorld.fs` — for bodies with MeshId: emit `CachedShapeRef(mesh_id, bbox_min, bbox_max)` instead of inline ShapeProto. For bodies without MeshId (primitives): emit inline ShapeProto as before.
- [x] T015 [US1] Modify `buildState` in `src/PhysicsSimulation/World/SimulationWorld.fs` — collect mesh IDs not yet in EmittedMeshIds, add corresponding `MeshGeometry(mesh_id, shape)` to `state.NewMeshes`, then add those IDs to EmittedMeshIds
- [x] T016 [US1] Update `SimulationWorld.fsi` signature file in `src/PhysicsSimulation/World/SimulationWorld.fsi` if any public API surface changed
- [x] T017 [US1] Update `convertShape` in `src/PhysicsSimulation/World/SimulationWorld.fs` to handle `CachedShapeRef` ShapeCase (should not occur in simulation commands — return Error if received)
- [x] T018 [US1] Add unit tests in `tests/PhysicsSimulation.Tests/SimulationWorldTests.fs` — test: add ConvexHull body → first state has CachedShapeRef + new_meshes entry; second state has CachedShapeRef only + empty new_meshes; add Sphere body → always inline; reset clears EmittedMeshIds
- [x] T019 [US1] Run simulation tests: `dotnet test tests/PhysicsSimulation.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Simulation emits bandwidth-efficient state — CachedShapeRef for complex shapes, new_meshes for first occurrence

---

## Phase 4: User Story 2 — Server-Side Mesh Cache and Distribution (Priority: P1)

**Goal**: Server caches mesh geometry and serves on-demand FetchMeshes requests. Late-joining subscribers can resolve all mesh IDs.

**Independent Test**: Start simulation with mesh bodies, connect a new subscriber, verify it receives CachedShapeRef IDs in state and can fetch full geometry via FetchMeshes.

### Implementation for User Story 2

- [x] T020 [P] [US2] Create `MeshCache.fsi` in `src/PhysicsServer/Hub/MeshCache.fsi` — expose: `type MeshCacheState`, `create: unit -> MeshCacheState`, `add: string -> Shape -> MeshCacheState -> unit`, `tryGet: string -> MeshCacheState -> Shape option`, `getMany: string list -> MeshCacheState -> MeshGeometry list`, `clear: MeshCacheState -> unit`, `count: MeshCacheState -> int`
- [x] T021 [P] [US2] Create `MeshCache.fs` in `src/PhysicsServer/Hub/MeshCache.fs` — implement using ConcurrentDictionary<string, Shape>. `getMany` returns MeshGeometry for all found IDs (silently skips unknown). `clear` removes all entries.
- [x] T022 [US2] Add `MeshCache.fsi` and `MeshCache.fs` to compilation order in `src/PhysicsServer/PhysicsServer.fsproj` (before MessageRouter)
- [x] T023 [US2] Add `MeshCache: MeshCacheState` field to `MessageRouter` type in `src/PhysicsServer/Hub/MessageRouter.fs`. Initialize in router creation.
- [x] T024 [US2] Modify `publishState` in `src/PhysicsServer/Hub/MessageRouter.fs` — iterate `state.NewMeshes`, add each to MeshCache via `MeshCache.add`
- [x] T025 [US2] Modify `disconnectSimulation` in `src/PhysicsServer/Hub/MessageRouter.fs` — call `MeshCache.clear` when simulation disconnects
- [x] T026 [US2] Handle `ResetSimulation` command in `src/PhysicsServer/Hub/MessageRouter.fs` — clear MeshCache when reset command passes through
- [x] T027 [US2] Implement `FetchMeshes` RPC override in `src/PhysicsServer/Services/PhysicsHubService.fs` — read mesh IDs from request, call `MeshCache.getMany`, return MeshResponse
- [x] T028 [P] [US2] Create `MeshResolver.fsi` in `src/PhysicsViewer/Streaming/MeshResolver.fsi` — expose: `type MeshResolverState`, `create: PhysicsHub.PhysicsHubClient -> MeshResolverState`, `processNewMeshes: MeshGeometry seq -> MeshResolverState -> unit`, `resolve: string -> MeshResolverState -> Shape option`, `fetchMissing: string list -> MeshResolverState -> Async<unit>`
- [x] T029 [P] [US2] Create `MeshResolver.fs` in `src/PhysicsViewer/Streaming/MeshResolver.fs` — local ConcurrentDictionary cache. `processNewMeshes` adds from state. `resolve` checks cache. `fetchMissing` calls FetchMeshes RPC, adds results to cache.
- [x] T030 [US2] Add `MeshResolver.fsi` and `MeshResolver.fs` to compilation order in `src/PhysicsViewer/PhysicsViewer.fsproj` (before Program.fs)
- [x] T031 [P] [US2] Create `MeshResolver.fsi` in `src/PhysicsClient/Connection/MeshResolver.fsi` — same interface as viewer but with synchronous fetch (`fetchMissingSync: string list -> MeshResolverState -> unit`)
- [x] T032 [P] [US2] Create `MeshResolver.fs` in `src/PhysicsClient/Connection/MeshResolver.fs` — same ConcurrentDictionary pattern, blocking FetchMeshes call
- [x] T033 [US2] Add `MeshResolver.fsi` and `MeshResolver.fs` to compilation order in `src/PhysicsClient/PhysicsClient.fsproj`
- [x] T034 [P] [US2] Create `MeshResolver.fsi` in `src/PhysicsSandbox.Mcp/MeshResolver.fsi` — same interface as client (synchronous fetch)
- [x] T035 [P] [US2] Create `MeshResolver.fs` in `src/PhysicsSandbox.Mcp/MeshResolver.fs` — same ConcurrentDictionary pattern, blocking FetchMeshes call
- [x] T036 [US2] Add `MeshResolver.fsi` and `MeshResolver.fs` to compilation order in `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T037 [US2] Integrate viewer MeshResolver into `src/PhysicsViewer/Program.fs` — create resolver with gRPC client, on each state update call `processNewMeshes` for state.NewMeshes, collect unknown CachedShapeRef mesh_ids, call `fetchMissing` asynchronously
- [x] T038 [US2] Integrate client MeshResolver into `src/PhysicsClient/Connection/Session.fs` — on state receipt, process new_meshes, resolve CachedShapeRefs for display
- [x] T039 [US2] Modify `shapeDescription` in `src/PhysicsClient/Display/StateDisplay.fs` — handle CachedShapeRef ShapeCase: resolve from MeshResolver if available, otherwise return `"Cached({mesh_id_prefix})"`
- [x] T040 [US2] Modify `matchesShape` in `src/PhysicsClient/Display/LiveWatch.fs` — handle CachedShapeRef: resolve from MeshResolver, match on resolved shape type
- [x] T041 [US2] Integrate MCP MeshResolver into `src/PhysicsSandbox.Mcp/Program.fs` — on state receipt, process new_meshes
- [x] T042 [US2] Modify `RecordingEngine.fs` in `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — on state receipt, also write `LogEntry.MeshDefinition` entries for each item in state.NewMeshes
- [x] T043 [US2] Add `MeshDefinition` case to `LogEntry` discriminated union in `src/PhysicsSandbox.Mcp/Recording/Types.fs` and `src/PhysicsSandbox.Mcp/Recording/Types.fsi`
- [x] T044 [P] [US2] Create `MeshCacheTests.fs` in `tests/PhysicsServer.Tests/MeshCacheTests.fs` — test: add/get roundtrip, getMany with partial hits, clear empties cache, concurrent read/write safety
- [x] T045 [US2] Add `MeshCacheTests.fs` to compilation order in `tests/PhysicsServer.Tests/PhysicsServer.Tests.fsproj`
- [x] T046 [P] [US2] Create `MeshResolverTests.fs` in `tests/PhysicsClient.Tests/MeshResolverTests.fs` — test: processNewMeshes populates cache, resolve returns Some for cached and None for unknown, shapeDescription resolves CachedShapeRef to underlying type name
- [x] T047 [US2] Add `MeshResolverTests.fs` to compilation order in `tests/PhysicsClient.Tests/PhysicsClient.Tests.fsproj`
- [x] T048 [US2] Run server and client tests: `dotnet test tests/PhysicsServer.Tests tests/PhysicsClient.Tests -p:StrideCompilerSkipBuild=true`
- [x] T049 [US2] Build full solution to verify all integrations compile: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Server caches meshes, subscribers can fetch on demand, late joiners work

---

## Phase 5: User Story 3 — Bounding Box Placeholder While Mesh Loads (Priority: P2)

**Goal**: Viewer displays correctly-sized bounding box placeholders for unresolved meshes, replaces with full shape once geometry arrives

**Independent Test**: Connect viewer to running simulation with ConvexHull bodies. Unresolved shapes appear as bounding boxes at correct position/size. After fetch completes, shapes update.

### Implementation for User Story 3

- [x] T050 [US3] Handle `CachedShapeRef` in `primitiveType` function in `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — return `PrimitiveModelType.Cube` (bounding box placeholder)
- [x] T051 [US3] Handle `CachedShapeRef` in `shapeSize` function in `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — compute size from `bbox_max - bbox_min`, return as Vector3
- [x] T052 [US3] Handle `CachedShapeRef` in `defaultColor` function in `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — use a distinct placeholder color (e.g., semi-transparent magenta) to visually indicate unresolved state
- [x] T053 [US3] Modify `createEntity` in `src/PhysicsViewer/Rendering/SceneManager.fs` — when body shape is CachedShapeRef and mesh is resolved in MeshResolver, use resolved shape for primitiveType/shapeSize instead. When unresolved, use CachedShapeRef bbox as placeholder.
- [x] T054 [US3] Modify `applyState` in `src/PhysicsViewer/Rendering/SceneManager.fs` — track which entities are placeholders. When a previously-unresolved mesh_id becomes resolved, recreate the entity with the real shape (delete old entity, create new with resolved shape).
- [x] T055 [US3] Handle `CachedShapeRef` in `src/PhysicsViewer/Rendering/DebugRenderer.fs` — render wireframe bounding box for unresolved meshes, update to resolved shape wireframe when geometry arrives
- [x] T056 [US3] Update `ShapeGeometry.fsi` in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi` if any public signatures changed
- [x] T057 [P] [US3] Add tests in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs` — test: CachedShapeRef returns Cube primitiveType, shapeSize returns bbox dimensions, defaultColor returns placeholder color
- [x] T058 [P] [US3] Create `MeshResolverTests.fs` in `tests/PhysicsViewer.Tests/MeshResolverTests.fs` — test: processNewMeshes populates cache, resolve returns Some for cached mesh_id and None for unknown, fetchMissing adds results to cache, duplicate fetch requests are deduplicated
- [x] T059 [US3] Add `MeshResolverTests.fs` to compilation order in `tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj`
- [x] T060 [US3] Run viewer tests: `dotnet test tests/PhysicsViewer.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Viewer shows bounding box placeholders that upgrade to real shapes on mesh resolution

---

## Phase 6: User Story 4 — Separate Mesh Channel (Priority: P2)

**Goal**: Mesh fetching runs asynchronously and does not block or degrade the 60 Hz state stream

**Independent Test**: Transfer large meshes while monitoring state update timing — state updates maintain <5% jitter

### Implementation for User Story 4

- [x] T061 [US4] Ensure viewer `fetchMissing` in `src/PhysicsViewer/Streaming/MeshResolver.fs` runs on a background task (not on the state-receive thread). Use `Async.Start` or `Task.Run` so mesh fetches are fire-and-forget relative to state processing.
- [x] T062 [US4] Add deduplication to viewer MeshResolver in `src/PhysicsViewer/Streaming/MeshResolver.fs` — track `pending: ConcurrentDictionary<string, unit>` to prevent duplicate fetch requests for the same mesh_id while a fetch is in flight
- [x] T063 [US4] Ensure client MeshResolver in `src/PhysicsClient/Connection/MeshResolver.fs` fetches on a separate thread from state display (or accepts short blocking since client is text-only)
- [x] T064 [US4] Ensure MCP MeshResolver in `src/PhysicsSandbox.Mcp/MeshResolver.fs` fetches synchronously (MCP tools are request/response, blocking is acceptable)

**Checkpoint**: Mesh fetching is decoupled from state streaming, no jitter on state updates

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Surface area baselines, observability, and validation

- [x] T065 [P] Update surface area baseline for PhysicsSimulation in `tests/PhysicsSimulation.Tests/` — add MeshIdGenerator public API entries
- [x] T066 [P] Create surface area baseline for MeshCache in `tests/PhysicsServer.Tests/` if server has baseline tests
- [x] T067 [P] Update surface area baseline for PhysicsViewer in `tests/PhysicsViewer.Tests/` — add MeshResolver public API entries
- [x] T068 [P] Update surface area baseline for PhysicsClient in `tests/PhysicsClient.Tests/` — add MeshResolver public API entries
- [x] T069 [P] Update surface area baseline for PhysicsSandbox.Mcp in `tests/PhysicsSandbox.Mcp.Tests/` — add MeshResolver public API entries
- [x] T070 Add structured log messages for mesh cache events in `src/PhysicsServer/Hub/MeshCache.fs` — log on: mesh cached (mesh_id, shape type, byte size), FetchMeshes served (count, hit/miss), cache cleared
- [x] T071 Add mesh cache metrics to `MetricsCounter` in `src/PhysicsServer/Hub/` — track: meshes_cached_total, fetch_requests_total, fetch_hits_total, fetch_misses_total
- [x] T072 Create integration test `MeshCacheIntegrationTests.cs` in `tests/PhysicsSandbox.Integration.Tests/` — end-to-end: start Aspire, add ConvexHull body via gRPC, stream state, verify CachedShapeRef in body shape, call FetchMeshes, verify geometry returned, assert late-joiner resolves all meshes within 5 seconds (SC-002)
- [x] T073 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T074 Update demo scripts in `Scripting/demos/` if any demos create ConvexHull/MeshShape/Compound bodies — verify they still work with CachedShapeRef in state (Prelude helpers may need to resolve shapes)
- [x] T075 Update `Prelude.fsx` in `Scripting/demos/Prelude.fsx` — add helper to resolve CachedShapeRef from state (fetch from server if needed) for scripts that inspect body shapes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (proto changes must compile first) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 (needs MeshIdGenerator)
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs simulation to emit CachedShapeRef)
- **User Story 3 (Phase 5)**: Depends on Phase 4 (needs MeshResolver to resolve shapes)
- **User Story 4 (Phase 6)**: Depends on Phase 4 (needs MeshResolver to exist for async optimization)
- **Polish (Phase 7)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only — independently testable by checking state message contents
- **US2 (P1)**: Depends on US1 — needs CachedShapeRef in state to exercise cache/fetch. Can be tested by adding bodies then calling FetchMeshes.
- **US3 (P2)**: Depends on US2 — needs MeshResolver for placeholder→real transition. Can be tested visually.
- **US4 (P2)**: Depends on US2 — needs MeshResolver async infrastructure. Can be tested by measuring state timing under load.

### Parallel Opportunities

Within Phase 2:
```
T006 (MeshIdGenerator.fsi) || T007 (MeshIdGenerator.fs)  -- parallel, different files
T009 (MeshIdGeneratorTests.fs)                            -- after T006+T007
```

Within Phase 4:
```
T020 (MeshCache.fsi) || T021 (MeshCache.fs)              -- parallel
T028 (Viewer MeshResolver.fsi) || T029 (Viewer MeshResolver.fs)  -- parallel
T031 (Client MeshResolver.fsi) || T032 (Client MeshResolver.fs)  -- parallel
T034 (MCP MeshResolver.fsi) || T035 (MCP MeshResolver.fs)        -- parallel
T044 (MeshCacheTests.fs) || T046 (ClientMeshResolverTests.fs)    -- parallel with resolver work
```

Within Phase 5:
```
T057 (ShapeGeometryTests.fs) || T058 (MeshResolverTests.fs)  -- parallel, different files
```

Within Phase 7:
```
T065 || T066 || T067 || T068 || T069  -- all surface area updates parallel
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Proto changes
2. Complete Phase 2: MeshIdGenerator
3. Complete Phase 3: US1 — simulation emits CachedShapeRef
4. Complete Phase 4: US2 — server cache + subscriber resolvers
5. **STOP and VALIDATE**: Full pipeline works, bandwidth reduced, late joiners fetch meshes
6. Build and test: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

### Incremental Delivery

1. US1 + US2 → Core bandwidth optimization (MVP)
2. US3 → Visual placeholder experience
3. US4 → Async non-blocking fetch (performance polish)
4. Phase 7 → Observability, baselines, demo updates

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US2 are both P1 but US2 depends on US1 (sequential within P1 tier)
- US3 and US4 are both P2 and can be done in parallel after US2
- All new F# modules require .fsi signature files (Constitution Principle V)
- Commit after each task or logical group
- Proto type alias pattern needed: `type ProtoCachedShapeRef = PhysicsSandbox.Shared.Contracts.CachedShapeRef` in F# files that also use BepuFSharp types
