# Research: Expose Session State

**Feature**: 005-expose-session-state | **Date**: 2026-03-29

## Research Tasks

### 1. Current Visibility of Target Functions

**Decision**: The three functions (`latestState`, `bodyRegistry`, `lastStateUpdate`) are currently marked `internal` in `Session.fsi`. The `internal` keyword in F# `.fsi` files restricts visibility to the declaring assembly. Removing the keyword makes them publicly accessible.

**Rationale**: The `.fsi` file is the single source of truth for visibility in F# modules (Constitution Principle V). The implementation in `Session.fs` requires no changes — the `internal` keyword only appears in the signature file.

**Alternatives considered**: None — this is the canonical F# mechanism for controlling visibility.

### 2. Thread Safety of Exposed State

**Decision**: All three accessors are already thread-safe:
- `bodyRegistry` returns a `ConcurrentDictionary<string, string>` — thread-safe by design.
- `latestState` returns a `SimulationState option` from a `mutable` field — single-word reference assignments are atomic in .NET.
- `lastStateUpdate` returns a `DateTime` (64-bit struct) from a `mutable` field — reads are atomic on 64-bit platforms.

**Rationale**: No synchronization primitives need to be added. The existing internal consumers already rely on these thread-safety properties.

**Alternatives considered**: Wrapping in explicit locks — rejected as unnecessary overhead given .NET's memory model guarantees for these types.

### 3. Surface Area Baseline Impact

**Decision**: The `SurfaceAreaTests.fs` baseline for `PhysicsClient.Session` currently lists 4 entries: `connect`, `disconnect`, `reconnect`, `isConnected`. After this change, it must list 7 entries (adding `bodyRegistry`, `latestState`, `lastStateUpdate`).

**Rationale**: Constitution Principle V requires surface area baselines to be updated when the public API changes. The test uses reflection to verify the module's public members match the expected list.

**Alternatives considered**: None — baseline update is mandatory per constitution.

### 4. Downstream Consumer Impact (Scripting Library)

**Decision**: The Scripting library currently does not access these internal functions. After exposure, it *can* use them but is not required to. No changes to Scripting are needed for this feature.

**Rationale**: The spec's SC-003 states "Downstream consumers (e.g., Scripting library) can access all three functions directly" — this is a capability enablement, not a usage requirement.

### 5. Return Type Considerations

**Decision**: Return types remain unchanged (FR-004):
- `bodyRegistry`: `ConcurrentDictionary<string, string>` — returns the live mutable dictionary (not a copy). Consumers can observe real-time changes but should treat it as read-only.
- `latestState`: `SimulationState option` — returns `None` before first state update, `Some state` after.
- `lastStateUpdate`: `DateTime` — returns `DateTime.MinValue` (default) before first update.

**Rationale**: Changing return types would break internal consumers (FR-005) and add unnecessary complexity. The live reference pattern is consistent with how these are already used internally.

**Alternatives considered**: Returning immutable snapshots — rejected per FR-004 (signatures must remain identical) and because the overhead would degrade real-time consumers.
