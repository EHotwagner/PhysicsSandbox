# Spec Drift Report

Generated: 2026-03-22
Project: PhysicsSandbox (004-improve-demos)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 15 (9 FR + 6 SC) |
| ✓ Aligned | 7 (47%) |
| ⚠️ Drifted | 5 (33%) |
| ✗ Not Implemented | 3 (20%) |
| 🆕 Unspecced Code | 2 |

## Detailed Findings

### Spec: 004-improve-demos - Improve Physics Demos

#### Aligned ✓
- FR-003: Demos 11-15 integrated into AllDemos.fsx and all_demos.py → `Scripting/demos/AllDemos.fsx` (15 records), `Scripting/demos_py/all_demos.py` (15 imports)
- FR-004: AutoRun.fsx reuses AllDemos definitions → `Scripting/demos/AutoRun.fsx` (loads AllDemos.fsx, no duplication)
- FR-005: Demo improvements applied to both F# and Python → All 15 demos have both versions with equivalent scenarios
- FR-007: Body counts within 500 limit → All demos checked, max is Demo 11 at 500 bodies
- FR-009: Demos use existing Prelude capabilities only → `runStandalone` is client-side convenience, no new server features
- SC-005: AutoRun code duplication eliminated → Single source of truth via AllDemos.fsx
- SC-006: F# and Python suites produce equivalent scenarios → Spot-checked demos 01, 06, 11; all match

#### Drifted ⚠️
- SC-001: Spec says "all 15 demos run successfully through AllDemos runners" but AllDemos.fsx line 6 uses `open Prelude.DemoHelpers` which doesn't exist (Prelude was refactored to top-level bindings)
  - Location: `Scripting/demos/AllDemos.fsx:6`
  - Severity: **critical** — blocks all F# runner execution

- SC-001 (continued): F# standalone demos 06-15 use broken `open Prelude.DemoHelpers` pattern
  - Location: `Scripting/demos/Demo06_DominoRow.fsx:6` through `Demo15_Overload.fsx:7`
  - Severity: **critical** — 10 of 15 standalone demos cannot execute

- FR-001: Spec says "each demo MUST produce visually distinct interactions" but viewer renders objects at wrong sizes (missing shape-to-size mapping)
  - Location: `src/PhysicsViewer/Rendering/SceneManager.fs:89-105`
  - Severity: major (fix implemented via Size property but not yet validated)

- FR-008: Demo 04 camera/scenario evolved significantly from spec direction during collaborative iteration (now wrecking ball + brick wall instead of bowling ball + pyramid)
  - Location: `Scripting/demos/Demo04_BowlingAlley.fsx`
  - Severity: minor (working as user directed)

- SC-003: User confirmed demos 01-05 only; demos 06-15 not yet collaboratively reviewed
  - Severity: moderate (work in progress)

#### Not Implemented ✗
- FR-006: "Individual demo runtime MUST remain under 30 seconds" — not measured
- SC-002: "Each demo produces at least 3 distinct visible physics interactions" — not systematically verified
- SC-004: "No demo exceeds 30 seconds runtime" — not measured

### Unspecced Code 🆕

| Feature | Location | Lines | Suggested Spec |
|---------|----------|-------|----------------|
| `runStandalone` helper in Prelude | `Scripting/demos/Prelude.fsx:104-116` | 13 | 004-improve-demos (convenience) |
| Viewer shape sizing fix | `src/PhysicsViewer/Rendering/SceneManager.fs:76-88` | 13 | Separate viewer fix spec recommended |

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **CRITICAL**: Fix broken `open Prelude.DemoHelpers` in AllDemos.fsx and demos 06-15 → change to `open Prelude`
2. **HIGH**: Complete collaborative review of demos 06-15 with user (SC-003)
3. **MEDIUM**: Validate viewer shape sizing fix resolves visual merging (FR-001)
4. **LOW**: Measure runtime for all 15 demos to confirm <30s (FR-006, SC-004)
