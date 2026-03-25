# Spec Drift Report

Generated: 2026-03-25
Project: PhysicsSandbox
Spec: 004-test-suite-cleanup

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 7 (FR) + 5 (SC) = 12 |
| Aligned | 11 (92%) |
| Drifted | 1 (8%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 004-test-suite-cleanup - Test Suite Cleanup

#### Aligned

- **FR-001**: Test coverage maintained or improved. 384 unit tests (was 383, +1 idempotency test). No behavior lost.
- **FR-002**: Duplicate helpers (`makeBody`, `makeState`) consolidated into `tests/CommonTestBuilders.fs`. Used by `StateStreamOptimizationTests.fs` and `StateDecompositionTests.fs`.
- **FR-003**: All single-test integration files consolidated. 6 files merged (5 planned + CommandAuditStreamTests.cs discovered during validation).
- **FR-004**: Shared utility module `tests/CommonTestBuilders.fs` created with `makeBody`, `makeState` helpers. Linked into 4 F# test projects via .fsproj.
- **FR-005**: `assertModuleSurface` helper added to `tests/SharedTestHelpers.fs`. Used in 5 SurfaceAreaTests.fs files (32 total calls). PhysicsServer.Tests has no SurfaceAreaTests.fs (correctly skipped).
- **FR-006**: All 4 oversized files split. Largest file now 23 tests (was 40). 10 new focused files created.
- **FR-007**: Full test suite passes. 384 unit tests green (1 pre-existing Mcp.Tests flake unchanged).
- **SC-001**: 384 vs 383 baseline = +0.3% (within 5% threshold).
- **SC-003**: Max tests per file = 23 (McpToolRegressionTests.cs). Under 25 limit.
- **SC-004**: Zero single-test integration files remain.
- **SC-005**: Full suite passes with same pre-existing failures as baseline.

#### Drifted

- **SC-002**: Spec says "Number of test files containing duplicated helper functions decreases by at least 50%"
  - **Spec expectation**: CommonTestBuilders referenced in 4+ test files (as stated in tasks.md checkpoint)
  - **Actual**: `open CommonTestBuilders` appears in 2 test files (StateStreamOptimizationTests.fs, StateDecompositionTests.fs). The MeshResolver `makeResolver` helper was NOT extracted because PhysicsClient and PhysicsViewer use different MeshResolver modules (`PhysicsClient.MeshResolver` vs `PhysicsViewer.Streaming.MeshResolver`), making a shared builder impractical.
  - **Impact**: Minor. The 2 actual duplicated helpers (makeBody, makeState) ARE centralized. The makeResolver duplication remains by design (different APIs).
  - Severity: minor

#### Not Implemented

(none)

### Unspecced Code

| Feature | Location | Lines | Notes |
|---------|----------|-------|-------|
| CommandAuditStreamTests merge | CommandRoutingTests.cs | +15 | 6th single-test file merged; not in original spec's list of 5 files |

## Inter-Spec Conflicts

None.

## Recommendations

1. **Update SC-002 wording**: The "50% decrease" metric is ambiguous. Only 3 helpers were truly duplicated (makeBody, makeState in 2 files each; makeResolver in 2 files but with different APIs). 2 of 3 were centralized (67%). Consider rewording to "all extractable duplicated helpers are centralized".
2. **Update US2 description**: The spec lists 5 single-test files but 6 were actually merged (CommandAuditStreamTests.cs was discovered during validation). Update the spec to reflect the actual scope.
3. **Mark spec status as Complete**: Change status from "Draft" to "Implemented".
