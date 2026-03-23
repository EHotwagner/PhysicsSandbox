# Quickstart: Backlog Fixes and Test Progress Reporting

**Branch**: `004-backlog-fix-test-progress`

## What This Feature Does

1. **Test progress script** — A shell script that runs the test suite with per-project progress reporting, showing completed/total projects, elapsed time, and ETA.
2. **Silent failure fixes** — PhysicsClient registry operations that silently ignore failures now return `Result.Error` with descriptive messages.
3. **Query expiration** — MessageRouter pending queries auto-expire after 30 seconds, preventing memory leaks in long sessions.
4. **Constraint builders** — Scripting library gains 6 new constraint type builders (completing all 10 types).
5. **Test helper consolidation** — Duplicated test utilities extracted into shared locations.

## Quick Verification

```bash
# Run the new test progress script
./test-progress.sh

# Run all tests (verify nothing is broken)
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Verify constraint builders (check Scripting surface area tests)
dotnet test tests/PhysicsSandbox.Scripting.Tests/ -p:StrideCompilerSkipBuild=true

# Verify PhysicsClient error handling (check client tests)
dotnet test tests/PhysicsClient.Tests/ -p:StrideCompilerSkipBuild=true

# Verify query expiration (check server tests)
dotnet test tests/PhysicsServer.Tests/ -p:StrideCompilerSkipBuild=true
```

## Key Files Changed

| Area | Files |
|------|-------|
| Test progress | `test-progress.sh` (new) |
| Silent failures | `src/PhysicsClient/Commands/SimulationCommands.fs` |
| Query expiration | `src/PhysicsServer/Hub/MessageRouter.fs`, `MessageRouter.fsi` |
| Constraint builders | `src/PhysicsSandbox.Scripting/ConstraintBuilders.fs`, `.fsi` |
| Test helpers (F#) | New shared test helper + 6 SurfaceAreaTests files |
| Test helpers (C#) | New `IntegrationTestHelpers.cs` + 14 integration test files |
