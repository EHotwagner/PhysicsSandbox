module PhysicsSandbox.Mcp.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    let t = typeof<PhysicsSandbox.Mcp.MeshResolver.MeshResolverState>.DeclaringType
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "create"; "fetchMissingSync"; "processNewMeshes"; "resolve" |] do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``MeshFetchQueryTools public API matches baseline`` () =
    let asm = typeof<PhysicsSandbox.Mcp.MeshResolver.MeshResolverState>.Assembly
    let t = asm.GetType("PhysicsSandbox.Mcp.MeshFetchQueryTools+MeshFetchQueryTools")
    Assert.NotNull(t)
    let members = getPublicMembers t
    Assert.True(members |> Array.exists (fun m -> m = "query_mesh_fetches"), "Missing public member: query_mesh_fetches")
