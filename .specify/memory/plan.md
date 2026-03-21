# PhysicsSandbox — Main Implementation Plan

**Last Updated**: 2026-03-21
**Revision**: Updated with 002-performance-diagnostics archival

## Technical Context

**Language/Version**: F# on .NET 10.0 (services), C# on .NET 10.0 (AppHost, ServiceDefaults)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.1.0 (local NuGet), Grpc.Net.Client 2.x, Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages), Spectre.Console 0.49.x (client TUI display), ModelContextProtocol 1.1.0 + ModelContextProtocol.AspNetCore 1.1.* (MCP server — HTTP/SSE transport)
**Storage**: N/A (in-memory physics world, stateless message routing, in-memory metrics counters/stress test state/command logs)
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
│   │   ├── MessageRouter.fsi/.fs        # Command/state routing, subscriber mgmt, batch routing
│   │   └── MetricsCounter.fsi/.fs       # Thread-safe per-service metrics tracking (Interlocked)
│   ├── Services/
│   │   ├── PhysicsHubService.fsi/.fs    # Client/viewer-facing gRPC
│   │   └── SimulationLinkService.fsi/.fs # Simulation-facing gRPC
│   └── Program.fs                       # Host setup
│
├── PhysicsSimulation/                   # F# — physics simulation (gRPC client)
│   ├── World/
│   │   ├── SimulationWorld.fsi/.fs      # BepuFSharp world wrapper, body/force management
│   ├── Commands/
│   │   ├── CommandHandler.fsi/.fs       # Command dispatch (9 command types)
│   ├── Client/
│   │   ├── SimulationClient.fsi/.fs     # Bidirectional streaming client, simulation loop
│   └── Program.fs                       # Host setup, Aspire service defaults
│
├── PhysicsViewer/                       # F# — 3D viewer (Stride3D + gRPC client)
│   ├── Rendering/
│   │   ├── SceneManager.fsi/.fs         # SimulationState → Stride entities, wireframe
│   │   ├── CameraController.fsi/.fs     # Camera state, input, REPL commands
│   │   └── FpsCounter.fsi/.fs           # Smoothed FPS calculation, logging, threshold warnings
│   ├── Streaming/
│   │   └── ViewerClient.fsi/.fs         # gRPC streaming client with auto-reconnect
│   └── Program.fs                       # Host + Stride game loop
│
└── PhysicsClient/                       # F# — REPL client library (gRPC client, Spectre.Console)
    ├── Bodies/
    │   ├── IdGenerator.fsi/.fs          # Thread-safe human-readable ID generation
    │   ├── Presets.fsi/.fs              # 7 body presets (marble, bowlingBall, crate, etc.)
    │   └── Generators.fsi/.fs           # Random generators + scene builders
    ├── Connection/
    │   └── Session.fsi/.fs              # gRPC connection, state caching, body registry
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
└── PhysicsSandbox.Mcp/                 # F# — MCP server (persistent HTTP/SSE, 38 tools)
    ├── GrpcConnection.fsi/.fs          # gRPC channel + 3 background streams (state, view, audit) + batch/metrics RPCs
    ├── SimulationTools.fsi/.fs         # 11 simulation command MCP tools (incl. restart_simulation)
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
│   ├── ComparisonIntegrationTests.cs  # MCP-vs-scripting comparison tests
│   └── xunit.runner.json              # Test runner configuration
├── PhysicsServer.Tests/                 # F# — unit tests (18 tests)
│   ├── StateCacheTests.fs
│   ├── MessageRouterTests.fs            # Includes readViewCommand tests
│   ├── BatchRoutingTests.fs             # Batch command routing tests
│   ├── MetricsCounterTests.fs           # MetricsCounter unit tests
│   └── PublicApiBaseline.txt            # Surface-area baseline
├── PhysicsSimulation.Tests/             # F# — unit tests (39 tests)
│   ├── SimulationWorldTests.fs          # Lifecycle, bodies, forces, gravity, stress
│   ├── CommandHandlerTests.fs           # Command dispatch, edge cases
│   ├── ResetSimulationTests.fs          # Reset/restart command tests
│   ├── StaticBodyTrackingTests.fs       # Static body state tracking tests
│   └── SurfaceAreaTests.fs              # Public API baseline verification
├── PhysicsViewer.Tests/                 # F# — unit tests (19 tests)
│   ├── SceneManagerTests.fs             # Shape classification, state accessors
│   ├── CameraControllerTests.fs         # Camera math, command application
│   ├── FpsCounterTests.fs               # FPS calculation, logging interval, threshold tests
│   ├── SurfaceAreaTests.fs              # Public API baseline verification
│   └── PublicApiBaseline.txt            # Surface-area baseline
└── PhysicsClient.Tests/                 # F# — unit tests (52 tests)
    ├── IdGeneratorTests.fs              # Sequential IDs, reset, thread safety
    ├── SessionTests.fs                  # Connection lifecycle
    ├── SimulationCommandsTests.fs       # Proto message construction, Vec3 conversion
    ├── PresetsTests.fs                  # Preset parameters, mass values
    ├── GeneratorsTests.fs               # Scene builder validation, count checks
    ├── SteeringTests.fs                 # Direction-to-Vec3 mapping
    ├── StateDisplayTests.fs             # Vec3 formatting, velocity magnitude, shapes
    └── SurfaceAreaTests.fs              # Public API baseline for all 9 modules
```

## Configuration

- `ASPIRE_CONTAINER_RUNTIME=podman` — set in AppHost launchSettings.json
- `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` — set by AppHost on server resource for gRPC
- `NuGet.config` — local feed at `~/.local/share/nuget-local/` for BepuFSharp package
- Simulation connects to server via Aspire service discovery (`services__server__https__0` env var)
- Viewer connects to server via same Aspire service discovery env vars
- Client connects to server via same Aspire service discovery env vars (fallback: `http://localhost:5000`)
- MCP server connects to PhysicsServer via Aspire service discovery (`services__server__https__0` / `services__server__http__0` env vars); falls back to CLI arg or `https://localhost:7180` for standalone use
- Stride3D uses OpenGL graphics API (`<StrideGraphicsApi>OpenGL</StrideGraphicsApi>`) for container/GPU-passthrough compatibility
- Stride asset compiler disabled by default (`StrideCompilerSkipBuild`); builds without it for CI, enable for live runs with GPU

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
