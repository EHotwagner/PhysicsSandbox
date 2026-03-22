# Spec Drift Report

Generated: 2026-03-22
Project: PhysicsSandbox (005-stride-bepu-integration)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 49 (38 FR + 11 SC) |
| Aligned | 45 (92%) |
| Drifted | 4 (8%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 005-stride-bepu-integration - Stride BepuPhysics Integration

#### Aligned (45/49)

**Shapes (FR-001 to FR-006c)**:
- FR-001: Capsule shapes -> `SimulationWorld.fs:164-170`, `ShapeGeometry.fs:41-45`
- FR-002: Cylinder shapes -> `SimulationWorld.fs:171-177`, `ShapeGeometry.fs:46-49`
- FR-003: Triangle shapes -> `SimulationWorld.fs:178-182`
- FR-004: Convex hull shapes -> `SimulationWorld.fs:183-188`
- FR-005: Compound shapes -> `SimulationWorld.fs:189-216`
- FR-006: Mesh shapes -> `SimulationWorld.fs:217-229`
- FR-006a: Shape registration -> `SimulationWorld.fs:502-522`
- FR-006c: Cache cleared on reset -> `SimulationWorld.fs:646`

**Debug Visualization (FR-008 to FR-010)**:
- FR-008: Wireframe overlay -> `DebugRenderer.fs:34-48`
- FR-009: Constraint connections -> `DebugRenderer.fs:67-90`
- FR-010: Runtime toggle -> `Program.fs:171-175`, F3 key

**Constraints (FR-011 to FR-016)** — All 10 types + management:
- FR-011: Hinge -> `SimulationWorld.fs:541-543`
- FR-012: Ball-socket -> `SimulationWorld.fs:538-540`
- FR-013: Distance limit -> `SimulationWorld.fs:547-549`
- FR-014: Weld -> `SimulationWorld.fs:544-546`
- FR-011a: Angular motor -> `SimulationWorld.fs:562-564`
- FR-011b: Linear motor -> `SimulationWorld.fs:559-561`
- FR-011c: Swing limit -> `SimulationWorld.fs:553-555`
- FR-011d: Twist limit -> `SimulationWorld.fs:556-558`
- FR-011e: Point-on-line -> `SimulationWorld.fs:565-567`
- FR-015: Remove by ID -> `SimulationWorld.fs:598-606`
- FR-016: Auto-remove on body deletion -> `SimulationWorld.fs:448-453`

**Materials (FR-017 to FR-019)**:
- FR-017: Friction -> MaterialProperties.friction
- FR-018: Bounciness -> MaterialProperties.max_recovery_velocity
- FR-019: Damping -> MaterialProperties.spring_frequency + spring_damping_ratio

**Queries (FR-020 to FR-024a)**:
- FR-020: Single-hit raycast -> `QueryHandler.fs:51-73`
- FR-021: Penetrating raycast -> `QueryHandler.fs:60`, all_hits flag
- FR-022: Sweep cast -> `QueryHandler.fs:75-101`
- FR-023: Overlap -> `QueryHandler.fs:103-119`
- FR-024: Collision filtering -> `QueryHandler.fs:23-25`
- FR-024a: Batch variants -> RaycastBatch, SweepCastBatch, OverlapBatch RPCs

**Collision Layers (FR-025 to FR-027)**:
- FR-025: 32 layers via uint32 -> `SimulationWorld.fs:345-346`
- FR-026: Configurable matrix -> SetCollisionFilter RPC
- FR-027: Non-interacting pass through -> Bit-based mask filtering

**Kinematic Bodies (FR-028, FR-029)**:
- FR-028: Unaffected by gravity -> `SimulationWorld.fs:139`
- FR-029: Collide and displace -> `PhysicsWorld.addKinematicBody`

**Per-Body Color (FR-035 to FR-038)**:
- FR-035: RGBA color per body -> Proto Color message + AddBody.color
- FR-036: Default by shape type -> `ShapeGeometry.fs:75-88`
- FR-037: Viewer renders color + transparency -> `SceneManager.fs:35-61`
- FR-038: Color in state stream -> `SimulationWorld.fs:87-92`

**BepuFSharp Wrapper (FR-031 to FR-035b)**:
- FR-031 to FR-034: Constraints, materials, queries, collision layers -> BepuFSharp 0.2.0-beta.1
- FR-035a: Stride.BepuPhysics types -> Transitive via CommunityToolkit.Bepu
- FR-035b: Type interop -> SceneManager + Bepu3DPhysicsOptions

**Success Criteria (9/11)**:
- SC-001: 9 shape types -> Verified via 82 simulation tests
- SC-002: Debug wireframe -> F3 toggle
- SC-003: 10 constraint types (exceeds spec's 9)
- SC-004: Different materials -> MaterialProperties presets
- SC-005: Raycast accuracy -> QueryHandler
- SC-006: Collision layers -> uint32 group/mask
- SC-007: Kinematic behavior -> Gravity skip + kinematic type
- SC-010: Backward compat -> 216 tests pass
- SC-011: Different colors -> bodyColor() in SceneManager

#### Drifted (4 items)

1. **FR-006b**: Shape caching optimization
   - Spec says: "include shape definitions in state streams only on first use"
   - Code does: Includes all registered shapes in every state stream
   - Location: `src/PhysicsSimulation/World/SimulationWorld.fs:123-127`
   - Severity: **minor** — Conservative approach; functionally correct, uses more bandwidth

2. **FR-007**: Viewer rendering for complex shapes
   - Spec says: "render each shape type with geometry that visually matches the physics collider dimensions"
   - Code does: Triangle, Compound, and Mesh shapes render as placeholder spheres
   - Location: `src/PhysicsViewer/Rendering/ShapeGeometry.fs:50-52`
   - Severity: **moderate** — 7/10 shapes render accurately; 3 complex types need custom tessellation

3. **FR-030**: Kinematic body runtime updates
   - Spec says: "Users MUST be able to set the position and velocity of kinematic bodies directly"
   - Code does: Position/velocity only settable at creation via AddBody; no runtime update RPC
   - Location: Missing SetKinematicPose command in proto and SimulationWorld
   - Severity: **moderate** — Kinematic bodies work but cannot be repositioned post-creation

4. **SC-008/SC-009**: Client interface coverage + performance validation
   - Spec says: "All new features accessible via REPL, MCP tools, scripting library" + "30 FPS with 200 bodies"
   - Code does: Core features accessible; scripting has 4/10 constraint convenience builders; SC-009 not benchmarked
   - Location: `src/PhysicsSandbox.Scripting/ConstraintBuilders.fs`
   - Severity: **minor** — All features reachable via proto; missing convenience wrappers for uncommon constraint types

#### Not Implemented

None — all functional requirements have at least partial implementation.

### Unspecced Code

No significant unspecced code found.

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **FR-030 (Kinematic pose updates)**: Add a `SetBodyPose` command to proto and SimulationWorld for runtime kinematic repositioning. Most significant functional gap.
2. **FR-007 (Complex shape rendering)**: Implement custom MeshDraw generation for Triangle, Compound, and Mesh shapes in ShapeGeometry.
3. **FR-006b (Shape caching)**: Consider per-subscriber cache tracking. Low priority.
4. **SC-008 (Constraint builders)**: Add convenience builders for remaining 6 constraint types in Scripting library.
