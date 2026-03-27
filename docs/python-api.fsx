(**
---
title: Python API Reference
category: Reference
categoryindex: 3
index: 1
description: API reference for the Python prelude library — session management, body creation, physics, camera, queries, and more.
---
*)

(**
# Python API Reference

The Python prelude (`Scripting/demos_py/prelude.py`) provides a complete scripting API for
the physics sandbox over gRPC. It mirrors the F# scripting library and is used by all 22 Python
demo scripts.

## Requirements

```
pip install grpcio>=1.60.0 grpcio-tools>=1.60.0 protobuf>=4.25.0
```

## Quick Start

```python
from Scripting.demos_py.prelude import *

s = connect("http://localhost:5180")
reset_simulation(s)

bid = add_sphere(s, (0, 10, 0), radius=0.5, mass=1.0)
run_for(s, 3.0)
list_bodies(s)

disconnect(s)
```

Or as a standalone script using the runner helper:

```python
from Scripting.demos_py.prelude import *

def run(s):
    add_sphere(s, (0, 10, 0), radius=0.5, mass=1.0)
    run_for(s, 3.0)

run_standalone(run, "My Demo")
```

---

## Session Management

| Function | Description |
|----------|-------------|
| `connect(address="http://localhost:5180") → Session` | Open a gRPC channel and return a `Session` dataclass |
| `disconnect(session)` | Close the gRPC channel |
| `run_standalone(run_fn, name="Demo")` | Connect, run `run_fn(session)`, reset, disconnect. Reads address from `sys.argv[1]` |

### `Session` dataclass

| Field | Type | Description |
|-------|------|-------------|
| `channel` | `grpc.Channel` | The underlying gRPC channel |
| `stub` | `PhysicsHubStub` | The gRPC service stub |
| `address` | `str` | The server address |

---

## Simulation Control

| Function | Description |
|----------|-------------|
| `play(session)` | Start simulation |
| `pause(session)` | Pause simulation |
| `step(session, delta_time=1/60)` | Advance one step |
| `reset(session)` | Server-side reset |
| `set_gravity(session, (x, y, z))` | Set gravity vector |
| `run_for(session, seconds)` | Play for duration, then pause |
| `reset_simulation(session)` | Full reset: pause, reset, clear IDs, add ground plane, set gravity to (0, -9.81, 0) |

---

## Body Creation

All `add_*` functions return the body ID string.

### Primitive Shapes

| Function | Signature |
|----------|-----------|
| `add_sphere` | `(session, pos, radius, mass, body_id=None) → str` |
| `add_box` | `(session, pos, half_extents, mass, body_id=None) → str` |
| `add_capsule` | `(session, pos, radius, length, mass, body_id=None, color=None, material=None) → str` |
| `add_cylinder` | `(session, pos, radius, length, mass, body_id=None, color=None, material=None) → str` |
| `add_plane` | `(session, normal=(0,1,0), body_id=None) → str` |

### Command Builders

These return `pb.SimulationCommand` objects for use with `batch_add`.

| Function | Signature |
|----------|-----------|
| `make_sphere_cmd` | `(body_id, pos, radius, mass)` |
| `make_box_cmd` | `(body_id, pos, half_extents, mass)` |
| `make_capsule_cmd` | `(body_id, pos, radius, length, mass)` |
| `make_cylinder_cmd` | `(body_id, pos, radius, length, mass)` |
| `make_triangle_cmd` | `(body_id, pos, a, b, c, mass)` |
| `make_convex_hull_cmd` | `(body_id, pos, points, mass)` — `points` is a list of `(x,y,z)` tuples |
| `make_mesh_cmd` | `(body_id, pos, triangles, mass)` — `triangles` is a list of `((ax,ay,az), (bx,by,bz), (cx,cy,cz))` |
| `make_compound_cmd` | `(body_id, pos, children, mass)` — `children` is a list of `(pb.Shape, (x,y,z))` |

### Command Modifiers

These mutate and return the command for chaining.

| Function | Signature |
|----------|-----------|
| `with_color_and_material` | `(cmd, color=None, material=None) → cmd` |
| `with_motion_type` | `(cmd, motion_type) → cmd` — 0=Dynamic, 1=Kinematic, 2=Static |
| `with_collision_filter` | `(cmd, group, mask) → cmd` |

### Kinematic Bodies

| Function | Signature |
|----------|-----------|
| `make_kinematic_cmd` | `(body_id, pos, shape) → cmd` — Creates a kinematic body (mass=0, motion_type=1) |

### Body Manipulation

| Function | Signature |
|----------|-----------|
| `remove_body` | `(session, body_id)` |
| `clear_all` | `(session)` — Removes all dynamic bodies |
| `set_body_pose` | `(session, body_id, pos)` — Teleport a body |

---

## Forces & Impulses

| Function | Signature |
|----------|-----------|
| `apply_force` | `(session, body_id, (fx, fy, fz))` — Continuous force |
| `apply_impulse` | `(session, body_id, (ix, iy, iz))` — Instantaneous impulse |
| `apply_torque` | `(session, body_id, (tx, ty, tz))` — Rotational torque |
| `clear_forces` | `(session, body_id)` |

### Steering Helpers

| Function | Signature |
|----------|-----------|
| `push` | `(session, body_id, direction, magnitude)` — Impulse along a `Direction` enum value |
| `launch` | `(session, body_id, target_pos, speed)` — Launch body toward a target position |

### `Direction` Enum

| Value | Vector |
|-------|--------|
| `Direction.Up` | `(0, 1, 0)` |
| `Direction.Down` | `(0, -1, 0)` |
| `Direction.North` | `(0, 0, -1)` |
| `Direction.South` | `(0, 0, 1)` |
| `Direction.East` | `(1, 0, 0)` |
| `Direction.West` | `(-1, 0, 0)` |

---

## Constraints

| Function | Signature |
|----------|-----------|
| `make_ball_socket_cmd` | `(constraint_id, body_a, body_b, offset_a, offset_b)` |
| `make_hinge_cmd` | `(constraint_id, body_a, body_b, axis, offset_a, offset_b)` |
| `make_weld_cmd` | `(constraint_id, body_a, body_b)` |
| `make_distance_limit_cmd` | `(constraint_id, body_a, body_b, min_dist, max_dist)` |

All constraint functions return a `pb.SimulationCommand` for use with `batch_add` or `_send`.

---

## Camera & View Commands

### Camera Positioning

| Function | Signature |
|----------|-----------|
| `set_camera` | `(session, position, target)` — Instant camera move |
| `smooth_camera` | `(session, position, target, duration_seconds)` — Animated transition |
| `stop_camera` | `(session)` — Stop any active camera animation |

### Camera Tracking

| Function | Signature |
|----------|-----------|
| `look_at_body` | `(session, body_id, duration_seconds)` — Turn camera to face a body |
| `follow_body` | `(session, body_id)` — Continuous camera follow |
| `orbit_body` | `(session, body_id, duration_seconds, degrees=360)` — Orbit around a body |
| `chase_body` | `(session, body_id, offset)` — Chase with fixed offset |
| `frame_bodies` | `(session, body_ids)` — Zoom to fit multiple bodies |

### Camera Effects

| Function | Signature |
|----------|-----------|
| `shake_camera` | `(session, intensity, duration_seconds)` |

### Display

| Function | Signature |
|----------|-----------|
| `wireframe` | `(session, enabled)` — Toggle wireframe rendering |
| `set_zoom` | `(session, level)` |
| `set_narration` | `(session, text)` — Show overlay text |
| `clear_narration` | `(session)` |
| `set_demo_info` | `(session, name, description)` — Set viewer demo label |

---

## Queries

### Raycasting

```python
hits = query_raycast(session, origin=(0,10,0), direction=(0,-1,0), max_distance=1000)
# Returns: [(body_id, (px,py,pz), (nx,ny,nz), distance), ...]
```

### Overlap

```python
body_ids = query_overlap_sphere(session, radius=5.0, position=(0,5,0))
# Returns: ["sphere-1", "box-3", ...]
```

### Sweep

```python
result = query_sweep_sphere(session, radius=0.5, start=(0,10,0),
                            direction=(0,-1,0), max_distance=100)
# Returns: (body_id, (px,py,pz), (nx,ny,nz), distance) or None
```

---

## Batch Operations

| Function | Signature |
|----------|-----------|
| `batch_commands` | `(session, commands) → BatchResponse` — Send up to 100 simulation commands |
| `batch_view_commands` | `(session, commands) → BatchResponse` — Send up to 100 view commands |
| `batch_add` | `(session, commands)` — Auto-chunks at 100, logs failures |

Example:

```python
cmds = [make_sphere_cmd(next_id("s"), (x, 10, 0), 0.3, 1.0) for x in range(200)]
batch_add(s, cmds)  # Automatically split into 2 batches
```

---

## State Inspection

| Function | Signature |
|----------|-----------|
| `get_state` | `(session) → SimulationState \| None` — Full state (merges TickState + PropertySnapshot) |
| `list_bodies` | `(session)` — Pretty-print table of dynamic bodies (ID, shape, position, velocity) |
| `status` | `(session)` — Print summary: time, running, body count, tick/serialize ms |

---

## Body Presets

Convenience functions that create common body types with realistic dimensions and masses.

| Function | Shape | Radius/Size | Default Mass |
|----------|-------|-------------|--------------|
| `marble(s, pos, mass, body_id)` | Sphere | r=0.01 | 0.005 |
| `bowling_ball(s, pos, mass, body_id)` | Sphere | r=0.11 | 6.35 |
| `beach_ball(s, pos, mass, body_id)` | Sphere | r=0.2 | 0.1 |
| `boulder(s, pos, mass, body_id)` | Sphere | r=0.5 | 200.0 |
| `crate(s, pos, mass, body_id)` | Box | 0.5³ | 20.0 |
| `brick(s, pos, mass, body_id)` | Box | 0.2×0.1×0.05 | 3.0 |
| `die(s, pos, mass, body_id)` | Box | 0.05³ | 0.03 |

All parameters are optional with sensible defaults.

---

## Generators

Batch-create bodies in common arrangements. All return a `list[str]` of body IDs.

| Function | Signature | Description |
|----------|-----------|-------------|
| `stack` | `(session, count, pos=None)` | Vertical stack of 1m boxes |
| `pyramid` | `(session, layers, pos=None)` | Pyramid of boxes |
| `row` | `(session, count, pos=None)` | Horizontal row of spheres |
| `grid` | `(session, rows, cols, pos=None)` | 2D grid of boxes |
| `random_spheres` | `(session, count, seed=None)` | Random spheres with varied size/mass |

---

## Materials

### Presets

| Constant | Friction | Max Recovery | Spring Freq | Spring Damping |
|----------|----------|-------------|-------------|----------------|
| `BOUNCY_MATERIAL` | 0.4 | 8.0 | 60.0 | 0.5 |
| `STICKY_MATERIAL` | 2.0 | 0.5 | 30.0 | 1.0 |
| `SLIPPERY_MATERIAL` | 0.01 | 2.0 | 30.0 | 1.0 |

### Custom Materials

```python
mat = make_material(friction=1.0, max_recovery=2.0,
                    spring_freq=30.0, spring_damping=1.0)
```

---

## Colors

### Palette Constants

| Constant | RGB |
|----------|-----|
| `PROJECTILE_COLOR` | (1.0, 0.2, 0.1) |
| `TARGET_COLOR` | (0.3, 0.6, 1.0) |
| `STRUCTURE_COLOR` | (0.7, 0.7, 0.7) |
| `ACCENT_YELLOW` | (1.0, 0.8, 0.0) |
| `ACCENT_GREEN` | (0.2, 0.8, 0.3) |
| `ACCENT_PURPLE` | (0.8, 0.4, 1.0) |
| `ACCENT_ORANGE` | (1.0, 0.5, 0.0) |
| `KINEMATIC_COLOR` | (0.0, 1.0, 1.0) |

### Custom Colors

```python
color = make_color(r=1.0, g=0.5, b=0.0, a=1.0)
```

---

## Utility Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `to_vec3` | `(x, y, z) → pb.Vec3` | Create protobuf Vec3 |
| `make_color` | `(r, g, b, a=1.0) → pb.Color` | Create protobuf Color |
| `make_material` | `(friction, max_recovery, spring_freq, spring_damping) → pb.MaterialProperties` | Create material |
| `next_id` | `(prefix) → str` | Auto-increment ID: `"sphere"` → `"sphere-1"`, `"sphere-2"`, ... |
| `reset_ids` | `()` | Reset all ID counters |
| `sleep` | `(ms)` | Sleep in milliseconds |
| `timed` | `(label)` | Context manager that prints elapsed time |

### `timed` example

```python
with timed("create bodies"):
    batch_add(s, commands)
# prints: [TIME] create bodies: 42 ms
```

---

## Gotchas

- **Static mesh bodies** require explicit `with_motion_type(cmd, 2)` (Static). Default is Dynamic, and mass=0 + Dynamic is rejected.
- **Mesh triangles** should be ~2m+ per edge for reliable collision. Use heightmap grids instead of narrow strips.
- **Proto naming**: Use `pb.MeshShape(triangles=[pb.MeshTriangle(...)])` — not `pb.Triangle` (that's a separate shape type).
- **Batch limit**: Server enforces 100 commands per batch. Use `batch_add` for auto-chunking.
- **Stub generation**: Run `Scripting/demos_py/generate_stubs.sh` to regenerate protobuf bindings after proto changes.

---

## Next Steps

- [Getting Started](getting-started.html) — Build and run the sandbox
- [Demo Scripts](demo-scripts.html) — 22 physics demos in F# and Python
- [Scripting Library](scripting-library.html) — F# scripting API (equivalent functionality)
- [MCP Tools](mcp-tools.html) — 59 tools for AI-assisted debugging
*)
