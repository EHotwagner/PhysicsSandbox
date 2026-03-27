# Data Model: Fix Container Build Scripts

**Feature**: 005-fix-container-build-scripts
**Date**: 2026-03-27

## Entities

No new data entities. This feature modifies script infrastructure files only:

- **AssemblyResolve handler**: Runtime event handler registered in FSI session — no persisted state
- **Python generated stubs**: Regenerated from existing proto file — no schema changes
- **Containerfile**: Environment variable removal — no data model impact
