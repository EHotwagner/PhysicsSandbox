# Spec Drift Report

Generated: 2026-03-22
Project: PhysicsSandbox
Scope: 004-scripting-nuget-package

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 14 (8 FR + 6 SC) |
| Aligned | 12 (86%) |
| Drifted | 1 (7%) |
| Not Implemented | 1 (7%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 004-scripting-nuget-package - Scripting Library NuGet Package

#### Aligned

- FR-001: All 4 projects packable with IsPackable, PackageId, Version → `src/*/.[cf]sproj`
- FR-002: Packages publishable to local feed → All 4 .nupkg files in `~/.local/share/nuget-local/`
- FR-003: ProjectReferences to Scripting replaced with PackageReferences → `PhysicsSandbox.Mcp.fsproj`, `PhysicsSandbox.Scripting.Tests.fsproj`
- FR-004: Script #r directives use version-agnostic NuGet references → `Scripting/scripts/HelloDrop.fsx`, `Scripting/demos/Prelude.fsx`, `Scripting/demos/AutoRun.fsx`
- FR-005: Scripting declares PhysicsClient as PackageReference → `PhysicsSandbox.Scripting.fsproj`
- FR-006: Pack workflow follows BepuFSharp conventions → `-p:NoWarn=NU5104 -o ~/.local/share/nuget-local/`
- FR-008: All localhost:5000 references corrected to 5180/7180 → Zero matches in `Scripting/`, `src/`, `.mcp.json`
- SC-001: Zero ProjectReferences to Scripting/PhysicsClient from external consumers → Verified
- SC-002: All existing tests pass → 19/19 scripting, 40/42 integration (2 pre-existing failures)
- SC-005: Zero localhost:5000 references in scripts/docs → Verified
- SC-006: Version increment on subsequent publishes → Documented in notes (process requirement)

#### Drifted

- US3-Scenario-2: Spec says "Given the shared Prelude.fsx preamble has been updated, When any script that loads it runs, Then all scripting library modules are available" but `Scripting/scripts/Prelude.fsx` was deleted post-implementation. `HelloDrop.fsx` now has `#r "nuget: PhysicsSandbox.Scripting"` inlined directly. The preamble is no longer needed since NuGet references are a single line.
  - Location: `Scripting/scripts/Prelude.fsx` (deleted)
  - Severity: minor — implementation is better than spec (simpler, fewer files), spec just needs updating

#### Not Implemented

- SC-004: "The pack-and-publish workflow completes in a single command sequence" — No pack script or documented single-command workflow exists. Pack commands were run manually in dependency order during implementation.
  - Severity: minor — could be addressed with a simple shell script

### Unspecced Code

| Feature | Location | Lines | Suggested Spec |
|---------|----------|-------|----------------|
| mcpReport.md port fix | `reports/mcpReport.md` | 1 line | 004-scripting-nuget-package (FR-008 scope expansion) |

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **Update spec US3-Scenario-2**: Remove reference to `scripts/Prelude.fsx` preamble — scripts now inline the NuGet reference directly. This is a positive drift (simpler architecture).
2. **Consider adding a pack script** (optional): A simple `pack.sh` that runs the 4 `dotnet pack` commands in dependency order would satisfy SC-004 and make the workflow reproducible.
3. **Update spec status**: Change from "Draft" to "Implemented".
