module PhysicsSandbox.Mcp.Tests.SurfaceAreaTests

open System
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

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
