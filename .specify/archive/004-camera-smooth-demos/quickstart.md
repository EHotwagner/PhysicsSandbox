# Quickstart: 004-camera-smooth-demos

## Build & Run

```bash
# Build everything
dotnet build PhysicsSandbox.slnx

# Run the full system (server + simulation + viewer + client + mcp)
./start.sh

# Run tests (headless)
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

## Key Files to Modify

### Proto Contract (change first)
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — Add 9 new ViewCommand variants (fields 5-13)

### Viewer (core camera logic)
- `src/PhysicsViewer/Rendering/CameraController.fs` + `.fsi` — Extend CameraState with ActiveMode, add interpolation/body-relative update functions
- `src/PhysicsViewer/Rendering/SceneManager.fs` + `.fsi` — Add NarrationText to SceneState, add applyNarration function
- `src/PhysicsViewer/Program.fs` — Add ViewCommand case handlers, build body position map, call camera mode update each frame, render narration label

### Client Library
- `src/PhysicsClient/Commands/ViewCommands.fs` + `.fsi` — Add 10 new client functions (smoothCamera, cameraLookAt, etc.)

### Scripting Helpers
- `Scripting/demos/Prelude.fsx` — Add F# wrapper functions
- `Scripting/demos_py/prelude.py` — Add Python wrapper functions

### Demo Scripts (42 files)
- `Scripting/demos/Demo01_HelloDrop.fsx` through `Demo21_MeshHullPlayground.fsx`
- `Scripting/demos_py/demo01_hello_drop.py` through `demo21_mesh_hull_playground.py`
- Plus new camera showcase demo (Demo22)

### Tests
- `tests/PhysicsViewer.Tests/` — CameraController interpolation unit tests
- `tests/PhysicsClient.Tests/` — ViewCommand builder tests
- `tests/PhysicsSandbox.Integration.Tests/` — End-to-end camera command tests

## Verification

```bash
# Run a single demo to verify camera works
cd Scripting/demos && dotnet fsi Demo01_HelloDrop.fsx

# Run the camera showcase demo
cd Scripting/demos && dotnet fsi Demo22_CameraShowcase.fsx

# Run all tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

## Architecture Notes

- **No server changes needed** — ViewCommands pass through the server untouched via bounded channel
- **Body positions available in viewer** — `latestSimState.Bodies` provides ID → position for all bodies
- **Fire-and-forget pattern** — Scripts send camera commands and `sleep` for duration; viewer handles interpolation independently
- **Mode cancellation** — Any new camera command, mouse input, or CameraStop cancels the active mode
