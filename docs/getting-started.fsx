(**
---
title: Getting Started
category: Overview
categoryindex: 1
index: 3
description: Build, run, and interact with the Physics Sandbox.
---
*)

(**
# Getting Started

This guide covers building, running, and interacting with the Physics Sandbox.

## Option A: Container (Quickest)

Pull or build the container image — no local .NET SDK or system packages needed.

### Run from Docker Hub
*)

(*** do-not-eval ***)
// podman run --rm -it \
//   --device /dev/dri \
//   --network host \
//   -e DISPLAY=$DISPLAY \
//   -v /tmp/.X11-unix:/tmp/.X11-unix \
//   docker.io/ehotwagner/physicssandbox:latest

(**
### Build Locally

The `Containerfile` at the repo root clones both repos (PhysicsSandbox and BepuFSharp)
from GitHub and builds inside the container — no `COPY` needed:
*)

(*** do-not-eval ***)
// podman build -t physicssandbox .
// podman run --rm -it \
//   --device /dev/dri \
//   --network host \
//   -e DISPLAY=$DISPLAY \
//   -v /tmp/.X11-unix:/tmp/.X11-unix \
//   physicssandbox

(**
<div class="alert alert-info">
<strong>Note:</strong> The viewer requires GPU access (<code>--device /dev/dri</code>) and X11 forwarding
(<code>-v /tmp/.X11-unix</code>). <code>--network host</code> exposes the Aspire dashboard
at <code>http://localhost:8081</code> and the MCP server at <code>http://localhost:5199/sse</code>.
</div>

---

## Option B: Build from Source

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- GPU with OpenGL support (for the 3D viewer)
- Linux system packages: `openal`, `freetype2`, `sdl2`, `ttf-liberation`, `freeimage`
- FreeImage symlink: `ln -sf /usr/lib/libfreeimage.so /usr/lib/freeimage.so`

### Build
*)

(*** do-not-eval ***)
// dotnet build PhysicsSandbox.slnx

(**
For headless/CI builds (skips Stride3D asset compiler):
*)

(*** do-not-eval ***)
// dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

(**
### Run

The easiest way to start everything is with the startup script:
*)

(*** do-not-eval ***)
// ./start.sh          # HTTP profile (default, required for MCP with Claude Code)
// ./start.sh --https  # HTTPS profile

(**
This kills any existing instances and launches the Aspire AppHost, which starts:

1. **PhysicsServer** — central gRPC hub
2. **PhysicsSimulation** — BepuPhysics2 engine (waits for server)
3. **PhysicsViewer** — Stride3D renderer (waits for server)
4. **PhysicsClient** — REPL console (waits for server)
5. **PhysicsSandbox.Mcp** — MCP server for AI assistants (waits for server)

The Aspire dashboard opens at `http://localhost:8081`.

Alternatively, run directly:
*)

(*** do-not-eval ***)
// dotnet run --project src/PhysicsSandbox.AppHost

(**
---

## Interact via REPL

The PhysicsClient starts as a REPL. Use the client library to send commands:
*)

(*** do-not-eval ***)
open PhysicsClient.Connection
open PhysicsClient.Commands
open PhysicsClient.Bodies

// Connect to the server
let session = Session.connect "https://localhost:7180"

// Add a sphere
let cmd = SimulationCommands.addSphere "ball-1" 0.0 10.0 0.0 0.5 1.0
Session.sendCommand session cmd

// Apply gravity and run
let gravity = SimulationCommands.setGravity 0.0 -9.81 0.0
Session.sendCommand session gravity
let play = SimulationCommands.play ()
Session.sendCommand session play

(**
## Interact via F# Scripts

22 demo scripts are available in the `Scripting/demos/` directory:
*)

(*** do-not-eval ***)
// dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx    -- single demo
// dotnet fsi Scripting/demos/AutoRun.fsx             -- run all demos automatically
// dotnet fsi Scripting/demos/RunAll.fsx              -- interactive runner

(**
## Interact via Python Scripts

Equivalent demos in Python (requires `grpcio`, `protobuf`):
*)

(*** do-not-eval ***)
// python Scripting/demos_py/demo01_hello_drop.py     -- single demo
// python Scripting/demos_py/auto_run.py              -- run all demos automatically

(**
## Interact via MCP (AI Assistants)

The MCP server exposes 59 tools for AI assistants like Claude Code:
*)

(*** do-not-eval ***)
// dotnet run --project src/PhysicsSandbox.Mcp

(**
Configure your AI assistant to connect to the MCP server endpoint.
Tools include `add_body`, `apply_force`, `set_gravity`, `get_state`,
`start_recording`, `query_snapshots`, `start_stress_test`, and many more.

## Run Tests
*)

(*** do-not-eval ***)
// dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

(**
This runs 467 tests across 7 projects:

| Project | Tests | Type |
|---|---|---|
| PhysicsSimulation.Tests | 114 | Unit (xUnit) |
| PhysicsViewer.Tests | 99 | Unit (xUnit) |
| PhysicsClient.Tests | 78 | Unit (xUnit) |
| PhysicsServer.Tests | 48 | Unit (xUnit) |
| PhysicsSandbox.Scripting.Tests | 26 | Unit (xUnit) |
| PhysicsSandbox.Mcp.Tests | 18 | Unit (xUnit) |
| PhysicsSandbox.Integration.Tests | 84 | Integration (Aspire + xUnit) |

## Next Steps

- [Architecture Overview](architecture.html) — understand the service design
- [Demo Scripts](demo-scripts.html) — explore the 22 physics demos
- [MCP Tools](mcp-tools.html) — use AI assistants to control the simulation
*)
