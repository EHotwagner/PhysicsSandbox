# Quickstart: 001-demo-script-modernization

## Prerequisites
- Aspire AppHost running: `dotnet run --project src/PhysicsSandbox.AppHost`
- Solution built: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

## Running Demos

### Non-interactive (all 10)
```bash
dotnet fsi demos/AutoRun.fsx
```

### Interactive (space/enter to advance)
```bash
dotnet fsi demos/RunAll.fsx
```

### Individual demo
```bash
dotnet fsi demos/Demo06_DominoRow.fsx
```

## Verifying Batch Usage
After running a batched demo, check the Aspire dashboard or use:
```bash
# Via MCP tool
get_command_log
```
Look for `SendBatchCommand` entries instead of multiple individual `SendCommand` entries.

## Key Changes from Previous Version
- `resetScene` → `resetSimulation` (uses server-side reset)
- Sequential body creation → `batchAdd` with command builders
- New Prelude helpers: `makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd`, `batchAdd`, `nextId`
