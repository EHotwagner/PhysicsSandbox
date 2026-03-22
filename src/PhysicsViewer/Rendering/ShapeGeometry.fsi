module PhysicsViewer.ShapeGeometry

open Stride.CommunityToolkit.Rendering.ProceduralModels
open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

/// Determine the Stride PrimitiveModelType for a given proto Shape.
val primitiveType: Shape -> PrimitiveModelType

/// Compute the visual size for a primitive based on the physics shape dimensions.
val shapeSize: Shape -> System.Nullable<Vector3>

/// Default color palette by shape type.
val defaultColor: Shape -> Stride.Core.Mathematics.Color
