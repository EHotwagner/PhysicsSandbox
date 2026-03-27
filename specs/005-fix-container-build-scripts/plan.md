# Implementation Plan: Fix Container Build Scripts

**Branch**: `005-fix-container-build-scripts` | **Date**: 2026-03-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-fix-container-build-scripts/spec.md`

**REVISED** after reproduction testing — see [research.md](research.md) for details.

## Summary

Fix two container build issues: (1) The repo's `nuget.config` declares a `local-packages` NuGet source that doesn't exist on dev workstations, causing NU1301 errors when FSI tries to resolve packages — the originally reported "assembly mismatch" was already fixed by 004-fix-fsi-assembly-mismatch and .NET's forward version unification handles the rest. (2) Regenerate Python gRPC stubs with correct relative imports (the `generate_stubs.sh` sed fix works but was never applied to the committed files) and remove the PYTHONPATH Containerfile workaround.

## Technical Context

**Language/Version**: F# scripts on .NET 10.0, Python 3.10+, Bash
**Primary Dependencies**: PhysicsClient 0.5.0 (NuGet), grpcio-tools (Python)
**Storage**: N/A
**Testing**: Manual verification (run demo scripts in container and outside)
**Target Platform**: Linux container + Linux/macOS development workstations
**Project Type**: Bug fix (scripting infrastructure)
**Performance Goals**: N/A
**Constraints**: Fixes must work both inside container and on dev workstations
**Scale/Scope**: 3 files modified, 2 files regenerated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | N/A | No service changes |
| II. Contract-First | N/A | No contract changes |
| III. Shared Nothing | N/A | No cross-service dependencies added |
| IV. Spec-First Delivery | PASS | This spec + plan exists |
| V. Compiler-Enforced Contracts | N/A | No .fs/.fsi changes |
| VI. Test Evidence | PASS | Verification via running demo scripts (see quickstart.md) |
| VII. Observability | N/A | No service changes |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/005-fix-container-build-scripts/
├── spec.md
├── plan.md              # This file
├── research.md
├── quickstart.md
├── data-model.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Scripting/
├── demos_py/
│   ├── generate_stubs.sh           # No changes (sed fix already correct)
│   └── generated/
│       ├── __init__.py              # No changes
│       ├── physics_hub_pb2.py       # Regenerate
│       └── physics_hub_pb2_grpc.py  # Regenerate (relative import fix)
nuget.config                         # Remove or condition the local-packages source
Containerfile                        # Remove PYTHONPATH workaround
```

**Structure Decision**: No new files or directories. All changes are modifications to existing files plus regeneration of Python stubs.

## Design

### Part 1: Fix NuGet Source Configuration

**Problem**: The repo `nuget.config` declares `<add key="local" value="local-packages" />` which is a relative path. In the container, the Containerfile creates `/src/local-packages` and registers it in the global NuGet config — so FSI finds packages there. But on dev workstations, the `local-packages/` directory doesn't exist, causing NU1301 errors when FSI processes `#r "nuget: ..."` directives.

**Solution**: Remove the `local` source from the repo-level `nuget.config`. The container already registers the source in the global NuGet config (line 58-59 of Containerfile). On dev workstations, PhysicsClient 0.5.0 is available from `~/.local/share/nuget-local/` (registered in the user's global NuGet config). The repo-level config only needs `nuget.org`.

**Why not `.gitkeep`**: An empty `local-packages/` directory would satisfy NuGet but contain no packages, leading to confusing "package not found" errors instead of clean resolution from the correct source.

**Files**: `nuget.config`

### Part 2: Python Stub Regeneration

**Problem**: The committed `physics_hub_pb2_grpc.py` has a bare `import physics_hub_pb2` (line 6) instead of `from . import physics_hub_pb2`. The `generate_stubs.sh` script has the correct sed fix but was never run on the committed files.

**Solution**: Regenerate stubs via `generate_stubs.sh` (already done during research), commit the result, and remove the `ENV PYTHONPATH=...` line from the Containerfile.

**Files**: `Scripting/demos_py/generated/physics_hub_pb2_grpc.py`, `Scripting/demos_py/generated/physics_hub_pb2.py`, `Containerfile`

## What Changed From Original Plan

| Original Plan | Revised Plan | Why |
|---------------|--------------|-----|
| Add AssemblyResolve handler to Prelude.fsx + PhysicsClient.fsx | No F# script changes | .NET forward version unification already works in FSI; 004-fix resolved the NuGet dep graph |
| 4 files modified + 2 regenerated | 3 files modified + 2 regenerated | Dropped Prelude.fsx and PhysicsClient.fsx changes |
| Root cause: FSI assembly mismatch | Root cause: NU1301 from missing local-packages dir | Reproduction testing disproved the original diagnosis |

## Complexity Tracking

No violations to justify.
