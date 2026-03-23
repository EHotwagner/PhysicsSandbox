module PhysicsServer.Hub.MeshCache

open PhysicsSandbox.Shared.Contracts

/// Opaque server-side mesh geometry cache.
type MeshCacheState

/// Create a new empty mesh cache.
val create : unit -> MeshCacheState

/// Set a logger for structured log messages.
val setLogger : Microsoft.Extensions.Logging.ILogger -> MeshCacheState -> unit

/// Add a mesh geometry to the cache by mesh ID.
val add : string -> Shape -> MeshCacheState -> unit

/// Try to retrieve a cached mesh geometry by mesh ID.
val tryGet : string -> MeshCacheState -> Shape option

/// Retrieve geometries for multiple mesh IDs. Returns only found entries.
val getMany : string list -> MeshCacheState -> MeshGeometry list

/// Remove all entries from the cache.
val clear : MeshCacheState -> unit

/// Return the number of cached entries.
val count : MeshCacheState -> int
