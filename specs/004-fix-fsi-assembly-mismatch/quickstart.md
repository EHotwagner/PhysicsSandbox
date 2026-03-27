# Quickstart: Fix FSI Assembly Version Mismatch

**Date**: 2026-03-27
**Feature**: 004-fix-fsi-assembly-mismatch

## What This Fixes

F# Interactive (FSI) demo scripts fail with `FileNotFoundException` for `Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0` because the PhysicsClient NuGet package's dependency graph resolves to 8.x while the .NET 10 runtime only has 10.x.

## How To Verify

1. Build and pack:
   ```bash
   dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
   dotnet pack src/PhysicsClient -c Release -o ~/.local/share/nuget-local/
   ```

2. Run a demo script:
   ```bash
   dotnet run --project src/PhysicsSandbox.AppHost &
   # Wait for server to start
   dotnet fsi Scripting/demos/001-basic-physics.fsx
   ```

3. Expected: Script runs without assembly errors.

## Key Changes

- `src/PhysicsClient/PhysicsClient.fsproj`: Added explicit `Microsoft.Extensions.Logging.Abstractions 10.*` dependency, bumped to 0.5.0
- `Scripting/demos/Prelude.fsx`: Removed manual Logging.Abstractions reference, updated PhysicsClient to 0.5.0
