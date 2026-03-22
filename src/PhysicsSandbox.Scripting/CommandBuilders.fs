module PhysicsSandbox.Scripting.CommandBuilders

open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Vec3Builders

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
