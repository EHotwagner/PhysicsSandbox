# Feature Specification: Upgrade BepuFSharp

**Feature Branch**: `004-upgrade-bepufsharp`
**Created**: 2026-03-24
**Status**: Draft
**Input**: User description: "Upgrade BepuFSharp to newer version and run the tests"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upgrade BepuFSharp package version (Priority: P1)

A developer upgrades the BepuFSharp dependency from 0.2.0-beta.1 to 0.3.0 in the PhysicsSimulation project. The solution builds successfully and all existing tests pass without modification, confirming backward compatibility.

**Why this priority**: This is the entire feature — bumping the version and validating that nothing breaks. The upgrade removes the unused Stride.BepuPhysics transitive dependency from BepuFSharp, simplifying the dependency graph, and moves off a pre-release version to a stable release.

**Independent Test**: Can be fully tested by changing the version number, building the solution, and running the full test suite. Delivers a cleaner dependency tree and a stable version.

**Acceptance Scenarios**:

1. **Given** PhysicsSimulation references BepuFSharp 0.2.0-beta.1, **When** the version is changed to 0.3.0 and the solution is built, **Then** the build succeeds with no errors or new warnings.
2. **Given** the solution builds with BepuFSharp 0.3.0, **When** the full test suite is run, **Then** all existing tests pass (unit tests across 7 projects + integration tests).
3. **Given** BepuFSharp 0.3.0 removes the StrideInterop module, **When** the codebase is searched for StrideInterop usage, **Then** no production or test code references it.

---

### User Story 2 - Update documentation to reflect new version (Priority: P2)

After the upgrade is validated, documentation references to BepuFSharp version numbers are updated to reflect 0.3.0.

**Why this priority**: Keeps docs accurate. Lower priority because it has no functional impact.

**Independent Test**: Can be verified by searching docs for "0.2.0-beta.1" and confirming all active references are updated to "0.3.0".

**Acceptance Scenarios**:

1. **Given** active documentation files reference BepuFSharp 0.2.0-beta.1, **When** the upgrade is complete, **Then** CLAUDE.md and project memory files reflect the new version 0.3.0.

---

### Edge Cases

- What happens if any test relies on StrideInterop types transitively through BepuFSharp? The codebase has been verified to have zero StrideInterop usage, so this is not expected.
- What happens if BepuPhysics2 transitive dependency version changes? Both BepuFSharp versions pin BepuPhysics 2.5.0-beta.28 — no change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The PhysicsSimulation project MUST reference BepuFSharp 0.3.0 instead of 0.2.0-beta.1
- **FR-002**: The solution MUST build successfully with the upgraded package
- **FR-003**: All existing unit tests (7 test projects) MUST pass without modification
- **FR-004**: All integration tests MUST pass without modification
- **FR-005**: Active documentation referencing the BepuFSharp version MUST be updated to 0.3.0

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Solution builds with zero errors after version bump
- **SC-002**: 100% of existing tests pass (unit + integration) with no test modifications
- **SC-003**: No production code changes required beyond the version number in the project file
- **SC-004**: All active documentation version references updated from 0.2.0-beta.1 to 0.3.0
