module PhysicsSimulation.SimulationWorld

open PhysicsSandbox.Shared.Contracts

/// Opaque simulation world handle
type World

/// Create a new simulation world with default configuration (paused, zero gravity)
val create : unit -> World

/// Destroy the simulation world and release resources
val destroy : World -> unit

/// Get whether the simulation is currently running (playing)
val isRunning : World -> bool

/// Get the current simulation time
val time : World -> double

/// Step the simulation by one fixed time step. Returns the new state.
val step : World -> SimulationState

/// Get the current state as a proto SimulationState (without stepping)
val currentState : World -> SimulationState

/// Set the running (play/pause) state
val setRunning : World -> bool -> unit

/// Add a rigid body to the world. Returns CommandAck with success/failure.
val addBody : World -> AddBody -> CommandAck

/// Remove a body by identifier. Auto-removes constraints referencing this body.
val removeBody : World -> string -> CommandAck

/// Add a persistent force to a body (accumulates, applied each step)
val applyForce : World -> string -> Vec3 -> CommandAck

/// Apply a one-shot linear impulse to a body (not stored)
val applyImpulse : World -> string -> Vec3 -> CommandAck

/// Apply a torque to a body (rotational force)
val applyTorque : World -> string -> Vec3 -> CommandAck

/// Remove all persistent forces from a body
val clearForces : World -> string -> CommandAck

/// Set the global gravity vector
val setGravity : World -> Vec3 -> unit

/// Register a named shape for later reference
val registerShape : World -> RegisterShape -> CommandAck

/// Unregister a named shape
val unregisterShape : World -> string -> CommandAck

/// Add a constraint between two bodies
val addConstraint : World -> AddConstraint -> CommandAck

/// Remove a constraint by identifier
val removeConstraint : World -> string -> CommandAck

/// Set collision filter on a body at runtime
val setCollisionFilter : World -> SetCollisionFilter -> CommandAck

/// Set position, orientation, and/or velocity of a body at runtime
val setBodyPose : World -> SetBodyPose -> CommandAck

/// Reset the simulation: remove all bodies, constraints, shapes, forces; reset time to 0
val resetSimulation : World -> CommandAck

/// Internal: body record for query handler ID resolution.
type internal BodyRecord =
    { Id: string
      BepuBodyId: BepuFSharp.BodyId
      BepuStaticId: BepuFSharp.StaticId
      ShapeId: BepuFSharp.ShapeId
      Mass: float32
      ShapeProto: PhysicsSandbox.Shared.Contracts.Shape
      IsStatic: bool
      MotionType: PhysicsSandbox.Shared.Contracts.BodyMotionType
      StaticPosition: System.Numerics.Vector3
      StaticOrientation: System.Numerics.Quaternion
      Color: PhysicsSandbox.Shared.Contracts.Color option
      Material: PhysicsSandbox.Shared.Contracts.MaterialProperties option
      CollisionGroup: uint32
      CollisionMask: uint32
      MeshId: string option
      BoundingBox: (PhysicsSandbox.Shared.Contracts.Vec3 * PhysicsSandbox.Shared.Contracts.Vec3) option }

/// Internal: enqueue a query response for the next state.
val internal addQueryResponse : World -> PhysicsSandbox.Shared.Contracts.QueryResponse -> unit

/// Internal: access the underlying physics world (for query handler).
val internal physicsWorld : World -> BepuFSharp.PhysicsWorld

/// Internal: access the bodies map (for query handler ID resolution).
val internal bodies : World -> Map<string, BodyRecord>

/// Internal: convert proto Shape to BepuFSharp PhysicsShape.
val internal convertShape : World -> PhysicsSandbox.Shared.Contracts.Shape -> Result<BepuFSharp.PhysicsShape * PhysicsSandbox.Shared.Contracts.Shape, string>

/// Latest physics tick time in milliseconds (updated each step)
val latestTickMs : unit -> double

/// Latest state serialization time in milliseconds (updated each step)
val latestSerializeMs : unit -> double
