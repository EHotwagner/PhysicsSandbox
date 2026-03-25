# Feature Specification: Codebase Cleanup and Refactoring

**Feature Branch**: `004-codebase-cleanup-refactor`
**Created**: 2026-03-25
**Status**: Completed
**Input**: User description: "clean up, refactoring, simplifying. analyze the codebase and improve it."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Eliminate Duplicate Utility Code (Priority: P1)

As a developer working on the codebase, I want shared utility functions (vector conversions, mesh resolution, ID generation) to exist in exactly one location so that bug fixes and improvements only need to happen once.

**Why this priority**: Duplicated code is the highest-risk maintenance burden — a fix in one copy but not another creates subtle bugs. Three separate MeshResolver modules and four copies of vector conversion functions are the most impactful targets.

**Independent Test**: Can be verified by confirming that duplicate function definitions are removed, all call sites reference the canonical implementation, and the full test suite passes with zero regressions.

**Acceptance Scenarios**:

1. **Given** a vector conversion function exists in multiple modules, **When** the refactoring is complete, **Then** exactly one canonical implementation exists and all callers reference it.
2. **Given** three separate MeshResolver modules exist (PhysicsClient, PhysicsViewer, MCP), **When** the refactoring is complete, **Then** PhysicsClient and MCP share a single canonical implementation, and PhysicsViewer retains its own async-capable variant (justified by dependency graph constraint and async/Pending differences).
3. **Given** ID generation logic is duplicated in SimulationTools and IdGenerator, **When** the refactoring is complete, **Then** SimulationTools delegates to the canonical IdGenerator module.

---

### User Story 2 - Reduce Shape-Building Boilerplate (Priority: P2)

As a developer adding new physics shape types, I want shape construction to follow a single consistent pattern so that adding a new shape requires minimal code and follows an obvious template.

**Why this priority**: Shape-building code is the largest source of repetitive boilerplate across SimulationCommands, SimulationTools, and ClientAdapter. Consolidating it reduces code volume significantly and makes future shape additions straightforward.

**Independent Test**: Can be verified by confirming that shape-building follows a unified pattern, all existing shapes still work correctly (test suite passes), and the total line count in affected files decreases measurably.

**Acceptance Scenarios**:

1. **Given** shape-building logic is scattered across 3+ modules, **When** the refactoring is complete, **Then** a shared shape-building abstraction exists that all modules use.
2. **Given** a developer wants to add a new shape type, **When** they follow the established pattern, **Then** they only need to define the shape-specific properties in one place.
3. **Given** the refactoring is complete, **When** any existing shape operation is performed (add sphere, box, capsule, etc.), **Then** the behavior is identical to before the refactoring.

---

### User Story 3 - Simplify Large Modules (Priority: P3)

As a developer navigating the codebase, I want large files (700+ lines) to be split into focused, cohesive modules so that I can find and understand related functionality quickly.

**Why this priority**: SimulationWorld.fs (708 lines) and other large files make it harder to locate functionality and understand responsibilities. Splitting them improves discoverability without changing behavior.

**Independent Test**: Can be verified by confirming that split modules each have a clear single responsibility, all public APIs remain unchanged, and the test suite passes with zero regressions.

**Acceptance Scenarios**:

1. **Given** SimulationWorld.fs contains vector converters, shape handling, and core state management, **When** the refactoring is complete, **Then** these are separated into focused modules with clear responsibilities.
2. **Given** a large module is split, **When** existing code references functions from the original module, **Then** those references continue to work without changes (or are updated to the new module locations).

---

### User Story 4 - Consolidate Integration Test Helpers (Priority: P4)

As a developer writing integration tests, I want shared test utilities (channel creation, command builders) to exist in one helper module so that new tests can reuse proven patterns without copy-pasting.

**Why this priority**: Test duplication is lower risk than production code duplication but still wastes time and creates inconsistency. This is a quick win that improves the test development experience.

**Independent Test**: Can be verified by confirming that duplicated test helper methods are extracted to shared locations, all tests pass, and no test file defines its own copy of a shared utility.

**Acceptance Scenarios**:

1. **Given** gRPC channel creation code is duplicated in 3 integration test helper methods, **When** the refactoring is complete, **Then** a single shared channel creation method is used across all test setups.

---

### Edge Cases

- What happens when a refactored module's public API changes? All downstream callers (including MCP tools, scripting library, and demo scripts) must be updated and verified.
- How does splitting a module affect compilation order? Files must be ordered correctly in project files — new files from splitting must be placed in dependency order.
- What happens if a "duplicate" function has subtle behavioral differences across copies? Each copy must be analyzed for semantic differences before consolidation; differences must be preserved through parameterization or composition.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All vector/quaternion conversion functions (toVec3, toVector3, fromVector3, toQuaternion, fromQuaternion) MUST have exactly one canonical definition per logical conversion, with all other call sites referencing that definition.
- **FR-002**: MeshResolver functionality MUST be consolidated where the dependency graph permits. PhysicsClient and MCP MUST share a single canonical implementation. PhysicsViewer MAY retain its own variant (async + pending tracking) since it does not reference PhysicsClient.
- **FR-003**: ID generation logic MUST exist in exactly one module, with all consumers referencing it rather than maintaining local copies.
- **FR-004**: Shape-building code MUST follow a unified pattern that eliminates repetitive boilerplate across command, tool, and adapter modules.
- **FR-005**: No source file in `src/` MUST exceed 550 lines after module splitting, with each resulting module having a clear single responsibility.
- **FR-006**: Integration test helper duplication (channel creation, command builders) MUST be eliminated by extracting to shared test utility modules.
- **FR-007**: All refactoring MUST preserve existing behavior — the full test suite MUST pass with zero regressions.
- **FR-008**: Signature files MUST be updated to reflect any module reorganization, maintaining the project's documentation requirements.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero duplicate utility function definitions remain across the codebase — vector conversions, mesh resolution, and ID generation each have exactly one canonical copy.
- **SC-002**: Total line count across affected source files decreases by at least 10%, measured before and after the refactoring.
- **SC-003**: No source file in `src/` exceeds 550 lines (down from the current maximum of 708 lines).
- **SC-004**: All existing tests (unit and integration) pass with zero regressions after refactoring.
- **SC-005**: Adding a hypothetical new shape type requires changes in no more than 2 files (the shape definition and one registration point), down from the current 3+ files.

## Assumptions

- The existing test suite provides sufficient coverage to detect behavioral regressions from refactoring.
- Module splitting can be done without changing public APIs by re-exporting from the original module namespace.
- The MeshResolver implementations across projects are semantically equivalent (modulo Viewer's pending tracking), allowing consolidation without behavioral changes.
- Demo scripts (F# and Python) do not need modification since they interact through the gRPC API, not internal module structure.

## Scope Boundaries

**In scope**:
- Eliminating code duplication (utilities, shape builders, test helpers)
- Splitting oversized modules into focused files
- Consolidating MeshResolver implementations
- Updating signature files to match refactored modules

**Out of scope**:
- Adding new features or changing existing behavior
- Modifying the gRPC API or proto contracts
- Changing the project structure (no new projects, no project merges)
- Refactoring demo scripts or the 3D viewer's rendering pipeline
- Performance optimization (this is a structural cleanup only)
