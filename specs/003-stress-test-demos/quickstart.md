# Quickstart: Stress Test Demos

**Feature**: 003-stress-test-demos

## Prerequisites

1. Build the solution: `dotnet build PhysicsSandbox.slnx`
2. Start Aspire: `dotnet run --project src/PhysicsSandbox.AppHost`
3. Wait for all services to be running (check Aspire dashboard)

## Run All Demos (Including Stress Tests)

```bash
# Interactive (press Space/Enter to advance)
dotnet fsi demos/RunAll.fsx

# Automated (no interaction needed)
dotnet fsi demos/AutoRun.fsx

# Custom server address
dotnet fsi demos/RunAll.fsx http://localhost:5000
```

## Run Individual Stress Demos

```bash
dotnet fsi demos/Demo11_BodyScaling.fsx
dotnet fsi demos/Demo12_CollisionPit.fsx
dotnet fsi demos/Demo13_ForceFrenzy.fsx
dotnet fsi demos/Demo14_DominoCascade.fsx
dotnet fsi demos/Demo15_Overload.fsx
```

## Run via MCP Tools

For AI assistant-driven stress testing, use the existing MCP tools.

### Demo 11 — Body Scaling via MCP

Use the built-in stress test runner for automated body scaling:

1. `start_stress_test(scenario: "body-scaling", max_bodies: 500)` — returns a test ID
2. Wait 30–60 seconds for completion
3. `get_stress_test_status(test_id)` — shows peak body count, degradation point, command rate
4. Compare degradation body count with the `[TIME]` markers from the script-based Demo 11

### Demo 15 — Overload via MCP

Replicate the combined stress scenario manually through MCP tools:

1. `restart_simulation` — clean slate
2. `generate_pyramid(layers: 7, position: "-5,0,0")` — Act 1 formation (~28 bodies)
3. `generate_stack(count: 10, position: "5,0,0")` — Act 1 formation
4. `generate_row(count: 12, position: "-5,0,5")` — Act 1 formation
5. `batch_commands` with 100 `add_body` sphere commands — Act 2 sphere rain
6. `batch_commands` with `apply_impulse` commands for all pyramid bodies — Act 3 impulse storm
7. `set_gravity(x: 0, y: 10, z: 0)` — Act 4 gravity flip
8. `get_diagnostics` — check pipeline timing (tick/serialize/transfer/render breakdown)
9. `set_gravity(x: 6, y: 0, z: 6)` — Act 4 sideways gravity
10. `set_camera` at multiple angles — Act 5 camera sweep
11. `get_diagnostics` again — compare pipeline timing under load

### Comparing Script vs MCP

Use the built-in comparison test for quantitative overhead measurement:

```
start_stress_test(scenario: "mcp-vs-script", max_bodies: 100, duration_seconds: 60)
get_stress_test_status(test_id)
```

This reports: script time, MCP time, batched MCP time, and overhead percentage.

## Reading Results

Stress demos print `[TIME]` markers showing elapsed milliseconds for each phase:

```
[TIME] Tier 50 setup: 120 ms
[TIME] Tier 50 simulation (3s): 3015 ms
[TIME] Tier 100 setup: 245 ms
...
```

Degradation is visible when setup or simulation times increase disproportionately between tiers.

## Troubleshooting

- **Connection refused**: Ensure Aspire is running and server is on http://localhost:5000
- **Batch failures**: Check `[BATCH FAIL]` messages — may indicate body ID conflicts (ensure resetSimulation runs)
- **Timeout**: Stress demos with 500 bodies may take up to 5 minutes. Be patient.
- **Viewer lag**: Expected at high body counts — the viewer's frame rate degradation is itself a useful observation
