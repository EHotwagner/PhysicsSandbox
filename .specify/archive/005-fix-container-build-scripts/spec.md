# Feature Specification: Fix Container Build Scripts

**Feature Branch**: `005-fix-container-build-scripts`
**Created**: 2026-03-27
**Status**: Draft
**Input**: User description: "Fix container build issues: F# REPL assembly resolution mismatch (BLOCKER) and Python generated stub imports"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - F# Demo Scripts Run in Container (Priority: P1)

A developer builds the container image and starts the PhysicsSandbox stack. They then run any F# demo script (e.g., `dotnet fsi demos/01-hello-physics.fsx`) from inside the container. The script loads `Prelude.fsx`, which pulls the PhysicsClient NuGet package and its transitive dependency chain. The script executes without assembly resolution errors and successfully connects to the running physics server.

**Why this priority**: This is a BLOCKER. F# demo scripts are a primary user-facing capability of the sandbox. The repo's `nuget.config` declares a `local-packages` NuGet source that doesn't exist on dev workstations, causing NU1301 errors when FSI tries to resolve NuGet packages. (Note: the originally reported assembly version mismatch was already fixed by 004-fix-fsi-assembly-mismatch; .NET forward version unification handles the Grpc.Net.Client 8.0.0.0 → 10.0.0.0 case automatically.)

**Independent Test**: Can be fully tested by running any F# demo script both inside the container and on a dev workstation. Success means the script completes without NuGet source errors or assembly binding failures.

**Acceptance Scenarios**:

1. **Given** a freshly built container with the PhysicsSandbox stack running, **When** a user runs `dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx`, **Then** the script loads without NuGet resolution errors and connects to the physics server.
2. **Given** a developer on a workstation without a `local-packages/` directory, **When** they run any F# demo script, **Then** NuGet resolves PhysicsClient from the global feed without NU1301 errors.
3. **Given** a developer running F# demo scripts outside the container on a standard .NET 10 workstation, **When** they run any demo script, **Then** scripts continue to work as before (no regression).

---

### User Story 2 - Python Demo Scripts Run in Container (Priority: P2)

A developer builds the container image and runs any Python demo script from inside the container. The Python scripts import from the `generated/` stub directory. The imports resolve correctly without requiring the `PYTHONPATH` environment variable workaround currently set in the Containerfile.

**Why this priority**: Python demos are a secondary scripting interface. The current PYTHONPATH workaround in the Containerfile makes them function, but the proper fix is at the project level so scripts work identically inside and outside containers.

**Independent Test**: Can be tested by running any Python demo script from the project root (e.g., `python3 Scripting/demos_py/01_hello_physics.py`) both inside and outside the container, without setting PYTHONPATH.

**Acceptance Scenarios**:

1. **Given** regenerated Python gRPC stubs, **When** the `physics_hub_pb2_grpc.py` file imports `physics_hub_pb2`, **Then** it uses a relative import (`from . import physics_hub_pb2`) that works regardless of the working directory or PYTHONPATH.
2. **Given** a freshly built container without the PYTHONPATH workaround, **When** a user runs a Python demo script, **Then** the script imports succeed and it connects to the physics server.
3. **Given** a developer running Python demo scripts from the project root outside the container, **When** they run any demo script, **Then** the imports resolve correctly as before (no regression).

---

### Edge Cases

- What happens when a user has cached NuGet packages from a previous PhysicsClient version? NuGet must still resolve from the correct source.
- What happens if `grpcio-tools` is upgraded and the generated stub format changes? The import fix must be resilient to minor formatting changes in generated code.
- What happens if the container's global NuGet config already has a `local-packages` source registered? Removing the repo-level source must not break the container build.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: F# demo scripts MUST load and execute in the container without NuGet source resolution failures (NU1301).
- **FR-002**: The NuGet configuration MUST work both inside the container and on dev workstations without manual setup of local package directories.
- **FR-003**: Python generated gRPC stubs MUST use relative imports so that scripts work from any working directory without PYTHONPATH manipulation.
- **FR-004**: The stub generation script (`generate_stubs.sh`) MUST produce correctly-importing stubs when re-run, so the fix is reproducible and not a one-time manual edit.
- **FR-005**: The PYTHONPATH workaround in the Containerfile MUST be removed once the proper fix is in place.
- **FR-006**: All existing F# and Python demo scripts MUST continue to work outside the container (no regressions).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 22 F# demo scripts execute without assembly resolution errors when run inside the container.
- **SC-002**: All 22 Python demo scripts execute without import errors when run inside the container, without PYTHONPATH being set.
- **SC-003**: Running `generate_stubs.sh` produces stubs with correct relative imports — verifiable by checking the generated file contains `from . import` instead of bare `import`.
- **SC-004**: Zero regressions in demo script execution outside the container on a standard .NET 10 development environment.
