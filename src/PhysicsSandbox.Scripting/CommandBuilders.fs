/// <summary>Factory functions for constructing protobuf <c>SimulationCommand</c> messages from simple F# values.</summary>
module PhysicsSandbox.Scripting.CommandBuilders

open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Vec3Builders

/// <summary>Builds an <c>AddBody</c> command for a sphere with the given parameters.</summary>
/// <param name="id">Unique body identifier (e.g., <c>"sphere-1"</c>).</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>.</param>
/// <param name="radius">Sphere radius.</param>
/// <param name="mass">Body mass (use 0 for static bodies).</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> wrapping the add-body request.</returns>
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

/// <summary>Builds an <c>AddBody</c> command for a box with the given parameters.</summary>
/// <param name="id">Unique body identifier.</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>.</param>
/// <param name="halfExtents">Box half-extents as <c>(hx, hy, hz)</c>.</param>
/// <param name="mass">Body mass (use 0 for static bodies).</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> wrapping the add-body request.</returns>
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

/// <summary>Builds an <c>ApplyImpulse</c> command that applies an instantaneous velocity change to a body.</summary>
/// <param name="bodyId">Target body identifier.</param>
/// <param name="impulse">Impulse vector as <c>(x, y, z)</c>.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> wrapping the impulse request.</returns>
let makeImpulseCmd (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    cmd

/// <summary>Builds an <c>ApplyTorque</c> command that applies a rotational force to a body.</summary>
/// <param name="bodyId">Target body identifier.</param>
/// <param name="torque">Torque vector as <c>(x, y, z)</c>.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> wrapping the torque request.</returns>
let makeTorqueCmd (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    cmd
