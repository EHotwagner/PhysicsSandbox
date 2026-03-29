# Feature Specification: Fix Session State and Cache Synchronization

**Feature Branch**: `005-fix-session-state-sync`
**Created**: 2026-03-29
**Status**: Completed
**Input**: User description: "Fix PClientV2 session state and cache synchronization issues reported in Mailbox/pclientv2-session-issues-2026-03-29.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reliable Simulation Reset (Priority: P1)

A scripter calls `resetSimulation` to start a fresh experiment. After the reset completes, the session reflects a clean state — no leftover bodies from previous runs, and newly created bodies appear correctly with auto-generated IDs.

**Why this priority**: This is the core workflow blocker. Without a reliable reset, every subsequent operation (body creation, queries, interactions) is built on corrupted state. This single issue causes the entire failure chain described in the problem report.

**Independent Test**: Can be tested by running a script that creates bodies, resets the simulation, then verifies that no stale bodies exist via overlap queries and that new auto-generated IDs work without collisions.

**Acceptance Scenarios**:

1. **Given** a session with 10 bodies from a previous experiment, **When** the user calls `resetSimulation`, **Then** all bodies are removed from both the server and the client-side cache, and overlap/raycast queries return no results for those bodies.
2. **Given** a session that has just been reset, **When** the user creates new bodies using auto-generated IDs (e.g., `sphere-1`), **Then** the bodies are created successfully without ID collisions.
3. **Given** a session that has just been reset, **When** the user calls `overlapSphere` with a large radius, **Then** only the ground plane (if present) is returned — no stale bodies from prior sessions.

---

### User Story 2 - Consistent Query Results (Priority: P2)

A scripter uses `overlapSphere` and `raycast` to find bodies in the scene. Both queries return consistent results that accurately reflect the current state of the physics simulation.

**Why this priority**: Query consistency is essential for debugging and interactive scripting. If different query methods disagree about what exists, users cannot trust any result.

**Independent Test**: Can be tested by creating a known set of bodies at known positions, then verifying that both overlap queries and raycasts find exactly those bodies.

**Acceptance Scenarios**:

1. **Given** a scene with 5 bodies at known positions, **When** the user runs `overlapSphere` centered on those positions, **Then** the results match the bodies that `raycast` can also detect.
2. **Given** a scene where bodies have been removed, **When** the user runs `overlapSphere`, **Then** removed bodies do not appear in the results.

---

### User Story 3 - Actionable Error Feedback from Batch Operations (Priority: P3)

A scripter submits a batch of body-creation commands. If any command fails (e.g., due to a duplicate ID), the scripter receives clear feedback about which commands succeeded and which failed, along with the reason for failure.

**Why this priority**: Silent failures make debugging extremely difficult. Even with Issues 1 and 2 fixed, users need feedback when operations fail for any reason.

**Independent Test**: Can be tested by deliberately submitting a batch with a duplicate ID and verifying that the failure is reported to the user.

**Acceptance Scenarios**:

1. **Given** a scene with an existing body named `sphere-1`, **When** the user submits a batch that includes a new body also named `sphere-1`, **Then** the user receives feedback indicating the duplicate ID failure, and the other commands in the batch still succeed.
2. **Given** a valid batch of 5 body-creation commands, **When** all commands succeed, **Then** the user receives confirmation that all 5 bodies were created.
3. **Given** a batch where 3 commands succeed and 2 fail, **When** the batch completes, **Then** the user can distinguish which commands succeeded and which failed.

---

### Edge Cases

- What happens when `resetSimulation` is called while the server is mid-step (actively simulating)?
- How does the system handle a `resetSimulation` call when the server is unreachable or slow to respond?
- What happens when two scripting sessions issue conflicting resets simultaneously?
- What happens if a body is removed by another client between an overlap query and a follow-up raycast?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The reset operation MUST remove all bodies from the server's physics world AND clear the client-side cached body collection before returning control to the user.
- **FR-002**: The reset operation MUST wait for server confirmation that all bodies have been removed before resetting the client-side ID counter.
- **FR-003**: Spatial queries (overlap) MUST query the physics server directly rather than reading from a client-side cache, ensuring results reflect the actual simulation state.
- **FR-004**: Spatial queries (raycast and overlap) MUST return consistent results for the same scene state — if a body exists, both query types MUST be able to find it within their respective query regions.
- **FR-005**: Batch operations MUST return per-command success/failure status, including the reason for any failures.
- **FR-006**: When a body-creation command fails due to a duplicate ID, the system MUST report the failure with a clear message identifying the conflicting ID.
- **FR-007**: Establishing a new session connection MUST start with empty caches and populate from the server's property stream. (Note: Already satisfied by current `Session.connect` implementation — no changes needed. See research.md R5.)

### Key Entities

- **Session**: Represents a client's connection to the physics server, including cached state and ID generation. Central to all reported issues.
- **Cached Bodies**: Client-side collection tracking known bodies. Must stay synchronized with server state after every mutation.
- **ID Generator**: Client-side counter for auto-generating body names. Must not produce IDs that collide with existing server-side bodies after a reset.
- **Batch Result**: Per-command outcome from batch operations, indicating success or failure with a reason.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a simulation reset, 100% of subsequent overlap and raycast queries return zero stale bodies from prior sessions.
- **SC-002**: After a simulation reset, auto-generated body IDs succeed on first attempt with no silent collisions.
- **SC-003**: `overlapSphere` and `raycast` agree on body existence for 100% of bodies in the scene (given appropriate query parameters that cover the same region).
- **SC-004**: Batch operation failures are reported for 100% of failed commands, with each failure including the body ID and reason.
- **SC-005**: The complete reset-and-recreate workflow (reset simulation, create bodies, verify via queries) completes reliably without requiring manual workarounds like custom ID assignment.

## Assumptions

- The physics server already supports a "clear all bodies" operation that removes bodies from the simulation; the primary issues are client-side cache synchronization and missing error propagation.
- Spatial overlap queries can be redirected to use the server's query endpoint rather than client-side cache without significant performance impact for typical scripting workloads.
- Batch operations currently receive per-command server responses; the issue is that failures are discarded rather than surfaced to the user.
