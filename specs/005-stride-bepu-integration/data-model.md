# Data Model: Stride BepuPhysics Integration

**Date**: 2026-03-22 | **Branch**: `005-stride-bepu-integration`

## Entities

### Shape (extended)

Geometry definition for collision detection. Extended from 3 to 10 types.

| Field | Type | Notes |
|-------|------|-------|
| type | enum | Sphere, Box, Plane, Capsule, Cylinder, Triangle, ConvexHull, Compound, MeshShape, ShapeReference |
| radius | float | Sphere, Capsule, Cylinder |
| half_extents | Vec3 | Box |
| normal | Vec3 | Plane |
| length | float | Capsule, Cylinder |
| vertices | Vec3[] | Triangle (3), ConvexHull (N) |
| children | CompoundChild[] | Compound |
| triangles | MeshTriangle[] | MeshShape |
| shape_handle | string | ShapeReference (pointer to registered shape) |

**Validation**: ConvexHull requires >= 4 non-coplanar points. Compound requires >= 1 child. MeshShape requires >= 1 triangle. Radius/length must be > 0.

### CompoundChild

Sub-shape within a compound shape.

| Field | Type | Notes |
|-------|------|-------|
| shape | Shape | Child shape definition |
| local_position | Vec3 | Offset from compound origin |
| local_orientation | Vec4 | Quaternion rotation from compound origin |

### RegisteredShape

Server-side cache entry for vertex-heavy shapes.

| Field | Type | Notes |
|-------|------|-------|
| handle | string | Client-assigned unique identifier |
| shape | Shape | Full shape definition |
| bepu_shape_id | ShapeId | Internal BepuPhysics2 shape handle |

**Lifecycle**: Created by `RegisterShape` command. Referenced by `ShapeReference` in `AddBody`. Cleared on `ResetSimulation`. Can be explicitly removed via `UnregisterShape`.

### Body (extended)

Physics body in the simulation world. Extended with color, motion type, material, collision filtering.

| Field | Type | Notes |
|-------|------|-------|
| id | string | Client-assigned unique identifier |
| position | Vec3 | World-space position |
| velocity | Vec3 | Linear velocity |
| angular_velocity | Vec3 | Angular velocity |
| orientation | Vec4 | Quaternion |
| mass | float | 0 = static (legacy), > 0 = dynamic |
| shape | Shape | Geometry definition |
| motion_type | enum | DYNAMIC (default), KINEMATIC, STATIC |
| material | MaterialProperties | Optional, defaults applied if absent |
| color | Color | Optional RGBA, auto-assigned by shape type if absent |
| collision_group | uint32 | Bit index (0-31), default 1 |
| collision_mask | uint32 | Bitmask, default 0xFFFFFFFF |
| is_static | bool | Legacy field, retained for backward compat |

**State transitions**:
- Created → Active (simulation running)
- Active → Removed (RemoveBody command)
- Motion type is immutable after creation

### MaterialProperties

Per-body surface interaction properties.

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| friction | float | 1.0 | Coulomb friction coefficient |
| max_recovery_velocity | float | 2.0 | Penetration recovery speed (higher = more bounce) |
| spring_frequency | float | 30.0 | Contact spring stiffness (Hz) |
| spring_damping_ratio | float | 1.0 | 1.0 = critically damped (no bounce), < 1.0 = bouncy |

**Per-pair resolution**: friction = min(A, B), max_recovery_velocity = max(A, B), spring values = average(A, B).

### Color

Per-body visual appearance.

| Field | Type | Range | Notes |
|-------|------|-------|-------|
| r | float | 0.0–1.0 | Red channel |
| g | float | 0.0–1.0 | Green channel |
| b | float | 0.0–1.0 | Blue channel |
| a | float | 0.0–1.0 | Alpha (1.0 = opaque) |

**Defaults by shape type**: Sphere = blue, Box = orange, Capsule = green, Cylinder = yellow, Plane = gray, Triangle = cyan, ConvexHull = purple, Compound = white, MeshShape = teal.

### Constraint

Physics relationship between two bodies.

| Field | Type | Notes |
|-------|------|-------|
| id | string | Client-assigned unique identifier |
| body_a | string | First body ID |
| body_b | string | Second body ID |
| type | ConstraintType | Polymorphic constraint parameters |

**Lifecycle**: Created by `AddConstraint`. Removed by `RemoveConstraint` or automatically when either referenced body is removed. Cleared on `ResetSimulation`.

### ConstraintType (discriminated union)

| Variant | Key Parameters |
|---------|---------------|
| BallSocket | local_offset_a, local_offset_b, spring |
| Hinge | local_hinge_axis_a/b, local_offset_a/b, spring |
| Weld | local_offset, local_orientation, spring |
| DistanceLimit | local_offset_a/b, min_distance, max_distance, spring |
| DistanceSpring | local_offset_a/b, target_distance, spring |
| SwingLimit | axis_local_a/b, max_swing_angle, spring |
| TwistLimit | local_axis_a/b, min_angle, max_angle, spring |
| LinearAxisMotor | local_offset_a/b, local_axis, target_velocity, motor |
| AngularMotor | target_velocity (Vec3), motor |
| PointOnLine | local_origin, local_direction, local_offset, spring |

### SpringSettings

Shared spring configuration for constraints.

| Field | Type | Notes |
|-------|------|-------|
| frequency | float | Oscillation frequency (Hz) |
| damping_ratio | float | 0–1 typical, > 1 overdamped |

### MotorConfig

Shared motor configuration for motor constraints.

| Field | Type | Notes |
|-------|------|-------|
| max_force | float | Maximum force the motor can apply |
| damping | float | Velocity damping |

### Physics Query Results

#### RayHit

| Field | Type | Notes |
|-------|------|-------|
| body_id | string | Hit body's user-facing ID |
| position | Vec3 | World-space hit point |
| normal | Vec3 | Surface normal at hit |
| distance | float | Distance along ray |

## Relationships

```
Body --[has]--> Shape (1:1)
Body --[has]--> MaterialProperties (1:1, optional)
Body --[has]--> Color (1:1, optional)
Body --[belongs to]--> CollisionGroup (1:1)
Constraint --[connects]--> Body (2:1, body_a + body_b)
Constraint --[has]--> ConstraintType (1:1)
RegisteredShape --[referenced by]--> ShapeReference (1:N)
```

## Uniqueness Rules

- Body IDs: unique within a simulation session
- Constraint IDs: unique within a simulation session
- RegisteredShape handles: unique within a simulation session
- All cleared on simulation reset
