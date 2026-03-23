module PhysicsSimulation.MeshIdGenerator

open System
open System.Security.Cryptography
open Google.Protobuf
open PhysicsSandbox.Shared.Contracts

let private isComplexShape (shape: Shape) =
    if isNull shape then false
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.ConvexHull -> true
        | Shape.ShapeOneofCase.Mesh -> true
        | Shape.ShapeOneofCase.Compound -> true
        | _ -> false

/// Serialize the geometry-relevant bytes of a complex shape for hashing.
let private geometryBytes (shape: Shape) : byte array =
    match shape.ShapeCase with
    | Shape.ShapeOneofCase.ConvexHull ->
        shape.ConvexHull.ToByteArray()
    | Shape.ShapeOneofCase.Mesh ->
        shape.Mesh.ToByteArray()
    | Shape.ShapeOneofCase.Compound ->
        shape.Compound.ToByteArray()
    | _ ->
        Array.empty

let computeMeshId (shape: Shape) : string option =
    if isNull shape || not (isComplexShape shape) then
        None
    else
        let bytes = geometryBytes shape
        if bytes.Length = 0 then None
        else
            let hash = SHA256.HashData(bytes)
            // Truncate to 128 bits (16 bytes), encode as 32 hex chars
            let truncated = hash |> Array.take 16
            Some (Convert.ToHexString(truncated).ToLowerInvariant())

let private minVec3 (a: Vec3) (b: Vec3) =
    let v = Vec3()
    v.X <- min a.X b.X
    v.Y <- min a.Y b.Y
    v.Z <- min a.Z b.Z
    v

let private maxVec3 (a: Vec3) (b: Vec3) =
    let v = Vec3()
    v.X <- max a.X b.X
    v.Y <- max a.Y b.Y
    v.Z <- max a.Z b.Z
    v

let private vec3Of (x: double) (y: double) (z: double) =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

let private expandBounds (bMin: Vec3) (bMax: Vec3) (p: Vec3) =
    if isNull p then (bMin, bMax)
    else (minVec3 bMin p, maxVec3 bMax p)

let rec computeBoundingBox (shape: Shape) : (Vec3 * Vec3) option =
    if isNull shape || not (isComplexShape shape) then
        None
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.ConvexHull ->
            let points = shape.ConvexHull.Points
            if points.Count = 0 then None
            else
                let mutable bMin = vec3Of Double.MaxValue Double.MaxValue Double.MaxValue
                let mutable bMax = vec3Of Double.MinValue Double.MinValue Double.MinValue
                for p in points do
                    if not (isNull p) then
                        let pair = expandBounds bMin bMax p
                        bMin <- fst pair
                        bMax <- snd pair
                Some (bMin, bMax)

        | Shape.ShapeOneofCase.Mesh ->
            let triangles = shape.Mesh.Triangles
            if triangles.Count = 0 then None
            else
                let mutable bMin = vec3Of Double.MaxValue Double.MaxValue Double.MaxValue
                let mutable bMax = vec3Of Double.MinValue Double.MinValue Double.MinValue
                for tri in triangles do
                    for v in [tri.A; tri.B; tri.C] do
                        if not (isNull v) then
                            let pair = expandBounds bMin bMax v
                            bMin <- fst pair
                            bMax <- snd pair
                Some (bMin, bMax)

        | Shape.ShapeOneofCase.Compound ->
            let children = shape.Compound.Children
            if children.Count = 0 then None
            else
                let mutable bMin = vec3Of Double.MaxValue Double.MaxValue Double.MaxValue
                let mutable bMax = vec3Of Double.MinValue Double.MinValue Double.MinValue
                for child in children do
                    let childShape: Shape = child.Shape
                    let childBBox = computeBoundingBox childShape
                    match childBBox with
                    | Some (cMin, cMax) ->
                        // Offset child bounds by local position
                        let lp = if isNull child.LocalPosition then Vec3() else child.LocalPosition
                        let offsetMin = vec3Of (cMin.X + lp.X) (cMin.Y + lp.Y) (cMin.Z + lp.Z)
                        let offsetMax = vec3Of (cMax.X + lp.X) (cMax.Y + lp.Y) (cMax.Z + lp.Z)
                        let pair1 = expandBounds bMin bMax offsetMin
                        bMin <- fst pair1
                        bMax <- snd pair1
                        let pair2 = expandBounds bMin bMax offsetMax
                        bMin <- fst pair2
                        bMax <- snd pair2
                    | None ->
                        // Non-complex child (primitive) — estimate 0.5m bounds around offset
                        let lp = if isNull child.LocalPosition then Vec3() else child.LocalPosition
                        let offset = 0.5
                        let pair1 = expandBounds bMin bMax (vec3Of (lp.X - offset) (lp.Y - offset) (lp.Z - offset))
                        bMin <- fst pair1
                        bMax <- snd pair1
                        let pair2 = expandBounds bMin bMax (vec3Of (lp.X + offset) (lp.Y + offset) (lp.Z + offset))
                        bMin <- fst pair2
                        bMax <- snd pair2
                Some (bMin, bMax)

        | _ -> None
