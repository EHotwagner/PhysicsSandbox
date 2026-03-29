# Data Model: Expose Session Caches

**Feature**: 005-expose-session-caches | **Date**: 2026-03-29

## Entities

No new entities are introduced. This feature exposes existing internal fields as public accessors.

### Session (existing, modified visibility)

| Field | Type | Visibility | Change |
|-------|------|------------|--------|
| `BodyPropertiesCache` | `ConcurrentDictionary<string, BodyProperties>` | internal → **public accessor** | New `bodyPropertiesCache` function |
| `CachedConstraints` | `ConstraintState list` | internal → **public accessor** | New `cachedConstraints` function |
| `CachedRegisteredShapes` | `RegisteredShapeState list` | internal → **public accessor** | New `cachedRegisteredShapes` function |
| `ServerAddress` | `string` | internal → **public accessor** | New `serverAddress` function |

### Referenced Types (from Contracts, unchanged)

- `BodyProperties` — semi-static body metadata (mass, shape, color, motion type, collision config, material)
- `ConstraintState` — constraint definition (type, connected body IDs, parameters)
- `RegisteredShapeState` — registered custom shape definition (mesh ID, shape data)

## Validation Rules

- No validation needed — these are read-only accessors to existing internal state.
- Return types match internal field types exactly (FR-006).

## State Transitions

None — read-only accessors do not modify state.
