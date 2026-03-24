module PhysicsViewer.ShapeGeometry

open Stride.CommunityToolkit.Rendering.ProceduralModels
open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

type StrideColor = Stride.Core.Mathematics.Color

/// Custom mesh data for non-primitive shapes.
type CustomMeshData =
    { Positions: Vector3 array
      Normals: Vector3 array
      Color: StrideColor
      Indices: int array
      WireframePositions: Vector3 array
      WireframeIndices: int array }

/// Compute face normal from three vertices via cross product.
let private computeFaceNormal (a: Vector3) (b: Vector3) (c: Vector3) =
    let mutable ab = Vector3.Zero
    let mutable ac = Vector3.Zero
    Vector3.Subtract(&b, &a, &ab)
    Vector3.Subtract(&c, &a, &ac)
    let mutable n = Vector3.Zero
    Vector3.Cross(&ab, &ac, &n)
    let len = n.Length()
    if len < 1.0e-8f then Vector3.UnitY
    else
        Vector3.Multiply(&n, 1.0f / len, &n)
        n

let private vec3FromProto (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

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
        | Shape.ShapeOneofCase.Triangle -> PrimitiveModelType.Sphere // fallback for degenerate
        | Shape.ShapeOneofCase.ConvexHull -> PrimitiveModelType.Sphere
        | Shape.ShapeOneofCase.Compound -> PrimitiveModelType.Cube
        | Shape.ShapeOneofCase.Mesh -> PrimitiveModelType.Sphere
        | Shape.ShapeOneofCase.ShapeRef -> PrimitiveModelType.Sphere
        | Shape.ShapeOneofCase.CachedRef -> PrimitiveModelType.Cube
        | _ -> PrimitiveModelType.Sphere

/// Clamp a dimension to a visible minimum.
let private clampMin (v: float32) = max v 0.01f

/// Compute the visual size for a primitive based on the physics shape dimensions.
let shapeSize (shape: Shape) =
    if isNull shape then System.Nullable<Vector3>()
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere ->
            let r = clampMin (float32 shape.Sphere.Radius)
            System.Nullable(Vector3(r, r, r))
        | Shape.ShapeOneofCase.Box ->
            let he = shape.Box.HalfExtents
            if isNull he then System.Nullable<Vector3>()
            else System.Nullable(Vector3(float32 he.X * 2.0f, float32 he.Y * 2.0f, float32 he.Z * 2.0f))
        | Shape.ShapeOneofCase.Plane ->
            System.Nullable(Vector3(1000.0f, 0.1f, 1000.0f))
        | Shape.ShapeOneofCase.Capsule ->
            let r = float32 shape.Capsule.Radius
            let l = float32 shape.Capsule.Length
            System.Nullable(Vector3(r, l, r))
        | Shape.ShapeOneofCase.Cylinder ->
            let r = float32 shape.Cylinder.Radius
            let l = float32 shape.Cylinder.Length
            System.Nullable(Vector3(r, l, r))
        | Shape.ShapeOneofCase.Triangle ->
            System.Nullable(Vector3(0.1f, 0.1f, 0.1f))
        | Shape.ShapeOneofCase.ConvexHull ->
            System.Nullable(Vector3(0.1f, 0.1f, 0.1f))
        | Shape.ShapeOneofCase.Compound ->
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
                    let offset = 0.5
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
            System.Nullable(Vector3(0.1f, 0.1f, 0.1f))
        | Shape.ShapeOneofCase.CachedRef ->
            let bMin = shape.CachedRef.BboxMin
            let bMax = shape.CachedRef.BboxMax
            if isNull bMin || isNull bMax then System.Nullable(Vector3(1.0f, 1.0f, 1.0f))
            else
                let sx = max (float32 (bMax.X - bMin.X)) 0.01f
                let sy = max (float32 (bMax.Y - bMin.Y)) 0.01f
                let sz = max (float32 (bMax.Z - bMin.Z)) 0.01f
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
        | Shape.ShapeOneofCase.CachedRef -> StrideColor(255uy, 0uy, 255uy, 128uy)
        | _ -> StrideColor.Red

/// Returns true for shapes that need custom mesh rendering.
let isCustomShape (shape: Shape) =
    if isNull shape then false
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Triangle
        | Shape.ShapeOneofCase.Mesh
        | Shape.ShapeOneofCase.ConvexHull -> true
        | _ -> false

/// Helper: add double-sided triangle to position/normal lists.
let private addDoubleSidedTriangle (positions: System.Collections.Generic.List<Vector3>) (normals: System.Collections.Generic.List<Vector3>) (a: Vector3) (b: Vector3) (c: Vector3) =
    let n = computeFaceNormal a b c
    // Front face
    positions.Add(a); positions.Add(b); positions.Add(c)
    normals.Add(n); normals.Add(n); normals.Add(n)
    // Back face (reversed winding)
    let mutable negN = Vector3.Zero
    Vector3.Negate(&n, &negN)
    positions.Add(a); positions.Add(c); positions.Add(b)
    normals.Add(negN); normals.Add(negN); normals.Add(negN)

/// Helper: add wireframe edge with deduplication.
let private addEdge (edgeSet: System.Collections.Generic.HashSet<int64>) (wirePositions: System.Collections.Generic.List<Vector3>) (wireIndices: System.Collections.Generic.List<int>) (v0: Vector3) (v1: Vector3) =
    let h0 = v0.GetHashCode() |> int64
    let h1 = v1.GetHashCode() |> int64
    let key = if h0 <= h1 then (h0 <<< 32) ||| (h1 &&& 0xFFFFFFFFL) else (h1 <<< 32) ||| (h0 &&& 0xFFFFFFFFL)
    if edgeSet.Add(key) then
        let idx = wirePositions.Count
        wirePositions.Add(v0)
        wirePositions.Add(v1)
        wireIndices.Add(idx)
        wireIndices.Add(idx + 1)

/// Build custom mesh data for a Triangle shape.
let buildTriangleMesh (shape: Shape) (color: StrideColor) : CustomMeshData option =
    if isNull shape || shape.ShapeCase <> Shape.ShapeOneofCase.Triangle then None
    else
        let tri = shape.Triangle
        if isNull tri.A || isNull tri.B || isNull tri.C then None
        else
            let a = vec3FromProto tri.A
            let b = vec3FromProto tri.B
            let c = vec3FromProto tri.C
            // Check for degenerate (collinear)
            let mutable ab = Vector3.Zero
            let mutable ac = Vector3.Zero
            Vector3.Subtract(&b, &a, &ab)
            Vector3.Subtract(&c, &a, &ac)
            let mutable cross = Vector3.Zero
            Vector3.Cross(&ab, &ac, &cross)
            if cross.Length() < 1.0e-6f then None
            else
                let n = computeFaceNormal a b c
                let mutable negN = Vector3.Zero
                Vector3.Negate(&n, &negN)
                let positions = [| a; b; c; a; c; b |]
                let normals = [| n; n; n; negN; negN; negN |]
                let indices = [| 0; 1; 2; 3; 4; 5 |]
                let wirePositions = [| a; b; c |]
                let wireIndices = [| 0; 1; 1; 2; 2; 0 |]
                Some { Positions = positions; Normals = normals; Color = color
                       Indices = indices
                       WireframePositions = wirePositions; WireframeIndices = wireIndices }

/// Build custom mesh data for a Mesh shape.
let buildMeshMesh (shape: Shape) (color: StrideColor) : CustomMeshData option =
    if isNull shape || shape.ShapeCase <> Shape.ShapeOneofCase.Mesh then None
    else
        let mesh = shape.Mesh
        if isNull mesh || mesh.Triangles.Count = 0 then None
        else
            let positions = System.Collections.Generic.List<Vector3>()
            let normals = System.Collections.Generic.List<Vector3>()
            let edgeSet = System.Collections.Generic.HashSet<int64>()
            let wirePositions = System.Collections.Generic.List<Vector3>()
            let wireIndices = System.Collections.Generic.List<int>()

            for tri in mesh.Triangles do
                let a = vec3FromProto tri.A
                let b = vec3FromProto tri.B
                let c = vec3FromProto tri.C
                addDoubleSidedTriangle positions normals a b c
                addEdge edgeSet wirePositions wireIndices a b
                addEdge edgeSet wirePositions wireIndices b c
                addEdge edgeSet wirePositions wireIndices c a

            let posArr = positions.ToArray()
            let indices = Array.init posArr.Length id
            Some { Positions = posArr; Normals = normals.ToArray(); Color = color
                   Indices = indices
                   WireframePositions = wirePositions.ToArray(); WireframeIndices = wireIndices.ToArray() }

/// Build custom mesh data for a ConvexHull shape using MIConvexHull.
let buildConvexHullMesh (shape: Shape) (color: StrideColor) : CustomMeshData option =
    if isNull shape || shape.ShapeCase <> Shape.ShapeOneofCase.ConvexHull then None
    else
        let hull = shape.ConvexHull
        if isNull hull || hull.Points.Count < 4 then
            if not (isNull hull) && hull.Points.Count = 3 then
                let triShape = Shape()
                let tri = Triangle()
                tri.A <- hull.Points.[0]
                tri.B <- hull.Points.[1]
                tri.C <- hull.Points.[2]
                triShape.Triangle <- tri
                buildTriangleMesh triShape color
            else None
        else
            let points =
                hull.Points
                |> Seq.map (fun p -> MIConvexHull.DefaultVertex(Position = [| float p.X; float p.Y; float p.Z |]))
                |> Array.ofSeq

            let result : MIConvexHull.ConvexHullCreationResult<MIConvexHull.DefaultVertex, MIConvexHull.DefaultConvexFace<MIConvexHull.DefaultVertex>> =
                MIConvexHull.ConvexHull.Create<MIConvexHull.DefaultVertex>(points)
            if isNull result || isNull result.Result then None
            else
                let faces : MIConvexHull.DefaultConvexFace<MIConvexHull.DefaultVertex> array = result.Result.Faces |> Seq.toArray
                if faces.Length = 0 then None
                else
                    let positions = System.Collections.Generic.List<Vector3>()
                    let normals = System.Collections.Generic.List<Vector3>()
                    let edgeSet = System.Collections.Generic.HashSet<int64>()
                    let wirePositions = System.Collections.Generic.List<Vector3>()
                    let wireIndices = System.Collections.Generic.List<int>()

                    for face in faces do
                        let fv : MIConvexHull.DefaultVertex array = face.Vertices
                        if not (isNull fv) then
                            let fvArr = fv
                            if fvArr.Length >= 3 then
                                let pos (v: MIConvexHull.DefaultVertex) =
                                    let p = v.Position
                                    Vector3(float32 p.[0], float32 p.[1], float32 p.[2])
                                let a = pos fvArr.[0]
                                let b = pos fvArr.[1]
                                let c = pos fvArr.[2]
                                addDoubleSidedTriangle positions normals a b c
                                addEdge edgeSet wirePositions wireIndices a b
                                addEdge edgeSet wirePositions wireIndices b c
                                addEdge edgeSet wirePositions wireIndices c a

                    if positions.Count = 0 then None
                    else
                        let posArr = positions.ToArray()
                        let indices = Array.init posArr.Length id
                        Some { Positions = posArr; Normals = normals.ToArray(); Color = color
                               Indices = indices
                               WireframePositions = wirePositions.ToArray(); WireframeIndices = wireIndices.ToArray() }

/// Dispatcher: build custom mesh data for any custom shape.
let buildCustomMesh (shape: Shape) (color: StrideColor) : CustomMeshData option =
    if isNull shape then None
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Triangle -> buildTriangleMesh shape color
        | Shape.ShapeOneofCase.Mesh -> buildMeshMesh shape color
        | Shape.ShapeOneofCase.ConvexHull -> buildConvexHullMesh shape color
        | _ -> None
