# Feature Specification: Test Suite Cleanup

**Feature Branch**: `004-test-suite-cleanup`
**Created**: 2026-03-25
**Status**: Draft
**Input**: User description: "analyze our tests and tests suite if there are redundancies and if they could be better structured. they were added ad hoc. stepping back and taking a long look might be useful. then implement the improvements."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Eliminate Duplicate Tests (Priority: P1)

A developer maintaining the test suite discovers that the same behavior is verified in multiple places across different test projects (e.g., MeshResolver tested identically in PhysicsClient.Tests and PhysicsViewer.Tests, ID generation tested 3 times, mesh caching tested in 4 projects). They consolidate duplicates so each behavior is tested once at the appropriate layer, reducing maintenance burden and confusion about which test is authoritative.

**Why this priority**: Redundant tests waste CI time, create confusion about test ownership, and multiply the effort needed when changing shared behavior. Eliminating duplicates is the highest-value structural improvement.

**Independent Test**: Can be verified by running the full test suite before and after, confirming equal or better coverage with fewer total tests. No test should fail that previously passed, and no behavior should lose coverage.

**Acceptance Scenarios**:

1. **Given** the MeshResolver is tested in both PhysicsClient.Tests and PhysicsViewer.Tests with partial overlap, **When** coverage is harmonized, **Then** both files retain their project-specific tests and share the common helper via CommonTestBuilders
2. **Given** ID generation appears in IdGeneratorTests, SimulationCommandsTests, and PresetsTests, **When** reviewed for duplication, **Then** each file is confirmed to test its own distinct concern (no consolidation needed — already well-separated)
3. **Given** mesh caching is tested across multiple projects at different layers, **When** shared helpers are extracted, **Then** the duplicated `makeResolver` helper is centralized in CommonTestBuilders while each project retains its layer-appropriate tests

---

### User Story 2 - Consolidate Small Integration Test Files (Priority: P2)

A developer notices that 5 integration test files contain only 1 test each (DiagnosticsIntegrationTests, ComparisonIntegrationTests, StaticBodyTests, StressTestIntegrationTests, RestartIntegrationTests). They merge these into logically related, feature-focused files to reduce file proliferation and improve discoverability.

**Why this priority**: Single-test files add navigation overhead and obscure the overall test structure. Consolidating them into cohesive groups makes the suite easier to understand and extend.

**Independent Test**: Can be verified by confirming that all previously-passing integration tests still pass after consolidation, and that the total number of integration test files is reduced.

**Acceptance Scenarios**:

1. **Given** 5 integration test files have 1 test each, **When** they are consolidated into related feature files, **Then** the number of integration test files decreases while all tests still pass
2. **Given** StaticBodyTests.cs has 1 test, **When** it is merged into a related simulation integration test file, **Then** the test is discoverable under a logical grouping

---

### User Story 3 - Extract Shared Test Data Builders (Priority: P2)

A developer observes that test helper functions like `makeBody`, `makeSphereCmd`, and `makeConvexHullBody` are redefined independently across test files. They extract these into shared test utility modules so that test setup is consistent and maintained in one place.

**Why this priority**: Duplicated test helpers drift apart over time, leading to inconsistent test setup and increased maintenance. Shared builders ensure consistency.

**Independent Test**: Can be verified by confirming shared builders are used across multiple test files, and that all tests still pass with the shared helpers.

**Acceptance Scenarios**:

1. **Given** test helper functions are duplicated across files, **When** shared test data builders are extracted, **Then** at least 3 test files reference the shared module instead of local helpers
2. **Given** Surface Area tests repeat the same boilerplate pattern in 6 files, **When** a shared utility is created, **Then** each Surface Area test file delegates to the shared utility with minimal per-file boilerplate

---

### User Story 4 - Rebalance Oversized Test Files (Priority: P3)

A developer finds that some test files contain 30-40+ tests (SceneManagerTests, CameraControllerTests, ExtendedFeatureTests, SimulationWorldTests) while others have only 1-3 tests. They split oversized files into focused, smaller files organized by behavior or sub-feature for better readability and maintainability.

**Why this priority**: Large test files are harder to navigate, slower to understand, and more likely to cause merge conflicts. Splitting by behavior improves clarity.

**Independent Test**: Can be verified by confirming oversized files are split into smaller focused files, all tests still pass, and no test is lost.

**Acceptance Scenarios**:

1. **Given** SceneManagerTests.fs has 40+ tests covering shapes, transforms, and rendering, **When** it is split by concern, **Then** each resulting file has a focused name and under 25 tests
2. **Given** ExtendedFeatureTests.fs has 36 tests covering shapes, constraints, and kinematic bodies, **When** it is split, **Then** each concern has its own test file

---

### Edge Cases

- What happens when a consolidated test file grows too large after merging? Keep merged files under ~25 tests; split further if needed.
- How are test helpers shared between F# and C# test projects? F# shared helpers remain in F# (SharedTestHelpers.fs); C# integration test helpers remain in C# (IntegrationTestHelpers.cs). No cross-language sharing is required.
- What if removing a "duplicate" test removes coverage for a subtle edge case? Each removal must be validated by confirming the remaining test covers the same or broader assertion.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The test suite MUST maintain or improve code coverage after cleanup — no previously-tested behavior loses its test
- **FR-002**: Duplicate tests (same assertions on same behavior in multiple locations) MUST be consolidated to a single authoritative location
- **FR-003**: Integration test files with only 1 test MUST be consolidated into logically related files
- **FR-004**: Repeated test helper functions (duplicated across test projects) MUST be extracted into shared utility modules
- **FR-005**: Surface Area test boilerplate MUST be unified into a shared pattern that reduces per-file duplication
- **FR-006**: Test files exceeding 30 tests SHOULD be split into focused sub-files organized by behavior
- **FR-007**: All tests MUST continue to pass after restructuring — the cleanup is behavior-preserving

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Total test count remains within 5% of the original count (currently ~463 tests), with any reduction coming solely from eliminating true duplicates
- **SC-002**: Number of test files containing duplicated helper functions decreases by at least 50%
- **SC-003**: No test file contains more than 25 tests after restructuring
- **SC-004**: Single-test integration files are reduced to zero
- **SC-005**: Full test suite passes with zero regressions after cleanup

## Assumptions

- The existing test suite has approximately 463 tests across 7 test projects
- Surface area tests follow a consistent pattern that can be unified
- Test naming conventions may vary but standardizing names is out of scope (focus is on structural cleanup)
- The cleanup is purely structural — no new test coverage is being added, and no behavioral changes are made to the code under test
- Integration tests will continue to use Aspire hosting infrastructure as-is
