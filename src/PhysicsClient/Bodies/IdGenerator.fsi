module PhysicsClient.IdGenerator

/// Generate the next human-readable ID for the given shape kind (e.g., "sphere" → "sphere-1").
val nextId : shapeKind: string -> string

/// Reset all counters. Used when reconnecting or clearing state.
val reset : unit -> unit
