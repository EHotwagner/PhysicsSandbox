module PhysicsViewer.Streaming.MeshResolver

open PhysicsSandbox.Shared.Contracts

/// Opaque mesh resolver state for the viewer.
type MeshResolverState

/// Create a new mesh resolver with a gRPC client for on-demand fetches.
val create : PhysicsHub.PhysicsHubClient -> MeshResolverState

/// Process new mesh geometries from a state update into the local cache.
val processNewMeshes : MeshGeometry seq -> MeshResolverState -> unit

/// Resolve a mesh ID to its full shape from the local cache.
val resolve : string -> MeshResolverState -> Shape option

/// Fetch missing mesh geometries from the server asynchronously.
val fetchMissing : string list -> MeshResolverState -> Async<unit>
