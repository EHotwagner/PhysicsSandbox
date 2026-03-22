module PhysicsViewer.ShapeGeometry

open Stride.CommunityToolkit.Rendering.ProceduralModels
open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

type StrideColor = Stride.Core.Mathematics.Color

/// Determine the Stride PrimitiveModelType for a given proto Shape.
let primitiveType (shape: Shape) =
    if isNull shape then PrimitiveModelType.Sphere
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere -> PrimitiveModelType.Sphere
        | Shape.ShapeOneofCase.Box -> PrimitiveModelType.Cube
        | Shape.ShapeOneofCase.Plane -> PrimitiveModelType.Cube
        | Shape.ShapeOneofCase.Capsule -> PrimitiveModelType.Capsule
        | Shape.ShapeOneofCase.Cylinder -> PrimitiveModelType.Cylinder
        | Shape.ShapeOneofCase.Triangle -> PrimitiveModelType.Cube // bounding box approximation
        | Shape.ShapeOneofCase.ConvexHull -> PrimitiveModelType.Cube // bounding box from points
        | Shape.ShapeOneofCase.Compound -> PrimitiveModelType.Cube // bounding box of children
        | Shape.ShapeOneofCase.Mesh -> PrimitiveModelType.Cube // bounding box of triangles
        | Shape.ShapeOneofCase.ShapeRef -> PrimitiveModelType.Sphere // resolved at render time
        | _ -> PrimitiveModelType.Sphere

/// Compute the visual size for a primitive based on the physics shape dimensions.
let shapeSize (shape: Shape) =
    if isNull shape then System.Nullable<Vector3>()
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere ->
            let d = float32 shape.Sphere.Radius * 2.0f
            System.Nullable(Vector3(d, d, d))
        | Shape.ShapeOneofCase.Box ->
            let he = shape.Box.HalfExtents
            if isNull he then System.Nullable<Vector3>()
            else System.Nullable(Vector3(float32 he.X * 2.0f, float32 he.Y * 2.0f, float32 he.Z * 2.0f))
        | Shape.ShapeOneofCase.Plane ->
            // Approximate plane as large thin box
            System.Nullable(Vector3(1000.0f, 0.1f, 1000.0f))
        | Shape.ShapeOneofCase.Capsule ->
            let r = float32 shape.Capsule.Radius
            let l = float32 shape.Capsule.Length
            // Capsule primitive: diameter = Size.X, total height = Size.Y
            System.Nullable(Vector3(r * 2.0f, l + r * 2.0f, r * 2.0f))
        | Shape.ShapeOneofCase.Cylinder ->
            let r = float32 shape.Cylinder.Radius
            let l = float32 shape.Cylinder.Length
            System.Nullable(Vector3(r * 2.0f, l, r * 2.0f))
        | Shape.ShapeOneofCase.Triangle ->
            // Bounding box of the 3 triangle vertices
            let a = shape.Triangle.A
            let b = shape.Triangle.B
            let c = shape.Triangle.C
            if isNull a || isNull b || isNull c then System.Nullable(Vector3(0.5f, 0.5f, 0.5f))
            else
                let minX = min (min a.X b.X) c.X
                let minY = min (min a.Y b.Y) c.Y
                let minZ = min (min a.Z b.Z) c.Z
                let maxX = max (max a.X b.X) c.X
                let maxY = max (max a.Y b.Y) c.Y
                let maxZ = max (max a.Z b.Z) c.Z
                let sx = max (float32 (maxX - minX)) 0.01f
                let sy = max (float32 (maxY - minY)) 0.01f
                let sz = max (float32 (maxZ - minZ)) 0.01f
                System.Nullable(Vector3(sx, sy, sz))
        | Shape.ShapeOneofCase.ConvexHull ->
            // Bounding box from convex hull points
            let points = shape.ConvexHull.Points
            if points.Count = 0 then System.Nullable<Vector3>()
            else
                let mutable minX = System.Double.MaxValue
                let mutable minY = System.Double.MaxValue
                let mutable minZ = System.Double.MaxValue
                let mutable maxX = System.Double.MinValue
                let mutable maxY = System.Double.MinValue
                let mutable maxZ = System.Double.MinValue
                for p in points do
                    if p.X < minX then minX <- p.X
                    if p.Y < minY then minY <- p.Y
                    if p.Z < minZ then minZ <- p.Z
                    if p.X > maxX then maxX <- p.X
                    if p.Y > maxY then maxY <- p.Y
                    if p.Z > maxZ then maxZ <- p.Z
                System.Nullable(Vector3(float32 (maxX - minX), float32 (maxY - minY), float32 (maxZ - minZ)))
        | Shape.ShapeOneofCase.Compound ->
            // Bounding box estimated from child shape offsets
            let children = shape.Compound.Children
            if children.Count = 0 then System.Nullable(Vector3(1.0f, 1.0f, 1.0f))
            else
                let mutable minX = System.Double.MaxValue
                let mutable minY = System.Double.MaxValue
                let mutable minZ = System.Double.MaxValue
                let mutable maxX = System.Double.MinValue
                let mutable maxY = System.Double.MinValue
                let mutable maxZ = System.Double.MinValue
                for child in children do
                    let p = if isNull child.LocalPosition then Vec3() else child.LocalPosition
                    let offset = 0.5 // assume ~0.5m child radius
                    if p.X - offset < minX then minX <- p.X - offset
                    if p.Y - offset < minY then minY <- p.Y - offset
                    if p.Z - offset < minZ then minZ <- p.Z - offset
                    if p.X + offset > maxX then maxX <- p.X + offset
                    if p.Y + offset > maxY then maxY <- p.Y + offset
                    if p.Z + offset > maxZ then maxZ <- p.Z + offset
                let sx = max (float32 (maxX - minX)) 0.1f
                let sy = max (float32 (maxY - minY)) 0.1f
                let sz = max (float32 (maxZ - minZ)) 0.1f
                System.Nullable(Vector3(sx, sy, sz))
        | Shape.ShapeOneofCase.Mesh ->
            // Bounding box from mesh triangle vertices
            let triangles = shape.Mesh.Triangles
            if triangles.Count = 0 then System.Nullable<Vector3>()
            else
                let mutable minX = System.Double.MaxValue
                let mutable minY = System.Double.MaxValue
                let mutable minZ = System.Double.MaxValue
                let mutable maxX = System.Double.MinValue
                let mutable maxY = System.Double.MinValue
                let mutable maxZ = System.Double.MinValue
                for tri in triangles do
                    for v in [tri.A; tri.B; tri.C] do
                        if not (isNull v) then
                            if v.X < minX then minX <- v.X
                            if v.Y < minY then minY <- v.Y
                            if v.Z < minZ then minZ <- v.Z
                            if v.X > maxX then maxX <- v.X
                            if v.Y > maxY then maxY <- v.Y
                            if v.Z > maxZ then maxZ <- v.Z
                let sx = max (float32 (maxX - minX)) 0.01f
                let sy = max (float32 (maxY - minY)) 0.01f
                let sz = max (float32 (maxZ - minZ)) 0.01f
                System.Nullable(Vector3(sx, sy, sz))
        | _ -> System.Nullable<Vector3>()

/// Default color palette by shape type.
let defaultColor (shape: Shape) : StrideColor =
    if isNull shape then StrideColor.Red
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere -> StrideColor(64uy, 128uy, 255uy, 255uy)
        | Shape.ShapeOneofCase.Box -> StrideColor.Orange
        | Shape.ShapeOneofCase.Plane -> StrideColor.Gray
        | Shape.ShapeOneofCase.Capsule -> StrideColor.Green
        | Shape.ShapeOneofCase.Cylinder -> StrideColor.Yellow
        | Shape.ShapeOneofCase.Triangle -> StrideColor.Cyan
        | Shape.ShapeOneofCase.ConvexHull -> StrideColor.Purple
        | Shape.ShapeOneofCase.Compound -> StrideColor.White
        | Shape.ShapeOneofCase.Mesh -> StrideColor(0uy, 128uy, 128uy, 255uy)
        | _ -> StrideColor.Red
