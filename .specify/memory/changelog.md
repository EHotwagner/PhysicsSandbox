# Merged Features Log

## Robust Network Connectivity — 2026-03-24
**Branch:** 005-robust-network-connectivity
**Spec:** specs/005-robust-network-connectivity

**What was added:**
- ViewCommand per-subscriber broadcast (ConcurrentDictionary<Guid, Channel<ViewCommand>>) replacing single-consumer Channel
- subscribeViewCommands/unsubscribeViewCommands functions in MessageRouter
- StreamViewCommands uses per-subscriber channel with try/finally cleanup
- MCP SSE endpoint isProxied=false in AppHost to bypass DCP HTTP/2 proxy
- NetworkProblems.md consolidated with container environment section (port table, networking boundary) and 7 structured entries
- Viewer 60 FPS when unfocused (WindowMinimumUpdateRate/MinimizedMinimumUpdateRate = 16ms)
- 6 new broadcast unit tests (ordering, multi-subscriber, disconnect, zero-sub, backpressure)
- 1 new integration test (two-viewer broadcast)

**New/Modified Components:**
- `src/PhysicsServer/Hub/MessageRouter.fs(i)` — ViewCommandSubscribers, subscribeViewCommands, unsubscribeViewCommands, removed readViewCommand
- `src/PhysicsServer/Services/PhysicsHubService.fs` — per-subscriber StreamViewCommands with Trace logging
- `src/PhysicsSandbox.AppHost/AppHost.cs` — MCP .WithEndpoint("http", e => e.IsProxied = false)
- `src/PhysicsViewer/Program.fs` — WindowMinimumUpdateRate 16ms
- `reports/NetworkProblems.md` — Container Environment section + 2 new entries
- `tests/PhysicsServer.Tests/MessageRouterTests.fs` — 6 new/updated tests
- `tests/PhysicsSandbox.Integration.Tests/ServerHubTests.cs` — BroadcastToMultipleSubscribers test

**Tasks Completed:** 29/29 tasks

---

## Smooth Camera Controls and Demo Narration — 2026-03-24
**Branch:** 004-camera-smooth-demos
**Spec:** specs/004-camera-smooth-demos

**What was added:**
- 9 new ViewCommand proto messages (SmoothCamera, CameraLookAt, CameraFollow, CameraOrbit, CameraChase, CameraFrameBodies, CameraShake, CameraStop, SetNarration) — fields 5-13
- CameraMode discriminated union state machine with 7 modes (Transitioning, LookingAt, Following, Orbiting, Chasing, Framing, Shaking) and smoothstep interpolation
- 6 body-relative camera modes tracking physics bodies by ID with per-frame updates
- Narration overlay at screen position (10, 50) via DebugTextSystem.Print
- 10 new PhysicsClient ViewCommand functions (smoothCamera, cameraLookAt, cameraFollow, cameraOrbit, cameraChase, cameraFrameBodies, cameraShake, cameraStop, setNarration + zoom variant)
- 10 F# scripting helpers in Prelude.fsx and 10 Python helpers in prelude.py
- Demo22_CameraShowcase (~40 seconds, 8+ camera movements) in both F# and Python
- All 42 demos (21 F# + 21 Python) enhanced with cinematic camera sequences and narration labels
- ConcurrentQueue<ViewCommand> drain loop replacing single-slot Volatile.Write (fixed rapid command drops)
- kill.sh fixed with .dll suffix patterns to prevent self-kill
- Body-not-found camera mode hold behavior (no longer cancels immediately)

**New/Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — 9 new ViewCommand oneof fields (5-13)
- `src/PhysicsViewer/Rendering/CameraController.fs(i)` — CameraMode DU, smoothstep, updateCameraMode, body-relative modes
- `src/PhysicsViewer/Rendering/SceneManager.fs(i)` — NarrationText field, applyNarration
- `src/PhysicsViewer/Program.fs` — 9 ViewCommand handlers, body position map, narration rendering, ConcurrentQueue
- `src/PhysicsClient/Commands/ViewCommands.fs(i)` — 10 new client functions
- `Scripting/demos/Prelude.fsx` — 10 camera/narration helpers, PhysicsClient 0.4.0
- `Scripting/demos/Demo22_CameraShowcase.fsx` — new
- `Scripting/demos_py/demo22_camera_showcase.py` — new
- `Scripting/demos_py/prelude.py` — 10 camera/narration helpers
- `kill.sh` — .dll suffix patterns
- `reports/NetworkProblems.md` — 3 new entries
- `reports/2026-03-24-camera-commands-debugging.md` — new debugging report

**Tasks Completed:** 99/99 tasks

---

## Enhance Demos with New Shapes and Viewer Labels — 2026-03-24
**Branch:** 004-enhance-demos-shapes
**Spec:** specs/004-enhance-demos-shapes

**What was added:**
- SetDemoMetadata ViewCommand (proto field 4) for transporting demo name/description to viewer
- Viewer window title "PhysicsSandbox Viewer" via game.Window.Title
- Demo label overlay at top-left (10, 10) showing current demo name + description; "Free Mode" default
- 8 existing demos enhanced with Triangle, ConvexHull, Mesh, Compound shapes (01, 03, 04, 06, 09, 10, 13, 14)
- Demo 19: Shape Gallery — all shape types side-by-side (F# + Python)
- Demo 20: Compound Constructions — L-shapes, T-shapes, dumbbells (F# + Python)
- Demo 21: Mesh & Hull Playground — convex hulls + meshes on obstacles (F# + Python)
- setDemoInfo/set_demo_info metadata calls added to all 21 demos
- makeMeshCmd/make_mesh_cmd helper in both Prelude.fsx and prelude.py

**New/Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — SetDemoMetadata message + ViewCommand field 4
- `src/PhysicsClient/Commands/ViewCommands.fs(i)` — setDemoMetadata function
- `src/PhysicsViewer/Rendering/SceneManager.fs(i)` — DemoName/DemoDescription fields, applyDemoMetadata
- `src/PhysicsViewer/Program.fs` — window title, demo label overlay, ViewCommand handling
- `Scripting/demos/Prelude.fsx` — makeMeshCmd, setDemoInfo, PhysicsClient 0.3.0
- `Scripting/demos/19_ShapeGallery.fsx`, `20_CompoundConstructions.fsx`, `21_MeshHullPlayground.fsx` — new
- `Scripting/demos_py/demo19_shape_gallery.py`, `demo20_compound_constructions.py`, `demo21_mesh_hull_playground.py` — new
- `Scripting/demos_py/prelude.py` — make_mesh_cmd, set_demo_info

**Tasks Completed:** 43/50 tasks (7 test tasks skipped per user request)

---

## Proper Shape Rendering — 2026-03-24
**Branch:** 004-proper-shape-rendering
**Spec:** specs/004-proper-shape-rendering

**What was added:**
- Custom mesh rendering for Triangle, Mesh, ConvexHull shapes (vertex/index buffers with per-face normals, double-sided faces)
- Compound shape decomposition into individually-rendered children with correct local transforms
- ShapeRef resolution via RegisteredShapes (ShapeHandle lookup)
- CachedRef flows through custom mesh pipeline after MeshResolver resolution
- Debug wireframes with actual geometry edges (LineList from deduplicated edge sets)
- Degenerate shape fallbacks (collinear triangles, empty meshes, <4-point hulls → visible placeholders)
- MIConvexHull 1.1.19 dependency for convex hull face computation

**New/Modified Components:**
- `src/PhysicsViewer/Rendering/ShapeGeometry.fs(i)` — CustomMeshData type, buildTriangleMesh, buildMeshMesh, buildConvexHullMesh, isCustomShape, buildCustomMesh dispatcher
- `src/PhysicsViewer/Rendering/SceneManager.fs(i)` — createModelFromMeshData, createCompoundEntity, createCustomEntity, ShapeRef resolution in resolveShape
- `src/PhysicsViewer/Rendering/DebugRenderer.fs` — createCustomWireframe (LineList), createShapeWireframe dispatch
- `tests/PhysicsViewer.Tests/SceneManagerTests.fs` — 15 new tests (custom mesh, convex hull, degenerate, color palette)

**Tasks Completed:** 59/59 tasks

---

## Backlog Fixes and Test Progress Reporting — 2026-03-23
**Branch:** 004-backlog-fix-test-progress
**Spec:** specs/004-backlog-fix-test-progress

**What was added:**
- Test progress script (`test-progress.sh`) with per-project `[3/7]` progress, ETA, immediate failure surfacing, headless build support
- Fixed 10 silent TryAdd/TryRemove failures: 6 single-body registry ops → `Result.Error`, 1 bulk clearAll → `Trace.TraceWarning`, 3 cache ops → `Trace.TraceWarning`
- Pending query expiration in MessageRouter (30s timeout, 10s sweep via `System.Threading.Timer`)
- 6 new constraint builders completing all 10 types: DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine
- Shared test helpers: `tests/SharedTestHelpers.fs` (F#, linked by 6 projects) + `IntegrationTestHelpers.cs` (C#, used by 14 test files)
- Spec drift resolution: FR-004a backfilled to match clearAll warning pattern

**New Components:**
- `test-progress.sh` — Bash test runner with progress display
- `tests/SharedTestHelpers.fs` — F# shared test helpers (getPublicMembers, assertContains)
- `tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs` — C# shared integration helpers
- `tests/PhysicsClient.Tests/RegistryErrorTests.fs` — 7 TryAdd/TryRemove error tests
- `tests/PhysicsServer.Tests/QueryExpirationTests.fs` — 4 query expiration tests
- `tests/PhysicsSandbox.Scripting.Tests/ConstraintBuilderTests.fs` — 6 new builder tests

**Modified Components:**
- `src/PhysicsClient/Commands/SimulationCommands.fs` — Result.Error returns for 6 body ops + TraceWarning for clearAll
- `src/PhysicsClient/Connection/MeshResolver.fs` — Trace.TraceWarning for cache duplicates (2 instances)
- `src/PhysicsClient/Connection/Session.fs` — Trace.TraceWarning for missing cache entry
- `src/PhysicsServer/Hub/MessageRouter.fs/.fsi` — PendingQueryEntry type, expireStaleQueries, sweep timer
- `src/PhysicsSandbox.Scripting/ConstraintBuilders.fs/.fsi` — 6 new builders + defaultMotor helper
- `src/PhysicsSandbox.Scripting/Prelude.fs/.fsi` — Re-export new builders
- 14 integration test files — migrated to IntegrationTestHelpers
- 6 F# test projects — migrated to SharedTestHelpers.fs

**Tasks Completed:** 77/78 tasks

---

## State Stream Bandwidth Optimization — 2026-03-23
**Branch:** 004-state-stream-optimization
**Spec:** specs/004-state-stream-optimization

**What was added:**
- Split monolithic 60 Hz SimulationState into lean TickState (pose-only for dynamic bodies) + PropertyEvent stream (semi-static properties on creation/change/backfill)
- StreamProperties RPC — server→client property event stream with PropertySnapshot late-joiner backfill
- TickState replaces SimulationState on StreamState RPC — server decomposes in MessageRouter
- ExcludeVelocity on StateRequest — viewer opts out of velocity fields for additional bandwidth savings
- Constraints and registered shapes moved to PropertyEvent channel (not in every tick)
- Client-side state reconstruction in all 4 client types (PhysicsClient, Viewer, MCP, demo scripts)
- Separate tick vs property bandwidth tracking in MetricsCounter
- ~69% bandwidth reduction at 200 bodies (TickState <=16 KB vs ~50 KB baseline)
- ~80% reduction for viewer (TickState without velocity <=11 KB)

**New Components:**
- `tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs` — 6 unit tests for split channel routing
- `tests/PhysicsSimulation.Tests/StateDecompositionTests.fs` — 11 unit tests for state decomposition
- `tests/PhysicsSandbox.Integration.Tests/StateStreamOptimizationIntegrationTests.cs` — 12 integration tests

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — BodyPose, TickState, BodyProperties, PropertyEvent, PropertySnapshot messages + StreamProperties RPC
- `src/PhysicsServer/Hub/MessageRouter.fs` — buildTickState, detectPropertyEvents, publishTick, publishPropertyEvent, property subscriber mgmt
- `src/PhysicsServer/Hub/StateCache.fs` — Dual cache (TickState + PropertySnapshot)
- `src/PhysicsServer/Hub/MetricsCounter.fs` — Tick vs property bandwidth counters
- `src/PhysicsServer/Services/PhysicsHubService.fs` — StreamState sends TickState, new StreamProperties RPC, StripVelocity helper
- `src/PhysicsClient/Connection/Session.fs` — BodyPropertiesCache, StreamProperties subscription, reconstructState
- `src/PhysicsSandbox.Mcp/GrpcConnection.fs` — Same reconstruction pattern as client
- `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — OnPropertyEventReceived for mesh recording
- `src/PhysicsViewer/Streaming/ViewerClient.fs` — streamState(excludeVelocity) + streamProperties
- `src/PhysicsViewer/Program.fs` — reconstructSimState, ExcludeVelocity=true

**Tasks Completed:** 108/109 tasks (T041 deferred — requires unimplemented SetColor command)

---

## MCP Mesh Fetch Logging — 2026-03-23
**Branch:** 004-mcp-mesh-logging
**Spec:** specs/004-mcp-mesh-logging

**What was added:**
- MeshFetchEvent recording: every FetchMeshes RPC call captured with requested IDs, hit/miss counts, missed IDs
- Mesh fetch observations published via CommandEvent audit stream (MeshFetchLog proto oneof case)
- MeshFetchEvent binary serialization (EntryType=3) in ChunkWriter/ChunkReader
- RecordingEngine detects mesh fetch events in command stream and records them
- `query_mesh_fetches` MCP tool with session, time-range, mesh ID filtering + pagination
- `query_summary` now includes mesh fetch event count
- 1 new unit test (round-trip), 2 surface area tests

**New Components:**
- `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fsi/.fs` — query_mesh_fetches MCP tool

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — MeshFetchLog message, CommandEvent.mesh_fetch_log oneof
- `src/PhysicsServer/Services/PhysicsHubService.fs` — Publish fetch observations to audit stream
- `src/PhysicsSandbox.Mcp/Recording/Types.fs/.fsi` — EntryType.MeshFetchEvent, LogEntry.MeshFetchEvent
- `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs` — MeshFetchEvent serialization
- `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fs` — MeshFetchEvent deserialization
- `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — Detect mesh fetch in command stream
- `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs` — query_summary includes fetch count

**Tasks Completed:** 17/17 tasks

---

## Mesh Cache and On-Demand Transport — 2026-03-23
**Branch:** 004-mesh-cache-transport
**Spec:** specs/004-mesh-cache-transport

**What was added:**
- Bandwidth-efficient state streaming: complex shapes (ConvexHull, MeshShape, Compound) replaced with CachedShapeRef (mesh_id + bounding box) after first transmission, reducing per-body message size by ~96%
- Content-addressed mesh ID generation (SHA-256 truncated to 128 bits) ensuring identical geometry always maps to the same identifier
- Server-side MeshCache (ConcurrentDictionary) populated from state updates, cleared on reset/disconnect
- FetchMeshes unary RPC for on-demand mesh geometry retrieval by late-joining subscribers
- MeshResolver modules for viewer (async fetch), client (sync fetch), MCP (sync fetch) — each with local ConcurrentDictionary cache
- Viewer bounding box placeholders (semi-transparent magenta) for unresolved meshes, replaced with real shapes on resolution
- Async non-blocking mesh fetch in viewer (Async.Start) — does not block 60 Hz state stream
- MeshDefinition recording entries in MCP for self-contained session replay
- Structured logging and metrics for mesh cache events (hits, misses, cached count)
- 2 integration tests: CachedShapeRef verification + late-joiner mesh resolution

**New Components:**
- `src/PhysicsSimulation/World/MeshIdGenerator.fsi/.fs` — Content-addressed SHA-256 mesh ID + AABB computation
- `src/PhysicsServer/Hub/MeshCache.fsi/.fs` — Server-side mesh geometry cache
- `src/PhysicsViewer/Streaming/MeshResolver.fsi/.fs` — Viewer local cache + async FetchMeshes
- `src/PhysicsClient/Connection/MeshResolver.fsi/.fs` — Client local cache + sync FetchMeshes
- `src/PhysicsSandbox.Mcp/MeshResolver.fsi/.fs` — MCP local cache + sync FetchMeshes
- `tests/PhysicsServer.Tests/MeshCacheTests.fs` — 8 MeshCache unit tests
- `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs` — 3 CachedShapeRef rendering tests
- `tests/PhysicsViewer.Tests/MeshResolverTests.fs` — 3 viewer resolver tests
- `tests/PhysicsClient.Tests/MeshResolverTests.fs` — 3 client resolver tests
- `tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs` — MCP MeshResolver surface area test
- `tests/PhysicsSandbox.Integration.Tests/MeshCacheIntegrationTests.cs` — 2 end-to-end tests

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — CachedShapeRef, MeshGeometry, MeshRequest/Response, FetchMeshes RPC, SimulationState.new_meshes
- `src/PhysicsSimulation/World/SimulationWorld.fs/.fsi` — BodyRecord (MeshId, BoundingBox), EmittedMeshIds tracking, CachedShapeRef emission
- `src/PhysicsServer/Hub/MessageRouter.fs/.fsi` — MeshCache field, populate on publishState, clear on disconnect/reset
- `src/PhysicsServer/Hub/MetricsCounter.fs/.fsi` — Mesh cache metrics (cached, fetch hits/misses)
- `src/PhysicsServer/Services/PhysicsHubService.fs/.fsi` — FetchMeshes RPC override
- `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — CachedShapeRef → Cube placeholder with bbox sizing
- `src/PhysicsViewer/Rendering/SceneManager.fs/.fsi` — Placeholder tracking + entity recreation on resolution
- `src/PhysicsViewer/Program.fs` — MeshResolver integration in state stream loop
- `src/PhysicsClient/Connection/Session.fs/.fsi` — MeshResolver integration
- `src/PhysicsClient/Display/StateDisplay.fs` — CachedShapeRef shape description resolution
- `src/PhysicsClient/Display/LiveWatch.fs` — CachedShapeRef shape filter resolution
- `src/PhysicsSandbox.Mcp/Program.fs` — MeshResolver integration
- `src/PhysicsSandbox.Mcp/Recording/Types.fs/.fsi` — MeshDefinition LogEntry case
- `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` — Write MeshDefinition entries
- `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fs` — Handle MeshDefinition serialization
- `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fs` — Handle MeshDefinition deserialization

**Tasks Completed:** 75/75 tasks

---

## MCP Data Logging for Analysis — 2026-03-23
**Branch:** 005-mcp-data-logging
**Spec:** specs/005-mcp-data-logging

**What was added:**
- Persistent data recording: auto-captures all simulation state updates and command events to disk at full fidelity using protobuf binary serialization in 1-minute chunk files
- Dual-limit storage management: configurable time window (default 10 min) and size cap (default 500 MB) with automatic pruning of oldest chunks
- 4 query MCP tools: query_body_trajectory, query_snapshots, query_events, query_summary — all with cursor-based pagination (default 100 entries/page)
- 5 session management MCP tools: start_recording, stop_recording, list_sessions, delete_session, recording_status
- Auto-start recording on first simulation state received (FR-210)
- Restart recovery: interrupted sessions marked Completed on MCP server startup
- GrpcConnection callback hooks (OnStateReceived, OnCommandReceived) for extensible stream processing
- Async Channel-based recording pipeline (capacity 10k, DropOldest) ensures zero impact on existing MCP tool responsiveness

**New Components:**
- `src/PhysicsSandbox.Mcp/Recording/Types.fsi/.fs` — LogEntry, SessionStatus, PaginationCursor, wire format
- `src/PhysicsSandbox.Mcp/Recording/SessionStore.fsi/.fs` — Session metadata CRUD (JSON persistence)
- `src/PhysicsSandbox.Mcp/Recording/ChunkWriter.fsi/.fs` — Async binary writer with pruning
- `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fsi/.fs` — Binary reader with pagination
- `src/PhysicsSandbox.Mcp/Recording/RecordingEngine.fsi/.fs` — Recording lifecycle orchestration
- `src/PhysicsSandbox.Mcp/RecordingTools.fsi/.fs` — 5 session management MCP tools
- `src/PhysicsSandbox.Mcp/RecordingQueryTools.fsi/.fs` — 4 query MCP tools
- `tests/PhysicsSandbox.Mcp.Tests/` — 12 unit tests (ChunkWriter, SessionStore, ChunkReader, RecordingEngine)

**Modified Components:**
- `src/PhysicsSandbox.Mcp/GrpcConnection.fsi/.fs` — Added OnStateReceived/OnCommandReceived callback hooks
- `src/PhysicsSandbox.Mcp/Program.fs` — RecordingEngine DI registration, callback wiring
- `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` — 16 new compile entries
- `PhysicsSandbox.slnx` — Added PhysicsSandbox.Mcp.Tests project

**Tasks Completed:** 28/31 tasks (3 deferred: integration tests + surface area baselines require running Aspire)

---

## Enhance Demos with New Body Types and Fix Impacts — 2026-03-23
**Branch:** 005-enhance-demos
**Spec:** specs/005-enhance-demos

**What was added:**
- Fixed Demo 03 (Crate Stack): boulder realigned to strike tower center via `launch` at speed 40
- Fixed Demo 04 (Bowling Alley): pyramid moved to Z=5, ball approaches frontally along Z-axis
- Added Demo 16 (Constraints): pendulum chain (ball-socket + distance-limit), hinged bridge (hinge), weld cluster (weld) — 4 constraint types
- Added Demo 17 (Query Range): raycast, overlap sphere, sweep sphere queries with printed results
- Added Demo 18 (Kinematic Sweep): kinematic bulldozer animated via setBodyPose plowing through 30 dynamic bodies
- Enhanced all 15 existing demos with custom colors using 8-color palette (projectile/target/structure + 4 accents + kinematic)
- Distributed capsules, cylinders, triangles, convex hulls, compounds across demos (8/10 shape types used)
- Applied bouncy/sticky/slippery material presets to 4 demos for visible behavioral contrast
- Extended Prelude.fsx: makeTriangleCmd, makeConvexHullCmd, makeCompoundCmd, makeKinematicCmd, withMotionType, withCollisionFilter, setPose, queryRaycast, queryOverlapSphere, querySweepSphere, 8 color palette constants
- Extended prelude.py with matching Python helpers
- Repacked PhysicsClient NuGet to 0.2.0 (with query/pose APIs)

**New Components:**
- `Scripting/demos/16_Constraints.fsx` — Constraint showcase demo
- `Scripting/demos/17_QueryRange.fsx` — Physics query demo
- `Scripting/demos/18_KinematicSweep.fsx` — Kinematic body demo

**Modified Components:**
- `Scripting/demos/Prelude.fsx` — Color palette, shape builders, kinematic/query/pose helpers
- `Scripting/demos/AllDemos.fsx` — All 18 demos (15 enhanced + 3 new)
- `Scripting/demos_py/prelude.py` — Python parity helpers
- `~/.local/share/nuget-local/PhysicsClient.0.2.0.nupkg` — Repacked NuGet

**Tasks Completed:** 29/43 tasks (14 remaining are Python parity + standalone sync)

---

## Viewer Display Settings & Shape Sizing Fix — 2026-03-23
**Branch:** 005-viewer-settings-sizing-fix
**Spec:** specs/005-viewer-settings-sizing-fix

**What was added:**
- Fixed shape sizing bug: sphere/capsule/cylinder rendered 2x too large (shapeSize passed diameter but Stride expects radius)
- Removed artificial 1.02x debug wireframe scaling — wireframes now match physics bounds exactly
- Compound shape debug wireframes render per-child shapes at correct local transforms
- Near-zero dimension clamping (0.01f minimum) prevents invisible bodies
- F11 borderless windowed fullscreen toggle with Escape to exit
- F2 settings overlay with Display (resolution) and Quality (MSAA, shadows, texture filtering, VSync) tabs
- Settings persisted to ~/.config/PhysicsSandbox/viewer-settings.json (System.Text.Json)
- MSAA applied via GraphicsDeviceManager.PreferredMultisampleCount; shadows via LightDirectionalShadowMap.CascadeCount

**New Components:**
- `src/PhysicsViewer/Settings/ViewerSettings.fsi/.fs` — Settings model + JSON persistence
- `src/PhysicsViewer/Settings/DisplayManager.fsi/.fs` — Stride window/graphics API wrapper
- `src/PhysicsViewer/Settings/SettingsOverlay.fsi/.fs` — Text-based settings UI
- `tests/PhysicsViewer.Tests/ViewerSettingsTests.fs` — Persistence round-trip tests
- `tests/PhysicsViewer.Tests/DisplayManagerTests.fs` — Display manager tests

**Modified Components:**
- `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — Fixed sizing for sphere/capsule/cylinder + min clamp
- `src/PhysicsViewer/Rendering/DebugRenderer.fs` — Removed 1.02x scale, compound child wireframes, Entity list map
- `src/PhysicsViewer/Program.fs` — Integrated F11/F2/Escape, settings persistence, overlay rendering
- `tests/PhysicsViewer.Tests/SceneManagerTests.fs` — Updated expected sizing values, added capsule/cylinder tests
- `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs` — Added baselines for 3 new modules

**Tasks Completed:** 40/40 tasks

---

## Stride BepuPhysics Integration — 2026-03-22
**Branch:** 005-stride-bepu-integration
**Spec:** specs/005-stride-bepu-integration

**What was added:**
- Extended physics sandbox from 3 to 10 shape types (capsule, cylinder, triangle, convex hull, compound, mesh, shape reference + existing sphere/box/plane)
- 10 constraint types (ball socket, hinge, weld, distance limit/spring, swing/twist limits, linear/angular motors, point-on-line) with auto-cleanup on body removal
- Per-body RGBA color with default palette by shape type
- Per-body material properties (friction, bounciness, spring settings) with presets (bouncy, sticky, slippery)
- Physics queries via dedicated RPCs: raycast (single/all hits), sweep cast, overlap — each with batch variant
- Collision layer filtering via 32-bit group/mask bitmask with runtime SetCollisionFilter
- Kinematic body support (unaffected by gravity, push dynamic bodies) with SetBodyPose runtime updates
- Debug wireframe visualization (F3 toggle) showing collider outlines and constraint connections
- Shape registration/caching mechanism for vertex-heavy shapes
- BepuFSharp 0.2.0-beta.1 wrapper with full constraint, material, query, and collision filter API
- 6 new query RPCs on PhysicsHub service
- Client interfaces: REPL commands (raycast, sweepCast, overlap, setBodyPose), MCP tools (sweep_cast, overlap, set_body_pose, add_constraint, register_shape, etc.), Scripting library (QueryBuilders, ConstraintBuilders modules)

**New Components:**
- `src/PhysicsSimulation/Queries/QueryHandler.fsi/.fs` — Query dispatch
- `src/PhysicsViewer/Rendering/ShapeGeometry.fsi/.fs` — Shape primitive selection, sizing, color palette
- `src/PhysicsViewer/Rendering/DebugRenderer.fsi/.fs` — Wireframe debug overlay
- `src/PhysicsSandbox.Scripting/ConstraintBuilders.fsi/.fs` — Constraint convenience builders
- `src/PhysicsSandbox.Scripting/QueryBuilders.fsi/.fs` — Query convenience wrappers
- `tests/PhysicsSimulation.Tests/ExtendedFeatureTests.fs` — 46 new unit tests

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — ~50 new messages, 6 query RPCs, SetBodyPose
- `src/PhysicsSimulation/World/SimulationWorld.fs` — 10 shape types, constraints, materials, colors, collision filters, kinematic dispatch, setBodyPose
- `src/PhysicsSimulation/Commands/CommandHandler.fs` — 7 new command dispatches
- `src/PhysicsServer/Hub/MessageRouter.fs` — Query channel and correlation
- `src/PhysicsServer/Services/PhysicsHubService.fs` — Query RPC implementations
- `src/PhysicsViewer/Rendering/SceneManager.fs` — Per-body color, shape geometry dispatch
- `src/PhysicsViewer/Program.fs` — F3 debug toggle integration
- `src/PhysicsClient/Commands/SimulationCommands.fs` — Query commands, setBodyPose, constraint/shape commands
- `src/PhysicsSandbox.Mcp/SimulationTools.fs` — 6 new MCP tools
- `Scripting/demos/Prelude.fsx` — Capsule/cylinder/color/material/constraint helpers
- `Scripting/demos_py/prelude.py` — Capsule/cylinder/color/material helpers

**Tasks Completed:** 86/86 tasks

---

## Improve Physics Demos — 2026-03-22
**Branch:** 004-improve-demos
**Spec:** specs/004-improve-demos

**What was added:**
- Improved all 15 physics demos to be more visually interesting and physically rich (both F# and Python suites)
- Unified demo suite: demos 11-15 integrated into AllDemos.fsx, AutoRun.fsx refactored to load AllDemos (eliminated code duplication)
- Each F# demo is now standalone-runnable via `dotnet fsi DemoNN.fsx` using new `runStandalone` Prelude helper
- Prelude.fsx refactored to top-level bindings with all PhysicsClient opens (no module wrapper)
- Added `#r "nuget: Microsoft.Extensions.Logging.Abstractions"` to Prelude for transitive dependency resolution
- Viewer shape sizing fix attempted (passing `Size` to `Bepu3DPhysicsOptions`) — visual merging still observed, deferred to separate spec

**Modified Components:**
- `Scripting/demos/Prelude.fsx` — top-level bindings, runStandalone, logging dependency
- `Scripting/demos/Demo01-15*.fsx` — improved physics scenarios + standalone boilerplate
- `Scripting/demos/AllDemos.fsx` — updated all 15 demo entries with new scenarios
- `Scripting/demos/AutoRun.fsx` — loads AllDemos instead of duplicating code
- `Scripting/demos_py/demo01-15*.py` — Python mirrors of all F# improvements
- `src/PhysicsViewer/Rendering/SceneManager.fs` — shape sizing via Bepu3DPhysicsOptions.Size

**Tasks Completed:** 45/45 tasks

---

## Scripting Library NuGet Package — 2026-03-22
**Branch:** 004-scripting-nuget-package
**Spec:** specs/004-scripting-nuget-package

**What was added:**
- Published 4 projects as local NuGet packages (0.1.0): PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, PhysicsClient, PhysicsSandbox.Scripting
- Packaging follows BepuFSharp pattern: `dotnet pack -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/`
- Dependency chain: Contracts + ServiceDefaults → PhysicsClient → Scripting
- Migrated MCP server and Scripting.Tests from ProjectReference to PackageReference
- Converted all F# script/demo DLL `#r` paths to version-agnostic `#r "nuget: ..."` references
- Eliminated `Scripting/scripts/Prelude.fsx` — scripts inline the NuGet reference directly
- Fixed port consistency: replaced all `localhost:5000` with canonical `localhost:5180` (HTTP) across ~15 files

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/*.csproj` — Added IsPackable, PackageId, Version
- `src/PhysicsSandbox.ServiceDefaults/*.csproj` — Added packaging metadata, set IsAspireSharedProject=false
- `src/PhysicsClient/*.fsproj` — ProjectRef→PackageRef for Contracts/ServiceDefaults
- `src/PhysicsSandbox.Scripting/*.fsproj` — ProjectRef→PackageRef for PhysicsClient
- `src/PhysicsSandbox.Mcp/*.fsproj` — ProjectRef→PackageRef for Scripting (removed transitive refs)
- `tests/PhysicsSandbox.Scripting.Tests/*.fsproj` — ProjectRef→PackageRef for Scripting
- `Scripting/scripts/HelloDrop.fsx` — Direct `#r "nuget: PhysicsSandbox.Scripting"`
- `Scripting/demos/Prelude.fsx` — DLL refs → `#r "nuget: PhysicsClient"`
- `Scripting/demos/AutoRun.fsx` — DLL refs → NuGet + port fix
- `Scripting/demos/Demo11-15*.fsx`, `RunAll.fsx` — Port fixes
- `Scripting/demos_py/prelude.py`, `auto_run.py`, `run_all.py` — Port fixes
- `src/PhysicsClient/Program.fs`, `src/PhysicsViewer/Program.fs` — Port fallback fixes
- `.mcp.json` — Port fix
- `reports/mcpReport.md` — Port fix

**Deleted Components:**
- `Scripting/scripts/Prelude.fsx` — No longer needed with NuGet packaging

**Tasks Completed:** 38/38 tasks

---

## F# Scripting Library — 2026-03-22
**Branch:** 004-fsharp-scripting-library
**Spec:** specs/004-fsharp-scripting-library

**What was added:**
- PhysicsSandbox.Scripting — F# class library bundling all Prelude.fsx convenience functions into 6 compiled modules with .fsi signatures
- Modules: Helpers (ok, sleep, timed), Vec3Builders (toVec3, toTuple), CommandBuilders (makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd), BatchOperations (batchAdd), SimulationLifecycle (resetSimulation, runFor, nextId), Prelude (AutoOpen re-export)
- scripts/ folder with Prelude.fsx (single #r reference) and HelloDrop.fsx validation script
- scratch/ folder (gitignored with .gitkeep) for experimentation
- MCP server integration — ClientAdapter.toVec3 now delegates to library
- 19 unit tests + surface area baseline verification

**New Components:**
- `src/PhysicsSandbox.Scripting/` — 6 module pairs (.fsi + .fs) + .fsproj
- `tests/PhysicsSandbox.Scripting.Tests/` — 4 test files + SurfaceAreaBaseline.txt
- `scripts/Prelude.fsx` — single-reference script prelude
- `scripts/HelloDrop.fsx` — validation script
- `scratch/.gitkeep` — experimentation folder

**Modified Components:**
- `PhysicsSandbox.slnx` — added Scripting + Scripting.Tests projects
- `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` — added Scripting reference
- `src/PhysicsSandbox.Mcp/ClientAdapter.fs` — toVec3 delegates to library
- `.gitignore` — added scratch/* pattern

**Tasks Completed:** 42/42 tasks

---

## Python Demo Scripts — 2026-03-21
**Branch:** 004-python-demo-scripts
**Spec:** specs/004-python-demo-scripts

**What was added:**
- 15 Python demo scripts mirroring the F# demo suite (Demo 01–15), communicating via gRPC with Python-generated proto stubs
- Shared prelude module (`prelude.py`) with 40+ functions: session management, all simulation/view commands, 7 body presets, 5 generators, steering (push/launch), display (list_bodies/status), timing (`timed` context manager), batch helpers (auto-chunking at 100), ID generation
- Automated runner (`auto_run.py`) with sequential execution, per-demo error handling, pass/fail summary
- Interactive runner (`run_all.py`) with keypress advancement between demos
- Proto stub generation script (`generate_stubs.sh`) from existing `physics_hub.proto`
- Python dependencies: grpcio, grpcio-tools, protobuf

**New Components:**
- `demos_py/prelude.py` — shared Python prelude (session, commands, presets, generators, steering, display)
- `demos_py/demo01_hello_drop.py` through `demos_py/demo15_overload.py` — 15 demos
- `demos_py/all_demos.py` — demo registry
- `demos_py/auto_run.py` — automated runner
- `demos_py/run_all.py` — interactive runner
- `demos_py/generate_stubs.sh` — proto stub generation
- `demos_py/generated/` — committed Python proto stubs

**Tasks Completed:** 29/29 tasks

---

## Stress Test Demos — 2026-03-21
**Branch:** 003-stress-test-demos
**Spec:** specs/003-stress-test-demos

**What was added:**
- 5 new stress test demos (Demos 11–15) pushing body count, collision density, bulk forces, and combined load
- Demo 11 (Body Scaling): progressive tiers of 50, 100, 200, 500 bodies with per-tier timing
- Demo 12 (Collision Pit): 120 spheres dropped into a 4x4m walled enclosure
- Demo 13 (Force Frenzy): 100 bodies hit with 3 rounds of escalating impulses, torques, and gravity shifts
- Demo 14 (Domino Cascade): 120 dominoes in a semicircular chain reaction
- Demo 15 (Overload): 200+ bodies with formations, impulse storms, gravity chaos, camera sweeps
- New `timed` Prelude.fsx helper for consistent `[TIME] label: N ms` timing output
- MCP stress testing procedure documented in quickstart.md

**Modified Components:**
- `demos/Prelude.fsx` — added `timed` helper
- `demos/AllDemos.fsx` — 15 demos (5 new stress entries)
- `demos/AutoRun.fsx` — 15 inline demos + `timed` helper in preamble
- `demos/RunAll.fsx` — dynamic demo count (no change needed)

**New Components:**
- `demos/Demo11_BodyScaling.fsx`
- `demos/Demo12_CollisionPit.fsx`
- `demos/Demo13_ForceFrenzy.fsx`
- `demos/Demo14_DominoCascade.fsx`
- `demos/Demo15_Overload.fsx`

**Tasks Completed:** 19/19 tasks

---

## Demo Script Modernization — 2026-03-21
**Branch:** 001-demo-script-modernization
**Spec:** specs/001-demo-script-modernization

**What was added:**
- Prelude helpers: `resetSimulation` (server-side reset), command builders (`makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd`), `batchAdd` (auto-split at 100), `nextId`, `toVec3`
- All 10 demos updated: `resetScene` → `resetSimulation` for server-side reset
- 6 demos converted to batch commands: Demo02 (5 marbles), Demo06 (12 boxes), Demo07 (4 bodies + 4 torques), Demo08 (5 beach balls), Demo09 (16 spheres), Demo10 (10 impulses)
- AllDemos.fsx, AutoRun.fsx, RunAll.fsx synced with all changes
- Error handling: reset fallback to manual clear, batch failure reporting with command indices

**Modified Components:**
- `demos/Prelude.fsx` — added 8 new helpers, replaced `resetScene`
- `demos/Demo01-10_*.fsx` — resetSimulation + batching where applicable
- `demos/AllDemos.fsx` — mirrored all demo changes + added Contracts import
- `demos/AutoRun.fsx` — self-contained with duplicated helpers + all demo changes
- `demos/RunAll.fsx` — cleanup line updated

**Tasks Completed:** 37/37 tasks

---

## Performance Diagnostics & Stress Testing — 2026-03-21
**Branch:** 002-performance-diagnostics
**Spec:** specs/002-performance-diagnostics

**What was added:**
- FPS overlay display in viewer (exponential moving average, 10s logging, configurable warning threshold)
- Per-service message count and data volume metrics (thread-safe Interlocked counters, 10s periodic logging)
- Batch command support at gRPC and MCP levels (simulation + view, max 100, per-command results)
- Simulation restart command (clears all bodies, resets time, metrics persist)
- Static body collision tracking (bodies tracked with `is_static` flag, included in state stream)
- Pipeline diagnostics (simulation tick, serialization, transfer timing via Stopwatch/delta measurement)
- Stress testing framework (body-scaling, command-throughput scenarios as background MCP jobs)
- MCP-vs-scripting performance comparison tool
- 6 new MCP tools: batch_commands, batch_view_commands, get_metrics, get_diagnostics, start_stress_test, get_stress_test_status, start_comparison_test

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — BatchSimulationRequest, BatchViewRequest, CommandResult, BatchResponse, MetricsRequest, ServiceMetricsReport, PipelineTimings, MetricsResponse, ResetSimulation, is_static, tick_ms, serialize_ms
- `src/PhysicsServer/Hub/MessageRouter.fsi/.fs` — Batch routing, metrics counters
- `src/PhysicsServer/Services/PhysicsHubService.fs` — Batch + metrics RPC handlers
- `src/PhysicsServer/Services/SimulationLinkService.fs` — Transfer time measurement
- `src/PhysicsSimulation/World/SimulationWorld.fs` — Static body tracking, reset, timing instrumentation
- `src/PhysicsSimulation/Client/SimulationClient.fs` — Metrics counters
- `src/PhysicsViewer/Program.fs` — FPS calculation, display, metrics logging
- `src/PhysicsSandbox.Mcp/GrpcConnection.fs` — Batch + metrics RPC calls
- `src/PhysicsSandbox.Mcp/SimulationTools.fs` — restart_simulation tool
- `src/PhysicsClient/Commands/SimulationCommands.fs` — reset, batch, metrics functions

**New Components:**
- `src/PhysicsServer/Hub/MetricsCounter.fsi/.fs` — Thread-safe service metrics
- `src/PhysicsViewer/Rendering/FpsCounter.fsi/.fs` — Smoothed FPS calculation
- `src/PhysicsSandbox.Mcp/BatchTools.fsi/.fs` — Batch MCP tools
- `src/PhysicsSandbox.Mcp/MetricsTools.fsi/.fs` — Metrics + diagnostics MCP tools
- `src/PhysicsSandbox.Mcp/StressTestTools.fsi/.fs` — Stress test MCP tools
- `src/PhysicsSandbox.Mcp/StressTestRunner.fsi/.fs` — Background stress test engine
- `src/PhysicsSandbox.Mcp/ComparisonTools.fsi/.fs` — Comparison MCP tool
- `tests/PhysicsServer.Tests/BatchRoutingTests.fs`
- `tests/PhysicsServer.Tests/MetricsCounterTests.fs`
- `tests/PhysicsSimulation.Tests/ResetSimulationTests.fs`
- `tests/PhysicsSimulation.Tests/StaticBodyTrackingTests.fs`
- `tests/PhysicsViewer.Tests/FpsCounterTests.fs`
- `tests/PhysicsSandbox.Integration.Tests/BatchIntegrationTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/RestartIntegrationTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/MetricsIntegrationTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/DiagnosticsIntegrationTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/StaticBodyTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/StressTestIntegrationTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/ComparisonIntegrationTests.cs`

**Tasks Completed:** 97/97 tasks

---

## MCP Persistent Service — 2026-03-21
**Branch:** 001-mcp-persistent-service
**Spec:** specs/001-mcp-persistent-service

**What was added:**
- MCP server switched from stdio to persistent HTTP/SSE transport (ModelContextProtocol.AspNetCore)
- New `CommandEvent` proto message + `StreamCommands` audit RPC on PhysicsServer
- GrpcConnection subscribes to 3 streams (state, view commands, command audit) with independent reconnection
- 32 total MCP tools: 10 simulation + 3 view + 2 query + 1 audit + 7 presets + 5 generators + 4 steering
- PhysicsClient referenced as library for convenience tool logic (ClientAdapter bridging layer)
- ServiceDefaults added for health checks and structured logging
- 3 new PhysicsServer unit tests (audit subscriber functions)
- 2 new integration test files (McpHttpTransportTests, CommandAuditStreamTests)

**Modified Components:**
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — CommandEvent message, StreamCommands RPC
- `src/PhysicsServer/Hub/MessageRouter.fsi/.fs` — CommandSubscribers, audit publishing
- `src/PhysicsServer/Services/PhysicsHubService.fs` — StreamCommands implementation
- `src/PhysicsSandbox.Mcp/Program.fs` — WebApplication + HTTP/SSE transport
- `src/PhysicsSandbox.Mcp/GrpcConnection.fsi/.fs` — 3 background streams, CommandLog
- `src/PhysicsSandbox.Mcp/QueryTools.fs` — 3-stream status reporting
- `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` — SDK.Web, new packages + references
- `.mcp.json` — SSE transport URL

**New Components:**
- `src/PhysicsSandbox.Mcp/AuditTools.fsi/.fs` — Command audit log query tool
- `src/PhysicsSandbox.Mcp/ClientAdapter.fsi/.fs` — GrpcConnection ↔ convenience function bridge
- `src/PhysicsSandbox.Mcp/PresetTools.fsi/.fs` — 7 body preset tools
- `src/PhysicsSandbox.Mcp/GeneratorTools.fsi/.fs` — 5 scene generator tools
- `src/PhysicsSandbox.Mcp/SteeringTools.fsi/.fs` — 4 steering tools
- `tests/PhysicsSandbox.Integration.Tests/McpHttpTransportTests.cs`
- `tests/PhysicsSandbox.Integration.Tests/CommandAuditStreamTests.cs`

**Tasks Completed:** 48/48 tasks

---

## MCP Server Aspire Orchestration — 2026-03-20
**Branch:** 006-mcp-aspire-orchestration
**Spec:** specs/006-mcp-aspire-orchestration

**What was added:**
- MCP server registered as Aspire project resource — starts/stops with AppHost, visible in dashboard
- Service discovery: MCP server resolves PhysicsServer address via Aspire environment variables (`services__server__https__0` / `services__server__http__0`)
- Standalone mode preserved: CLI arg override and hardcoded fallback still work for manual launches
- 3 new integration tests (McpOrchestrationTests: resource lifecycle, graceful shutdown, WaitFor dependency)

**Modified Components:**
- `src/PhysicsSandbox.AppHost/AppHost.cs` — Added MCP project resource with WithReference + WaitFor
- `src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj` — Added project reference
- `src/PhysicsSandbox.Mcp/Program.fs` — Env var service discovery

**New Components:**
- `tests/PhysicsSandbox.Integration.Tests/McpOrchestrationTests.cs` — 3 integration tests

**Tasks Completed:** 9/9 tasks

---

## MCP Server and Integration Testing — 2026-03-20
**Branch:** 005-mcp-server-testing
**Spec:** specs/005-mcp-server-testing

**What was added:**
- PhysicsSandbox.Mcp — F# MCP server exposing 15 tools for interactive physics debugging via AI assistants (Claude Code, etc.)
- 10 simulation tools (add_body, apply_force/impulse/torque, set_gravity, step, play, pause, remove_body, clear_forces)
- 3 view tools (set_camera, set_zoom, toggle_wireframe)
- 2 query tools (get_state with cached snapshot + staleness, get_status with connection health)
- Background StreamState subscription with exponential backoff reconnection
- Simulation SSL fix: dev certificate bypass + auto-reconnection (1s → 10s exponential backoff), preserving world state
- Viewer DISPLAY environment variable propagation from Aspire AppHost
- 27 new integration tests (32 total) across 5 test classes: SimulationConnectionTests, CommandRoutingTests, StateStreamingTests, ErrorConditionTests, ServerHubTests
- Tests exercise real physics (gravity, forces, impulses, torques) with state verification

**New Components:**
- `src/PhysicsSandbox.Mcp/` — F# MCP server (GrpcConnection, SimulationTools, ViewTools, QueryTools, Program)
- `tests/PhysicsSandbox.Integration.Tests/SimulationConnectionTests.cs` — 7 connection lifecycle tests
- `tests/PhysicsSandbox.Integration.Tests/CommandRoutingTests.cs` — 10 command routing tests
- `tests/PhysicsSandbox.Integration.Tests/StateStreamingTests.cs` — 4 state streaming tests
- `tests/PhysicsSandbox.Integration.Tests/ErrorConditionTests.cs` — 5 error condition tests

**Tasks Completed:** 29/29 tasks

---

## Client REPL Library — 2026-03-20
**Branch:** 004-client-repl
**Spec:** specs/004-client-repl

**What was added:**
- PhysicsClient library — F# REPL-friendly library for controlling physics simulation and 3D viewer via gRPC
- 9 modules with .fsi signatures: Session, SimulationCommands, ViewCommands, Presets, Generators, Steering, StateDisplay, LiveWatch, IdGenerator
- Session management: connect/disconnect/reconnect, background state caching, body registry tracking
- All 9 proto simulation commands + 3 view commands wrapped as Result-returning functions
- 7 body presets (marble, bowlingBall, beachBall, crate, brick, boulder, die) with optional position/mass/ID overrides
- Random generators (spheres, boxes, mixed) with seedable RNG + scene builders (stack, row, grid, pyramid)
- Steering functions: push (named direction), pushVec, launch (toward target), spin, stop (counter-impulse)
- Spectre.Console state display: body tables, body inspection panels, simulation status, staleness timestamps
- Cancellable live-watch mode with filtering by body ID, shape type, velocity threshold
- Thread-safe human-readable ID generation ("sphere-1", "box-3")
- FSI convenience script (PhysicsClient.fsx)
- 52 unit tests (IdGenerator, Session, SimulationCommands, Presets, Generators, Steering, StateDisplay, SurfaceArea)

**New Components:**
- `src/PhysicsClient/` — F# client library (Bodies/, Connection/, Commands/, Steering/, Display/ modules)
- `tests/PhysicsClient.Tests/` — F# unit tests
- `src/PhysicsClient/PhysicsClient.fsx` — FSI convenience script

**Tasks Completed:** 51/52 tasks

---

## 3D Viewer — 2026-03-20
**Branch:** 003-3d-viewer
**Spec:** specs/003-3d-viewer

**What was added:**
- PhysicsViewer service — F# Stride3D (Community Toolkit, code-only) 3D viewer connecting to server via gRPC
- Real-time body rendering: spheres (blue), boxes (orange), unknown (red) with position/orientation from proto
- Interactive mouse/keyboard camera: left-drag orbit, scroll zoom, middle-drag pan
- REPL camera commands: SetCamera, SetZoom override interactive input
- Wireframe toggle via ToggleWireframe command (entity recreation with flat materials)
- Simulation status overlay via DebugTextSystem (time + running/paused)
- Ground grid at Y=0 (Add3DGround + AddGroundGizmo)
- Auto-reconnect with exponential backoff (1s→30s) on connection loss
- ServiceDefaults via background host for OpenTelemetry + structured logging
- Proto extended with StreamViewCommands RPC on PhysicsHub service
- Server extended with readViewCommand function and StreamViewCommands override
- 16 viewer unit tests (SceneManager, CameraController, SurfaceArea), 3 new server tests (readViewCommand), 2 new integration tests (StreamViewCommands)

**New Components:**
- `src/PhysicsViewer/` — F# viewer service (Rendering/SceneManager, Rendering/CameraController, Streaming/ViewerClient, Program)
- `tests/PhysicsViewer.Tests/` — F# unit tests with PublicApiBaseline.txt

**Tasks Completed:** 35/35 tasks

---

## Physics Simulation Service — 2026-03-20
**Branch:** 002-physics-simulation
**Spec:** specs/002-physics-simulation

**What was added:**
- PhysicsSimulation service — F# background worker connecting to server via SimulationLink bidirectional gRPC stream
- BepuFSharp (BepuPhysics2 wrapper) integration via local NuGet package for rigid body dynamics
- Lifecycle control: play, pause, single-step at 60Hz fixed timestep
- Body management: add/remove dynamic bodies (sphere, box) and static planes
- Force system: persistent forces, one-shot impulses, torques, clear-forces, global gravity
- State streaming: complete world state (position, velocity, angular velocity, orientation) after every step
- Proto contract extensions: RemoveBody, ApplyImpulse, ApplyTorque, ClearForces commands + Body angular_velocity/orientation fields
- 37 unit tests (lifecycle, bodies, forces, gravity, edge cases, 100-body stress test, surface-area baselines)

**New Components:**
- `src/PhysicsSimulation/` — F# simulation service (World, Commands, Client modules with .fsi signatures)
- `tests/PhysicsSimulation.Tests/` — F# unit tests
- `NuGet.config` — local NuGet feed for BepuFSharp

**Tasks Completed:** 39/45 tasks (6 integration tests deferred — require Aspire containers)

---

## Contracts and Server Hub — 2026-03-20
**Branch:** 001-server-hub
**Spec:** specs/001-server-hub

**What was added:**
- Aspire AppHost orchestrator with Podman support
- Shared gRPC contracts (PhysicsHub + SimulationLink services, all message types)
- PhysicsServer hub — central message router with state caching and single-simulation enforcement
- Shared ServiceDefaults (health checks, OpenTelemetry, service discovery, resilience)
- 10 unit tests (F#) + 3 integration tests (C#, Aspire)

**New Components:**
- `src/PhysicsSandbox.AppHost/` — C# Aspire orchestrator
- `src/PhysicsSandbox.ServiceDefaults/` — C# shared infrastructure
- `src/PhysicsSandbox.Shared.Contracts/` — Proto gRPC contracts
- `src/PhysicsServer/` — F# server hub (Hub/StateCache, Hub/MessageRouter, Services/PhysicsHubService, Services/SimulationLinkService)
- `tests/PhysicsServer.Tests/` — F# unit tests
- `tests/PhysicsSandbox.Integration.Tests/` — C# integration tests

**Tasks Completed:** 34/34 tasks
