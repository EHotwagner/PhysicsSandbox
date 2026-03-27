# Research: Fix Container Build Scripts

**Feature**: 005-fix-container-build-scripts
**Date**: 2026-03-27 (updated after reproduction testing)

## Problem 1: F# FSI Demo Scripts Broken — REVISED

### Original Diagnosis (INCORRECT)

The original report stated that `Grpc.Net.Client 2.76.0` compiled against `Microsoft.Extensions.Logging.Abstractions` assembly version `8.0.0.0` would fail in FSI because FSI doesn't perform assembly version unification.

### Actual Finding

**The assembly mismatch is not the problem.** Reproduction testing confirmed:

1. `.NET runtime forward version unification works in FSI` — when Grpc.Net.Client requests 8.0.0.0 and 10.0.0.0 is loaded, the runtime satisfies the request automatically. Tested: `#r "nuget: PhysicsClient, 0.5.0"` + `GrpcChannel.ForAddress()` succeeds without errors.

2. `Grpc.Net.Client 2.76.0 already targets net10.0` — added via [PR #2653](https://github.com/grpc/grpc-dotnet/pull/2653). It deliberately pins `Microsoft.Extensions.Logging.Abstractions` to `8.0.0` for LTS compatibility, but forward unification handles this.

3. `The 004-fix-fsi-assembly-mismatch already resolved the dependency graph` — PhysicsClient 0.5.0 nupkg declares `Microsoft.Extensions.Logging.Abstractions >= 10.0.5`, ensuring FSI pulls the correct version.

### Actual Root Cause

The real failure is **NU1301: NuGet source not found**. The repo's `nuget.config` declares:
```xml
<add key="local" value="local-packages" />
```

This relative path resolves to `{repo-root}/local-packages/`. In the container, the Containerfile packs packages to `/src/local-packages` and registers that path in the **global** NuGet config — so FSI finds them via the global config. But on a dev workstation where `local-packages/` doesn't exist as a directory, FSI fails with NU1301.

### Decision: No F# script changes needed

The 004-fix already resolved the assembly issue. The NU1301 error is a NuGet configuration issue, not an FSI runtime problem. The fix is to make the `nuget.config` `local` source conditional or ensure the directory exists.

### Alternatives Considered

1. **AssemblyResolve handler in Prelude.fsx** — Not needed. Forward unification already works. Adding unnecessary handlers masks real problems.

2. **Make `local-packages` source conditional** (chosen) — NuGet config doesn't support conditions natively, but we can either:
   - Create an empty `local-packages/` directory (simplest, `.gitkeep`)
   - Or remove the `local` source from repo `nuget.config` and rely only on the global config in the container

3. **Remove `local` source from nuget.config entirely** — Would break the `dotnet build` step in the Containerfile if it relies on the repo-level config. But the Containerfile already registers in the global config, so it may be fine.

### Files to Modify

- `nuget.config` — Either remove the `local` source or add a `.gitkeep` to `local-packages/`
- No changes to `Prelude.fsx` or `PhysicsClient.fsx`

---

## Problem 2: Python Generated Stub Imports

### Decision: Regenerate stubs (sed fix already works)

### Rationale

The `generate_stubs.sh` script already has a working sed command:
```bash
sed -i 's/^import physics_hub_pb2 as/from . import physics_hub_pb2 as/' "$OUT_DIR/physics_hub_pb2_grpc.py"
```

The currently committed `physics_hub_pb2_grpc.py` has the bare `import physics_hub_pb2` because the stubs were committed before the sed fix was added (or regenerated without running the script). Reproduction confirmed: running `generate_stubs.sh` produces correct relative imports, and Python imports then work without PYTHONPATH.

### Alternatives Considered

1. **`sys.path` manipulation in `__init__.py`** — Fragile, pollutes Python path.

2. **Sed post-processing** (chosen, already in place) — Simple, reliable, widely used. Just needs the committed stubs to be regenerated.

3. **Custom protoc plugin** — Overkill for a single proto file.

### Implementation Approach

1. Run `generate_stubs.sh` to regenerate stubs (applies the sed fix)
2. Commit the regenerated stubs with correct relative imports
3. Remove the `PYTHONPATH` workaround from the Containerfile

### Files to Modify

- `Scripting/demos_py/generated/physics_hub_pb2_grpc.py` — Regenerated (already done)
- `Scripting/demos_py/generated/physics_hub_pb2.py` — Regenerated (already done)
- `Containerfile` — Remove PYTHONPATH workaround line
