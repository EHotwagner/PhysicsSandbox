module PhysicsViewer.Streaming.MeshResolver

open System.Collections.Concurrent
open PhysicsSandbox.Shared.Contracts

type MeshResolverState =
    { Cache: ConcurrentDictionary<string, Shape>
      Pending: ConcurrentDictionary<string, unit>
      Client: PhysicsHub.PhysicsHubClient }

let create (client: PhysicsHub.PhysicsHubClient) =
    { Cache = ConcurrentDictionary<string, Shape>()
      Pending = ConcurrentDictionary<string, unit>()
      Client = client }

let processNewMeshes (meshes: MeshGeometry seq) (state: MeshResolverState) =
    for mg in meshes do
        state.Cache.TryAdd(mg.MeshId, mg.Shape) |> ignore
        state.Pending.TryRemove(mg.MeshId) |> ignore

let resolve (meshId: string) (state: MeshResolverState) =
    match state.Cache.TryGetValue(meshId) with
    | true, shape -> Some shape
    | _ -> None

let fetchMissing (meshIds: string list) (state: MeshResolverState) =
    async {
        // Filter to IDs not already cached or pending
        let toFetch =
            meshIds
            |> List.filter (fun id ->
                not (state.Cache.ContainsKey(id)) && state.Pending.TryAdd(id, ()))
        if not toFetch.IsEmpty then
            try
                let request = MeshRequest()
                for id in toFetch do
                    request.MeshIds.Add(id)
                let response = state.Client.FetchMeshes(request)
                for mg in response.Meshes do
                    state.Cache.TryAdd(mg.MeshId, mg.Shape) |> ignore
                    state.Pending.TryRemove(mg.MeshId) |> ignore
            with _ ->
                // Remove pending markers on failure so they can be retried
                for id in toFetch do
                    state.Pending.TryRemove(id) |> ignore
    }
