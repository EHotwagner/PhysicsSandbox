# Quickstart: Fix Session State and Cache Synchronization

## What this fixes

The `resetSimulation` → create bodies → query cycle is broken because the client returns before the server finishes processing the reset. This causes:
1. ID collisions (new bodies reuse IDs of not-yet-removed old bodies)
2. Stale query results (overlap/raycast find ghost bodies)
3. Silent batch failures (duplicate ID errors are swallowed)

## Implementation order

1. **Proto contract** — Add `ConfirmedReset` RPC and messages to `physics_hub.proto`
2. **Server handler** — Implement `ConfirmedReset` in `PhysicsHubService` using query infrastructure for confirmation
3. **Client `confirmedReset`** — New function in `SimulationCommands` that calls the RPC and clears caches
4. **Client `clearCaches`** — New internal helper on `Session` to reset all mutable cache state
5. **Scripting `resetSimulation`** — Switch from fire-and-forget `reset` to `confirmedReset`
6. **Scripting `batchAdd`** — Return `BatchResult` instead of `unit`
7. **Tests** — Integration test for reset reliability, unit tests for batch results
8. **Surface area baselines** — Update for changed signatures

## Key files to modify

| File | Change |
|------|--------|
| `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` | Add `ConfirmedReset` RPC + messages |
| `src/PhysicsServer/Services/PhysicsHubService.fs` | Implement handler |
| `src/PhysicsServer/Hub/MessageRouter.fs` + `.fsi` | Add confirmed reset routing |
| `src/PhysicsClient/Connection/Session.fs` + `.fsi` | Add `clearCaches` |
| `src/PhysicsClient/Commands/SimulationCommands.fs` + `.fsi` | Add `confirmedReset` |
| `src/PhysicsSandbox.Scripting/SimulationLifecycle.fs` | Use confirmed reset |
| `src/PhysicsSandbox.Scripting/BatchOperations.fs` + `.fsi` | Return `BatchResult` |
| `src/PhysicsSandbox.Scripting/Prelude.fsi` | Update re-exports |

## Verification

```bash
# Build
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run all tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Manual verification: start server, run a demo that calls resetSimulation
./start.sh
dotnet fsi Scripting/demos/01-hello-sphere.fsx
```
