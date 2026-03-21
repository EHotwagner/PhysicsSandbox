# Implementation Plan: Stress Test Demos

**Branch**: `003-stress-test-demos` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-stress-test-demos/spec.md`

## Summary

Add 5+ stress test demo scripts to the existing demo suite, each designed to push a specific system axis (body count, collision density, bulk forces, combined load, MCP comparison) beyond normal usage. Demos follow existing Prelude.fsx conventions, integrate into AllDemos/RunAll/AutoRun, and print timing markers to identify degradation points. A new `timed` helper in Prelude.fsx provides elapsed-time reporting. No new compiled code, services, or proto contracts are needed — this is a scripts-only feature.

## Technical Context

**Language/Version**: F# scripts (.fsx) on .NET 10.0
**Primary Dependencies**: PhysicsClient.dll (existing), PhysicsSandbox.Shared.Contracts.dll (existing), Grpc.Net.Client, Google.Protobuf
**Storage**: N/A (in-memory physics simulation)
**Testing**: Demo scripts are self-testing (run or fail); AutoRun.fsx provides pass/fail reporting; existing integration tests cover gRPC infrastructure
**Target Platform**: Linux with GPU (container)
**Project Type**: Demo scripts extending existing physics sandbox
**Performance Goals**: Identify degradation point (body count where step > 100ms); run each demo within 5 minutes
**Constraints**: 100-command batch limit (auto-split by batchAdd); single stress test at a time via MCP
**Scale/Scope**: 5–7 new demo scripts, ~50–100 bodies per collision demo, up to 500 bodies for scaling demo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies? | Status | Notes |
|-----------|----------|--------|-------|
| I. Service Independence | No | Pass | No new services — scripts only |
| II. Contract-First | No | Pass | No new service boundaries or proto contracts |
| III. Shared Nothing | No | Pass | Scripts use existing PhysicsClient library |
| IV. Spec-First | Yes | Pass | Spec complete at specs/003-stress-test-demos/spec.md |
| V. Compiler-Enforced (.fsi) | No | Pass | .fsx scripts have no .fsi requirement |
| VI. Test Evidence | Partial | Pass | AutoRun.fsx provides pass/fail for each demo; no new compiled code requiring unit tests |
| VII. Observability | No | Pass | Scripts print timing markers; no new service instrumentation needed |

**Gate result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/003-stress-test-demos/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
demos/
├── Prelude.fsx              # MODIFY: add timed helper
├── AllDemos.fsx             # MODIFY: add 5+ new stress demo entries
├── RunAll.fsx               # MODIFY: update demo count display
├── AutoRun.fsx              # MODIFY: add new demos inline (self-contained)
├── Demo11_BodyScaling.fsx   # NEW: progressive body count (50/100/200/500)
├── Demo12_CollisionPit.fsx  # NEW: 100+ bodies in confined space
├── Demo13_ForceFrenzy.fsx   # NEW: bulk impulses/torques on 100+ bodies
├── Demo14_DominoCascade.fsx # NEW: 100+ domino chain reaction
├── Demo15_Overload.fsx      # NEW: combined stress (bodies + forces + camera)
```

**Structure Decision**: All new code lives in `demos/` as .fsx scripts following the existing pattern. No new compiled projects needed. Each demo is defined as a function in AllDemos.fsx and duplicated as a self-contained inline in AutoRun.fsx (matching existing convention).

## Demo Designs

### Demo 11: Body Scaling
Progressive body creation in tiers: 50, 100, 200, 500. At each tier, run simulation for a few seconds and print elapsed time per tier. Reports degradation point (first tier where step time > 100ms or visible lag). Uses batchAdd with spheres in a grid pattern. Camera pulls back at each tier to show the full scene.

### Demo 12: Collision Pit
Create a "pit" using walls (static boxes), then drop 100–150 spheres from height into the confined space. All bodies collide simultaneously in the pit. Observe settling time and whether any bodies escape or pass through walls. Print collision density observation (time to settle).

### Demo 13: Force Frenzy
Create 100 bodies in a grid, let them settle, then batch-apply random impulses to all bodies simultaneously. Repeat 3 rounds with increasing force magnitude. Also applies rapid gravity changes between rounds. Reports timing for each force application round.

### Demo 14: Domino Cascade
Scale up Demo 06 from 12 to 100+ dominoes in a curved path. Push the first one and observe propagation. Reports total cascade time and whether the chain completes. Camera follows the cascade along the path.

### Demo 15: Overload
Combined scenario: build a pyramid (50 bodies), add random spheres (50), apply impulses to all, change gravity, sweep camera — all in rapid succession. Reports per-stage timing and total elapsed. This is Demo 10 (Chaos) scaled up 3x with timing markers.

## Prelude.fsx Enhancement

Add a `timed` helper that wraps an action and prints elapsed time:

```fsharp
let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result
```

This gives all demos a consistent way to report timing without duplicating Stopwatch boilerplate.

## MCP Comparison (FR-010)

The body-scaling and overload demos can be replicated via MCP tools:
- `batch_commands` for body creation and force application
- `get_diagnostics` for pipeline timing
- `start_stress_test` with `body-scaling` scenario for automated comparison

No new MCP tools are needed. The existing 38 tools are sufficient.

## Complexity Tracking

No constitution violations to justify — all gates pass.
