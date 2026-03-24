# Research: Upgrade BepuFSharp

## R-001: API Compatibility Between 0.2.0-beta.1 and 0.3.0

**Decision**: The upgrade is a safe drop-in replacement. All 193 non-StrideInterop API members are identical between versions.

**Rationale**: XML doc comparison shows 200 members in 0.2.0-beta.1 vs 193 in 0.3.0. The 7 removed members are all in the `BepuFSharp.StrideInterop` module, which is not used anywhere in the PhysicsSandbox codebase. All other types, functions, and properties have identical signatures.

**Alternatives considered**: None — 0.3.0 is the only newer version available.

## R-002: Dependency Changes

**Decision**: BepuFSharp 0.3.0 drops the `Stride.BepuPhysics 4.3.0.2507` dependency. All other transitive dependencies are unchanged.

**Rationale**: Nuspec comparison:
- 0.2.0-beta.1 deps: BepuPhysics 2.5.0-beta.28, BepuUtilities 2.5.0-beta.28, FSharp.Core 10.0.104, **Stride.BepuPhysics 4.3.0.2507**
- 0.3.0 deps: BepuPhysics 2.5.0-beta.28, BepuUtilities 2.5.0-beta.28, FSharp.Core 10.0.104

This aligns with the StrideInterop module removal — the Stride dependency was only needed for that module.

**Alternatives considered**: N/A.

## R-003: StrideInterop Usage in Codebase

**Decision**: No production or test code uses `BepuFSharp.StrideInterop`. Only archived spec documents reference it.

**Rationale**: `grep -r StrideInterop` across all `.fs`, `.fsx`, `.fsproj`, `.csproj` files returns zero matches. The only hits are in `.specify/archive/` documentation from the original 005-stride-bepu-integration spec, which planned but never implemented direct StrideInterop usage.

**Alternatives considered**: N/A.

## R-004: Package Availability

**Decision**: BepuFSharp 0.3.0 is available in the local NuGet feed at `~/.local/share/nuget-local/BepuFSharp.0.3.0.nupkg`.

**Rationale**: The project uses a local NuGet feed configured in NuGet.config. The 0.3.0 package is already present.

**Alternatives considered**: N/A.
