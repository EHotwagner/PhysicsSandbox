# Data Model: Fix FSI Assembly Version Mismatch

**Date**: 2026-03-27
**Feature**: 004-fix-fsi-assembly-mismatch

## Entities

This feature involves no new data entities. It modifies NuGet package dependency metadata and script references.

### NuGet Dependency Graph (Modified)

The only data change is in the PhysicsClient NuGet package's dependency declaration:

- **Before**: Microsoft.Extensions.Logging.Abstractions >= 8.0.0 (transitive, from ServiceDefaults)
- **After**: Microsoft.Extensions.Logging.Abstractions >= 10.0.0 (explicit, in PhysicsClient.fsproj)

### Version References (Modified)

| Artifact | Before | After |
|----------|--------|-------|
| PhysicsClient NuGet version | 0.4.0 | 0.5.0 |
| Prelude.fsx PhysicsClient pin | 0.4.0 | 0.5.0 |
| Prelude.fsx Logging.Abstractions | unpinned (present) | removed |
