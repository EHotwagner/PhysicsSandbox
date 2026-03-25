# Implementation Plan: Static Mesh Terrain Demos

**Branch**: `004-mesh-terrain-demos` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-mesh-terrain-demos/spec.md`

## Summary

Add two new demo scripts (Demo 23: Ball Rollercoaster, Demo 24: Halfpipe Arena) that showcase static mesh triangles as sculptured terrain. Both demos construct complex 3D surfaces from mesh triangles (mass=0, static bodies), drop dynamic objects onto them, and include cinematic camera work and narration. Implementation is script-only — no new services, proto messages, or compiled modules. F# (.fsx) and Python (.py) versions of each demo, plus registration in the demo suite runners.

## Technical Context

**Language/Version**: F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio
**Primary Dependencies**: PhysicsClient 0.4.0 (NuGet, F#), PhysicsSandbox.Shared.Contracts 0.4.0 (proto types), grpcio + protobuf (Python)
**Storage**: N/A (stateless scripts communicating with running physics server)
**Testing**: Manual execution against running PhysicsSandbox server; inclusion in AutoRun.fsx/auto_run.py suite
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Demo scripts (not compiled projects)
**Performance Goals**: Each demo completes within 30 seconds; mesh terrain renders at interactive frame rates
**Constraints**: Triangle count per mesh body must stay practical (target 100-300 triangles per terrain piece); batch commands chunked in 100s
**Scale/Scope**: 2 new demo scripts (4 files F#, 4 files Python), updates to 3 runner files per language

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicable? | Status | Notes |
|-----------|-------------|--------|-------|
| I. Service Independence | No | N/A | No new services — scripts only |
| II. Contract-First | No | N/A | No new proto messages — uses existing MeshShape, AddBody, etc. |
| III. Shared Nothing | No | N/A | Scripts are standalone; no cross-service dependencies |
| IV. Spec-First Delivery | Yes | PASS | Spec created and clarified before planning |
| V. Compiler-Enforced Contracts (.fsi) | No | N/A | .fsx scripts have no public module surface — not compiled |
| VI. Test Evidence | Partial | PASS | Demo scripts are self-verifying (run against live server); no unit test targets for scripts. Suite runners provide regression coverage. |
| VII. Observability by Default | No | N/A | Scripts use existing server instrumentation |

**Gate result**: PASS — no violations. This is a script-only feature with no service, contract, or public API changes.

## Project Structure

### Documentation (this feature)

```text
specs/004-mesh-terrain-demos/
├── plan.md              # This file
├── research.md          # Phase 0: mesh geometry research
├── data-model.md        # Phase 1: terrain geometry definitions
├── quickstart.md        # Phase 1: how to run the demos
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Scripting/
├── demos/
│   ├── Demo23_BallRollercoaster.fsx    # NEW — F# rollercoaster demo
│   ├── Demo24_HalfpipeArena.fsx        # NEW — F# halfpipe demo
│   ├── AllDemos.fsx                    # MODIFIED — add Demo 23, 24 entries
│   ├── RunAll.fsx                      # MODIFIED (if needed — may auto-discover)
│   └── AutoRun.fsx                     # MODIFIED (if needed)
├── demos_py/
│   ├── demo23_ball_rollercoaster.py    # NEW — Python rollercoaster demo
│   ├── demo24_halfpipe_arena.py        # NEW — Python halfpipe demo
│   ├── all_demos.py                    # MODIFIED — add Demo 23, 24 entries
│   ├── run_all.py                      # MODIFIED (if needed)
│   └── auto_run.py                     # MODIFIED (if needed)
└── demos/Prelude.fsx                   # UNCHANGED — existing helpers sufficient
```

**Structure Decision**: Script-only addition to existing `Scripting/demos/` and `Scripting/demos_py/` directories. No new compiled projects, no solution file changes.

## Complexity Tracking

No constitution violations — table not needed.

## Design Decisions

### Mesh Terrain Construction Approach

Each terrain is a single static mesh body (mass=0.0) created via `makeMeshCmd`. Triangles are generated procedurally using parametric math functions:

**Demo 23 — Rollercoaster Track**:
- Track path defined as a parametric curve with elevation changes (sine waves for hills, steep linear drops, banked turns via lateral tilt)
- Cross-section: flat strip with raised edges (3 triangles wide per segment — left wall, floor, right wall)
- Segments: ~15-20 along the track path, each producing 6 triangles (2 per strip face) = ~90-120 triangles total
- Track colored with `structureColor` (gray); sidewalls slightly darker
- Balls (5-8) released at the top with staggered timing

**Demo 24 — Halfpipe Arena**:
- Halfpipe cross-section: semicircular arc discretized into 8-12 strips
- Extruded along a straight path with ~10-15 segments = ~160-360 triangles total
- Optional: bowl end-caps (quarter-sphere approximation) for a skate park feel
- Terrain colored with `accentPurple` or custom color; dynamic objects in `projectileColor`/`accentYellow`
- Objects (5-8 balls + 2-3 capsules) dropped from varying heights

### Camera Strategy

Both demos follow the established Demo 22 pattern:
1. Opening wide shot via `smoothCamera` showing full terrain
2. Action phase: `followBody` or `chaseBody` tracking lead ball
3. Mid-demo: `orbitBody` around terrain center for dramatic effect
4. Closing: wide pullback via `smoothCamera`
5. Narration text at each transition describing what's happening

### Material Properties

- Rollercoaster track: `slipperyMaterial` (friction=0.01) for fast rolling
- Halfpipe surface: custom moderate friction (0.3) for realistic oscillation with gradual energy loss
- Dynamic balls: default material (no override) or slightly bouncy

### Ground Plane

Both demos call `resetSimulation` which adds a default ground plane. The mesh terrain sits above this plane. Balls that fall off the terrain land on the ground plane below — this is intentional per the edge case spec.
