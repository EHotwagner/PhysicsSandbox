# Quickstart: 005-enhance-demos

## Prerequisites

1. PhysicsSandbox server running: `./start.sh --http`
2. Wait for all services to be healthy (check Aspire dashboard or `curl http://localhost:5180`)

## Run All Demos

```bash
# F# (primary)
dotnet fsi Scripting/demos/AutoRun.fsx

# Python
python Scripting/demos_py/auto_run.py
```

Expected: `Results: 18 passed, 0 failed`

## Run Individual Demo

```bash
# F# — run single demo by file
dotnet fsi Scripting/demos/16_Constraints.fsx

# Python — run single demo by file
python Scripting/demos_py/demo_16_constraints.py
```

## Verify Specific Fixes

### Demo 03 (Crate Stack) — Boulder hits tower centrally
```bash
dotnet fsi Scripting/demos/03_CrateStack.fsx
# Watch: boulder approaches from front, tower topples dramatically
```

### Demo 04 (Bowling Alley) — Ball hits pyramid head-on
```bash
dotnet fsi Scripting/demos/04_BowlingAlley.fsx
# Watch: ball approaches from directly in front, pyramid scatters
```

## Verify New Capabilities

### Demo 16 (Constraints) — Pendulum, hinge, weld
```bash
dotnet fsi Scripting/demos/16_Constraints.fsx
# Watch: pendulum swings, bridge flexes under load, weld cluster tumbles as one
```

### Demo 17 (Query Range) — Raycasts and overlaps
```bash
dotnet fsi Scripting/demos/17_QueryRange.fsx
# Check: console prints hit body IDs, positions, distances, overlap counts
```

### Demo 18 (Kinematic Sweep) — Pusher plows through bodies
```bash
dotnet fsi Scripting/demos/18_KinematicSweep.fsx
# Watch: cyan kinematic box moves through scene, pushing dynamic bodies aside
```

## Key Files

| File | Purpose |
|------|---------|
| `Scripting/demos/Prelude.fsx` | Shared helpers — new shape/constraint/query builders |
| `Scripting/demos/AllDemos.fsx` | Demo registry (18 entries) |
| `Scripting/demos/AutoRun.fsx` | Automated runner (0-failure gate) |
| `Scripting/demos_py/prelude.py` | Python equivalent of Prelude.fsx |
| `Scripting/demos_py/all_demos.py` | Python demo registry |
