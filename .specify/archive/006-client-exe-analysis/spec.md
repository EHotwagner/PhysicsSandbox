# Feature Specification: PhysicsClient Exe vs Library Analysis

**Feature Branch**: `006-client-exe-analysis`
**Created**: 2026-03-27
**Status**: Draft
**Input**: User description: "Analyze the usefulness of PhysicsClient being a service and executable"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Script Author Uses PhysicsClient as Library (Priority: P1)

A developer writing F# demo scripts or the Scripting library references PhysicsClient as a NuGet package or project reference to access its gRPC session management, simulation commands, view commands, presets, generators, steering, and display modules. The developer never launches PhysicsClient as a standalone process — they consume it purely as a library dependency.

**Why this priority**: This is the dominant usage pattern today. PhysicsClient's entire public API (Session, SimulationCommands, ViewCommands, Presets, Generators, Steering, ShapeBuilders, Vec3Helpers, IdGenerator, StateDisplay, LiveWatch, MeshResolver) is consumed by PhysicsSandbox.Scripting, unit tests, and F# scripts via NuGet. The library role delivers the core value.

**Independent Test**: Can be tested by building the Scripting project and running demo scripts with PhysicsClient as a library reference (no PhysicsClient process running) and verifying all simulation commands work correctly.

**Acceptance Scenarios**:

1. **Given** PhysicsClient is a library-only project, **When** the Scripting library references it, **Then** all 12+ public modules remain accessible and functional.
2. **Given** PhysicsClient is packaged as NuGet, **When** an F# script adds `#r "nuget: PhysicsClient, 0.5.0"`, **Then** the script can create sessions, send commands, and display state without any separate PhysicsClient process.
3. **Given** PhysicsClient has no entry point, **When** `dotnet build` is run, **Then** it produces a DLL (not an EXE) suitable for referencing.

---

### User Story 2 - Aspire Orchestrator Manages PhysicsClient Service (Priority: P2)

The Aspire AppHost currently launches PhysicsClient as one of five managed projects. It injects `services__server__http__0` environment variables and keeps the process alive alongside the server, simulation, viewer, and MCP services. An operator starting the system via `dotnet run --project AppHost` sees PhysicsClient listed as a running resource in the Aspire dashboard.

**Why this priority**: Understanding the value (or lack thereof) of running PhysicsClient as a long-lived Aspire service is central to this analysis. The current entry point creates a host, logs the server address, and runs indefinitely — it performs no active work beyond staying alive.

**Independent Test**: Can be tested by removing PhysicsClient from AppHost orchestration and verifying that all other services (server, simulation, viewer, MCP) and all demo scripts continue to function correctly.

**Acceptance Scenarios**:

1. **Given** PhysicsClient is removed from AppHost, **When** the system is started via `dotnet run --project AppHost`, **Then** server, simulation, viewer, and MCP all start and operate normally.
2. **Given** PhysicsClient is removed from AppHost, **When** F# demo scripts are run, **Then** they connect directly to the server and function without issue.
3. **Given** PhysicsClient remains in AppHost, **When** the dashboard is inspected, **Then** the PhysicsClient service shows as running but performs no visible activity beyond maintaining a connection.

---

### User Story 3 - Developer Evaluates Build Artifacts and Packaging (Priority: P3)

A developer packaging PhysicsClient for NuGet distribution needs clarity on whether the project should produce an executable or a library. Currently it produces an executable that is also packable as NuGet, meaning `dotnet pack` produces a NuGet package from an executable project. The developer wants to understand if this hybrid configuration causes issues or confusion.

**Why this priority**: Packaging concerns affect downstream consumers (scripts, Scripting library) and the correctness of NuGet distribution.

**Independent Test**: Can be tested by changing the output type to library, running `dotnet pack`, and verifying the resulting NuGet package works identically in F# scripts and the Scripting project.

**Acceptance Scenarios**:

1. **Given** PhysicsClient output type is library, **When** `dotnet pack` is run, **Then** a valid NuGet package is produced without warnings about executable packaging.
2. **Given** the NuGet package from a library-type project, **When** consumed by F# scripts, **Then** all public modules are accessible and functional.

---

### Edge Cases

- What happens if any external tool or script depends on the PhysicsClient executable (not just the library)?
- How does removing the executable affect integration tests that may expect PhysicsClient as a running Aspire resource?
- Does the Aspire AppHost require all referenced projects to be executable, or can it reference library projects without launching them?
- Are there any telemetry, health check, or service discovery features that only work when PhysicsClient runs as an Aspire-managed service?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Analysis MUST catalog every consumer of PhysicsClient (projects, scripts, NuGet references) and classify each as "library consumer" or "service consumer".
- **FR-002**: Analysis MUST identify all functionality in PhysicsClient's entry point and determine whether any of it provides value beyond what library consumers already get.
- **FR-003**: Analysis MUST verify whether removing PhysicsClient from Aspire AppHost orchestration causes any downstream failures in server, simulation, viewer, MCP, or demo scripts.
- **FR-004**: Analysis MUST determine whether changing from executable to library output affects NuGet packaging, script consumption, or project references.
- **FR-005**: Analysis MUST produce a clear recommendation (keep as executable, convert to library, or hybrid) with supporting evidence.
- **FR-006**: If the recommendation is to convert to library, the analysis MUST identify all required changes (AppHost removal, entry point removal, project file changes, downstream updates).

### Key Entities

- **PhysicsClient Project**: The F# project that currently serves as both an executable service and a library. Key attributes: executable output type, packable as NuGet, 12+ public modules, entry point.
- **Aspire AppHost**: The orchestrator that launches PhysicsClient as a managed service alongside 4 other projects.
- **PhysicsClient Consumers**: Projects and scripts that depend on PhysicsClient — currently Scripting (project ref), Tests (project ref), F# demo scripts (NuGet ref), integration tests (indirect).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Analysis identifies 100% of PhysicsClient consumers across the codebase (project references, NuGet references, script imports).
- **SC-002**: Analysis provides a definitive answer on whether PhysicsClient's entry point performs any work that cannot be achieved through library consumption alone.
- **SC-003**: Analysis produces a recommendation supported by at least 3 concrete evidence points from the codebase.
- **SC-004**: If recommending conversion to library, the change list covers all files requiring modification with zero missed dependencies.

## Assumptions

- The analysis is scoped to the current PhysicsSandbox codebase as it exists today; future planned features are not considered unless already specified.
- "Service" refers to PhysicsClient running as a long-lived Aspire-managed process, not as a gRPC service (PhysicsClient is a gRPC *client*, not a server).
- The current entry point behavior (create host, log address, run indefinitely) represents the complete service-mode functionality — there is no hidden background work.
- NuGet packaging with executable output type and packable flag is a supported but atypical .NET configuration.
