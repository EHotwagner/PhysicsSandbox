# Data Model: Client REPL Library

**Feature**: 004-client-repl | **Date**: 2026-03-20

## Entities

### Session

The central entity representing an active connection to the physics server.

| Field | Type | Description |
|-------|------|-------------|
| Channel | GrpcChannel | Reusable gRPC channel to the server |
| Client | PhysicsHubClient | Typed gRPC client for PhysicsHub service |
| ServerAddress | string | Server address used for connection |
| CancellationSource | CancellationTokenSource | Controls background state stream |
| BodyRegistry | ConcurrentDictionary<string, ShapeKind> | Tracks body IDs created by this session (for clear-all) |
| IdCounters | ConcurrentDictionary<string, int ref> | Per-shape-type counters for ID generation |
| LatestState | SimulationState option (mutable) | Cached latest state from background stream |
| IsConnected | bool (mutable) | Connection status flag |

**Lifecycle**: Created by `connect` → used by all command/query functions → terminated by `disconnect`.

### BodyPreset

A pre-configured body template.

| Field | Type | Description |
|-------|------|-------------|
| Name | string | Human-readable preset name (e.g., "bowlingBall") |
| Shape | Shape | Proto Shape (Sphere/Box) |
| Mass | float | Default mass in kg |
| DefaultPosition | Vec3 option | Default spawn position (None = origin) |
| DefaultVelocity | Vec3 option | Default velocity (None = stationary) |

**Instances** (minimum 5 presets):

| Preset | Shape | Radius/HalfExtents | Mass |
|--------|-------|-------------------|------|
| marble | Sphere | r=0.01 | 0.005 |
| bowlingBall | Sphere | r=0.11 | 6.35 |
| beachBall | Sphere | r=0.2 | 0.1 |
| crate | Box | 0.5 × 0.5 × 0.5 | 20.0 |
| brick | Box | 0.2 × 0.1 × 0.05 | 3.0 |
| boulder | Sphere | r=0.5 | 200.0 |
| die | Box | 0.05 × 0.05 × 0.05 | 0.03 |

### ShapeKind

Discriminated union for ID generation context.

| Case | Description |
|------|-------------|
| Sphere | Sphere body |
| Box | Box body |
| Plane | Static plane |

### Direction

Named directions for steering functions.

| Case | Vec3 Mapping |
|------|-------------|
| Up | (0, 1, 0) |
| Down | (0, -1, 0) |
| North | (0, 0, -1) |
| South | (0, 0, 1) |
| East | (1, 0, 0) |
| West | (-1, 0, 0) |

## Relationships

```
Session ──1:N──▶ BodyRegistry entries (tracked body IDs)
Session ──1:1──▶ LatestState (cached SimulationState from stream)
BodyPreset ──creates──▶ AddBody command (via Session)
Direction ──maps to──▶ Vec3 (for force/impulse vectors)
```

## State Transitions

### Session State

```
Disconnected ──connect()──▶ Connected
Connected ──disconnect()──▶ Disconnected
Connected ──server drops──▶ Disconnected (detected on next command)
Disconnected ──reconnect()──▶ Connected
```

### Body Registry

```
Empty ──addBody/preset/random──▶ Has Entries
Has Entries ──removeBody──▶ Entry Removed
Has Entries ──clearAll──▶ Empty
```
