# Data Model: State Stream Bandwidth Optimization

**Feature**: 004-state-stream-optimization
**Date**: 2026-03-23

## Entities

### BodyPose (new — continuous stream)

Per-body data sent on every tick for dynamic bodies only.

| Field | Type | Description |
|-------|------|-------------|
| id | string | Body identifier |
| position | Vec3 | Current position |
| orientation | Vec4 | Current orientation quaternion |
| velocity | Vec3 | Linear velocity (omitted for clients that opt out) |
| angular_velocity | Vec3 | Angular velocity (omitted for clients that opt out) |

### TickState (new — replaces SimulationState on 60 Hz stream)

Lean per-tick message containing only continuous data.

| Field | Type | Description |
|-------|------|-------------|
| bodies | repeated BodyPose | Pose data for all dynamic bodies |
| time | double | Simulation time |
| running | bool | Whether simulation is playing |
| tick_ms | double | Physics step duration |
| serialize_ms | double | Serialization duration |
| query_responses | repeated QueryResponse | Pending query results (infrequent) |

### BodyProperties (new — semi-static, via property event stream)

Full semi-static properties for a body. Sent on creation, on change, and as backfill.

| Field | Type | Description |
|-------|------|-------------|
| id | string | Body identifier |
| shape | Shape | Body shape (including CachedShapeRef for complex shapes) |
| color | Color | Render color |
| mass | double | Body mass |
| is_static | bool | Whether body is static |
| motion_type | BodyMotionType | Dynamic, static, or kinematic |
| collision_group | uint32 | Collision group bitmask |
| collision_mask | uint32 | Collision mask bitmask |
| material | MaterialProperties | Friction, restitution, etc. |
| position | Vec3 | Pose (included for static bodies and motion type transitions) |
| orientation | Vec4 | Orientation (included for static bodies and motion type transitions) |

### PropertyEvent (new — server → client event stream)

Wrapper message for property/lifecycle events delivered via the bidirectional channel.

| Field | Type | Description |
|-------|------|-------------|
| body_created | BodyProperties | Body was added to simulation (oneof) |
| body_removed | string | Body ID was removed from simulation (oneof) |
| body_updated | BodyProperties | Semi-static properties changed (oneof) |
| constraints_snapshot | repeated ConstraintState | Full constraint set (sent on change) (oneof) |
| registered_shapes_snapshot | repeated RegisteredShapeState | Full registered shapes set (sent on change) (oneof) |
| new_meshes | repeated MeshGeometry | New mesh geometry definitions (oneof) |

### StateRequest (modified)

Extended with client field profile for velocity opt-out.

| Field | Type | Description |
|-------|------|-------------|
| exclude_velocity | bool | If true, tick stream omits velocity + angular velocity. Default: false (include velocity for backward compat). |

## Relationships

```
TickState (60 Hz stream)
  └── BodyPose[] — dynamic bodies only, pose + optional velocity

PropertyEvent (event stream via bidirectional channel)
  ├── BodyProperties — creation / update / backfill
  ├── body_removed (string) — removal
  ├── ConstraintState[] — constraint snapshots on change
  ├── RegisteredShapeState[] — shape registry snapshots on change
  └── MeshGeometry[] — new mesh definitions

Client Local State (reconstructed)
  └── Full Body = BodyPose (from tick) + BodyProperties (from cache)
```

## State Transitions

### Body Lifecycle

```
[Created] → PropertyEvent.body_created → client caches BodyProperties
                                       → body appears in TickState (if dynamic)

[Property Changed] → PropertyEvent.body_updated → client updates cache
                                                 → tick stream unaffected

[Dynamic → Static] → PropertyEvent.body_updated (motion_type=static, includes final pose)
                   → body stops appearing in TickState
                   → client uses cached pose

[Static → Dynamic] → PropertyEvent.body_updated (motion_type=dynamic)
                   → body starts appearing in TickState on next tick

[Removed] → PropertyEvent.body_removed → client deletes from cache
                                        → body stops appearing in TickState
```

### Client Connection Lifecycle

```
[Connect] → Subscribe to property event stream
          → Receive PropertyEvent backfill (all existing bodies + constraints + shapes)
          → Subscribe to tick stream (with StateRequest.include_velocity preference)
          → Begin receiving TickState at 60 Hz
          → Merge tick + cached properties for full state
```

## Data Volume Estimates (200 Dynamic Bodies, Steady State)

| Channel | Per-message | Frequency | Per-second |
|---------|-------------|-----------|------------|
| TickState (with velocity) | ~15 KB | 60 Hz | ~900 KB/s |
| TickState (without velocity, viewer) | ~11 KB | 60 Hz | ~660 KB/s |
| PropertyEvent (steady state) | 0 | On change | ~0 |
| PropertyEvent (backfill, 200 bodies) | ~40 KB | Once | Once |
| **Current SimulationState (baseline)** | **~50 KB** | **60 Hz** | **~3 MB/s** |
