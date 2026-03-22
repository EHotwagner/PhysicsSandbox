# Feature Specification: Improve Physics Demos

**Feature Branch**: `004-improve-demos`
**Created**: 2026-03-22
**Status**: Completed
**Input**: User description: "Improve all 15 physics demos to be more satisfying, visually interesting, and physically rich. Work collaboratively on each demo."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Satisfying Demo Experience (Priority: P1)

A user runs any individual demo and sees a visually interesting, physically rich simulation that clearly demonstrates the demo's named concept. Each demo should feel "complete" — not a minimal smoke test, but a satisfying showcase of the physics feature it represents.

**Why this priority**: The demos are the primary way users experience and evaluate the physics sandbox. Thin demos undermine confidence in the system's capabilities.

**Independent Test**: Run each demo individually and confirm it produces a visually compelling result that clearly demonstrates its named physics concept within its runtime window.

**Acceptance Scenarios**:

1. **Given** any demo from 01-15, **When** a user runs it, **Then** the demo produces visible, interesting physics interactions that go beyond a minimal smoke test
2. **Given** a demo focused on a specific concept (e.g., "Bowling Alley"), **When** it runs, **Then** the physics scenario clearly and satisfyingly demonstrates that concept
3. **Given** a demo with multi-act structure, **When** it runs, **Then** each act builds on the previous one and the progression feels intentional

---

### User Story 2 - Complete Demo Suite Integration (Priority: P2)

A user can run all 15 demos through the AllDemos runner (interactive or automated) without any demos being excluded or requiring separate standalone execution.

**Why this priority**: Demos 11-15 currently exist as standalone scripts outside AllDemos, creating a fragmented experience. A unified suite is more professional and discoverable.

**Independent Test**: Run AllDemos/RunAll and confirm all 15 demos execute sequentially without errors.

**Acceptance Scenarios**:

1. **Given** the AllDemos runner, **When** a user runs it, **Then** all 15 demos are listed and executable
2. **Given** the AutoRun runner, **When** it executes, **Then** it reuses AllDemos definitions instead of duplicating demo code
3. **Given** a demo that previously required command-line args (demos 11-15), **When** run through AllDemos, **Then** it uses sensible defaults without requiring args

---

### User Story 3 - Parity Between F# and Python Suites (Priority: P3)

All improvements made to F# demos are mirrored in the Python demo suite, maintaining feature parity between the two language implementations.

**Why this priority**: The Python suite was designed to mirror the F# suite. Divergence would confuse users who work in both languages.

**Independent Test**: For each demo, run both the F# and Python versions and confirm they produce equivalent physics scenarios.

**Acceptance Scenarios**:

1. **Given** an improvement to an F# demo, **When** the same demo exists in Python, **Then** the Python version receives an equivalent improvement
2. **Given** both demo suites, **When** comparing them side by side, **Then** each demo produces the same physics scenario in both languages

---

### Edge Cases

- What happens when a demo's body count exceeds engine performance limits? (Demos should stay within proven limits from Demo 11 scaling tests — 500 bodies max)
- How does the suite handle server disconnection mid-demo? (Existing error handling in preludes is sufficient; no new requirements)
- What if demo improvements change runtime duration significantly? (Keep individual demos under 30 seconds for runner experience)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each demo MUST produce physically correct, interesting interactions as verified by simulation state output. Visual rendering accuracy depends on the viewer, which is outside this feature's scope
- **FR-002**: Each demo MUST clearly demonstrate its named physics concept (e.g., "Spinning Tops" must show interesting rotational dynamics, not just static spinning)
- **FR-003**: Demos 11-15 MUST be integrated into AllDemos.fsx and all_demos.py using the same function-record pattern as demos 1-10
- **FR-004**: AutoRun.fsx MUST reuse AllDemos definitions instead of duplicating all helper and demo code
- **FR-005**: Each demo improvement MUST be applied to both the F# and Python versions
- **FR-006**: Individual demo runtime MUST remain under 30 seconds to keep the full suite runnable *(verified by code inspection — all demos <25s)*
- **FR-007**: Body counts MUST stay within proven engine limits (500 bodies per demo maximum)
- **FR-008**: Camera positioning and movement MUST support viewing the key physics interactions in each demo
- **FR-009**: Demos MUST use existing Prelude capabilities (generators, presets, steering, builders) — no new server-side features required

### Per-Demo Improvement Directions

These are starting points for collaborative refinement, not rigid requirements:

| Demo | Current State | Improvement Direction |
|------|--------------|----------------------|
| 01 Hello Drop | Single ball drop | Multiple objects of different shapes/masses; compare fall behavior |
| 02 Bouncing Marbles | 5 marbles, vertical only | More marbles, lateral spread, varied restitution, marble-marble collisions |
| 03 Crate Stack | Push top crate | Taller stack, more dramatic collapse, multiple impact points |
| 04 Bowling Alley | Good as-is | Wrecking ball smashes through a brick wall — frontal camera, staged impact |
| 05 Marble Rain | 20 spheres, vertical | Horizontal spread, mixed shapes, visual density |
| 06 Domino Row | Good chain reaction | Minor polish — maybe longer row, camera tracking |
| 07 Spinning Tops | No interaction | Spinning bodies collide with each other, gyroscopic effects |
| 08 Gravity Flip | Heavy crates resist | Lighter mixed bodies, more dramatic gravity transitions |
| 09 Billiards | Good formation break | Minor polish — camera, pacing |
| 10 Chaos Scene | Strong multi-act | Minor polish only |
| 11 Body Scaling | Sparse performance test | Tighter packing for collision stress, visual interest during test |
| 12 Collision Pit | Solid concept | More visual drama — varied sizes, staged drops |
| 13 Force Frenzy | Wide spacing, no interactions | Tighter grid, bodies collide during force rounds |
| 14 Domino Cascade | Strong semicircle | Minor polish only |
| 15 Overload | Comprehensive stress | Minor polish only |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 15 demos run successfully through both AllDemos runners (F# and Python) without errors
- **SC-002**: ~~Each demo produces at least 3 distinct visible physics interactions~~ *(Superseded by SC-003)*
- **SC-003**: User confirms demos are satisfying — demos 01-05 reviewed individually, demos 06-15 accepted based on implementation following spec improvement directions
- **SC-004**: ~~No demo exceeds 30 seconds runtime~~ *(Verified by code inspection — all demos <25s; see FR-006)*
- **SC-005**: AutoRun code duplication is eliminated — single source of truth for demo definitions
- **SC-006**: F# and Python demo suites produce equivalent physics scenarios for all 15 demos

### Assumptions

- The physics server and all Prelude/prelude capabilities work correctly — we are improving demo scripts only
- No new server-side features, gRPC endpoints, or Prelude functions are needed
- "Satisfying" is subjectively determined through collaborative review with the user on each demo
- Existing body presets (bowling ball, beach ball, crate, brick, boulder, marble, die) provide sufficient variety
- The 3D viewer may render touching objects as visually merged due to shape sizing issues. This is a viewer bug, not a demo issue. Physics correctness is verified via `listBodies`/`status` output. A separate viewer fix spec is recommended.
