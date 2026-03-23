module PhysicsClient.MeshResolver

open System.Collections.Concurrent
open PhysicsSandbox.Shared.Contracts

type MeshResolverState =
    { Cache: ConcurrentDictionary<string, Shape>
      Client: PhysicsHub.PhysicsHubClient }

let create (client: PhysicsHub.PhysicsHubClient) =
    { Cache = ConcurrentDictionary<string, Shape>()
      Client = client }

let processNewMeshes (meshes: MeshGeometry seq) (state: MeshResolverState) =
    for mg in meshes do
        state.Cache.TryAdd(mg.MeshId, mg.Shape) |> ignore

let resolve (meshId: string) (state: MeshResolverState) =
    match state.Cache.TryGetValue(meshId) with
    | true, shape -> Some shape
    | _ -> None

let fetchMissingSync (meshIds: string list) (state: MeshResolverState) =
    let toFetch = meshIds |> List.filter (fun id -> not (state.Cache.ContainsKey(id)))
    if not toFetch.IsEmpty then
        try
            let request = MeshRequest()
            for id in toFetch do
                request.MeshIds.Add(id)
            let response = state.Client.FetchMeshes(request)
            for mg in response.Meshes do
                state.Cache.TryAdd(mg.MeshId, mg.Shape) |> ignore
        with _ -> ()
