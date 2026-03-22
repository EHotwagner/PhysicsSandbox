(**
---
title: Scripting Library
category: Tutorials
categoryindex: 2
index: 3
description: PhysicsSandbox.Scripting — a convenience library for F# script authors.
---
*)

(**
# Scripting Library

`PhysicsSandbox.Scripting` is an F# class library that bundles all the convenience functions
script authors need to interact with the physics sandbox. Instead of referencing 3 DLLs and
4 NuGet packages, scripts reference a single DLL and get everything through an `[<AutoOpen>]`
Prelude module.

> **Note:** This project uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development.
> See [constitution.md](https://github.com/EHotwagner/PhysicsSandbox/blob/main/.specify/memory/constitution.md)
> for the governing principles.

## Modules

The library is organized into six modules, each with a `.fsi` signature file
(per [Constitution Principle V](https://github.com/EHotwagner/PhysicsSandbox/blob/main/.specify/memory/constitution.md)):

| Module | Functions | Purpose |
|--------|-----------|---------|
| `Helpers` | `ok`, `sleep`, `timed` | Error handling, timing, thread control |
| `Vec3Builders` | `toVec3`, `toTuple` | Convert between tuples and protobuf Vec3 |
| `CommandBuilders` | `makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd` | Build proto SimulationCommand messages |
| `BatchOperations` | `batchAdd` | Send commands in auto-chunked batches of 100 |
| `SimulationLifecycle` | `resetSimulation`, `runFor`, `nextId` | Simulation state control and ID generation |
| `Prelude` | *(all of the above)* | `[<AutoOpen>]` re-export for script convenience |

## Quick Start

A script needs just one `#r` directive and a few `open` statements:
*)

(*** do-not-eval ***)
#r "../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Scripting.Prelude

// Connect and go
let s = connect "http://localhost:5180" |> ok
resetSimulation s

let id = nextId "sphere"
let cmd = makeSphereCmd id (0.0, 10.0, 0.0) 0.5 1.0
batchAdd s [cmd]
runFor s 3.0

disconnect s

(**

<div class="alert alert-info">
<strong>Tip:</strong> Both <code>Scripting/scratch/</code> and <code>Scripting/scripts/</code> folders use the same
relative path to the DLL, so moving a script between them requires no code changes.
</div>

## Helpers Module

Utility functions for error handling and timing.

### `ok` — Unwrap a Result

Extracts the `Ok` value or throws with the error message:
*)

(*** do-not-eval ***)
let value = Ok 42 |> ok          // returns 42
let boom = Error "oops" |> ok    // throws System.Exception("oops")

(**
### `timed` — Measure Execution

Wraps a function call with a `Stopwatch` and prints elapsed time:
*)

(*** do-not-eval ***)
let result = timed "my operation" (fun () ->
    // ... expensive work ...
    42)
// prints: [TIME] my operation: 123 ms
// returns: 42

(**
## Vec3Builders Module

Convert between F# tuples and protobuf `Vec3` messages.
*)

(*** do-not-eval ***)
let v = toVec3 (1.0, 2.0, 3.0)   // Vec3 { X=1, Y=2, Z=3 }
let t = toTuple v                  // (1.0, 2.0, 3.0)

(**
## CommandBuilders Module

Build protobuf `SimulationCommand` messages from simple F# values.
No need to manually construct `Shape`, `AddBody`, `ApplyImpulse`, or `ApplyTorque` proto objects.
*)

(*** do-not-eval ***)
// Add a sphere at position (0, 10, 0) with radius 0.5 and mass 1.0
let sphere = makeSphereCmd "sphere-1" (0.0, 10.0, 0.0) 0.5 1.0

// Add a box at origin with half-extents (0.5, 0.5, 0.5) and mass 2.0
let box = makeBoxCmd "box-1" (0.0, 5.0, 0.0) (0.5, 0.5, 0.5) 2.0

// Apply an upward impulse to a body
let kick = makeImpulseCmd "sphere-1" (0.0, 20.0, 0.0)

// Apply spin around the Y axis
let spin = makeTorqueCmd "box-1" (0.0, 5.0, 0.0)

(**
## BatchOperations Module

Send lists of commands efficiently with automatic chunking.
The server enforces a 100-command limit per batch request;
`batchAdd` handles splitting transparently:
*)

(*** do-not-eval ***)
// Create 200 spheres — automatically split into 2 batches of 100
let cmds =
    [ for i in 1..200 ->
        let id = nextId "sphere"
        makeSphereCmd id (0.0, float i * 0.5, 0.0) 0.3 1.0 ]
batchAdd s cmds

(**
Any per-command failures are logged with their index and error message:

```
  [BATCH FAIL] command 42: Body ID already exists
```

## SimulationLifecycle Module

### `resetSimulation` — Clean Slate

Resets the simulation to a pristine state:

1. Pause the simulation
2. Server-side reset (falls back to manual `clearAll` on error)
3. Reset the ID generator counter
4. Add a ground plane
5. Set gravity to (0, -9.81, 0)
6. Wait 100ms for stabilization

### `runFor` — Timed Execution

Play the simulation for a fixed duration, then pause:
*)

(*** do-not-eval ***)
resetSimulation s          // clean slate
// ... add bodies ...
runFor s 3.0               // run for 3 seconds, then pause

(**
### `nextId` — Sequential IDs

Generate human-readable, sequential body IDs:
*)

(*** do-not-eval ***)
let id1 = nextId "sphere"   // "sphere-1"
let id2 = nextId "sphere"   // "sphere-2"
let id3 = nextId "box"      // "box-1"

(**
## Extending the Library

To add a new function:

1. Add the signature to the appropriate `.fsi` file (e.g., `Vec3Builders.fsi`)
2. Add the implementation to the matching `.fs` file
3. *(Optional)* Re-export in `Prelude.fsi` and `Prelude.fs` for script convenience
4. Update `SurfaceAreaBaseline.txt` in the test project
5. Rebuild — existing scripts and the MCP server continue to work unchanged

<div class="alert alert-warning">
<strong>Constitution Principle V:</strong> Every public F# module must have a <code>.fsi</code>
signature file. The surface area baseline test will fail if a new public symbol is added without
updating the baseline.
</div>

## Folder Layout

All scripting folders live under `Scripting/` at the repo root:

| Folder | Tracked | Purpose |
|--------|---------|---------|
| `Scripting/scripts/` | Git | Curated, production-quality scripts |
| `Scripting/scratch/` | Gitignored | Developer-local experimentation |
| `Scripting/demos/` | Git | F# demo suite (15 demos + runners) |
| `Scripting/demos_py/` | Git | Python demo suite (15 demos + runners) |

`scripts/` and `scratch/` use the same relative path to the library DLL,
so moving a script between them requires no code changes.

## MCP Server Integration

The MCP server references `PhysicsSandbox.Scripting` as a project dependency.
`ClientAdapter.toVec3` delegates to `Vec3Builders.toVec3`, eliminating code duplication
between the scripting and MCP tool surfaces.

## Next Steps

- [Getting Started](getting-started.html) — build and run the full sandbox
- [Demo Scripts](demo-scripts.html) — 15 physics demos in F# and Python
- [MCP Tools](mcp-tools.html) — 38 AI-assisted debugging tools
*)
