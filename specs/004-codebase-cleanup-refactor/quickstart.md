# Quickstart: Codebase Cleanup and Refactoring

**Date**: 2026-03-25
**Feature**: 004-codebase-cleanup-refactor

## Prerequisites

- .NET 10.0 SDK
- PhysicsSandbox solution builds successfully: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- All tests pass: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

## Verification Strategy

Since this is a pure refactoring (no behavior changes), verification is:

1. **Build check**: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — zero warnings/errors
2. **Test suite**: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all 468 tests pass (384 unit + 84 integration)
3. **Surface area baselines**: No public API changes to PhysicsClient or Scripting NuGet packages
4. **Line count targets**: No src/ file exceeds 550 lines

## Implementation Order

1. **PhysicsSimulation internal consolidation** (lowest risk — all internal, no public API changes)
   - Extract ProtoConversions module
   - Extract ShapeConversion module
   - Update SimulationWorld and QueryHandler to use shared modules

2. **PhysicsClient shape builders** (medium risk — adds public module, existing APIs unchanged)
   - Create ShapeBuilders module with mkShape helpers
   - Refactor SimulationCommands to use addGenericBody pattern
   - Remove internal toVec3, delegate to Vec3Builders

3. **PhysicsViewer internal consolidation** (low risk — all internal)
   - Extract ProtoConversions module for proto→Stride conversions

4. **MCP consolidation** (medium risk — removes module, changes imports)
   - Delete MCP MeshResolver, use PhysicsClient.MeshResolver
   - Remove local nextId, use PhysicsClient.IdGenerator
   - Update ClientAdapter to delegate to SimulationCommands/ShapeBuilders

5. **Integration test helpers** (minimal risk)
   - Extract CreateGrpcChannel method

## Key Constraints

- F# compilation order matters — new modules must be added BEFORE their consumers in .fsproj files
- Every new public F# module requires a .fsi signature file (Constitution Principle V)
- Surface area baseline tests must be updated if public module structure changes
- MCP's Nullable<T> parameter pattern must be preserved (not converted to F# Option)
