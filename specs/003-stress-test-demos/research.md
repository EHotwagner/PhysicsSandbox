# Research: Stress Test Demos

**Feature**: 003-stress-test-demos
**Date**: 2026-03-21

## R1: Existing Demo Conventions

**Decision**: Follow AllDemos.fsx pattern — each demo is a `{ Name; Description; Run }` record in the `demos` array.

**Rationale**: All 10 existing demos follow this pattern. RunAll.fsx and AutoRun.fsx iterate the array. Adding entries to the array automatically integrates new demos into both runners.

**Alternatives considered**:
- Separate StressDemos.fsx with its own array: Rejected because RunAll/AutoRun expect a single `demos` array. Would require runner changes.
- New compiled test project: Rejected — demos are interactive scripts, not automated test harnesses.

## R2: Batch Command Limits

**Decision**: Continue using batchAdd with 100-command auto-split. For demos needing 200+ bodies, batchAdd will issue multiple gRPC calls automatically.

**Rationale**: The 100-command limit is enforced in both Prelude.fsx (line 90) and BatchTools.fs. batchAdd handles splitting transparently. No changes needed.

**Alternatives considered**:
- Increasing batch limit: Rejected — would require gRPC message size changes and MCP tool updates across multiple files.

## R3: Timing Instrumentation

**Decision**: Add a `timed` helper to Prelude.fsx using System.Diagnostics.Stopwatch. Each stress demo wraps setup, simulation, and teardown phases in `timed` calls.

**Rationale**: Consistent timing format across all demos. Stopwatch is the standard .NET high-resolution timer. Printing `[TIME] label: N ms` makes degradation points immediately visible in console output.

**Alternatives considered**:
- Using the existing MCP `get_diagnostics` tool: Rejected for scripts — that tool reports server-side pipeline timing, not end-to-end demo timing. Both are useful but serve different purposes.
- Per-demo custom timing: Rejected — leads to inconsistent output formats.

## R4: Body Count Tiers for Scaling Demo

**Decision**: Use tiers of 50, 100, 200, 500 bodies. Reset simulation between tiers to isolate measurements.

**Rationale**: 50 is 2.5x the current max (20 in Marble Rain). 500 matches the StressTestRunner's default max_bodies. Resetting between tiers prevents cumulative effects from skewing measurements.

**Alternatives considered**:
- Cumulative scaling (add bodies without resetting): Considered but rejected for measurement clarity. Cumulative effects make it hard to attribute degradation to a specific body count.
- Higher ceiling (1000+): Deferred — start with 500 and extend if the system handles it easily.

## R5: Collision Pit Design

**Decision**: Use 4 static box walls forming a pit (open top), drop spheres from height into the pit. Walls are large static boxes (mass=0). Pit dimensions ~4x4x4 meters.

**Rationale**: Static bodies are approximated as large boxes (BepuPhysics2 has no infinite plane — see CLAUDE.md gotcha). A pit confines bodies to maximize collision density. Open top allows dropping.

**Alternatives considered**:
- Funnel shape: More complex geometry, same collision outcome. Pit is simpler.
- Using the ground plane + walls: Viable but pit gives better visual containment.

## R6: AutoRun.fsx Duplication

**Decision**: New demos must be added as inline functions in AutoRun.fsx (matching existing pattern where all helpers and demos are self-contained in one file).

**Rationale**: AutoRun.fsx is designed for CI/automated use — it cannot depend on `#load` directives that may not resolve in all environments. All 10 existing demos are duplicated inline.

**Alternatives considered**:
- Refactoring AutoRun to use `#load "AllDemos.fsx"`: Would simplify maintenance but changes the CI-friendly single-file design. Out of scope for this feature.
