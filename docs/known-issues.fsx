(**
---
title: Known Issues & Limitations
category: Reference
categoryindex: 5
index: 7
description: Current limitations, platform requirements, and workarounds.
---
*)

(**
# Known Issues & Limitations

This page documents known issues, platform-specific constraints, and recommended
workarounds for the Physics Sandbox. Each entry describes the problem, its root
cause, and how to resolve or work around it.

---

## gRPC Configuration

### HTTP/2 Protocol for Plain HTTP

F# service projects must reference `Grpc.AspNetCore.Server` (not the umbrella
`Grpc.AspNetCore` package) to avoid proto compilation errors in non-C# projects.
When running gRPC over plain HTTP (no TLS), the Aspire AppHost must configure
Kestrel to accept HTTP/2 on cleartext connections:
*)

(*** do-not-eval ***)
// In the AppHost project, set the environment variable on each gRPC resource:
//   ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS = Http1AndHttp2
//
// Without this, Kestrel defaults to HTTP/1.1 only on non-TLS endpoints,
// and gRPC calls fail with "Protocol error" because gRPC requires HTTP/2.

(**
### SSL Certificate Validation in Integration Tests

Aspire integration tests that connect to services over HTTPS must bypass
certificate validation for the development certificate. Without this, the
`HttpClient` rejects the self-signed cert and all gRPC calls fail.
*)

(*** do-not-eval ***)
// When creating a gRPC channel for integration tests, configure the handler:
//
// let handler = new System.Net.Http.SocketsHttpHandler(
//     SslOptions = System.Net.Security.SslClientAuthenticationOptions(
//         RemoteCertificateValidationCallback = fun _ _ _ _ -> true
//     )
// )
// let channel = Grpc.Net.Client.GrpcChannel.ForAddress(address, GrpcChannelOptions(HttpHandler = handler))

(**
---

## Stride3D / Viewer

### Linux System Dependencies

The Stride3D viewer requires several system packages on Linux. Missing any of
these causes runtime crashes (often with unhelpful native library errors).

Required packages: `openal`, `freetype2`, `sdl2`, `ttf-liberation`.

FreeImage needs a compatibility symlink because Stride looks for `freeimage.so`
rather than the versioned library name:
*)

(*** do-not-eval ***)
// Create the FreeImage symlink:
//   ln -sf /usr/lib/libfreeimage.so /usr/lib/freeimage.so
//
// The GLSL shader compiler binary must also be present at:
//   linux-x64/glslangValidator.bin

(**
### Camera Controller Conflict

Stride's `Add3DCameraController()` extension installs a built-in camera input
handler. If you also have a custom `CameraController` component, the two fight
for mouse/keyboard input, causing erratic camera behavior.

**Workaround:** Use `Add3DCamera()` (without the controller) and apply all camera
transforms manually through your own component.
*)

(*** do-not-eval ***)
// Correct: add camera without built-in controller
// game.Add3DCamera()
//
// Wrong: this conflicts with custom CameraController
// game.Add3DCameraController()

(**
### Vector3 Interop with F#

Stride's `Vector3` type uses `inref<>` operator overloads (C# `in` parameters)
for `+`, `-`, and `*`. F# cannot call these operators with the standard `+`/`*`
syntax because F# does not support `inref<>` parameter passing for operators.

The same limitation applies to `Vector3.Cross` and similar static methods that
use `byref` calling conventions.

**Workaround:** Use the explicit static methods with mutable variables:
*)

(*** do-not-eval ***)
// Instead of:  let result = a + b
// Use:
let mutable result = Unchecked.defaultof<_> // Stride.Core.Mathematics.Vector3
// Stride.Core.Mathematics.Vector3.Add(&a, &b, &result)

// Instead of:  let cross = Vector3.Cross(a, b)
// Use:
let mutable cross = Unchecked.defaultof<_>
// Stride.Core.Mathematics.Vector3.Cross(&a, &b, &cross)

(**
### Asset Compiler in CI / Headless Builds

The Stride asset compiler requires GPU access, font files, and FreeImage at
build time. In CI or headless environments, this fails.

**Workaround:** Pass `StrideCompilerSkipBuild=true` to skip asset compilation:
*)

(*** do-not-eval ***)
// Headless / CI build:
//   dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
//   dotnet test  PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
//
// The viewer's .fsproj defaults StrideCompilerSkipBuild to false,
// so live GPU builds compile assets normally without extra flags.

(**
---

## Physics Engine

### Plane Bodies Approximated as Static Boxes

BepuPhysics2 does not support infinite plane shapes. Ground planes are
approximated as very large static boxes (e.g., 1000x1x1000 units). This means:

- Planes are static bodies and are **not tracked** in the simulation state stream.
- Objects that travel far enough laterally can fall off the "infinite" plane.
- The plane has a finite thickness (1 unit), which is normally invisible.

There is no workaround other than using a sufficiently large box for the
expected simulation bounds.

### Proto Type Name Conflicts with BepuFSharp

The proto-generated types `Sphere` and `Box` (in the `PhysicsSandbox.Shared.Contracts`
namespace) conflict with BepuFSharp's native `Sphere` and `Box` shape types
when both are opened in an F# file.

**Workaround:** Use F# type aliases to disambiguate:
*)

(*** do-not-eval ***)
// At the top of files that need both proto types and BepuFSharp shapes:
type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box

// Then use ProtoSphere/ProtoBox for gRPC messages
// and Sphere/Box for physics engine shapes.

(**
---

## Build System

### Solution File Format (.slnx)

The solution file uses the `.slnx` format (XML-based, introduced in .NET 10)
rather than the traditional `.sln` format. Older tooling or IDE versions that
do not recognize `.slnx` will fail to open the solution.

**Requirement:** .NET 10.0 SDK or later.

### BepuFSharp NuGet Packaging

When packing BepuFSharp, NuGet emits `NU5104` warnings because BepuPhysics2
dependencies use prerelease version numbers. These warnings are harmless but
noisy.

**Workaround:** Suppress the warning during pack:
*)

(*** do-not-eval ***)
// Pack with warning suppression:
//   dotnet pack -p:NoWarn=NU5104
//
// The local NuGet feed is located at:
//   ~/.local/share/nuget-local/

(**
---

## Stride3D / Viewer (Post-005)

### Custom Shape Rendering

Triangle, mesh, and convex hull shapes now render with actual geometry (custom
vertex/index buffers) in both solid and wireframe views. Compound shapes decompose
into individually-rendered children. ConvexHull face computation uses the MIConvexHull
NuGet library. ShapeRef and CachedRef resolve to their underlying shapes before
rendering. All 10 shape types render with accurate collision-matching geometry.

---

## Physics Engine (005 Release)

### SetBodyPose for Static Bodies

`SetBodyPose` command rejects static bodies. Static bodies cannot be repositioned
after creation — they must be removed and re-created.

---

## Scripting Library (005 Release)

### Constraint Builder Coverage

Only 4 of 10 constraint types have convenience builders in the Scripting library
(`BallSocket`, `Hinge`, `Weld`, `DistanceLimit`). The remaining 6
(`DistanceSpring`, `SwingLimit`, `TwistLimit`, `LinearAxisMotor`, `AngularMotor`,
`PointOnLine`) require manual proto message construction via the full PhysicsClient
API.

---

## Build System (005 Release)

### NuGet Package Cache Staleness

After repacking local NuGet packages (`PhysicsSandbox.Shared.Contracts`,
`PhysicsClient`, `PhysicsSandbox.Scripting`), NuGet may serve stale cached
versions. Clear the global packages folder if new proto types are not visible:
*)

(*** do-not-eval ***)
// Clear stale NuGet cache for local packages:
//   rm -rf ~/.nuget/packages/physicssandbox.shared.contracts/0.1.0
//   dotnet restore --force

(**
---

## Physics Engine (Post-005)

### Static Mesh Body MotionType

Static mesh bodies (mass=0) require explicit `MotionType.Static` (enum value 2).
The default MotionType is Dynamic (0), and mass=0 + Dynamic is rejected by the server.
Without the explicit MotionType, the mesh body silently fails to be created.
*)

(*** do-not-eval ***)
// F#: use withMotionType BodyMotionType.Static
// Python: with_motion_type(cmd, 2)

(**
### Mesh Triangle Size

Mesh collision triangles must be approximately 2m+ per edge for reliable collision
detection. Very thin or narrow triangles (from parametric cross-section strips)
allow small objects to fall through. Use heightmap grids with well-shaped quads
(2 triangles per ~2×2m cell) instead of narrow strip geometry.

---

## MCP Server (Post-005)

### Nullable Parameter Pattern

MCP tool parameters use `Nullable<T>` (not F# `Option<T>`) for optional value types.
The ModelContextProtocol.AspNetCore framework only recognizes `Nullable<T>` as optional
in auto-generated JSON schemas — F#'s `?param: Type` (which compiles to `FSharpOption<T>`)
is treated as required. Use `param.HasValue`/`param.Value` instead of `defaultArg`.

### Proto MeshShape vs Triangle Naming

The proto `MeshShape` message (Shape oneof field `mesh`) uses `MeshTriangle` for its
triangle elements — not `Triangle` (which is a separate shape type). In F# scripts
use `MeshShape()` and `MeshTriangle()`. In Python use `pb.MeshShape(triangles=[pb.MeshTriangle(...)])`.

---

## No Unimplemented Features

The codebase contains **zero** `TODO`, `FIXME`, `HACK`, `BUG`, `XXX`, or
`NotImplementedException` instances across all source files under `src/`.
All planned features have been fully implemented and tested.
*)
