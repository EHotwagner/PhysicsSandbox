module PhysicsViewer.Tests.MeshResolverTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.Streaming.MeshResolver

let private makeResolver () =
    create (Unchecked.defaultof<PhysicsHub.PhysicsHubClient>)

[<Fact>]
let ``processNewMeshes populates cache`` () =
    let resolver = makeResolver ()
    let mg = MeshGeometry(MeshId = "mesh-001", Shape = Shape(Sphere = Sphere(Radius = 1.0)))
    processNewMeshes [mg] resolver
    let result = resolve "mesh-001" resolver
    Assert.True(result.IsSome)

[<Fact>]
let ``resolve returns None for unknown`` () =
    let resolver = makeResolver ()
    Assert.True((resolve "unknown" resolver).IsNone)

[<Fact>]
let ``duplicate processNewMeshes does not overwrite`` () =
    let resolver = makeResolver ()
    let mg1 = MeshGeometry(MeshId = "a", Shape = Shape(Sphere = Sphere(Radius = 1.0)))
    let mg2 = MeshGeometry(MeshId = "a", Shape = Shape(Sphere = Sphere(Radius = 2.0)))
    processNewMeshes [mg1] resolver
    processNewMeshes [mg2] resolver
    let result = (resolve "a" resolver).Value
    Assert.Equal(1.0, result.Sphere.Radius) // first write wins (TryAdd)
