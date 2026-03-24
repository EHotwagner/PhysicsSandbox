# Quickstart: Enhance Demos with New Shapes and Viewer Labels

## Prerequisites

- .NET 10.0 SDK
- Python 3.10+ with grpcio, protobuf
- Running PhysicsSandbox (via `./start.sh` or Aspire AppHost)

## Development Order

1. **Proto contract** — Add `SetDemoMetadata` message and ViewCommand variant
2. **Build contracts** — `dotnet build src/PhysicsSandbox.Shared.Contracts/` to regenerate C# types
3. **Regenerate Python stubs** — Run proto compiler for `Scripting/demos_py/generated/`
4. **Viewer window title** — Set `game.Window.Title` in Program.fs
5. **Viewer label rendering** — Add demo metadata fields to SceneState, render in update loop
6. **PhysicsClient helper** — Add `setDemoMetadata` function to ViewCommands module + .fsi
7. **Prelude helpers** — Add `setDemoInfo` to F# Prelude.fsx and Python prelude.py
8. **Add makeMeshCmd** — Add mesh shape builder to Prelude.fsx and prelude.py
9. **Enhance existing demos** — Add new shapes to 8+ existing demos
10. **New Demo 19** — Shape Gallery (all shape types)
11. **New Demo 20** — Compound Constructions
12. **New Demo 21** — Mesh & Hull Playground
13. **Register new demos** — Update AllDemos, RunAll, AutoRun, Python equivalents
14. **Add demo metadata** — All 21 demos call `setDemoInfo` with name + description
15. **Tests** — Integration tests for metadata transport, unit tests for new helpers

## Quick Validation

```bash
# Build everything
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Manual validation: start sandbox and run a demo
./start.sh
# In another terminal, run a demo script
dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx
# Verify: viewer shows window title + demo label in top-left
```
