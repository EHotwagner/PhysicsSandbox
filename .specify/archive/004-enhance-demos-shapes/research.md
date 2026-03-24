# Research: Enhance Demos with New Shapes and Viewer Labels

**Date**: 2026-03-24

## R1: Demo Metadata Transport Mechanism

**Decision**: Extend ViewCommand with a new `SetDemoMetadata` message in the proto file.

**Rationale**: ViewCommand already flows unidirectionally from client → server → viewer via `StreamViewCommands`. The server auto-forwards ViewCommands without code changes. This is the lowest-effort, lowest-bandwidth approach — metadata is sent once per demo load rather than on every 60 Hz tick.

**Alternatives Considered**:
- **Add fields to TickState**: Rejected — adds string data to the 60 Hz stream, wasteful for static metadata.
- **New dedicated RPC**: Rejected — requires new server handler, polling pattern, and more proto changes for no functional benefit.
- **PropertyEvent extension**: Possible but PropertyEvent is designed for body/constraint/shape lifecycle, not UI metadata.

## R2: Viewer Window Title

**Decision**: Set `game.Window.Title <- "PhysicsSandbox Viewer"` during game initialization in Program.fs.

**Rationale**: Stride's Game class exposes `game.Window.Title` property. Single line change. Static title is appropriate — dynamic per-demo info goes in the overlay label.

**Alternatives Considered**:
- Dynamic title per demo: Rejected per spec assumption — overlay label serves this purpose.

## R3: Demo Label Rendering

**Decision**: Use existing `DebugTextSystem.Print()` to render demo name and description. Position above the existing status bar at (10, 10), push status bar down to (10, 30).

**Rationale**: DebugTextSystem is already used for the FPS/status bar and settings overlay. No new rendering infrastructure needed. Consistent visual style.

**Alternatives Considered**:
- Stride UI system (full GUI): Rejected — overkill for a text label; DebugTextSystem is sufficient.
- Settings overlay integration: Rejected — the demo label should always be visible, not toggled.

## R4: Existing Demo Numbering

**Decision**: New demos will be numbered 19, 20, 21 (not 16, 17, 18 as originally assumed in spec).

**Rationale**: Demos 16 (Constraints), 17 (QueryRange), and 18 (KinematicSweep) already exist as standalone .fsx files. The spec's intent is 3 new demos — they'll be numbered sequentially.

**Note**: The spec references "Demo 16, 17, 18" but the intent is "3 new demos". Implementation will use the correct next numbers (19, 20, 21).

## R5: Shape Builder Availability in Prelude

**Decision**: Add `makeMeshCmd` helper to both F# Prelude.fsx and Python prelude.py. Triangle, ConvexHull, and Compound builders already exist.

**Rationale**: Prelude already has `makeTriangleCmd`, `makeConvexHullCmd`, and `makeCompoundCmd`. Only Mesh is missing. Adding it completes the shape palette for demo authors.

## R6: Existing Demo Shape Enhancement Candidates

**Decision**: Enhance at least 8 of the original 15 demos. Several already use advanced shapes:
- Demo 08 (Gravity Flip): Already has Triangle + ConvexHull
- Demo 11 (Body Scaling): Already has Compound
- Demo 12 (Collision Pit): Already has ConvexHull + Compound

**Candidates for enhancement** (adding new shapes where thematically appropriate):
- Demo 01 (Hello Drop): Add a triangle and convex hull alongside existing primitives — natural "shape gallery" demo
- Demo 03 (Crate Stack): Add compound crates (multi-part stacking objects)
- Demo 04 (Bowling Alley): Add convex hull bowling ball or compound pins
- Demo 06 (Domino Row): Add compound dominoes (L-shaped or T-shaped pieces)
- Demo 09 (Billiards): Add convex hull or mesh table bumpers
- Demo 10 (Chaos): Add mesh and triangle shapes to increase visual variety
- Demo 13 (Force Frenzy): Add triangle and convex hull projectiles
- Demo 14 (Domino Cascade): Add compound domino pieces

## R7: New Demo Themes

**Decision**: Three new demos with distinct themes:
- **Demo 19 — Shape Gallery**: All 10 shape types displayed side-by-side with labels via color coding. Slow drop to show rendering and collision. Educational showcase.
- **Demo 20 — Compound Constructions**: Complex compound shapes (vehicles, furniture, tools) interacting. Demonstrates nesting and composite physics.
- **Demo 21 — Mesh & Hull Playground**: Varied convex hulls (tetrahedra, octahedra, dodecahedra) and triangle meshes tumbling through obstacle courses. Stress-tests custom geometry rendering.

## R8: ViewCommand Server Routing

**Decision**: No server code changes needed for metadata forwarding.

**Rationale**: The PhysicsServer already forwards all ViewCommand messages from `SendViewCommand` RPC to `StreamViewCommands` without inspecting the payload. Adding a new oneof variant to ViewCommand will be auto-forwarded. Verified by examining PhysicsHubService.fs routing logic.

## R9: Demo Metadata in Prelude Helpers

**Decision**: Add `setDemoInfo` helper to Prelude.fsx and prelude.py that wraps the new `SendViewCommand(SetDemoMetadata(...))` call. All demos call this after `resetSimulation` and `setCamera`.

**Rationale**: Centralizes the metadata-sending pattern. Demo authors write `setDemoInfo s "Hello Drop" "Six shapes fall side by side"` instead of constructing proto messages directly.
