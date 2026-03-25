module PhysicsSandbox.Mcp.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

let private anchorType = typeof<PhysicsSandbox.Mcp.GrpcConnection.GrpcConnection>

[<Fact>]
let ``MeshFetchQueryTools public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Mcp.MeshFetchQueryTools+MeshFetchQueryTools"
        [ "query_mesh_fetches" ]
