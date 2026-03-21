# Data Model: Stress Test Demos

**Feature**: 003-stress-test-demos
**Date**: 2026-03-21

## Entities

This feature adds no persistent data or new entity types. All demos use the existing `Demo` record type from AllDemos.fsx:

```
Demo
├── Name: string         — Display name for the runner
├── Description: string  — One-line description shown before execution
└── Run: Session -> unit — Demo execution function
```

## New Helper

```
timed: string -> (unit -> 'a) -> 'a
```

Added to Prelude.fsx. Takes a label and a function, executes the function, prints elapsed time in `[TIME] label: N ms` format, returns the function's result.

## Body Configurations Per Demo

| Demo | Bodies | Types | Layout |
|------|--------|-------|--------|
| 11 - Body Scaling | 50/100/200/500 (per tier) | Spheres | Grid, reset between tiers |
| 12 - Collision Pit | 100-150 spheres + 4 static walls | Spheres, Boxes (static) | Confined pit, drop from above |
| 13 - Force Frenzy | 100 | Spheres | Grid, 3 rounds of bulk impulses |
| 14 - Domino Cascade | 100+ | Boxes (domino-shaped) | Curved path |
| 15 - Overload | 100+ (pyramid + random) | Mixed | Combined formations |

## Existing Entities Used (No Changes)

- **Session**: gRPC connection to PhysicsServer
- **SimulationCommand**: Proto command (AddBody, ApplyImpulse, ApplyTorque, etc.)
- **BatchSimulationRequest**: Collection of up to 100 SimulationCommands
- **Vec3**: 3D vector (position, force, etc.)
- **Shape**: Sphere or Box geometry
