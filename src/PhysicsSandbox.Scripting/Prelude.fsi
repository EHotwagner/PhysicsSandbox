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
