# Public API Contract: PhysicsSandbox.Scripting

**Feature**: 004-fsharp-scripting-library | **Date**: 2026-03-22

## Overview

This document defines the public API surface of the `PhysicsSandbox.Scripting` library. All symbols listed here will be verified by `SurfaceAreaTests.fs` via reflection. The `.fsi` signature files are the compiler-enforced source of truth; this document serves as the planning-phase contract.

## Module: PhysicsSandbox.Scripting.Helpers

```fsharp
val ok : Result<'a, string> -> 'a
val sleep : int -> unit
val timed : string -> (unit -> 'a) -> 'a
```

## Module: PhysicsSandbox.Scripting.Vec3Builders

```fsharp
val toVec3 : (float * float * float) -> Vec3
```

## Module: PhysicsSandbox.Scripting.CommandBuilders

```fsharp
val makeSphereCmd : id: string -> pos: (float * float * float) -> radius: float -> mass: float -> SimulationCommand
val makeBoxCmd : id: string -> pos: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> SimulationCommand
val makeImpulseCmd : bodyId: string -> impulse: (float * float * float) -> SimulationCommand
val makeTorqueCmd : bodyId: string -> torque: (float * float * float) -> SimulationCommand
```

## Module: PhysicsSandbox.Scripting.BatchOperations

```fsharp
val batchAdd : Session -> SimulationCommand list -> unit
```

## Module: PhysicsSandbox.Scripting.SimulationLifecycle

```fsharp
val resetSimulation : Session -> unit
val runFor : Session -> float -> unit
val nextId : string -> string
```

## Module: PhysicsSandbox.Scripting.Prelude (AutoOpen)

Re-exports all symbols from the above modules. When a script opens `PhysicsSandbox.Scripting.Prelude`, all functions are available without qualification.

## Dependency Re-exports

The library's compiled output directory will contain (via transitive project references):
- `PhysicsSandbox.Scripting.dll` (this library)
- `PhysicsClient.dll`
- `PhysicsSandbox.Shared.Contracts.dll`
- `PhysicsSandbox.ServiceDefaults.dll`
- gRPC/Protobuf runtime assemblies

Scripts reference only `PhysicsSandbox.Scripting.dll`; all dependencies resolve from the same directory.

## Versioning

Initial version: 0.1.0 (aligned with PhysicsClient). Public API changes require SurfaceAreaTests baseline update per Constitution Principle V.
