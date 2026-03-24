module PhysicsViewer.Tests.SceneManagerTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.SceneManager
open PhysicsViewer.ShapeGeometry
open Stride.CommunityToolkit.Rendering.ProceduralModels

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box
type StrideColor = Stride.Core.Mathematics.Color

// ---------------------------------------------------------------------------
// ShapeGeometry.primitiveType
// ---------------------------------------------------------------------------

[<Fact>]
let ``primitiveType returns Sphere for proto Sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 1.0))
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType shape)

[<Fact>]
let ``primitiveType returns Cube for proto Box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))
    Assert.Equal(PrimitiveModelType.Cube, primitiveType shape)

[<Fact>]
let ``primitiveType returns Sphere for null shape`` () =
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType null)

[<Fact>]
let ``primitiveType returns Sphere for unset shape`` () =
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType (Shape()))

[<Fact>]
let ``defaultColor returns blue for Sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 1.0))
    let c = defaultColor shape
    Assert.Equal(StrideColor(64uy, 128uy, 255uy, 255uy), c)

[<Fact>]
let ``defaultColor returns orange for Box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))
    Assert.Equal(StrideColor.Orange, defaultColor shape)

// ---------------------------------------------------------------------------
// FR-007: Shape type renders as correct primitive
// ---------------------------------------------------------------------------

[<Fact>]
let ``primitiveType returns Sphere fallback for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    // Triangle now uses custom mesh path; primitiveType is only for degenerate fallback
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType (Shape(Triangle = tri)))

[<Fact>]
let ``primitiveType returns Sphere fallback for Mesh`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    // Mesh now uses custom mesh path; primitiveType is only for degenerate fallback
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType (Shape(Mesh = mesh)))

[<Fact>]
let ``primitiveType returns Cube for Compound`` () =
    let compound = Compound()
    let child = CompoundChild()
    child.Shape <- Shape(Sphere = PhysicsSandbox.Shared.Contracts.Sphere(Radius = 0.5))
    compound.Children.Add(child)
    Assert.Equal(PrimitiveModelType.Cube, primitiveType (Shape(Compound = compound)))

[<Fact>]
let ``shapeSize returns small fallback for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 2.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 3.0, Z = 0.0)
    // Triangle now uses custom mesh path; shapeSize is only for degenerate fallback
    let size = shapeSize (Shape(Triangle = tri))
    Assert.True(size.HasValue)
    Assert.Equal(0.1f, size.Value.X)

[<Fact>]
let ``shapeSize returns small fallback for Mesh`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = -1.0, Y = 0.0, Z = -1.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 2.0, Z = 1.0)
    mesh.Triangles.Add(mt)
    // Mesh now uses custom mesh path; shapeSize is only for degenerate fallback
    let size = shapeSize (Shape(Mesh = mesh))
    Assert.True(size.HasValue)
    Assert.Equal(0.1f, size.Value.X)

// ---------------------------------------------------------------------------
// T029b: Default Color Palette Tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``defaultColor returns green for Capsule`` () =
    let shape = Shape(Capsule = PhysicsSandbox.Shared.Contracts.Capsule(Radius = 0.5, Length = 1.0))
    Assert.Equal(StrideColor.Green, defaultColor shape)

[<Fact>]
let ``defaultColor returns yellow for Cylinder`` () =
    let shape = Shape(Cylinder = PhysicsSandbox.Shared.Contracts.Cylinder(Radius = 0.5, Length = 1.0))
    Assert.Equal(StrideColor.Yellow, defaultColor shape)

[<Fact>]
let ``defaultColor returns gray for Plane`` () =
    let shape = Shape(Plane = Plane(Normal = Vec3(X = 0.0, Y = 1.0, Z = 0.0)))
    Assert.Equal(StrideColor.Gray, defaultColor shape)

[<Fact>]
let ``defaultColor returns cyan for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    let shape = Shape(Triangle = tri)
    Assert.Equal(StrideColor(0uy, 255uy, 255uy, 255uy), defaultColor shape)

[<Fact>]
let ``defaultColor returns teal for MeshShape`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    let shape = Shape(Mesh = mesh)
    Assert.Equal(StrideColor(0uy, 128uy, 128uy, 255uy), defaultColor shape)

// ---------------------------------------------------------------------------
// ShapeGeometry.shapeSize
// ---------------------------------------------------------------------------

[<Fact>]
let ``shapeSize returns radius for sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 2.0))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X)
    Assert.Equal(2.0f, size.Value.Y)
    Assert.Equal(2.0f, size.Value.Z)

[<Fact>]
let ``shapeSize returns doubled extents for box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 2.0, Z = 3.0)))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X)
    Assert.Equal(4.0f, size.Value.Y)
    Assert.Equal(6.0f, size.Value.Z)

[<Fact>]
let ``shapeSize returns radius and length for capsule`` () =
    let shape = Shape(Capsule = PhysicsSandbox.Shared.Contracts.Capsule(Radius = 0.3, Length = 1.0))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(0.3f, size.Value.X)
    Assert.Equal(1.0f, size.Value.Y)
    Assert.Equal(0.3f, size.Value.Z)

[<Fact>]
let ``shapeSize returns radius and length for cylinder`` () =
    let shape = Shape(Cylinder = PhysicsSandbox.Shared.Contracts.Cylinder(Radius = 0.5, Length = 2.0))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(0.5f, size.Value.X)
    Assert.Equal(2.0f, size.Value.Y)
    Assert.Equal(0.5f, size.Value.Z)

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

// ---------------------------------------------------------------------------
// SceneState initial values
// ---------------------------------------------------------------------------

[<Fact>]
let ``create returns state with zero simulation time`` () =
    let state = create ()
    Assert.Equal(0.0, simulationTime state)

[<Fact>]
let ``create returns state with running false`` () =
    let state = create ()
    Assert.False(isRunning state)

[<Fact>]
let ``create returns state with wireframe false`` () =
    let state = create ()
    Assert.False(isWireframe state)

// ---------------------------------------------------------------------------
// T038: SceneManager narration tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyNarration sets NarrationText`` () =
    let state = create () |> applyNarration "Hello, world!"
    Assert.Equal(Some "Hello, world!", narrationText state)

[<Fact>]
let ``applyNarration with empty string clears NarrationText`` () =
    let state =
        create ()
        |> applyNarration "Some narration"
        |> applyNarration ""
    Assert.Equal(None, narrationText state)
