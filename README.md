> **Note:** This project uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development.
> Development is guided by a project constitution — see [.specify/memory/constitution.md](.specify/memory/constitution.md) for the
> governing principles and architectural constraints.

# Physics Sandbox

A real-time 3D physics simulation built as an F# microservices architecture on .NET 10, orchestrated by [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/). Drop bodies, apply forces, flip gravity, and watch the results in a live Stride3D viewer — controlled through a REPL client, F#/Python scripts, or AI assistants via [MCP](https://modelcontextprotocol.io/).

## Architecture

Four F# services communicate through a central gRPC hub:

```
                ┌──────────────┐
                │    Server    │
                │   (Hub/API)  │
                └──┬──┬──┬────┘
     commands ▲    │  │  │    ▼ state + view cmds
        ┌──────────┘  │  └──────────┐
        │             │             │
┌───────┴──────┐      │      ┌──────┴───────┐
│  Simulation  │      │      │   3D Viewer  │
│ (BepuPhysics)│      │      │  (Stride3D)  │
└──────────────┘      │      └──────────────┘
                      │
               ▲ cmds │ state ▼
                ┌─────┴────────┐
                │  REPL Client │
                │  (Spectre)   │
                └──────────────┘
```

| Service | Role |
|---|---|
| **PhysicsServer** | Central gRPC hub — routes commands, fans out state, tracks metrics |
| **PhysicsSimulation** | BepuPhysics2 engine — steps the world, streams state |
| **PhysicsViewer** | Stride3D renderer — visualizes bodies, applies camera commands |
| **PhysicsClient** | REPL console — sends commands, displays state with Spectre.Console |
| **PhysicsSandbox.Mcp** | MCP server — 38 tools for AI-assisted simulation control |

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

**F# Scripts** — 15 demos in `demos/`:
```bash
dotnet fsi demos/Demo01_HelloDrop.fsx    # single demo
dotnet fsi demos/AutoRun.fsx             # run all automatically
```

**Python Scripts** — equivalent demos in `demos_py/`:
```bash
pip install grpcio grpcio-tools protobuf
python demos_py/demo01_hello_drop.py
```

**MCP (AI Assistants)** — 38 tools for Claude Code, etc.:
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
- [MCP Tools](https://EHotwagner.github.io/PhysicsSandbox/mcp-tools.html) — 38 tools for AI-assisted control
- [Test Suite](https://EHotwagner.github.io/PhysicsSandbox/tests.html) — 165 tests across 5 projects
- [Known Issues](https://EHotwagner.github.io/PhysicsSandbox/known-issues.html) — limitations and workarounds

## Features

- **Real-time physics** — BepuPhysics2 simulation with spheres, boxes, and planes
- **3D visualization** — Stride3D renderer with camera control and wireframe mode
- **gRPC communication** — contract-first design with proto files
- **Aspire orchestration** — service discovery, health checks, telemetry dashboard
- **MCP integration** — 38 tools for AI assistants (add bodies, apply forces, stress test)
- **Dual scripting** — 15 demos in both F# (.fsx) and Python
- **Batch commands** — submit up to 100 commands per RPC call
- **Stress testing** — built-in tools for load testing and performance comparison
- **Metrics pipeline** — per-service message counts, pipeline timings, diagnostics
- **165 tests** — unit tests (xUnit) + Aspire integration tests

## Testing

```bash
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

| Project | Tests | Type |
|---|---|---|
| PhysicsClient.Tests | 52 | Unit |
| PhysicsServer.Tests | 27 | Unit |
| PhysicsSimulation.Tests | 49 | Unit |
| PhysicsViewer.Tests | 24 | Unit |
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
  PhysicsViewer/                    # F# 3D viewer (Stride3D)
  PhysicsClient/                    # F# REPL client (Spectre.Console)
  PhysicsSandbox.Mcp/               # F# MCP server (38 tools)
tests/
  PhysicsServer.Tests/              # 18 unit tests
  PhysicsSimulation.Tests/          # 39 unit tests
  PhysicsViewer.Tests/              # 19 unit tests
  PhysicsClient.Tests/              # 52 unit tests
  PhysicsSandbox.Integration.Tests/ # 42 integration tests
demos/                              # F# demo scripts (15 demos)
demos_py/                           # Python demo scripts (15 demos)
```

## Known Issues

See [Known Issues](https://EHotwagner.github.io/PhysicsSandbox/known-issues.html) for current limitations and workarounds.

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
