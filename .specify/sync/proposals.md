# Drift Resolution Proposals

Generated: 2026-03-22
Based on: drift-report from 2026-03-22

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code -> Spec) | 2 |
| Align (Spec -> Code) | 2 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 005-stride-bepu-integration/FR-006b

**Direction**: BACKFILL

**Current State**:
- Spec says: "The server MUST cache registered shapes and include shape definitions in state streams only on first use, so all clients can build and maintain a local shape cache."
- Code does: All registered shapes are included in every state stream update. This is simpler, stateless, and ensures late-joining clients always receive the full shape cache.

**Proposed Resolution**:

Update FR-006b to:

> **FR-006b**: The server MUST cache registered shapes and include all registered shape definitions in every state stream update, so all clients (including late-joiners) receive the complete shape cache without per-subscriber tracking.

**Rationale**: The current implementation is a deliberate simplification. Per-subscriber cache tracking adds complexity (subscriber identity management, cache invalidation, reconnection handling) with minimal benefit given the small size of registered shape data. The implementation is functionally correct -- all clients get the data they need. The spec optimization was aspirational; the simpler approach is appropriate for the current scale.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: 005-stride-bepu-integration/FR-007

**Direction**: ALIGN

**Current State**:
- Spec says: "The viewer MUST render each shape type with geometry that visually matches the physics collider dimensions."
- Code does: 7/10 shape types render accurately (Sphere, Box, Plane, Capsule, Cylinder, ConvexHull bounding box, ShapeReference). Triangle, Compound, and Mesh shapes render as placeholder spheres.

**Proposed Resolution**:

Implement custom rendering for the 3 remaining shape types in `ShapeGeometry.fs`:
- **Triangle**: Generate a `MeshDraw` from the 3 vertices (single face, double-sided).
- **Compound**: Create child entities with local transforms, each rendered via recursive `primitiveType`/`shapeSize` dispatch.
- **Mesh**: Generate a `MeshDraw` from the triangle list vertex data.

This requires adding custom `MeshDraw` generation functions to `ShapeGeometry.fs` and updating `SceneManager.applyState` to use them for non-primitive shapes.

**Estimated effort**: Medium (1-2 tasks). The vertex data is already available in the proto state stream.

**Rationale**: The spec explicitly requires "visually matches the physics collider dimensions" for each shape type. Placeholder spheres are functionally misleading -- a mesh collider shaped like a ramp rendering as a sphere hides the actual collision geometry. This is especially important for debug/development use cases where developers need to see what they built. 7/10 is good but the remaining 3 are the most geometrically interesting shapes.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: 005-stride-bepu-integration/FR-030

**Direction**: ALIGN

**Current State**:
- Spec says: "Users MUST be able to set the position and velocity of kinematic bodies directly."
- Code does: Position and velocity can only be set at creation time via `AddBody`. No command exists to update kinematic body pose or velocity after creation.

**Proposed Resolution**:

Add a `SetBodyPose` command:
1. **Proto**: Add `SetBodyPose` message with fields: `body_id`, `position` (Vec3), `orientation` (Vec4, optional), `velocity` (Vec3, optional), `angular_velocity` (Vec3, optional).
2. **SimulationWorld**: Add `setBodyPose` function that updates position/velocity on the BepuFSharp body. For kinematic bodies, use `PhysicsWorld.setKinematicBodyPose`; for dynamic bodies, optionally allow teleporting.
3. **SimulationCommand**: Add `set_body_pose` variant to the oneof.
4. **CommandHandler**: Dispatch `SetBodyPose` to `SimulationWorld.setBodyPose`.
5. **Client interfaces**: Add `setBodyPose` to PhysicsClient, MCP tool, and Scripting library.

**Estimated effort**: Medium (1 task across proto + simulation + server + clients).

**Rationale**: Kinematic bodies are fundamentally designed to be script-driven -- their entire purpose is to move according to explicit commands rather than physics forces. Without runtime pose updates, kinematic bodies are limited to constant-velocity motion set at creation, which severely limits their utility (no moving platforms that change direction, no animated obstacles, no scripted waypoint motion). This is the most significant functional gap in the feature.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 4: 005-stride-bepu-integration/SC-008

**Direction**: BACKFILL

**Current State**:
- Spec says: "All new features are accessible through the existing client interfaces (REPL commands, MCP tools, scripting library)."
- Code does: All features are accessible via proto commands (REPL + MCP cover all command types). The Scripting library provides convenience builders for 4/10 constraint types (BallSocket, Hinge, Weld, DistanceLimit). The remaining 6 constraint types can be created via manual proto construction through the scripting library's `batchAdd` with hand-built `SimulationCommand` objects.

**Proposed Resolution**:

Update SC-008 to acknowledge the tiered accessibility:

> **SC-008**: All new features MUST be accessible through the REPL and MCP interfaces. The scripting library MUST provide convenience builders for the most commonly used constraint types (ball-socket, hinge, weld, distance limit) with the remaining types constructible via standard proto message builders.

Additionally, add 6 missing constraint convenience builders as a follow-up enhancement (not blocking):
- `makeDistanceSpringCmd`
- `makeSwingLimitCmd`
- `makeTwistLimitCmd`
- `makeLinearAxisMotorCmd`
- `makeAngularMotorCmd`
- `makePointOnLineCmd`

**Rationale**: The core requirement (feature accessibility) is met -- every feature is reachable through REPL and MCP. The scripting library is a convenience layer; having builders for the 4 most common constraint types covers the majority of use cases. The SC-009 (30 FPS benchmark) is a runtime validation that requires a live system test, not a code-level verification.

**Confidence**: MEDIUM

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
