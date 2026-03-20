module PhysicsSimulation.SimulationWorld

open System.Numerics
open PhysicsSandbox.Shared.Contracts
open BepuFSharp

// Alias to avoid conflict with proto Sphere/Box types
type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box

type BodyRecord =
    { Id: string
      BepuBodyId: BodyId
      ShapeId: ShapeId
      Mass: float32
      ShapeProto: Shape }

type World =
    { Physics: PhysicsWorld
      mutable Bodies: Map<string, BodyRecord>
      mutable ActiveForces: Map<string, Vec3 list>
      mutable Gravity: Vector3
      mutable SimulationTime: double
      mutable Running: bool
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
    let pose = PhysicsWorld.getBodyPose record.BepuBodyId world.Physics
    let vel = PhysicsWorld.getBodyVelocity record.BepuBodyId world.Physics
    let b = Body()
    b.Id <- record.Id
    b.Position <- fromVector3 pose.Position
    b.Velocity <- fromVector3 vel.Linear
    b.AngularVelocity <- fromVector3 vel.Angular
    b.Mass <- double record.Mass
    b.Shape <- record.ShapeProto
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
            let gravityForce = world.Gravity * kvp.Value.Mass
            PhysicsWorld.applyForce kvp.Value.BepuBodyId gravityForce dt world.Physics
    // Apply persistent per-body forces
    for kvp in world.ActiveForces do
        match Map.tryFind kvp.Key world.Bodies with
        | Some record ->
            for force in kvp.Value do
                PhysicsWorld.applyForce record.BepuBodyId (toVector3 force) dt world.Physics
        | None -> ()

let create () =
    let config = { PhysicsConfig.defaults with Gravity = Vector3.Zero }
    { Physics = PhysicsWorld.create config
      Bodies = Map.empty
      ActiveForces = Map.empty
      Gravity = Vector3.Zero
      SimulationTime = 0.0
      Running = false
      TimeStep = 1.0f / 60.0f }

let destroy (world: World) =
    PhysicsWorld.destroy world.Physics

let isRunning (world: World) = world.Running

let time (world: World) = world.SimulationTime

let step (world: World) =
    applyAllForces world
    PhysicsWorld.step world.TimeStep world.Physics
    world.SimulationTime <- world.SimulationTime + double world.TimeStep
    buildState world

let currentState (world: World) =
    buildState world

let setRunning (world: World) (running: bool) =
    world.Running <- running

let addBody (world: World) (cmd: AddBody) =
    if world.Bodies |> Map.containsKey cmd.Id then
        CommandAck(Success = false, Message = $"Body '{cmd.Id}' already exists")
    elif cmd.Mass <= 0.0 then
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
            // Planes are static bodies
            let desc = StaticBodyDesc.create shapeId pose
            let _staticId = PhysicsWorld.addStatic desc world.Physics
            // We still track it but with a dummy BodyId — static bodies can't have forces
            // For simplicity, skip tracking static bodies in the Bodies map for now
            // since they can't receive forces/impulses
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
                  ShapeProto = shapeProto }
            world.Bodies <- Map.add cmd.Id record world.Bodies
            CommandAck(Success = true, Message = $"Body '{cmd.Id}' added")

let removeBody (world: World) (id: string) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.removeBody record.BepuBodyId world.Physics
        world.Bodies <- Map.remove id world.Bodies
        world.ActiveForces <- Map.remove id world.ActiveForces
        CommandAck(Success = true, Message = $"Body '{id}' removed")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

let applyForce (world: World) (id: string) (force: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some _ ->
        let existing = Map.tryFind id world.ActiveForces |> Option.defaultValue []
        world.ActiveForces <- Map.add id (force :: existing) world.ActiveForces
        CommandAck(Success = true, Message = $"Force applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

let applyImpulse (world: World) (id: string) (impulse: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyLinearImpulse record.BepuBodyId (toVector3 impulse) world.Physics
        CommandAck(Success = true, Message = $"Impulse applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

let applyTorque (world: World) (id: string) (torque: Vec3) =
    match Map.tryFind id world.Bodies with
    | Some record ->
        PhysicsWorld.applyTorque record.BepuBodyId (toVector3 torque) world.TimeStep world.Physics
        CommandAck(Success = true, Message = $"Torque applied to '{id}'")
    | None ->
        CommandAck(Success = true, Message = $"Body '{id}' not found (no-op)")

let clearForces (world: World) (id: string) =
    world.ActiveForces <- Map.remove id world.ActiveForces
    CommandAck(Success = true, Message = $"Forces cleared for '{id}'")

let setGravity (world: World) (gravity: Vec3) =
    world.Gravity <- toVector3 gravity
