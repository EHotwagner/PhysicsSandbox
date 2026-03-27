# Quickstart: PhysicsClient Exe vs Library Conversion

**Date**: 2026-03-27
**Feature**: 006-client-exe-analysis

## Overview

Convert PhysicsClient from an executable service to a library-only project. The executable entry point does no useful work — all value comes from library consumption by Scripting, tests, and F# demo scripts.

## Changes Required

### 1. PhysicsClient Project File
- Change `OutputType` from `Exe` to `Library`
- Remove `ProjectReference` to `PhysicsSandbox.ServiceDefaults` (only used by Program.fs)
- Bump `Version` to `0.6.0`

### 2. Delete Program.fs
- Remove `src/PhysicsClient/Program.fs` (22-line no-op entry point)
- Remove `<Compile Include="Program.fs" />` from fsproj

### 3. AppHost Changes
- Remove `AddProject<Projects.PhysicsClient>("client")` block from `AppHost.cs`
- Remove `ProjectReference` to PhysicsClient from `AppHost.csproj`

### 4. NuGet Repackaging
- `dotnet pack` the updated PhysicsClient as version 0.6.0
- Update `Scripting/demos/Prelude.fsx` to pin `PhysicsClient, 0.6.0`

## Verification

1. `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — builds clean
2. `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all tests pass
3. `dotnet run --project src/PhysicsSandbox.AppHost` — starts without PhysicsClient resource, all other services functional
4. F# demo scripts work with new NuGet package version

## What Does NOT Change

- All 12 PhysicsClient library modules (Session, SimulationCommands, ViewCommands, etc.)
- Scripting project reference to PhysicsClient
- Test project reference to PhysicsClient
- Any public API surface
