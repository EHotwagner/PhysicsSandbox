(**
---
title: MCP Tools Reference
category: Reference
categoryindex: 5
index: 1
description: 38 MCP tools for AI-assisted physics simulation control.
---
*)

(**
# MCP Tools Reference

The Physics Sandbox MCP server exposes tools that AI assistants (Claude, GPT, etc.) can
call over the Model Context Protocol to control a live physics simulation. Tools are
registered automatically at startup via `WithToolsFromAssembly()` and communicate with
the physics server over gRPC.

This page documents every tool grouped by category.

## Summary Table

| # | Category | Tool | Description |
|---|----------|------|-------------|
| 1 | Simulation | `add_body` | Add a rigid body (sphere or box) to the simulation |
| 2 | Simulation | `apply_force` | Apply a continuous force to a body |
| 3 | Simulation | `apply_impulse` | Apply an instantaneous impulse to a body |
| 4 | Simulation | `apply_torque` | Apply a torque to a body |
| 5 | Simulation | `set_gravity` | Set the global gravity vector |
| 6 | Simulation | `step` | Advance the simulation by one time step |
| 7 | Simulation | `play` | Start continuous simulation |
| 8 | Simulation | `pause` | Pause the simulation |
| 9 | Simulation | `remove_body` | Remove a body from the simulation |
| 10 | Simulation | `clear_forces` | Clear all forces on a body |
| 11 | Simulation | `restart_simulation` | Reset the simulation to initial state |
| 12 | View | `set_camera` | Set the 3D viewer camera position and target |
| 13 | View | `set_zoom` | Set the 3D viewer zoom level |
| 14 | View | `toggle_wireframe` | Toggle wireframe rendering mode |
| 15 | Presets | `add_marble` | Add a marble (tiny sphere) |
| 16 | Presets | `add_bowling_ball` | Add a bowling ball (dense sphere) |
| 17 | Presets | `add_beach_ball` | Add a beach ball (large, lightweight sphere) |
| 18 | Presets | `add_crate` | Add a crate (1x1x1 box) |
| 19 | Presets | `add_brick` | Add a brick (flat rectangular box) |
| 20 | Presets | `add_boulder` | Add a boulder (heavy sphere) |
| 21 | Presets | `add_die` | Add a die (tiny cube) |
| 22 | Generators | `generate_random_bodies` | Generate a random mix of spheres and boxes |
| 23 | Generators | `generate_stack` | Generate a vertical stack of crates |
| 24 | Generators | `generate_row` | Generate a horizontal row of spheres |
| 25 | Generators | `generate_grid` | Generate a grid of crates on a plane |
| 26 | Generators | `generate_pyramid` | Generate a pyramid of crates |
| 27 | Steering | `push_body` | Push a body in a compass direction |
| 28 | Steering | `launch_body` | Launch a body toward a target position |
| 29 | Steering | `spin_body` | Spin a body around an axis |
| 30 | Steering | `stop_body` | Stop a body by cancelling its velocity |
| 31 | Batch | `batch_commands` | Submit multiple simulation commands in one call |
| 32 | Batch | `batch_view_commands` | Submit multiple view commands in one call |
| 33 | Query | `get_state` | Get the current simulation state |
| 34 | Query | `get_status` | Get MCP server connection health |
| 35 | Metrics | `get_metrics` | Get performance metrics from all services |
| 36 | Metrics | `get_diagnostics` | Get pipeline timing diagnostics |
| 37 | Audit | `get_command_log` | Get the recent command audit trail |
| 38 | Stress Test | `start_stress_test` | Start a background stress test scenario |
| 39 | Stress Test | `get_stress_test_status` | Get status and results of a stress test |
| 40 | Comparison | `start_comparison_test` | Run an MCP vs direct scripting comparison |

---

## Simulation Tools

Core simulation commands for adding bodies, applying forces, and controlling playback.
Defined in `SimulationTools.fs`.

### `add_body`

Add a rigid body (sphere or box) to the physics simulation at the specified position.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `shape` | string | *(required)* | Body shape: `"sphere"` or `"box"` |
| `radius` | float | 0.5 | Sphere radius (required if shape=sphere) |
| `half_extents_x` | float | 0.5 | Box half-extent X (required if shape=box) |
| `half_extents_y` | float | 0.5 | Box half-extent Y (required if shape=box) |
| `half_extents_z` | float | 0.5 | Box half-extent Z (required if shape=box) |
| `x` | float | 0.0 | Position X |
| `y` | float | 5.0 | Position Y |
| `z` | float | 0.0 | Position Z |
| `mass` | float | 1.0 | Body mass (0 = static) |

A unique body ID is auto-generated from the shape name (e.g., `sphere-1`, `box-2`).
*)

(*** do-not-eval ***)
// Example: add a sphere at position (0, 10, 0) with mass 2
// add_body shape="sphere" radius=0.5 x=0 y=10 z=0 mass=2

(**
### `apply_force`

Apply a continuous force vector to a body. The force persists across simulation steps
until cleared with `clear_forces`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Target body ID |
| `x` | float | 0.0 | Force X component |
| `y` | float | 0.0 | Force Y component |
| `z` | float | 0.0 | Force Z component |

### `apply_impulse`

Apply an instantaneous impulse to a body, immediately changing its velocity.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Target body ID |
| `x` | float | 0.0 | Impulse X component |
| `y` | float | 0.0 | Impulse Y component |
| `z` | float | 0.0 | Impulse Z component |

### `apply_torque`

Apply a rotational torque to a body around the specified axis vector.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Target body ID |
| `x` | float | 0.0 | Torque X component |
| `y` | float | 0.0 | Torque Y component |
| `z` | float | 0.0 | Torque Z component |

### `set_gravity`

Set the global gravity vector for the simulation.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `x` | float | 0.0 | Gravity X |
| `y` | float | -9.81 | Gravity Y |
| `z` | float | 0.0 | Gravity Z |

### `step`

Advance the physics simulation by a single time step. No parameters.

### `play`

Start continuous simulation playback, stepping automatically each frame. No parameters.

### `pause`

Pause continuous simulation playback, freezing all bodies in place. No parameters.

### `remove_body`

Remove a body from the simulation by its identifier.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID to remove |

### `clear_forces`

Clear all accumulated continuous forces on a body.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID |

### `restart_simulation`

Reset the entire simulation: removes all bodies, clears forces, and resets time to zero.
Performance metrics persist across restarts. No parameters.

---

## View Tools

Controls for the 3D Stride viewer: camera positioning, zoom, and wireframe rendering.
Defined in `ViewTools.fs`.

### `set_camera`

Set the 3D viewer camera position and look-at target.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pos_x` | float | 0.0 | Camera position X |
| `pos_y` | float | 10.0 | Camera position Y |
| `pos_z` | float | 20.0 | Camera position Z |
| `target_x` | float | 0.0 | Look-at target X |
| `target_y` | float | 0.0 | Look-at target Y |
| `target_z` | float | 0.0 | Look-at target Z |

### `set_zoom`

Set the 3D viewer zoom level.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `level` | float | *(required)* | Zoom level (1.0 = default) |

### `toggle_wireframe`

Toggle wireframe rendering mode on or off in the 3D viewer. No parameters.

---

## Preset Tools

Preset body types with realistic physical dimensions and masses.
Defined in `PresetTools.fs`. All presets share a common parameter set.

**Common parameters for all preset tools:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `x` | float | 0.0 | X position |
| `y` | float | 0.0 | Y position |
| `z` | float | 0.0 | Z position |
| `mass` | float | *(varies)* | Mass override (uses preset default if <= 0) |
| `id` | string | *(auto)* | Custom body ID (auto-generated if empty) |

### `add_marble`

Add a marble: a tiny sphere with radius 0.01 and default mass 0.005 kg.

### `add_bowling_ball`

Add a bowling ball: a dense sphere with radius 0.11 and default mass 6.35 kg.

### `add_beach_ball`

Add a beach ball: a large, lightweight sphere with radius 0.2 and default mass 0.1 kg.

### `add_crate`

Add a crate: a 1x1x1 box (0.5 half-extents) with default mass 20 kg.

### `add_brick`

Add a brick: a flat rectangular box (0.2x0.1x0.05 half-extents) with default mass 3 kg.

### `add_boulder`

Add a boulder: a heavy sphere with radius 0.5 and default mass 200 kg.

### `add_die`

Add a die: a tiny cube (0.05 half-extents) with default mass 0.03 kg.

---

## Generator Tools

Procedural body arrangement generators for quickly populating the simulation.
Defined in `GeneratorTools.fs`.

### `generate_random_bodies`

Generate a random mix of spheres and boxes with randomized positions, sizes, and masses
within a bounded volume.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | int | *(required)* | Number of bodies to create |
| `seed` | int | 0 | Random seed (0 for non-deterministic) |

### `generate_stack`

Generate a vertical stack of unit-sized crates at the specified base position, each
spaced 1 unit apart vertically.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | int | *(required)* | Number of crates in the stack |
| `x` | float | 0.0 | Base X position |
| `y` | float | 0.0 | Base Y position |
| `z` | float | 0.0 | Base Z position |

### `generate_row`

Generate a horizontal row of spheres along the X axis.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | int | *(required)* | Number of spheres in the row |
| `x` | float | 0.0 | Start X position |
| `y` | float | 0.0 | Y position |
| `z` | float | 0.0 | Z position |
| `spacing` | float | 0.5 | Spacing between sphere centers |

### `generate_grid`

Generate a 2D grid of crates on the XZ plane with 1-unit spacing between centers.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rows` | int | *(required)* | Number of rows |
| `cols` | int | *(required)* | Number of columns |
| `x` | float | 0.0 | Start X position |
| `y` | float | 0.5 | Y position |
| `z` | float | 0.0 | Start Z position |

### `generate_pyramid`

Generate a pyramid of crates with the widest layer at the base, narrowing by one crate
per layer.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `layers` | int | *(required)* | Number of layers |
| `x` | float | 0.0 | Base X position |
| `y` | float | 0.0 | Base Y position |
| `z` | float | 0.0 | Base Z position |

---

## Steering Tools

High-level body movement using compass directions and target-based launching.
Defined in `SteeringTools.fs`.

### `push_body`

Push a body with an impulse in one of six compass directions: `up`, `down`, `north`,
`south`, `east`, `west`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID to push |
| `direction` | string | *(required)* | Direction: up, down, north, south, east, west |
| `strength` | float | 10.0 | Impulse magnitude |

### `launch_body`

Launch a body toward a target position. Computes the direction vector from the body's
current position and applies a normalized impulse at the given speed.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID to launch |
| `target_x` | float | *(required)* | Target X |
| `target_y` | float | *(required)* | Target Y |
| `target_z` | float | *(required)* | Target Z |
| `speed` | float | 10.0 | Launch speed |

### `spin_body`

Apply a rotational torque to spin a body around a compass-direction axis.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID to spin |
| `axis` | string | *(required)* | Axis direction: up, down, north, south, east, west |
| `strength` | float | 10.0 | Torque magnitude |

### `stop_body`

Stop a body by clearing its forces and applying an opposing impulse proportional to its
current momentum to cancel its velocity.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_id` | string | *(required)* | Body ID to stop |

---

## Batch Tools

Batch command submission for reducing round-trip overhead. Accepts JSON arrays of
commands and sends them in a single gRPC call. Defined in `BatchTools.fs`.

### `batch_commands`

Submit multiple simulation commands in a single batch. Each element in the JSON array
must have a `type` field identifying the command.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `commands` | string | *(required)* | JSON array of commands |

**Supported command types:** `add_body`, `apply_force`, `apply_impulse`, `step`, `play`,
`pause`, `set_gravity`, `remove_body`, `clear_forces`, `reset`.
*)

(*** do-not-eval ***)
// Example JSON for batch_commands:
// [
//   {"type":"add_body","shape":"sphere","radius":0.5,"x":0,"y":5,"z":0,"mass":1},
//   {"type":"add_body","shape":"box","hx":0.5,"hy":0.5,"hz":0.5,"x":2,"y":5,"z":0,"mass":5},
//   {"type":"step"},
//   {"type":"step"}
// ]

(**
### `batch_view_commands`

Submit multiple view commands in a single batch.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `commands` | string | *(required)* | JSON array of view commands |

**Supported command types:** `set_camera`, `set_zoom`, `toggle_wireframe`.
*)

(*** do-not-eval ***)
// Example JSON for batch_view_commands:
// [
//   {"type":"set_camera","px":0,"py":10,"pz":20,"tx":0,"ty":0,"tz":0},
//   {"type":"set_zoom","level":1.5}
// ]

(**
---

## Query Tools

Tools for querying simulation state and server connection health.
Defined in `QueryTools.fs`.

### `get_state`

Get the current simulation state including all body positions, velocities, masses, and
shapes. Returns cached data from the background gRPC state stream. No parameters.

The output includes a formatted table of all bodies with their ID, position, velocity,
mass, and shape.

### `get_status`

Get MCP server connection status and health. Reports the state of the gRPC state stream,
view command stream, and audit stream, along with data staleness. No parameters.

---

## Metrics Tools

Performance monitoring tools for message counts, data volumes, and pipeline timing.
Defined in `MetricsTools.fs`.

### `get_metrics`

Fetch performance metrics from all services (Server, Simulation, Viewer, MCP) including
message counts, byte volumes, and pipeline timing breakdowns. No parameters.

### `get_diagnostics`

Fetch pipeline diagnostics showing the timing breakdown across four stages: simulation
tick, serialization, gRPC transfer, and rendering. Highlights the slowest bottleneck
stage. No parameters.

---

## Audit Tools

Command audit trail for debugging and replay. Defined in `AuditTools.fs`.

### `get_command_log`

Retrieve the most recent command events from the audit stream, formatted with
human-readable descriptions of each simulation or view command.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | int | 20 | Maximum number of entries to return |

---

## Stress Test Tools

Background stress tests for measuring simulation performance limits.
Defined in `StressTestTools.fs`.

### `start_stress_test`

Start a background stress test. Returns a test ID that can be polled with
`get_stress_test_status`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `scenario` | string | *(required)* | Scenario: `"body-scaling"` or `"command-throughput"` |
| `max_bodies` | int | 500 | Maximum bodies for body-scaling scenario |
| `duration_seconds` | int | 30 | Duration for command-throughput scenario |

**Scenarios:**

- **body-scaling** -- Adds bodies incrementally until performance degrades, measuring the
  simulation's body count limit.
- **command-throughput** -- Measures the maximum command processing rate over a fixed
  duration.

### `get_stress_test_status`

Get the status, progress, and results of a running or completed stress test.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `test_id` | string | *(required)* | Test ID returned by `start_stress_test` |

---

## Comparison Tools

Performance comparison between direct gRPC scripting and MCP command paths.
Defined in `ComparisonTools.fs`.

### `start_comparison_test`

Run a three-phase comparison test that executes the same workload (add N bodies, step M
times) via direct gRPC calls and batched MCP calls, measuring timing differences and
overhead. Returns a test ID -- poll with `get_stress_test_status`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `body_count` | int | 100 | Number of bodies to add in each path |
| `step_count` | int | 60 | Number of simulation steps in each path |

---

## See Also

- [Architecture Overview](architecture.html) -- system design and gRPC message flow
- [Demo Scripts](demo-scripts.html) -- F# and Python demo scripts that exercise these tools
- [Getting Started](getting-started.html) -- build, run, and connect to the sandbox
*)
