# Research: 005-enhance-demos

## R1: Demo 03 (Crate Stack) — Root Cause of Missed Impact

**Decision**: Fix projectile-target alignment and impulse magnitude.

**Findings**: In AllDemos.fsx Demo 03, the tower is built at X=0 as a vertical stack. The boulder spawns at (-6, 1, 0) and receives impulse (2000, 0, 0) which is +X direction. Geometrically this should travel toward the tower. However, the `launch` function is used in the standalone file but `makeImpulseCmd` is used in AllDemos — the raw impulse of 2000 N·s on a 200kg boulder produces only 10 m/s velocity, which may not be enough given the 6m distance and gravity drop. Additionally, the boulder's Y=1 starting height means it drops significantly during travel and may pass under the tower.

**Fix approach**:
- Spawn boulder closer: (-4, 0.5, 0) — reduces travel time and gravity drop
- Use `launch` helper to aim directly at the tower's center of mass (~Y=3 for 12-crate stack)
- Increase speed to ensure dramatic impact

**Alternatives considered**:
- Increasing impulse magnitude alone → still has gravity drop issue
- Using kinematic body for projectile → defeats the physics demo purpose

## R2: Demo 04 (Bowling Alley) — Root Cause of Side Hit

**Decision**: Fix X-axis misalignment between ball and pyramid.

**Findings**: In AllDemos.fsx Demo 04, the `pyramid` generator places bricks at X=5 (passed as position parameter). The bowling ball spawns at (0, 0.5, -4) with impulse (0, 0, 100) — pure Z-axis. The ball travels along Z but the pyramid is offset 5 units in X. The ball passes to the side of the pyramid entirely, or at best clips the edge.

**Fix approach**:
- Place pyramid at origin: pyramid(s, 4, Some(0.0, 0.0, 5.0)) — pyramid at Z=5
- Ball at (0, 0.5, -2) with impulse (0, 0, N) along +Z — direct frontal approach
- Increase impulse for more dramatic scatter (200-500 N·s on 10kg ball = 20-50 m/s)

**Alternatives considered**:
- Moving ball to match pyramid X → still a side approach depending on formation orientation
- Using `launch` helper → good option, can aim at pyramid center precisely

## R3: Prelude Extension Strategy — Script vs. Library

**Decision**: Keep new helpers in Prelude.fsx/prelude.py (script-only), not in PhysicsSandbox.Scripting library.

**Rationale**: Adding to the Scripting library triggers Constitution Principle V (.fsi files, surface area baselines, tests). The new helpers (compound shape builder, convex hull builder, triangle builder, kinematic body builder) are demo conveniences, not core API. They can be promoted to the library in a future feature if demand arises.

**Alternatives considered**:
- Promote to Scripting library → triggers .fsi + surface area work, increases scope significantly
- Inline in each demo → code duplication across 18+ files

## R4: New Helpers Needed in Prelude

**Decision**: Add these helpers to Prelude.fsx and prelude.py:

| Helper | Purpose | Proto types used |
|--------|---------|-----------------|
| `makeTriangleCmd(id, pos, a, b, c, mass)` | Create triangle shape body | Triangle, Shape, AddBody |
| `makeConvexHullCmd(id, pos, points, mass)` | Create convex hull body from point cloud | ConvexHull, Shape, AddBody |
| `makeCompoundCmd(id, pos, children, mass)` | Create compound body from (shape, localPos) pairs | Compound, CompoundChild, Shape, AddBody |
| `makeKinematicCmd(id, pos, shape)` | Create kinematic body (mass=0, KINEMATIC motion type) | AddBody with BodyMotionType.Kinematic |
| `withMotionType(motionType, cmd)` | Set motion type on AddBody command | BodyMotionType enum |
| `withCollisionFilter(group, mask, cmd)` | Set collision group+mask on AddBody command | collision_group, collision_mask fields |

Existing Prelude already has: `makeBallSocketCmd`, `makeHingeCmd`, `makeWeldCmd` (from ConstraintBuilders), `makeDistanceLimitCmd`, `raycast`, `raycastAll`, `sweepSphere`, `overlapSphere` (from QueryBuilders), `makeSetBodyPoseCmd` (from CommandBuilders).

## R5: Demo 16 (Constraints) — Scene Design

**Decision**: Three-act constraint showcase.

**Act 1 — Pendulum Chain**: 5 spheres linked by ball-socket constraints, hanging from a static anchor. Disturb the first to create wave motion.

**Act 2 — Hinged Bridge**: 6 planks (boxes) linked end-to-end by hinge constraints, anchored at both ends to static pillars. Drop heavy objects on the bridge to see it flex.

**Act 3 — Weld Cluster**: 4 boxes welded into a rigid cross shape, dropped onto a pile. Shows weld keeps relative orientation fixed during collision.

Constraint types covered: ball socket (Act 1), hinge (Act 2), weld (Act 3), distance limit (Act 1, as chain link length limiter). Total: 4 types, exceeding the SC-003 target of 3.

## R6: Demo 17 (Query Range) — Scene Design

**Decision**: Raycast + overlap demonstration.

**Scene**: Drop 20 random bodies into a pit. Then:
1. Fire 5 raycasts downward from different X positions, print which body each hits first
2. Perform overlap sphere test at the center of the pit, print how many bodies are within radius
3. Sweep a sphere from one side to the other, print first hit

This demonstrates raycast, raycastAll (optional), overlapSphere, and sweepSphere.

## R7: Demo 18 (Kinematic Sweep) — Scene Design

**Decision**: Kinematic pusher plowing through dynamic bodies.

**Scene**: Place 30 small dynamic bodies in a line. Create a kinematic box "bulldozer" at one end. Animate it by stepping through positions using `makeSetBodyPoseCmd` in a loop (play, set pose, step, repeat). The kinematic body pushes dynamic bodies aside as it moves through.

**Implementation detail**: Kinematic animation requires:
1. Create body with `BodyMotionType.Kinematic` (mass=0)
2. In a loop: pause → setBodyPose to new position → play → short sleep → repeat
3. This creates discrete position updates that the physics engine interpolates

## R8: Color Palette for Demo Enhancement

**Decision**: Define a standard 8-color palette in Prelude for consistent visual coding.

| Role | Color (R, G, B, A) | Usage |
|------|-------------------|-------|
| Projectile | (1.0, 0.2, 0.1, 1.0) | Red — bowling balls, boulders, wrecking balls |
| Target | (0.3, 0.6, 1.0, 1.0) | Blue — bricks, crates, pins |
| Structure | (0.7, 0.7, 0.7, 1.0) | Gray — walls, pillars, ground elements |
| Accent 1 | (1.0, 0.8, 0.0, 1.0) | Yellow — special objects, highlighted bodies |
| Accent 2 | (0.2, 0.8, 0.3, 1.0) | Green — capsules, organic shapes |
| Accent 3 | (0.8, 0.4, 1.0, 1.0) | Purple — compound shapes, complex geometry |
| Accent 4 | (1.0, 0.5, 0.0, 1.0) | Orange — cylinders, warm-toned shapes |
| Kinematic | (0.0, 1.0, 1.0, 1.0) | Cyan — kinematic/scripted bodies |

These will be defined as named constants in Prelude.

## R9: Material Usage Plan

**Decision**: Apply material presets strategically across demos.

| Demo | Material Usage | Visible Effect |
|------|---------------|----------------|
| 01 Hello Drop | bouncy on beach ball, sticky on bowling ball | Different bounce heights |
| 09 Billiards | slippery on all balls | Low-friction rolling/sliding |
| 13 Force Frenzy | bouncy on half, sticky on other half | Bouncy ones fly higher, sticky ones clump |
| 16 Constraints | bouncy on pendulum end | Energetic pendulum swings |

This satisfies SC-007 (≥3 demos with material contrast).

## R10: Shape Distribution Plan

**Decision**: Distribute advanced shapes across existing demos rather than concentrating in one.

| Shape | Demos Using It (after enhancement) |
|-------|-----------------------------------|
| Sphere | 01, 02, 05, 07, 08, 09, 10, 11, 12, 13, 15, 16, 17 |
| Box | 01, 03, 04, 05, 06, 07, 08, 10, 11, 14, 15, 16, 18 |
| Capsule | 01, 05, 07, 08, 11 |
| Cylinder | 01, 07, 08, 10, 11 |
| Triangle | 08 (as "ramp" or "wedge" in gravity flip scene) |
| ConvexHull | 08 (octahedron die), 12 (tetrahedron in pit) |
| Compound | 11 (body scaling with compound "dumbbell"), 12 (in pit mix) |
| Plane | all (ground) |

**Shape type count**: 8 of 10 (excluding Mesh and ShapeReference per spec assumptions). Exceeds SC-002 target of 7.
