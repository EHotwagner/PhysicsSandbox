# Data Model: Codebase Cleanup and Refactoring

**Date**: 2026-03-25
**Feature**: 004-codebase-cleanup-refactor

## Overview

This is a structural refactoring feature — no new data entities are introduced. The data model documents the **module relationships and dependency constraints** that govern where consolidated code can live.

## Project Dependency Graph (Current)

```
PhysicsSandbox.Shared.Contracts (C#, proto types)
├── PhysicsServer (F#) ← references Contracts
├── PhysicsSimulation (F#) ← references Contracts, ServiceDefaults
├── PhysicsClient (F#) ← references Contracts, ServiceDefaults
├── PhysicsViewer (F#) ← references Contracts, ServiceDefaults
├── PhysicsSandbox.Scripting (F#) ← references PhysicsClient
└── PhysicsSandbox.Mcp (F#) ← references Contracts, Scripting (NuGet)
```

## Consolidation Location Decisions

| Duplicated Code | Canonical Location | Consumers | Rationale |
|---|---|---|---|
| MeshResolver (Client + MCP) | PhysicsClient.MeshResolver | PhysicsClient, MCP (via Scripting) | MCP already transitively depends on PhysicsClient |
| MeshResolver (Viewer) | PhysicsViewer.Streaming.MeshResolver | PhysicsViewer only | Async + Pending tracking; Viewer has no PhysicsClient dependency |
| toVec3 (tuple→proto) | PhysicsSandbox.Scripting.Vec3Builders | SimulationCommands, ClientAdapter | Scripting → PhysicsClient chain exists |
| Vec3↔Vector3 conversions | PhysicsSimulation.ProtoConversions (new module) | SimulationWorld, QueryHandler | Internal to PhysicsSimulation project |
| Proto→Stride vectors | PhysicsViewer.Rendering.ProtoConversions (new module) | CameraController, DebugRenderer, SceneManager | Internal to PhysicsViewer project |
| Shape construction helpers | PhysicsClient.ShapeBuilders (new module) | SimulationCommands, ClientAdapter, SimulationTools | Central to client-side shape building |
| ID generation | PhysicsClient.IdGenerator (existing) | SimulationCommands, SimulationTools, GeneratorTools | MCP transitively depends on PhysicsClient |
| gRPC channel creation | IntegrationTestHelpers.CreateGrpcChannel (extract) | All integration test helpers | Single file, private method extraction |

## New Modules Created

| Module | Project | .fsi Required | Purpose |
|---|---|---|---|
| ProtoConversions | PhysicsSimulation | Yes | Vec3↔Vector3, Vec4↔Quaternion, proto builders |
| ShapeConversion | PhysicsSimulation | Yes | convertShape, convertConstraintType |
| ShapeBuilders | PhysicsClient | Yes | mkSphere, mkBox, mkCapsule, etc. |
| ProtoConversions | PhysicsViewer | Yes | protoVec3ToStride, protoQuatToStride |

## Modules Deleted

| Module | Project | Replaced By |
|---|---|---|
| MeshResolver | PhysicsSandbox.Mcp | PhysicsClient.MeshResolver |

## State Transitions

N/A — no runtime state changes. All changes are compile-time module reorganization.
