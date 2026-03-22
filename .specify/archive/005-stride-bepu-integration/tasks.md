# Tasks: Stride BepuPhysics Integration

**Input**: Design documents from `/specs/005-stride-bepu-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to

---

## Phase 1: Setup (Proto Contracts & Dependencies)

**Purpose**: Extend proto contract and update BepuFSharp dependency — blocks all implementation

- [X] T001 Add all new proto messages to src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto: shape types (Capsule, Cylinder, Triangle, ConvexHull, Compound, MeshShape, ShapeReference in Shape.oneof fields 4-10), shape registration (RegisterShape, UnregisterShape), Color, MaterialProperties, BodyMotionType enum, SpringSettings, MotorConfig, all 10 constraint messages (BallSocketConstraint through PointOnLineConstraint), ConstraintType oneof, AddConstraint, RemoveConstraint, ConstraintState, SetCollisionFilter, RegisteredShapeState per contracts/proto-extensions.md
- [X] T002 Add new fields to existing proto messages in src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto: AddBody fields 6-12 (material, color, motion_type, collision_group, collision_mask, angular_velocity, orientation), Body fields 9-13 (color, motion_type, collision_group, collision_mask, material), SimulationState fields 6-7 (constraints, registered_shapes), SimulationCommand oneof fields 11-15 (add_constraint, remove_constraint, register_shape, unregister_shape, set_collision_filter)
- [X] T003 Add physics query messages and RPCs to src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto: RaycastRequest/Response, RaycastBatchRequest/Response, SweepCastRequest/Response, SweepCastBatchRequest/Response, OverlapRequest/Response, OverlapBatchRequest/Response, RayHit, and 6 new RPCs on PhysicsHub service (Raycast, RaycastBatch, SweepCast, SweepCastBatch, Overlap, OverlapBatch)
- [X] T004 Build contracts project and verify proto generation: run `dotnet build src/PhysicsSandbox.Shared.Contracts/` and verify all new C# types are generated without errors
- [X] T005 Update BepuFSharp PackageReference from 0.1.0 to 0.2.0-beta.1 in src/PhysicsSimulation/PhysicsSimulation.fsproj

**Checkpoint**: Proto types generated, BepuFSharp 0.2.0-beta.1 available — all downstream work unblocked

---

## Phase 2: Foundational (Simulation & Server Core)

**Purpose**: Extend core simulation and server infrastructure that ALL user stories depend on

- [X] T006 Extend BodyRecord in src/PhysicsSimulation/SimulationWorld.fs with new fields: Color (Color option), Material (MaterialProperties), MotionType (BodyMotionType), CollisionGroup (uint32), CollisionMask (uint32)
- [X] T007 Extend addBody function in src/PhysicsSimulation/SimulationWorld.fs to parse new AddBody fields: extract material, color, motion_type, collision_group, collision_mask, angular_velocity, orientation from proto message and store in BodyRecord
- [X] T008 Extend state serialization in src/PhysicsSimulation/SimulationWorld.fs: include color, motion_type, collision_group, collision_mask, material in each Body message of buildState function
- [X] T009 Extend CommandHandler in src/PhysicsSimulation/CommandHandler.fs to dispatch 5 new command types: AddConstraint, RemoveConstraint, RegisterShape, UnregisterShape, SetCollisionFilter (stub handlers that log and return, full implementation in story phases)
- [X] T010 Extend MessageRouter in src/PhysicsServer/MessageRouter.fs to route new command types through the command channel to simulation (no new logic needed — existing channel handles all SimulationCommand variants)
- [X] T011 Update PhysicsHubService in src/PhysicsServer/Services/PhysicsHubService.fs to handle new SimulationCommand oneof cases in SendCommand/SendBatchCommand (pass-through to MessageRouter)
- [X] T012 Verify full solution builds: run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and fix any compilation errors from proto changes and BepuFSharp 0.2.0-beta.1 breaking change (raycast/raycastAll now require CollisionFilter option parameter — update all call sites to pass None)

**Checkpoint**: Solution builds, new proto fields flow through simulation and server — story-specific work can begin

---

## Phase 3: User Story 1 + 8 — Extended Shapes & Per-Body Color (Priority: P1)

**Goal**: All 9 shape types can be created, simulated, and visualized. Bodies render in user-specified or auto-assigned colors.

**Independent Test**: Send AddBody commands with each new shape type and custom colors; verify viewer renders correct geometry in correct color.

### Simulation — Shape Support

- [X] T013 [US1] Implement shape conversion from proto to BepuFSharp PhysicsShape for all new types in src/PhysicsSimulation/SimulationWorld.fs: Capsule(radius, length), Cylinder(radius, length), Triangle(a, b, c), ConvexHull(points), Compound(children with local transforms), MeshShape(triangles). Existing Sphere/Box/Plane converters serve as reference.
- [X] T014 [US1] Add shape validation in src/PhysicsSimulation/SimulationWorld.fs: reject ConvexHull with < 4 points, Compound with 0 children, MeshShape with 0 triangles, negative radius/length. Return error in CommandAck.

### Simulation — Shape Registration & Caching

- [X] T015 [US1] Add RegisteredShapes map (Map<string, ShapeId * Shape>) to SimulationWorld state in src/PhysicsSimulation/SimulationWorld.fs
- [X] T016 [US1] Implement RegisterShape command handler in src/PhysicsSimulation/CommandHandler.fs: register shape with BepuFSharp addShape, store handle→(ShapeId, Shape) mapping. Reject duplicate handles.
- [X] T017 [US1] Implement UnregisterShape command handler in src/PhysicsSimulation/CommandHandler.fs: remove from map. Do not call BepuFSharp removeShape if bodies still reference it (log warning).
- [X] T018 [US1] Implement ShapeReference resolution in addBody: when Shape.oneof is ShapeReference, look up handle in RegisteredShapes map, use cached ShapeId. Reject unknown handles with error.
- [X] T019 [US1] Include RegisteredShapeState list in SimulationState serialization in src/PhysicsSimulation/SimulationWorld.fs: emit all registered shapes so late-joining clients can build cache.
- [X] T020 [US1] Clear RegisteredShapes map on ResetSimulation in src/PhysicsSimulation/SimulationWorld.fs

### Viewer — New Shape Rendering

- [X] T021 [P] [US1] Create src/PhysicsViewer/Rendering/ShapeGeometry.fs: module with functions to generate Stride 3D primitive models for each shape type. For capsule: use PrimitiveModelType.Capsule. For cylinder: use PrimitiveModelType.Cylinder. For triangle/convex hull/mesh: generate custom MeshDraw from vertex data. For compound: create child entities with local transforms.
- [X] T022 [US1] Extend SceneManager.applyState in src/PhysicsViewer/Rendering/SceneManager.fs to use ShapeGeometry for creating entities: replace current shape-type switch (Sphere→PrimitiveModelType.Sphere, Box→Cube) with dispatch to ShapeGeometry module supporting all 9 types.
- [X] T023 [US1] Handle compound shapes in SceneManager: create parent entity with child entities for each CompoundChild, applying local position and orientation transforms.

### Viewer — Per-Body Color

- [X] T024 [P] [US8] Add color-to-material conversion in src/PhysicsViewer/Rendering/SceneManager.fs: function that takes a Color proto message and returns a Stride Material with the specified RGBA values, including alpha transparency support.
- [X] T025 [US8] Add default color palette by shape type in src/PhysicsViewer/Rendering/SceneManager.fs: Sphere=blue, Box=orange, Capsule=green, Cylinder=yellow, Plane=gray, Triangle=cyan, ConvexHull=purple, Compound=white, MeshShape=teal.
- [X] T026 [US8] Update entity creation in SceneManager.applyState to use body color from state stream: if Body.Color is set use it, otherwise apply default color from palette based on shape type. Replace current hardcoded blue/orange/red color logic.

### Signature Files & Build Verification

- [X] T027 [P] [US1] Create or update .fsi signature file for ShapeGeometry module: src/PhysicsViewer/Rendering/ShapeGeometry.fsi
- [X] T028 [US1] Update .fsi signature files for modified modules: src/PhysicsSimulation/SimulationWorld.fsi (if exists), src/PhysicsViewer/Rendering/SceneManager.fsi (if exists)
- [X] T029 [US1] Verify Phase 3 builds and existing tests pass: run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true && dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

### Tests for Phase 3

- [X] T029a [US1] Add unit tests for shape conversion in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs: test proto→PhysicsShape conversion for all 6 new shape types (capsule, cylinder, triangle, convex hull, compound, mesh), shape validation rejection (degenerate hull, empty compound, negative radius), and ShapeReference resolution (valid handle, unknown handle error)
- [X] T029b [US8] Add unit tests for per-body color in tests/PhysicsViewer.Tests/SceneManagerTests.fs: test color-to-material conversion (RGBA values, alpha transparency), default color palette by shape type, color from state stream applied to entity
- [X] T029c [US1] Add unit tests for shape registration in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs: test RegisterShape stores handle, UnregisterShape removes, duplicate handle rejection, reset clears cache, RegisteredShapeState in serialized state

**Checkpoint**: All 9 shape types create, simulate, and render with per-body color. Shape caching works. Existing demos still pass. Tests verify shape conversion, registration, and color rendering.

---

## Phase 4: User Story 2 — Debug Visualization (Priority: P2)

**Goal**: Wireframe debug overlay for all colliders and constraint connections, togglable at runtime.

**Independent Test**: Toggle debug mode in viewer; verify wireframe outlines appear over all physics bodies.

- [X] T030 [US2] Create src/PhysicsViewer/Rendering/DebugRenderer.fsi: signature file declaring public API — initialize (register SinglePassWireframeRenderFeature), updateShapes (sync wireframes to current body states), updateConstraints (render constraint lines), setEnabled (toggle), isEnabled.
- [X] T031 [US2] Create src/PhysicsViewer/Rendering/DebugRenderer.fs: implement debug wireframe rendering using Stride.BepuPhysics.Debug's WireFrameRenderObject and SinglePassWireframeRenderFeature. Generate vertex/index buffers for each body's shape using ShapeGeometry, create WireFrameRenderObjects, add to scene's visibility group. Update positions each frame from SimulationState.
- [X] T032 [US2] Integrate DebugRenderer into viewer game loop in src/PhysicsViewer/PhysicsViewerApp.fs (or equivalent entry point): initialize on startup, call updateShapes after each state apply, toggle via keyboard shortcut (e.g., F3) or ToggleWireframe ViewCommand.
- [X] T033 [US2] Add constraint visualization in DebugRenderer: for each ConstraintState in SimulationState, draw a line between body_a and body_b positions with color coding by constraint type. Use WireFrameRenderObject with simple line geometry.
- [X] T034 [US2] Update .fsi and add DebugRenderer to PhysicsViewer.fsproj compile order (after ShapeGeometry, before PhysicsViewerApp)
- [X] T035 [US2] Verify debug visualization builds: `dotnet build src/PhysicsViewer/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Debug wireframes render for all shapes and constraints, togglable at runtime.

---

## Phase 5: User Story 3 — Constraints and Joints (Priority: P3)

**Goal**: 10 constraint types can connect bodies with correct constrained motion. Auto-cleanup on body removal.

**Independent Test**: Create two bodies, add a hinge constraint, apply force, observe constrained rotation.

### Simulation — Constraint Management

- [X] T036 [US3] Add constraint registry to SimulationWorld state in src/PhysicsSimulation/SimulationWorld.fs: Constraints map (Map<string, ConstraintId * string * string * ConstraintType>) storing constraint ID, body_a ID, body_b ID, and constraint type proto.
- [X] T037 [US3] Implement AddConstraint command handler in src/PhysicsSimulation/CommandHandler.fs: convert proto ConstraintType oneof to BepuFSharp ConstraintDesc (all 10 variants: BallSocket, Hinge, Weld, DistanceLimit, DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine), look up body_a and body_b BodyIds, call PhysicsWorld.addConstraint, store in Constraints map. Reject if either body not found.
- [X] T038 [US3] Implement RemoveConstraint command handler in src/PhysicsSimulation/CommandHandler.fs: look up ConstraintId from Constraints map, call PhysicsWorld.removeConstraint, remove from map. Reject if constraint not found.
- [X] T039 [US3] Implement auto-remove constraints on body removal in src/PhysicsSimulation/SimulationWorld.fs: when RemoveBody is handled, scan Constraints map for any constraints referencing the removed body ID, remove each via PhysicsWorld.removeConstraint, remove from map.
- [X] T040 [US3] Serialize ConstraintState in SimulationState in src/PhysicsSimulation/SimulationWorld.fs: iterate Constraints map, emit ConstraintState (id, body_a, body_b, type) for each. Use BepuFSharp getConstraintDescription to read back current parameters.
- [X] T041 [US3] Clear Constraints map on ResetSimulation in src/PhysicsSimulation/SimulationWorld.fs

### Tests for Phase 5

- [X] T041a [US3] Add unit tests for constraints in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs: test AddConstraint for all 10 types (create + verify in state), RemoveConstraint (removed from state), auto-remove on body removal (remove body_a → constraint gone), reject constraint with unknown body ID, clear on reset

**Checkpoint**: All 10 constraint types work, auto-cleanup on body removal, constraints appear in state stream. Tests verify all constraint CRUD operations.

---

## Phase 6: User Story 4 — Material Properties (Priority: P4)

**Goal**: Per-body material properties (friction, bounciness, damping) produce visibly different collision behaviors.

**Independent Test**: Drop two balls with different material settings onto same surface; observe different bounce heights.

- [X] T042 [US4] Implement material property passthrough in src/PhysicsSimulation/SimulationWorld.fs: when AddBody includes MaterialProperties, convert proto (friction, max_recovery_velocity, spring_frequency, spring_damping_ratio) to BepuFSharp MaterialProperties and pass to DynamicBodyDesc/KinematicBodyDesc/StaticBodyDesc. When absent, use BepuFSharp MaterialProperties.defaults.
- [X] T043 [US4] Implement SetCollisionFilter command handler in src/PhysicsSimulation/CommandHandler.fs: look up body by ID, call BepuFSharp setCollisionFilter with new group/mask values. (Serves both US4 and US6 but placed here as it's part of runtime modification.)

**Checkpoint**: Material properties affect physics behavior. Bodies bounce/slide differently based on settings.

---

## Phase 7: User Story 5 — Physics Queries (Priority: P5)

**Goal**: Raycast, sweep cast, and overlap queries return correct results via dedicated RPCs.

**Independent Test**: Populate scene with known bodies, issue raycast, verify correct hit body and position returned.

### Simulation — Query Handler

- [X] T044 [US5] Create src/PhysicsSimulation/QueryHandler.fsi: signature file declaring query dispatch functions — handleRaycast, handleSweepCast, handleOverlap, each taking request proto and SimulationWorld, returning response proto.
- [X] T045 [US5] Create src/PhysicsSimulation/QueryHandler.fs: implement query dispatch. For Raycast: convert proto to BepuFSharp raycast/raycastAll call (with CollisionFilter option from collision_mask), map RayHit results back to proto RayHit (resolve BodyId→string ID via Bodies map). For SweepCast: convert proto Shape to PhysicsShape, call BepuFSharp sweepCast, map result. For Overlap: call BepuFSharp overlap, map OverlapResult body/static IDs to string IDs.
- [X] T046 [US5] Add QueryHandler to PhysicsSimulation.fsproj compile order (after SimulationWorld, before SimulationClient)

### Server — Query Routing

- [X] T047 [US5] Add query routing mechanism in src/PhysicsServer/MessageRouter.fs: add a query channel (bounded channel of query request + TaskCompletionSource<response>) that the simulation can poll. When a query RPC arrives, server enqueues the request and awaits the TCS. Simulation dequeues, executes via QueryHandler, completes the TCS.
- [X] T048 [US5] Implement Raycast and RaycastBatch RPCs in src/PhysicsServer/Services/PhysicsHubService.fs: enqueue query request to MessageRouter query channel, await response, return typed proto response. RaycastBatch iterates requests and collects responses.
- [X] T049 [US5] Implement SweepCast, SweepCastBatch, Overlap, OverlapBatch RPCs in src/PhysicsServer/Services/PhysicsHubService.fs: same pattern as Raycast — enqueue to query channel, await, return.
- [X] T050 [US5] Extend SimulationClient loop in src/PhysicsSimulation/SimulationClient.fs to poll query channel: after processing commands each tick, check for pending queries, execute via QueryHandler, complete TCS with result.
- [X] T051 [US5] Forward query requests from server to simulation via the existing SimulationLink gRPC connection in src/PhysicsServer/Services/SimulationLinkService.fs: queries MUST be executed on the simulation (server has no physics engine). Add a concurrent task that reads from the query channel (T047), sends each query to the simulation via a new bidirectional query stream or by piggybacking on the command stream with a response callback, and completes the TCS when the simulation responds.

### Tests for Phase 7

- [X] T051a [US5] Add integration tests for query RPCs in tests/PhysicsSandbox.Integration.Tests/: test Raycast (hit + miss), RaycastBatch (multiple rays), SweepCast (sphere sweep hits box), Overlap (bodies inside/outside volume), collision mask filtering on queries

**Checkpoint**: All 6 query RPCs return correct results. Batch variants work. Integration tests verify end-to-end query flow.

---

## Phase 8: User Story 6 — Collision Layers and Filtering (Priority: P6)

**Goal**: Bodies on non-interacting layers pass through each other. Queries respect collision masks.

**Independent Test**: Create two bodies on different layers with disabled interaction; verify they pass through each other.

- [X] T052 [US6] Implement collision filter passthrough in src/PhysicsSimulation/SimulationWorld.fs: when AddBody includes collision_group/collision_mask, pass to BepuFSharp body desc CollisionGroup/CollisionMask fields. Default: group=1, mask=0xFFFFFFFF (collide with all).
- [X] T053 [US6] Verify SetCollisionFilter runtime modification works end-to-end: the handler from T043 should allow changing group/mask at runtime via SetCollisionFilter command.
- [X] T054 [US6] Verify query filtering: ensure Raycast/SweepCast/Overlap RPCs pass collision_mask from request to BepuFSharp query functions (CollisionFilter option parameter).

**Checkpoint**: Collision layer filtering works for physics and queries.

---

## Phase 9: User Story 7 — Kinematic Bodies (Priority: P7)

**Goal**: Kinematic bodies move at set velocity, push dynamic bodies, unaffected by gravity.

**Independent Test**: Create kinematic body with velocity, verify it moves steadily and pushes dynamic bodies on contact.

- [X] T055 [US7] Implement BodyMotionType dispatch in src/PhysicsSimulation/SimulationWorld.fs addBody function: when motion_type is DYNAMIC call BepuFSharp addBody (DynamicBodyDesc), KINEMATIC call addKinematicBody (KinematicBodyDesc), STATIC call addStatic (StaticBodyDesc). Preserve backward compat: if motion_type is default (DYNAMIC/0) and mass=0, treat as static (legacy behavior).
- [X] T056 [US7] Skip gravity application for kinematic bodies in src/PhysicsSimulation/SimulationWorld.fs step function: kinematic bodies should not have gravity force applied (they are force-immune).
- [X] T057 [US7] Serialize motion_type in Body state: ensure kinematic bodies report BodyMotionType.KINEMATIC in the state stream.

### Tests for Phase 9

- [X] T057a [US7] Add unit tests for kinematic bodies in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs: test BodyMotionType dispatch (DYNAMIC→addBody, KINEMATIC→addKinematicBody, STATIC→addStatic), backward compat (mass=0 default→static), gravity skip for kinematic, motion_type in serialized state

**Checkpoint**: Three body types (dynamic, kinematic, static) fully supported. Tests verify dispatch and backward compat.

---

## Phase 10: Client Interfaces

**Purpose**: Expose all new features through REPL, MCP, and Scripting library

### PhysicsClient REPL

- [X] T058 [P] Add constraint commands to src/PhysicsClient/: AddConstraint (all 10 types with parameters), RemoveConstraint by ID. Follow existing command pattern (CommandBuilders module).
- [X] T059 [P] Add shape registration commands to src/PhysicsClient/: RegisterShape (with shape definition), UnregisterShape by handle.
- [X] T060 [P] Add query commands to src/PhysicsClient/: Raycast (origin, direction, max distance, optional mask), SweepCast, Overlap. Display results in Spectre.Console table format.
- [X] T061 [P] Extend AddBody command in src/PhysicsClient/ to support all 9 shape types (capsule with radius+length, cylinder with radius+length, triangle with 3 vertices, convex hull with point list, compound with children, mesh with triangles, shape reference by handle) plus optional material, color, motion_type, collision_group, collision_mask parameters.
- [X] T062 [P] Add SetCollisionFilter command to src/PhysicsClient/

### MCP Server

- [X] T063 [P] Add constraint tools to src/PhysicsSandbox.Mcp/Tools/: add_constraint (all 10 types), remove_constraint, list_constraints.
- [X] T064 [P] Add query tools to src/PhysicsSandbox.Mcp/Tools/: raycast, sweep_cast, overlap (with structured JSON results).
- [X] T065 [P] Add shape registration tools to src/PhysicsSandbox.Mcp/Tools/: register_shape, unregister_shape.
- [X] T066 [P] Add material/filter tools to src/PhysicsSandbox.Mcp/Tools/: set_collision_filter. Extend add_body tool with material, color, motion_type, collision_group, collision_mask parameters.

### Scripting Library

- [X] T067 [P] Add constraint helpers to src/PhysicsSandbox.Scripting/: convenience functions for creating common constraint configurations (e.g., createHinge, createBallSocket, createWeld with sensible defaults).
- [X] T068 [P] Add query helpers to src/PhysicsSandbox.Scripting/: raycast, sweepCast, overlap convenience wrappers that return typed F# results.
- [X] T069 [P] Add material/color builder helpers to src/PhysicsSandbox.Scripting/: MaterialProperties builder with named presets (bouncy, sticky, slippery, default), Color builder with named colors.

### Signature Files & Surface Area

- [X] T070a Update .fsi signature files and surface area baselines for simulation modules: tests/PhysicsSimulation.Tests/ (SimulationWorld, CommandHandler, QueryHandler baselines)
- [X] T070b Update .fsi signature files and surface area baselines for server modules: tests/PhysicsServer.Tests/ (MessageRouter, PhysicsHubService baselines)
- [X] T070c Update .fsi signature files and surface area baselines for client modules: tests/PhysicsClient.Tests/, tests/PhysicsSandbox.Scripting.Tests/
- [X] T071 Verify all client interface changes build: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: All new features accessible via REPL, MCP tools, and Scripting library.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, demo updates, and backward compatibility verification

- [X] T072 Update existing F# demos in Scripting/demos/ to use new shape types and colors where appropriate (enhance visual variety without breaking existing demo logic)
- [X] T073 Update existing Python demos in Scripting/demos_py/ to use new shape types and colors where appropriate
- [X] T074 [P] Update Prelude.fsx in Scripting/demos/ with helpers for new features (constraint builders, color helpers, material presets)
- [X] T075 [P] Update prelude.py in Scripting/demos_py/ with helpers for new features
- [X] T076 Run full test suite and fix any failures: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [X] T077 Update CLAUDE.md with new feature documentation: add new shape types, constraints, material properties, queries, collision layers, kinematic bodies, and per-body color to Active Technologies and Recent Changes sections
- [X] T078 Verify full system end-to-end: run `./start.sh`, create bodies with various shapes/colors/materials/constraints via client, verify viewer renders correctly, test debug visualization toggle

**Checkpoint**: Feature complete. All user stories verified. Backward compatible.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion — BLOCKS all user stories
- **Phases 3-9 (User Stories)**: All depend on Phase 2. Most can proceed in parallel with notes:
  - **US1+US8 (Phase 3)**: No story dependencies — start first (P1)
  - **US2 (Phase 4)**: Depends on US1 (needs ShapeGeometry module). Constraint viz (T033) depends on US3.
  - **US3 (Phase 5)**: No story dependencies — can parallel with US1
  - **US4 (Phase 6)**: No story dependencies — can parallel with US1
  - **US5 (Phase 7)**: No story dependencies but query filtering benefits from US6 collision layers
  - **US6 (Phase 8)**: T043 (SetCollisionFilter handler) is in US4 phase but serves US6
  - **US7 (Phase 9)**: No story dependencies — can parallel with US1
- **Phase 10 (Client Interfaces)**: Depends on Phases 3-9 for the features to expose
- **Phase 11 (Polish)**: Depends on Phase 10

### User Story Independence

| Story | Can start after | Dependencies on other stories |
|-------|----------------|-------------------------------|
| US1+US8 (Shapes+Color) | Phase 2 | None |
| US2 (Debug Viz) | Phase 3 (US1) | US1 for ShapeGeometry; US3 for constraint viz |
| US3 (Constraints) | Phase 2 | None |
| US4 (Materials) | Phase 2 | None |
| US5 (Queries) | Phase 2 | None (collision filtering optional) |
| US6 (Collision Layers) | Phase 2 | US4 T043 for SetCollisionFilter handler |
| US7 (Kinematic) | Phase 2 | None |

### Parallel Opportunities

- T001, T002, T003 can be done as sequential edits to one file, or as one combined task
- T021 (ShapeGeometry) and T024 (color conversion) are parallel within Phase 3
- T030-T035 (debug viz) is entirely self-contained once US1 is done
- T058-T069 (all client interface tasks) are parallel across different projects
- T072-T075 (demo updates) are parallel

---

## Parallel Example: Phase 3 (US1+US8)

```bash
# After T020 (simulation shape support complete), launch viewer work in parallel:
Task T021: "Create ShapeGeometry module"     # new file, no dependencies
Task T024: "Color-to-material conversion"     # new function, no dependencies

# After T021+T024, sequential viewer integration:
Task T022: "Extend SceneManager.applyState"   # depends on T021
Task T026: "Apply per-body color"             # depends on T024
```

---

## Implementation Strategy

### MVP First (Phase 1 + 2 + 3 = US1+US8)

1. Complete Phase 1: Proto contracts + BepuFSharp upgrade
2. Complete Phase 2: Foundational simulation/server changes
3. Complete Phase 3: Extended shapes + per-body color
4. **STOP and VALIDATE**: Create bodies with all 9 shape types and custom colors
5. This alone delivers dramatic visual improvement

### Incremental Delivery

1. Phase 1+2 → Foundation ready
2. Phase 3 (US1+US8) → 9 shapes + color (MVP)
3. Phase 5 (US3) → Constraints (high-impact, independent of US2)
4. Phase 4 (US2) → Debug viz (benefits from US1+US3)
5. Phase 6 (US4) → Materials (quick win)
6. Phase 7 (US5) → Queries (new capability)
7. Phase 8+9 (US6+US7) → Collision layers + kinematic (refinements)
8. Phase 10+11 → Client interfaces + polish

---

## Notes

- Proto changes (T001-T003) are one file — do them sequentially or as one combined edit
- BepuFSharp 0.2.0-beta.1 breaking change: raycast/raycastAll require CollisionFilter option — fix in T012
- Debug viz uses WireFrameRenderObject (not DebugRenderProcessor) per research.md Decision 1
- Constraint interop with Stride deferred per BepuFSharp implementation report
- [P] tasks = different files, no dependencies
- Commit after each task or logical group
