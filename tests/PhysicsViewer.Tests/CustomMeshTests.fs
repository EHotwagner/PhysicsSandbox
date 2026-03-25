module PhysicsViewer.Tests.CustomMeshTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.ShapeGeometry

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box
type StrideColor = Stride.Core.Mathematics.Color

// ---------------------------------------------------------------------------
// Custom Mesh Infrastructure Tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``isCustomShape returns true for Triangle, Mesh, ConvexHull`` () =
    let tri = Shape(Triangle = Triangle())
    let mesh = Shape(Mesh = MeshShape())
    let hull = Shape(ConvexHull = ConvexHull())
    Assert.True(isCustomShape tri)
    Assert.True(isCustomShape mesh)
    Assert.True(isCustomShape hull)

[<Fact>]
let ``isCustomShape returns false for primitives`` () =
    Assert.False(isCustomShape (Shape(Sphere = ProtoSphere(Radius = 1.0))))
    Assert.False(isCustomShape (Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))))
    Assert.False(isCustomShape (Shape(Capsule = PhysicsSandbox.Shared.Contracts.Capsule(Radius = 0.5, Length = 1.0))))
    Assert.False(isCustomShape (Shape(Cylinder = PhysicsSandbox.Shared.Contracts.Cylinder(Radius = 0.5, Length = 1.0))))
    Assert.False(isCustomShape (Shape(Plane = Plane())))

[<Fact>]
let ``buildCustomMesh returns None for primitives`` () =
    Assert.True((buildCustomMesh (Shape(Sphere = ProtoSphere(Radius = 1.0))) StrideColor.Red).IsNone)
    Assert.True((buildCustomMesh (Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))) StrideColor.Red).IsNone)

[<Fact>]
let ``buildTriangleMesh produces 6 vertices and 6 indices for valid triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    let result = buildTriangleMesh (Shape(Triangle = tri)) StrideColor.Cyan
    Assert.True(result.IsSome)
    let data = result.Value
    Assert.Equal(6, data.Positions.Length) // 3 front + 3 back (double-sided)
    Assert.Equal(6, data.Indices.Length)
    Assert.Equal(3, data.WireframePositions.Length) // 3 edge vertices
    Assert.Equal(6, data.WireframeIndices.Length)   // 3 edges × 2 indices

[<Fact>]
let ``buildTriangleMesh returns None for collinear vertices`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 2.0, Y = 0.0, Z = 0.0) // collinear
    let result = buildTriangleMesh (Shape(Triangle = tri)) StrideColor.Cyan
    Assert.True(result.IsNone)

[<Fact>]
let ``buildTriangleMesh uses correct color`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    let color = StrideColor.Cyan
    let result = buildTriangleMesh (Shape(Triangle = tri)) color
    Assert.True(result.IsSome)
    Assert.Equal(color, result.Value.Color)

[<Fact>]
let ``buildMeshMesh produces correct vertex count for multi-triangle mesh`` () =
    let mesh = MeshShape()
    for i in 0 .. 3 do
        let mt = MeshTriangle()
        mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        mt.B <- Vec3(X = float (i + 1), Y = 0.0, Z = 0.0)
        mt.C <- Vec3(X = 0.0, Y = float (i + 1), Z = 0.0)
        mesh.Triangles.Add(mt)
    let result = buildMeshMesh (Shape(Mesh = mesh)) StrideColor.Red
    Assert.True(result.IsSome)
    let data = result.Value
    Assert.Equal(24, data.Positions.Length) // 4 triangles × 6 vertices (double-sided)

[<Fact>]
let ``buildMeshMesh returns None for empty mesh`` () =
    let mesh = MeshShape()
    let result = buildMeshMesh (Shape(Mesh = mesh)) StrideColor.Red
    Assert.True(result.IsNone)

[<Fact>]
let ``buildMeshMesh uses correct color`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    let color = StrideColor(0uy, 128uy, 128uy, 255uy)
    let result = buildMeshMesh (Shape(Mesh = mesh)) color
    Assert.True(result.IsSome)
    Assert.Equal(color, result.Value.Color)

[<Fact>]
let ``buildCustomMesh dispatches to buildTriangleMesh for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    let result = buildCustomMesh (Shape(Triangle = tri)) StrideColor.Cyan
    Assert.True(result.IsSome)
    Assert.Equal(6, result.Value.Positions.Length)

[<Fact>]
let ``buildCustomMesh dispatches to buildMeshMesh for Mesh`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    let result = buildCustomMesh (Shape(Mesh = mesh)) StrideColor.Red
    Assert.True(result.IsSome)

[<Fact>]
let ``buildConvexHullMesh produces faces for 8-point cube`` () =
    let hull = ConvexHull()
    // 8 corners of a unit cube
    for x in [0.0; 1.0] do
        for y in [0.0; 1.0] do
            for z in [0.0; 1.0] do
                hull.Points.Add(Vec3(X = x, Y = y, Z = z))
    let result = buildConvexHullMesh (Shape(ConvexHull = hull)) StrideColor.Purple
    Assert.True(result.IsSome)
    let data = result.Value
    // Cube has 6 faces × 2 triangles = 12 faces × 6 vertices (double-sided) = 72
    Assert.True(data.Positions.Length >= 12) // At least 12 triangular faces (double-sided = ×2)

[<Fact>]
let ``buildConvexHullMesh produces faces for tetrahedron`` () =
    let hull = ConvexHull()
    hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.5, Y = 1.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.5, Y = 0.5, Z = 1.0))
    let result = buildConvexHullMesh (Shape(ConvexHull = hull)) StrideColor.Purple
    Assert.True(result.IsSome)
    let data = result.Value
    // Tetrahedron has 4 faces × 6 vertices (double-sided) = 24
    Assert.Equal(24, data.Positions.Length)

[<Fact>]
let ``buildConvexHullMesh returns None for fewer than 4 points`` () =
    let hull = ConvexHull()
    hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
    let result = buildConvexHullMesh (Shape(ConvexHull = hull)) StrideColor.Purple
    Assert.True(result.IsNone)

[<Fact>]
let ``buildCustomMesh dispatches to buildConvexHullMesh for ConvexHull`` () =
    let hull = ConvexHull()
    hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.5, Y = 1.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.5, Y = 0.5, Z = 1.0))
    let result = buildCustomMesh (Shape(ConvexHull = hull)) StrideColor.Purple
    Assert.True(result.IsSome)
