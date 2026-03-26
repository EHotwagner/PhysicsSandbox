(**
---
title: "Release: Stride BepuPhysics Integration"
category: Overview
categoryindex: 1
index: 3
description: What's new in the 005-stride-bepu-integration release — 10 shapes, constraints, queries, debug viz.
---
*)

(**
# Release: Stride BepuPhysics Integration

The `005-stride-bepu-integration` release is the largest expansion of Physics Sandbox to date.
It upgrades the underlying physics wrapper from BepuFSharp 0.1.0 to **0.2.0-beta.1**, adds
seven new shape types, ten constraint types, physics queries, per-body color and material
properties, collision layer filtering, kinematic bodies, and a debug wireframe overlay in the
Stride3D viewer.

---

## What's New

### Extended Shape Support (10 Types)

The original sandbox shipped with three shapes: sphere, box, and plane. This release brings the
total to **ten**, matching the full set exposed by BepuFSharp 0.2.0:

| Shape | Notes |
|-------|-------|
| Sphere | Unchanged from prior releases |
| Box | Unchanged from prior releases |
| Plane | Approximated as large static box (BepuPhysics2 has no infinite plane) |
| **Capsule** | Radius + length, aligned along local Y axis |
| **Cylinder** | Radius + length |
| **Triangle** | Three arbitrary vertices |
| **Convex Hull** | Arbitrary point cloud; minimum 4 points required |
| **Compound** | Composed of multiple child shapes with local offsets |
| **Mesh** | Triangle mesh; minimum 1 triangle required |
| **Shape Reference** | Re-uses a previously registered shape by handle |

<div class="alert alert-info">

**Shape registration and caching** — Vertex-heavy shapes (convex hull, compound, mesh) are
registered once via `RegisterShape` and then referenced by handle in subsequent `AddBody`
commands. This avoids re-transmitting large vertex buffers on every body creation. Registered
shapes are included in every state stream update so that late-joining clients always receive the
full shape cache.

</div>

Shape validation is enforced at creation time:

- Convex hull requires at least 4 points
- Compound requires at least 1 child shape
- Mesh requires at least 1 triangle
*)

(*** do-not-eval ***)
// Example: adding a capsule body
let capsuleCmd =
    makeCapsuleCmd "capsule-1" (0.0, 5.0, 0.0) 0.3 1.0 2.0
//  id               position          radius length mass

// Example: registering a convex hull shape, then spawning bodies from it
let regCmd = registerConvexHull "hull-shape-1" points
let bodyCmd = makeShapeRefCmd "hull-body-1" (0.0, 10.0, 0.0) "hull-shape-1" 3.0

(**
### 10 Constraint Types

Constraints connect pairs of bodies and restrict their relative motion. All ten constraint types
supported by BepuFSharp 0.2.0 are now available:

| Constraint | Description |
|------------|-------------|
| **Ball Socket** | Spherical joint — bodies share a pivot point but rotate freely |
| **Hinge** | Single-axis rotation around a shared axis |
| **Weld** | Rigid connection — no relative motion |
| **Distance Limit** | Enforces min/max distance between attachment points |
| **Distance Spring** | Spring force that pulls bodies toward a target separation |
| **Swing Limit** | Limits the angular swing cone between two bodies |
| **Twist Limit** | Limits relative twist rotation around a shared axis |
| **Linear Axis Motor** | Drives relative motion along a linear axis |
| **Angular Motor** | Drives relative angular velocity around an axis |
| **Point-on-Line** | Constrains one body's attachment point to a line defined on another |

Constraints are **automatically removed** when either connected body is deleted, preventing
dangling references. In the debug wireframe overlay (see below), constraints render as
color-coded lines between their anchor points.
*)

(*** do-not-eval ***)
// Example: connecting two bodies with a ball-socket joint
open PhysicsSandbox.Scripting.ConstraintBuilders

let joint = makeBallSocket "body-a" "body-b" (0.0, 5.0, 0.0)
sendCommand s joint

// Hinge with explicit axis
let hinge = makeHinge "body-a" "body-b" (0.0, 5.0, 0.0) (0.0, 1.0, 0.0)
sendCommand s hinge

(**
### Per-Body Color and Material Properties

Every body now carries an **RGBA color** (0.0 -- 1.0 per channel) that the Stride3D viewer uses
for rendering. When no color is specified, a **default palette** assigns colors by shape type:

| Shape | Default Color |
|-------|---------------|
| Sphere | Blue |
| Box | Orange |
| Capsule | Green |
| Cylinder | Purple |
| Triangle | Yellow |
| Convex Hull | Cyan |
| Compound | Magenta |
| Mesh | Red |

Bodies also accept **material properties** that control contact behavior:

| Property | Description | Default |
|----------|-------------|---------|
| `friction` | Coulomb friction coefficient | 1.0 |
| `max_recovery_velocity` | Maximum speed for penetration recovery | 2.0 |
| `spring_frequency` | Contact spring frequency (Hz) | 30.0 |
| `spring_damping_ratio` | Contact spring damping ratio | 1.0 |

Three **material presets** are available for common scenarios:
*)

(*** do-not-eval ***)
// Bouncy: high restitution, low friction
let bouncyBall = makeSphereCmd "bouncy-1" (0.0, 10.0, 0.0) 0.5 1.0
                 |> withMaterial Bouncy

// Sticky: high friction, low restitution
let stickyBox = makeBoxCmd "sticky-1" (3.0, 5.0, 0.0) (1.0, 1.0, 1.0) 2.0
                |> withMaterial Sticky

// Slippery: near-zero friction
let iceCube = makeBoxCmd "ice-1" (-3.0, 5.0, 0.0) (0.8, 0.8, 0.8) 1.0
              |> withMaterial Slippery

(**
### Physics Queries

Three query types allow scripts and tools to interrogate the physics world without modifying it:

**Raycast** — cast an infinite ray from an origin along a direction and find what it hits.
Both single-hit (nearest) and all-hits variants are available.

**Sweep Cast** — cast a shape (e.g., a sphere) along a path and report the first intersection.
Useful for predictive collision checks ("will this object fit through that gap?").

**Overlap** — test whether a volume (positioned shape) intersects any existing bodies.
Useful for spawn-point validation and trigger regions.
*)

(*** do-not-eval ***)
open PhysicsSandbox.Scripting.QueryBuilders

// Single raycast: origin, direction, max distance
let hit = raycast s (0.0, 10.0, 0.0) (0.0, -1.0, 0.0) 100.0

// Sweep a sphere along a path
let sweepHit = sweepSphere s 0.5 (0.0, 10.0, 0.0) (0.0, -1.0, 0.0) 50.0

// Test whether a sphere at a position overlaps any bodies
let overlapping = overlapSphere s 1.0 (0.0, 5.0, 0.0)

(**
<div class="alert alert-info">

All three query types have **batch variants** for bulk interrogation. Batch queries are
processed in a single simulation step, avoiding the overhead of multiple round-trips. Queries
also respect **collision mask filtering** — you can restrict results to specific layer groups.

</div>

### Collision Layers

Bodies can be assigned to collision layers using a **32-bit group/mask bitmask**. Two bodies
only interact when their masks overlap (`(a.group & b.mask) != 0 && (b.group & a.mask) != 0`).
This enables scenarios like non-interacting ghost objects, player-only triggers, and
category-based filtering.
*)

(*** do-not-eval ***)
// Assign body to layer 1 (group=1, mask=all)
setCollisionFilter s "body-1" 0x0001u 0xFFFFFFFFu

// Assign body to layer 2, only colliding with layer 2
setCollisionFilter s "body-2" 0x0002u 0x0002u

// These two bodies will pass through each other

(**
The `SetCollisionFilter` command can be issued at any time — filters are applied immediately
to existing bodies.

### Kinematic Bodies

Kinematic bodies are **unaffected by gravity and forces** but still collide with and push dynamic
bodies. They are ideal for moving platforms, animated obstacles, and scripted motion paths.
*)

(*** do-not-eval ***)
// Create a kinematic platform
let platform =
    makeBoxCmd "platform-1" (0.0, 2.0, 0.0) (4.0, 0.3, 4.0) 0.0
    |> withKinematic true

sendCommand s platform

// Animate it by updating its pose over time
for i in 0..100 do
    let y = 2.0 + sin (float i * 0.1) * 3.0
    setBodyPose s "platform-1" (0.0, y, 0.0)
    sleep 50

(**
### Debug Wireframe Visualization

Press **F3** in the Stride3D viewer to toggle the debug wireframe overlay. When active, it
renders:

- **Collider outlines** for every body, matching the actual physics shape (not the visual mesh)
- **Constraint connection lines** between anchor points, color-coded by constraint type
- **Query visualizations** for the most recent raycast, sweep, or overlap (when issued from the
  REPL or MCP)

The wireframe renderer uses `DebugRenderer.fs` and `ShapeGeometry.fs`, which generate line
primitives for each of the ten shape types.

### Client Interface Updates

All three client interfaces received new commands and tools:

<details>
<summary><strong>REPL Client</strong> — new interactive commands</summary>

| Command | Description |
|---------|-------------|
| `raycast` | Cast a ray and print the nearest hit |
| `sweepCast` | Sweep a shape along a path |
| `overlap` | Test volume intersection |
| `setBodyPose` | Update a kinematic body's position/orientation |
| `addConstraint` | Create a constraint between two bodies |

</details>

<details>
<summary><strong>MCP Server</strong> — expanded to 59 tools total</summary>

| Tool | Description |
|------|-------------|
| `sweep_cast` | Sweep a shape along a direction |
| `overlap` | Test for overlapping bodies at a position |
| `set_body_pose` | Move a kinematic body |
| `add_constraint` | Add a constraint between two bodies |
| `register_shape` | Register a reusable shape (hull, mesh, compound) |
| `set_collision_filter` | Set group/mask collision filter on a body |

</details>

<details>
<summary><strong>Scripting Library</strong> — 2 new modules</summary>

| Module | Key Functions |
|--------|---------------|
| `QueryBuilders` | `raycast`, `sweepSphere`, `overlapSphere` |
| `ConstraintBuilders` | `makeBallSocket`, `makeHinge`, `makeWeld`, `makeDistanceLimit` |

</details>

### BepuFSharp 0.2.0-beta.1

The underlying physics wrapper was upgraded from 0.1.0 to **0.2.0-beta.1**. Key additions:

- Sweep cast and overlap query APIs
- Filtered raycast (respects collision masks)
- Constraint readback (query active constraints on a body)
- Runtime collision filter and material property modification
- 10 shape types and 10 constraint types (up from 3 shapes and 0 constraints)

The local NuGet package is available at `~/.local/share/nuget-local/`.

---

## Known Drift and Limitations

### Resolved Since Release

<div class="alert alert-info">

**Complex shape rendering** — Triangle, Mesh, and Convex Hull shapes now render with actual
geometry (custom vertex/index buffers) in both solid and wireframe views. Compound shapes
decompose into individually-rendered children. All 10 shape types render with accurate
collision-matching geometry.

</div>

<div class="alert alert-warning">

**Scripting constraint builders** — 4 of 10 constraint types have convenience builders in the
`ConstraintBuilders` module (BallSocket, Hinge, Weld, DistanceLimit). The remaining 6
(DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine) require
manual protobuf message construction via the `PhysicsSandbox.Shared.Contracts` types.

</div>

### Shape Caching Behavior

Registered shapes are included in **every** state stream update. The original spec called for
"first use only" transmission, but the current implementation always includes the full shape
cache. This is simpler and ensures late-joining clients (e.g., a viewer started after simulation
is running) always receive complete shape data. The trade-off is slightly larger state messages
when many shapes are registered.

---

## Test Coverage

This release adds **46 new tests** specifically for extended features, bringing the total to
over 225 tests across five test projects:

| Project | Tests | Focus |
|---------|------:|-------|
| PhysicsSimulation.Tests | 114 | Shape creation, constraints, queries, kinematic bodies, collision filters |
| PhysicsClient.Tests | 78 | Command building, REPL parsing, color/material handling |
| PhysicsViewer.Tests | 99 | Scene management, shape geometry, debug renderer |
| PhysicsServer.Tests | 48 | Message routing, state fan-out, constraint cleanup |
| PhysicsSandbox.Scripting.Tests | 26 | Surface area, builder correctness, query helpers |
| PhysicsSandbox.Mcp.Tests | 18 | MCP tool correctness, deserialization |
| PhysicsSandbox.Integration.Tests | 84 | End-to-end Aspire integration tests |
| **Total** | **467** | |

All tests run headless with `StrideCompilerSkipBuild=true`:
*)

(*** do-not-eval ***)
// dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

(**
---

## Links

- [BepuPhysics2](https://github.com/bepu/bepuphysics2) — underlying physics engine
- [Stride3D](https://www.stride3d.net/) — 3D visualization engine
- [Architecture Overview](architecture.html) — how the services fit together
- [API Reference](reference/index.html) — full API documentation
- [Scripting Library](scripting-library.html) — convenience functions for F# scripts
- [Demo Scripts](demo-scripts.html) — 22 annotated demos in F# and Python

*)
