(**
---
title: Architecture Overview
category: Design
categoryindex: 4
index: 1
description: Service design, data flow, gRPC contracts, and Aspire orchestration.
---
*)

(**
# Architecture Overview

Physics Sandbox is a distributed real-time physics simulation built as four F# microservices
plus a C# Aspire orchestrator. All inter-service communication flows through gRPC,
with a central hub routing messages between producers and consumers.

## Service Topology

<pre class="mermaid">
graph TD
    Server["Server\n(gRPC Hub)"]
    Sim["Simulation\n(BepuPhysics2)"]
    Viewer["3D Viewer\n(Stride3D)"]
    Client["REPL Client\n(Spectre.Console)"]
    MCP["MCP Server\n(38 AI Tools)"]
    Server <-->|"SimulationCommand ↓ · SimulationState ↑\nbidirectional stream (ConnectSimulation)"| Sim
    Client -->|"SendCommand / SendBatchCommand\nSendViewCommand"| Server
    Server -->|"StreamState"| Client
    Server -->|"StreamState\nStreamViewCommands"| Viewer
    MCP -->|"SendCommand"| Server
    Server -->|"StreamState"| MCP
</pre>

## Services

| Service | Language | Role |
|---|---|---|
| **PhysicsServer** | F# | Central gRPC hub — routes commands to simulation, fans out state to all subscribers |
| **PhysicsSimulation** | F# | Runs BepuPhysics2 via BepuFSharp — steps the physics world, streams state back |
| **PhysicsViewer** | F# | Stride3D-based 3D renderer — receives state and camera commands, renders scene |
| **PhysicsClient** | F# | REPL console with Spectre.Console TUI — sends commands, displays state |
| **PhysicsSandbox.Mcp** | F# | MCP server (38 tools) — enables AI assistants to control the simulation |
| **AppHost** | C# | Aspire orchestrator — service discovery, health checks, startup ordering |

## Communication Flows

### 1. Commands: Client/MCP -> Server -> Simulation
*)

(*** do-not-eval ***)
// Client creates a command:
//   SimulationCommand { add_body { id: "ball-1", position: {y: 10}, mass: 1.0, shape: { sphere: { radius: 0.5 } } } }
//
// Server receives via PhysicsHub.SendCommand RPC
// Server forwards via SimulationLink.ConnectSimulation bidirectional stream
// Simulation processes command, updates physics world

(**
### 2. State: Simulation -> Server -> Client + Viewer
*)

(*** do-not-eval ***)
// Simulation pushes state every tick:
//   SimulationState { bodies: [...], time: 1.234, running: true, tick_ms: 0.5, serialize_ms: 0.1 }
//
// Server caches latest state (for late joiners)
// Server fans out to all StreamState subscribers (Client, Viewer)

(**
### 3. View Commands: Client -> Server -> Viewer
*)

(*** do-not-eval ***)
// Client sends camera/wireframe/zoom commands:
//   ViewCommand { set_camera { position: {x:10, y:5, z:10}, target: {x:0,y:0,z:0} } }
//
// Server forwards via StreamViewCommands to Viewer
// Viewer applies camera transform to Stride3D scene

(**
## gRPC Contract

The entire API is defined in a single proto file (`physics_hub.proto`):

**Two services:**

- `PhysicsHub` — client/viewer-facing (8 RPCs)
- `SimulationLink` — simulation-facing (1 bidirectional stream)

**Key RPCs:**

| RPC | Direction | Description |
|---|---|---|
| `SendCommand` | Client -> Server | Single simulation command |
| `SendBatchCommand` | Client -> Server | Up to 100 commands in one call |
| `StreamState` | Server -> Client/Viewer | Server-streaming state updates |
| `StreamViewCommands` | Server -> Viewer | Server-streaming camera/UI commands |
| `StreamCommands` | Server -> Client | Audit stream of all commands |
| `ConnectSimulation` | Simulation <-> Server | Bidirectional: state up, commands down |
| `GetMetrics` | Client -> Server | Performance counters and pipeline timings |

**Key messages:**

| Message | Fields | Purpose |
|---|---|---|
| `SimulationCommand` | oneof: AddBody, ApplyForce, ApplyImpulse, ApplyTorque, SetGravity, Step, PlayPause, RemoveBody, ClearForces, Reset | All physics commands |
| `ViewCommand` | oneof: SetCamera, ToggleWireframe, SetZoom | All viewer commands |
| `SimulationState` | bodies[], time, running, tick_ms, serialize_ms | Full world snapshot per tick |
| `Body` | id, position, velocity, mass, shape, angular_velocity, orientation, is_static | Single body state |

## Aspire Orchestration

The AppHost defines service dependencies:
*)

(*** do-not-eval ***)
// AppHost startup order:
// 1. Server starts first (no dependencies)
// 2. Simulation, Viewer, Client, MCP start after Server is healthy
//    Each uses .WithReference(server).WaitFor(server)
//
// Service discovery: services find the server via "https+http://server"
// Health checks: /health (readiness) and /alive (liveness)
// Dashboard: https://localhost:15888 with logs, traces, metrics

(**
## Key Design Decisions

### Hub-and-Spoke over Mesh
All communication routes through the Server hub. This simplifies service
discovery (everyone only knows the server), enables centralized metrics
and audit logging, and allows late-joining clients to receive cached state.

### Bidirectional Streaming for Simulation
The simulation uses a single bidirectional gRPC stream (`ConnectSimulation`)
rather than separate request/response RPCs. This eliminates per-command
connection overhead and enables continuous state streaming at physics tick rate.

### Contract-First with Proto Files
The proto file is the single source of truth. C# generates the base classes
and message types via `Grpc.Tools`. F# services reference the shared contracts
project. This enforces API discipline across the polyglot solution.

### Compiler-Enforced API Surface
Every public F# module has a `.fsi` signature file. The compiler verifies
implementations match their signatures. Surface-area baseline tests detect
unintentional API changes.

## Next Steps

- [Getting Started](getting-started.html) — build and run the sandbox
- [Demo Scripts](demo-scripts.html) — see physics demos in action
- [MCP Tools](mcp-tools.html) — AI-assisted simulation control
- [API Reference](reference/index.html) — generated API documentation
*)
