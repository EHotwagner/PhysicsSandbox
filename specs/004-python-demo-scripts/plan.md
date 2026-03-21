# Implementation Plan: Python Demo Scripts

**Branch**: `004-python-demo-scripts` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-python-demo-scripts/spec.md`

## Summary

Port all 15 F# physics demo scripts to Python, providing an equivalent scripting experience for Python developers. The Python demos communicate directly with PhysicsServer via gRPC using Python-generated protobuf stubs from the existing `physics_hub.proto` contract. A shared `prelude.py` module provides helper functions mirroring the F# Prelude (session management, body creation helpers, batch commands, timing). Three runner scripts provide automated, interactive, and individual execution modes.

## Technical Context

**Language/Version**: Python 3.10+
**Primary Dependencies**: grpcio, grpcio-tools, protobuf (for gRPC stub generation and communication)
**Storage**: N/A (stateless scripts communicating with running server)
**Testing**: N/A (demos are informal end-to-end smoke tests; no unit test framework needed)
**Target Platform**: Linux (same development container as the .NET services)
**Project Type**: Scripts (standalone Python scripts, not a library or service)
**Performance Goals**: Demo execution within 3 minutes for the full 15-demo suite (matching F# timing)
**Constraints**: Must not require .NET tooling to run; only Python + pip dependencies
**Scale/Scope**: 15 demo scripts + 1 prelude module + 2 runner scripts + generated proto stubs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicable? | Status | Notes |
|-----------|-------------|--------|-------|
| I. Service Independence | No | N/A | Python demos are client scripts, not a service. They connect to the existing PhysicsServer via gRPC — no new service created. |
| II. Contract-First | Yes | PASS | Demos use the existing `physics_hub.proto` contract. No new contracts needed. Python stubs are generated from the same proto file. |
| III. Shared Nothing | Yes | PASS | Python scripts reference only the proto-generated stubs. No direct project references to F# services. Communication exclusively via gRPC. |
| IV. Spec-First Delivery | Yes | PASS | This plan and spec exist before implementation. |
| V. Compiler-Enforced Contracts | No | N/A | Python scripts are not F# modules — no `.fsi` files required. The constitution scopes this to "public F# modules." |
| VI. Test Evidence | Partial | PASS | Demos are informal smoke tests (same as F# demos per archived spec 007). No unit tests required; the demos themselves serve as end-to-end verification. |
| VII. Observability | No | N/A | Client scripts, not a deployed service. No telemetry/health checks needed. |

**Engineering Constraint — "F# on .NET is the exclusive stack"**: This constraint states multi-language needs "MUST be addressed by separate services communicating via gRPC." The Python demos are client scripts (not a service) communicating via gRPC with the existing F# services. This satisfies the spirit and letter of the constraint — Python is used at the scripting/client layer, not within a service boundary.

**Pre-Phase 0 Gate**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/004-python-demo-scripts/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
demos_py/
├── __init__.py              # Package marker (empty)
├── prelude.py               # Shared helpers: session, commands, batch, timing, ID gen
├── all_demos.py             # All 15 demos registered as a list of (name, desc, run_fn)
├── auto_run.py              # Automated runner: all 15 demos, pass/fail summary
├── run_all.py               # Interactive runner: keypress advancement
├── generate_stubs.sh        # Script to generate Python proto stubs from physics_hub.proto
├── requirements.txt         # Python dependencies (grpcio, protobuf)
├── generated/               # Python proto stubs (generated, not hand-written)
│   ├── __init__.py
│   ├── physics_hub_pb2.py   # Generated message classes
│   └── physics_hub_pb2_grpc.py  # Generated service stubs
├── demo01_hello_drop.py
├── demo02_bouncing_marbles.py
├── demo03_crate_stack.py
├── demo04_bowling_alley.py
├── demo05_marble_rain.py
├── demo06_domino_row.py
├── demo07_spinning_tops.py
├── demo08_gravity_flip.py
├── demo09_billiards.py
├── demo10_chaos.py
├── demo11_body_scaling.py
├── demo12_collision_pit.py
├── demo13_force_frenzy.py
├── demo14_domino_cascade.py
└── demo15_overload.py
```

**Structure Decision**: Flat directory (`demos_py/`) at the repo root, parallel to the existing `demos/` directory for F# scripts. Generated proto stubs live in a `generated/` subdirectory. Each demo is a standalone script that imports from `prelude.py`. This mirrors the F# structure as closely as possible while following Python conventions (snake_case filenames, `__init__.py` packages).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Python in an F#-exclusive project | Constitution allows multi-language at client/script layer via gRPC | N/A — not a violation; demos are scripts, not services |
