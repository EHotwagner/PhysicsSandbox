# Feature Specification: Enhance Demos with New Body Types and Fix Impacts

**Feature Branch**: `005-enhance-demos`
**Created**: 2026-03-23
**Status**: Completed
**Input**: User description: "improve the demos to show the new body types and other capabilities. also some demos need work like the tower that does not get hit or the pyramid that gets hit too light and from the side not frontal."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fix Broken Impact Demos (Priority: P1)

A user runs the demo suite and expects projectile impacts to visibly destroy their targets. Currently, the "Crate Stack" tower may not get hit properly and the "Bowling Alley" wall gets hit too lightly and from the side rather than head-on. After this fix, every impact demo should produce a satisfying, visible collision that topples or scatters the target formation.

**Why this priority**: Broken demos undermine confidence in the physics system. Fixing existing demos before adding new ones ensures a solid foundation.

**Independent Test**: Run the Crate Stack and Bowling Alley demos individually and confirm the projectile strikes the target formation centrally, with enough force to produce visible destruction.

**Acceptance Scenarios**:

1. **Given** the Crate Stack demo builds a tower, **When** the boulder is launched, **Then** it strikes the tower centrally (aligned on the same axis) and topples at least half the crates visibly.
2. **Given** the Bowling Alley demo builds a brick formation, **When** the wrecking ball is launched, **Then** it approaches from directly in front (not from the side) and scatters the majority of bricks.
3. **Given** either impact demo is run, **When** the projectile is launched, **Then** the impulse magnitude is strong enough to produce dramatic visible destruction, not a gentle nudge.

---

### User Story 2 - Showcase Constraint Physics (Priority: P1)

A user runs a demo that demonstrates mechanical joints and linkages — things connected to other things. Currently no demos use any of the 10 available constraint types. New demos should show constraints in action: a swinging pendulum, a hinged door or bridge, a chain of linked bodies, or a wrecking ball on a cable.

**Why this priority**: Constraints are a major physics capability with zero demo coverage. Showing them is essential for demonstrating the system's range.

**Independent Test**: Run constraint demos and confirm bodies are visibly linked, move together as expected, and the constraint behavior (swinging, hinging, rigid attachment) is clearly observable.

**Acceptance Scenarios**:

1. **Given** a constraint demo is started, **When** bodies are created with constraints between them, **Then** the connected bodies move in a physically plausible linked manner (e.g., pendulum swings, chain sags, hinged objects pivot).
2. **Given** a constraint demo with a wrecking ball or pendulum, **When** the ball swings and strikes a target, **Then** the impact is visible and the ball continues to swing naturally after impact.

---

### User Story 3 - Showcase Advanced Shape Types (Priority: P1)

A user runs demos and sees only spheres and boxes in nearly every scene. After enhancement, demos should feature capsules, cylinders, triangles, convex hulls, and compound shapes in meaningful roles — not just token appearances but as integral parts of the scene (e.g., capsule "logs" in a cabin, compound "vehicles," cylinder "pillars" in a temple).

**Why this priority**: 8 of 10 shape types have zero or near-zero demo coverage. The demo suite should represent the full shape vocabulary.

**Independent Test**: Run the full demo suite and confirm that at least 7 of the 10 shape types appear across the demos, each used in a context where its geometry matters.

**Acceptance Scenarios**:

1. **Given** the demo suite runs, **When** all demos complete, **Then** capsules, cylinders, convex hulls, compound shapes, and triangles each appear in at least one demo in a contextually meaningful role.
2. **Given** a demo using compound shapes, **When** the compound body is created, **Then** it behaves as a single rigid body composed of multiple child shapes.

---

### User Story 4 - Showcase Physics Queries (Priority: P2)

A user wants to see the system's spatial query capabilities. New or updated demos should demonstrate raycasts, sweep casts, or overlap tests — for example, a raycast that detects which body is directly below a dropper, or a sweep test that predicts a projectile's path before launch.

**Why this priority**: Physics queries are a unique capability with zero demo coverage. Demonstrating them shows the system goes beyond simple simulation.

**Independent Test**: Run a query demo and confirm that query results are printed or used to influence the demo behavior.

**Acceptance Scenarios**:

1. **Given** a demo uses raycast or sweep queries, **When** the query executes, **Then** the result is printed showing hit body ID, position, and distance.
2. **Given** a demo uses overlap queries, **When** the query executes at a crowded location, **Then** it reports the IDs of overlapping bodies.

---

### User Story 5 - Expand Use of Colors and Materials (Priority: P2)

A user notices that only Demo 01 uses custom colors and material properties. After enhancement, colors should be used across demos to distinguish different object types, roles (projectile vs. target), or materials. Material presets (bouncy, sticky, slippery) should appear in demos where they create visible behavioral differences.

**Why this priority**: Color and material variety makes demos more visually informative and showcases under-used features.

**Independent Test**: Run the demo suite and confirm that at least half the demos use custom colors to distinguish objects, and at least 3 demos use distinct material properties that create visible behavioral differences.

**Acceptance Scenarios**:

1. **Given** a demo with projectiles and targets, **When** the scene is built, **Then** projectiles and targets have visually distinct colors.
2. **Given** a demo with material-varied objects, **When** objects interact, **Then** bouncy objects bounce noticeably higher, slippery objects slide further, and sticky objects decelerate faster than default objects in the same scene.

---

### User Story 6 - Demonstrate Kinematic Bodies (Priority: P3)

A user wants to see bodies that move on a scripted path, unaffected by physics forces, but still interact with dynamic bodies. A demo should show a kinematic "pusher" or "elevator" that moves through a scene, sweeping dynamic bodies along.

**Why this priority**: Kinematic bodies are available but never demonstrated. They enable scripted mechanical elements that enrich scenes.

**Independent Test**: Run a kinematic demo and confirm that a body moves along a path regardless of collisions, while dynamic bodies it contacts are pushed or carried.

**Acceptance Scenarios**:

1. **Given** a kinematic body is created, **When** it moves through a group of dynamic bodies, **Then** the dynamic bodies are pushed aside or carried while the kinematic body's path is unaffected.

---

### Edge Cases

- What happens when a constraint connects a dynamic body to a static body (e.g., pendulum anchored to ceiling)? The static body should remain fixed.
- What happens when a compound shape with many children is involved in a high-speed collision? It should behave as a single rigid body.
- What happens when a raycast hits no bodies? The demo should handle empty results gracefully and print "no hit."
- What happens when demos are run without the viewer? All demos must still complete successfully in headless mode (viewer optional).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Crate Stack demo (Demo 03) MUST launch its projectile along the same axis as the tower, striking it centrally, with sufficient impulse to topple the majority of crates.
- **FR-002**: The Bowling Alley demo (Demo 04) MUST launch its projectile from directly in front of the brick formation (not from the side), with sufficient impulse to scatter the majority of bricks.
- **FR-003**: The demo suite MUST include at least one demo that creates and demonstrates constraint-linked bodies (using ball socket, hinge, weld, or distance constraints).
- **FR-004**: The demo suite MUST include at least one demo that creates compound shapes (multi-child rigid bodies).
- **FR-005**: The demo suite MUST feature capsule, cylinder, convex hull, and triangle shapes in contextually meaningful roles across the demos (not limited to a single "shape showcase" demo).
- **FR-006**: The demo suite MUST include at least one demo that performs a physics query (raycast, sweep cast, or overlap test) and prints or uses the results.
- **FR-007**: At least half of all demos MUST use custom per-body colors to visually distinguish object roles (projectiles, targets, structural elements, etc.).
- **FR-008**: At least 3 demos MUST use distinct material presets (bouncy, sticky, slippery) where the material difference produces visible behavioral contrast.
- **FR-009**: The demo suite MUST include at least one demo that uses kinematic bodies interacting with dynamic bodies.
- **FR-010**: All F# demos MUST have corresponding Python demos that demonstrate the same scenarios.
- **FR-011**: All existing demos that are not being replaced MUST continue to pass without regressions.
- **FR-012**: The AllDemos.fsx registry and Python equivalent MUST be updated to include all new demos and reflect any changes to existing demos.
- **FR-013**: The AutoRun.fsx runner MUST complete the full suite with 0 failures.

### Key Entities

- **Demo**: A self-contained physics scenario with a name, description, and run function that operates on a connected session.
- **Shape**: A collision geometry type (sphere, box, capsule, cylinder, triangle, convex hull, compound, mesh, shape reference, plane).
- **Constraint**: A mechanical joint linking two bodies with specific degrees of freedom and spring/damper parameters.
- **Query**: A spatial interrogation of the physics world (raycast, sweep, overlap) returning hit information.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All demos complete with 0 failures in the automated runner (AutoRun.fsx).
- **SC-002**: At least 7 of 10 shape types are used across the demo suite (currently 4: sphere, box, capsule, cylinder).
- **SC-003**: At least 3 constraint types are demonstrated across the demo suite (currently 0).
- **SC-004**: At least 1 physics query type (raycast, sweep, or overlap) is demonstrated with printed results.
- **SC-005**: Impact demos (Crate Stack, Bowling Alley) produce visible central destruction — projectile trajectory is aligned with the target formation's center.
- **SC-006**: At least 8 demos use custom per-body colors (currently 1).
- **SC-007**: At least 3 demos use material presets with visible behavioral differences (currently 1).
- **SC-008**: F# and Python demo suites have feature parity — same number of demos, same scenarios.

## Assumptions

- The total demo count may increase beyond 15 if new demos are added, or stay at 15 if new capabilities are integrated into existing demos. The approach will be a mix: fix existing broken demos, enhance existing demos with more shape/color/material variety, and add new demos for entirely new capabilities (constraints, queries, kinematic bodies).
- Convex hull demos will use simple point clouds (tetrahedra, octahedra) since complex hulls are harder to specify in script form.
- Mesh and ShapeReference shapes are excluded from the "7 of 10" target since meshes require vertex data that is unwieldy in demo scripts, and ShapeReference is a registration mechanism rather than a visual shape.
- Demo ordering in AllDemos.fsx may be adjusted to present a logical progression from simple to complex.
- The Prelude helpers and generator functions (stack, pyramid, row, grid, etc.) may be extended if needed to support new shape types or constraint creation.
