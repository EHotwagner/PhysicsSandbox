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
