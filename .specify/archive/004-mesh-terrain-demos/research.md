# Research: Static Mesh Terrain Demos

**Branch**: `004-mesh-terrain-demos` | **Date**: 2026-03-25

## R1: Static Mesh Body Creation

**Decision**: Use `makeMeshCmd` with `mass=0.0` to create static mesh terrain bodies.

**Rationale**: The existing `makeMeshCmd` helper in Prelude.fsx (line 249) accepts a mass parameter. When mass=0.0, BepuPhysics2 creates a static body that is immovable — exactly what terrain requires. This is the same pattern used for static box obstacles in Demo 21 (mass=0.0 boxes).

**Alternatives considered**:
- Kinematic bodies (mass=0, MotionType=Kinematic): Overkill — kinematic is for bodies that move via scripting. Static terrain doesn't need pose updates.
- Multiple smaller mesh bodies: More complexity for no benefit. A single mesh body with all triangles is simpler and performs the same.

## R2: Procedural Triangle Generation for Curved Surfaces

**Decision**: Generate mesh triangles procedurally using parametric math in the demo script itself.

**Rationale**: The rollercoaster track and halfpipe both require curved surfaces that can be expressed as parametric functions. Generating triangles in-script keeps the demo self-contained (no external mesh files) and allows easy tuning of segment counts, dimensions, and curvature.

**Approach**:
- Rollercoaster: Define a path curve `(x(t), y(t), z(t))` for t in [0..1], sample at N points, generate a cross-section (flat strip + walls) at each sample, connect adjacent cross-sections with triangle pairs (quad → 2 triangles).
- Halfpipe: Define a semicircular arc cross-section, extrude along a straight axis, generate triangle pairs between adjacent arc segments.

**Alternatives considered**:
- Loading mesh from file: Would require file I/O in scripts, external asset management. Too complex for a demo.
- ConvexHull shapes: Cannot represent concave surfaces (halfpipe). Mesh is the only option for arbitrary concave terrain.

## R3: Triangle Density and Performance

**Decision**: Target 100-300 triangles per terrain mesh body.

**Rationale**: Demo 21 already creates mesh bodies with 4 triangles per mesh object, and the system handles multiple such bodies. A single mesh body with 100-300 triangles is well within BepuPhysics2 capabilities. The batch command system chunks at 100 commands, but a single `makeMeshCmd` with 300 triangles is one command (the triangles are part of the MeshShape message, not separate commands).

**Alternatives considered**:
- Higher density (1000+ triangles): Unnecessary for visual smoothness at demo scale. Would increase script size without visible benefit.
- Lower density (20-50 triangles): Might look too angular, especially for the halfpipe curves.

## R4: Material Properties for Terrain

**Decision**: Use `slipperyMaterial` (friction=0.01) for the rollercoaster track; custom moderate material (friction=0.3) for the halfpipe.

**Rationale**: The rollercoaster needs low friction so balls roll freely and maintain speed through the track. The halfpipe needs moderate friction so objects oscillate realistically — too slippery and they'd never settle, too sticky and they'd stop immediately.

**Alternatives considered**:
- Default material (no override): Would use BepuPhysics2 defaults which may be too high-friction for the rollercoaster effect.
- `bouncyMaterial`: Balls bouncing off the track surface isn't the desired visual — rolling is.

## R5: Demo Registration Pattern

**Decision**: Add entries to `AllDemos.fsx` array and `all_demos.py` list. Create standalone script files following Demo 22 pattern.

**Rationale**: All 22 existing demos follow this exact pattern. The RunAll.fsx and AutoRun.fsx runners iterate the AllDemos array, so adding entries there automatically includes them in suite runs.

**Alternatives considered**: None — this is the established convention with no reason to deviate.
