# Quickstart: PhysicsClient Library

**Feature**: 004-client-repl | **Date**: 2026-03-20

## Prerequisites

- .NET 10.0 SDK
- PhysicsSandbox solution built (`dotnet build PhysicsSandbox.slnx`)
- Server running (via `dotnet run --project src/PhysicsSandbox.AppHost` or standalone)

## Load in FSI

```fsharp
// Load the library
#r "src/PhysicsClient/bin/Debug/net10.0/PhysicsClient.dll"
#r "src/PhysicsSandbox.Shared.Contracts/bin/Debug/net10.0/PhysicsSandbox.Shared.Contracts.dll"

open PhysicsClient

// Connect to the server
let session = Session.connect "http://localhost:5000" |> Result.defaultWith failwith
```

## Basic Usage (5 calls to running simulation)

```fsharp
// 1. Connect
let s = Session.connect "http://localhost:5000" |> Result.defaultWith failwith
// 2. Add a body
Presets.bowlingBall s ~position:(0.0, 5.0, 0.0)
// 3. Add ground
SimulationCommands.addPlane s
// 4. Start simulation
SimulationCommands.play s
// 5. Watch it
StateDisplay.listBodies s
```

## Ready-Made Bodies

```fsharp
Presets.marble s ~position:(0.0, 3.0, 0.0)
Presets.crate s ~position:(1.0, 2.0, 0.0)
Presets.boulder s ~position:(-2.0, 10.0, 0.0)
```

## Random Generators

```fsharp
// 10 random spheres with seed for reproducibility
Generators.randomSpheres s 10 ~seed:42

// Stack of 5 boxes
Generators.stack s 5 ~position:(0.0, 0.0, 0.0)

// 3×4 grid of boxes
Generators.grid s 3 4

// Pyramid of 4 layers
Generators.pyramid s 4
```

## Steering

```fsharp
// Push a body east
Steering.push s "sphere-1" East 50.0

// Launch toward a target
Steering.launch s "sphere-1" (5.0, 0.0, 5.0) 20.0

// Spin a body
Steering.spin s "box-1" Up 10.0

// Stop a body
Steering.stop s "sphere-1"
```

## State Display

```fsharp
// List all bodies as a table
StateDisplay.listBodies s

// Inspect one body
StateDisplay.inspect s "sphere-1"

// Show simulation status
StateDisplay.status s

// Live watch (Ctrl+C to stop)
LiveWatch.watch s

// Watch with filter
LiveWatch.watch s ~minVelocity:1.0
LiveWatch.watch s ~bodyIds:["sphere-1"; "box-2"]
```

## Viewer Control

```fsharp
// Set camera
ViewCommands.setCamera s (10.0, 5.0, 10.0) (0.0, 0.0, 0.0)

// Zoom
ViewCommands.setZoom s 2.0

// Wireframe on/off
ViewCommands.wireframe s true
```

## Cleanup

```fsharp
// Remove all bodies
SimulationCommands.clearAll s

// Disconnect
Session.disconnect s
```
