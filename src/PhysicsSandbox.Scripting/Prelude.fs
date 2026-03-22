/// <summary>
/// Auto-opened convenience module that re-exports all scripting functions.
/// Opening <c>PhysicsSandbox.Scripting.Prelude</c> (or just referencing the assembly)
/// makes all helpers, builders, and lifecycle functions available without qualification.
/// </summary>
[<AutoOpen>]
module PhysicsSandbox.Scripting.Prelude

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

/// <summary>Unwraps an <c>Ok</c> value from a <c>Result</c>, throwing an exception on <c>Error</c>.</summary>
/// <param name="r">The result to unwrap.</param>
/// <returns>The contained value if <c>Ok</c>.</returns>
/// <exception cref="T:System.Exception">Thrown with the error message when the result is <c>Error</c>.</exception>
/// <example>
/// <code>
/// let id = connect "http://localhost:5180" |> ok   // returns Session on success, throws on failure
/// </code>
/// </example>
let ok r = PhysicsSandbox.Scripting.Helpers.ok r

/// <summary>Pauses the current thread for the specified number of milliseconds.</summary>
/// <param name="ms">Duration to sleep in milliseconds. Typical values: 100 (stabilization pause),
/// 500 (short delay between actions), 1000–5000 (observation window).</param>
let sleep ms = PhysicsSandbox.Scripting.Helpers.sleep ms

/// <summary>Executes a function, measures its wall-clock duration, and prints the elapsed time to the console.</summary>
/// <param name="label">A descriptive label like <c>"add bodies"</c> or <c>"batch 200 spheres"</c>.</param>
/// <param name="f">The function to execute and time.</param>
/// <returns>The return value of <paramref name="f"/>.</returns>
/// <remarks>Output format: <c>  [TIME] label: N ms</c></remarks>
let timed label f = PhysicsSandbox.Scripting.Helpers.timed label f

/// <summary>Creates a protobuf <c>Vec3</c> from a float triple. Y is up; ground is at Y=0.</summary>
/// <param name="v">Position or direction as <c>(x, y, z)</c>.</param>
/// <returns>A new Vec3 with the given coordinates.</returns>
/// <example>
/// <code>
/// let above = toVec3 (0.0, 10.0, 0.0)     // 10m above ground
/// let gravity = toVec3 (0.0, -9.81, 0.0)   // standard gravity
/// </code>
/// </example>
let toVec3 v = PhysicsSandbox.Scripting.Vec3Builders.toVec3 v

/// <summary>Extracts the components of a <c>Vec3</c> as a float triple <c>(X, Y, Z)</c>.</summary>
/// <param name="v">The Vec3 to decompose.</param>
/// <returns>A tuple of <c>(X, Y, Z)</c>.</returns>
let toTuple v = PhysicsSandbox.Scripting.Vec3Builders.toTuple v

/// <summary>Builds an <c>AddBody</c> command for a sphere.</summary>
/// <param name="id">Unique body identifier. Use <c>nextId "sphere"</c> for auto-generated IDs like <c>"sphere-1"</c>.</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>. Y is up; ground at Y=0. Example: <c>(0.0, 5.0, 0.0)</c>.</param>
/// <param name="radius">Sphere radius in meters. Reference: marble=0.01, bowling ball=0.11, beach ball=0.2, boulder=0.5.</param>
/// <param name="mass">Mass in kg. Use 0 for static. Reference: marble=0.005, beach ball=0.1, bowling ball=6.35, boulder=200.</param>
/// <returns>A SimulationCommand ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let cmd = makeSphereCmd "ball-1" (0.0, 10.0, 0.0) 0.5 1.0
/// batchAdd session [cmd]
/// </code>
/// </example>
let makeSphereCmd id pos radius mass = PhysicsSandbox.Scripting.CommandBuilders.makeSphereCmd id pos radius mass

/// <summary>Builds an <c>AddBody</c> command for a box.</summary>
/// <param name="id">Unique body identifier. Use <c>nextId "box"</c> for auto-generated IDs.</param>
/// <param name="pos">World-space position as <c>(x, y, z)</c>. Position is the box center.
/// For a box resting on the ground, set Y to the half-height.</param>
/// <param name="halfExtents">Box half-dimensions as <c>(hx, hy, hz)</c> in meters. A <c>(0.5, 0.5, 0.5)</c> = 1m cube.
/// Reference: crate=(0.5,0.5,0.5), brick=(0.2,0.1,0.05), die=(0.05,0.05,0.05), domino=(0.1,0.3,0.02).</param>
/// <param name="mass">Mass in kg. Use 0 for static walls/floors. Reference: die=0.03, brick=3, crate=20.</param>
/// <returns>A SimulationCommand ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let crate = makeBoxCmd "crate-1" (0.0, 5.0, 0.0) (0.5, 0.5, 0.5) 20.0
/// let wall = makeBoxCmd "wall" (3.0, 1.0, 0.0) (0.1, 1.0, 2.0) 0.0  // static wall
/// </code>
/// </example>
let makeBoxCmd id pos halfExtents mass = PhysicsSandbox.Scripting.CommandBuilders.makeBoxCmd id pos halfExtents mass

/// <summary>Builds an <c>ApplyImpulse</c> command — an instantaneous velocity change applied once.</summary>
/// <param name="bodyId">Target body identifier. Must match an existing body's ID.</param>
/// <param name="impulse">Impulse vector as <c>(x, y, z)</c> in Newton-seconds. Effect scales inversely with mass.
/// Typical: <c>(0, 5, 0)</c> gentle nudge, <c>(0, 50, 0)</c> strong launch, <c>(10, 0, 0)</c> horizontal push.</param>
/// <returns>A SimulationCommand ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let launch = makeImpulseCmd "sphere-1" (0.0, 20.0, 0.0)  // upward launch
/// let push = makeImpulseCmd "box-1" (15.0, 0.0, 0.0)       // sideways push
/// </code>
/// </example>
let makeImpulseCmd bodyId impulse = PhysicsSandbox.Scripting.CommandBuilders.makeImpulseCmd bodyId impulse

/// <summary>Builds an <c>ApplyTorque</c> command — a rotational force applied to a body.</summary>
/// <param name="bodyId">Target body identifier. Must match an existing body's ID.</param>
/// <param name="torque">Torque vector as <c>(x, y, z)</c> in Newton-meters. The axis determines rotation direction;
/// magnitude determines strength. Typical: <c>(0, 5, 0)</c> gentle Y-spin, <c>(0, 50, 0)</c> fast spin,
/// <c>(10, 10, 0)</c> diagonal tumble. Heavier bodies need proportionally larger torques.</param>
/// <returns>A SimulationCommand ready to send via <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let spin = makeTorqueCmd "box-1" (0.0, 10.0, 0.0)       // vertical spin
/// let tumble = makeTorqueCmd "sphere-1" (5.0, 0.0, 5.0)   // diagonal tumble
/// </code>
/// </example>
let makeTorqueCmd bodyId torque = PhysicsSandbox.Scripting.CommandBuilders.makeTorqueCmd bodyId torque

/// <summary>Builds an <c>AddBody</c> command for a capsule (cylinder with hemispherical caps).</summary>
let makeCapsuleCmd id pos radius length mass = PhysicsSandbox.Scripting.CommandBuilders.makeCapsuleCmd id pos radius length mass

/// <summary>Builds an <c>AddBody</c> command for a cylinder.</summary>
let makeCylinderCmd id pos radius length mass = PhysicsSandbox.Scripting.CommandBuilders.makeCylinderCmd id pos radius length mass

/// <summary>Creates a <c>MaterialProperties</c> proto message from physical parameters.</summary>
let makeMaterialProperties friction maxRecovery springFreq springDamping = PhysicsSandbox.Scripting.CommandBuilders.makeMaterialProperties friction maxRecovery springFreq springDamping

/// <summary>Creates a <c>Color</c> proto message from RGBA components (each 0.0–1.0).</summary>
let makeColor r g b a = PhysicsSandbox.Scripting.CommandBuilders.makeColor r g b a

/// <summary>Bouncy material preset.</summary>
let bouncyMaterial = PhysicsSandbox.Scripting.CommandBuilders.bouncyMaterial

/// <summary>Sticky/high-friction material preset.</summary>
let stickyMaterial = PhysicsSandbox.Scripting.CommandBuilders.stickyMaterial

/// <summary>Slippery/ice-like material preset.</summary>
let slipperyMaterial = PhysicsSandbox.Scripting.CommandBuilders.slipperyMaterial

/// <summary>Builds an <c>AddConstraint</c> command for a ball-socket joint.</summary>
let makeBallSocketCmd id bodyA bodyB offsetA offsetB = PhysicsSandbox.Scripting.ConstraintBuilders.makeBallSocketCmd id bodyA bodyB offsetA offsetB

/// <summary>Builds an <c>AddConstraint</c> command for a hinge joint.</summary>
let makeHingeCmd id bodyA bodyB axis offsetA offsetB = PhysicsSandbox.Scripting.ConstraintBuilders.makeHingeCmd id bodyA bodyB axis offsetA offsetB

/// <summary>Builds an <c>AddConstraint</c> command for a weld joint.</summary>
let makeWeldCmd id bodyA bodyB = PhysicsSandbox.Scripting.ConstraintBuilders.makeWeldCmd id bodyA bodyB

/// <summary>Builds an <c>AddConstraint</c> command for a distance limit.</summary>
let makeDistanceLimitCmd id bodyA bodyB minDist maxDist = PhysicsSandbox.Scripting.ConstraintBuilders.makeDistanceLimitCmd id bodyA bodyB minDist maxDist

/// <summary>Builds a <c>RemoveConstraint</c> command.</summary>
let makeRemoveConstraintCmd constraintId = PhysicsSandbox.Scripting.ConstraintBuilders.makeRemoveConstraintCmd constraintId

/// <summary>Builds a SetBodyPose command to update a body's position at runtime.</summary>
let makeSetBodyPoseCmd bodyId pos = PhysicsSandbox.Scripting.CommandBuilders.makeSetBodyPoseCmd bodyId pos

/// <summary>Casts a ray and returns hit results as (bodyId, position, normal, distance) tuples.</summary>
let raycast session origin direction maxDistance = PhysicsSandbox.Scripting.QueryBuilders.raycast session origin direction maxDistance

/// <summary>Casts a ray and returns all hits along the ray.</summary>
let raycastAll session origin direction maxDistance = PhysicsSandbox.Scripting.QueryBuilders.raycastAll session origin direction maxDistance

/// <summary>Performs a sphere sweep cast. Returns Some hit or None.</summary>
let sweepSphere session radius startPosition direction maxDistance = PhysicsSandbox.Scripting.QueryBuilders.sweepSphere session radius startPosition direction maxDistance

/// <summary>Tests for overlapping bodies at a position using a sphere shape. Returns body IDs.</summary>
let overlapSphere session radius position = PhysicsSandbox.Scripting.QueryBuilders.overlapSphere session radius position

/// <summary>Sends a list of simulation commands in batches of 100, logging any failures to the console.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="commands">List of SimulationCommands. Any length — auto-chunked at 100 (server maximum).
/// Typical: 1–50 interactive, 100–500 stress tests.</param>
/// <example>
/// <code>
/// let cmds = [ for i in 1..200 ->
///     makeSphereCmd (nextId "sphere") (0.0, float i * 0.5, 0.0) 0.3 1.0 ]
/// batchAdd session cmds   // sent in 2 automatic batches of 100
/// </code>
/// </example>
let batchAdd (s: Session) (commands: SimulationCommand list) = PhysicsSandbox.Scripting.BatchOperations.batchAdd s commands

/// <summary>Fully resets the simulation: pause, clear all bodies, reset ID counters, add ground plane, set gravity to -9.81.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <remarks>Call at the start of every demo or experiment for a predictable starting state.</remarks>
let resetSimulation (s: Session) = PhysicsSandbox.Scripting.SimulationLifecycle.resetSimulation s

/// <summary>Runs the simulation for the specified duration, then pauses automatically.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="seconds">Duration in seconds. Typical: 1–2 for quick drops, 3–5 for bouncing/rolling,
/// 10+ for long scenarios. Simulation runs at 60 Hz, so 3.0s = 180 physics steps.</param>
/// <example>
/// <code>
/// runFor session 3.0    // run for 3 seconds then pause
/// </code>
/// </example>
let runFor (s: Session) (seconds: float) = PhysicsSandbox.Scripting.SimulationLifecycle.runFor s seconds

/// <summary>Generates the next sequential ID for a body with the given shape prefix.</summary>
/// <param name="prefix">Shape prefix like <c>"sphere"</c>, <c>"box"</c>, <c>"crate"</c>, <c>"wall"</c>.
/// Any string works — each prefix has an independent counter. Counters reset on <c>resetSimulation</c>.</param>
/// <returns>Human-readable ID like <c>"sphere-1"</c>, <c>"box-3"</c>.</returns>
/// <example>
/// <code>
/// let id1 = nextId "sphere"   // "sphere-1"
/// let id2 = nextId "sphere"   // "sphere-2"
/// let id3 = nextId "box"      // "box-1"
/// </code>
/// </example>
let nextId prefix = PhysicsSandbox.Scripting.SimulationLifecycle.nextId prefix
