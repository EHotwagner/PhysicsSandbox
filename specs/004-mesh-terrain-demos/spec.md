# Feature Specification: Static Mesh Terrain Demos

**Feature Branch**: `004-mesh-terrain-demos`
**Created**: 2026-03-25
**Status**: Draft
**Input**: User description: "create 2 new demos showcasing using static mesh as interesting ground. a rollercoaster for balls, halfpipes....."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ball Rollercoaster Demo (Priority: P1)

A user runs a demo that constructs a rollercoaster-style track from static mesh triangles and releases balls at the top. The balls roll along the track's banked curves, drops, and hills, demonstrating how static mesh geometry can serve as complex sculptured terrain. The demo includes cinematic camera movements that follow the action and narration text describing what is happening.

**Why this priority**: This is the most visually impressive and technically demanding use of static mesh as terrain — a twisting track with elevation changes showcases mesh geometry far beyond flat ground planes.

**Independent Test**: Can be fully tested by running the demo script against a live PhysicsSandbox server and observing balls traversing the rollercoaster track without falling through or getting stuck.

**Acceptance Scenarios**:

1. **Given** a running PhysicsSandbox server, **When** the rollercoaster demo script is executed, **Then** a track made of static mesh triangles is constructed with at least 3 distinct track features (drops, hills, banked curves) and balls are released that roll along the track.
2. **Given** the rollercoaster track is built, **When** balls are released from the top, **Then** they stay on the track surface (no clipping through mesh) and reach the end of the track.
3. **Given** the demo is running, **When** the camera follows the action, **Then** smooth camera transitions and narration text describe each phase of the rollercoaster ride.

---

### User Story 2 - Halfpipe Arena Demo (Priority: P1)

A user runs a demo that constructs a halfpipe (U-shaped ramp) and bowl arena from static mesh triangles, then drops balls and other objects into it. Objects roll back and forth in the halfpipe, demonstrating concave mesh terrain interactions. The demo includes camera work and narration.

**Why this priority**: A halfpipe/bowl is the quintessential concave terrain showcase — objects oscillating in a curved surface highlights mesh collision fidelity and is equally visually engaging.

**Independent Test**: Can be fully tested by running the demo script against a live PhysicsSandbox server and observing objects rolling and oscillating within the halfpipe without escaping or clipping.

**Acceptance Scenarios**:

1. **Given** a running PhysicsSandbox server, **When** the halfpipe demo script is executed, **Then** a halfpipe or bowl shape is constructed from static mesh triangles with smooth curvature (sufficient triangle density to appear curved).
2. **Given** the halfpipe is built, **When** balls are dropped from above, **Then** they roll back and forth along the curved surface, losing energy over time and eventually settling at the bottom.
3. **Given** the halfpipe demo is running, **When** additional objects of different shapes/sizes are introduced, **Then** they also interact correctly with the mesh surface (no clipping, appropriate rolling/sliding behavior).

---

### Edge Cases

- What happens when a ball reaches the edge of the track without sidewalls? It should fall off naturally under gravity.
- How does the simulation handle high-speed balls on tight mesh curves? Triangle density must be sufficient to prevent tunneling at expected speeds.
- What if the mesh has gaps between triangles? The track construction must produce watertight geometry with no gaps.
- What if too many triangles are used and the batch command exceeds limits? Triangle count should stay within practical limits for a single mesh body.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each demo MUST construct its terrain entirely from static mesh triangles (mass = 0, static body) using the existing mesh body creation pattern.
- **FR-002**: The rollercoaster demo MUST include at least 3 distinct track features: a steep drop, a hill/hump, and a banked curve.
- **FR-003**: The halfpipe demo MUST construct a concave curved surface with sufficient triangle density (minimum 20 triangles) to appear smooth.
- **FR-004**: Each demo MUST release at least 5 dynamic bodies (balls or mixed shapes) that interact with the mesh terrain.
- **FR-005**: Each demo MUST include cinematic camera movements (smooth transitions, following action) and narration text describing each phase.
- **FR-006**: Each demo MUST follow the existing demo script conventions: use shared prelude, call reset, set demo info, and be runnable standalone.
- **FR-007**: Both demos MUST be added to the demo suite runners so they execute as part of the full demo sequence.
- **FR-008**: Both F# and Python versions of each demo MUST be created, matching the existing dual-language demo pattern.
- **FR-009**: The mesh terrain bodies MUST be colored distinctly from dynamic objects so the terrain is visually distinguishable.
- **FR-010**: The demos MUST use appropriate material properties (e.g., low friction for smooth rolling on the rollercoaster, moderate friction for the halfpipe).

### Key Entities

- **Mesh Terrain**: A static body composed of many mesh triangles forming a 3D surface (track or bowl shape). Zero mass, positioned at world origin or slightly elevated.
- **Dynamic Objects**: Balls (spheres) and optionally capsules/cylinders that interact with the terrain. Non-zero mass, dropped or placed on the terrain.
- **Track Feature**: A distinct section of the rollercoaster (drop, hill, curve) built from a contiguous set of mesh triangles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Both demos run to completion without errors when executed against a running PhysicsSandbox server.
- **SC-002**: All dynamic objects remain on the mesh terrain surface (no clipping through) for at least 90% of the demo duration.
- **SC-003**: The rollercoaster demo shows balls traversing the full track length from start to finish.
- **SC-004**: The halfpipe demo shows at least 3 visible oscillation cycles of objects rolling back and forth before settling.
- **SC-005**: Both demos complete within 30 seconds of total runtime (matching the pace of existing demos).
- **SC-006**: Demo narration and camera work provide a clear, watchable experience — a viewer unfamiliar with the project can understand what is being demonstrated.

## Assumptions

- The existing mesh body creation helpers support creating static mesh bodies when mass is set to 0.
- The physics simulation handles static mesh collision correctly for fast-moving small objects at the triangle densities used.
- Demo numbering continues from the current highest (Demo 22), making these Demo 23 and Demo 24.
- The Python demo versions mirror the F# versions using the existing Python prelude and proto-generated stubs.
