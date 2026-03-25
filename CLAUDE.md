# PhysicsSandbox Development Guidelines

Last updated: 2026-03-25

## Active Technologies
- F# on .NET 10.0 (services, MCP, client), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts)
- .NET Aspire 13.2.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x
- BepuFSharp 0.3.0 (local NuGet, physics engine wrapper — 10 shape types, 10 constraint types, sweep/overlap queries, collision filtering, material properties), Grpc.Net.Client 2.x
- Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages, 3D viewer)
- Spectre.Console (client library TUI display)
- ModelContextProtocol.AspNetCore 1.1.* (MCP server, persistent HTTP/SSE transport)
- xUnit 2.x, Aspire.Hosting.Testing 10.x
- In-memory storage (physics world, shape cache, constraint registry, metrics counters, stress test state, command logs)
- F# scripts (.fsx) on .NET 10.0 + PhysicsClient.dll, PhysicsSandbox.Shared.Contracts.dll (proto-generated types) (001-demo-script-modernization)
- F# scripts (.fsx) on .NET 10.0 + PhysicsClient.dll (existing), PhysicsSandbox.Shared.Contracts.dll (existing), Grpc.Net.Client, Google.Protobuf (003-stress-test-demos)
- N/A (in-memory physics simulation) (003-stress-test-demos)
- Python 3.10+ + grpcio, grpcio-tools, protobuf (for gRPC stub generation and communication) (004-python-demo-scripts)
- N/A (stateless scripts communicating with running server) (004-python-demo-scripts)
- F# on .NET 10.0 + PhysicsClient (project ref), PhysicsSandbox.Shared.Contracts (transitive), Grpc.Net.Client 2.x, Google.Protobuf 3.x (004-fsharp-scripting-library)
- N/A (stateless library) (004-fsharp-scripting-library)
- F# on .NET 10.0 (PhysicsClient, Scripting), C# on .NET 10.0 (Contracts, ServiceDefaults) + Grpc.Net.Client 2.x, Google.Protobuf 3.x, Grpc.AspNetCore 2.x, Spectre.Console 0.49.x, OpenTelemetry 1.14.x, Microsoft.Extensions.ServiceDiscovery 10.1.0 (004-scripting-nuget-package)
- N/A (local NuGet feed at `~/.local/share/nuget-local/`) (004-scripting-nuget-package)
- F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio + PhysicsClient (F# NuGet), prelude.py (Python), existing Prelude.fsx helpers (004-improve-demos)
- N/A (stateless scripts communicating with running physics server) (004-improve-demos)
- F# on .NET 10.0 (services, MCP, client, scripting), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts) + .NET Aspire 13.2.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.1.0→0.2.0 (local NuGet), Stride.CommunityToolkit.Bepu 1.0.0-preview.62, Spectre.Console, ModelContextProtocol.AspNetCore 1.1.* (005-stride-bepu-integration)
- In-memory (physics world, shape cache, constraint registry, metrics counters) (005-stride-bepu-integration)
- F# on .NET 10.0 (PhysicsViewer project) + Stride.CommunityToolkit 1.0.0-preview.62 (rendering), Stride.CommunityToolkit.Bepu 1.0.0-preview.62 (Bepu3DPhysicsOptions, Create3DPrimitive), Grpc.Net.Client 2.x (server communication), System.Text.Json (settings persistence) (005-viewer-settings-sizing-fix)
- JSON file at `~/.config/PhysicsSandbox/viewer-settings.json` (005-viewer-settings-sizing-fix)
- F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio + PhysicsClient.dll (NuGet), PhysicsSandbox.Shared.Contracts.dll (proto types), Grpc.Net.Client 2.x, Google.Protobuf 3.x (005-enhance-demos)
- F# on .NET 10.0 (services, MCP, client, scripting), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts), Python 3.10+ (demo scripts) + .NET Aspire 13.2.0, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console, xUnit 2.x (005-refactor-evaluation)
- F# on .NET 10.0 + Google.Protobuf 3.x (binary serialization), System.Text.Json (session metadata), System.Threading.Channels (async producer-consumer), ModelContextProtocol.AspNetCore 1.1.* (MCP tool registration) (005-mcp-data-logging)
- Append-only protobuf binary files at `~/.config/PhysicsSandbox/recordings/`, JSON metadata per session (005-mcp-data-logging)
- F# on .NET 10.0 (services, MCP, client, viewer), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests) + .NET Aspire 13.2.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console (004-mesh-cache-transport)
- In-memory (physics world, mesh caches). Append-only protobuf binary files for MCP recordings. (004-mesh-cache-transport)
- F# on .NET 10.0 (PhysicsServer, PhysicsSandbox.Mcp), C# on .NET 10.0 (integration tests) + Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, ModelContextProtocol.AspNetCore 1.1.*, System.Threading.Channels (004-mcp-mesh-logging)
- Append-only protobuf binary files at `~/.config/PhysicsSandbox/recordings/` (existing recording infrastructure) (004-mcp-mesh-logging)
- F# on .NET 10.0 (services, MCP, client), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests) + .NET Aspire 13.2.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console (004-state-stream-optimization)
- In-memory (physics world, mesh caches, state caches). Append-only protobuf binary files for MCP recordings. (004-state-stream-optimization)
- F# on .NET 10.0 (PhysicsClient, PhysicsServer, Scripting), C# on .NET 10.0 (Integration Tests), Bash (test progress script) + xUnit 2.x, Aspire.Hosting.Testing 10.x, Grpc.Net.Client 2.x, Google.Protobuf 3.x (004-backlog-fix-test-progress)
- N/A (in-memory ConcurrentDictionary for pending queries) (004-backlog-fix-test-progress)
- F# on .NET 10.0 (PhysicsViewer) + Stride.CommunityToolkit 1.0.0-preview.62 (existing), MIConvexHull (new, convex hull face computation) (004-proper-shape-rendering)
- N/A (in-memory rendering only) (004-proper-shape-rendering)
- F# on .NET 10.0 (services, viewer, client, scripting), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts), Python 3.10+ (demo scripts) + Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, Stride.CommunityToolkit 1.0.0-preview.62, Spectre.Console, grpcio (Python) (004-enhance-demos-shapes)
- N/A (in-memory only, no persistence changes) (004-enhance-demos-shapes)
- F# on .NET 10.0 (viewer, client, scripting), C# on .NET 10.0 (contracts), Python 3.10+ (demo scripts) + Stride.CommunityToolkit 1.0.0-preview.62 (viewer), Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, grpcio (Python) (004-camera-smooth-demos)
- N/A (in-memory camera state only) (004-camera-smooth-demos)
- F# on .NET 10.0 (services, MCP, client, viewer), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests) + .NET Aspire 13.2.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, System.Threading.Channels (in-box) (005-robust-network-connectivity)
- N/A (in-memory channel/subscriber state only) (005-robust-network-connectivity)
- F# on .NET 10.0 (PhysicsSimulation), C# on .NET 10.0 (integration tests) + BepuFSharp 0.2.0-beta.1 → 0.3.0 (local NuGet at `~/.local/share/nuget-local/`). Transitive: BepuPhysics 2.5.0-beta.28 (unchanged), BepuUtilities 2.5.0-beta.28 (unchanged), FSharp.Core 10.0.104 (unchanged) (004-upgrade-bepufsharp)
- N/A (no storage changes) (004-upgrade-bepufsharp)
- F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio + PhysicsClient 0.4.0 (NuGet, F#), PhysicsSandbox.Shared.Contracts 0.4.0 (proto types), grpcio + protobuf (Python) (004-mesh-terrain-demos)
- F# on .NET 10.0 (MCP server, tool modules), C# on .NET 10.0 (integration tests) + ModelContextProtocol.AspNetCore 1.1.* (MCP framework), Grpc.Net.Client 2.x, xUnit 2.x, Aspire.Hosting.Testing 10.x (004-mcp-fix-aspire-config)

## Project Structure

```text
PhysicsSandbox.slnx
src/
  PhysicsSandbox.AppHost/           # C# Aspire orchestrator
  PhysicsSandbox.ServiceDefaults/   # C# shared health/telemetry
  PhysicsSandbox.Shared.Contracts/  # Proto gRPC contracts
  PhysicsServer/                    # F# server hub (message router)
  PhysicsSimulation/                # F# physics simulation (gRPC client, BepuFSharp)
  PhysicsViewer/                    # F# 3D viewer (Stride3D + gRPC client, debug wireframes)
  PhysicsClient/                    # F# REPL client library (gRPC client, Spectre.Console)
  PhysicsSandbox.Mcp/               # F# MCP server (47 tools, interactive debugging via AI assistants, recording + query)
  PhysicsSandbox.Scripting/         # F# scripting convenience library (wraps PhysicsClient, 6 modules)
tests/
  PhysicsServer.Tests/              # F# unit tests (44 tests)
  PhysicsSimulation.Tests/          # F# unit tests (114 tests)
  PhysicsViewer.Tests/              # F# unit tests (99 tests)
  PhysicsClient.Tests/              # F# unit tests (77 tests)
  PhysicsSandbox.Mcp.Tests/          # F# unit tests (19 tests)
  PhysicsSandbox.Scripting.Tests/   # F# unit + surface area tests (26 tests)
  PhysicsSandbox.Integration.Tests/ # C# Aspire integration tests (60 tests)
Scripting/
  demos/                            # F# demo scripts (22 demos + runners)
  demos_py/                         # Python demo scripts (22 demos + runners)
  scripts/                          # Curated F# scripts using Scripting library
  scratch/                          # Gitignored experimentation folder
```

## Commands

```bash
# Build
dotnet build PhysicsSandbox.slnx

# Build (headless/CI, skip Stride asset compiler)
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run (starts Aspire dashboard + server + simulation + viewer + client + mcp)
dotnet run --project src/PhysicsSandbox.AppHost

# Run (with process cleanup, kills existing instances first)
./start.sh          # HTTPS profile
./start.sh --http   # HTTP profile

# Test
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run MCP server (for interactive debugging via AI assistants)
dotnet run --project src/PhysicsSandbox.Mcp
# Or with custom server address:
dotnet run --project src/PhysicsSandbox.Mcp -- https://localhost:7180
```

## Code Style

- F# services: `.fsi` signature files required for all public modules (constitution Principle V)
- C# infrastructure projects: standard conventions, no domain logic
- Proto files: `physics_sandbox` package, `PhysicsSandbox.Shared.Contracts` C# namespace

## Recent Changes
- 004-mcp-fix-aspire-config: Fixed 17 MCP tool deserialization failures by converting F# optional params to Nullable<T>. Improved tool descriptions (applicability, defaults). Configured Aspire Dashboard MCP stdio transport in .mcp.json. All 59 MCP tools now accept minimal relevant params.
- 004-mesh-terrain-demos: Demo 23 (Ball Rollercoaster) and Demo 24 (Halfpipe Arena) — static mesh heightmap terrain, F#/Python, MotionType.Static for mesh bodies, heightmap grid approach for reliable BepuPhysics2 collision
- 004-upgrade-bepufsharp: Added F# on .NET 10.0 (PhysicsSimulation), C# on .NET 10.0 (integration tests) + BepuFSharp 0.2.0-beta.1 → 0.3.0 (local NuGet at `~/.local/share/nuget-local/`). Transitive: BepuPhysics 2.5.0-beta.28 (unchanged), BepuUtilities 2.5.0-beta.28 (unchanged), FSharp.Core 10.0.104 (unchanged)

## Environment

- Container with GPU passthrough — not headless
- Stride3D viewer and other GPU workloads are expected to run

## Network Problem Logging

When you encounter errors related to **Aspire service discovery, port binding, gRPC connectivity, HTTP/2 negotiation, SSL/TLS certificates, or service-to-service communication** during implementation or debugging, append a structured entry to `reports/NetworkProblems.md`. Create the file if it doesn't exist.

Entry format:
```markdown
### [Short title] — YYYY-MM-DD

**Context**: What you were doing when it happened
**Error**: Paste the actual error message or relevant log output
**Root Cause**: What caused it (if known)
**Hypothesis**: If root cause unknown, what you suspect
**Resolution**: What fixed it (or "unresolved")
**Prevention**: How to avoid it in the future (if applicable)
```

Do this every time — even for transient errors that self-resolve, since patterns in transient failures are valuable.

## Known Issues & Gotchas

### gRPC HTTP/2 Configuration
F# service projects need `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation errors in non-C# projects. AppHost must set `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` for gRPC over plain HTTP.

### Integration Test SSL
Aspire integration tests connecting via HTTPS need `RemoteCertificateValidationCallback = (_, _, _, _) => true` on the `SocketsHttpHandler` to accept dev certificates.

### Solution Format
Solution file is `.slnx` (XML-based, .NET 10 default), not `.sln`.

### BepuFSharp NuGet Packaging
Pack BepuFSharp with `-p:NoWarn=NU5104` to suppress prerelease dependency warnings from BepuPhysics2 beta packages. Local NuGet feed at `~/.local/share/nuget-local/`.

### Proto Type Name Conflicts
Proto `Sphere`/`Box` type names conflict with BepuFSharp shapes in F#. Use type aliases (`ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere`) to disambiguate.

### Plane Bodies
Planes are approximated as large static boxes (BepuPhysics2 has no infinite plane). Static bodies are not tracked in the simulation state stream.

### Stride3D Vector3 Interop
Stride's `Vector3` uses `inref<>` operator overloads that don't work with F# `+`/`*`. Use `Vector3.Add(&a, &b, &result)` or helper functions. Same applies to `Vector3.Cross` — must use `byref` calling convention.

### Stride3D Camera Controller Conflict
Do not use Stride's `Add3DCameraController()` alongside a custom CameraController — they fight for input. Use `Add3DCamera()` only and apply camera transforms manually.

### Stride3D Linux Dependencies
Viewer needs `openal`, `freetype2`, `sdl2`, `ttf-liberation` system packages. FreeImage requires `freeimage.so` symlink (`ln -sf /usr/lib/libfreeimage.so /usr/lib/freeimage.so`). GLSL shader compiler binary must be at `linux-x64/glslangValidator.bin`.

### Stride Create3DPrimitive Size Semantics
`Bepu3DPhysicsOptions.Size` interpretation varies by primitive type: Sphere/Capsule/Cylinder use Size.X as **radius** (not diameter); Cube uses full extents. When computing sizes from physics dimensions, pass radius directly — do not double it. This was the root cause of the original shape sizing bug.

### Viewer Custom Shape Rendering
Triangle, mesh, and convex hull shapes render with actual geometry (custom vertex/index buffers) in both solid and wireframe views. Compound shapes decompose into individually-rendered children. ConvexHull face computation uses MIConvexHull NuGet library. ShapeRef and CachedRef resolve to their underlying shapes before rendering. All 10 shape types render with accurate collision-matching geometry.

### Proto MeshShape vs Triangle Naming
The proto `MeshShape` message (Shape oneof field `mesh`) uses `MeshTriangle` for its triangle elements — not `Triangle` (which is a separate shape type). In F# scripts use `MeshShape()` and `MeshTriangle()`. In Python use `pb.MeshShape(triangles=[pb.MeshTriangle(...)])`.

### Viewer Demo Label
The viewer displays a demo name/description overlay at (10, 10) via `DebugTextSystem.Print()`. Status bar (FPS/time/status) is at (10, 30). Demo metadata is transported via `SetDemoMetadata` ViewCommand (field 4) — auto-forwarded by server, no server code changes needed.

### PhysicsClient NuGet Version
Demo scripts pin `PhysicsClient 0.4.0` (added smooth camera + narration commands). The Prelude.fsx uses `#r "nuget: PhysicsClient, 0.4.0"`. Contracts also at 0.4.0.

### Static Mesh Body MotionType
Static mesh bodies (mass=0) require explicit `MotionType.Static` (enum value 2) via `withMotionType BodyMotionType.Static` (F#) or `with_motion_type(cmd, 2)` (Python). The default MotionType is Dynamic (0), and mass=0 + Dynamic is rejected by the server. Without the explicit MotionType, the mesh body silently fails to be created.

### BepuPhysics2 Mesh Triangle Size
Mesh collision triangles must be ~2m+ per edge for reliable collision detection. Very thin or narrow triangles (from parametric cross-section strips) allow small objects to fall through. Use heightmap grids with well-shaped quads (2 triangles per ~2×2m cell) instead of narrow strip geometry.

### MCP Tool Parameter Types (Nullable<T> Pattern)
MCP tool parameters in `PhysicsSandbox.Mcp` use `Nullable<T>` (not F# `Option<T>`) for optional value types. The ModelContextProtocol.AspNetCore framework only recognizes `Nullable<T>` as optional in auto-generated JSON schemas — F#'s `?param: Type` (which compiles to `FSharpOption<T>`) is treated as required. Use `param.HasValue`/`param.Value` instead of `defaultArg`/pattern matching. String optional params use plain `string` with null checks. This pattern is required for all MCP tool methods marked with `[<McpServerTool>]`.

### Aspire Dashboard MCP (stdio transport)
The Aspire Dashboard MCP is configured in `.mcp.json` using stdio transport via `aspire agent mcp --nologo --non-interactive`. The HTTP/SSE endpoint at port 18093 returns 403 Forbidden (documented in NetworkProblems.md). The stdio transport provides 14 tools including `list_resources`, `list_console_logs`, `doctor`, `search_docs`. Requires the Aspire stack to be running first.

### Stride3D Asset Compiler
`StrideCompilerSkipBuild=true` skips asset compilation for CI/headless builds. For live GPU runs, build without this flag (requires fonts + FreeImage). The viewer's `.fsproj` defaults to `false` unless overridden.
