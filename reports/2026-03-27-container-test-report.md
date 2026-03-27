# Container Test Report — 2026-03-27

## Summary

Full end-to-end test of the PhysicsSandbox Containerfile: build from scratch, run all interaction models (Python REPL, F# REPL, MCP), identify and fix issues.

**Result:** 21/21 Python demos pass. MCP works (59 tools). F# REPL broken due to FSI regression in .NET SDK 10.0.201. Viewer requires correct GPU flag.

## Environment

- Host: Arch Linux 6.19.9, NVIDIA GeForce RTX 4070, driver 590.48.01
- Container runtime: Podman (rootless), nvidia-container-toolkit 1.19.0
- Base image: `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.201, Ubuntu 24.04)

## Issues Found and Fixed

### 1. Build Failure — Mcp Missing ServiceDefaults Reference

**Severity:** Blocker (build fails)

`PhysicsSandbox.Mcp.fsproj` was missing a `ProjectReference` to `PhysicsSandbox.ServiceDefaults`. `Program.fs` calls `builder.AddServiceDefaults()` (line 11) and `app.MapDefaultEndpoints()` (line 60), which are extension methods defined in `ServiceDefaults/Extensions.cs`. Every other service project had this reference.

```
error FS0039: The type 'WebApplicationBuilder' does not define the field, constructor or member 'AddServiceDefaults'.
error FS0039: The type 'WebApplication' does not define the field, constructor or member 'MapDefaultEndpoints'.
```

**Fix (committed, pushed):** Added `<ProjectReference Include="..\PhysicsSandbox.ServiceDefaults\PhysicsSandbox.ServiceDefaults.csproj" />` to the Mcp fsproj.

### 2. GPU Documentation — Missing NVIDIA Prerequisites

**Severity:** High (viewer crashes silently on NVIDIA hosts)

The README stated "replace `--device /dev/dri` with `--device nvidia.com/gpu=all`" for NVIDIA GPUs but didn't mention the nvidia-container-toolkit prerequisite. Without it, the viewer crashes with `GLXBadCurrentWindow` and no error appears in container stdout (Aspire captures child logs internally).

Viewer error chain on NVIDIA without toolkit:
```
pci id for fd 253: 10de:2786, driver (null)
failed to load driver: nvidia-drm
MESA: error: CreateSwapchainKHR failed with VK_ERROR_INITIALIZATION_FAILED
X Error of failed request:  GLXBadCurrentWindow
```

**Fix (committed, pushed):** Updated README and Containerfile with separate AMD/Intel vs NVIDIA run commands and documented the nvidia-container-toolkit + CDI spec prerequisites. Verified viewer runs correctly with `--device nvidia.com/gpu=all` when toolkit is installed.

## Open Issue: F# REPL Assembly Mismatch

### Symptom

All 24 F# demo scripts and the interactive REPL (`PhysicsClient.fsx`) fail with:
```
Could not load file or assembly 'Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0,
Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.
```

The error occurs when `GrpcChannel.ForAddress()` is called — the `Grpc.Net.Client` assembly internally references `Microsoft.Extensions.Logging.Abstractions` assembly version 8.0.0.0, but only 10.x is available. FSI does not apply binding redirects like compiled apps do.

### Root Cause: FSI Regression in SDK 10.0.201

Narrowed down through controlled comparison between the emacs-dev container (Arch Linux, SDK 10.0.104) and the PhysicsSandbox container (Microsoft image, SDK 10.0.201):

| | emacs-dev | PhysicsSandbox |
|---|---|---|
| Base image | Arch Linux | `mcr.microsoft.com/dotnet/sdk:10.0` (Ubuntu) |
| .NET SDK | 10.0.104 | 10.0.201 |
| FSI version | 14.0.104.0 | 15.2.201.0 |
| `#r "nuget: Grpc.Net.Client"` then `GrpcChannel.ForAddress()` | Works | Works |
| `#r "nuget: Grpc.AspNetCore"` then `GrpcChannel.ForAddress()` | Works | **Fails** |

The trigger is `Grpc.AspNetCore` — pulled in transitively by `PhysicsSandbox.Shared.Contracts`. When FSI 14.0.104.0 (SDK 10.0.104) resolves `Grpc.AspNetCore` and its dependency chain, it correctly rolls forward the 8.0.0.0 assembly reference to 10.x. FSI 15.2.201.0 (SDK 10.0.201) does not.

Bare `Grpc.Net.Client` (without `Grpc.AspNetCore`) works in both SDKs. The difference is that `Grpc.AspNetCore` brings in additional ASP.NET Core server dependencies that trigger a different assembly loading code path in the newer FSI.

### Tested Workarounds (All Failed)

- Explicit `#r "nuget: Microsoft.Extensions.Logging.Abstractions"` before/after other packages
- `AppDomain.CurrentDomain.add_AssemblyResolve` handler to redirect 8.0.0.0 → loaded version
- `AssemblyLoadContext.Default.add_Resolving` handler
- Adding explicit `Microsoft.Extensions.Logging.Abstractions 10.*` PackageReference in PhysicsClient.fsproj (commit aa9107f — does not fix the container issue)

### Recommended Fixes

**Option A — Pin SDK version** in Containerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0.104
```
Simplest fix. Locks to the known-working SDK. Downside: misses future SDK patches.

**Option B — Replace `Grpc.AspNetCore` in Shared.Contracts:**
The `Shared.Contracts` project only needs gRPC codegen and client stubs. It should not depend on `Grpc.AspNetCore` (server hosting). Replace with `Grpc.Net.Client` + `Grpc.Tools` + `Google.Protobuf`. The server projects that actually host gRPC services can reference `Grpc.AspNetCore` directly. This is the architecturally correct fix and would also work with future SDK versions.

## Test Results

### Python Demos (21/21 pass)

| Demo | Result |
|---|---|
| demo01_hello_drop | Pass |
| demo02_bouncing_marbles | Pass |
| demo03_crate_stack | Pass |
| demo04_bowling_alley | Pass |
| demo05_marble_rain | Pass |
| demo06_domino_row | Pass |
| demo07_spinning_tops | Pass |
| demo08_gravity_flip | Pass |
| demo09_billiards | Pass |
| demo10_chaos | Pass |
| demo11_body_scaling | Pass |
| demo12_collision_pit | Pass |
| demo13_force_frenzy | Pass |
| demo14_domino_cascade | Pass |
| demo15_overload | Pass |
| demo19_shape_gallery | Pass |
| demo20_compound_constructions | Pass |
| demo21_mesh_hull_playground | Pass |
| demo22_camera_showcase | Pass |
| demo23_ball_rollercoaster | Pass |
| demo24_halfpipe_arena | Pass |

### F# Demos (0/24 pass)

All fail with the `Microsoft.Extensions.Logging.Abstractions 8.0.0.0` assembly error described above.

### MCP

- SSE endpoint at `http://localhost:5199/sse` connects and returns session IDs
- `tools/list` returns 59 tools (source defines 60 `[<McpServerTool>]` attributes — one tool may have a registration issue)

### Services

All 6 Aspire-managed services start successfully with NVIDIA GPU:
- PhysicsSandbox.AppHost (orchestrator)
- PhysicsServer (gRPC hub, port 5180)
- PhysicsSimulation (BepuPhysics2)
- PhysicsViewer (Stride3D, requires GPU)
- PhysicsClient (Spectre.Console)
- PhysicsSandbox.Mcp (MCP server, port 5199)

Aspire dashboard accessible at `http://localhost:8081`.

## Minor Issues

- **MCP tool count:** 59 at runtime vs 60 in source/README.
- **Silent viewer crash:** When the viewer fails (wrong GPU flag, missing drivers), no error appears in container stdout. Crash details are only in Aspire's temp files at `/tmp/aspire-*/viewer-*_err_*`.
- **`git clone` caching:** The Containerfile clones from GitHub; Podman caches this layer. Rebuilds without `--no-cache` use stale code.
- **`COPY entrypoint.sh` after `git clone`:** Redundant — the file is already in the cloned repo. The COPY pulls from the local build context, which may differ from the cloned code.
- **Image size:** 4.27 GB. A multi-stage build could reduce this.
