# Feature Specification: Fix FSI Assembly Version Mismatch

**Feature Branch**: `004-fix-fsi-assembly-mismatch`
**Created**: 2026-03-27
**Status**: Draft
**Input**: User description: "Fix Problem 3: F# Interactive assembly version mismatch — PhysicsClient NuGet package's transitive dependency on Microsoft.Extensions.Logging.Abstractions 8.x causes FileNotFoundException in FSI on .NET 10"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Demo Scripts Run Without Assembly Errors (Priority: P1)

A developer runs any F# demo script (e.g., `dotnet fsi Scripting/demos/001-basic-physics.fsx`) against a running PhysicsSandbox server. The script loads PhysicsClient via NuGet and executes without any assembly version mismatch errors.

**Why this priority**: This is the core problem — demo scripts are currently broken by FileNotFoundException for Microsoft.Extensions.Logging.Abstractions. Without this fix, no FSI-based demos work.

**Independent Test**: Run any demo script with `dotnet fsi` on .NET 10. The script should load all dependencies and connect to the server without assembly resolution failures.

**Acceptance Scenarios**:

1. **Given** a running PhysicsSandbox server and the PhysicsClient NuGet package installed, **When** a developer runs `dotnet fsi Scripting/demos/001-basic-physics.fsx`, **Then** the script loads successfully without FileNotFoundException or assembly version mismatch errors
2. **Given** a freshly-built container from the Containerfile, **When** a developer runs any demo script inside the container, **Then** all NuGet dependencies resolve to versions compatible with the .NET 10 runtime
3. **Given** a developer writes a new FSI script that references PhysicsClient via NuGet, **When** they run it with `dotnet fsi`, **Then** all transitive dependencies resolve without version conflicts

---

### User Story 2 - Prelude.fsx Works Without Manual Dependency Pinning (Priority: P2)

A developer creating a new demo script loads the shared Prelude.fsx file. The prelude handles all dependency resolution transparently — the developer does not need to manually pin transitive dependency versions to avoid assembly conflicts.

**Why this priority**: The Prelude.fsx is the standard entry point for all demo scripts. If it requires manual version pinning of internal dependencies, it becomes fragile and confusing for new contributors.

**Independent Test**: Create a minimal FSI script that only loads Prelude.fsx and calls `connect`. It should work without adding extra dependency directives for transitive dependencies.

**Acceptance Scenarios**:

1. **Given** a script containing only a Prelude.fsx load and a `connect` call, **When** run with `dotnet fsi`, **Then** it succeeds without requiring additional dependency directives for transitive dependencies of PhysicsClient
2. **Given** a future .NET SDK patch update, **When** a developer runs existing demo scripts, **Then** the dependency resolution remains stable and does not regress

---

### Edge Cases

- What happens when the developer has cached older NuGet packages locally? The fix must work even with stale NuGet caches (or the version bump naturally invalidates the cache).
- What happens if a developer pins an older version of PhysicsClient (e.g., 0.3.0) in a script? Only the new version guarantees compatibility; older versions are out of scope.
- What happens if Microsoft.Extensions.Logging.Abstractions releases a new major version (e.g., 11.x)? The fix should be resilient to minor/patch updates within the same major version band.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The PhysicsClient NuGet package MUST declare its transitive dependency on Microsoft.Extensions.Logging.Abstractions at a version compatible with the .NET 10 shared framework (10.x)
- **FR-002**: All F# demo scripts MUST load and execute without assembly version mismatch errors when run with `dotnet fsi` on the .NET 10 SDK
- **FR-003**: The shared Prelude.fsx MUST NOT require developers to manually pin versions of PhysicsClient's transitive dependencies
- **FR-004**: The container build process MUST produce a PhysicsClient package whose dependency graph resolves cleanly in FSI on .NET 10
- **FR-005**: The fix MUST NOT break compiled applications (e.g., `dotnet run`) which already work via runtime config roll-forward

### Key Entities

- **PhysicsClient NuGet Package**: The packaged library consumed by FSI scripts. Its dependency metadata determines which assembly versions FSI resolves.
- **Prelude.fsx**: The shared script preamble that loads PhysicsClient and its dependencies for all demo scripts.
- **Microsoft.Extensions.Logging.Abstractions**: The transitive dependency whose version mismatch (8.x recorded vs 10.x in runtime) causes the failure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 22 F# demo scripts execute successfully without assembly-related exceptions when run on .NET 10
- **SC-002**: A freshly-built container runs demo scripts without manual intervention or dependency workarounds
- **SC-003**: The Prelude.fsx contains zero version-pinning directives for transitive dependencies of PhysicsClient (only the PhysicsClient package itself is pinned)
- **SC-004**: Compiled applications and test suites continue to pass with no regressions

## Assumptions

- The fix will involve updating the PhysicsClient project's dependency declarations to explicitly reference Microsoft.Extensions.Logging.Abstractions at a version compatible with .NET 10 (>= 10.0.0), so NuGet resolution in FSI picks up the framework-compatible version.
- The PhysicsClient NuGet package version will be bumped to distinguish the fixed package from the broken 0.4.0 version.
- The Prelude.fsx unpinned `#r "nuget: Microsoft.Extensions.Logging.Abstractions"` line can be removed once PhysicsClient's NuGet dependency graph resolves correctly on its own.
- No API changes are needed in PhysicsClient — the Microsoft.Extensions.Logging.Abstractions 10.x API is backward-compatible with 8.x usage patterns in this project.
