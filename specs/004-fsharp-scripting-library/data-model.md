# Data Model: F# Scripting Library

**Feature**: 004-fsharp-scripting-library | **Date**: 2026-03-22

## Overview

The scripting library is stateless — it holds no data of its own. It operates on types defined by `PhysicsClient` (Session, IdGenerator) and `PhysicsSandbox.Shared.Contracts` (proto-generated: Vec3, SimulationCommand, AddBody, Shape, Sphere, Box, ApplyImpulse, ApplyTorque, BatchResponse). This document maps the type relationships the library bridges.

## Entities

### Session (from PhysicsClient)

The opaque handle representing a connection to the physics sandbox server. All library functions that interact with the simulation require a Session.

- **Source**: `PhysicsClient.Session.Session`
- **Lifecycle**: `connect` → use → `disconnect`
- **Thread safety**: Internally uses ConcurrentDictionary for body registry

### SimulationCommand (from Shared.Contracts)

Proto-generated command type. The library's command builders construct these from simple F# tuples.

- **Source**: `PhysicsSandbox.Shared.Contracts.SimulationCommand`
- **Variants used by library**: AddBody, ApplyImpulse, ApplyTorque
- **Builder pattern**: `(id, position, shape, mass) → SimulationCommand`

### Vec3 (from Shared.Contracts)

Proto-generated 3D vector. The library's `toVec3` converts F# tuples to this type.

- **Source**: `PhysicsSandbox.Shared.Contracts.Vec3`
- **Fields**: X (float), Y (float), Z (float)
- **Conversion**: `(float * float * float) → Vec3`

### BatchResponse (from Shared.Contracts)

Response from batch command execution. Used by `batchAdd` for error reporting.

- **Source**: `PhysicsSandbox.Shared.Contracts.BatchResponse`
- **Fields**: Results (seq of CommandResult with Success, Index, Message)

## Type Flow

```
Script author works with:
  F# tuples (float * float * float)     → positions, vectors
  string                                 → body IDs
  float                                  → mass, radius, time
  SimulationCommand list                 → batch operations

Library converts to:
  Vec3 (proto)                           → via toVec3
  SimulationCommand (proto)              → via makeSphereCmd, makeBoxCmd, etc.
  PhysicsClient API calls                → via resetSimulation, batchAdd, runFor

PhysicsClient sends via:
  gRPC (proto serialization)             → to PhysicsServer
```

## Module → Type Dependencies

| Module | Consumes | Produces |
|--------|----------|----------|
| Helpers | Result<'a, string> | 'a (unwrapped) |
| Vec3Builders | (float * float * float) | Vec3 |
| CommandBuilders | string, tuples, float | SimulationCommand |
| BatchOperations | Session, SimulationCommand list | unit (with error logging) |
| SimulationLifecycle | Session | unit |
| Prelude | (re-exports all above) | (re-exports all above) |
