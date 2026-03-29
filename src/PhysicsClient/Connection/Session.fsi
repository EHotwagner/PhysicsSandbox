module PhysicsClient.Session

/// Opaque session handle for all library operations.
type Session

/// Connect to the physics server. Returns a session or error.
val connect : serverAddress: string -> Result<Session, string>

/// Disconnect and clean up resources.
val disconnect : session: Session -> unit

/// Reconnect a disconnected session to the same server.
val reconnect : session: Session -> Result<Session, string>

/// Check if the session is currently connected.
val isConnected : session: Session -> bool

/// Internal: get the gRPC client from a session. Used by command modules.
val internal client : session: Session -> PhysicsSandbox.Shared.Contracts.PhysicsHub.PhysicsHubClient

/// Get the body registry from a session. Maps body names to shape kinds.
val bodyRegistry : session: Session -> System.Collections.Concurrent.ConcurrentDictionary<string, string>

/// Get the latest cached simulation state, or None if no state has arrived yet.
val latestState : session: Session -> PhysicsSandbox.Shared.Contracts.SimulationState option

/// Get the timestamp of the last state update received from the server.
val lastStateUpdate : session: Session -> System.DateTime

/// Internal: clear all client-side caches (body registry, properties, constraints, shapes, state).
val internal clearCaches : session: Session -> unit

/// Internal: send a simulation command and return the ack result.
val internal sendCommand : session: Session -> PhysicsSandbox.Shared.Contracts.SimulationCommand -> Result<unit, string>

/// Internal: send a view command and return the ack result.
val internal sendViewCommand : session: Session -> PhysicsSandbox.Shared.Contracts.ViewCommand -> Result<unit, string>

/// Internal: get the mesh resolver state for resolving CachedShapeRef.
val internal meshResolver : session: Session -> PhysicsClient.MeshResolver.MeshResolverState
