# Data Model: Static Mesh Terrain Demos

**Branch**: `004-mesh-terrain-demos` | **Date**: 2026-03-25

## Entities

This feature introduces no new persistent entities or data structures. All geometry is generated procedurally at runtime within demo scripts. The following describes the conceptual model used by the scripts.

### Terrain Mesh (runtime, per-demo)

A single static body composed of mesh triangles.

| Attribute | Type | Description |
|-----------|------|-------------|
| id | string | Unique body ID (e.g., "terrain-1") |
| position | Vec3 | World position (typically origin) |
| mass | float | Always 0.0 (static) |
| triangles | list of (Vec3, Vec3, Vec3) | Triangle vertices in world-relative coordinates |
| color | Color | Terrain color (distinct from dynamic objects) |
| material | MaterialProperties | Friction/restitution for surface interaction |

### Dynamic Object (runtime, per-demo)

Balls and other shapes dropped onto terrain.

| Attribute | Type | Description |
|-----------|------|-------------|
| id | string | Unique body ID (e.g., "ball-1") |
| position | Vec3 | Starting position (above terrain) |
| mass | float | Positive value (1.0-5.0 typical) |
| shape | Shape | Sphere, Capsule, or Cylinder |
| color | Color | Distinct from terrain color |

### Rollercoaster Track Geometry

The track is generated from a parametric path curve sampled at discrete points. At each sample point, a cross-section (flat floor + optional side walls) is generated and connected to the next sample's cross-section via triangle pairs.

- **Path**: `(x(t), y(t), z(t))` for t in [0..N] with elevation changes (sine wave hills, linear drops)
- **Cross-section**: 3-strip wide (left wall, floor, right wall), each strip = 2 triangles between segments
- **Total triangles**: ~90-120 (15-20 segments x 6 triangles/segment)

### Halfpipe Geometry

The halfpipe is a semicircular arc cross-section extruded along a straight axis.

- **Arc**: Semicircle discretized into 8-12 strips
- **Extrusion**: 10-15 segments along the length
- **Each segment pair**: 2 triangles per strip = 16-24 triangles per segment pair
- **Total triangles**: ~160-360

## Relationships

```
Demo Script
  └── creates → Terrain Mesh (1 static body with N triangles)
  └── creates → Dynamic Objects (5-8 bodies with positive mass)
  └── controls → Camera (smooth transitions, follow, orbit)
  └── displays → Narration (text overlay describing action)
```

## State Transitions

No persistent state transitions. Runtime flow per demo:

1. `resetSimulation` → clean slate with ground plane
2. Build terrain mesh → static body created on server
3. Drop dynamic objects → bodies created and simulated
4. `runFor` phases → simulation advances, camera follows action
5. `resetSimulation` at exit → cleanup
