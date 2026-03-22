# Quickstart: Scripting Library NuGet Package

**Feature**: 004-scripting-nuget-package | **Date**: 2026-03-22

## Overview

This feature publishes 4 projects as local NuGet packages, migrates consumers to PackageReferences, updates script DLL paths to version-agnostic NuGet references, and fixes port inconsistencies.

## Implementation Order

### Phase 1: Package Foundation (Layer 0)

1. Add packaging metadata to `PhysicsSandbox.Shared.Contracts.csproj`:
   - `IsPackable=true`, `Version=0.1.0`, `PackageId=PhysicsSandbox.Shared.Contracts`

2. Add packaging metadata to `PhysicsSandbox.ServiceDefaults.csproj`:
   - `IsPackable=true`, `Version=0.1.0`, `PackageId=PhysicsSandbox.ServiceDefaults`
   - Set `IsAspireSharedProject=false` or remove the flag

3. Pack and publish both:
   ```bash
   dotnet pack src/PhysicsSandbox.Shared.Contracts -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/
   dotnet pack src/PhysicsSandbox.ServiceDefaults -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/
   ```

### Phase 2: Package PhysicsClient (Layer 1)

4. In `PhysicsClient.fsproj`, replace ProjectReferences with PackageReferences:
   ```xml
   <!-- Remove -->
   <ProjectReference Include="..\PhysicsSandbox.Shared.Contracts\..." />
   <ProjectReference Include="..\PhysicsSandbox.ServiceDefaults\..." />
   <!-- Add -->
   <PackageReference Include="PhysicsSandbox.Shared.Contracts" Version="0.1.0" />
   <PackageReference Include="PhysicsSandbox.ServiceDefaults" Version="0.1.0" />
   ```

5. Pack and publish:
   ```bash
   dotnet pack src/PhysicsClient -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/
   ```

### Phase 3: Package Scripting (Layer 2)

6. In `PhysicsSandbox.Scripting.fsproj`, replace ProjectReference with PackageReference:
   ```xml
   <!-- Remove -->
   <ProjectReference Include="..\PhysicsClient\PhysicsClient.fsproj" />
   <!-- Add -->
   <PackageReference Include="PhysicsClient" Version="0.1.0" />
   ```

7. Pack and publish:
   ```bash
   dotnet pack src/PhysicsSandbox.Scripting -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/
   ```

### Phase 4: Migrate Consumers

8. In `PhysicsSandbox.Mcp.fsproj`:
   - Replace ProjectReference to Scripting with PackageReference
   - Remove any direct ProjectReferences to PhysicsClient/Contracts/ServiceDefaults (now transitive)

9. In `PhysicsSandbox.Scripting.Tests.fsproj`:
   - Replace ProjectReference to Scripting with PackageReference

### Phase 5: Update Scripts and Demos

10. Update `Scripting/scripts/Prelude.fsx`:
    ```fsharp
    #r "nuget: PhysicsSandbox.Scripting"
    ```

11. Update `Scripting/demos/Prelude.fsx` and `AutoRun.fsx`:
    ```fsharp
    #r "nuget: PhysicsClient"
    #r "nuget: Spectre.Console"
    ```
    (Remove hardcoded DLL paths and redundant nuget refs that are now transitive)

12. Fix all `localhost:5000` → `localhost:5180` across F# demos, Python demos, service fallbacks, and .mcp.json

### Phase 6: Verify

13. Build full solution: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
14. Run all tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
15. Verify no `localhost:5000` references remain
16. Verify no hardcoded DLL `#r` paths remain for packaged projects

## Key Files

| File | Change |
|------|--------|
| `src/PhysicsSandbox.Shared.Contracts/*.csproj` | Add packaging metadata |
| `src/PhysicsSandbox.ServiceDefaults/*.csproj` | Add packaging metadata, disable Aspire shared flag |
| `src/PhysicsClient/*.fsproj` | ProjectRef→PackageRef |
| `src/PhysicsSandbox.Scripting/*.fsproj` | ProjectRef→PackageRef |
| `src/PhysicsSandbox.Mcp/*.fsproj` | ProjectRef→PackageRef |
| `tests/PhysicsSandbox.Scripting.Tests/*.fsproj` | ProjectRef→PackageRef |
| `Scripting/scripts/Prelude.fsx` | DLL→NuGet ref |
| `Scripting/demos/Prelude.fsx` | DLL→NuGet ref |
| `Scripting/demos/AutoRun.fsx` | DLL→NuGet ref |
| `Scripting/demos/*.fsx` (5 files) | Port fix |
| `Scripting/demos_py/prelude.py` | Port fix |
| `Scripting/demos_py/auto_run.py` | Port fix |
| `Scripting/demos_py/run_all.py` | Port fix |
| `src/PhysicsClient/Program.fs` | Port fix |
| `src/PhysicsViewer/Program.fs` | Port fix |
| `.mcp.json` | Port fix |
