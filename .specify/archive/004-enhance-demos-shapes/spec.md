# Feature Specification: Enhance Demos with New Shapes and Viewer Labels

**Feature Branch**: `004-enhance-demos-shapes`
**Created**: 2026-03-24
**Status**: Completed
**Input**: User description: "improve the demos by adding the new shapes and add 3 more showcasing all new shapes in detail with variation. also add a title to the viewer window. also add a label in the top left of the window screen displaying the demo name and description."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Viewer Window Title and Demo Label (Priority: P1)

When a user runs the physics sandbox, the 3D viewer window displays a meaningful title in the OS title bar. When a demo is loaded, the top-left corner of the viewer shows the demo name and a short description so the user always knows which demo they are watching.

**Why this priority**: Without visual identification, users cannot distinguish which demo is running or what the viewer application is. This is foundational UX that benefits all demos and everyday usage.

**Independent Test**: Run any demo and verify the window title bar shows a meaningful application name. Verify the top-left overlay displays the demo name and description. Load a different demo and confirm the label updates.

**Acceptance Scenarios**:

1. **Given** the viewer is launched, **When** the window appears, **Then** the OS title bar displays a meaningful application name (e.g., "PhysicsSandbox Viewer").
2. **Given** a demo is running, **When** the viewer renders, **Then** the top-left corner of the screen displays the demo name (e.g., "Demo 01: Hello Drop") and a one-line description.
3. **Given** the simulation is reset and a new demo starts, **When** the viewer updates, **Then** the demo label updates to reflect the new demo's name and description.
4. **Given** no demo name has been set (e.g., free-form usage), **When** the viewer renders, **Then** the label area is either blank or shows a default message (e.g., "Free Mode").

---

### User Story 2 - Existing Demos Use New Shape Types (Priority: P2)

The 18 existing demos are enhanced to incorporate the newer shape types (Triangle, ConvexHull, Mesh, Compound) where they add visual or physical variety. This makes demos more interesting and exercises the full shape palette rather than only using spheres, boxes, capsules, and cylinders.

**Why this priority**: Enriching existing demos shows off the full capabilities of the physics engine and validates that new shapes work correctly in varied scenarios. This builds on existing content rather than requiring wholly new demos.

**Independent Test**: Run each enhanced demo and verify that new shape types appear, interact physically with other bodies, and render correctly in the viewer (both solid and wireframe modes).

**Acceptance Scenarios**:

1. **Given** an existing demo that previously used only primitive shapes, **When** it is run after enhancement, **Then** at least some bodies use Triangle, ConvexHull, Mesh, or Compound shapes.
2. **Given** new shape types are added to a demo, **When** the simulation runs, **Then** these shapes interact physically (collide, bounce, stack) with other bodies as expected.
3. **Given** new shape types are present in a demo, **When** the viewer renders in both solid and wireframe modes (F3 toggle), **Then** the shapes display their accurate collision geometry.
4. **Given** an existing demo is enhanced, **When** the demo runs, **Then** the overall character and purpose of the demo is preserved — enhancements add variety without breaking the demo's original intent.

---

### User Story 3 - Three New Demos Showcasing All New Shapes (Priority: P2)

Three new demos (Demo 19, 20, 21) are added that each focus on showcasing the newer shape types (Triangle, ConvexHull, Mesh, Compound) with variation in size, orientation, material properties, and physical interaction. These demos serve as a gallery and validation of every shape the engine supports.

**Why this priority**: Dedicated demos for new shapes provide a clear showcase and regression test for shape rendering and physics. Having three demos allows different aspects to be highlighted — one could focus on shape variety, another on compound interactions, another on mesh and hull detail.

**Independent Test**: Run each new demo individually and verify all advertised shape types are present, physically interact, and render correctly.

**Acceptance Scenarios**:

1. **Given** Demo 19 is run, **When** the simulation starts, **Then** it showcases a variety of new shape types (Triangle, ConvexHull, Mesh, Compound) with different sizes, colors, and materials.
2. **Given** Demo 20 is run, **When** the simulation starts, **Then** it demonstrates compound shapes in interesting configurations (e.g., composite objects built from multiple sub-shapes).
3. **Given** Demo 21 is run, **When** the simulation starts, **Then** it highlights mesh and convex hull shapes with varied point counts, orientations, and physical behaviors.
4. **Given** any new demo is run, **When** viewing in the viewer, **Then** the demo label displays the correct demo name and description.
5. **Given** a new demo is run, **When** the Python demo runner is used, **Then** a matching Python version of each new demo exists and produces equivalent behavior.

---

### User Story 4 - Demo Metadata for Labels (Priority: P3)

Each demo (existing and new) provides a name and short description that the viewer can display. The demo metadata is communicated through the existing simulation infrastructure so the viewer can pick it up without manual coordination.

**Why this priority**: This is the data pipeline that connects demo scripts to the viewer label. It is lower priority because it is an enabler for Story 1 rather than direct user value, and a simple mechanism suffices.

**Independent Test**: Run a demo script and verify that the viewer receives and displays the demo's name and description without any manual viewer configuration.

**Acceptance Scenarios**:

1. **Given** a demo script sets a name and description, **When** the viewer is connected, **Then** the viewer's top-left label displays that name and description.
2. **Given** a demo does not set metadata, **When** the viewer renders, **Then** a sensible default is shown instead of blank or error text.

---

### Edge Cases

- What happens when the demo name or description is very long? The label should truncate or wrap gracefully without obscuring the 3D view.
- What happens when multiple demos are run in rapid succession (e.g., AutoRun mode)? The label should update promptly for each new demo.
- What happens when the simulation is reset without loading a new demo? The label should clear or revert to a default.
- What happens if a demo uses a Compound shape with deeply nested children? Rendering should handle reasonable nesting depth (e.g., 3-4 levels).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The viewer window MUST display a meaningful application title in the OS title bar when launched.
- **FR-002**: The viewer MUST display the current demo name and a one-line description in the top-left corner of the screen, above or integrated with the existing status bar.
- **FR-003**: The demo label MUST update when a new demo is loaded or the simulation is reset.
- **FR-004**: Existing demos (1-18) MUST be enhanced to include at least some of the newer shape types (Triangle, ConvexHull, Mesh, Compound) where appropriate.
- **FR-005**: Three new demos (19, 20, 21) MUST be created, each featuring the newer shape types with variation in size, orientation, color, and material properties.
- **FR-006**: Each new demo MUST have both an F# (.fsx) and Python (.py) version.
- **FR-007**: New demos MUST be registered in the demo runner infrastructure (AllDemos, RunAll, AutoRun equivalents).
- **FR-008**: Each demo (existing and new) MUST provide a name and short description as metadata that the viewer can display.
- **FR-009**: The demo label MUST not obscure critical simulation information (FPS, simulation time, status) already displayed in the viewer.
- **FR-010**: New shape types used in demos MUST interact physically (collision, gravity, forces) with all other shape types correctly.
- **FR-011**: The demo label MUST be readable (sufficient contrast, appropriate font size) against the 3D scene background.

### Key Entities

- **Demo Metadata**: A name (e.g., "Hello Drop") and description (e.g., "Six shapes fall side by side") associated with each demo script.
- **Demo Label**: A viewer overlay element that renders the current demo's metadata in the top-left screen area.
- **Shape Types**: The 10 supported shapes — Sphere, Box, Plane, Capsule, Cylinder, Triangle, ConvexHull, Mesh, Compound, and ShapeRef/CachedRef.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The viewer window title bar displays a recognizable application name in 100% of launches.
- **SC-002**: The demo name and description are visible in the viewer within 1 second of a demo loading.
- **SC-003**: At least 8 of the 18 existing demos incorporate one or more of the newer shape types (Triangle, ConvexHull, Mesh, Compound).
- **SC-004**: All 3 new demos collectively use all 4 newer shape types (Triangle, ConvexHull, Mesh, Compound) at least once each.
- **SC-005**: All 21 demos (18 existing + 3 new) provide name and description metadata displayable by the viewer.
- **SC-006**: All 3 new demos have matching F# and Python versions that produce equivalent behavior.
- **SC-007**: The demo label text remains readable and does not overlap with the existing FPS/status display.

## Assumptions

- The existing debug text overlay mechanism used for the FPS/status display is sufficient for rendering the demo label — no new UI framework is needed.
- Demo metadata (name + description) can be communicated through the existing simulation infrastructure (e.g., via a simulation property, command, or state field).
- Not all 15 existing demos need new shapes — only those where new shapes make thematic sense. The target of 8+ demos is a reasonable minimum.
- The viewer window title is a static application name, not dynamic per-demo (the per-demo info goes in the overlay label).
- Compound shapes in demos use 2-3 levels of nesting at most (reasonable for demonstration purposes).
- Demo descriptions are kept to a single short line (under ~80 characters) to fit the overlay cleanly.

## Scope Boundaries

**In scope**:
- Viewer window title bar text
- Viewer top-left demo label overlay
- Enhancement of existing demos with new shape types
- Three new demos (F# + Python) showcasing new shapes
- Demo metadata infrastructure (name + description per demo)
- Registration of new demos in runner infrastructure

**Out of scope**:
- Changes to the settings overlay (F2 menu)
- Changes to the debug wireframe system (beyond verifying new shapes render)
- New constraint types or physics engine changes
- Changes to the MCP server or recording infrastructure
- Performance optimization of the viewer or simulation
- Interactive demo selection UI in the viewer
