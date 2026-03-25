# Quickstart: Test Suite Cleanup

## Build & Test

```bash
# Build (headless)
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run all tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run single test project
dotnet test tests/PhysicsSimulation.Tests -p:StrideCompilerSkipBuild=true
```

## Key Files

| File | Purpose |
|------|---------|
| `tests/SharedTestHelpers.fs` | Shared reflection utilities (extend with `assertModuleSurface`) |
| `tests/CommonTestBuilders.fs` | NEW: shared test data builders (`makeBody`, `makeState`, `makeResolver`) |
| `tests/*/SurfaceAreaTests.fs` | Surface area baseline tests (simplify with shared helper) |

## Implementation Order

1. **Create shared infrastructure** (SharedTestHelpers extension + CommonTestBuilders.fs)
2. **Wire up .fsproj files** (Link CommonTestBuilders before test files)
3. **Update test files** to use shared builders (replace local helpers)
4. **Merge integration tests** (move test methods, delete source files)
5. **Split oversized files** (create new files, move tests, update .fsproj order)
6. **Run full suite** to verify zero regressions

## Verification Checklist

- [ ] `dotnet test` passes with zero failures
- [ ] No test file exceeds 25 tests
- [ ] No single-test integration files remain
- [ ] Test count within 5% of baseline (~358 unit + ~80 integration)
