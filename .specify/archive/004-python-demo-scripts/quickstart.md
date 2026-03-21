# Quickstart: Python Demo Scripts

**Feature**: 004-python-demo-scripts

## Prerequisites

1. Build the .NET solution: `dotnet build PhysicsSandbox.slnx`
2. Start the Aspire stack: `dotnet run --project src/PhysicsSandbox.AppHost` (or `./start.sh`)
3. Python 3.10+ with pip

## Setup

```bash
# Install Python dependencies
pip install -r demos_py/requirements.txt

# Generate proto stubs (only needed once, or after proto changes)
cd demos_py && bash generate_stubs.sh && cd ..
```

## Run All Demos (Automated)

```bash
python demos_py/auto_run.py
# Or with custom server address:
python demos_py/auto_run.py http://localhost:5000
```

## Run All Demos (Interactive)

```bash
python demos_py/run_all.py
# Press Enter/Space to advance between demos
```

## Run a Single Demo

```bash
python demos_py/demo01_hello_drop.py
python demos_py/demo05_marble_rain.py http://custom:5000
```

## Demo List

| # | Name | Description |
|---|------|-------------|
| 01 | Hello Drop | A single bowling ball falls from height |
| 02 | Bouncing Marbles | Five marbles from different heights |
| 03 | Crate Stack | Tower of 8 crates — push the top one |
| 04 | Bowling Alley | Bowling ball vs pyramid of bricks |
| 05 | Marble Rain | 20 random spheres rain down |
| 06 | Domino Row | 12 dominoes toppled by a push |
| 07 | Spinning Tops | Bodies spinning with torques + wireframe |
| 08 | Gravity Flip | Normal gravity, then reversed, then sideways |
| 09 | Billiards | Cue ball breaks a triangle formation |
| 10 | Chaos Scene | Everything: presets, generators, steering, gravity, camera sweeps |
| 11 | Body Scaling | Progressive body count: 50 → 100 → 200 → 500 |
| 12 | Collision Pit | 120 spheres in a walled pit |
| 13 | Force Frenzy | 100 bodies with escalating impulses/torques |
| 14 | Domino Cascade | 120 semicircular dominoes chain reaction |
| 15 | Overload | 200+ bodies combined stress ceiling test |

## Troubleshooting

- **"Connection refused"**: Ensure the Aspire stack is running (`dotnet run --project src/PhysicsSandbox.AppHost`)
- **"ModuleNotFoundError: generated"**: Run `bash demos_py/generate_stubs.sh` to generate proto stubs
- **"No module named grpc"**: Run `pip install -r demos_py/requirements.txt`
