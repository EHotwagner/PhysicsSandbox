# Implementation Plan: Mesh Cache and On-Demand Transport

**Branch**: `004-mesh-cache-transport` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-mesh-cache-transport/spec.md`

## Summary

Replace inline mesh geometry in state updates with content-addressed identifiers and bounding box extents. Complex shapes (ConvexHull, MeshShape, Compound) are transmitted once, cached by the server, and fetched on demand by subscribers. Primitives (Sphere, Box, Capsule, Cylinder, Plane, Triangle) continue inline. A new `FetchMeshes` RPC provides a dedicated channel for on-demand mesh retrieval separate from the state stream.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, MCP, client, viewer), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console
**Storage**: In-memory (physics world, mesh caches). Append-only protobuf binary files for MCP recordings.
**Testing**: xUnit 2.x (F# unit tests), Aspire.Hosting.Testing 10.x (C# integration tests), surface area baselines
**Target Platform**: Linux with GPU (container)
**Project Type**: Distributed simulation system (Aspire-orchestrated microservices)
**Performance Goals**: 60 Hz state streaming, ≥80% bandwidth reduction for mesh-heavy scenes, <5% jitter during mesh transfers
**Constraints**: Coordinated upgrade (no backward compat). Content-addressed mesh IDs. Server is single cache authority.
**Scale/Scope**: Typical scenes have 10-100 bodies, 0-20 of which use complex shapes. Single simulation, 1-3 subscribers.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | ✅ Pass | Mesh cache is server-internal. Communication via gRPC only. No shared mutable state across services. |
| II. Contract-First | ✅ Pass | Proto changes (new messages, new RPC) defined before implementation. Contracts in `PhysicsSandbox.Shared.Contracts`. |
| III. Shared Nothing | ✅ Pass | Only shared artifact is proto contracts. Each service maintains its own local mesh cache. |
| IV. Spec-First | ✅ Pass | Feature spec complete with clarifications. This plan precedes implementation. |
| V. Compiler-Enforced | ⚠️ Requires | New/modified F# modules need `.fsi` signature files and surface area baseline updates. |
| VI. Test Evidence | ⚠️ Requires | Unit tests for mesh ID generation, cache logic, proto changes. Integration tests for end-to-end mesh caching flow. |
| VII. Observability | ⚠️ Requires | Mesh cache hit/miss counts and mesh transfer timing should be logged via existing structured diagnostics. |

**Gate result: PASS** — no violations. Items marked ⚠️ are implementation requirements, not blockers.

## Project Structure

### Documentation (this feature)

```text
specs/004-mesh-cache-transport/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
  PhysicsSandbox.Shared.Contracts/
    Protos/physics_hub.proto          # MODIFY: new messages + RPC
  PhysicsSimulation/
    World/SimulationWorld.fs          # MODIFY: mesh ID generation, CachedShapeRef emission
    World/SimulationWorld.fsi         # MODIFY: updated signatures
    World/MeshIdGenerator.fs          # NEW: content-hash mesh ID computation
    World/MeshIdGenerator.fsi         # NEW: signature file
  PhysicsServer/
    Hub/MeshCache.fs                  # NEW: server-side mesh geometry cache
    Hub/MeshCache.fsi                 # NEW: signature file
    Hub/MessageRouter.fs              # MODIFY: intercept new_meshes, populate cache
    Services/PhysicsHubService.fs     # MODIFY: implement FetchMeshes RPC
  PhysicsViewer/
    Streaming/MeshResolver.fs         # NEW: local cache + on-demand fetch
    Streaming/MeshResolver.fsi        # NEW: signature file
    Rendering/ShapeGeometry.fs        # MODIFY: handle CachedShapeRef → placeholder
    Rendering/SceneManager.fs         # MODIFY: integrate mesh resolution
  PhysicsClient/
    Connection/MeshResolver.fs        # NEW: local cache + fetch for text display
    Connection/MeshResolver.fsi       # NEW: signature file
    Display/StateDisplay.fs           # MODIFY: resolve CachedShapeRef for descriptions
  PhysicsSandbox.Mcp/
    MeshResolver.fs                   # NEW: local cache + fetch for recording/tools
    MeshResolver.fsi                  # NEW: signature file
    Recording/RecordingEngine.fs      # MODIFY: resolve meshes before recording

tests/
  PhysicsSimulation.Tests/
    MeshIdGeneratorTests.fs           # NEW: hash determinism, collision avoidance
  PhysicsServer.Tests/
    MeshCacheTests.fs                 # NEW: cache store/retrieve/invalidate
  PhysicsViewer.Tests/
    MeshResolverTests.fs              # NEW: cache hits, placeholder fallback
    ShapeGeometryTests.fs             # MODIFY: CachedShapeRef handling
  PhysicsClient.Tests/
    MeshResolverTests.fs              # NEW: cache + display integration
  PhysicsSandbox.Integration.Tests/
    MeshCacheIntegrationTests.cs      # NEW: end-to-end caching flow
```

**Structure Decision**: Extends existing multi-project Aspire solution. Each service gets a `MeshResolver` module (local cache + fetch client). Server gets `MeshCache` module (authoritative cache). Simulation gets `MeshIdGenerator` module (content hashing). No new projects — all changes fit within existing service boundaries.

## Complexity Tracking

No violations to justify. All changes fit within existing project structure.
