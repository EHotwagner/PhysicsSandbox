# Drift Resolution Proposals

Generated: 2026-03-22
Based on: drift-report from 2026-03-22 (004-scripting-nuget-package)

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 1 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 1 |

## Proposals

### Proposal 1: 004-scripting-nuget-package/US3-Scenario-2

**Direction**: BACKFILL

**Current State**:
- Spec says: "Given the shared Prelude.fsx preamble has been updated, When any script that loads it runs, Then all scripting library modules are available"
- Code does: `Scripting/scripts/Prelude.fsx` was deleted. `HelloDrop.fsx` inlines `#r "nuget: PhysicsSandbox.Scripting"` directly. With NuGet packaging, the preamble is a single line and not worth a separate file.

**Proposed Resolution**:

Replace US3 acceptance scenario 2 in `specs/004-scripting-nuget-package/spec.md` from:

> 2. **Given** the shared Prelude.fsx preamble has been updated, **When** any script that loads it runs, **Then** all scripting library modules are available.

To:

> 2. **Given** a script includes `#r "nuget: PhysicsSandbox.Scripting"`, **When** the script is executed, **Then** all scripting library modules and transitive dependencies are available without additional references.

**Rationale**: The code is simpler than the spec anticipated. With NuGet packaging, a single `#r "nuget: ..."` line replaces the need for a shared preamble file. The Prelude.fsx indirection layer added no value and was correctly eliminated. Tests pass, scripts work.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: 004-scripting-nuget-package/SC-004

**Direction**: REMOVE FROM SPEC

**Current State**:
- Spec says: "The pack-and-publish workflow completes in a single command sequence, following the established local NuGet pattern."
- Code does: Pack commands run manually in dependency order (4 sequential `dotnet pack` commands). No automation script exists.

**Proposed Resolution**:

Reword SC-004 in `specs/004-scripting-nuget-package/spec.md` from:

> - **SC-004**: The pack-and-publish workflow completes in a single command sequence, following the established local NuGet pattern.

To:

> - **SC-004**: The pack-and-publish workflow follows the established local NuGet pattern (dependency-ordered `dotnet pack` with `-p:NoWarn=NU5104 -o ~/.local/share/nuget-local/`).

**Rationale**: "Single command sequence" implies a script or automation that doesn't exist and wasn't part of the BepuFSharp pattern being followed. BepuFSharp itself uses manual `dotnet pack` commands. The intent was to follow that convention, which is satisfied. A pack script could be added later but is not a requirement of this feature.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: Backfill mcpReport.md port fix into FR-008 scope

**Direction**: BACKFILL

**Current State**:
- Spec FR-008 says: "All server port references across scripts and documentation MUST use the canonical ports"
- Code does: `reports/mcpReport.md` was also fixed (localhost:5000 → localhost:5180), which is within the spirit of FR-008 ("documentation") but the file wasn't explicitly listed.

**Proposed Resolution**:

No spec change needed. FR-008 says "scripts and documentation" — `reports/mcpReport.md` is documentation. The fix is within scope. The report file simply wasn't enumerated in the plan's port table but was correctly caught during verification.

**Rationale**: Already covered by FR-008's wording. No update warranted.

**Confidence**: HIGH

**Action**:
- [ ] Approve (no change needed)
- [ ] Reject
- [ ] Modify
