// Shared preamble for all demo scripts
// Usage: #load "Prelude.fsx" at the top of any demo script

#r "nuget: PhysicsClient, 0.4.0"
#r "nuget: Microsoft.Extensions.Logging.Abstractions"
#r "nuget: Spectre.Console"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let ok r = r |> Result.defaultWith (fun e -> failwith e)
let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

let runFor (s: Session) (seconds: float) =
    play s |> ignore
    sleep (int (seconds * 1000.0))
    pause s |> ignore

let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x; v.Y <- y; v.Z <- z
    v

let resetSimulation (s: Session) =
    pause s |> ignore
    clearAll s |> ignore
    try
        reset s |> ok
    with ex ->
        printfn "  [RESET ERROR] %s — falling back to manual clear" ex.Message
    PhysicsClient.IdGenerator.reset ()
    addPlane s None None |> ignore
    setGravity s (0.0, -9.81, 0.0) |> ignore
    sleep 100

let nextId prefix = PhysicsClient.IdGenerator.nextId prefix

let makeSphereCmd (id: string) (pos: float * float * float) (radius: float) (mass: float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeBoxCmd (id: string) (pos: float * float * float) (halfExtents: float * float * float) (mass: float) =
    let box = Box()
    box.HalfExtents <- toVec3 halfExtents
    let shape = Shape()
    shape.Box <- box
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeImpulseCmd (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    cmd

let makeTorqueCmd (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    cmd

let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result

let batchAdd (s: Session) (commands: SimulationCommand list) =
    let chunks = commands |> List.chunkBySize 100
    for chunk in chunks do
        let response = batchCommands s chunk |> ok
        let failures = response.Results |> Seq.filter (fun r -> not r.Success) |> Seq.toList
        if failures.Length > 0 then
            for f in failures do
                printfn "  [BATCH FAIL] command %d: %s" f.Index f.Message

// ─── New Shape Commands ──────────────────────────────────────────────────

let makeCapsuleCmd (id: string) (pos: float * float * float) (radius: float) (length: float) (mass: float) =
    let capsule = Capsule()
    capsule.Radius <- radius
    capsule.Length <- length
    let shape = Shape()
    shape.Capsule <- capsule
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeCylinderCmd (id: string) (pos: float * float * float) (radius: float) (length: float) (mass: float) =
    let cylinder = Cylinder()
    cylinder.Radius <- radius
    cylinder.Length <- length
    let shape = Shape()
    shape.Cylinder <- cylinder
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

// ─── Color & Material Helpers ───────────────────────────────────────────

let makeColor (r: float) (g: float) (b: float) (a: float) =
    let c = Color()
    c.R <- r; c.G <- g; c.B <- b; c.A <- a
    c

let makeMaterialProperties (friction: float) (maxRecovery: float) (springFreq: float) (springDamping: float) =
    let m = MaterialProperties()
    m.Friction <- friction
    m.MaxRecoveryVelocity <- maxRecovery
    m.SpringFrequency <- springFreq
    m.SpringDampingRatio <- springDamping
    m

let bouncyMaterial = makeMaterialProperties 0.4 8.0 60.0 0.5
let stickyMaterial = makeMaterialProperties 2.0 0.5 30.0 1.0
let slipperyMaterial = makeMaterialProperties 0.01 2.0 30.0 1.0

/// Apply color and/or material to an AddBody SimulationCommand
let withColorAndMaterial (color: Color option) (material: MaterialProperties option) (cmd: SimulationCommand) =
    color |> Option.iter (fun c -> cmd.AddBody.Color <- c)
    material |> Option.iter (fun m -> cmd.AddBody.Material <- m)
    cmd

// ─── Constraint Helpers ─────────────────────────────────────────────────

let makeBallSocketCmd (id: string) (bodyA: string) (bodyB: string) (offsetA: float * float * float) (offsetB: float * float * float) =
    let ct = ConstraintType()
    ct.BallSocket <- BallSocketConstraint()
    ct.BallSocket.LocalOffsetA <- toVec3 offsetA
    ct.BallSocket.LocalOffsetB <- toVec3 offsetB
    let ac = AddConstraint()
    ac.Id <- id; ac.BodyA <- bodyA; ac.BodyB <- bodyB; ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

let makeHingeCmd (id: string) (bodyA: string) (bodyB: string) (axis: float * float * float) (offsetA: float * float * float) (offsetB: float * float * float) =
    let ct = ConstraintType()
    ct.Hinge <- HingeConstraint()
    ct.Hinge.LocalHingeAxisA <- toVec3 axis
    ct.Hinge.LocalHingeAxisB <- toVec3 axis
    ct.Hinge.LocalOffsetA <- toVec3 offsetA
    ct.Hinge.LocalOffsetB <- toVec3 offsetB
    let ac = AddConstraint()
    ac.Id <- id; ac.BodyA <- bodyA; ac.BodyB <- bodyB; ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

// ─── Color Palette ────────────────────────────────────────────────────

let projectileColor = makeColor 1.0 0.2 0.1 1.0   // Red
let targetColor     = makeColor 0.3 0.6 1.0 1.0   // Blue
let structureColor  = makeColor 0.7 0.7 0.7 1.0   // Gray
let accentYellow    = makeColor 1.0 0.8 0.0 1.0   // Yellow
let accentGreen     = makeColor 0.2 0.8 0.3 1.0   // Green
let accentPurple    = makeColor 0.8 0.4 1.0 1.0   // Purple
let accentOrange    = makeColor 1.0 0.5 0.0 1.0   // Orange
let kinematicColor  = makeColor 0.0 1.0 1.0 1.0   // Cyan

// ─── Advanced Shape Commands ──────────────────────────────────────────

let makeTriangleCmd (id: string) (pos: float * float * float) (a: float * float * float) (b: float * float * float) (c: float * float * float) (mass: float) =
    let tri = Triangle()
    tri.A <- toVec3 a; tri.B <- toVec3 b; tri.C <- toVec3 c
    let shape = Shape()
    shape.Triangle <- tri
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeConvexHullCmd (id: string) (pos: float * float * float) (points: (float * float * float) list) (mass: float) =
    let hull = ConvexHull()
    for p in points do hull.Points.Add(toVec3 p)
    let shape = Shape()
    shape.ConvexHull <- hull
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeCompoundCmd (id: string) (pos: float * float * float) (children: (Shape * (float * float * float)) list) (mass: float) =
    let compound = Compound()
    for (childShape, localPos) in children do
        let child = CompoundChild()
        child.Shape <- childShape
        child.LocalPosition <- toVec3 localPos
        let orient = Vec4()
        orient.W <- 1.0
        child.LocalOrientation <- orient
        compound.Children.Add(child)
    let shape = Shape()
    shape.Compound <- compound
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeMeshCmd (id: string) (pos: float * float * float) (triangles: ((float * float * float) * (float * float * float) * (float * float * float)) list) (mass: float) =
    let mesh = MeshShape()
    for (a, b, c) in triangles do
        let tri = MeshTriangle()
        tri.A <- toVec3 a; tri.B <- toVec3 b; tri.C <- toVec3 c
        mesh.Triangles.Add(tri)
    let shape = Shape()
    shape.Mesh <- mesh
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

// ─── Kinematic & Filter Helpers ───────────────────────────────────────

let withMotionType (mt: BodyMotionType) (cmd: SimulationCommand) =
    cmd.AddBody.MotionType <- mt
    cmd

let withCollisionFilter (group: uint32) (mask: uint32) (cmd: SimulationCommand) =
    cmd.AddBody.CollisionGroup <- group
    cmd.AddBody.CollisionMask <- mask
    cmd

let makeKinematicCmd (id: string) (pos: float * float * float) (shape: Shape) =
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- 0.0
    body.MotionType <- BodyMotionType.Kinematic
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let setPose (s: Session) (bodyId: string) (pos: float * float * float) =
    setBodyPose s bodyId pos None None None |> ok

// ─── Query Helpers ────────────────────────────────────────────────────

let queryRaycast (s: Session) (origin: float * float * float) (direction: float * float * float) (maxDist: float) =
    let resp = raycast s origin direction maxDist false None |> ok
    if resp.Hit then
        resp.Hits |> Seq.map (fun h -> (h.BodyId, (h.Position.X, h.Position.Y, h.Position.Z), (h.Normal.X, h.Normal.Y, h.Normal.Z), h.Distance)) |> Seq.toList
    else []

let queryOverlapSphere (s: Session) (radius: float) (position: float * float * float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let resp = overlap s shape position None None |> ok
    resp.BodyIds |> Seq.toList

let querySweepSphere (s: Session) (radius: float) (startPos: float * float * float) (direction: float * float * float) (maxDist: float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let resp = sweepCast s shape startPos direction maxDist None None |> ok
    if resp.Hit then
        let h = resp.Closest
        Some (h.BodyId, (h.Position.X, h.Position.Y, h.Position.Z), (h.Normal.X, h.Normal.Y, h.Normal.Z), h.Distance)
    else None

// ─── Demo Info ───────────────────────────────────────────────────────

let setDemoInfo (s: Session) (name: string) (description: string) =
    setDemoMetadata s name description |> ignore

// ─── Camera & Narration Helpers ──────────────────────────────────────

let smoothCamera (s: Session) (pos: float * float * float) (target: float * float * float) (durationSeconds: float) =
    PhysicsClient.ViewCommands.smoothCamera s pos target durationSeconds |> ignore

let lookAtBody (s: Session) (bodyId: string) (durationSeconds: float) =
    PhysicsClient.ViewCommands.cameraLookAt s bodyId durationSeconds |> ignore

let followBody (s: Session) (bodyId: string) =
    PhysicsClient.ViewCommands.cameraFollow s bodyId |> ignore

let orbitBody (s: Session) (bodyId: string) (durationSeconds: float) (degrees: float) =
    PhysicsClient.ViewCommands.cameraOrbit s bodyId durationSeconds degrees |> ignore

let chaseBody (s: Session) (bodyId: string) (offset: float * float * float) =
    PhysicsClient.ViewCommands.cameraChase s bodyId offset |> ignore

let frameBodies (s: Session) (bodyIds: string list) =
    PhysicsClient.ViewCommands.cameraFrameBodies s bodyIds |> ignore

let shakeCamera (s: Session) (intensity: float) (durationSeconds: float) =
    PhysicsClient.ViewCommands.cameraShake s intensity durationSeconds |> ignore

let stopCamera (s: Session) =
    PhysicsClient.ViewCommands.cameraStop s |> ignore

let setNarration (s: Session) (text: string) =
    PhysicsClient.ViewCommands.setNarration s text |> ignore

let clearNarration (s: Session) =
    PhysicsClient.ViewCommands.setNarration s "" |> ignore

// ─── Standalone Runner ────────────────────────────────────────────────

let runStandalone (name: string) (run: Session -> unit) =
    let addr =
        match fsi.CommandLineArgs |> Array.tryItem 1 with
        | Some a -> a
        | None -> "http://localhost:5180"
    printfn "\n  %s\n" name
    printfn "  Connecting to %s..." addr
    let s = connect addr |> ok
    printfn "  Connected!\n"
    run s
    printfn "\n  Done."
    resetSimulation s
    disconnect s
