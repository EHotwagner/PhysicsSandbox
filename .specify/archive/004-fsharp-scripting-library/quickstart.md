# Quickstart: F# Scripting Library

**Feature**: 004-fsharp-scripting-library | **Date**: 2026-03-22

## Prerequisites

1. Build the solution: `dotnet build PhysicsSandbox.slnx`
2. Start the sandbox: `./start.sh` (or `dotnet run --project src/PhysicsSandbox.AppHost`)

## Writing a Script

### In the scratch folder (experimentation)

Create `scratch/my-experiment.fsx`:

```fsharp
#r "../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Scripting.Prelude

let s = connect "http://localhost:5180" |> ok

resetSimulation s
let id = nextId "sphere"
let cmd = makeSphereCmd id (0.0, 10.0, 0.0) 0.5 1.0
batchAdd s [cmd]
runFor s 3.0

disconnect s
```

Run it:
```bash
dotnet fsi scratch/my-experiment.fsx
```

### Promoting to scripts folder

When your experiment is ready to share, move it:
```bash
mv scratch/my-experiment.fsx scripts/my-experiment.fsx
```

No code changes needed — the relative path to the library DLL is the same from both folders.

## Using in the MCP Server

The MCP server references `PhysicsSandbox.Scripting` as a project dependency. Import and use shared functions:

```fsharp
open PhysicsSandbox.Scripting.Vec3Builders
open PhysicsSandbox.Scripting.CommandBuilders

let v = toVec3 (1.0, 2.0, 3.0)
let cmd = makeSphereCmd "test-sphere" (0.0, 5.0, 0.0) 0.5 1.0
```

## Extending the Library

1. Add your function to the appropriate module (e.g., `CommandBuilders.fs`)
2. Add the signature to the corresponding `.fsi` file
3. Optionally re-export from `Prelude.fs` / `Prelude.fsi`
4. Update `SurfaceAreaTests.fs` baseline
5. Rebuild: `dotnet build PhysicsSandbox.slnx`

## Available Functions

| Function | Module | Description |
|----------|--------|-------------|
| `ok` | Helpers | Unwrap Result or fail |
| `sleep` | Helpers | Thread.Sleep wrapper |
| `timed` | Helpers | Execute and log elapsed time |
| `toVec3` | Vec3Builders | Tuple to proto Vec3 |
| `makeSphereCmd` | CommandBuilders | Build AddBody sphere command |
| `makeBoxCmd` | CommandBuilders | Build AddBody box command |
| `makeImpulseCmd` | CommandBuilders | Build ApplyImpulse command |
| `makeTorqueCmd` | CommandBuilders | Build ApplyTorque command |
| `batchAdd` | BatchOperations | Send commands in chunks of 100 |
| `resetSimulation` | SimulationLifecycle | Pause, clear, add plane, set gravity |
| `runFor` | SimulationLifecycle | Play for N seconds then pause |
| `nextId` | SimulationLifecycle | Generate sequential body ID |
