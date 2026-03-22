> **Note:** This project uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development.
> Development is guided by a project constitution — see [.specify/memory/constitution.md](.specify/memory/constitution.md) for the
> governing principles and architectural constraints.

# Physics Sandbox

A real-time 3D physics simulation built as an F# microservices architecture on .NET 10, orchestrated by [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/). Drop bodies, apply forces, flip gravity, and watch the results in a live [Stride3D](https://www.stride3d.net/) viewer — controlled through a REPL client, F#/Python scripts, or AI assistants via [MCP](https://modelcontextprotocol.io/). Physics powered by [BepuPhysics2](https://github.com/bepu/bepuphysics2).

## Architecture

Five F# services communicate through a central gRPC hub:

```mermaid
graph TD
    Server["Server\n(gRPC Hub)"]
    Sim["Simulation\n(BepuPhysics2)"]
    Viewer["3D Viewer\n(Stride3D)"]
    Client["REPL Client\n(Spectre.Console)"]
    MCP["MCP Server\n(44 AI Tools)"]

    Server <-->|"commands / state\nbidirectional stream"| Sim
    Client -->|"commands\nview commands"| Server
    Server -->|"state stream"| Client
    Server -->|"state stream\nview commands"| Viewer
    MCP -->|"commands"| Server
    Server -->|"state stream"| MCP
```

| Service | Role |
|---|---|
| **PhysicsServer** | Central gRPC hub — routes commands, fans out state, tracks metrics |
| **PhysicsSimulation** | BepuPhysics2 engine — steps the world, streams state |
| **PhysicsViewer** | Stride3D renderer — visualizes bodies, applies camera commands |
| **PhysicsClient** | REPL console — sends commands, displays state with Spectre.Console |
| **PhysicsSandbox.Mcp** | MCP server — 44 tools for AI-assisted simulation control |

## Key Technologies

- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) — service orchestration and telemetry
- [BepuPhysics2](https://github.com/bepu/bepuphysics2) — high-performance rigid body physics engine
- [Stride3D](https://www.stride3d.net/) — open-source C# game engine for 3D visualization
- [MCP](https://modelcontextprotocol.io/) — Model Context Protocol for AI assistant integration
- [Spectre.Console](https://spectreconsole.net/) — rich terminal UI for the REPL client

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- GPU with OpenGL support (for the 3D viewer)
- Linux packages: `openal`, `freetype2`, `sdl2`, `ttf-liberation`

### Build & Run

```bash
# Build
dotnet build PhysicsSandbox.slnx

# Run (starts all services + Aspire dashboard)
./start.sh          # HTTPS profile
./start.sh --http   # HTTP profile

# Or run directly
dotnet run --project src/PhysicsSandbox.AppHost
```

The Aspire dashboard opens at `https://localhost:15888`.

### Interact

**REPL Client** — starts automatically with the AppHost.

**F# Scripts** — 15 demos in `Scripting/demos/`:
```bash
dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx    # single demo
dotnet fsi Scripting/demos/AutoRun.fsx             # run all automatically
```

**Python Scripts** — equivalent demos in `Scripting/demos_py/`:
```bash
pip install grpcio grpcio-tools protobuf
python -m Scripting.demos_py.demo01_hello_drop
```

**MCP (AI Assistants)** — 44 tools for Claude Code, etc.:
```bash
dotnet run --project src/PhysicsSandbox.Mcp
```

## Documentation

Full documentation is available at **https://EHotwagner.github.io/PhysicsSandbox/**

To build and preview locally:
```bash
dotnet tool restore
dotnet fsdocs watch
```
Then open http://localhost:8901.

### Documentation Contents

- [Getting Started](https://EHotwagner.github.io/PhysicsSandbox/getting-started.html) — build, run, and interact
- [Architecture](https://EHotwagner.github.io/PhysicsSandbox/architecture.html) — service design and gRPC contracts
- [Demo Scripts](https://EHotwagner.github.io/PhysicsSandbox/demo-scripts.html) — 15 physics demos in F# and Python
- [MCP Tools](https://EHotwagner.github.io/PhysicsSandbox/mcp-tools.html) — 44 tools for AI-assisted control
- [Test Suite](https://EHotwagner.github.io/PhysicsSandbox/tests.html) — 225+ tests across 6 projects
- [Release: Stride BepuPhysics Integration](https://EHotwagner.github.io/PhysicsSandbox/release-005.html) — what's new in the latest release
- [Known Issues](https://EHotwagner.github.io/PhysicsSandbox/known-issues.html) — limitations and workarounds

## Features

- **Real-time physics** — BepuPhysics2 simulation with 10 shape types (sphere, box, plane, capsule, cylinder, triangle, convex hull, compound, mesh, shape reference)
- **10 constraint types** — hinge, ball-socket, weld, distance, swing/twist limits, motors, point-on-line
- **Per-body color** — RGBA color per body with auto-assigned shape-type defaults
- **Material properties** — friction, bounciness, damping per body with presets
- **Physics queries** — raycast, sweep cast, overlap with batch variants via dedicated RPCs
- **Collision layers** — 32-bit group/mask filtering for physics and queries
- **Kinematic bodies** — script-driven motion, unaffected by gravity
- **Debug visualization** — wireframe collider outlines and constraint connections (F3 toggle)
- **3D visualization** — Stride3D renderer with camera control and per-body color
- **gRPC communication** — contract-first design with proto files
- **Aspire orchestration** — service discovery, health checks, telemetry dashboard
- **MCP integration** — 44 tools for AI assistants
- **Dual scripting** — 15 demos in both F# and Python
- **225+ tests** — unit tests (xUnit) + Aspire integration tests

## Testing

```bash
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

| Project | Tests | Type |
|---|---|---|
| PhysicsClient.Tests | 52 | Unit |
| PhysicsServer.Tests | 27 | Unit |
| PhysicsSimulation.Tests | 85 | Unit |
| PhysicsViewer.Tests | 40 | Unit |
| PhysicsSandbox.Scripting.Tests | 20 | Unit |
| PhysicsSandbox.Integration.Tests | 42 | Integration (Aspire) |

## Project Structure

```
PhysicsSandbox.slnx
src/
  PhysicsSandbox.AppHost/           # C# Aspire orchestrator
  PhysicsSandbox.ServiceDefaults/   # C# shared health/telemetry
  PhysicsSandbox.Shared.Contracts/  # Proto gRPC contracts
  PhysicsServer/                    # F# server hub (message router)
  PhysicsSimulation/                # F# physics simulation (BepuFSharp)
    Queries/                        #   QueryHandler (raycast, sweep, overlap)
  PhysicsViewer/                    # F# 3D viewer (Stride3D)
    Rendering/ShapeGeometry         #   procedural mesh generation
    Rendering/DebugRenderer         #   wireframe collider + constraint visualization
  PhysicsClient/                    # F# REPL client (Spectre.Console)
  PhysicsSandbox.Mcp/               # F# MCP server (44 tools)
  PhysicsSandbox.Scripting/         # F# scripting library (6 modules)
    ConstraintBuilders              #   constraint creation helpers
    QueryBuilders                   #   physics query helpers
tests/
  PhysicsServer.Tests/              # 27 unit tests
  PhysicsSimulation.Tests/          # 85 unit tests
  PhysicsViewer.Tests/              # 40 unit tests
  PhysicsClient.Tests/              # 52 unit tests
  PhysicsSandbox.Scripting.Tests/   # 20 unit tests
  PhysicsSandbox.Integration.Tests/ # 42 integration tests
Scripting/
  demos/                            # F# demo scripts (15 demos)
  demos_py/                         # Python demo scripts (15 demos)
  scripts/                          # Curated F# scripts (Scripting library)
  scratch/                          # Experimentation (gitignored)
```

## Known Issues

See [Known Issues](https://EHotwagner.github.io/PhysicsSandbox/known-issues.html) for current limitations and workarounds.

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
