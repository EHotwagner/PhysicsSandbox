# Quickstart: Proper Shape Rendering

**Branch**: `004-proper-shape-rendering`

## What This Feature Does

Replaces bounding-box approximations for 5 shape types (Triangle, Mesh, ConvexHull, Compound, CachedRef/ShapeRef) with accurate geometry rendering in the 3D viewer. After this feature, all 10 physics shape types render with geometry matching their actual collision boundaries.

## Key Files to Modify

| File | Change |
|------|--------|
| `src/PhysicsViewer/Rendering/ShapeGeometry.fs(i)` | Add `buildCustomMesh`, `buildTriangleMesh`, `buildMeshMesh`, `buildConvexHullMesh` functions |
| `src/PhysicsViewer/Rendering/SceneManager.fs(i)` | Branch `createEntity` for custom shapes; add compound child entity creation |
| `src/PhysicsViewer/Rendering/DebugRenderer.fs(i)` | Branch wireframe creation for custom shape edge rendering |
| `src/PhysicsViewer/PhysicsViewer.fsproj` | Add MIConvexHull NuGet dependency |

## Build & Test

```bash
# Build
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run viewer with server
dotnet run --project src/PhysicsSandbox.AppHost --launch-profile http

# Run tests
dotnet test tests/PhysicsViewer.Tests -p:StrideCompilerSkipBuild=true

# Run demos (visual validation)
dotnet fsi Scripting/demos/AutoRun.fsx http://localhost:5180
```

## Architecture Overview

```
Shape arrives via proto
  ├─ Primitive? → Create3DPrimitive (unchanged)
  ├─ Triangle/Mesh/ConvexHull? → buildCustomMesh → MeshDraw → Model → Entity
  ├─ Compound? → iterate children, render each recursively
  ├─ CachedRef? → MeshResolver.resolve → re-dispatch
  └─ ShapeRef? → RegisteredShapes lookup → re-dispatch
```

## New Dependency

- **MIConvexHull** (NuGet): Pure C# QuickHull for convex hull face computation. No transitive dependencies.
