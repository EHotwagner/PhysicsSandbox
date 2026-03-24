# Feature Specification: Proper Shape Rendering

**Feature Branch**: `004-proper-shape-rendering`
**Created**: 2026-03-24
**Status**: Completed
**Input**: User description: "Build proper rendering for the missing shapes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Triangle and Mesh Rendering (Priority: P1)

A user creates triangle or mesh bodies in the physics simulation and sees the actual geometry rendered in the 3D viewer — individual triangular faces for triangles and complete mesh surfaces for meshes — rather than bounding-box cubes.

**Why this priority**: Mesh and triangle are the most commonly used complex shapes and the most visually jarring when rendered as boxes. Triangle is the simplest custom geometry (3 vertices), and mesh extends the same approach to many triangles, so they share core infrastructure.

**Independent Test**: Create a triangle body and a mesh body with known vertices. Verify both display their actual surfaces in the viewer, not cubes.

**Acceptance Scenarios**:

1. **Given** a triangle body with vertices A(0,0,0), B(1,0,0), C(0,1,0), **When** the viewer renders it, **Then** a visible triangular surface appears matching those vertices.
2. **Given** a mesh body with 4 triangles forming a tetrahedron, **When** the viewer renders it, **Then** all 4 faces are visible as a closed solid.
3. **Given** a mesh body with hundreds of triangles, **When** the viewer renders it, **Then** the shape appears without noticeable frame rate degradation.
4. **Given** a triangle or mesh body, **When** debug wireframes are enabled, **Then** the wireframe traces actual triangle edges, not a bounding box.

---

### User Story 2 - Convex Hull Rendering (Priority: P2)

A user creates a convex hull body from a point cloud and sees the convex hull surface rendered in the viewer, showing the actual convex envelope of the points.

**Why this priority**: Convex hulls are common for representing irregular solid objects. Rendering requires computing hull faces from the point set, which builds on the mesh rendering infrastructure but adds hull computation.

**Independent Test**: Create a convex hull from a known point set (e.g., 8 points forming a cube). Verify the viewer shows a closed convex surface through those points.

**Acceptance Scenarios**:

1. **Given** a convex hull body from 8 corner points of a cube, **When** the viewer renders it, **Then** the displayed shape visually matches a cube.
2. **Given** a convex hull from an irregular point set, **When** viewed from different angles, **Then** the rendered surface fully encloses all input points.
3. **Given** a convex hull, **When** debug wireframes are enabled, **Then** wireframe edges trace the hull facets, not a bounding box.

---

### User Story 3 - Compound Shape Rendering (Priority: P2)

A user creates a compound body (multiple child shapes combined) and sees each child shape rendered individually at its correct local offset and orientation, rather than a single bounding-box cube.

**Why this priority**: Compound shapes are assemblies of primitives and other shapes. Rendering them correctly requires decomposing the compound into children and rendering each at its local pose — a different approach than vertex-based geometry, but high visual impact.

**Independent Test**: Create a compound body with 2 spheres at different offsets. Verify both spheres render at their correct positions relative to the body.

**Acceptance Scenarios**:

1. **Given** a compound body with a sphere at offset (0,0,0) and a box at offset (2,0,0), **When** the viewer renders it, **Then** both child shapes appear at their correct positions.
2. **Given** a compound body with rotated children, **When** rendered, **Then** each child shape appears at its correct local orientation.
3. **Given** a compound body, **When** debug wireframes are enabled, **Then** each child shape has its own wireframe rather than one bounding box for the whole compound.
4. **Given** a compound whose children include mesh or convex hull shapes, **When** rendered, **Then** each child renders with its proper geometry (not a box).

---

### User Story 4 - CachedRef and ShapeRef Resolution (Priority: P3)

When the viewer receives a CachedRef shape (a reference to cached mesh data), it resolves and fetches the actual mesh data to render the real geometry instead of a bounding-box placeholder.

**Why this priority**: CachedRef is an optimization for streaming — it carries bounding box metadata but the actual mesh must be fetched. This depends on mesh rendering (P1) being complete first and is less common than direct shape usage.

**Independent Test**: Create a body that results in a CachedRef. Verify the viewer resolves it and renders actual geometry.

**Acceptance Scenarios**:

1. **Given** a body with a CachedRef shape, **When** the mesh data is available, **Then** the viewer resolves and renders the actual mesh geometry.
2. **Given** a CachedRef shape where the mesh data is not yet loaded, **When** the viewer renders it, **Then** a visible placeholder is shown until the real geometry loads.

---

### Edge Cases

- What happens when a triangle has degenerate geometry (all vertices collinear or coincident)? Should fall back to a visible placeholder.
- What happens when a mesh body has zero triangles? Should show a fallback placeholder, not become invisible.
- What happens when a convex hull has fewer than 4 points (cannot form a 3D hull)? Should degrade gracefully to a point, line, or flat triangle as appropriate.
- What happens when a compound body has zero children? Should show a fallback placeholder.
- What happens when a compound child is itself a compound or mesh? Recursive rendering should handle nested composition.
- What happens when a body's shape changes type at runtime (e.g., shape replaced via command)? The viewer should update to the new geometry.

## Clarifications

### Session 2026-03-24

- Q: Should ShapeRef resolution be in scope for "all 10 shape types" rendering? → A: Yes — the viewer resolves ShapeRef to its underlying registered shape and renders that shape's geometry.
- Q: What is the expected upper bound for triangle count per mesh without LOD? → A: 10,000 triangles per mesh — balanced for typical collision geometry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The viewer MUST render triangle shapes as flat triangular surfaces using the actual vertex positions from the proto data.
- **FR-002**: The viewer MUST render mesh shapes as complete surfaces built from all triangles in the mesh definition.
- **FR-003**: The viewer MUST render convex hull shapes as closed convex surfaces derived from the input point cloud.
- **FR-004**: The viewer MUST render compound shapes by individually rendering each child shape at its local offset and orientation.
- **FR-005**: The viewer MUST render CachedRef shapes by resolving the underlying mesh data and displaying the actual geometry.
- **FR-011**: The viewer MUST render ShapeRef shapes by resolving the reference to its underlying registered shape and rendering that shape's geometry.
- **FR-006**: The viewer MUST display correct debug wireframes for all shape types, tracing actual geometry edges rather than bounding boxes.
- **FR-007**: Degenerate shapes (zero-area triangles, empty meshes, sub-4-point hulls, empty compounds) MUST fall back to a visible placeholder rather than becoming invisible.
- **FR-008**: Shape rendering MUST correctly update when body poses (position and rotation) change during simulation.
- **FR-009**: Custom geometry MUST use the existing per-shape-type color palette for visual consistency.
- **FR-010**: The viewer MUST maintain interactive frame rates when rendering scenes with complex shapes (up to 10,000 triangles per mesh) at the same body counts currently supported.

### Key Entities

- **Custom Mesh Geometry**: Vertex and index buffer data generated from proto shape definitions (triangles, meshes, hulls), used to create visual model components for non-primitive shapes.
- **Hull Face**: A triangular face of a convex hull, computed from the input point cloud, used for both solid rendering and wireframe display.
- **Compound Child Instance**: A positioned and oriented child shape within a compound, rendered as an independent visual entity attached to the parent body's scene node.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 physics shape types render with geometry that matches their collision boundaries — no bounding-box approximations remain for any shape type.
- **SC-002**: Debug wireframes for all shape types trace actual geometry edges, not axis-aligned bounding boxes.
- **SC-003**: A scene with 200 bodies including a mix of all shape types (meshes up to 10,000 triangles each) renders at comparable frame rate to 200 primitive-only bodies (within 10% tolerance).
- **SC-004**: Existing demo scripts that create complex shapes display correctly in the viewer without modification to the demo scripts.
- **SC-005**: All existing viewer unit tests continue to pass, and new tests cover geometry generation for each newly rendered shape type.
