# Quickstart: Static Mesh Terrain Demos

**Branch**: `004-mesh-terrain-demos` | **Date**: 2026-03-25

## Prerequisites

- PhysicsSandbox server running (`./start.sh` or `dotnet run --project src/PhysicsSandbox.AppHost`)
- .NET 10.0 SDK installed (for F# scripts)
- Python 3.10+ with grpcio installed (for Python scripts)

## Running Individual Demos

### F# Scripts

```bash
# Demo 23: Ball Rollercoaster
dotnet fsi Scripting/demos/Demo23_BallRollercoaster.fsx

# Demo 24: Halfpipe Arena
dotnet fsi Scripting/demos/Demo24_HalfpipeArena.fsx

# With custom server address
dotnet fsi Scripting/demos/Demo23_BallRollercoaster.fsx http://localhost:5180
```

### Python Scripts

```bash
# Demo 23: Ball Rollercoaster
python Scripting/demos_py/demo23_ball_rollercoaster.py

# Demo 24: Halfpipe Arena
python Scripting/demos_py/demo24_halfpipe_arena.py
```

## Running Full Demo Suite

```bash
# Interactive (press Space to advance between demos)
dotnet fsi Scripting/demos/RunAll.fsx

# Non-interactive (auto-advance)
dotnet fsi Scripting/demos/AutoRun.fsx

# Python equivalents
python Scripting/demos_py/run_all.py
python Scripting/demos_py/auto_run.py
```

## What to Observe

### Demo 23: Ball Rollercoaster
- A curved track made of mesh triangles with hills, drops, and banked turns
- Balls released at the top rolling down the entire track
- Camera follows the lead ball through the course
- Narration describing each track feature as balls pass through

### Demo 24: Halfpipe Arena
- A U-shaped halfpipe constructed from mesh triangles
- Balls and capsules dropped from above into the halfpipe
- Objects oscillating back and forth, gradually losing energy
- Camera orbiting and panning along the halfpipe
- Narration describing the physics interactions

## Verification

Both demos succeed if:
1. No errors printed during execution
2. Objects stay on mesh surfaces (no clipping through)
3. Camera transitions are smooth
4. Narration text appears in the viewer overlay
5. Demo completes within ~30 seconds
