/// <summary>
/// Auto-opened convenience module re-exporting all scripting functions.
/// Referencing the assembly makes all helpers, builders, and lifecycle functions
/// available without qualification.
/// </summary>
[<AutoOpen>]
module PhysicsSandbox.Scripting.Prelude

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

/// <summary>Unwraps an <c>Ok</c> Result, throwing on <c>Error</c>. All PhysicsClient operations return Results — use this for scripting convenience.</summary>
/// <param name="r">The result to unwrap.</param>
val ok : Result<'a, string> -> 'a

/// <summary>Pauses the current thread for the specified milliseconds.</summary>
/// <param name="ms">Duration in ms. Typical: 100 (stabilization), 500 (delay), 1000–5000 (observation).</param>
val sleep : int -> unit

/// <summary>Times a function and prints elapsed duration. Output: <c>[TIME] label: N ms</c>.</summary>
/// <param name="label">Descriptive label like <c>"add bodies"</c>.</param>
/// <param name="f">Function to time.</param>
val timed : string -> (unit -> 'a) -> 'a

/// <summary>Creates a protobuf Vec3 from <c>(x, y, z)</c>. Y-up coordinate system; ground at Y=0.</summary>
val toVec3 : (float * float * float) -> Vec3

/// <summary>Extracts Vec3 components as <c>(X, Y, Z)</c> tuple.</summary>
val toTuple : Vec3 -> (float * float * float)

/// <summary>Builds a sphere AddBody command.</summary>
/// <param name="id">Unique ID. Use <c>nextId "sphere"</c> for auto-generated.</param>
/// <param name="pos">Position <c>(x, y, z)</c>. Ground at Y=0.</param>
/// <param name="radius">Radius in meters. Reference: marble=0.01, bowling ball=0.11, beach ball=0.2, boulder=0.5.</param>
/// <param name="mass">Mass in kg. 0=static. Reference: marble=0.005, bowling ball=6.35, boulder=200.</param>
val makeSphereCmd : id: string -> pos: (float * float * float) -> radius: float -> mass: float -> SimulationCommand

/// <summary>Builds a box AddBody command.</summary>
/// <param name="id">Unique ID. Use <c>nextId "box"</c> for auto-generated.</param>
/// <param name="pos">Position <c>(x, y, z)</c>. Box center; set Y to half-height to rest on ground.</param>
/// <param name="halfExtents">Half-dimensions <c>(hx, hy, hz)</c>. <c>(0.5,0.5,0.5)</c>=1m cube. Reference: crate=(0.5,0.5,0.5), brick=(0.2,0.1,0.05), domino=(0.1,0.3,0.02).</param>
/// <param name="mass">Mass in kg. 0=static walls/floors. Reference: die=0.03, brick=3, crate=20.</param>
val makeBoxCmd : id: string -> pos: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> SimulationCommand

/// <summary>Builds an impulse command — instantaneous velocity change.</summary>
/// <param name="bodyId">Target body ID.</param>
/// <param name="impulse">Impulse <c>(x, y, z)</c> in N·s. Typical: <c>(0,5,0)</c> nudge, <c>(0,50,0)</c> launch, <c>(10,0,0)</c> push.</param>
val makeImpulseCmd : bodyId: string -> impulse: (float * float * float) -> SimulationCommand

/// <summary>Builds a torque command — rotational force.</summary>
/// <param name="bodyId">Target body ID.</param>
/// <param name="torque">Torque <c>(x, y, z)</c> in N·m. Axis = rotation direction, magnitude = strength. Typical: <c>(0,5,0)</c> gentle spin, <c>(0,50,0)</c> fast spin.</param>
val makeTorqueCmd : bodyId: string -> torque: (float * float * float) -> SimulationCommand

/// <summary>Builds a capsule AddBody command.</summary>
/// <param name="id">Unique ID. Use <c>nextId "capsule"</c> for auto-generated.</param>
/// <param name="pos">Position <c>(x, y, z)</c>. Capsule center.</param>
/// <param name="radius">Capsule radius in meters. Reference: limb=0.05, pipe=0.1, barrel=0.25.</param>
/// <param name="length">Cylinder portion length in meters (total = length + 2*radius). Reference: limb=0.3, pole=2.0.</param>
/// <param name="mass">Mass in kg. 0=static.</param>
val makeCapsuleCmd : id: string -> pos: (float * float * float) -> radius: float -> length: float -> mass: float -> SimulationCommand

/// <summary>Builds a cylinder AddBody command.</summary>
/// <param name="id">Unique ID. Use <c>nextId "cylinder"</c> for auto-generated.</param>
/// <param name="pos">Position <c>(x, y, z)</c>. Cylinder center.</param>
/// <param name="radius">Cylinder radius in meters. Reference: coin=0.01, can=0.03, barrel=0.25.</param>
/// <param name="length">Cylinder height in meters. Reference: coin=0.002, can=0.12, barrel=0.9.</param>
/// <param name="mass">Mass in kg. 0=static.</param>
val makeCylinderCmd : id: string -> pos: (float * float * float) -> radius: float -> length: float -> mass: float -> SimulationCommand

/// <summary>Creates MaterialProperties from physical parameters.</summary>
/// <param name="friction">Coulomb friction. 0=ice, 0.5=wood, 1.0=rubber.</param>
/// <param name="maxRecovery">Max recovery velocity m/s. Higher=bouncier.</param>
/// <param name="springFreq">Contact spring frequency Hz.</param>
/// <param name="springDamping">Contact spring damping ratio. 1.0=critical.</param>
val makeMaterialProperties : friction: float -> maxRecovery: float -> springFreq: float -> springDamping: float -> MaterialProperties

/// <summary>Creates a Color from RGBA (each 0.0–1.0). 1.0 alpha = opaque.</summary>
val makeColor : r: float -> g: float -> b: float -> a: float -> Color

/// <summary>Bouncy material: friction 0.4, high recovery 8.0, spring 60 Hz / 0.5 damping.</summary>
val bouncyMaterial : MaterialProperties

/// <summary>Sticky material: friction 2.0, low recovery 0.5, spring 30 Hz / 1.0 damping.</summary>
val stickyMaterial : MaterialProperties

/// <summary>Slippery material: friction 0.01, recovery 2.0, spring 30 Hz / 1.0 damping.</summary>
val slipperyMaterial : MaterialProperties

/// <summary>Builds a ball-socket constraint — free rotation around anchor.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">First body ID.</param>
/// <param name="bodyB">Second body ID.</param>
/// <param name="offsetA">Local offset on body A <c>(x, y, z)</c>.</param>
/// <param name="offsetB">Local offset on body B <c>(x, y, z)</c>.</param>
val makeBallSocketCmd : id: string -> bodyA: string -> bodyB: string -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> SimulationCommand

/// <summary>Builds a hinge constraint — rotation around a single axis.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">First body ID.</param>
/// <param name="bodyB">Second body ID.</param>
/// <param name="axis">Hinge axis direction <c>(x, y, z)</c>.</param>
/// <param name="offsetA">Local offset on body A.</param>
/// <param name="offsetB">Local offset on body B.</param>
val makeHingeCmd : id: string -> bodyA: string -> bodyB: string -> axis: (float * float * float) -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> SimulationCommand

/// <summary>Builds a weld constraint — rigidly joins two bodies.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">First body ID.</param>
/// <param name="bodyB">Second body ID.</param>
val makeWeldCmd : id: string -> bodyA: string -> bodyB: string -> SimulationCommand

/// <summary>Builds a distance-limit constraint — keeps bodies within min/max distance.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">First body ID.</param>
/// <param name="bodyB">Second body ID.</param>
/// <param name="minDist">Minimum distance in meters.</param>
/// <param name="maxDist">Maximum distance in meters.</param>
val makeDistanceLimitCmd : id: string -> bodyA: string -> bodyB: string -> minDist: float -> maxDist: float -> SimulationCommand

/// <summary>Builds a remove-constraint command.</summary>
/// <param name="constraintId">ID of the constraint to remove.</param>
val makeRemoveConstraintCmd : constraintId: string -> SimulationCommand

/// <summary>Builds a SetBodyPose command to teleport/reposition a body.</summary>
val makeSetBodyPoseCmd : bodyId: string -> pos: (float * float * float) -> SimulationCommand

/// <summary>Casts a ray and returns hit results as (bodyId, position, normal, distance) tuples.</summary>
/// <param name="session">Active session.</param>
/// <param name="origin">Ray origin <c>(x, y, z)</c>.</param>
/// <param name="direction">Ray direction <c>(x, y, z)</c>.</param>
/// <param name="maxDistance">Maximum ray distance.</param>
val raycast : session: Session -> origin: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) list

/// <summary>Casts a ray and returns all hits along the ray.</summary>
val raycastAll : session: Session -> origin: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) list

/// <summary>Performs a sphere sweep cast. Returns Some (bodyId, position, normal, distance) or None.</summary>
val sweepSphere : session: Session -> radius: float -> startPosition: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) option

/// <summary>Tests for overlapping bodies using a sphere. Returns overlapping body IDs.</summary>
val overlapSphere : session: Session -> radius: float -> position: (float * float * float) -> string list

/// <summary>Sends commands in auto-chunked batches of 100. Logs per-command failures.</summary>
/// <param name="s">Active session.</param>
/// <param name="commands">Commands to send. Any length; auto-split at 100.</param>
val batchAdd : Session -> SimulationCommand list -> unit

/// <summary>Resets simulation: pause, clear bodies, reset IDs, add ground plane, set gravity -9.81, wait 100ms.</summary>
/// <param name="s">Active session.</param>
val resetSimulation : Session -> unit

/// <summary>Runs simulation for N seconds then pauses. 60 Hz timestep, so 3.0s = 180 steps.</summary>
/// <param name="s">Active session.</param>
/// <param name="seconds">Duration. Typical: 1–2 drops, 3–5 bouncing, 10+ long scenarios.</param>
val runFor : Session -> float -> unit

/// <summary>Generates sequential body ID like <c>"sphere-1"</c>. Each prefix has independent counter; resets on <c>resetSimulation</c>.</summary>
/// <param name="prefix">Shape prefix: <c>"sphere"</c>, <c>"box"</c>, <c>"crate"</c>, etc.</param>
val nextId : string -> string
