# PhysicsSandbox — Main Implementation Plan

**Last Updated**: 2026-03-23
**Revision**: Updated with 004-mesh-cache-transport archival

## Technical Context

**Language/Version**: F# on .NET 10.0 (services), C# on .NET 10.0 (AppHost, ServiceDefaults)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.2.0-beta.1 (local NuGet, 10 shape types, 10 constraint types, sweep/overlap queries, collision filtering, material properties), Grpc.Net.Client 2.x, Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages, includes Stride.BepuPhysics 4.3.0.2507 + Stride.BepuPhysics.Debug 4.3.0.2507 transitively), Spectre.Console 0.49.x (client TUI display), ModelContextProtocol 1.1.0 + ModelContextProtocol.AspNetCore 1.1.* (MCP server — HTTP/SSE transport)
**Storage**: Append-only protobuf binary files at `~/.config/PhysicsSandbox/recordings/` (recording sessions), JSON metadata per session. In-memory physics world and stateless routing otherwise.
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x, Grpc.Net.Client 2.x
**Target Platform**: Linux (rootless Podman for containers)
**Solution Format**: `.slnx` (XML-based, .NET 10 default)

## Project Structure

```text
PhysicsSandbox.slnx

src/
├── PhysicsSandbox.AppHost/              # C# — Aspire orchestrator
│   ├── AppHost.cs                       # Service registration + HTTP/2 config
│   └── Properties/launchSettings.json   # Podman runtime
│
├── PhysicsSandbox.ServiceDefaults/      # C# — shared telemetry, health, resilience
│   └── Extensions.cs                    # AddServiceDefaults() + MapDefaultEndpoints()
│
├── PhysicsSandbox.Shared.Contracts/     # Proto — shared gRPC contracts
│   └── Protos/physics_hub.proto         # PhysicsHub + SimulationLink services
│
├── PhysicsServer/                       # F# — server hub (central message router)
│   ├── Hub/
│   │   ├── StateCache.fsi/.fs           # Latest-state caching for late joiners
│   │   ├── MeshCache.fsi/.fs             # Server-side mesh geometry cache (ConcurrentDictionary)
│   │   ├── MessageRouter.fsi/.fs        # Command/state routing, subscriber mgmt, batch routing
│   │   └── MetricsCounter.fsi/.fs       # Thread-safe per-service metrics tracking (Interlocked, mesh cache counters)
│   ├── Services/
│   │   ├── PhysicsHubService.fsi/.fs    # Client/viewer-facing gRPC
│   │   └── SimulationLinkService.fsi/.fs # Simulation-facing gRPC
│   └── Program.fs                       # Host setup
│
├── PhysicsSimulation/                   # F# — physics simulation (gRPC client)
│   ├── World/
│   │   ├── MeshIdGenerator.fsi/.fs      # Content-addressed mesh ID (SHA-256) + AABB computation
│   │   ├── SimulationWorld.fsi/.fs      # BepuFSharp world wrapper, body/force management, CachedShapeRef emission
│   ├── Commands/
│   │   ├── CommandHandler.fsi/.fs       # Command dispatch (16 command types incl. constraints, shapes, queries, pose)
│   ├── Queries/
│   │   ├── QueryHandler.fsi/.fs         # Raycast, sweep cast, overlap query dispatch
│   ├── Client/
│   │   ├── SimulationClient.fsi/.fs     # Bidirectional streaming client, simulation loop
│   └── Program.fs                       # Host setup, Aspire service defaults
│
├── PhysicsViewer/                       # F# — 3D viewer (Stride3D + gRPC client)
│   ├── Rendering/
│   │   ├── SceneManager.fsi/.fs         # SimulationState → Stride entities, per-body color, wireframe
│   │   ├── ShapeGeometry.fsi/.fs        # Primitive type selection, bounding size, default color palette
│   │   ├── DebugRenderer.fsi/.fs        # Wireframe overlay, constraint line visualization, F3 toggle
│   │   ├── CameraController.fsi/.fs     # Camera state, input, REPL commands
│   │   └── FpsCounter.fsi/.fs           # Smoothed FPS calculation, logging, threshold warnings
│   ├── Settings/
│   │   ├── ViewerSettings.fsi/.fs       # Settings model, JSON persistence (~/.config/PhysicsSandbox/)
│   │   ├── DisplayManager.fsi/.fs       # Fullscreen/resolution/quality Stride API wrapper
│   │   └── SettingsOverlay.fsi/.fs      # F2 text-based settings UI (DebugTextSystem)
│   ├── Streaming/
│   │   ├── MeshResolver.fsi/.fs         # Local mesh cache + async FetchMeshes client
│   │   └── ViewerClient.fsi/.fs         # gRPC streaming client with auto-reconnect
│   └── Program.fs                       # Host + Stride game loop + F11/F2/Escape input
│
└── PhysicsClient/                       # F# — REPL client library (gRPC client, Spectre.Console)
    ├── Bodies/
    │   ├── IdGenerator.fsi/.fs          # Thread-safe human-readable ID generation
    │   ├── Presets.fsi/.fs              # 7 body presets (marble, bowlingBall, crate, etc.)
    │   └── Generators.fsi/.fs           # Random generators + scene builders
    ├── Connection/
    │   ├── MeshResolver.fsi/.fs         # Local mesh cache + sync FetchMeshes client
    │   └── Session.fsi/.fs              # gRPC connection, state caching, body registry, MeshResolver
    ├── Commands/
    │   ├── SimulationCommands.fsi/.fs   # All simulation command wrappers
    │   └── ViewCommands.fsi/.fs         # Camera, zoom, wireframe wrappers
    ├── Steering/
    │   └── Steering.fsi/.fs             # Push, launch, spin, stop + Direction DU
    ├── Display/
    │   ├── StateDisplay.fsi/.fs         # Spectre.Console tables, panels, staleness
    │   └── LiveWatch.fsi/.fs            # Cancellable live state feed with filters
    ├── Program.fs                       # Aspire entry point
    └── PhysicsClient.fsx                # FSI convenience script
│
├── PhysicsSandbox.Scripting/           # F# — scripting convenience library (wraps PhysicsClient)
│   ├── Helpers.fsi/.fs                # ok, sleep, timed
│   ├── Vec3Builders.fsi/.fs           # toVec3, toTuple
│   ├── CommandBuilders.fsi/.fs        # makeSphereCmd, makeBoxCmd, makeCapsuleCmd, makeCylinderCmd, makeImpulseCmd, makeTorqueCmd, makeColor, makeMaterialProperties, makeSetBodyPoseCmd
│   ├── ConstraintBuilders.fsi/.fs    # makeBallSocketCmd, makeHingeCmd, makeWeldCmd, makeDistanceLimitCmd, makeRemoveConstraintCmd
│   ├── QueryBuilders.fsi/.fs         # raycast, raycastAll, sweepSphere, overlapSphere
│   ├── BatchOperations.fsi/.fs        # batchAdd (auto-chunking at 100)
│   ├── SimulationLifecycle.fsi/.fs    # resetSimulation, runFor, nextId
│   └── Prelude.fsi/.fs               # [<AutoOpen>] re-export of all functions
│
└── PhysicsSandbox.Mcp/                 # F# — MCP server (persistent HTTP/SSE, 58 tools)
    ├── MeshResolver.fsi/.fs            # Local mesh cache + sync FetchMeshes client
    ├── GrpcConnection.fsi/.fs          # gRPC channel + 3 background streams (state, view, audit) + batch/metrics RPCs
    ├── SimulationTools.fsi/.fs         # 17 simulation/query MCP tools (incl. constraints, shapes, queries, collision filter, body pose)
    ├── ViewTools.fsi/.fs               # 3 view command MCP tools
    ├── QueryTools.fsi/.fs              # get_state, get_status MCP tools
    ├── AuditTools.fsi/.fs              # Command audit log query tool
    ├── ClientAdapter.fsi/.fs           # Adapter bridging GrpcConnection with convenience functions
    ├── PresetTools.fsi/.fs             # 7 body preset MCP tools
    ├── GeneratorTools.fsi/.fs          # 5 scene generator MCP tools
    ├── SteeringTools.fsi/.fs           # 4 steering MCP tools
    ├── BatchTools.fsi/.fs              # batch_commands, batch_view_commands MCP tools
    ├── MetricsTools.fsi/.fs            # get_metrics, get_diagnostics MCP tools
    ├── StressTestTools.fsi/.fs         # start_stress_test, get_stress_test_status MCP tools
    ├── StressTestRunner.fsi/.fs        # Background stress test execution engine
    ├── ComparisonTools.fsi/.fs         # start_comparison_test MCP tool
    ├── Recording/
    │   ├── Types.fsi/.fs                 # LogEntry, SessionStatus, PaginationCursor, wire format
    │   ├── SessionStore.fsi/.fs          # Session metadata CRUD (JSON persistence)
    │   ├── ChunkWriter.fsi/.fs           # Async Channel-based binary writer with pruning
    │   ├── ChunkReader.fsi/.fs           # Binary reader with pagination cursors
    │   └── RecordingEngine.fsi/.fs       # Recording lifecycle orchestration, auto-start
    ├── RecordingTools.fsi/.fs            # 5 session management MCP tools
    ├── RecordingQueryTools.fsi/.fs       # 4 query MCP tools
    └── Program.fs                      # WebApplication + HTTP/SSE MCP transport

tests/
├── PhysicsSandbox.Integration.Tests/    # C# — Aspire end-to-end tests (42 tests)
│   ├── ServerHubTests.cs               # 6 tests (SendCommand, StreamState, SendViewCommand, StreamViewCommands)
│   ├── SimulationConnectionTests.cs    # 7 tests (connection lifecycle, physics verification, 30s stability)
│   ├── CommandRoutingTests.cs          # 10 tests (all 9 command types + ClearForces end-to-end)
│   ├── StateStreamingTests.cs          # 4 tests (concurrent subscribers, late-joiner, view command forwarding)
│   ├── ErrorConditionTests.cs          # 5 tests (no simulation, empty command, rapid stress)
│   ├── McpOrchestrationTests.cs       # 3 tests (MCP resource lifecycle in Aspire)
│   ├── BatchIntegrationTests.cs       # Batch RPC end-to-end tests
│   ├── RestartIntegrationTests.cs     # Restart command end-to-end tests
│   ├── MetricsIntegrationTests.cs     # Metrics collection end-to-end tests
│   ├── DiagnosticsIntegrationTests.cs # Pipeline diagnostics end-to-end tests
│   ├── StaticBodyTests.cs             # Static body collision verification
│   ├── StressTestIntegrationTests.cs  # Stress test MCP tool end-to-end tests
│   ├── MeshCacheIntegrationTests.cs   # End-to-end mesh caching: CachedShapeRef + FetchMeshes + late-joiner
│   ├── ComparisonIntegrationTests.cs  # MCP-vs-scripting comparison tests
│   └── xunit.runner.json              # Test runner configuration
├── PhysicsServer.Tests/                 # F# — unit tests (35 tests)
│   ├── StateCacheTests.fs
│   ├── MessageRouterTests.fs            # Includes readViewCommand tests
│   ├── BatchRoutingTests.fs             # Batch command routing tests
│   ├── MetricsCounterTests.fs           # MetricsCounter unit tests
│   └── PublicApiBaseline.txt            # Surface-area baseline
├── PhysicsSimulation.Tests/             # F# — unit tests (103 tests)
│   ├── SimulationWorldTests.fs          # Lifecycle, bodies, forces, gravity, stress
│   ├── CommandHandlerTests.fs           # Command dispatch, edge cases
│   ├── ResetSimulationTests.fs          # Reset/restart command tests
│   ├── StaticBodyTrackingTests.fs       # Static body state tracking tests
│   └── SurfaceAreaTests.fs              # Public API baseline verification
├── PhysicsViewer.Tests/                 # F# — unit tests (56 tests)
│   ├── SceneManagerTests.fs             # Shape classification, state accessors
│   ├── CameraControllerTests.fs         # Camera math, command application
│   ├── FpsCounterTests.fs               # FPS calculation, logging interval, threshold tests
│   ├── ViewerSettingsTests.fs            # Settings persistence round-trip tests
│   ├── DisplayManagerTests.fs           # Display manager logic tests
│   ├── SurfaceAreaTests.fs              # Public API baseline (8 modules incl. Settings)
│   └── PublicApiBaseline.txt            # Surface-area baseline
├── PhysicsSandbox.Scripting.Tests/     # F# — unit + surface area tests (20 tests)
│   ├── HelpersTests.fs
│   ├── Vec3BuildersTests.fs
│   ├── CommandBuildersTests.fs
│   ├── SurfaceAreaTests.fs
│   └── SurfaceAreaBaseline.txt
├── PhysicsClient.Tests/                 # F# — unit tests (56 tests)
│   ├── IdGeneratorTests.fs              # Sequential IDs, reset, thread safety
│   ├── SessionTests.fs                  # Connection lifecycle
│   ├── SimulationCommandsTests.fs       # Proto message construction, Vec3 conversion
│   ├── PresetsTests.fs                  # Preset parameters, mass values
│   ├── GeneratorsTests.fs               # Scene builder validation, count checks
│   ├── SteeringTests.fs                 # Direction-to-Vec3 mapping
│   ├── StateDisplayTests.fs             # Vec3 formatting, velocity magnitude, shapes
│   └── SurfaceAreaTests.fs              # Public API baseline for all 9 modules
├── PhysicsSandbox.Mcp.Tests/            # F# — unit tests (13 tests)
│   ├── ChunkWriterTests.fs
│   ├── ChunkReaderTests.fs
│   ├── SessionStoreTests.fs
│   └── RecordingEngineTests.fs

Scripting/                                   # All scripting folders consolidated
├── scratch/                                 # Gitignored experimentation folder (.gitkeep only)
├── scripts/                                 # Curated F# scripts using PhysicsSandbox.Scripting library
│   ├── Prelude.fsx                          # Single #r to Scripting DLL + opens
│   └── HelloDrop.fsx                        # Minimal validation script
├── demos/                                   # F# scripts — demo suite (18 demos + runners)
├── Prelude.fsx                            # Shared helpers: command builders (sphere, box, capsule, cylinder, triangle, convex hull, compound, kinematic), color palette (8 constants), material presets, constraint helpers (ball-socket, hinge), query helpers (raycast, overlap, sweep), setPose, batchAdd, resetSimulation, runFor, nextId, toVec3, timed, runStandalone + all PhysicsClient 0.2.0 opens
├── 01_HelloDrop.fsx                       # 6 shapes (sphere, box, capsule, cylinder) — comparative fall + bouncy/sticky materials
├── 02_BouncingMarbles.fsx                 # 25 marbles in 2 color-coded waves (yellow/green)
├── 03_CrateStack.fsx                      # 12 blue crates — red boulder strikes tower center via launch
├── 04_BowlingAlley.fsx                    # 4-layer pyramid — red ball frontal Z-axis impact
├── 05_MarbleRain.fsx                      # 50 mixed shapes (spheres, crates, capsules, cylinders, dice) — color-coded
├── 06_DominoRow.fsx                       # 20 dominoes with blue→purple color gradient
├── 07_SpinningTops.fsx                    # 6 spinning objects (spheres, capsules, cylinders) in colored ring
├── 08_GravityFlip.fsx                     # Mixed shapes incl. triangles + convex hull octahedra — 4 gravity directions
├── 09_Billiards.fsx                       # 16 colored balls with slippery material + red cue ball break
├── 10_ChaosScene.fsx                      # Full showcase with cylinder pillars, colored formations, fixed boulder targeting
├── 11_BodyScaling.fsx                     # Stress: 5 shape types (sphere, box, capsule, cylinder, compound dumbbell), 50–500 tiers
├── 12_CollisionPit.fsx                    # Stress: 3 colored waves + convex hull tetrahedra + compound bodies
├── 13_ForceFrenzy.fsx                     # Stress: 80 bodies — bouncy (yellow) vs sticky (purple) material contrast
├── 14_DominoCascade.fsx                   # Stress: 120 dominoes with blue→red color gradient along semicircle
├── 15_Overload.fsx                        # Stress: 200+ mixed shapes (spheres, capsules, cylinders) with colors
├── 16_Constraints.fsx                     # NEW: pendulum chain (ball-socket + distance-limit), hinged bridge, weld cluster — 4 constraint types
├── 17_QueryRange.fsx                      # NEW: raycast, overlap sphere, sweep sphere queries with printed results
├── 18_KinematicSweep.fsx                  # NEW: kinematic bulldozer plows through 30 dynamic bodies
├── AllDemos.fsx                           # All 18 demos as inline functions (loaded by RunAll + AutoRun)
├── AutoRun.fsx                            # Non-interactive runner (loads AllDemos.fsx — no code duplication)
└── RunAll.fsx                             # Interactive runner (space/enter to advance)

└── demos_py/                                # Python scripts — demo suite (18 demos + runners)
├── prelude.py                               # Shared helpers: session, commands, presets, generators, steering, display, batch, ID gen, color palette, advanced shape builders (triangle, convex hull, compound, kinematic), constraint helpers, query helpers, set_body_pose
├── demo01_hello_drop.py – demo18_kinematic_sweep.py # 18 demos mirroring F# suite
├── all_demos.py                             # Demo registry (name, description, run) tuples
├── auto_run.py                              # Automated runner with pass/fail summary
├── run_all.py                               # Interactive runner with keypress advancement
├── generate_stubs.sh                        # Proto stub generation from physics_hub.proto
├── requirements.txt                         # Python dependencies (grpcio, grpcio-tools, protobuf)
└── generated/                               # Python proto stubs (committed for convenience)
    ├── physics_hub_pb2.py                   # Generated message classes
    └── physics_hub_pb2_grpc.py              # Generated service stubs
```

## Configuration

- `ASPIRE_CONTAINER_RUNTIME=podman` — set in AppHost launchSettings.json
- `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` — set by AppHost on server resource for gRPC
- `NuGet.config` — local feed at `~/.local/share/nuget-local/` for BepuFSharp, PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, PhysicsClient, and PhysicsSandbox.Scripting packages
- Simulation connects to server via Aspire service discovery (`services__server__https__0` env var)
- Viewer connects to server via same Aspire service discovery env vars
- Client connects to server via same Aspire service discovery env vars (fallback: `http://localhost:5180`)
- MCP server connects to PhysicsServer via Aspire service discovery (`services__server__https__0` / `services__server__http__0` env vars); falls back to CLI arg or `https://localhost:7180` for standalone use
- Viewer settings persisted at `~/.config/PhysicsSandbox/viewer-settings.json` (resolution, fullscreen, AA, shadows, texture filtering, VSync)
- Stride3D uses OpenGL graphics API (`<StrideGraphicsApi>OpenGL</StrideGraphicsApi>`) for container/GPU-passthrough compatibility
- Stride asset compiler disabled by default (`StrideCompilerSkipBuild`); builds without it for CI, enable for live runs with GPU
- Recording sessions persist at `~/.config/PhysicsSandbox/recordings/{session-guid}/` (session.json metadata + chunk-*.bin data files)

## Engineering Exceptions

| Exception | Justification |
|-----------|---------------|
| AppHost in C# | No F# Aspire AppHost templates. ~30 lines boilerplate, no domain logic. |
| ServiceDefaults in C# | Standard Aspire template. Extension methods only. |
| Integration tests in C# | Aspire DistributedApplicationTestingBuilder has better C# support. |

## Future Services (Planned)

All five services (Server, Simulation, Viewer, Client, MCP) are now Aspire-managed project resources. No planned future services.

## Known Issues & Gotchas

- gRPC requires HTTP/2; plain HTTP endpoints need `Http1AndHttp2` protocol config. [Source: specs/001-server-hub]
- F# projects must use `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation conflicts. [Source: specs/001-server-hub]
- Integration tests must connect via HTTPS endpoint with dev cert validation bypass (`RemoteCertificateValidationCallback = (_, _, _, _) => true`). [Source: specs/001-server-hub]
- Solution file is `.slnx` (XML format) not `.sln` — .NET 10 default. [Source: specs/001-server-hub]
- BepuFSharp must be packed with `-p:NoWarn=NU5104` due to prerelease BepuPhysics2 dependency. [Source: specs/002-physics-simulation]
- Proto `Sphere`/`Box` type names conflict with BepuFSharp shapes in F#; use type aliases (`ProtoSphere`, `ProtoBox`) to disambiguate. [Source: specs/002-physics-simulation]
- Simulation is a Worker service (not Web), acts as gRPC client — no Kestrel config needed on the simulation project itself. [Source: specs/002-physics-simulation]
- Plane bodies are approximated as large static boxes (BepuPhysics2 has no infinite plane). Statics are not tracked in state stream. [Source: specs/002-physics-simulation]
- Stride3D `Vector3` uses `inref<>` operator overloads that don't work with F# `+`/`*`; use `Vector3.Add(&a, &b, &result)` or helper functions. Same for `Vector3.Cross`. [Source: specs/003-3d-viewer]
- Stride's `Game.Run()` blocks the main thread; gRPC streams run on background tasks with `ConcurrentQueue<T>` bridging. [Source: specs/003-3d-viewer]
- Stride's `Add3DCameraController()` conflicts with custom CameraController — do not use both. [Source: specs/003-3d-viewer]
- Viewer needs `openal`, `freetype2`, `sdl2`, `ttf-liberation` system packages and `freeimage.so` symlink on Linux. [Source: specs/003-3d-viewer]
- Viewer uses `DebugTextSystem.Print` for status overlay (no font assets needed). [Source: specs/003-3d-viewer]
- Client library uses `OutputType=Exe` for Aspire orchestration but is also FSI-loadable via `#r` on the compiled DLL. [Source: specs/004-client-repl]
- Client gRPC channels are lazy — `GrpcChannel.ForAddress` succeeds immediately; failures surface on first RPC call. [Source: specs/004-client-repl]
- Client IdGenerator uses ConcurrentDictionary.AddOrUpdate which may invoke the update delegate multiple times under contention — but always produces correct results. Use unique shape keys in tests to avoid cross-test interference. [Source: specs/004-client-repl]
- Spectre.Console Live context with Ctrl+C cancellation: must set `args.Cancel = true` in CancelKeyPress handler to prevent process termination. [Source: specs/004-client-repl]
- ~~MCP server must log to stderr only — stdout is the stdio MCP transport.~~ Superseded: MCP now uses HTTP/SSE transport; logging goes through standard ASP.NET Core logging pipeline with ServiceDefaults. [Source: specs/005-mcp-server-testing → specs/001-mcp-persistent-service]
- MCP `[<McpServerToolType>]` requires static methods on types — F# types with static members compile correctly for SDK discovery via `WithToolsFromAssembly()`. [Source: specs/005-mcp-server-testing]
- Simulation SSL: must use SocketsHttpHandler with `RemoteCertificateValidationCallback` returning true (same as PhysicsClient and integration tests). Without this, bidirectional stream fails silently on HTTPS. [Source: specs/005-mcp-server-testing]
- Simulation reconnection: exponential backoff (1s → 10s max) preserves BepuPhysics world across stream reconnections. Only exits on CancellationToken cancellation. [Source: specs/005-mcp-server-testing]
- Viewer DISPLAY env: Aspire doesn't propagate DISPLAY automatically; must add `.WithEnvironment("DISPLAY", ...)` in AppHost. Fallback to `:0`. [Source: specs/005-mcp-server-testing]
- Integration tests need `xunit.runner.json` with `"diagnosticMessages": true` for timeout debugging. Tests use 30s+ timeouts for simulation stability verification. [Source: specs/005-mcp-server-testing]
- Plane bodies are now tracked in `world.Bodies` with `IsStatic = true`; previously they were added to BepuPhysics2 but invisible in state. [Source: specs/002-performance-diagnostics]
- Batch commands limited to 100 per request. Server enforces this in `sendBatchCommand`/`sendBatchViewCommand`. [Source: specs/002-performance-diagnostics]
- Stress tests run in MCP server process as background tasks; only one test at a time (guarded by lock). Results stored in-memory, lost on MCP restart. [Source: specs/002-performance-diagnostics]
- Pipeline diagnostics: viewer render time (`ViewerRenderMs`) not yet populated — remains 0.0. Simulation tick, serialization, and transfer times are measured correctly. [Source: specs/002-performance-diagnostics]
- PhysicsClient NuGet repacked to 0.2.0 (from 0.1.0) to expose raycast, sweepCast, overlap, setBodyPose APIs to demo scripts. Prelude.fsx pins `#r "nuget: PhysicsClient, 0.2.0"`. ServiceDefaults and Contracts also repacked to 0.2.0 as transitive dependencies. [Source: specs/005-enhance-demos]
- Demo Prelude weld/distance-limit constraints are built inline (not from Prelude helpers) because only ball-socket and hinge are in the Prelude constraint section. Promoting all constraint builders to Prelude is a future improvement. [Source: specs/005-enhance-demos]
- Recording data stored at `~/.config/PhysicsSandbox/recordings/` with one subdirectory per session containing `session.json` + `chunk-*.bin` files. Follows same XDG convention as viewer settings. [Source: specs/005-mcp-data-logging]
- ChunkWriter uses bounded Channel<LogEntry> with DropOldest (capacity 10,000). Under extreme load, oldest entries may be dropped before reaching disk. Non-blocking design ensures stream callbacks never stall. [Source: specs/005-mcp-data-logging]
- Recording auto-starts on first SimulationState received (FR-210). If manually stopped, does NOT auto-restart — requires explicit start_recording tool call. [Source: specs/005-mcp-data-logging]
- Restart recovery: on MCP server startup, any session with Status=Recording is marked Completed (was interrupted). Data is preserved for querying. [Source: specs/005-mcp-data-logging]
- Proto `CachedShapeRef` type added to Shape oneof (field 11). Use type alias `ProtoCachedShapeRef = PhysicsSandbox.Shared.Contracts.CachedShapeRef` in F# files that also use BepuFSharp types. [Source: specs/004-mesh-cache-transport]
- Mesh IDs are SHA-256 hashes truncated to 128 bits (32 hex chars). Protobuf serialization is deterministic for proto3 (no map fields in shape messages). [Source: specs/004-mesh-cache-transport]
- Body removal does NOT evict mesh cache entries — orphaned IDs persist until reset/disconnect. Content-addressed IDs prevent correctness issues. [Source: specs/004-mesh-cache-transport]
- Compound shapes are cached as atomic units (single hash of entire Compound proto). Children are not independently identified. [Source: specs/004-mesh-cache-transport]
- Viewer MeshResolver uses `Async.Start` for non-blocking fetch. Client/MCP use synchronous fetch (blocking acceptable for text/request-response). [Source: specs/004-mesh-cache-transport]
- Demo Prelude.fsx references PhysicsClient NuGet 0.2.0 which does not include CachedShapeRef/MeshGeometry/FetchMeshes types. The `resolveShape` helper cannot be added until PhysicsClient is repacked. [Source: specs/004-mesh-cache-transport]
