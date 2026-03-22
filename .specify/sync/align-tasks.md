# Alignment Tasks

Generated: 2026-03-22
Based on: approved proposals from drift analysis

---

## Task 1: Align FR-007 — Custom rendering for Triangle, Compound, and Mesh shapes

**Spec Requirement**: FR-007 — "The viewer MUST render each shape type with geometry that visually matches the physics collider dimensions."
**Current Code**: Triangle, Compound, and Mesh shapes render as placeholder spheres in `ShapeGeometry.fs`.
**Required Change**: Implement custom `MeshDraw` generation for the 3 remaining shape types.

**Files to Modify**:
- `src/PhysicsViewer/Rendering/ShapeGeometry.fs` — Add custom mesh generation functions
- `src/PhysicsViewer/Rendering/ShapeGeometry.fsi` — Add new public functions if needed
- `src/PhysicsViewer/Rendering/SceneManager.fs` — Use custom mesh for non-primitive shapes
- `tests/PhysicsViewer.Tests/SceneManagerTests.fs` — Add tests for custom mesh rendering

**Estimated Effort**: Medium

### Implementation Details

1. **Triangle**: Generate a `MeshDraw` from the 3 vertices (A, B, C). Create a double-sided triangle face with computed normal. The vertex data is available in `Body.Shape.Triangle.A/B/C` fields.

2. **Compound**: Create child entities with local transforms. For each `CompoundChild`, recursively resolve the child shape's primitive type and size, create a child entity, and apply `LocalPosition` and `LocalOrientation` transforms relative to the parent entity.

3. **Mesh**: Generate a `MeshDraw` from the triangle list. Iterate `Body.Shape.Mesh.Triangles`, extract vertex positions, compute per-face normals, and build a vertex/index buffer.

### Acceptance Criteria
- [ ] Triangle shapes render as a visible triangle face at correct vertices
- [ ] Compound shapes render all child sub-shapes with correct local transforms
- [ ] Mesh shapes render all triangles from the mesh definition
- [ ] Existing primitive shapes (Sphere, Box, Capsule, Cylinder) are unaffected
- [ ] Debug wireframes still work for all shape types

---

## Task 2: Align FR-030 — Add SetBodyPose command for kinematic runtime updates

**Spec Requirement**: FR-030 — "Users MUST be able to set the position and velocity of kinematic bodies directly."
**Current Code**: Position/velocity only settable at creation time via `AddBody`. No runtime update command exists.
**Required Change**: Add `SetBodyPose` proto message and command with full pipeline through server, simulation, and client interfaces.

**Files to Modify**:
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — Add `SetBodyPose` message and `SimulationCommand` variant
- `src/PhysicsSimulation/World/SimulationWorld.fs` — Add `setBodyPose` function
- `src/PhysicsSimulation/World/SimulationWorld.fsi` — Add signature
- `src/PhysicsSimulation/Commands/CommandHandler.fs` — Dispatch new command
- `src/PhysicsServer/Hub/MessageRouter.fs` — Route new command (pass-through)
- `src/PhysicsServer/Services/PhysicsHubService.fs` — Handle new command variant
- `src/PhysicsClient/Commands/SimulationCommands.fs` — Add `setBodyPose` function
- `src/PhysicsClient/Commands/SimulationCommands.fsi` — Add signature
- `src/PhysicsSandbox.Mcp/SimulationTools.fs` — Add `set_body_pose` MCP tool
- `src/PhysicsSandbox.Mcp/SimulationTools.fsi` — Add signature
- `src/PhysicsSandbox.Scripting/CommandBuilders.fs` — Add `makeSetBodyPoseCmd` builder
- `src/PhysicsSandbox.Scripting/Prelude.fs` — Re-export builder
- `tests/PhysicsSimulation.Tests/ExtendedFeatureTests.fs` — Add tests

**Estimated Effort**: Medium

### Implementation Details

1. **Proto message**:
   ```protobuf
   message SetBodyPose {
     string body_id = 1;
     Vec3 position = 2;
     Vec4 orientation = 3;
     Vec3 velocity = 4;
     Vec3 angular_velocity = 5;
   }
   ```
   Add `SetBodyPose set_body_pose = 16;` to `SimulationCommand` oneof.

2. **SimulationWorld.setBodyPose**: Look up body by ID. For kinematic bodies, call BepuFSharp `setKinematicBodyPose` (or `setBodyPose` if available). For dynamic bodies, directly set position/velocity. Update `BodyRecord` state fields if needed. Reject for static bodies.

3. **Client interfaces**: Follow existing pattern (e.g., `setCollisionFilter`). Accept body_id + optional position/orientation/velocity/angular_velocity.

### Acceptance Criteria
- [ ] `SetBodyPose` command changes a kinematic body's position at runtime
- [ ] Velocity updates take effect on kinematic bodies
- [ ] Orientation updates apply correctly
- [ ] Static bodies reject the command with an error message
- [ ] Dynamic bodies can also be teleported (position set)
- [ ] Command accessible via REPL, MCP, and Scripting library
- [ ] Viewer reflects the position change in the next state stream
- [ ] Unit tests verify all scenarios
