/// <summary>Factory functions for constructing protobuf <c>SimulationCommand</c> messages from simple F# values.</summary>
/// <remarks>
/// These builders hide the nested proto message hierarchy (Shape → AddBody → SimulationCommand).
/// Use <c>batchAdd</c> to send the resulting commands.
/// </remarks>
module PhysicsSandbox.Scripting.CommandBuilders

open PhysicsSandbox.Shared.Contracts

/// <summary>Builds an <c>AddBody</c> command for a sphere.</summary>
/// <param name="id">Unique body ID. Use <c>nextId "sphere"</c> for auto-generated IDs like <c>"sphere-1"</c>.</param>
/// <param name="pos">World-space position <c>(x, y, z)</c>. Y is up; ground at Y=0. Example: <c>(0.0, 5.0, 0.0)</c>.
/// Space bodies at least 2× radius apart to avoid overlap.</param>
/// <param name="radius">Sphere radius in meters. Reference: marble=0.01, bowling ball=0.11,
/// beach ball=0.2, boulder=0.5. Typical range: 0.01–1.0.</param>
/// <param name="mass">Mass in kg. Use 0 for static. Reference: marble=0.005, beach ball=0.1,
/// bowling ball=6.35, boulder=200. Typical range: 0.01–200.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let cmd = makeSphereCmd "ball-1" (0.0, 10.0, 0.0) 0.5 1.0
/// batchAdd session [cmd]
/// </code>
/// </example>
val makeSphereCmd : id: string -> pos: (float * float * float) -> radius: float -> mass: float -> SimulationCommand

/// <summary>Builds an <c>AddBody</c> command for a box.</summary>
/// <param name="id">Unique body ID. Use <c>nextId "box"</c> for auto-generated IDs.</param>
/// <param name="pos">World-space position <c>(x, y, z)</c>. Position is the box center.
/// For a box resting on the ground, set Y to its half-height.</param>
/// <param name="halfExtents">Half-dimensions <c>(hx, hy, hz)</c> in meters. <c>(0.5, 0.5, 0.5)</c> = 1m cube.
/// Reference: crate=(0.5,0.5,0.5), brick=(0.2,0.1,0.05), die=(0.05,0.05,0.05), domino=(0.1,0.3,0.02).</param>
/// <param name="mass">Mass in kg. Use 0 for static walls/floors. Reference: die=0.03, brick=3, crate=20.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let crate = makeBoxCmd "crate-1" (0.0, 5.0, 0.0) (0.5, 0.5, 0.5) 20.0
/// let wall = makeBoxCmd "wall" (3.0, 1.0, 0.0) (0.1, 1.0, 2.0) 0.0   // static
/// </code>
/// </example>
val makeBoxCmd : id: string -> pos: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> SimulationCommand

/// <summary>Builds an <c>ApplyImpulse</c> command — an instantaneous velocity change applied once.</summary>
/// <param name="bodyId">Target body ID. Must match an existing body.</param>
/// <param name="impulse">Impulse vector <c>(x, y, z)</c> in Newton-seconds. Effect scales inversely with mass.
/// Typical: <c>(0, 5, 0)</c> gentle nudge, <c>(0, 50, 0)</c> strong launch, <c>(10, 0, 0)</c> horizontal push,
/// <c>(0, 0, -30)</c> bowling-ball break shot.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let launch = makeImpulseCmd "sphere-1" (0.0, 20.0, 0.0)
/// let push = makeImpulseCmd "box-1" (15.0, 0.0, 0.0)
/// </code>
/// </example>
val makeImpulseCmd : bodyId: string -> impulse: (float * float * float) -> SimulationCommand

/// <summary>Builds an <c>ApplyTorque</c> command — a rotational force applied to a body.</summary>
/// <param name="bodyId">Target body ID. Must match an existing body.</param>
/// <param name="torque">Torque vector <c>(x, y, z)</c> in Newton-meters. The axis determines rotation direction;
/// magnitude determines strength. Typical: <c>(0, 5, 0)</c> gentle Y-spin, <c>(0, 50, 0)</c> fast spin,
/// <c>(10, 10, 0)</c> diagonal tumble. Heavier bodies need proportionally larger torques.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let spin = makeTorqueCmd "box-1" (0.0, 10.0, 0.0)
/// let tumble = makeTorqueCmd "sphere-1" (5.0, 0.0, 5.0)
/// </code>
/// </example>
val makeTorqueCmd : bodyId: string -> torque: (float * float * float) -> SimulationCommand

/// <summary>Builds an <c>AddBody</c> command for a capsule (cylinder with hemispherical caps).</summary>
/// <param name="id">Unique body ID. Use <c>nextId "capsule"</c> for auto-generated IDs.</param>
/// <param name="pos">World-space position <c>(x, y, z)</c>. Position is the capsule center.</param>
/// <param name="radius">Capsule radius in meters. Reference: limb=0.05, pipe=0.1, barrel=0.25. Typical range: 0.01–0.5.</param>
/// <param name="length">Capsule length (cylinder portion, excluding caps) in meters. Total height = length + 2×radius.
/// Reference: finger=0.05, limb=0.3, pole=2.0. Typical range: 0.1–3.0.</param>
/// <param name="mass">Mass in kg. Use 0 for static. Reference: limb=2, pipe=5, log=50.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let capsule = makeCapsuleCmd "cap-1" (0.0, 5.0, 0.0) 0.2 1.0 5.0
/// batchAdd session [capsule]
/// </code>
/// </example>
val makeCapsuleCmd : id: string -> pos: (float * float * float) -> radius: float -> length: float -> mass: float -> SimulationCommand

/// <summary>Builds an <c>AddBody</c> command for a cylinder.</summary>
/// <param name="id">Unique body ID. Use <c>nextId "cylinder"</c> for auto-generated IDs.</param>
/// <param name="pos">World-space position <c>(x, y, z)</c>. Position is the cylinder center.</param>
/// <param name="radius">Cylinder radius in meters. Reference: coin=0.01, can=0.03, barrel=0.25. Typical range: 0.01–1.0.</param>
/// <param name="length">Cylinder length (height) in meters. Reference: coin=0.002, can=0.12, barrel=0.9. Typical range: 0.01–3.0.</param>
/// <param name="mass">Mass in kg. Use 0 for static. Reference: coin=0.01, can=0.35, barrel=50.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let cyl = makeCylinderCmd "cyl-1" (0.0, 3.0, 0.0) 0.25 0.9 50.0
/// batchAdd session [cyl]
/// </code>
/// </example>
val makeCylinderCmd : id: string -> pos: (float * float * float) -> radius: float -> length: float -> mass: float -> SimulationCommand

/// <summary>Creates a <c>MaterialProperties</c> proto message from physical parameters.</summary>
/// <param name="friction">Coulomb friction coefficient. 0=frictionless ice, 0.5=wood, 1.0=rubber. Typical range: 0.0–2.0.</param>
/// <param name="maxRecovery">Maximum recovery velocity in m/s. Higher = bouncier collisions. Typical: 1.0–10.0.</param>
/// <param name="springFreq">Contact spring frequency in Hz. Higher = stiffer contact. Typical: 30–120.</param>
/// <param name="springDamping">Contact spring damping ratio. 1.0 = critically damped. Typical: 0.5–2.0.</param>
/// <returns>A MaterialProperties message to assign to a body's material field.</returns>
val makeMaterialProperties : friction: float -> maxRecovery: float -> springFreq: float -> springDamping: float -> MaterialProperties

/// <summary>Creates a <c>Color</c> proto message from RGBA components (each 0.0–1.0).</summary>
/// <param name="r">Red channel 0.0–1.0.</param>
/// <param name="g">Green channel 0.0–1.0.</param>
/// <param name="b">Blue channel 0.0–1.0.</param>
/// <param name="a">Alpha channel 0.0–1.0. 1.0 = fully opaque.</param>
/// <returns>A Color message to assign to a body's color field.</returns>
val makeColor : r: float -> g: float -> b: float -> a: float -> Color

/// <summary>Bouncy material preset: moderate friction (0.4), high recovery (8.0), stiff spring (60 Hz, 0.5 damping).</summary>
val bouncyMaterial : MaterialProperties

/// <summary>Sticky/high-friction material preset: friction 2.0, low recovery (0.5), stiff spring (30 Hz, 1.0 damping).</summary>
val stickyMaterial : MaterialProperties

/// <summary>Slippery/ice-like material preset: near-zero friction (0.01), moderate recovery (2.0), standard spring (30 Hz, 1.0 damping).</summary>
val slipperyMaterial : MaterialProperties

/// <summary>Builds a <c>SetBodyPose</c> command to update a body's position at runtime.</summary>
val makeSetBodyPoseCmd : bodyId: string -> pos: (float * float * float) -> SimulationCommand
