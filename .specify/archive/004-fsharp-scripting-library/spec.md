# Feature Specification: F# Scripting Library

**Feature Branch**: `004-fsharp-scripting-library`
**Created**: 2026-03-22
**Status**: Implemented
**Input**: User description: "create a new library project for fsharp scripting. it should bundle all existing prelude and convenience functions and be designed to be extended. it should also be available to the mcp server. there should be a scratch folder to experiment with the more successful code being extracted to a scripts folder and if useful being put into the library."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use Library Functions in Scripts (Priority: P1)

A script author wants to write F# scripts (.fsx) that interact with the physics sandbox without manually setting up gRPC references, session boilerplate, or reimplementing common helper functions. They reference the scripting library and immediately have access to session management, body creation helpers, batch commands, timing utilities, and all convenience functions currently in Prelude.fsx.

**Why this priority**: This is the core value proposition — consolidating scattered helpers into a single, reusable library eliminates duplication and gives script authors a stable foundation to build on.

**Independent Test**: Can be tested by writing a minimal .fsx script that references only the new library, creates a session, adds a body, and runs the simulation — verifying all Prelude functionality works through the library.

**Acceptance Scenarios**:

1. **Given** the scripting library is built, **When** a script author references it in an .fsx file, **Then** all existing Prelude.fsx functions (resetSimulation, makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd, batchAdd, nextId, toVec3, ok, sleep, runFor, timed) are available without additional package directives for gRPC or Protobuf.
2. **Given** a script references the library, **When** the author calls session management and body creation functions, **Then** the behavior is identical to the current Prelude.fsx-based workflow.

---

### User Story 2 - Experiment in Scratch Folder (Priority: P2)

A developer wants to quickly prototype and experiment with physics sandbox scenarios. They create scripts in a dedicated scratch folder where they can iterate freely. Successful experiments are promoted to the scripts folder for others to use.

**Why this priority**: The scratch/scripts workflow enables a natural experimentation-to-production pipeline, encouraging exploration while keeping the scripts folder curated.

**Independent Test**: Can be tested by creating a scratch script, running it against the sandbox, then copying it to the scripts folder and confirming it runs correctly from there.

**Acceptance Scenarios**:

1. **Given** a scratch folder exists in the project, **When** a developer creates a new script file there, **Then** they can reference the scripting library and run experiments immediately.
2. **Given** a successful experiment in the scratch folder, **When** the developer moves it to the scripts folder, **Then** the script works without modification (same library references, same conventions).
3. **Given** the scratch folder contains experimental files, **When** the project is built or tested in CI, **Then** scratch files are excluded from build validation and test runs.

---

### User Story 3 - MCP Server Uses Library Functions (Priority: P2)

The MCP server currently implements its own adapter layer for physics commands. The scripting library should be available as a dependency so the MCP server can reuse shared helper functions, reducing duplication between script-side and MCP-side code.

**Why this priority**: Sharing code between the MCP server and scripts prevents drift and ensures consistent behavior across both interaction modes.

**Independent Test**: Can be tested by adding the library as a project reference to the MCP server, calling a shared function, and verifying it produces the expected result.

**Acceptance Scenarios**:

1. **Given** the scripting library is a project in the solution, **When** the MCP server project references it, **Then** shared helper functions are callable from MCP tool implementations.
2. **Given** a helper function exists in the library, **When** it is used by both a script and the MCP server, **Then** the behavior is identical in both contexts.

---

### User Story 4 - Extend the Library with New Functions (Priority: P3)

A developer identifies a useful pattern in their scripts (scratch or demos) and wants to extract it into the scripting library so it becomes available to all scripts and the MCP server.

**Why this priority**: Extensibility ensures the library grows organically from real usage patterns rather than speculative design.

**Independent Test**: Can be tested by adding a new function to the library, rebuilding, and verifying it is accessible from both scripts and the MCP server.

**Acceptance Scenarios**:

1. **Given** the library has a clear module structure, **When** a developer adds a new public function, **Then** it is discoverable and usable from scripts without modifying existing code.
2. **Given** a new function is added, **When** the library is rebuilt, **Then** existing scripts and the MCP server continue to work without changes.

---

### Edge Cases

- What happens when the scripting library is referenced but the sandbox server is not running? Functions that require a session should fail gracefully with clear error messages.
- What happens when a scratch script has dependencies not available in the library? The scratch folder should allow additional package directives beyond the library.
- What happens when a library function signature changes? Existing scripts referencing the old signature should get clear compilation errors at script load time.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project MUST provide a single library that bundles all current Prelude.fsx functions (resetSimulation, makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd, batchAdd, nextId, toVec3, ok, sleep, runFor, timed) as compiled, reusable modules.
- **FR-002**: The library MUST be referenceable from scripts via a single directive pointing to the compiled DLL.
- **FR-003**: The library MUST be addable as a standard project reference by other projects in the solution (including the MCP server).
- **FR-004**: The project MUST include a `scratch/` folder at the repo root for experimental scripts that is excluded from CI build validation and tests.
- **FR-005**: The project MUST include a `scripts/` folder at the repo root for curated, production-quality scripts.
- **FR-006**: The library MUST be organized into logical modules (e.g., session helpers, body creation, batch operations, utilities) to support extension.
- **FR-007**: The library MUST include signature files for all public modules, consistent with project conventions.
- **FR-008**: The library MUST re-export or bundle necessary dependencies (gRPC client, Protobuf types) so scripts don't need separate package directives for those packages.
- **FR-009**: Moving a script from scratch to scripts MUST NOT require code changes — both folders use the same library reference pattern.
- **FR-010**: The library MUST be part of the solution file and buildable with the standard build command.

### Key Entities

- **Scripting Library**: The compiled library project containing shared helpers, session management, and convenience functions for physics sandbox interaction.
- **Scratch Folder**: An experimentation area where developers prototype scripts freely.
- **Scripts Folder**: A curated collection of vetted scripts that serve as examples and reusable tools.
- **Prelude Functions**: The set of helper functions currently defined in demos/Prelude.fsx that form the initial library content.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing Prelude.fsx functions are available through the library, and a script using the library produces identical results to one using the original Prelude.fsx.
- **SC-002**: A new script can be created in the scratch folder and start interacting with the sandbox in under 5 lines of boilerplate (library reference + open statements + session creation).
- **SC-003**: The MCP server can reference and use at least one shared function from the library without duplicating code.
- **SC-004**: Adding a new public function to the library requires changes to at most 2 files per module (implementation + signature file) and no changes to existing consumers. Optionally, 2 additional files (Prelude.fsi + Prelude.fs) may be updated to re-export the function for script convenience.

## Clarifications

### Session 2026-03-22

- Q: Should existing demos be migrated to use the new library as part of this feature? → A: No — demo migration is out of scope; contradictory acceptance scenario removed.
- Q: Where should scratch/ and scripts/ folders be located? → A: Repo root, alongside existing demos/ folder.
- Q: Should scratch/ be tracked in git or gitignored? → A: Gitignored with a .gitkeep to preserve the folder structure.

## Assumptions

- The existing Prelude.fsx function set represents the initial library content; additional functions may be added over time.
- The scratch folder uses a convention-based approach (folder location) rather than tooling enforcement for the promotion workflow from scratch to scripts.
- The library targets the same runtime version as the rest of the solution.
- The scratch folder is gitignored (with a .gitkeep to preserve the empty directory), while the scripts folder is tracked in version control.
- The existing demos/ folder continues to exist alongside the new scripts/ folder; migration of demos to use the library is out of scope for this feature and will be handled separately.
