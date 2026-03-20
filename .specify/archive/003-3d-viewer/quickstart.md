# Quickstart: 003-3d-viewer

## Build & Run

```bash
# Build everything
dotnet build PhysicsSandbox.slnx

# Run (starts Aspire dashboard + server + simulation + viewer)
dotnet run --project src/PhysicsSandbox.AppHost

# Run tests (no GPU required for unit tests)
dotnet test PhysicsSandbox.slnx
```

## Stride3D Build Note

Stride3D projects require `-p:StrideCompilerSkipBuild=true` when building without GPU/display (CI, headless). The viewer's `.fsproj` should set `<StrideCompileAssets>false</StrideCompileAssets>` conditionally for test/CI builds.

The GLSL shader compiler binary must be available at `linux-x64/glslangValidator.bin` relative to the working directory. Add an MSBuild target to copy it from the NuGet cache.

## Project Dependencies

```
PhysicsViewer.fsproj
  ├── PackageReference: Stride.CommunityToolkit (1.0.0-preview.62)
  ├── PackageReference: Stride.CommunityToolkit.Bepu (1.0.0-preview.62)
  ├── PackageReference: Stride.CommunityToolkit.Skyboxes (1.0.0-preview.62)
  ├── PackageReference: Stride.CommunityToolkit.Linux (1.0.0-preview.62)
  ├── PackageReference: Grpc.Net.Client (2.*)
  ├── ProjectReference: PhysicsSandbox.Shared.Contracts
  └── ProjectReference: PhysicsSandbox.ServiceDefaults
```

## Key Skills for Implementation

- `/stride3d-fsharp` (stride3d-fsharp skill) — scene setup, entity creation, input handling, F# interop patterns
- `/fsgrpc-proto` — extending physics_hub.proto with StreamViewCommands RPC
- `/fsgrpc-client` — gRPC streaming client (server streaming pattern for StreamState and StreamViewCommands)

## Key Files

| File | Purpose |
|------|---------|
| `src/PhysicsViewer/Rendering/SceneManager.fsi/.fs` | SimulationState → Stride entities |
| `src/PhysicsViewer/Rendering/CameraController.fsi/.fs` | Camera state + input + commands |
| `src/PhysicsViewer/Streaming/ViewerClient.fsi/.fs` | gRPC streaming client |
| `src/PhysicsViewer/Program.fs` | Entry point: host + game loop |
| `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` | Extended with StreamViewCommands |
| `src/PhysicsServer/Hub/MessageRouter.fsi/.fs` | Extended with readViewCommand |
| `src/PhysicsServer/Services/PhysicsHubService.fsi/.fs` | Extended with StreamViewCommands |

## Architecture Notes

- **Threading**: Stride3D `Game.Run()` blocks the main thread. gRPC streams run on background tasks. `ConcurrentQueue<T>` bridges the two.
- **Graphics API**: OpenGL (`<StrideGraphicsApi>OpenGL</StrideGraphicsApi>`) for container/GPU-passthrough compatibility.
- **Body colors**: Spheres = blue, Boxes = orange, Unknown = red (per spec clarification).
- **Ground grid**: Stride's `Add3DGround()` provides the Y=0 reference surface; supplement with `AddGroundGizmo()` for grid lines.
- **Camera**: Stride's `Add3DCameraController()` provides built-in mouse/keyboard orbit control. REPL commands override by setting `Entity.Transform.Position` and computing look-at rotation.
