# Feature Specification: Expose Session State

**Feature Branch**: `005-expose-session-state`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Make latestState, bodyRegistry, and lastStateUpdate publicly exposed in PhysicsClient Session module"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read Current Simulation State (Priority: P1)

A developer using the PhysicsClient library (via the Scripting library or custom code) wants to read the current simulation state to inspect body positions, velocities, and other properties without relying on internal module access.

**Why this priority**: Accessing live simulation state is the most fundamental capability for building any interactive tool, script, or integration on top of PhysicsClient.

**Independent Test**: Can be tested by connecting a session, waiting for state to arrive, and calling `latestState` from code outside the PhysicsClient assembly to retrieve a populated simulation state.

**Acceptance Scenarios**:

1. **Given** a connected session with an active simulation, **When** a consumer calls `latestState`, **Then** they receive the current simulation state containing body data.
2. **Given** a connected session before any state has arrived, **When** a consumer calls `latestState`, **Then** they receive an indication that no state is available yet.

---

### User Story 2 - Look Up Body Names (Priority: P1)

A developer wants to look up the mapping between body IDs and their human-readable names to correlate simulation state entries with the bodies they created.

**Why this priority**: Body names are essential for identifying which body is which when inspecting state — without this, numeric IDs are opaque.

**Independent Test**: Can be tested by connecting a session, creating named bodies, and calling `bodyRegistry` from outside the assembly to verify the name-to-ID mappings are accessible.

**Acceptance Scenarios**:

1. **Given** a connected session with bodies created using names, **When** a consumer calls `bodyRegistry`, **Then** they receive the complete mapping of body names to IDs.
2. **Given** a connected session with no bodies created, **When** a consumer calls `bodyRegistry`, **Then** they receive an empty mapping.

---

### User Story 3 - Check State Freshness (Priority: P2)

A developer wants to check when the last state update was received to determine if the state stream is active and data is fresh.

**Why this priority**: Useful for diagnostics and deciding whether to trust cached state, but secondary to actually reading the state itself.

**Independent Test**: Can be tested by connecting a session, observing state updates, and calling `lastStateUpdate` from outside the assembly to verify the timestamp is recent.

**Acceptance Scenarios**:

1. **Given** a connected session receiving state updates, **When** a consumer calls `lastStateUpdate`, **Then** they receive a timestamp close to the current time.
2. **Given** a connected session that has not yet received state, **When** a consumer calls `lastStateUpdate`, **Then** they receive a default timestamp indicating no updates have arrived.

---

### Edge Cases

- What happens when the session is disconnected — do the accessors still return the last known values?
- What happens if multiple threads read state concurrently — the underlying data structures must remain thread-safe (they already are, since `ConcurrentDictionary` and atomic reference updates are used internally).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `latestState` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-002**: The `bodyRegistry` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-003**: The `lastStateUpdate` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-004**: The public signatures and return types of all three functions MUST remain identical to their current internal signatures.
- **FR-005**: Existing internal consumers within the PhysicsClient assembly MUST continue to work without modification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All three functions (`latestState`, `bodyRegistry`, `lastStateUpdate`) can be called from code outside the PhysicsClient assembly without compilation errors.
- **SC-002**: Existing tests continue to pass with no modifications required.
- **SC-003**: Downstream consumers (e.g., Scripting library) can access all three functions directly.
