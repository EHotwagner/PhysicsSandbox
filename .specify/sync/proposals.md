# Drift Resolution Proposals

Generated: 2026-03-25
Based on: drift-report from 2026-03-25T14:00:00Z

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code -> Spec) | 3 |
| Align (Spec -> Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 004-test-suite-cleanup/SC-002

**Direction**: BACKFILL

**Current State**:
- Spec says: "Number of test files containing duplicated helper functions decreases by at least 50%"
- Code does: 2 of 3 duplicated helpers centralized (67%). The third (`makeResolver`) was intentionally kept local because PhysicsClient.MeshResolver and PhysicsViewer.Streaming.MeshResolver are different APIs — a shared builder would require importing both modules.

**Proposed Resolution**:

Update SC-002 from:
```
- **SC-002**: Number of test files containing duplicated helper functions decreases by at least 50%
```
To:
```
- **SC-002**: All extractable duplicated test helper functions (those with identical signatures and semantics across projects) are centralized in shared utility modules
```

**Rationale**: The original metric ("50% decrease") was based on an initial estimate that more helpers were duplicated than actually were. Research revealed only 3 true duplicates, and 1 of those (`makeResolver`) uses different underlying APIs in each project, making extraction counterproductive. The code correctly centralizes the 2 that can be shared. Backfilling the spec to match the implementation is the right call — the code represents a well-reasoned design decision documented in plan.md D2.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: 004-test-suite-cleanup/US2 scope expansion

**Direction**: BACKFILL

**Current State**:
- Spec says: "5 integration test files contain only 1 test each (DiagnosticsIntegrationTests, ComparisonIntegrationTests, StaticBodyTests, StressTestIntegrationTests, RestartIntegrationTests)"
- Code does: 6 files were merged — the 5 listed plus CommandAuditStreamTests.cs, which was discovered as a single-test file during SC-004 validation.

**Proposed Resolution**:

Update US2 description from:
```
A developer notices that 5 integration test files contain only 1 test each (DiagnosticsIntegrationTests, ComparisonIntegrationTests, StaticBodyTests, StressTestIntegrationTests, RestartIntegrationTests).
```
To:
```
A developer notices that 6 integration test files contain only 1 test each (DiagnosticsIntegrationTests, ComparisonIntegrationTests, StaticBodyTests, StressTestIntegrationTests, RestartIntegrationTests, CommandAuditStreamTests).
```

And update acceptance scenario 1 from "5 integration test files" to "6 integration test files".

**Rationale**: The additional file was discovered during implementation validation (SC-004 requires zero single-test files). Merging it was the correct action to satisfy the success criterion. The spec should reflect the actual scope of work performed.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: Spec status update

**Direction**: BACKFILL

**Current State**:
- Spec status: "Draft"
- Implementation: Complete, all success criteria met

**Proposed Resolution**:

Update spec.md header from `**Status**: Draft` to `**Status**: Implemented`.

**Rationale**: All requirements implemented and validated. No outstanding items.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
