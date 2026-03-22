# BepuFSharp Wrapper Extensions

**Date**: 2026-03-22 | **Branch**: `005-stride-bepu-integration`

## Current API Surface (already implemented)

- 8 shape types: Sphere, Box, Capsule, Cylinder, Triangle, ConvexHull, Compound, Mesh
- 10 constraint types: BallSocket, Hinge, Weld, DistanceLimit, DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine
- Body operations: addBody (dynamic), addKinematicBody, addStatic, removeBody
- Constraint operations: addConstraint, removeConstraint
- Queries: raycast (single hit), raycastAll (penetrating)
- Per-body material properties: MaterialProperties (Friction, MaxRecoveryVelocity, SpringFrequency, SpringDampingRatio)
- Per-body collision filtering: CollisionFilter (Group, Mask)
- Contact events: Began, Persisted, Ended callbacks
- Escape hatches: raw Simulation and BufferPool access

## New APIs Required

### Sweep Cast

```fsharp
// PhysicsWorld.fsi
val sweepCast:
    shape: PhysicsShape ->
    startPose: Pose ->
    direction: Vector3 ->
    maxDistance: float32 ->
    ?collisionMask: uint32 ->
    PhysicsWorld -> SweepHit option

type SweepHit = {
    BodyId: BodyId option
    StaticId: StaticId option
    Position: Vector3
    Normal: Vector3
    Distance: float32
}
```

### Overlap Query

```fsharp
// PhysicsWorld.fsi
val overlap:
    shape: PhysicsShape ->
    pose: Pose ->
    ?collisionMask: uint32 ->
    PhysicsWorld -> OverlapResult list

type OverlapResult = {
    BodyId: BodyId option
    StaticId: StaticId option
}
```

### Constraint Readback

```fsharp
// PhysicsWorld.fsi
val getConstraintDescription: ConstraintId -> PhysicsWorld -> ConstraintDesc option
val constraintExists: ConstraintId -> PhysicsWorld -> bool
val getConstraintBodies: ConstraintId -> PhysicsWorld -> (BodyHandle * BodyHandle) option
```

### Filtered Raycasting

```fsharp
// Extend existing raycast/raycastAll with optional collision mask
val raycast:
    origin: Vector3 ->
    direction: Vector3 ->
    maxDistance: float32 ->
    ?collisionMask: uint32 ->
    PhysicsWorld -> RayHit option

val raycastAll:
    origin: Vector3 ->
    direction: Vector3 ->
    maxDistance: float32 ->
    ?collisionMask: uint32 ->
    PhysicsWorld -> RayHit list
```

### Runtime Modification

```fsharp
// PhysicsWorld.fsi
val setCollisionFilter: BodyId -> CollisionFilter -> PhysicsWorld -> unit
val setStaticCollisionFilter: StaticId -> CollisionFilter -> PhysicsWorld -> unit
val setMaterial: BodyId -> MaterialProperties -> PhysicsWorld -> unit
```

### Stride.BepuPhysics Interop Module (new)

```fsharp
// StrideInterop.fsi (new module)
module BepuFSharp.StrideInterop

open Stride.BepuPhysics
open Stride.BepuPhysics.Definitions.Colliders

/// Convert BepuFSharp shape to Stride collider
val toStrideCollider: PhysicsShape -> ColliderBase

/// Convert Stride collider to BepuFSharp shape
val fromStrideCollider: ColliderBase -> PhysicsShape

/// Convert BepuFSharp material to Stride material properties
val toStrideMaterial: MaterialProperties -> Stride.BepuPhysics.Definitions.MaterialProperties

/// Convert Stride material to BepuFSharp material properties
val fromStrideMaterial: Stride.BepuPhysics.Definitions.MaterialProperties -> MaterialProperties

/// Convert BepuFSharp constraint desc to Stride constraint component parameters
val toStrideConstraint: ConstraintDesc -> ConstraintComponentParams

/// Convert BepuFSharp collision filter to Stride collision group
val toStrideCollisionGroup: CollisionFilter -> CollisionGroup
```

## Dependency Changes

- Add `Stride.BepuPhysics >= 4.3.0.2507` as a direct dependency
- This pulls in `Stride.Engine >= 4.3.0.2507` transitively
- BepuFSharp becomes a Stride-aware library (not standalone)
- Package version bumped to 0.2.0
