# Data Model: Expose Session State

**Feature**: 005-expose-session-state | **Date**: 2026-03-29

## Entities

No new entities are introduced. This feature exposes read access to existing internal state.

### Session (existing, unchanged)

The `Session` record type in `PhysicsClient.Connection.Session` already contains the relevant fields. No structural changes needed.

| Field | Type | Visibility Change |
|-------|------|-------------------|
| `BodyRegistry` | `ConcurrentDictionary<string, string>` | Accessor `bodyRegistry` promoted from `internal` to `public` |
| `LatestState` | `SimulationState option` | Accessor `latestState` promoted from `internal` to `public` |
| `LastStateUpdate` | `DateTime` | Accessor `lastStateUpdate` promoted from `internal` to `public` |

### Key Types (existing, unchanged)

- **`SimulationState`** (from `PhysicsSandbox.Shared.Contracts`): Protobuf-generated message containing body states, timestamps, and simulation metadata.
- **`ConcurrentDictionary<string, string>`**: Maps body name (e.g., `"sphere-1"`) to shape kind (e.g., `"sphere"`).
- **`DateTime`**: UTC timestamp of last state stream update.

## Relationships

```
Session --has--> BodyRegistry (ConcurrentDictionary<string, string>)
Session --has--> LatestState (SimulationState option)
Session --has--> LastStateUpdate (DateTime)
```

## Validation Rules

No new validation. Existing invariants apply:
- `latestState` returns `None` until the background state stream delivers the first update.
- `lastStateUpdate` returns `DateTime.MinValue` until the first update.
- `bodyRegistry` is always non-null (initialized as empty on connect).

## State Transitions

No state transitions changed. The background state stream continues to update these fields as before.
