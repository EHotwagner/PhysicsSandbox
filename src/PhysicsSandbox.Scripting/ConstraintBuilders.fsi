/// <summary>Factory functions for constructing constraint-related <c>SimulationCommand</c> messages from simple F# values.</summary>
/// <remarks>
/// These builders hide the nested proto message hierarchy (ConstraintType → AddConstraint → SimulationCommand).
/// Use <c>batchAdd</c> to send the resulting commands. Constraint IDs can be generated with <c>nextId "constraint"</c>.
/// </remarks>
module PhysicsSandbox.Scripting.ConstraintBuilders

open PhysicsSandbox.Shared.Contracts

/// <summary>Builds an <c>AddConstraint</c> command for a ball-socket joint — allows free rotation around the anchor point.</summary>
/// <param name="id">Unique constraint ID. Use <c>nextId "constraint"</c> for auto-generated IDs.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="offsetA">Local-space offset on body A <c>(x, y, z)</c> where the socket attaches.
/// Example: <c>(0.0, 0.5, 0.0)</c> = top of a 1m-tall body.</param>
/// <param name="offsetB">Local-space offset on body B <c>(x, y, z)</c> where the socket attaches.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let joint = makeBallSocketCmd "joint-1" "arm" "forearm" (0.0, -0.5, 0.0) (0.0, 0.5, 0.0)
/// batchAdd session [joint]
/// </code>
/// </example>
val makeBallSocketCmd : id: string -> bodyA: string -> bodyB: string -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a hinge joint — allows rotation around a single axis.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="axis">Hinge axis direction <c>(x, y, z)</c>. Example: <c>(0.0, 1.0, 0.0)</c> for vertical hinge,
/// <c>(1.0, 0.0, 0.0)</c> for horizontal hinge along X.</param>
/// <param name="offsetA">Local-space attachment offset on body A.</param>
/// <param name="offsetB">Local-space attachment offset on body B.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let hinge = makeHingeCmd "hinge-1" "door" "frame" (0.0, 1.0, 0.0) (-0.5, 0.0, 0.0) (0.5, 0.0, 0.0)
/// batchAdd session [hinge]
/// </code>
/// </example>
val makeHingeCmd : id: string -> bodyA: string -> bodyB: string -> axis: (float * float * float) -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a weld joint — rigidly fixes two bodies together.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body. The bodies are welded at their current relative position and orientation.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let weld = makeWeldCmd "weld-1" "plate-1" "plate-2"
/// batchAdd session [weld]
/// </code>
/// </example>
val makeWeldCmd : id: string -> bodyA: string -> bodyB: string -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a distance limit — keeps two bodies within a distance range.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="minDist">Minimum allowed distance in meters. Use 0 for no minimum.</param>
/// <param name="maxDist">Maximum allowed distance in meters. Typical: 1.0–10.0 for chain links.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let chain = makeDistanceLimitCmd "chain-1" "link-1" "link-2" 0.5 1.5
/// batchAdd session [chain]
/// </code>
/// </example>
val makeDistanceLimitCmd : id: string -> bodyA: string -> bodyB: string -> minDist: float -> maxDist: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a distance spring — pulls two bodies toward a target distance.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="offsetA">Local-space offset on body A <c>(x, y, z)</c>.</param>
/// <param name="offsetB">Local-space offset on body B <c>(x, y, z)</c>.</param>
/// <param name="targetDistance">Target distance in meters that the spring pulls toward.</param>
val makeDistanceSpringCmd : id: string -> bodyA: string -> bodyB: string -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> targetDistance: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a swing limit — limits the angle between two axes.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="axisA">Local axis on body A <c>(x, y, z)</c>.</param>
/// <param name="axisB">Local axis on body B <c>(x, y, z)</c>.</param>
/// <param name="maxAngle">Maximum swing angle in radians.</param>
val makeSwingLimitCmd : id: string -> bodyA: string -> bodyB: string -> axisA: (float * float * float) -> axisB: (float * float * float) -> maxAngle: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a twist limit — limits rotation around an axis to an angle range.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="axisA">Local axis on body A <c>(x, y, z)</c>.</param>
/// <param name="axisB">Local axis on body B <c>(x, y, z)</c>.</param>
/// <param name="minAngle">Minimum twist angle in radians.</param>
/// <param name="maxAngle">Maximum twist angle in radians.</param>
val makeTwistLimitCmd : id: string -> bodyA: string -> bodyB: string -> axisA: (float * float * float) -> axisB: (float * float * float) -> minAngle: float -> maxAngle: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a linear axis motor — drives linear motion along an axis.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="offsetA">Local-space offset on body A <c>(x, y, z)</c>.</param>
/// <param name="offsetB">Local-space offset on body B <c>(x, y, z)</c>.</param>
/// <param name="axis">Local axis direction <c>(x, y, z)</c> for the motor.</param>
/// <param name="targetVelocity">Target velocity along the axis in m/s.</param>
/// <param name="maxForce">Maximum force the motor can apply.</param>
val makeLinearAxisMotorCmd : id: string -> bodyA: string -> bodyB: string -> offsetA: (float * float * float) -> offsetB: (float * float * float) -> axis: (float * float * float) -> targetVelocity: float -> maxForce: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for an angular motor — drives rotation around axes.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="targetVelocity">Target angular velocity <c>(x, y, z)</c> in rad/s.</param>
/// <param name="maxForce">Maximum force the motor can apply.</param>
val makeAngularMotorCmd : id: string -> bodyA: string -> bodyB: string -> targetVelocity: (float * float * float) -> maxForce: float -> SimulationCommand

/// <summary>Builds an <c>AddConstraint</c> command for a point-on-line constraint — constrains a point to slide along a line.</summary>
/// <param name="id">Unique constraint ID.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="origin">Local origin of the line on body A <c>(x, y, z)</c>.</param>
/// <param name="direction">Local direction of the line <c>(x, y, z)</c>.</param>
/// <param name="offset">Local offset on body B <c>(x, y, z)</c>.</param>
val makePointOnLineCmd : id: string -> bodyA: string -> bodyB: string -> origin: (float * float * float) -> direction: (float * float * float) -> offset: (float * float * float) -> SimulationCommand

/// <summary>Builds a <c>RemoveConstraint</c> command to delete an existing constraint.</summary>
/// <param name="constraintId">The ID of the constraint to remove. Must match a previously added constraint.</param>
/// <returns>A SimulationCommand ready for <c>batchAdd</c>.</returns>
/// <example>
/// <code>
/// let remove = makeRemoveConstraintCmd "joint-1"
/// batchAdd session [remove]
/// </code>
/// </example>
val makeRemoveConstraintCmd : constraintId: string -> SimulationCommand
