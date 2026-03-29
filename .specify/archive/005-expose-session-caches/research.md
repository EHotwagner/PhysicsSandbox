# Research: Expose Session Caches

**Feature**: 005-expose-session-caches | **Date**: 2026-03-29

## Research Tasks

### 1. Existing accessor pattern (005-expose-session-state)

**Decision**: Follow the identical pattern — add `let functionName (session: Session) = session.FieldName` accessor functions in Session.fs and corresponding `val functionName : session: Session -> ReturnType` declarations in Session.fsi.

**Rationale**: The previous feature (005-expose-session-state, commit 2eb9d05) established this exact pattern for `latestState`, `bodyRegistry`, and `lastStateUpdate`. Consistency reduces cognitive overhead.

**Alternatives considered**: Direct record field exposure (rejected — Session type is opaque in .fsi).

### 2. Do accessor functions already exist?

**Decision**: Three of the four fields (`bodyPropertiesCache`, `cachedConstraints`, `cachedRegisteredShapes`) have NO existing accessor functions. `serverAddress` also has no accessor — the `ServerAddress` field is only accessed directly within the module (in `reconnect`).

**Rationale**: Verified by reading Session.fs lines 252-278 — only `bodyRegistry`, `latestState`, `lastStateUpdate`, `client`, `meshResolver`, and `clearCaches` have accessor functions.

**Action**: Create 4 new accessor functions (not just change visibility).

### 3. Return types for each accessor

| Function | Return Type | Source |
|----------|-------------|--------|
| `bodyPropertiesCache` | `ConcurrentDictionary<string, BodyProperties>` | Session.fs line 25 |
| `cachedConstraints` | `ConstraintState list` | Session.fs line 27 |
| `cachedRegisteredShapes` | `RegisteredShapeState list` | Session.fs line 29 |
| `serverAddress` | `string` | Session.fs line 17 |

### 4. Thread safety considerations

**Decision**: Safe to expose as read-only accessors.

**Rationale**:
- `BodyPropertiesCache` is a `ConcurrentDictionary` — inherently thread-safe for reads.
- `CachedConstraints` and `CachedRegisteredShapes` are mutable F# list references — reads are atomic (single pointer read); the lists themselves are immutable.
- `ServerAddress` is an immutable string set at connection time.

### 5. Downstream impact (Scripting library)

**Decision**: No Scripting library changes required for this feature.

**Rationale**: The Scripting library does not currently reference any of these four fields. Exposing them enables future use but requires no immediate downstream changes.

## All NEEDS CLARIFICATION: Resolved

No unresolved unknowns remain.
