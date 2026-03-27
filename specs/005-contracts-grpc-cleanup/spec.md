# Feature Specification: Contracts gRPC Package Cleanup

**Feature Branch**: `005-contracts-grpc-cleanup`
**Created**: 2026-03-27
**Status**: Draft
**Input**: User description: "Replace Grpc.AspNetCore with Grpc.Net.Client + Grpc.Tools in Shared.Contracts (cleaner — Contracts doesn't need the ASP.NET server package)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Leaner Contracts Package (Priority: P1)

As a developer building against the PhysicsSandbox solution, I want the Shared.Contracts project to depend only on the gRPC packages it actually needs (proto compilation and client stubs), so that consuming projects don't transitively pull in the full ASP.NET Core server stack unless they explicitly need it.

**Why this priority**: This is the entire scope of the feature. Removing the unnecessary server dependency reduces the transitive dependency footprint for every project that references Contracts, making builds cleaner and package semantics more accurate.

**Independent Test**: Build the entire solution successfully after the package swap, run all existing tests, and verify that no project loses access to types it needs.

**Acceptance Scenarios**:

1. **Given** the Shared.Contracts project references `Grpc.AspNetCore`, **When** the dependency is replaced with `Grpc.Net.Client` + `Grpc.Tools`, **Then** the project builds successfully and produces the same generated C# types from the proto file.
2. **Given** all downstream projects reference Shared.Contracts, **When** the solution is built after the change, **Then** every project compiles without errors.
3. **Given** the full test suite exists, **When** all unit and integration tests are run after the change, **Then** all tests pass with no regressions.

---

### User Story 2 - NuGet Package Remains Publishable (Priority: P2)

As a maintainer who publishes the Contracts package to a local NuGet feed, I want the updated package to remain packable and consumable by downstream scripts and tools.

**Why this priority**: The Contracts package is distributed as a NuGet package (currently version 0.5.0) and consumed by demo scripts. It must remain packable after the dependency change.

**Independent Test**: Run `dotnet pack` on Shared.Contracts and verify the resulting .nupkg contains the expected dependencies (Grpc.Net.Client, Google.Protobuf) but not Grpc.AspNetCore.

**Acceptance Scenarios**:

1. **Given** the updated Contracts project, **When** `dotnet pack` is run, **Then** a valid .nupkg is produced.
2. **Given** the produced .nupkg, **When** its dependency list is inspected, **Then** it lists `Grpc.Net.Client` and `Google.Protobuf` but not `Grpc.AspNetCore`.

---

### Edge Cases

- What happens if any project was relying on transitive ASP.NET server types pulled in via Grpc.AspNetCore through Contracts? Those projects must add their own explicit dependency.
- What happens if the proto compilation behavior changes due to the package swap? Generated code must remain identical since Grpc.Tools (which drives proto compilation) is already a direct dependency and unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Shared.Contracts project MUST replace the `Grpc.AspNetCore` package reference with `Grpc.Net.Client`.
- **FR-002**: The Shared.Contracts project MUST retain `Grpc.Tools` (already present) and `Google.Protobuf` (already present) as direct dependencies.
- **FR-003**: The proto file (`physics_hub.proto`) MUST continue to generate both client and server stubs (`GrpcServices="Both"`).
- **FR-004**: All projects in the solution MUST build successfully after the change.
- **FR-005**: All existing unit tests and integration tests MUST pass without modification.
- **FR-006**: If any downstream project loses a transitive dependency it needs, that project MUST add an explicit package reference to restore it.

### Key Entities

- **PhysicsSandbox.Shared.Contracts**: The C# project that holds proto definitions and generates gRPC types. Currently version 0.5.0, packable as a NuGet package.
- **Grpc.AspNetCore**: The current (overly broad) dependency — bundles server, client, and tooling. Being removed.
- **Grpc.Net.Client**: The replacement dependency — provides only gRPC client types needed for generated stubs.
- **Grpc.Tools**: Already a direct dependency — drives proto compilation. Unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The Shared.Contracts project has zero references to `Grpc.AspNetCore` after the change.
- **SC-002**: The full solution builds with zero errors and zero new warnings.
- **SC-003**: 100% of existing tests (unit + integration) pass after the change.
- **SC-004**: The Shared.Contracts NuGet package can be packed successfully.

## Assumptions

- The `GrpcServices="Both"` setting will continue to work because `Grpc.Tools` (which handles proto compilation) is already a direct dependency and is not being changed.
- No project in the solution relies on ASP.NET Core server types transitively through the Contracts project. If any do, they will surface as build errors and need explicit references added.
- The package version pin (`2.*`) will be preserved for the replacement package to maintain consistency with the existing versioning strategy.
