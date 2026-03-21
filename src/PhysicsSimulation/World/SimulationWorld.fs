module PhysicsSimulation.SimulationWorld

open System.Diagnostics
open System.Numerics
open PhysicsSandbox.Shared.Contracts
open BepuFSharp

// Alias to avoid conflict with proto Sphere/Box types
type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box

/// <summary>
/// Internal record tracking a single rigid body in the simulation, mapping
/// between the user-facing string identifier and the underlying BepuPhysics2 handle.
/// </summary>
type BodyRecord =
    { /// <summary>User-assigned unique identifier for this body.</summary>
      Id: string
      /// <summary>BepuPhysics2 body handle (unused for static bodies).</summary>
      BepuBodyId: BodyId
      /// <summary>BepuPhysics2 collision shape handle.</summary>
      ShapeId: ShapeId
      /// <summary>Mass in kilograms. Zero for static bodies.</summary>
      Mass: float32
      /// <summary>Protobuf shape descriptor echoed back in state snapshots.</summary>
      ShapeProto: Shape
      /// <summary>Whether this body is static (immovable).</summary>
      IsStatic: bool
      /// <summary>Cached position for static bodies (not queried from Bepu each frame).</summary>
      StaticPosition: Vector3
      /// <summary>Cached orientation for static bodies.</summary>
      StaticOrientation: Quaternion }

/// <summary>
/// Mutable simulation world containing the BepuPhysics2 engine, tracked bodies,
/// persistent forces, gravity, and timing state.
/// </summary>
type World =
    { /// <summary>Underlying BepuPhysics2 physics world.</summary>
      Physics: PhysicsWorld
      /// <summary>Map of user-facing body ID to body record.</summary>
      mutable Bodies: Map<string, BodyRecord>
      /// <summary>Accumulated persistent forces per body, applied every tick.</summary>
      mutable ActiveForces: Map<string, Vec3 list>
      /// <summary>Current gravity vector applied as a force to all dynamic bodies.</summary>
      mutable Gravity: Vector3
      /// <summary>Elapsed simulation time in seconds.</summary>
      mutable SimulationTime: double
      /// <summary>Whether the simulation is actively stepping.</summary>
      mutable Running: bool
      /// <summary>Fixed time step duration in seconds (default 1/60).</summary>
      TimeStep: float32 }

let private toVector3 (v: Vec3) =
    Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let private fromVector3 (v: Vector3) =
    let r = Vec3()
    r.X <- double v.X
    r.Y <- double v.Y
    r.Z <- double v.Z
    r

let private fromQuaternion (q: Quaternion) =
    let r = Vec4()
    r.X <- double q.X
    r.Y <- double q.Y
    r.Z <- double q.Z
    r.W <- double q.W
    r

let private buildBodyProto (world: World) (record: BodyRecord) =
    let b = Body()
    b.Id <- record.Id
    b.Mass <- double record.Mass
    b.Shape <- record.ShapeProto
    b.IsStatic <- record.IsStatic
    if record.IsStatic then
        b.Position <- fromVector3 record.StaticPosition
        b.Orientation <- fromQuaternion record.StaticOrientation
        b.Velocity <- Vec3()
        b.AngularVelocity <- Vec3()
    else
        let pose = PhysicsWorld.getBodyPose record.BepuBodyId world.Physics
        let vel = PhysicsWorld.getBodyVelocity record.BepuBodyId world.Physics
        b.Position <- fromVector3 pose.Position
        b.Velocity <- fromVector3 vel.Linear
        b.AngularVelocity <- fromVector3 vel.Angular
        b.Orientation <- fromQuaternion pose.Orientation
    b

let private buildState (world: World) =
    let state = SimulationState()
    state.Time <- world.SimulationTime
    state.Running <- world.Running
    for kvp in world.Bodies do
        state.Bodies.Add(buildBodyProto world kvp.Value)
    state

let private applyAllForces (world: World) =
    let dt = world.TimeStep
    // Apply gravity as force (mass * gravity) to each dynamic body
    if world.Gravity <> Vector3.Zero then
        for kvp in world.Bodies do
            if not kvp.Value.IsStatic then
                let gravityForce = world.Gravity * kvp.Value.Mass
                PhysicsWorld.applyForce kvp.Value.BepuBodyId gravityForce dt world.Physics
    // Apply persistent per-body forces
    for kvp in world.ActiveForces do
        match Map.tryFind kvp.Key world.Bodies with
        | Some record ->
            for force in kvp.Value do
                PhysicsWorld.applyForce record.BepuBodyId (toVector3 force) dt world.Physics
        | None -> ()

/// <summary>
/// Creates a new simulation world with zero gravity, paused state, and a 60 Hz fixed time step.
/// </summary>
/// <returns>A fresh World instance ready for bodies and commands.</returns>
let create () =
    let config = { PhysicsConfig.defaults with Gravity = Vector3.Zero }
    { Physics = PhysicsWorld.create config
      Bodies = Map.empty
      ActiveForces = Map.empty
      Gravity = Vector3.Zero
      SimulationTime = 0.0
      Running = false
      TimeStep = 1.0f / 60.0f }

// Pipeline timing (module-level, updated each step)
let mutable private lastTickMs = 0.0
let mutable private lastSerializeMs = 0.0

/// <summary>
/// Returns the physics tick duration in milliseconds from the most recent simulation step.
/// </summary>
let latestTickMs () = lastTickMs

/// <summary>
/// Returns the state serialization duration in milliseconds from the most recent simulation step.
/// </summary>
let latestSerializeMs () = lastSerializeMs

/// <summary>
/// Destroys the simulation world and releases all BepuPhysics2 resources.
/// </summary>
/// <param name="world">The world to destroy.</param>
let destroy (world: World) =
    PhysicsWorld.destroy world.Physics

/// <summary>
/// Returns whether the simulation is currently running (playing).
/// </summary>
/// <param name="world">The world to query.</param>
let isRunning (world: World) = world.Running

/// <summary>
/// Returns the current simulation time in seconds.
/// </summary>
/// <param name="world">The world to query.</param>
let time (world: World) = world.SimulationTime

/// <summary>
/// Advances the simulation by one fixed time step, applying all forces and gravity,
/// then returns the new state as a protobuf SimulationState with timing diagnostics.
/// </summary>
/// <param name="world">The world to step.</param>
/// <returns>A SimulationState snapshot after the step, including tick and serialize timing.</returns>
let step (world: World) =
    let sw = Stopwatch.StartNew()
    applyAllForces world
    PhysicsWorld.step world.TimeStep world.Physics
    world.SimulationTime <- world.SimulationTime + double world.TimeStep
    sw.Stop()
    lastTickMs <- sw.Elapsed.TotalMilliseconds

    let sw2 = Stopwatch.StartNew()
    let state = buildState world
    sw2.Stop()
    lastSerializeMs <- sw2.Elapsed.TotalMilliseconds

    state.TickMs <- lastTickMs
    state.SerializeMs <- lastSerializeMs
    state

/// <summary>
/// Returns the current simulation state without advancing time. Includes the latest timing diagnostics.
/// </summary>
/// <param name="world">The world to snapshot.</param>
/// <returns>A SimulationState reflecting the current positions and velocities of all bodies.</returns>
let currentState (world: World) =
    let state = buildState world
    state.TickMs <- lastTickMs
    state.SerializeMs <- lastSerializeMs
    state

/// <summary>
/// Sets whether the simulation is running (playing) or paused.
/// </summary>
/// <param name="world">The world to update.</param>
/// <param name="running">True to play, false to pause.</param>
let setRunning (world: World) (running: bool) =
    world.Running <- running

/// <summary>
/// Adds a rigid body to the simulation. Supports spheres, boxes, and planes (approximated as
/// large static boxes). Fails if a body with the same ID already exists or mass is non-positive.
/// </summary>
/// <param name="world">The world to add the body to.</param>
/// <param name="cmd">The AddBody protobuf command specifying shape, mass, position, and velocity.</param>
/// <returns>A CommandAck indicating success or failure with a descriptive message.</returns>
let addBody (world: World) (cmd: AddBody) =
    let isPlaneShape = cmd.Shape <> null && cmd.Shape.ShapeCase = Shape.ShapeOneofCase.Plane
    if world.Bodies |> Map.containsKey cmd.Id then
        CommandAck(Success = false, Message = $"Body '{cmd.Id}' already exists")
    elif cmd.Mass <= 0.0 && not isPlaneShape then
        CommandAck(Success = false, Message = $"Mass must be positive, got {cmd.Mass}")
    else
        let mass = float32 cmd.Mass
        let pos = if cmd.Position <> null then toVector3 cmd.Position else Vector3.Zero
        let vel = if cmd.Velocity <> null then toVector3 cmd.Velocity else Vector3.Zero
        let pose = Pose.ofPosition pos
        let velocity = Velocity.create vel Vector3.Zero

        let shape, shapeProto =
            match cmd.Shape with
            | null ->
                PhysicsShape.Sphere 1.0f, Shape(Sphere = ProtoSphere(Radius = 1.0))
            | s when s.ShapeCase = Shape.ShapeOneofCase.Sphere ->
                PhysicsShape.Sphere(float32 s.Sphere.Radius), s
            | s when s.ShapeCase = Shape.ShapeOneofCase.Box ->
                let he = s.Box.HalfExtents
                let w, h, l =
                    if he <> null then float32 he.X * 2.0f, float32 he.Y * 2.0f, float32 he.Z * 2.0f
                    else 1.0f, 1.0f, 1.0f
                PhysicsShape.Box(w, h, l), s
            | s when s.ShapeCase = Shape.ShapeOneofCase.Plane ->
                // Approximate plane as large thin static box
                PhysicsShape.Box(1000.0f, 0.1f, 1000.0f), s
            | s -> PhysicsShape.Sphere 1.0f, s

        let shapeId = PhysicsWorld.addShape shape world.Physics

        let isPlane = cmd.Shape <> null && cmd.Shape.ShapeCase = Shape.ShapeOneofCase.Plane
        if isPlane then
            // Planes are static bodies — add to Bepu and track in Bodies map
            let desc = StaticBodyDesc.create shapeId pose
            let _staticId = PhysicsWorld.addStatic desc world.Physics
            let record =
                { Id = cmd.Id
                  BepuBodyId = Unchecked.defaultof<BodyId> // not used for statics
                  ShapeId = shapeId
                  Mass = 0.0f
                  ShapeProto = shapeProto
                  IsStatic = true
                  StaticPosition = pos
                  StaticOrientation = Quaternion.Identity }
            world.Bodies <- Map.add cmd.Id record world.Bodies
            CommandAck(Success = true, Message = $"Static plane '{cmd.Id}' added")
        else
            let desc =
                { DynamicBodyDesc.create shapeId pose mass with
                    Velocity = velocity }
            let bodyId = PhysicsWorld.addBody desc world.Physics
            let record =
                { Id = cmd.Id
                  BepuBodyId = bodyId
                  ShapeId = shapeId
                  Mass = mass
                  ShapeProto = shapeProto
                  IsStatic = false
                  StaticPosition = Vector3.Zero
                  StaticOrientation = Quaternion.Identity }
            world.Bodies <- Map.add cmd.Id record world.Bodies
            CommandAck(Success = true, Message = $"Body '{cmd.Id}' added")

/// <summary>
/// Removes a body by its identifier. Returns success even if the body does not exist (no-op).
/// Static bodies are untracked but remain in the Bepu engine (no remove API for statics).
/// </summary>
/// <param name="world">The world to remove the body from.</param>
/// <param name="id">The user-facing body identifier.</param>
/// <returns>A CommandAck indicating the result.</returns>
let removeBody (world: World) (id: string) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        if not record.IsStatic then
            PhysicsWorld.removeBody record.BepuBodyId world.Physics
        // Static bodies remain in Bepu (no removeStatic API) but are untracked
        world.Bodies <- Map.remove id world.Bodies
        world.ActiveForces <- Map.remove id world.ActiveForces
        CommandAck(Success = true, Message = $"Body '{id}' removed")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// <summary>
/// Adds a persistent force to a body. Forces accumulate and are applied every simulation step
/// until explicitly cleared with clearForces.
/// </summary>
/// <param name="world">The simulation world.</param>
/// <param name="id">Target body identifier.</param>
/// <param name="force">Force vector to apply persistently.</param>
/// <returns>A CommandAck indicating the result.</returns>
let applyForce (world: World) (id: string) (force: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some _ ->
        let existing = Map.tryFind id world.ActiveForces |> Option.defaultValue []
        world.ActiveForces <- Map.add id (force :: existing) world.ActiveForces
        CommandAck(Success = true, Message = $"Force applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// <summary>
/// Applies a one-shot linear impulse to a body, immediately changing its velocity.
/// Unlike forces, impulses are not stored and only take effect once.
/// </summary>
/// <param name="world">The simulation world.</param>
/// <param name="id">Target body identifier.</param>
/// <param name="impulse">Impulse vector to apply.</param>
/// <returns>A CommandAck indicating the result.</returns>
let applyImpulse (world: World) (id: string) (impulse: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyLinearImpulse record.BepuBodyId (toVector3 impulse) world.Physics
        CommandAck(Success = true, Message = $"Impulse applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// <summary>
/// Applies a rotational torque to a body for a single time step.
/// </summary>
/// <param name="world">The simulation world.</param>
/// <param name="id">Target body identifier.</param>
/// <param name="torque">Torque vector to apply.</param>
/// <returns>A CommandAck indicating the result.</returns>
let applyTorque (world: World) (id: string) (torque: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyTorque record.BepuBodyId (toVector3 torque) world.TimeStep world.Physics
        CommandAck(Success = true, Message = $"Torque applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// <summary>
/// Removes all persistent forces from a body. Succeeds even if no forces were active.
/// </summary>
/// <param name="world">The simulation world.</param>
/// <param name="id">Target body identifier.</param>
/// <returns>A CommandAck confirming forces were cleared.</returns>
let clearForces (world: World) (id: string) =
    world.ActiveForces <- Map.remove id world.ActiveForces
    CommandAck(Success = true, Message = $"Forces cleared for '{id}'")

/// <summary>
/// Sets the global gravity vector. Gravity is applied as a force (mass * gravity) to all
/// dynamic bodies each simulation step.
/// </summary>
/// <param name="world">The simulation world.</param>
/// <param name="gravity">The new gravity vector.</param>
let setGravity (world: World) (gravity: Vec3) =
    world.Gravity <- toVector3 gravity

/// <summary>
/// Resets the simulation to its initial state: removes all dynamic bodies from the physics
/// engine, clears all tracked bodies and forces, resets time to zero, and pauses the simulation.
/// </summary>
/// <param name="world">The world to reset.</param>
/// <returns>A CommandAck confirming the reset.</returns>
let resetSimulation (world: World) =
    // Remove all dynamic bodies from the physics engine (static bodies have no remove API)
    for kvp in world.Bodies do
        if not kvp.Value.IsStatic then
            PhysicsWorld.removeBody kvp.Value.BepuBodyId world.Physics
    world.Bodies <- Map.empty
    world.ActiveForces <- Map.empty
    world.SimulationTime <- 0.0
    world.Running <- false
    CommandAck(Success = true, Message = "Simulation reset")
