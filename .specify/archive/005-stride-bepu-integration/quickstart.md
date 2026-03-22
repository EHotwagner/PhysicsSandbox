# Quickstart: Stride BepuPhysics Integration

**Branch**: `005-stride-bepu-integration`

## What's Changing

This feature extends the physics sandbox from 3 shape types to 10, adds 10 constraint types, per-body color/material/collision filtering, kinematic bodies, physics queries (raycast/sweep/overlap), and debug wireframe visualization. Changes span BepuFSharp wrapper, proto contracts, simulation, server, viewer, and all client interfaces.

## Development Order

### Phase 0: External Dependency (BPEWrapper project — separate feature)
0. Complete BepuFSharp 0.2.0 in `~/projects/BPEWrapper/` (sweep cast, overlap, constraint readback, filtered raycasting, runtime modification, Stride interop). Spec at `~/projects/BPEWrapper/stride-bepu-integration-spec.md`. Publish to local NuGet feed.

### Phase 1: Contracts
1. Extend `physics_hub.proto` with all new messages (shapes, constraints, material, color, queries, etc.)
2. Update BepuFSharp PackageReference to 0.2.0 in PhysicsSimulation

### Phase 2: Simulation Core
4. Update `SimulationWorld` to handle new shapes (capsule, cylinder, triangle, convex hull, compound, mesh)
5. Add shape registration/caching (`RegisterShape`/`UnregisterShape` commands)
6. Add constraint handling (`AddConstraint`/`RemoveConstraint`, auto-cleanup on body removal)
7. Add material properties, color, collision filter to `AddBody` and `BodyRecord`
8. Add kinematic body support (`BodyMotionType` dispatch)
9. Serialize constraint state and registered shapes in `SimulationState`

### Phase 3: Server & Queries
10. Route new commands through `MessageRouter`
11. Add query routing mechanism (server → simulation request-response)
12. Implement `Raycast`, `SweepCast`, `Overlap` RPCs + batch variants on `PhysicsHub`
13. Handle `SetCollisionFilter` command routing

### Phase 4: Viewer
14. Render new shape types (capsule, cylinder, triangle, convex hull, compound, mesh geometry)
15. Apply per-body color from state stream
16. Implement debug wireframe overlay using Stride's `WireFrameRenderObject` + `SinglePassWireframeRenderFeature`
17. Render constraint connections in debug mode

### Phase 5: Client Interfaces
18. Extend PhysicsClient REPL with new commands
19. Extend MCP server with new tools
20. Extend Scripting library with new helpers
21. Update demos and scripts

## Build & Test

```bash
# Build everything
dotnet build PhysicsSandbox.slnx

# Build headless (skip Stride asset compiler)
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run the system
./start.sh
```

## Key Files to Touch

| Area | Key Files |
|------|-----------|
| Proto | `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` |
| Simulation | `src/PhysicsSimulation/SimulationWorld.fs`, `CommandHandler.fs` |
| Server | `src/PhysicsServer/Services/PhysicsHubService.fs`, `MessageRouter.fs` |
| Viewer | `src/PhysicsViewer/Rendering/SceneManager.fs` |
| Client | `src/PhysicsClient/Commands/`, `src/PhysicsSandbox.Mcp/Tools/` |
| Scripting | `src/PhysicsSandbox.Scripting/` |
| BepuFSharp | External dependency — separate feature in `~/projects/BPEWrapper/` (0.1.0 → 0.2.0) |

## Proto Regeneration

After editing `physics_hub.proto`:
```bash
dotnet build src/PhysicsSandbox.Shared.Contracts/
```
This auto-generates C# types via `Grpc.Tools`. All consuming projects pick up changes transitively.
