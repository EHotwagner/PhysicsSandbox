# Implementation Plan: Enhance Demos

**Branch**: `005-enhance-demos` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-enhance-demos/spec.md`

## Summary

Fix broken impact demos (Crate Stack projectile misalignment, Bowling Alley side-hit), add 3 new demos showcasing constraints, physics queries, and kinematic bodies, enhance existing demos with advanced shapes (capsule, cylinder, convex hull, triangle, compound), expand color and material usage across the suite, and maintain F#/Python parity throughout. Total demo count: 15 → 18.

## Technical Context

**Language/Version**: F# scripts (.fsx) on .NET 10.0; Python 3.10+ with grpcio
**Primary Dependencies**: PhysicsClient.dll (NuGet), PhysicsSandbox.Shared.Contracts.dll (proto types), Grpc.Net.Client 2.x, Google.Protobuf 3.x
**Storage**: N/A (stateless scripts communicating with running server)
**Testing**: AutoRun.fsx (F# demo runner, 0-failure gate), auto_run.py (Python equivalent)
**Target Platform**: Linux (container with GPU passthrough for viewer)
**Project Type**: Demo scripts (not a library or service — .fsx/.py files only)
**Performance Goals**: All 18 demos complete within AutoRun timeout; individual demos under 30s
**Constraints**: No changes to server, simulation, viewer, client library, or proto contracts
**Scale/Scope**: 18 F# demos + 18 Python demos, ~50-100 lines each; Prelude.fsx/prelude.py extensions for new shape/constraint/query helpers

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | N/A | No service changes — scripts only |
| II. Contract-First | PASS | Using existing proto contracts; no new RPCs |
| III. Shared Nothing | N/A | No cross-service dependencies |
| IV. Spec-First Delivery | PASS | Spec written, plan in progress |
| V. Compiler-Enforced Contracts | CONDITIONAL | Only applies if Prelude.fsx helpers are promoted to Scripting library. If new helpers stay in Prelude.fsx (script-only), no .fsi required. If added to `PhysicsSandbox.Scripting`, .fsi + surface area updates required. |
| VI. Test Evidence | PASS | AutoRun.fsx 0-failure is the test gate; each demo is a behavioral test |
| VII. Observability | N/A | Demo scripts, not services |

**Gate result**: PASS — all applicable principles satisfied. Principle V conditional on implementation approach (see research).

## Project Structure

### Documentation (this feature)

```text
specs/005-enhance-demos/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Scripting/
├── demos/
│   ├── Prelude.fsx              # MODIFY: add helpers for compound, convex hull, triangle, kinematic, queries
│   ├── AllDemos.fsx             # MODIFY: register 3 new demos, update descriptions
│   ├── AutoRun.fsx              # NO CHANGE (loads AllDemos dynamically)
│   ├── RunAll.fsx               # NO CHANGE
│   ├── 01_HelloDrop.fsx         # ENHANCE: already has colors/materials, add capsule + cylinder variety
│   ├── 02_BouncingMarbles.fsx   # ENHANCE: add colors per wave
│   ├── 03_CrateStack.fsx        # FIX: align boulder with tower, increase impulse
│   ├── 04_BowlingAlley.fsx      # FIX: align ball with pyramid frontally, increase impulse
│   ├── 05_MarbleRain.fsx        # ENHANCE: add capsules + cylinders to rain mix, colors
│   ├── 06_DominoRow.fsx         # ENHANCE: add colors (gradient along row)
│   ├── 07_SpinningTops.fsx      # ENHANCE: use capsule + cylinder shapes, colors
│   ├── 08_GravityFlip.fsx       # ENHANCE: mixed shapes (capsule, cylinder, convex hull), colors
│   ├── 09_Billiards.fsx         # ENHANCE: per-ball colors, slippery material for table feel
│   ├── 10_ChaosScene.fsx        # ENHANCE: mix in advanced shapes, fix boulder targeting, colors
│   ├── 11_BodyScaling.fsx       # ENHANCE: add capsule + cylinder + compound to shape mix
│   ├── 12_CollisionPit.fsx      # ENHANCE: add compound shapes to pit, colors by wave
│   ├── 13_ForceFrenzy.fsx       # ENHANCE: bouncy + sticky materials for contrast, colors
│   ├── 14_DominoCascade.fsx     # ENHANCE: color gradient along semicircle
│   ├── 15_Overload.fsx          # ENHANCE: advanced shapes in formations, colors
│   ├── 16_Constraints.fsx       # NEW: pendulum chain + hinged bridge + weld cluster
│   ├── 17_QueryRange.fsx        # NEW: raycast detection, sweep prediction, overlap counting
│   └── 18_KinematicSweep.fsx    # NEW: kinematic pusher plowing through dynamic bodies
├── demos_py/
│   ├── prelude.py               # MODIFY: add helpers matching F# Prelude extensions
│   ├── all_demos.py             # MODIFY: register 3 new demos
│   ├── demo_01_hello_drop.py    # ENHANCE: mirror F# changes
│   ├── ... (all demos mirror F# changes)
│   ├── demo_16_constraints.py   # NEW: mirror F# demo
│   ├── demo_17_query_range.py   # NEW: mirror F# demo
│   └── demo_18_kinematic_sweep.py # NEW: mirror F# demo
```

**Structure Decision**: All changes are in `Scripting/demos/` and `Scripting/demos_py/`. No new projects, services, or library modules. Prelude helpers stay in script files (not promoted to Scripting library) to keep the change scope minimal and avoid .fsi/surface-area obligations.

## Complexity Tracking

> No constitution violations. Table not needed.
