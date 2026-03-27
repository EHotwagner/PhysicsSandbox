# Data Model: PhysicsClient Exe vs Library Analysis

**Date**: 2026-03-27
**Feature**: 006-client-exe-analysis

## Entities

This feature is an analysis/refactor — no new data entities are introduced. The key entities are existing project configurations that change:

### PhysicsClient Project Configuration

| Attribute | Current | Proposed |
|-----------|---------|----------|
| OutputType | Exe | Library |
| IsPackable | true | true (unchanged) |
| Version | 0.5.0 | 0.6.0 |
| ServiceDefaults ref | Yes | Removed |
| Program.fs | Present (22 lines, no-op) | Deleted |
| Module count | 12 + Program.fs | 12 |

### AppHost Resource Registry

| Resource | Current | Proposed |
|----------|---------|----------|
| server | Present | Unchanged |
| simulation | Present | Unchanged |
| viewer | Present | Unchanged |
| client | Present (PhysicsClient) | Removed |
| mcp | Present | Unchanged |

### Dependency Graph Changes

**Before** (PhysicsClient as Exe):
```
AppHost → PhysicsClient (project ref, launches as service)
Scripting → PhysicsClient (project ref, library usage)
Tests → PhysicsClient (project ref, library usage)
F# Scripts → PhysicsClient (NuGet, library usage)
PhysicsClient → ServiceDefaults (only used by Program.fs)
PhysicsClient → Contracts (used by library modules)
```

**After** (PhysicsClient as Library):
```
Scripting → PhysicsClient (project ref, library usage) — unchanged
Tests → PhysicsClient (project ref, library usage) — unchanged
F# Scripts → PhysicsClient (NuGet, library usage) — unchanged
PhysicsClient → Contracts (used by library modules) — unchanged
```

Removed edges:
- `AppHost → PhysicsClient` (project ref deleted)
- `PhysicsClient → ServiceDefaults` (reference removed)
