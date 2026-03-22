# Quickstart: Improve Physics Demos

## Prerequisites

- Physics server running (via `./start.sh` or `dotnet run --project src/PhysicsSandbox.AppHost`)
- .NET 10.0 SDK (for F# scripts)
- Python 3.10+ with grpcio (for Python demos)

## Running Individual Demos

```bash
# F# demo (replace NN with demo number)
dotnet fsi Scripting/demos/DemoNN_Name.fsx

# Python demo
python Scripting/demos_py/demoNN_name.py
```

## Running All Demos

```bash
# F# interactive runner (press Space to advance)
dotnet fsi Scripting/demos/RunAll.fsx

# F# automated runner
dotnet fsi Scripting/demos/AutoRun.fsx

# Python interactive runner
python Scripting/demos_py/run_all.py

# Python automated runner
python Scripting/demos_py/auto_run.py
```

## Development Workflow

For each demo improvement:

1. Read the current F# demo
2. Discuss improvements with the user
3. Edit the F# version
4. Mirror changes to the Python version
5. Test by running through the AllDemos runner

## Key Files

| File | Purpose |
|------|---------|
| `Scripting/demos/Prelude.fsx` | F# shared helpers (generators, presets, steering) |
| `Scripting/demos/AllDemos.fsx` | F# demo registry (all 15 demos as records) |
| `Scripting/demos_py/prelude.py` | Python shared helpers |
| `Scripting/demos_py/all_demos.py` | Python demo registry |
