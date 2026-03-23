module PhysicsSimulation.Tests.MeshIdGeneratorTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.MeshIdGenerator

let private makeVec3 x y z =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

let private makeConvexHull (points: (float * float * float) list) =
    let hull = ConvexHull()
    for (x, y, z) in points do
        hull.Points.Add(makeVec3 x y z)
    Shape(ConvexHull = hull)

let private makeMeshShape (triangles: ((float*float*float) * (float*float*float) * (float*float*float)) list) =
    let mesh = MeshShape()
    for ((ax,ay,az), (bx,by,bz), (cx,cy,cz)) in triangles do
        let tri = MeshTriangle()
        tri.A <- makeVec3 ax ay az
        tri.B <- makeVec3 bx by bz
        tri.C <- makeVec3 cx cy cz
        mesh.Triangles.Add(tri)
    Shape(Mesh = mesh)

[<Fact>]
let ``Same ConvexHull points produce same mesh ID`` () =
    let points = [(0.0, 0.0, 0.0); (1.0, 0.0, 0.0); (0.0, 1.0, 0.0); (0.0, 0.0, 1.0)]
    let shape1 = makeConvexHull points
    let shape2 = makeConvexHull points
    let id1 = computeMeshId shape1
    let id2 = computeMeshId shape2
    Assert.True(id1.IsSome)
    Assert.Equal(id1.Value, id2.Value)

[<Fact>]
let ``Different ConvexHull points produce different mesh ID`` () =
    let shape1 = makeConvexHull [(0.0, 0.0, 0.0); (1.0, 0.0, 0.0); (0.0, 1.0, 0.0); (0.0, 0.0, 1.0)]
    let shape2 = makeConvexHull [(0.0, 0.0, 0.0); (2.0, 0.0, 0.0); (0.0, 2.0, 0.0); (0.0, 0.0, 2.0)]
    let id1 = computeMeshId shape1
    let id2 = computeMeshId shape2
    Assert.True(id1.IsSome)
    Assert.True(id2.IsSome)
    Assert.NotEqual<string>(id1.Value, id2.Value)

[<Fact>]
let ``Sphere returns None`` () =
    let shape = Shape(Sphere = Sphere(Radius = 1.0))
    Assert.True((computeMeshId shape).IsNone)

[<Fact>]
let ``Box returns None`` () =
    let shape = Shape(Box = Box(HalfExtents = makeVec3 1.0 1.0 1.0))
    Assert.True((computeMeshId shape).IsNone)

[<Fact>]
let ``Triangle primitive returns None`` () =
    let tri = Triangle()
    tri.A <- makeVec3 0.0 0.0 0.0
    tri.B <- makeVec3 1.0 0.0 0.0
    tri.C <- makeVec3 0.0 1.0 0.0
    let shape = Shape(Triangle = tri)
    Assert.True((computeMeshId shape).IsNone)

[<Fact>]
let ``Null shape returns None`` () =
    Assert.True((computeMeshId null).IsNone)

[<Fact>]
let ``MeshShape produces deterministic ID`` () =
    let tris = [((0.0,0.0,0.0), (1.0,0.0,0.0), (0.0,1.0,0.0))]
    let shape1 = makeMeshShape tris
    let shape2 = makeMeshShape tris
    let id1 = computeMeshId shape1
    let id2 = computeMeshId shape2
    Assert.True(id1.IsSome)
    Assert.Equal(id1.Value, id2.Value)
    Assert.Equal(32, id1.Value.Length) // 128 bits = 32 hex chars

[<Fact>]
let ``Compound with ConvexHull children produces deterministic ID`` () =
    let child1 = CompoundChild()
    child1.Shape <- makeConvexHull [(0.0, 0.0, 0.0); (1.0, 0.0, 0.0); (0.0, 1.0, 0.0); (0.0, 0.0, 1.0)]
    child1.LocalPosition <- makeVec3 1.0 0.0 0.0
    let compound = Compound()
    compound.Children.Add(child1)
    let shape1 = Shape(Compound = compound)

    let child2 = CompoundChild()
    child2.Shape <- makeConvexHull [(0.0, 0.0, 0.0); (1.0, 0.0, 0.0); (0.0, 1.0, 0.0); (0.0, 0.0, 1.0)]
    child2.LocalPosition <- makeVec3 1.0 0.0 0.0
    let compound2 = Compound()
    compound2.Children.Add(child2)
    let shape2 = Shape(Compound = compound2)

    let id1 = computeMeshId shape1
    let id2 = computeMeshId shape2
    Assert.True(id1.IsSome)
    Assert.Equal(id1.Value, id2.Value)

[<Fact>]
let ``ConvexHull AABB computation is correct`` () =
    let shape = makeConvexHull [(0.0, -1.0, 2.0); (3.0, 4.0, -1.0); (1.0, 2.0, 0.0); (-2.0, 0.0, 1.0)]
    let bbox = computeBoundingBox shape
    Assert.True(bbox.IsSome)
    let (bMin, bMax) = bbox.Value
    Assert.Equal(-2.0, bMin.X)
    Assert.Equal(-1.0, bMin.Y)
    Assert.Equal(-1.0, bMin.Z)
    Assert.Equal(3.0, bMax.X)
    Assert.Equal(4.0, bMax.Y)
    Assert.Equal(2.0, bMax.Z)

[<Fact>]
let ``MeshShape AABB computation is correct`` () =
    let shape = makeMeshShape [((0.0, 0.0, 0.0), (5.0, 0.0, 0.0), (0.0, 3.0, -2.0))]
    let bbox = computeBoundingBox shape
    Assert.True(bbox.IsSome)
    let (bMin, bMax) = bbox.Value
    Assert.Equal(0.0, bMin.X)
    Assert.Equal(0.0, bMin.Y)
    Assert.Equal(-2.0, bMin.Z)
    Assert.Equal(5.0, bMax.X)
    Assert.Equal(3.0, bMax.Y)
    Assert.Equal(0.0, bMax.Z)

[<Fact>]
let ``Sphere AABB returns None`` () =
    let shape = Shape(Sphere = Sphere(Radius = 1.0))
    Assert.True((computeBoundingBox shape).IsNone)
