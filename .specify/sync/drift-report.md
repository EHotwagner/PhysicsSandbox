# Spec Drift Report

Generated: 2026-03-22
Project: PhysicsSandbox

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 (004-fsharp-scripting-library) |
| Requirements Checked | 14 (10 FR + 4 SC) |
| Aligned | 13 (93%) |
| Drifted | 1 (7%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 004-fsharp-scripting-library — F# Scripting Library

#### Aligned ✓

- FR-001: Library bundles all 12 Prelude.fsx functions → `src/PhysicsSandbox.Scripting/Prelude.fsi` (all 12 present)
- FR-002: Single #r directive reference → `scripts/Prelude.fsx` (one `#r` line, no nuget directives)
- FR-003: Project reference by MCP server → `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` (ProjectReference added)
- FR-004: scratch/ folder gitignored → `scratch/.gitkeep` exists, `.gitignore` has `scratch/*` and `!scratch/.gitkeep`
- FR-005: scripts/ folder at repo root → `scripts/Prelude.fsx` and `scripts/HelloDrop.fsx` present
- FR-006: Logical module organization → 6 modules: Helpers, Vec3Builders, CommandBuilders, BatchOperations, SimulationLifecycle, Prelude
- FR-007: .fsi signature files for all modules → All 6 modules have matching .fsi files
- FR-008: Dependencies re-exported → `scripts/Prelude.fsx` has zero nuget directives; all flow through the DLL
- FR-009: scratch/scripts portability → `scratch/Prelude.fsx` and `scripts/Prelude.fsx` are identical
- FR-010: Library in solution → `PhysicsSandbox.slnx` has both library and test project entries
- SC-001: All 12 Prelude functions available → Verified via Prelude.fsi and SurfaceAreaTests (19 tests pass)
- SC-002: Under 5 lines boilerplate → `scripts/Prelude.fsx` has exactly 5 executable lines (1 #r + 4 opens)
- SC-003: MCP uses shared function → `ClientAdapter.fs` delegates `toVec3` to `PhysicsSandbox.Scripting.Vec3Builders.toVec3`

#### Drifted ⚠️

- SC-004: Spec says "at most 2 files" but `toTuple` extensibility proof required 4 files (Vec3Builders.fsi + .fs + Prelude.fsi + .fs)
  - Location: `src/PhysicsSandbox.Scripting/Prelude.fsi:11`, `src/PhysicsSandbox.Scripting/Prelude.fs:11`
  - Severity: minor
  - Note: The 2-file claim holds for module-level access. Re-exporting via Prelude is an optional convenience step that adds 2 more files.

#### Not Implemented ✗

(none)

### Unspecced Code 🆕

| Feature | Location | Lines | Suggested Spec |
|---------|----------|-------|----------------|
| `toTuple` function | `src/PhysicsSandbox.Scripting/Vec3Builders.fs:10` | 1 | 004 (extensibility proof from US4) |

## Inter-Spec Conflicts

None.

## Recommendations

1. **SC-004 clarification (minor)**: Update to "at most 2 files per module, plus optionally 2 more if re-exporting via Prelude."
2. **Spec status**: Update from "Draft" to "Implemented."
