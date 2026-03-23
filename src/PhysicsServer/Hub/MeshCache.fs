module PhysicsServer.Hub.MeshCache

open System.Collections.Concurrent
open Microsoft.Extensions.Logging
open PhysicsSandbox.Shared.Contracts

type MeshCacheState =
    { Entries: ConcurrentDictionary<string, Shape>
      mutable Logger: ILogger option }

let create () =
    { Entries = ConcurrentDictionary<string, Shape>()
      Logger = None }

let setLogger (logger: ILogger) (cache: MeshCacheState) =
    cache.Logger <- Some logger

let private shapeTypeName (shape: Shape) =
    if isNull shape then "Unknown"
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.ConvexHull -> "ConvexHull"
        | Shape.ShapeOneofCase.Mesh -> "MeshShape"
        | Shape.ShapeOneofCase.Compound -> "Compound"
        | _ -> shape.ShapeCase.ToString()

let add (meshId: string) (shape: Shape) (cache: MeshCacheState) =
    let added = cache.Entries.TryAdd(meshId, shape)
    if added then
        match cache.Logger with
        | Some logger ->
            logger.LogDebug("MeshCache: cached {MeshId} ({ShapeType}, {ByteSize} bytes)",
                meshId, shapeTypeName shape, shape.CalculateSize())
        | None -> ()

let tryGet (meshId: string) (cache: MeshCacheState) =
    match cache.Entries.TryGetValue(meshId) with
    | true, shape -> Some shape
    | _ -> None

let getMany (meshIds: string list) (cache: MeshCacheState) =
    let results =
        meshIds
        |> List.choose (fun id ->
            match cache.Entries.TryGetValue(id) with
            | true, shape ->
                let mg = MeshGeometry()
                mg.MeshId <- id
                mg.Shape <- shape
                Some mg
            | _ -> None)
    match cache.Logger with
    | Some logger ->
        let hits = results.Length
        let misses = meshIds.Length - hits
        logger.LogDebug("MeshCache: FetchMeshes served {Hits} hit(s), {Misses} miss(es)", hits, misses)
    | None -> ()
    results

let clear (cache: MeshCacheState) =
    let prevCount = cache.Entries.Count
    cache.Entries.Clear()
    match cache.Logger with
    | Some logger ->
        logger.LogDebug("MeshCache: cleared {Count} entries", prevCount)
    | None -> ()

let count (cache: MeshCacheState) =
    cache.Entries.Count
