# Implementation Plan: Improve Physics Demos

**Branch**: `004-improve-demos` | **Date**: 2026-03-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-improve-demos/spec.md`

## Summary

Improve all 15 physics demos to be more visually interesting and physically rich, integrate demos 11-15 into the AllDemos runners, eliminate AutoRun code duplication, and maintain F#/Python parity. Work collaboratively on each demo — discuss, tweak, iterate.

## Technical Context

**Language/Version**: F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio
**Primary Dependencies**: PhysicsClient (F# NuGet), prelude.py (Python), existing Prelude.fsx helpers
**Storage**: N/A (stateless scripts communicating with running physics server)
**Testing**: Manual demo execution + AllDemos runner validation; existing unit/integration tests for server
**Target Platform**: Linux container with GPU passthrough
**Project Type**: Demo scripts (F# .fsx and Python .py)
**Performance Goals**: Each demo completes in under 30 seconds; max 500 bodies per demo
**Constraints**: Must use existing Prelude capabilities only — no server-side changes
**Scale/Scope**: 15 F# demos + 15 Python demos + 3 F# runners + 3 Python runners = 36 files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicable? | Status | Notes |
|-----------|------------|--------|-------|
| I. Service Independence | No | N/A | No new services — scripts only |
| II. Contract-First | No | N/A | No new service boundaries or contracts |
| III. Shared Nothing | No | N/A | No cross-service dependencies |
| IV. Spec-First | Yes | ✅ PASS | Spec exists at specs/004-improve-demos/spec.md |
| V. Compiler-Enforced Contracts | No | N/A | No new public F# modules; .fsx scripts don't use .fsi files |
| VI. Test Evidence | Partial | ✅ PASS | Demo scripts are verified by running through AllDemos runners. No unit tests needed for scripts — the physics server tests cover correctness |
| VII. Observability | No | N/A | Scripts, not services |

**Gate result**: PASS — no violations. All applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/004-improve-demos/
├── plan.md              # This file
├── research.md          # Phase 0 output (minimal — no unknowns)
├── quickstart.md        # Phase 1 output
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
Scripting/
├── demos/                          # F# demo scripts (15 demos + runners)
│   ├── Prelude.fsx                 # Shared helpers (no changes expected)
│   ├── Demo01_HelloDrop.fsx        # Improve: multi-object comparison
│   ├── Demo02_BouncingMarbles.fsx  # Improve: more marbles, lateral spread
│   ├── Demo03_CrateStack.fsx      # Improve: taller, more dramatic collapse
│   ├── Demo04_BowlingAlley.fsx    # Polish: camera, pacing
│   ├── Demo05_MarbleRain.fsx      # Improve: mixed shapes, density
│   ├── Demo06_DominoRow.fsx       # Polish: longer row, camera tracking
│   ├── Demo07_SpinningTops.fsx    # Improve: spinning collisions
│   ├── Demo08_GravityFlip.fsx     # Improve: lighter bodies, dramatic
│   ├── Demo09_Billiards.fsx       # Polish: camera, pacing
│   ├── Demo10_Chaos.fsx           # Polish: minor only
│   ├── Demo11_BodyScaling.fsx     # Improve: tighter packing + integrate
│   ├── Demo12_CollisionPit.fsx    # Improve: varied sizes, staged drops
│   ├── Demo13_ForceFrenzy.fsx     # Improve: tighter grid, collisions
│   ├── Demo14_DominoCascade.fsx   # Polish: minor only
│   ├── Demo15_Overload.fsx        # Polish: minor only
│   ├── AllDemos.fsx               # FIX: integrate demos 11-15
│   ├── AutoRun.fsx                # FIX: reuse AllDemos, eliminate duplication
│   └── RunAll.fsx                 # Update if needed for 11-15
│
└── demos_py/                       # Python demo scripts (mirror F# changes)
    ├── prelude.py                  # Shared helpers (no changes expected)
    ├── demo01_hello_drop.py        # Mirror F# improvements
    ├── ...                         # (all 15 demos mirror F# changes)
    ├── all_demos.py               # FIX: integrate demos 11-15
    ├── auto_run.py                # FIX: reuse all_demos, eliminate duplication
    └── run_all.py                 # Update if needed for 11-15
```

**Structure Decision**: No new files or directories. All changes are to existing demo scripts and runners within the existing `Scripting/demos/` and `Scripting/demos_py/` directories.

## Implementation Approach

### Collaborative Per-Demo Workflow

For each demo (01 through 15):

1. **Review** — Read current F# demo, discuss what would make it more satisfying
2. **Improve F#** — Edit the F# version with agreed improvements
3. **Mirror Python** — Apply equivalent changes to the Python version
4. **Confirm** — User runs or reviews, confirms satisfaction

### Demo Categorization

**Major improvements** (thin demos needing significant rework):
- Demo 01: Hello Drop — expand from 1 body to multi-object shape/mass comparison
- Demo 02: Bouncing Marbles — more bodies, lateral spread, marble-marble collisions
- Demo 07: Spinning Tops — add body-body collisions during spin
- Demo 08: Gravity Flip — replace heavy crates with lighter mixed bodies
- Demo 11: Body Scaling — tighter packing for collision density during stress test
- Demo 13: Force Frenzy — tighter grid so bodies interact during force rounds

**Moderate improvements** (solid concept, needs enrichment):
- Demo 03: Crate Stack — taller stack, multiple impact angles
- Demo 05: Marble Rain — mixed shapes, horizontal distribution
- Demo 12: Collision Pit — varied sphere sizes, staged drop waves

**Polish only** (already strong, minor camera/pacing tweaks):
- Demo 04: Bowling Alley
- Demo 06: Domino Row
- Demo 09: Billiards
- Demo 10: Chaos Scene
- Demo 14: Domino Cascade
- Demo 15: Overload

### Structural Fixes (do first)

1. **Integrate demos 11-15 into AllDemos.fsx** — Convert standalone scripts to the `{ Name; Description; Run }` record pattern, using sensible defaults instead of command-line args
2. **Fix AutoRun.fsx** — Replace duplicated demo definitions with `#load "AllDemos.fsx"` reference
3. **Mirror structural fixes in Python** — Same changes to all_demos.py and auto_run.py

### Design Decisions

- **No Prelude changes**: All improvements use existing helpers (generators, presets, steering, builders)
- **No server changes**: Demos communicate with the existing physics server via existing gRPC API
- **No new body types**: Use the 7 existing presets (marble, bowling ball, beach ball, boulder, crate, brick, die) in more creative combinations
- **Body count limits**: Keep each demo under 500 bodies (proven safe in Demo 11 scaling tests)
- **Runtime limits**: Each demo stays under 30 seconds

## Complexity Tracking

No constitution violations to justify. This feature modifies only script files — no new services, contracts, modules, or dependencies.

## Post-Design Constitution Re-Check

| Principle | Status | Notes |
|-----------|--------|-------|
| IV. Spec-First | ✅ PASS | Spec and plan complete |
| VI. Test Evidence | ✅ PASS | AllDemos runner serves as integration verification |

No violations introduced by the design.
