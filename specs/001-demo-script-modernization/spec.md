# Feature Specification: Demo Script Modernization

**Feature Branch**: `001-demo-script-modernization`
**Created**: 2026-03-21
**Status**: Completed
**Input**: User description: "Bring demo scripts and scripting capabilities up to date with batching and restart simulation"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Batch Command Support in Demo Scripts (Priority: P1)

A developer running demo scripts wants scenes to set up faster by sending multiple commands in a single batch rather than issuing them one at a time. Demos that place many bodies (e.g., domino rows, billiard triangles, marble rain, chaos scene) should group their setup commands into batches so the scene builds in one round-trip instead of many sequential calls.

**Why this priority**: Batching is the core capability gap. The batch command infrastructure already exists in the client library and server but demos never use it. Adopting it demonstrates the feature, improves demo performance, and validates the batch API end-to-end.

**Independent Test**: Run any updated demo that uses batching (e.g., Domino Row or Billiards) and verify the scene sets up correctly with all bodies placed in the expected positions, and that execution completes faster than the sequential equivalent.

**Acceptance Scenarios**:

1. **Given** a demo script that places multiple bodies, **When** the demo runs, **Then** the body creation commands are sent as a single batch and all bodies appear correctly in the simulation.
2. **Given** a batch of body-creation commands, **When** one command in the batch is invalid, **Then** the valid commands still succeed and the demo reports the failure gracefully.
3. **Given** a demo using batched setup, **When** compared to the same demo using sequential commands, **Then** the batched version completes setup measurably faster.

---

### User Story 2 - Proper Simulation Reset Between Demos (Priority: P1)

A developer running the demo suite wants each demo to start from a clean simulation state by using the server's built-in reset capability instead of manually pausing, clearing bodies, resetting the ID counter, re-adding a ground plane, and restoring gravity. The reset command should handle all of this atomically on the server side, providing a reliable and consistent starting state.

**Why this priority**: The current `resetScene` helper manually reconstructs initial state with 5 separate commands plus a sleep. Using the server-side reset is more reliable, eliminates timing issues, and ensures demos start from a truly clean state (including resetting simulation time to zero).

**Independent Test**: Run the full demo suite end-to-end and verify that each demo starts with a clean simulation (no leftover bodies from previous demos, gravity at default, simulation time reset).

**Acceptance Scenarios**:

1. **Given** a completed demo with bodies in the scene, **When** the next demo's reset runs, **Then** all bodies are removed, gravity returns to default, and simulation time resets to zero.
2. **Given** the demo suite running all 10 demos in sequence, **When** the suite completes, **Then** no demo fails due to leftover state from a previous demo.
3. **Given** the reset command, **When** it executes, **Then** the ground plane is re-established as part of the post-reset setup without requiring multiple manual commands.

---

### User Story 3 - Updated Scripting Helpers in Shared Prelude (Priority: P2)

A developer writing new demo scripts or modifying existing ones wants the shared helper module (Prelude) to expose convenience functions for batching and reset so they don't have to import and wire up these capabilities manually. The helpers should be simple wrappers that make common patterns easy.

**Why this priority**: The Prelude module is the foundation all demo scripts build on. Updating it once benefits all demos and future script authors. However, this is secondary to actually adopting the capabilities in the demos themselves.

**Independent Test**: Create a minimal new demo script that uses the updated Prelude helpers for batching and reset, and verify it works correctly without any additional boilerplate.

**Acceptance Scenarios**:

1. **Given** the updated Prelude module, **When** a demo script loads it, **Then** batch and reset helper functions are available without additional imports.
2. **Given** a list of body-creation commands, **When** passed to the batch helper, **Then** they execute as a single batch operation and return the created body IDs.
3. **Given** the reset helper, **When** called between demos, **Then** it uses the server-side reset and re-establishes the ground plane in a single operation.

---

### User Story 4 - AutoRun Script Uses Modern Capabilities (Priority: P2)

A developer or CI system running the self-contained AutoRun script wants it to use the same modern batching and reset capabilities as the individual demo scripts. The AutoRun script should be updated in lockstep with the individual demos so both execution modes produce consistent results.

**Why this priority**: AutoRun is the non-interactive runner used for automated validation. It must stay consistent with the individual demo scripts to serve as a reliable automated test of demo functionality.

**Independent Test**: Run `dotnet fsi demos/AutoRun.fsx` and verify all 10 demos pass with the updated batching and reset logic.

**Acceptance Scenarios**:

1. **Given** the AutoRun script, **When** executed against a running server, **Then** all 10 demos complete successfully using batched commands and server-side reset.
2. **Given** the AutoRun script results, **When** compared to RunAll results, **Then** both report the same pass/fail outcomes for each demo.

---

### Edge Cases

- What happens when the server's reset command fails (e.g., server is unresponsive)? The script should fall back to the manual reset sequence or report a clear error.
- What happens when a batch exceeds the 100-command limit? The helpers should split large batches automatically or reject with a clear message.
- What happens when the server doesn't support the reset command (older server version)? The script should degrade gracefully to the manual approach.
- What happens when a demo is run standalone (not via RunAll/AutoRun) — does it still reset properly at the start?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Demo scripts that create multiple bodies MUST use batch commands to group body-creation into fewer round-trips where practical.
- **FR-002**: The shared Prelude module MUST provide a `resetSimulation` helper that uses the server-side reset command followed by ground plane re-establishment.
- **FR-003**: The shared Prelude module MUST provide a batch helper function that accepts a list of commands and submits them as a single batch operation.
- **FR-004**: All 10 existing demo scripts MUST be updated to use the new reset helper instead of the manual multi-step reset.
- **FR-005**: Demo scripts placing 3 or more bodies in setup MUST use batch commands for those placements.
- **FR-006**: The AutoRun script MUST be updated to use the same batching and reset patterns as the individual demo scripts.
- **FR-007**: The AllDemos module MUST be updated consistently with the individual demo script changes.
- **FR-008**: Batch helpers MUST handle the 100-command limit by splitting oversized batches automatically.
- **FR-009**: All demo scripts MUST continue to work correctly when run standalone (individual script execution) or as part of the suite (RunAll/AutoRun).
- **FR-010**: Error handling for batch and reset operations MUST produce clear, actionable error messages visible in the demo output.

### Key Entities

- **Demo Script**: An individual `.fsx` file that demonstrates a physics scenario, using Prelude helpers and client library functions.
- **Prelude Module**: The shared helper module (`Prelude.fsx`) providing convenience functions (reset, batch, runFor, sleep) used by all demo scripts.
- **Batch Command**: A group of simulation or view commands submitted together for atomic execution, limited to 100 commands per batch.
- **Simulation Reset**: A server-side operation that clears all bodies, resets forces, and returns simulation time to zero.

## Assumptions

- The server-side reset command (`reset` in PhysicsClient) is fully functional and clears all simulation state including time.
- After a reset, a ground plane must be explicitly re-added (the reset does not create one automatically).
- The existing batch command API (`batchCommands` in SimulationCommands) is stable and handles the command types needed by demos (primarily body creation).
- The 100-command batch limit is sufficient for all current demo scenarios (the largest demo, Chaos, uses roughly 30-40 body creations).
- The `IdGenerator.reset()` call in the current `resetScene` is still needed after server-side reset to keep client-side ID generation in sync.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 demos run successfully in both RunAll (interactive) and AutoRun (non-interactive) modes with zero failures caused by the modernization changes.
- **SC-002**: Demo scripts that batch body creation complete their setup phase at least 30% faster than the sequential equivalent (measurable by timing the setup portion).
- **SC-003**: Every demo starts from a verified clean state — zero leftover bodies from previous demos when running the full suite.
- **SC-004**: No demo script requires more than one import line beyond the Prelude to access batching and reset capabilities.
- **SC-005**: The total line count change across all demo scripts is net-neutral or a net reduction (modernization should simplify, not add complexity).
