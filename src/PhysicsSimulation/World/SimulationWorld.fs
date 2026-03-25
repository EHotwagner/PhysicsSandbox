module PhysicsSimulation.SimulationWorld

open System.Diagnostics
open System.Numerics
open PhysicsSandbox.Shared.Contracts
open BepuFSharp
open PhysicsSimulation.ProtoConversions
open PhysicsSimulation.ShapeConversion

/// Internal record tracking a single rigid body in the simulation.
type BodyRecord =
    { Id: string
      BepuBodyId: BodyId
      BepuStaticId: StaticId
      ShapeId: ShapeId
      Mass: float32
      ShapeProto: Shape
      IsStatic: bool
      MotionType: BodyMotionType
      StaticPosition: Vector3
      StaticOrientation: Quaternion
      Color: ProtoColor option
      Material: ProtoMaterialProperties option
      CollisionGroup: uint32
      CollisionMask: uint32
      MeshId: string option
      BoundingBox: (Vec3 * Vec3) option }

/// Internal record tracking a constraint between two bodies.
type ConstraintRecord =
    { Id: string
      BepuConstraintId: ConstraintId
      BodyA: string
      BodyB: string
      TypeProto: ConstraintType }

/// Mutable simulation world containing the BepuPhysics2 engine, tracked bodies,
/// persistent forces, gravity, and timing state.
type World =
    { Physics: PhysicsWorld
      mutable Bodies: Map<string, BodyRecord>
      mutable Constraints: Map<string, ConstraintRecord>
      mutable RegisteredShapes: Map<string, ShapeId * Shape>
      mutable ActiveForces: Map<string, Vec3 list>
      mutable Gravity: Vector3
      mutable SimulationTime: double
      mutable Running: bool
      TimeStep: float32
      mutable PendingQueryResponses: QueryResponse list
      mutable EmittedMeshIds: Set<string> }

let private buildBodyProto (world: World) (record: BodyRecord) =
    let b = Body()
    b.Id <- record.Id
    b.Mass <- double record.Mass
    // For complex shapes with a MeshId, emit CachedShapeRef instead of inline geometry
    match record.MeshId, record.BoundingBox with
    | Some meshId, Some (bboxMin, bboxMax) ->
        let cached = ProtoCachedShapeRef()
        cached.MeshId <- meshId
        cached.BboxMin <- bboxMin
        cached.BboxMax <- bboxMax
        b.Shape <- Shape(CachedRef = cached)
    | _ ->
        b.Shape <- record.ShapeProto
    b.IsStatic <- record.IsStatic
    b.MotionType <- record.MotionType
    b.CollisionGroup <- record.CollisionGroup
    b.CollisionMask <- record.CollisionMask
    match record.Color with
    | Some c -> b.Color <- c
    | None -> ()
    match record.Material with
    | Some m -> b.Material <- m
    | None -> ()
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

let private buildConstraintStateProto (record: ConstraintRecord) =
    let cs = ConstraintState()
    cs.Id <- record.Id
    cs.BodyA <- record.BodyA
    cs.BodyB <- record.BodyB
    cs.Type <- record.TypeProto
    cs

let private buildState (world: World) =
    let state = SimulationState()
    state.Time <- world.SimulationTime
    state.Running <- world.Running
    // Collect mesh IDs not yet emitted for new_meshes
    let mutable newMeshIds = Set.empty<string>
    for kvp in world.Bodies do
        state.Bodies.Add(buildBodyProto world kvp.Value)
        match kvp.Value.MeshId with
        | Some meshId when not (Set.contains meshId world.EmittedMeshIds) && not (Set.contains meshId newMeshIds) ->
            let mg = MeshGeometry()
            mg.MeshId <- meshId
            mg.Shape <- kvp.Value.ShapeProto
            state.NewMeshes.Add(mg)
            newMeshIds <- Set.add meshId newMeshIds
        | _ -> ()
    // Update emitted set
    world.EmittedMeshIds <- Set.union world.EmittedMeshIds newMeshIds
    for kvp in world.Constraints do
        state.Constraints.Add(buildConstraintStateProto kvp.Value)
    for kvp in world.RegisteredShapes do
        let rs = RegisteredShapeState()
        rs.ShapeHandle <- kvp.Key
        rs.Shape <- snd kvp.Value
        state.RegisteredShapes.Add(rs)
    // Attach pending query responses
    for qr in world.PendingQueryResponses do
        state.QueryResponses.Add(qr)
    world.PendingQueryResponses <- []
    state

let private applyAllForces (world: World) =
    let dt = world.TimeStep
    // Apply gravity as force (mass * gravity) to each dynamic body
    if world.Gravity <> Vector3.Zero then
        for kvp in world.Bodies do
            if not kvp.Value.IsStatic && kvp.Value.MotionType <> BodyMotionType.Kinematic then
                let gravityForce = world.Gravity * kvp.Value.Mass
                PhysicsWorld.applyForce kvp.Value.BepuBodyId gravityForce dt world.Physics
    // Apply persistent per-body forces
    for kvp in world.ActiveForces do
        match Map.tryFind kvp.Key world.Bodies with
        | Some record ->
            for force in kvp.Value do
                PhysicsWorld.applyForce record.BepuBodyId (toVector3 force) dt world.Physics
        | None -> ()

/// Delegates to ShapeConversion.convertShape with world dependencies.
let internal convertShape (world: World) (s: Shape) =
    ShapeConversion.convertShape world.Physics world.RegisteredShapes s

/// Creates a new simulation world with zero gravity, paused state, and a 60 Hz fixed time step.
let create () =
    let config = { PhysicsConfig.defaults with Gravity = Vector3.Zero }
    { Physics = PhysicsWorld.create config
      Bodies = Map.empty
      Constraints = Map.empty
      RegisteredShapes = Map.empty
      ActiveForces = Map.empty
      Gravity = Vector3.Zero
      SimulationTime = 0.0
      Running = false
      TimeStep = 1.0f / 60.0f
      PendingQueryResponses = []
      EmittedMeshIds = Set.empty }

// Pipeline timing (module-level, updated each step)
let mutable private lastTickMs = 0.0
let mutable private lastSerializeMs = 0.0

/// Returns the physics tick duration in milliseconds from the most recent simulation step.
let latestTickMs () = lastTickMs

/// Returns the state serialization duration in milliseconds from the most recent simulation step.
let latestSerializeMs () = lastSerializeMs

/// Destroys the simulation world and releases all BepuPhysics2 resources.
let destroy (world: World) =
    PhysicsWorld.destroy world.Physics

/// Returns whether the simulation is currently running (playing).
let isRunning (world: World) = world.Running

/// Returns the current simulation time in seconds.
let time (world: World) = world.SimulationTime

/// Advances the simulation by one fixed time step.
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

/// Returns the current simulation state without advancing time.
let currentState (world: World) =
    let state = buildState world
    state.TickMs <- lastTickMs
    state.SerializeMs <- lastSerializeMs
    state

/// Sets whether the simulation is running (playing) or paused.
let setRunning (world: World) (running: bool) =
    world.Running <- running

/// Adds a rigid body to the simulation. Supports all 10 shape types plus shape references.
let addBody (world: World) (cmd: AddBody) =
    let isPlaneShape = cmd.Shape <> null && cmd.Shape.ShapeCase = Shape.ShapeOneofCase.Plane
    let isShapeRef = cmd.Shape <> null && cmd.Shape.ShapeCase = Shape.ShapeOneofCase.ShapeRef

    if world.Bodies |> Map.containsKey cmd.Id then
        CommandAck(Success = false, Message = $"Body '{cmd.Id}' already exists")
    else

    // Determine motion type: explicit motion_type field, or legacy heuristic
    let motionType =
        match cmd.MotionType with
        | BodyMotionType.Static -> BodyMotionType.Static
        | BodyMotionType.Kinematic -> BodyMotionType.Kinematic
        | _ -> // DYNAMIC (default/0)
            if isPlaneShape then BodyMotionType.Static
            else BodyMotionType.Dynamic

    if motionType = BodyMotionType.Dynamic && cmd.Mass <= 0.0 then
        CommandAck(Success = false, Message = $"Mass must be positive, got {cmd.Mass}")
    else

    match convertShape world cmd.Shape with
    | Error msg ->
        CommandAck(Success = false, Message = msg)
    | Ok (physShape, shapeProto) ->

    // Compute mesh ID and bounding box for complex shapes (one-time on addition)
    let meshId = MeshIdGenerator.computeMeshId shapeProto
    let boundingBox = MeshIdGenerator.computeBoundingBox shapeProto

    let mass = float32 cmd.Mass
    let pos = if cmd.Position <> null then toVector3 cmd.Position else Vector3.Zero
    let vel = if cmd.Velocity <> null then toVector3 cmd.Velocity else Vector3.Zero
    let angVel = if cmd.AngularVelocity <> null then toVector3 cmd.AngularVelocity else Vector3.Zero
    let ori = if cmd.Orientation <> null then toQuaternion cmd.Orientation else Quaternion.Identity
    let pose = Pose.create pos ori
    let velocity = Velocity.create vel angVel

    let collisionGroup = if cmd.CollisionGroup = 0u then 1u else cmd.CollisionGroup
    let collisionMask = if cmd.CollisionMask = 0u then 0xFFFFFFFFu else cmd.CollisionMask

    let color = if isNull cmd.Color then None else Some cmd.Color
    let material = if isNull cmd.Material then None else Some cmd.Material

    // Use pre-registered ShapeId for shape references, otherwise add new shape
    let shapeId =
        if isShapeRef then
            match Map.tryFind cmd.Shape.ShapeRef.ShapeHandle world.RegisteredShapes with
            | Some (sid, _) -> sid
            | None -> PhysicsWorld.addShape physShape world.Physics // fallback
        else
            PhysicsWorld.addShape physShape world.Physics

    let bepuMat =
        match material with
        | Some m -> toBepuMaterial m
        | None -> BepuFSharp.MaterialProperties.defaults

    match motionType with
    | BodyMotionType.Static ->
        let desc =
            { StaticBodyDesc.create shapeId pose with
                Material = bepuMat
                CollisionGroup = collisionGroup
                CollisionMask = collisionMask }
        let staticId = PhysicsWorld.addStatic desc world.Physics
        let record =
            { Id = cmd.Id
              BepuBodyId = Unchecked.defaultof<BodyId>
              BepuStaticId = staticId
              ShapeId = shapeId
              Mass = 0.0f
              ShapeProto = shapeProto
              IsStatic = true
              MotionType = BodyMotionType.Static
              StaticPosition = pos
              StaticOrientation = ori
              Color = color
              Material = material
              CollisionGroup = collisionGroup
              CollisionMask = collisionMask
              MeshId = meshId
              BoundingBox = boundingBox }
        world.Bodies <- Map.add cmd.Id record world.Bodies
        CommandAck(Success = true, Message = $"Static body '{cmd.Id}' added")

    | BodyMotionType.Kinematic ->
        let desc =
            { KinematicBodyDesc.create shapeId pose with
                Velocity = velocity
                Material = bepuMat
                CollisionGroup = collisionGroup
                CollisionMask = collisionMask }
        let bodyId = PhysicsWorld.addKinematicBody desc world.Physics
        let record =
            { Id = cmd.Id
              BepuBodyId = bodyId
              BepuStaticId = Unchecked.defaultof<StaticId>
              ShapeId = shapeId
              Mass = 0.0f
              ShapeProto = shapeProto
              IsStatic = false
              MotionType = BodyMotionType.Kinematic
              StaticPosition = Vector3.Zero
              StaticOrientation = Quaternion.Identity
              Color = color
              Material = material
              CollisionGroup = collisionGroup
              CollisionMask = collisionMask
              MeshId = meshId
              BoundingBox = boundingBox }
        world.Bodies <- Map.add cmd.Id record world.Bodies
        CommandAck(Success = true, Message = $"Kinematic body '{cmd.Id}' added")

    | _ -> // Dynamic
        let desc =
            { DynamicBodyDesc.create shapeId pose mass with
                Velocity = velocity
                Material = bepuMat
                CollisionGroup = collisionGroup
                CollisionMask = collisionMask }
        let bodyId = PhysicsWorld.addBody desc world.Physics
        let record =
            { Id = cmd.Id
              BepuBodyId = bodyId
              BepuStaticId = Unchecked.defaultof<StaticId>
              ShapeId = shapeId
              Mass = mass
              ShapeProto = shapeProto
              IsStatic = false
              MotionType = BodyMotionType.Dynamic
              StaticPosition = Vector3.Zero
              StaticOrientation = Quaternion.Identity
              Color = color
              Material = material
              CollisionGroup = collisionGroup
              CollisionMask = collisionMask
              MeshId = meshId
              BoundingBox = boundingBox }
        world.Bodies <- Map.add cmd.Id record world.Bodies
        CommandAck(Success = true, Message = $"Body '{cmd.Id}' added")

/// Removes a body by its identifier. Also auto-removes any constraints referencing it.
let removeBody (world: World) (id: string) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        // Auto-remove constraints referencing this body
        let constraintsToRemove =
            world.Constraints
            |> Map.filter (fun _ cr -> cr.BodyA = id || cr.BodyB = id)
        for kvp in constraintsToRemove do
            PhysicsWorld.removeConstraint kvp.Value.BepuConstraintId world.Physics
            world.Constraints <- Map.remove kvp.Key world.Constraints

        if record.IsStatic then
            PhysicsWorld.removeStatic record.BepuStaticId world.Physics
        else
            PhysicsWorld.removeBody record.BepuBodyId world.Physics
        world.Bodies <- Map.remove id world.Bodies
        world.ActiveForces <- Map.remove id world.ActiveForces
        CommandAck(Success = true, Message = $"Body '{id}' removed")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// Adds a persistent force to a body.
let applyForce (world: World) (id: string) (force: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some _ ->
        let existing = Map.tryFind id world.ActiveForces |> Option.defaultValue []
        world.ActiveForces <- Map.add id (force :: existing) world.ActiveForces
        CommandAck(Success = true, Message = $"Force applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// Applies a one-shot linear impulse to a body.
let applyImpulse (world: World) (id: string) (impulse: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyLinearImpulse record.BepuBodyId (toVector3 impulse) world.Physics
        CommandAck(Success = true, Message = $"Impulse applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// Applies a rotational torque to a body.
let applyTorque (world: World) (id: string) (torque: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyTorque record.BepuBodyId (toVector3 torque) world.TimeStep world.Physics
        CommandAck(Success = true, Message = $"Torque applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

/// Removes all persistent forces from a body.
let clearForces (world: World) (id: string) =
    world.ActiveForces <- Map.remove id world.ActiveForces
    CommandAck(Success = true, Message = $"Forces cleared for '{id}'")

/// Sets the global gravity vector.
let setGravity (world: World) (gravity: Vec3) =
    world.Gravity <- toVector3 gravity

/// Registers a named shape for later reference by AddBody commands.
let registerShape (world: World) (cmd: RegisterShape) =
    if Map.containsKey cmd.ShapeHandle world.RegisteredShapes then
        CommandAck(Success = false, Message = $"Shape handle '{cmd.ShapeHandle}' already registered")
    else
        match convertShape world cmd.Shape with
        | Error msg ->
            CommandAck(Success = false, Message = msg)
        | Ok (physShape, shapeProto) ->
            let shapeId = PhysicsWorld.addShape physShape world.Physics
            world.RegisteredShapes <- Map.add cmd.ShapeHandle (shapeId, shapeProto) world.RegisteredShapes
            CommandAck(Success = true, Message = $"Shape '{cmd.ShapeHandle}' registered")

/// Unregisters a named shape.
let unregisterShape (world: World) (handle: string) =
    match Map.tryFind handle world.RegisteredShapes with
    | Some _ ->
        world.RegisteredShapes <- Map.remove handle world.RegisteredShapes
        CommandAck(Success = true, Message = $"Shape '{handle}' unregistered")
    | None ->
        CommandAck(Success = true, Message = $"Shape '{handle}' not found (no-op)")


/// Adds a constraint between two bodies.
let addConstraint (world: World) (cmd: AddConstraint) =
    if Map.containsKey cmd.Id world.Constraints then
        CommandAck(Success = false, Message = $"Constraint '{cmd.Id}' already exists")
    else
    match Map.tryFind cmd.BodyA world.Bodies, Map.tryFind cmd.BodyB world.Bodies with
    | Some bodyA, Some bodyB ->
        if bodyA.IsStatic || bodyB.IsStatic then
            CommandAck(Success = false, Message = "Cannot add constraint to static bodies")
        else
        match convertConstraintType cmd.Type with
        | None ->
            CommandAck(Success = false, Message = "Unknown or empty constraint type")
        | Some desc ->
            let constraintId = PhysicsWorld.addConstraint bodyA.BepuBodyId bodyB.BepuBodyId desc world.Physics
            let record =
                { Id = cmd.Id
                  BepuConstraintId = constraintId
                  BodyA = cmd.BodyA
                  BodyB = cmd.BodyB
                  TypeProto = cmd.Type }
            world.Constraints <- Map.add cmd.Id record world.Constraints
            CommandAck(Success = true, Message = $"Constraint '{cmd.Id}' added")
    | None, _ ->
        CommandAck(Success = false, Message = $"Body '{cmd.BodyA}' not found")
    | _, None ->
        CommandAck(Success = false, Message = $"Body '{cmd.BodyB}' not found")

/// Removes a constraint by its identifier.
let removeConstraint (world: World) (id: string) =
    match Map.tryFind id world.Constraints with
    | Some record ->
        PhysicsWorld.removeConstraint record.BepuConstraintId world.Physics
        world.Constraints <- Map.remove id world.Constraints
        CommandAck(Success = true, Message = $"Constraint '{id}' removed")
    | None ->
        CommandAck(Success = true, Message = $"Constraint '{id}' not found (no-op)")

/// Sets collision filter on a body at runtime.
let setCollisionFilter (world: World) (cmd: SetCollisionFilter) =
    match Map.tryFind cmd.BodyId world.Bodies with
    | Some record ->
        let filter = { Group = cmd.CollisionGroup; Mask = cmd.CollisionMask }
        if record.IsStatic then
            PhysicsWorld.setStaticCollisionFilter record.BepuStaticId filter world.Physics
        else
            PhysicsWorld.setCollisionFilter record.BepuBodyId filter world.Physics
        let updated = { record with CollisionGroup = cmd.CollisionGroup; CollisionMask = cmd.CollisionMask }
        world.Bodies <- Map.add cmd.BodyId updated world.Bodies
        CommandAck(Success = true, Message = $"Collision filter updated for '{cmd.BodyId}'")
    | None ->
        CommandAck(Success = false, Message = $"Body '{cmd.BodyId}' not found")

/// Sets the position, orientation, and/or velocity of a body at runtime.
let setBodyPose (world: World) (cmd: SetBodyPose) =
    match Map.tryFind cmd.BodyId world.Bodies with
    | Some record ->
        if record.IsStatic then
            CommandAck(Success = false, Message = $"Cannot set pose on static body '{cmd.BodyId}'")
        else
            let pos = if cmd.Position <> null then toVector3 cmd.Position else Vector3.Zero
            let ori = if cmd.Orientation <> null then toQuaternion cmd.Orientation else Quaternion.Identity
            let vel = if cmd.Velocity <> null then toVector3 cmd.Velocity else Vector3.Zero
            let angVel = if cmd.AngularVelocity <> null then toVector3 cmd.AngularVelocity else Vector3.Zero
            let pose = Pose.create pos ori
            let velocity = Velocity.create vel angVel
            PhysicsWorld.setBodyPose record.BepuBodyId pose world.Physics
            PhysicsWorld.setBodyVelocity record.BepuBodyId velocity world.Physics
            CommandAck(Success = true, Message = $"Pose updated for '{cmd.BodyId}'")
    | None ->
        CommandAck(Success = false, Message = $"Body '{cmd.BodyId}' not found")

/// Enqueue a query response to be sent with the next state.
let internal addQueryResponse (world: World) (response: QueryResponse) =
    world.PendingQueryResponses <- response :: world.PendingQueryResponses

/// Access the underlying physics world (internal, for query handler).
let internal physicsWorld (world: World) = world.Physics

/// Access the bodies map (internal, for query handler ID resolution).
let internal bodies (world: World) = world.Bodies

/// Resets the simulation to its initial state.
let resetSimulation (world: World) =
    // Remove all constraints
    for kvp in world.Constraints do
        PhysicsWorld.removeConstraint kvp.Value.BepuConstraintId world.Physics
    world.Constraints <- Map.empty
    // Remove all bodies
    for kvp in world.Bodies do
        if kvp.Value.IsStatic then
            PhysicsWorld.removeStatic kvp.Value.BepuStaticId world.Physics
        else
            PhysicsWorld.removeBody kvp.Value.BepuBodyId world.Physics
    world.Bodies <- Map.empty
    world.RegisteredShapes <- Map.empty
    world.ActiveForces <- Map.empty
    world.EmittedMeshIds <- Set.empty
    world.SimulationTime <- 0.0
    world.Running <- false
    CommandAck(Success = true, Message = "Simulation reset")
