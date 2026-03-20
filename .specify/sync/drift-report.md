# Spec Drift Report

Generated: 2026-03-20
Project: PhysicsSandbox (full project)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 5 |
| Requirements Checked | 76 |
| ✓ Aligned | 76 (100%) |
| ⚠️ Drifted | 0 (0%) |
| ✗ Not Implemented | 0 (0%) |
| 🆕 Unspecced Code | 2 |

## Detailed Findings

### Spec: 001-server-hub — Contracts and Server Hub

#### Aligned ✓ (14/14)
- FR-001: Solution structure with AppHost, contracts, service defaults, server hub → `src/PhysicsSandbox.AppHost/AppHost.cs`, `src/PhysicsSandbox.ServiceDefaults/`, `src/PhysicsSandbox.Shared.Contracts/`, `src/PhysicsServer/`
- FR-002: PhysicsHub + SimulationLink services with all RPCs → `Protos/physics_hub.proto:7-34`
- FR-003: SimulationCommand with add body, apply force, set gravity, step, play/pause → `physics_hub.proto:38-49`
- FR-004: ViewCommand with set camera, toggle wireframe, set zoom → `physics_hub.proto:77-82`
- FR-005: SimulationState with bodies, time, running flag → `physics_hub.proto:99-113`
- FR-006: Server accepts and forwards simulation commands → `PhysicsHubService.fs:11-13`, `MessageRouter.fs:29-45`
- FR-007: Server accepts and forwards view commands → `PhysicsHubService.fs:15-17`, `MessageRouter.fs:47-54`
- FR-008: Server fans out state to all subscribers → `MessageRouter.fs:64-73`, `PhysicsHubService.fs:19-44`
- FR-009: Graceful handling when no downstream connected → `MessageRouter.fs:33-36` (returns dropped message, no error)
- FR-010: AppHost registers server → `AppHost.cs:3`
- FR-011: Service defaults with health checks, logging, tracing → `Extensions.cs:21-107`
- FR-012: Server references service defaults → `PhysicsServer/Program.fs:8`, `PhysicsServer.fsproj:26`
- FR-013: Server caches most recent state for late joiners → `Hub/StateCache.fs`, `PhysicsHubService.fs:24-26`
- FR-014: Single simulation source enforcement → `MessageRouter.fs:78-84`, `SimulationLinkService.fs:17-18` (AlreadyExists rejection)

---

### Spec: 002-physics-simulation — Physics Simulation Service

#### Aligned ✓ (20/20)
- FR-001: Simulation connects to server via simulation link → `SimulationClient.fs:29-31, 106-136`
- FR-002: Starts in paused state → `SimulationWorld.fs:88` (Running = false)
- FR-003: Play, pause, single-step commands → `CommandHandler.fs:8-14`
- FR-004: Fixed timestep advancement with state streaming → `SimulationWorld.fs:89` (60Hz), `SimulationClient.fs:75-79`
- FR-005: Add rigid bodies with position, velocity, mass, shape → `SimulationWorld.fs:110-162`
- FR-006: Unique body identifiers → `SimulationWorld.fs:111-112`
- FR-007: Remove bodies by identifier → `SimulationWorld.fs:164-172`
- FR-008: Persistent forces → `SimulationWorld.fs:174-179, 66-79`
- FR-009: One-shot impulses → `SimulationWorld.fs:183-189`
- FR-010: Torques → `SimulationWorld.fs:191-197`
- FR-011: Global gravity vector → `SimulationWorld.fs:203-204`
- FR-012: Stream state after every step → `SimulationClient.fs:36, 71-72, 76-77`
- FR-013: State includes all dynamic body fields → `SimulationWorld.fs:45-56`
- FR-014: State includes simulation time and running flag → `SimulationWorld.fs:58-64`
- FR-015: Graceful handling of non-existent body targets → `SimulationWorld.fs:175-181, 184-189, 192-197`
- FR-016: Server disconnection → clean shutdown with logging → `SimulationClient.fs:84-91, 120-131`
- FR-017: Reject zero or negative mass → `SimulationWorld.fs:113-114`
- FR-018: Registered with Aspire orchestrator → `AppHost.cs:6-8`
- FR-019: Extend contracts preserving backward compat → `physics_hub.proto:38-49` (oneof strategy)
- FR-020: Clear-forces command → `SimulationWorld.fs:199-201`, `CommandHandler.fs:25-26`

---

### Spec: 003-3d-viewer — 3D Viewer

#### Aligned ✓ (16/16)
- FR-001: Connect to server and subscribe to state stream → `ViewerClient.fs:39-50`, `Program.fs:70-78`
- FR-002: Render bodies as 3D shapes → `SceneManager.fs:40-44` (Sphere→Sphere, Box→Cube, Unknown→Sphere fallback)
- FR-003: Position and orient bodies → `SceneManager.fs:46-52, 65-66`
- FR-004: Update scene on each new state → `Program.fs:90-102`, `SceneManager.fs:74-101`
- FR-005: Apply SetCamera commands → `CameraController.fs:24-30, 92-113`
- FR-006: Apply SetZoom commands → `CameraController.fs:32-33`
- FR-007: Apply ToggleWireframe commands → `SceneManager.fs:103-109`
- FR-008: Display simulation time → `Program.fs:124-130`
- FR-009: Running/paused indicator → `Program.fs:126-129`
- FR-010: Default camera position → `CameraController.fs:14-18` (position 10,8,10 target 0,0,0)
- FR-011: Late-join graceful handling → server sends cached state, `SceneManager.applyState` is idempotent
- FR-012: Shape-type color differentiation → `SceneManager.fs:34-38` (Sphere→Blue, Box→Orange, Unknown→Red)
- FR-013: Aspire orchestrator registration → `Program.fs:36-40`, `AppHost.cs:10-13`
- FR-014: Interactive mouse/keyboard camera → `CameraController.fs:42-90` (orbit, zoom, pan)
- FR-015: Interactive + REPL camera coexistence → `Program.fs:103-117` (interactive first, REPL overrides)
- FR-016: Ground reference grid at Y=0 → `Program.fs:48, 51-54` (Add3DGround + AddGroundGizmo)

---

### Spec: 004-client-repl — Client REPL Library

#### Aligned ✓ (13/13)
- FR-001: Connect function returning session handle → `Session.connect` returns `Result<Session, string>`
- FR-002: All simulation commands (12 functions) → `SimulationCommands.fs`
- FR-002a: Clear-all function → `SimulationCommands.fs:83-98` (removes all tracked bodies)
- FR-003: 7 presets with auto-ID and optional `id` override → `Presets.fs` (marble, bowlingBall, beachBall, crate, brick, boulder, die — all accept `id: string option`)
- FR-004: Randomized body generation → `Generators.fs` (randomSpheres, randomBoxes, randomBodies with optional seed)
- FR-005: 4 scene builders → `Generators.fs` (stack, row, grid, pyramid)
- FR-006: Steering functions → `Steering.fs` (push, pushVec, launch, spin, stop)
- FR-007: State query functions → `StateDisplay.fs` (listBodies, inspect, status, snapshot)
- FR-008: Cancellable live-watch with filtering → `LiveWatch.fs` (body ID, shape, velocity threshold filters)
- FR-009: Viewer control → `ViewCommands.fs` (setCamera, setZoom, wireframe)
- FR-010: FSI-loadable library → `.fsi` signature files + `PhysicsClient.fsx` convenience script
- FR-011: Result-based error handling → all public functions return `Result<_,string>`
- FR-012: Formatted terminal output → Spectre.Console tables, panels, aligned columns

---

### Spec: 005-mcp-server-testing — MCP Server and Integration Testing

#### Aligned ✓ (12/12)
- FR-001: 15 individual MCP tools → `SimulationTools.fs` (10), `ViewTools.fs` (3), `QueryTools.fs` (2)
- FR-002: Cached state with timestamp from background stream → `GrpcConnection.LatestState` + `LastUpdateTime`
- FR-003: Structured parameters, human-readable results → gRPC-matching schemas, "Success/Failed/Error" format
- FR-004: Stdio transport → `Program.fs` with `.WithStdioServerTransport()`
- FR-005: Graceful connection error handling → try-catch in `sendCmd`/`sendView` helpers
- FR-006: Simulation HTTPS + auto-reconnect with exponential backoff → `SimulationClient.fs:15-23` (SSL bypass), `SimulationClient.fs:110-135` (1s→10s backoff)
- FR-007: Viewer DISPLAY env variable → `AppHost.cs:13` with `:0` fallback
- FR-008: Integration tests covering all PhysicsHub RPCs → 32 tests across 5 classes
- FR-009: Tests with real physics data → `CommandRoutingTests.cs` verifies force/impulse/torque/gravity effects
- FR-010: Error condition tests → `ErrorConditionTests.cs` (no simulation, empty command, rapid stress)
- FR-011: Concurrent subscriber tests → `StateStreamingTests.cs` (3 concurrent subscribers, late-joiner)
- FR-012: Headless-compatible → no GPU/display dependencies, gRPC-only communication

---

## Unspecced Code 🆕

| Feature | Location | Description | Suggested Spec |
|---------|----------|-------------|----------------|
| Demo Scripts | `demos/` (14 files) | 10 FSX demo scripts (HelloDrop through Chaos) + Prelude.fsx, AllDemos.fsx, AutoRun.fsx, RunAll.fsx | 006-demos |
| Error Report | `2026-03-20-Error-Report.md` | Post-mortem documenting 7 problems found during demo execution; fixes applied in spec 005 | N/A (artifact) |

## Inter-Spec Conflicts

None detected. All five specs build on each other in a clean dependency chain.

## Corrections from Previous Report

- **004 FR-003 drift resolved**: Previous report flagged Presets as missing `?id` parameter. Code inspection confirms all 7 preset functions accept `id: string option` as a parameter (e.g., `Presets.fs:6`). The drift was a false positive.
- **004 FR-010 drift resolved**: Previous report flagged missing `.fsx` script. `PhysicsClient.fsx` now exists.

## Recommendations

1. **Consider specifying demos**: The `demos/` directory contains 14 files of unspecced code. A backfill spec (006-demos) would document the demo catalogue and usage patterns.
2. **Archive error report**: `2026-03-20-Error-Report.md` documents problems fixed in spec 005. Consider moving to `docs/` or project memory.
3. **Update CLAUDE.md test count**: Says "20+ integration tests" but actual count is 32.
