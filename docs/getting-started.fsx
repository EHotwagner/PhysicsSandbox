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

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- GPU with OpenGL support (for the 3D viewer)
- Linux system packages: `openal`, `freetype2`, `sdl2`, `ttf-liberation`

## Build

Build the entire solution:
*)

(*** do-not-eval ***)
// dotnet build PhysicsSandbox.slnx

(**
For headless/CI builds (skips Stride3D asset compiler):
*)

(*** do-not-eval ***)
// dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

(**
## Run

The easiest way to start everything is with the startup script:
*)

(*** do-not-eval ***)
// ./start.sh          # HTTPS profile
// ./start.sh --http   # HTTP profile

(**
This kills any existing instances and launches the Aspire AppHost, which starts:

1. **PhysicsServer** — central gRPC hub
2. **PhysicsSimulation** — BepuPhysics2 engine (waits for server)
3. **PhysicsViewer** — Stride3D renderer (waits for server)
4. **PhysicsClient** — REPL console (waits for server)
5. **PhysicsSandbox.Mcp** — MCP server for AI assistants (waits for server)

The Aspire dashboard opens at `https://localhost:15888`.

Alternatively, run directly:
*)

(*** do-not-eval ***)
// dotnet run --project src/PhysicsSandbox.AppHost

(**
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

15 demo scripts are available in the `demos/` directory:
*)

(*** do-not-eval ***)
// dotnet fsi demos/Demo01_HelloDrop.fsx    -- single demo
// dotnet fsi demos/AutoRun.fsx             -- run all demos automatically
// dotnet fsi demos/RunAll.fsx              -- interactive runner

(**
## Interact via Python Scripts

Equivalent demos in Python (requires `grpcio`, `protobuf`):
*)

(*** do-not-eval ***)
// python demos_py/demo01_hello_drop.py     -- single demo
// python demos_py/auto_run.py              -- run all demos automatically

(**
## Interact via MCP (AI Assistants)

The MCP server exposes 38 tools for AI assistants like Claude Code:
*)

(*** do-not-eval ***)
// dotnet run --project src/PhysicsSandbox.Mcp

(**
Configure your AI assistant to connect to the MCP server endpoint.
Tools include `add_body`, `apply_force`, `set_gravity`, `get_state`,
`start_stress_test`, and many more.

## Run Tests
*)

(*** do-not-eval ***)
// dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

(**
This runs 165 tests across 5 projects:

| Project | Tests | Type |
|---|---|---|
| PhysicsClient.Tests | 52 | Unit (xUnit) |
| PhysicsServer.Tests | 27 | Unit (xUnit) |
| PhysicsSimulation.Tests | 49 | Unit (xUnit) |
| PhysicsViewer.Tests | 24 | Unit (xUnit) |
| PhysicsSandbox.Integration.Tests | 42 | Integration (Aspire + xUnit) |

## Next Steps

- [Architecture Overview](architecture.html) — understand the service design
- [Demo Scripts](demo-scripts.html) — explore the 15 physics demos
- [MCP Tools](mcp-tools.html) — use AI assistants to control the simulation
*)
