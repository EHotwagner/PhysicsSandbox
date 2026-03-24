module PhysicsViewer.ShapeGeometry

open Stride.CommunityToolkit.Rendering.ProceduralModels
open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

/// Custom mesh data for non-primitive shapes. Uses plain arrays for Stride-agnostic geometry.
type CustomMeshData =
    { Positions: Vector3 array
      Normals: Vector3 array
      Color: Stride.Core.Mathematics.Color
      Indices: int array
      WireframePositions: Vector3 array
      WireframeIndices: int array }

/// Determine the Stride PrimitiveModelType for a given proto Shape.
val primitiveType: Shape -> PrimitiveModelType

/// Compute the visual size for a primitive based on the physics shape dimensions.
val shapeSize: Shape -> System.Nullable<Vector3>

/// Default color palette by shape type.
val defaultColor: Shape -> Stride.Core.Mathematics.Color

/// Returns true for shapes that need custom mesh rendering (Triangle, Mesh, ConvexHull).
val isCustomShape: Shape -> bool

/// Build custom mesh data for a Triangle shape.
val buildTriangleMesh: Shape -> Stride.Core.Mathematics.Color -> CustomMeshData option

/// Build custom mesh data for a Mesh shape.
val buildMeshMesh: Shape -> Stride.Core.Mathematics.Color -> CustomMeshData option

/// Build custom mesh data for a ConvexHull shape.
val buildConvexHullMesh: Shape -> Stride.Core.Mathematics.Color -> CustomMeshData option

/// Dispatcher: build custom mesh data for any custom shape. Returns None for primitives or degenerate shapes.
val buildCustomMesh: Shape -> Stride.Core.Mathematics.Color -> CustomMeshData option
