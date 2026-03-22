# Drift Resolution Proposals

Generated: 2026-03-22
Based on: drift-report from 2026-03-22

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 2 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 004-fsharp-scripting-library/SC-004

**Direction**: BACKFILL

**Current State**:
- Spec says: "Adding a new public function to the library requires changes to at most 2 files (implementation + signature file) and no changes to existing consumers."
- Code does: Adding `toTuple` required 4 file changes — Vec3Builders.fsi + Vec3Builders.fs (module-level) plus Prelude.fsi + Prelude.fs (re-export). Module-level access works with 2 files; Prelude convenience re-export adds 2 more.

**Proposed Resolution**:

Update SC-004 in `specs/004-fsharp-scripting-library/spec.md` from:

> - **SC-004**: Adding a new public function to the library requires changes to at most 2 files (implementation + signature file) and no changes to existing consumers.

To:

> - **SC-004**: Adding a new public function to the library requires changes to at most 2 files per module (implementation + signature file) and no changes to existing consumers. Optionally, 2 additional files (Prelude.fsi + Prelude.fs) may be updated to re-export the function for script convenience.

**Rationale**: The code is correct and well-designed. The Prelude re-export is an intentional convenience layer. The spec should reflect the actual 2-tier pattern: module access (2 files) + optional Prelude re-export (2 more). Tests pass, architecture is sound, and the 2-file per-module guarantee holds.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: Backfill `toTuple` into spec

**Direction**: BACKFILL

**Current State**:
- Spec FR-001 lists 12 Prelude functions as the required set
- Code has 13 functions (12 original + `toTuple`)
- `toTuple` was added during US4 extensibility validation

**Proposed Resolution**:

No spec change needed. FR-001 specifies the *minimum* set ("all current Prelude.fsx functions"), not a maximum. Adding functions beyond the baseline is expected per the Assumptions section: "additional functions may be added over time." The `toTuple` function validates the extensibility story (US4) and does not conflict with any requirement.

**Rationale**: The spec defines a floor, not a ceiling. No update warranted.

**Confidence**: HIGH

**Action**:
- [ ] Approve (no change needed)
- [ ] Reject
- [ ] Modify
