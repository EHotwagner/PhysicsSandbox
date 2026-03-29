# Feature Specification: Expose Session Caches

**Feature Branch**: `005-expose-session-caches`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Expose bodyPropertiesCache, cachedConstraints, and cachedRegisteredShapes from the PhysicsClient Session module as public accessors. Same pattern as 005-expose-session-state — promote internal accessors to public via .fsi signature file changes and update surface area baseline tests. Also expose serverAddress for diagnostics."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inspect Body Properties (Priority: P1)

A developer using the PhysicsClient library wants to look up semi-static body properties (mass, shape details, motion type) for any body in the simulation without parsing the full simulation state stream.

**Why this priority**: Body properties are the most commonly needed metadata after state — scripts and tools need to know mass, shape, and configuration to make decisions about forces, constraints, and rendering.

**Independent Test**: Can be tested by connecting a session, creating bodies with known properties, and calling `bodyPropertiesCache` from code outside the PhysicsClient assembly to verify the properties are accessible and match what was created.

**Acceptance Scenarios**:

1. **Given** a connected session with bodies created, **When** a consumer calls `bodyPropertiesCache`, **Then** they receive a dictionary mapping body IDs to their properties (mass, shape, motion type).
2. **Given** a connected session with no bodies, **When** a consumer calls `bodyPropertiesCache`, **Then** they receive an empty dictionary.

---

### User Story 2 - View Active Constraints (Priority: P1)

A developer wants to see which constraints (joints, springs, hinges) currently exist between bodies to understand the simulation's structural relationships.

**Why this priority**: Constraints define how bodies interact mechanically — essential for debugging physics setups and verifying that joints were created correctly.

**Independent Test**: Can be tested by connecting a session, creating constraints between bodies, and calling `cachedConstraints` from outside the assembly to verify the constraint list is accessible.

**Acceptance Scenarios**:

1. **Given** a connected session with constraints created, **When** a consumer calls `cachedConstraints`, **Then** they receive a list of all active constraint states.
2. **Given** a connected session with no constraints, **When** a consumer calls `cachedConstraints`, **Then** they receive an empty list.

---

### User Story 3 - View Registered Shapes (Priority: P1)

A developer wants to see which custom shapes (meshes, compounds, convex hulls) have been registered in the simulation for reuse.

**Why this priority**: Registered shapes are prerequisites for creating bodies that reference them — knowing what's registered helps avoid duplicate registrations and diagnose shape-related issues.

**Independent Test**: Can be tested by connecting a session, registering custom shapes, and calling `cachedRegisteredShapes` from outside the assembly to verify the shape list is accessible.

**Acceptance Scenarios**:

1. **Given** a connected session with registered shapes, **When** a consumer calls `cachedRegisteredShapes`, **Then** they receive a list of all registered shape states.
2. **Given** a connected session with no registered shapes, **When** a consumer calls `cachedRegisteredShapes`, **Then** they receive an empty list.

---

### User Story 4 - Check Server Address (Priority: P2)

A developer wants to check which server address the session is connected to for diagnostics, logging, or multi-server tooling.

**Why this priority**: Useful for diagnostics and multi-session scenarios but secondary to accessing simulation metadata.

**Independent Test**: Can be tested by connecting a session to a known address and calling `serverAddress` from outside the assembly to verify it returns the expected address string.

**Acceptance Scenarios**:

1. **Given** a connected session, **When** a consumer calls `serverAddress`, **Then** they receive the server URL string that was used to establish the connection.

---

### Edge Cases

- What happens when the session is disconnected — do the cache accessors still return the last known values?
- What happens if bodies are removed between reading `bodyPropertiesCache` and using the result — stale entries may exist momentarily (inherent to concurrent dictionaries).
- What happens if constraints/shapes are updated while iterating `cachedConstraints`/`cachedRegisteredShapes` — list snapshots are atomic (F# immutable list assignment).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `bodyPropertiesCache` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-002**: The `cachedConstraints` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-003**: The `cachedRegisteredShapes` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-004**: The `serverAddress` function MUST be publicly accessible to consumers outside the PhysicsClient assembly.
- **FR-005**: New accessor functions MUST be created for fields that do not currently have accessor functions in Session.fs.
- **FR-006**: The return types MUST match the existing internal field types exactly.
- **FR-007**: Existing internal consumers within the PhysicsClient assembly MUST continue to work without modification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four functions (`bodyPropertiesCache`, `cachedConstraints`, `cachedRegisteredShapes`, `serverAddress`) can be called from code outside the PhysicsClient assembly without compilation errors.
- **SC-002**: Existing tests continue to pass with no modifications required.
- **SC-003**: Downstream consumers (e.g., Scripting library) can access all four functions directly.
- **SC-004**: Surface area baseline test for the Session module reflects the expanded public API.
