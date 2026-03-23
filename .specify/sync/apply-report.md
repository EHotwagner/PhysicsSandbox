# Sync Apply Report

Applied: 2026-03-23
Feature: 004-backlog-fix-test-progress

## Changes Made

### Specs Updated

| Spec | Requirement | Change Type | Detail |
|------|-------------|-------------|--------|
| 004-backlog-fix-test-progress | FR-004a | Modified | Split "7 body registry → Result.Error" into "6 single-body → Result.Error + 1 bulk cleanup → Trace.TraceWarning" |
| 004-backlog-fix-test-progress | SC-003 | Modified | Updated counts to reflect 6 Error + 1 Warning + 3 cache warnings |
| 004-backlog-fix-test-progress | US2-3 (acceptance) | Modified | Updated to reflect clearAll uses TraceWarning pattern |

### Code Updated

| File | Change | Detail |
|------|--------|--------|
| src/PhysicsClient/Commands/SimulationCommands.fs:308 | Added `Trace.TraceWarning` | clearAll now emits structured warning when registry TryRemove fails during cleanup |

### Backups

- `specs/004-backlog-fix-test-progress/spec.md` → `.specify/sync/backups/004-backlog-fix-test-progress/spec.md.bak`

## Next Steps

1. Verify build: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
2. Run tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
3. Commit changes when ready
