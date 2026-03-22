# Feature Specification: Scripting Library NuGet Package

**Feature Branch**: `004-scripting-nuget-package`
**Created**: 2026-03-22
**Status**: Implemented
**Input**: User description: "publish the scripting library as a nupkg in the local nuget feed. change all references to that."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pack and Publish Libraries (Priority: P1)

A developer packs all referenced libraries (PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, PhysicsClient, and PhysicsSandbox.Scripting) into NuGet packages and publishes them to the local NuGet feed in dependency order. This follows the same pattern already established by BepuFSharp in this project and eliminates all hardcoded build-output DLL paths from scripts.

**Why this priority**: Nothing else can happen until all packages exist in the local feed. Dependencies must be published in order (Contracts and ServiceDefaults first, then PhysicsClient, then Scripting).

**Independent Test**: Run the pack commands, verify all .nupkg files appear in the local feed directory, and confirm a consuming project can restore and build against the Scripting package (which transitively pulls in all dependencies).

**Acceptance Scenarios**:

1. **Given** all library projects are buildable, **When** they are packed and published in dependency order, **Then** all packages appear in the local NuGet feed.
2. **Given** all dependencies are in the local feed, **When** PhysicsSandbox.Scripting is packed with PackageReferences to its dependencies, **Then** the Scripting package appears in the local feed with correct dependency declarations.
3. **Given** all packages are in the local feed, **When** a project adds a PackageReference to PhysicsSandbox.Scripting, **Then** NuGet restore succeeds and transitively resolves all dependencies.

---

### User Story 2 - Migrate In-Solution Project References (Priority: P2)

Projects within the solution that currently use a direct ProjectReference to PhysicsSandbox.Scripting (MCP server, test project) switch to consuming the published NuGet package via PackageReference. The solution continues to build and all existing functionality works identically.

**Why this priority**: These are the primary consumers within the solution. Migrating them validates that the package is complete and functional.

**Independent Test**: After switching each project to PackageReference, build the full solution and run all tests to verify no regressions.

**Acceptance Scenarios**:

1. **Given** PhysicsSandbox.Mcp uses a PackageReference to the scripting library, **When** the solution is built, **Then** the MCP server compiles and all tools function correctly.
2. **Given** PhysicsSandbox.Scripting.Tests uses a PackageReference, **When** tests are run, **Then** all 19 existing tests pass.
3. **Given** all project references have been migrated, **When** `dotnet build` is run on the full solution, **Then** the build succeeds with no errors.

---

### User Story 3 - Update All Script and Demo References (Priority: P3)

All F# scripts (`Scripting/scripts/`), F# demos (`Scripting/demos/`), and Python demos (`Scripting/demos_py/`) update their DLL reference paths and server port references. F# files switch from build-output DLL paths to the NuGet package location. All files use canonical ports (5180 HTTP / 7180 HTTPS) instead of `localhost:5000`.

**Why this priority**: Scripts and demos are secondary consumers, but they are the primary source of the `localhost:5000` references and stale DLL paths. Fixing them in one pass alongside the NuGet migration avoids a separate cleanup feature.

**Independent Test**: Run an existing F# script and a Python demo after updating references and verify they load, connect, and execute without errors.

**Acceptance Scenarios**:

1. **Given** F# script `#r` directives use version-agnostic NuGet references, **When** a script is executed, **Then** it automatically resolves the newest package version and runs identically to before.
2. **Given** a script includes `#r "nuget: PhysicsSandbox.Scripting"`, **When** the script is executed, **Then** all scripting library modules and transitive dependencies are available without additional references.
3. **Given** F# demo prelude files have been updated, **When** a demo is executed, **Then** it connects to the correct server port and runs successfully.
4. **Given** Python demo `prelude.py` has been updated, **When** a Python demo is executed, **Then** it connects to the correct server port.
5. **Given** a newer version of the scripting library is published to the local feed, **When** a script runs without changes, **Then** it picks up the new version automatically.

---

### User Story 4 - Fix Port Consistency Across Scripts and Docs (Priority: P4)

All scripts and documentation use consistent, correct server port references. Currently, demo scripts hardcode `http://localhost:5000`, scripting scripts use `http://localhost:5180`, and the MCP server defaults to `https://localhost:7180`. The canonical ports are 5180 (HTTP) and 7180 (HTTPS), matching the PhysicsServer launchSettings.json.

**Why this priority**: Port inconsistency causes connection failures and developer confusion. Since this feature already touches script files for DLL reference updates, fixing ports in the same pass is efficient.

**Independent Test**: After updating, grep for port references and verify no scripts or docs reference `localhost:5000`. Run a script to confirm it connects to the correct server.

**Acceptance Scenarios**:

1. **Given** all scripts have been updated, **When** searching for `localhost:5000` across the codebase, **Then** zero matches are found in script or doc files.
2. **Given** a script uses the corrected port, **When** the server is running with the default profile, **Then** the script connects successfully.

---

### Edge Cases

- What happens if the local NuGet feed directory does not exist on a fresh developer machine? (NuGet restore should fail with a clear feed-not-found error, consistent with BepuFSharp behavior.)
- What happens if a consumer references a package version that hasn't been published yet? (Standard NuGet version-not-found error.)
- What happens if transitive dependencies (PhysicsClient, gRPC, Protobuf, Contracts) are not properly declared in the package? (Consumer build fails with missing assembly errors.)
- What happens if a developer packs without incrementing the version number? (Stale cached packages are served instead of the updated code, causing hard-to-diagnose issues.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: PhysicsClient, PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, and PhysicsSandbox.Scripting MUST all be packable into .nupkg files with defined package identities and versions.
- **FR-002**: The package MUST be publishable to the existing local NuGet feed.
- **FR-003**: All in-solution ProjectReferences to PhysicsSandbox.Scripting MUST be replaced with PackageReferences.
- **FR-004**: All F# script and demo `#r` directives referencing the scripting library or PhysicsClient DLLs MUST be replaced with version-agnostic NuGet references (e.g., `#r "nuget: PhysicsSandbox.Scripting"` without a version specifier) so they automatically resolve the newest package from the local feed.
- **FR-005**: PhysicsSandbox.Scripting MUST declare PhysicsClient as a NuGet package dependency (not a ProjectReference) so that transitive dependency resolution works for consumers.
- **FR-006**: The packaging workflow MUST follow the same conventions as BepuFSharp (local feed path, version scheme, pack flags).
- **FR-007**: Each new package publish MUST use an incremented version number to prevent stale cached packages from being served to consumers.
- **FR-008**: All server port references across scripts and documentation MUST use the canonical ports: 5180 for HTTP, 7180 for HTTPS. References to `localhost:5000` MUST be corrected.

### Key Entities

- **NuGet Package (PhysicsSandbox.Shared.Contracts)**: The packaged gRPC/Protobuf contracts library. Dependency of PhysicsClient.
- **NuGet Package (PhysicsSandbox.ServiceDefaults)**: The packaged shared health/telemetry defaults library. Referenced directly by demo scripts.
- **NuGet Package (PhysicsClient)**: The packaged client library. Depends on Shared.Contracts. Dependency of PhysicsSandbox.Scripting.
- **NuGet Package (PhysicsSandbox.Scripting)**: The packaged scripting library containing all 6 modules. Depends on PhysicsClient (and transitively on Contracts and ServiceDefaults).
- **Local NuGet Feed**: The shared local package repository already configured in the global NuGet.Config as `local-feed`.

## Clarifications

### Session 2026-03-22

- Q: Should fixing inconsistent port references (5000 vs 5180 vs 7180) be included in this feature's scope? → A: Yes, include port fixes — update all scripts and docs to use consistent canonical ports (5180 HTTP, 7180 HTTPS).
- Q: Should package version numbers be required to increment on each publish? → A: Yes, each new publish must increment the version to prevent stale cached packages.
- Q: Should PhysicsClient also be packed as a separate NuGet package? → A: Yes, pack PhysicsClient as its own package; Scripting declares it as a dependency.
- Q: Should demo scripts (F# and Python) be updated alongside Scripting/scripts/? → A: Yes, update all — demos (F# + Python) and scripts — fix both ports and DLL references everywhere.
- Q: Should scripts hardcode package version numbers? → A: No, scripts MUST use version-agnostic NuGet references (no version specifier) to automatically resolve the newest available package. Out-of-date references must be fixed.
- Q: Should Shared.Contracts and ServiceDefaults also be packaged as NuGet packages? → A: Yes, pack all referenced projects to eliminate all hardcoded build-output DLL paths from scripts.

## Assumptions

- The existing local NuGet feed at `~/.local/share/nuget-local/` and its global NuGet.Config entry are in place.
- Package version starts at `0.1.0`, matching the version already declared in the project file.
- The BepuFSharp local NuGet pattern is the established convention and will be followed.
- PhysicsClient and its transitive dependencies will be declared as package dependencies.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The full solution builds successfully with zero ProjectReferences to PhysicsSandbox.Scripting or PhysicsClient (from external consumers) remaining.
- **SC-002**: All existing tests (scripting tests and integration tests) pass after the migration.
- **SC-003**: F# scripts load and execute correctly using the packaged library references.
- **SC-004**: The pack-and-publish workflow follows the established local NuGet pattern (dependency-ordered `dotnet pack` with `-p:NoWarn=NU5104 -o ~/.local/share/nuget-local/`).
- **SC-005**: Zero references to `localhost:5000` remain in script or documentation files after the migration.
- **SC-006**: Each subsequent package publish uses a higher version number than the previous one in the local feed.
