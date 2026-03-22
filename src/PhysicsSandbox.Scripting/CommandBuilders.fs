/// <summary>Factory functions for constructing protobuf <c>SimulationCommand</c> messages from simple F# values.</summary>
/// <remarks>
/// These builders create the nested proto message hierarchy (Shape → AddBody → SimulationCommand)
/// that the gRPC API requires. Use <see cref="M:PhysicsSandbox.Scripting.BatchOperations.batchAdd"/>
/// to send the resulting commands efficiently.
/// </remarks>
module PhysicsSandbox.Scripting.CommandBuilders

open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Vec3Builders

/// <summary>Builds an <c>AddBody</c> command for a sphere.</summary>
/// <param name="id">Unique body identifier. Use <see cref="M:PhysicsSandbox.Scripting.SimulationLifecycle.nextId"/>
/// to generate sequential IDs like <c>"sphere-1"</c>, or pass a custom string. Must be unique across all bodies.</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>. Y is up; ground is at Y=0.
/// Typical values: <c>(0.0, 5.0, 0.0)</c> for 5m above ground. Space bodies at least 2× radius apart to avoid overlap.</param>
/// <param name="radius">Sphere radius in meters. Reference values: marble=0.01, bowling ball=0.11,
/// beach ball=0.2, boulder=0.5. Typical range: 0.01–1.0.</param>
/// <param name="mass">Body mass in kilograms. Use 0 for a static (immovable) body. Reference values:
/// marble=0.005, beach ball=0.1, bowling ball=6.35, boulder=200. Typical range: 0.01–200.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let cmd = makeSphereCmd "ball-1" (0.0, 10.0, 0.0) 0.5 1.0
/// batchAdd session [cmd]
/// </code>
/// </example>
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

/// <summary>Builds an <c>AddBody</c> command for a box.</summary>
/// <param name="id">Unique body identifier. Use <c>nextId "box"</c> for auto-generated IDs.</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>. The position is the box center.
/// For a box resting on the ground, set Y to the box's half-height (e.g., <c>(0.0, 0.5, 0.0)</c> for a 1m-tall box).</param>
/// <param name="halfExtents">Box half-extents as <c>(hx, hy, hz)</c> — half the width, height, and depth in meters.
/// A <c>(0.5, 0.5, 0.5)</c> creates a 1m cube. Reference values: crate=(0.5,0.5,0.5), brick=(0.2,0.1,0.05),
/// die=(0.05,0.05,0.05), domino=(0.1,0.3,0.02). Typical range: 0.01–2.0 per axis.</param>
/// <param name="mass">Body mass in kilograms. Use 0 for static (walls, floors). Reference values:
/// die=0.03, brick=3, crate=20. Typical range: 0.01–100.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// // A 1m wooden crate at 5m height
/// let crate = makeBoxCmd "crate-1" (0.0, 5.0, 0.0) (0.5, 0.5, 0.5) 20.0
/// // A static wall (mass 0 = immovable)
/// let wall = makeBoxCmd "wall" (3.0, 1.0, 0.0) (0.1, 1.0, 2.0) 0.0
/// </code>
/// </example>
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

/// <summary>Builds an <c>ApplyImpulse</c> command — an instantaneous velocity change applied once.</summary>
/// <param name="bodyId">Target body identifier. Must match an existing body's ID.</param>
/// <param name="impulse">Impulse vector as <c>(x, y, z)</c> in Newton-seconds. The effect scales inversely
/// with body mass. Typical values: <c>(0.0, 5.0, 0.0)</c> for a gentle upward nudge on a 1kg body,
/// <c>(0.0, 50.0, 0.0)</c> for a strong launch, <c>(10.0, 0.0, 0.0)</c> for a horizontal push.
/// For a bowling-ball break shot: <c>(0.0, 0.0, -30.0)</c>.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// // Launch a sphere upward
/// let launch = makeImpulseCmd "sphere-1" (0.0, 20.0, 0.0)
/// // Push a body sideways
/// let push = makeImpulseCmd "box-1" (15.0, 0.0, 0.0)
/// </code>
/// </example>
let makeImpulseCmd (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    cmd

/// <summary>Builds an <c>ApplyTorque</c> command — a rotational force applied to a body.</summary>
/// <param name="bodyId">Target body identifier. Must match an existing body's ID.</param>
/// <param name="torque">Torque vector as <c>(x, y, z)</c> in Newton-meters. The axis of the vector
/// determines the rotation axis; the magnitude determines the strength.
/// Typical values: <c>(0.0, 5.0, 0.0)</c> for a gentle Y-axis spin,
/// <c>(0.0, 50.0, 0.0)</c> for a fast spin, <c>(10.0, 10.0, 0.0)</c> for a diagonal tumble.
/// Higher mass bodies need proportionally larger torques.</param>
/// <returns>A <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// // Spin a box around the vertical axis
/// let spin = makeTorqueCmd "box-1" (0.0, 10.0, 0.0)
/// // Tumble a sphere diagonally
/// let tumble = makeTorqueCmd "sphere-1" (5.0, 0.0, 5.0)
/// </code>
/// </example>
let makeTorqueCmd (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    cmd
