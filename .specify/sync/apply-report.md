# Sync Apply Report

Applied: 2026-03-20

## Changes Made

### Specs Updated

| Spec | Requirement | Change Type | Description |
|------|-------------|-------------|-------------|
| 001-server-hub | FR-002 | Modified | Added SimulationLink service to contracts requirement |
| 001-server-hub | SC-002 | Modified | Scoped dashboard health visibility to Development mode |
| 002-physics-simulation | FR-013 | Modified | Clarified state includes dynamic bodies only; statics excluded |
| 002-physics-simulation | SC-003 | Modified | Clarified zero skipped (not zero latency); backpressure pacing |
| 002-physics-simulation | Assumptions | Modified | Replaced Euler integrator with BepuFSharp; added plane static note |

### New Specs Created

(None)

### Implementation Tasks Generated

(None — all proposals were BACKFILL direction)

### Not Applied

| Proposal | Reason |
|----------|--------|
| P3 (HTTP/2 config) | No spec change needed — infrastructure detail |

## Backup

- 001: `.specify/sync/backups/001-server-hub-spec-2026-03-20.md`
- 002: `.specify/sync/backups/002-physics-simulation-spec-2026-03-20.md`

## Next Steps

1. Review updated spec: `specs/002-physics-simulation/spec.md`
2. Drift is now fully resolved — 0 remaining drift items
