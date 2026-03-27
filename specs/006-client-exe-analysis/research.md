# Research: PhysicsClient Exe vs Library Analysis

**Date**: 2026-03-27
**Feature**: 006-client-exe-analysis

## R1: What does PhysicsClient's Program.fs entry point actually do?

**Finding**: The entry point (`src/PhysicsClient/Program.fs`) performs exactly 4 actions:
1. Creates an `ApplicationBuilder` host with `AddServiceDefaults()` (Aspire telemetry/health)
2. Reads the server address from Aspire-injected environment variables (`services__server__https__0` / `services__server__http__0`), falling back to `http://localhost:5180`
3. Logs "PhysicsClient starting, server address: {Address}"
4. Calls `host.Run()` to keep the process alive indefinitely

**Analysis**: The entry point does **no useful work**. It:
- Does NOT create a gRPC session or connect to the server
- Does NOT run any simulation commands
- Does NOT start any background processing
- Does NOT expose any endpoints
- Only stays alive for Aspire dashboard visibility (the comment says "Keep alive for Aspire orchestration")

**Decision**: The entry point provides zero functional value beyond appearing in the Aspire dashboard as a running resource.

## R2: Who consumes PhysicsClient and how?

**Finding**: PhysicsClient has exactly 4 consumer categories:

| Consumer | Reference Type | Uses Exe? | Uses Library? |
|----------|---------------|-----------|---------------|
| PhysicsSandbox.Scripting | Project reference | No | Yes (all modules) |
| PhysicsClient.Tests | Project reference | No | Yes (all modules) |
| F# demo scripts (Prelude.fsx) | NuGet `#r "nuget: PhysicsClient, 0.5.0"` | No | Yes (all modules) |
| AppHost | Project reference + `AddProject` | Yes (launches as service) | No |

**Analysis**: 3 out of 4 consumers use PhysicsClient purely as a library. Only AppHost launches it as an executable, and the launched process does nothing useful.

**Decision**: Library consumption is the sole value-delivering usage pattern. The Exe usage is vestigial.

## R3: Does removing PhysicsClient from AppHost break anything?

**Finding**:
- Integration tests (`PhysicsSandbox.Integration.Tests`) do NOT reference PhysicsClient or the "client" resource name at all
- No other Aspire resource depends on PhysicsClient (`WithReference(client)` appears nowhere)
- Demo scripts connect directly to the server via `Session.connect("http://localhost:5180")` — they don't route through the PhysicsClient process
- The Scripting library uses PhysicsClient as a library dependency, not as a running service

**Decision**: Removing PhysicsClient from AppHost orchestration would have zero functional impact on the system.

## R4: Can an Exe project be consumed as a library in .NET?

**Finding**: Yes, .NET supports this. An `Exe` project produces both a DLL and an executable wrapper. Project references and NuGet packages from Exe projects work — the consuming project links against the DLL. However:
- It's unconventional and confusing to other developers
- NuGet packages from Exe projects include the entry point assembly, which is unnecessary baggage
- `dotnet pack` on an Exe project may include unneeded runtime configuration files

**Decision**: While technically functional, `OutputType=Exe` for a project used primarily as a library is an anti-pattern that adds confusion without benefit.

## R5: Does ServiceDefaults dependency matter?

**Finding**: PhysicsClient references `PhysicsSandbox.ServiceDefaults` — but this reference is used ONLY in Program.fs (`builder.AddServiceDefaults()`). None of the library modules (Session, SimulationCommands, etc.) use ServiceDefaults. The NuGet package includes ServiceDefaults as a transitive dependency unnecessarily.

**Decision**: If PhysicsClient becomes a library-only project, the ServiceDefaults reference can be removed entirely, simplifying the dependency tree for NuGet consumers.

## R6: Impact on NuGet packaging

**Finding**: Current NuGet package (`PhysicsClient 0.5.0`) works despite being from an Exe project. However:
- The package carries unnecessary dependencies (ServiceDefaults, Microsoft.Extensions.Hosting)
- The assembly contains an entry point that NuGet consumers never use
- Removing the Exe output type and Program.fs would produce a cleaner, smaller NuGet package

**Decision**: Converting to Library would improve NuGet package quality by removing vestigial entry-point code and unnecessary hosting dependencies.

## Summary Recommendation

**Convert PhysicsClient from Exe to Library.** Evidence:

1. **Program.fs does nothing useful** — it only keeps a process alive for dashboard visibility
2. **All value comes from library consumption** — 3 of 4 consumers use it as a library; the 4th (AppHost) launches it to no effect
3. **No downstream breakage** — integration tests don't reference it, no service depends on it
4. **Cleaner NuGet package** — removing Exe/ServiceDefaults reduces unnecessary transitive dependencies
5. **Constitution alignment** — Principle III (Shared Nothing) is better served by a focused library without an executable entry point

### Required Changes

1. `src/PhysicsClient/PhysicsClient.fsproj`: Change `OutputType` from `Exe` to `Library`, remove ServiceDefaults reference
2. `src/PhysicsClient/Program.fs`: Delete entirely
3. `src/PhysicsSandbox.AppHost/AppHost.cs`: Remove `AddProject<Projects.PhysicsClient>("client")` block (lines 15-17)
4. `src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj`: Remove `ProjectReference` to PhysicsClient
5. Version bump to 0.6.0 for NuGet repackaging

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Keep as Exe, do nothing | Maintains vestigial code and confusing architecture |
| Keep as Exe, add real service logic | No identified use case for a long-running client process — scripts and Scripting library handle all client needs |
| Conditional compilation (Exe for dev, Lib for NuGet) | Over-engineered for zero identified benefit of the Exe |
