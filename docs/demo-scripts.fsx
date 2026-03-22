(**
---
title: Demo Scripts
category: Tutorials
categoryindex: 2
index: 1
description: 15 physics demos available in F# and Python.
---
*)

(**
# Demo Scripts

Physics Sandbox includes 15 demo scripts that showcase different physics scenarios.
Each demo is available in both F# (`.fsx`) and Python, with identical behavior.

## Running Demos

### F# Demos
*)

(*** do-not-eval ***)
// Run a single demo:
// dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx

// Run all demos automatically (10s each):
// dotnet fsi Scripting/demos/AutoRun.fsx

// Interactive runner (prompts between demos):
// dotnet fsi Scripting/demos/RunAll.fsx

(**
### Python Demos
*)

(*** do-not-eval ***)
// Prerequisites: pip install grpcio grpcio-tools protobuf

// Run a single demo:
// python Scripting/demos_py/demo01_hello_drop.py

// Run all demos automatically:
// python Scripting/demos_py/auto_run.py

// Interactive runner:
// python Scripting/demos_py/run_all.py

(**
## Demo Catalog

| # | Name | Description |
|---|---|---|
| 01 | Hello Drop | Single sphere dropped under gravity — the simplest demo |
| 02 | Bouncing Marbles | Multiple marbles dropped onto a ground plane |
| 03 | Crate Stack | Tower of crates that collapses under gravity |
| 04 | Bowling Alley | Bowling ball launched at a row of pins |
| 05 | Marble Rain | Random spheres spawned above the ground |
| 06 | Domino Row | Line of dominoes knocked over by an impulse |
| 07 | Spinning Tops | Bodies with angular velocity (torque applied) |
| 08 | Gravity Flip | Gravity direction changes mid-simulation |
| 09 | Billiards | Pool table setup with cue ball impulse |
| 10 | Chaos | Many random bodies with random forces |
| 11 | Body Scaling | Bodies of varying sizes and masses |
| 12 | Collision Pit | Bodies funneled into a pit |
| 13 | Force Frenzy | Continuous forces applied to multiple bodies |
| 14 | Domino Cascade | Large-scale domino chain reaction |
| 15 | Overload | Stress test with maximum body count |

## Shared Prelude

Both language suites share a prelude library with 40+ helper functions:

### F# Prelude (`Scripting/demos/Prelude.fsx`)

Key helpers:
*)

(*** do-not-eval ***)
// Session management
// let session = connect "https://localhost:7180"

// Body presets (marble, bowling ball, crate, etc.)
// let cmd = makeMarble "m-1" 0.0 5.0 0.0

// Batch commands (auto-splits at 100)
// batchAdd session commands

// Generators (stack, grid, pyramid, row)
// let cmds = makeStack "s" 5 0.0 0.0 0.0

// Simulation control
// resetSimulation session
// play session
// pause session

(**
### Python Prelude (`Scripting/demos_py/prelude.py`)

Mirrors the F# prelude with identical function names and behavior:
*)

(*** do-not-eval ***)
// # Session management
// channel, stub = connect("localhost:7180")
//
// # Body presets
// cmd = make_marble("m-1", 0.0, 5.0, 0.0)
//
// # Batch commands
// batch_add(stub, commands)
//
// # Generators
// cmds = make_stack("s", 5, 0.0, 0.0, 0.0)

(**
## Demo Structure

Each demo follows the same pattern:

1. **Reset** the simulation (clear all bodies, pause, reset time)
2. **Create** bodies using presets or generators
3. **Configure** gravity, camera, and other settings
4. **Play** the simulation
5. **Apply** forces, impulses, or other interactions
6. **Wait** for the demo duration
7. **Pause** and clean up

### Example: Demo 01 — Hello Drop
*)

(*** do-not-eval ***)
// 1. Reset simulation
// resetSimulation session
//
// 2. Add a ground plane and a sphere at height 10
// let plane = makePlane "ground" 0.0 0.0 0.0
// let sphere = makeMarble "ball-1" 0.0 10.0 0.0
// batchAdd session [plane; sphere]
//
// 3. Set gravity and camera
// setGravity session 0.0 -9.81 0.0
// setCamera session (10.0, 5.0, 10.0) (0.0, 0.0, 0.0)
//
// 4. Play and watch
// play session
// sleep 5000

(**
## Next Steps

- [Getting Started](getting-started.html) — build and run the sandbox
- [MCP Tools](mcp-tools.html) — control demos via AI assistants
- [Architecture](architecture.html) — understand the service design
*)
