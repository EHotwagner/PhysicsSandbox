/// <summary>
/// Auto-opened convenience module that re-exports all scripting functions.
/// Opening <c>PhysicsSandbox.Scripting.Prelude</c> (or just referencing the assembly)
/// makes all helpers, builders, and lifecycle functions available without qualification.
/// </summary>
[<AutoOpen>]
module PhysicsSandbox.Scripting.Prelude

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

/// <summary>Unwraps a Result value, throwing on Error. See <see cref="M:PhysicsSandbox.Scripting.Helpers.ok``1"/>.</summary>
let ok r = PhysicsSandbox.Scripting.Helpers.ok r
/// <summary>Thread sleep in milliseconds. See <see cref="M:PhysicsSandbox.Scripting.Helpers.sleep"/>.</summary>
let sleep ms = PhysicsSandbox.Scripting.Helpers.sleep ms
/// <summary>Timed execution with console logging. See <see cref="M:PhysicsSandbox.Scripting.Helpers.timed``1"/>.</summary>
let timed label f = PhysicsSandbox.Scripting.Helpers.timed label f
/// <summary>Tuple to Vec3 conversion. See <see cref="M:PhysicsSandbox.Scripting.Vec3Builders.toVec3"/>.</summary>
let toVec3 v = PhysicsSandbox.Scripting.Vec3Builders.toVec3 v
/// <summary>Vec3 to tuple conversion. See <see cref="M:PhysicsSandbox.Scripting.Vec3Builders.toTuple"/>.</summary>
let toTuple v = PhysicsSandbox.Scripting.Vec3Builders.toTuple v
/// <summary>Build a sphere AddBody command. See <see cref="M:PhysicsSandbox.Scripting.CommandBuilders.makeSphereCmd"/>.</summary>
let makeSphereCmd id pos radius mass = PhysicsSandbox.Scripting.CommandBuilders.makeSphereCmd id pos radius mass
/// <summary>Build a box AddBody command. See <see cref="M:PhysicsSandbox.Scripting.CommandBuilders.makeBoxCmd"/>.</summary>
let makeBoxCmd id pos halfExtents mass = PhysicsSandbox.Scripting.CommandBuilders.makeBoxCmd id pos halfExtents mass
/// <summary>Build an impulse command. See <see cref="M:PhysicsSandbox.Scripting.CommandBuilders.makeImpulseCmd"/>.</summary>
let makeImpulseCmd bodyId impulse = PhysicsSandbox.Scripting.CommandBuilders.makeImpulseCmd bodyId impulse
/// <summary>Build a torque command. See <see cref="M:PhysicsSandbox.Scripting.CommandBuilders.makeTorqueCmd"/>.</summary>
let makeTorqueCmd bodyId torque = PhysicsSandbox.Scripting.CommandBuilders.makeTorqueCmd bodyId torque
/// <summary>Send commands in auto-chunked batches. See <see cref="M:PhysicsSandbox.Scripting.BatchOperations.batchAdd"/>.</summary>
let batchAdd (s: Session) (commands: SimulationCommand list) = PhysicsSandbox.Scripting.BatchOperations.batchAdd s commands
/// <summary>Reset simulation to clean state. See <see cref="M:PhysicsSandbox.Scripting.SimulationLifecycle.resetSimulation"/>.</summary>
let resetSimulation (s: Session) = PhysicsSandbox.Scripting.SimulationLifecycle.resetSimulation s
/// <summary>Run simulation for N seconds then pause. See <see cref="M:PhysicsSandbox.Scripting.SimulationLifecycle.runFor"/>.</summary>
let runFor (s: Session) (seconds: float) = PhysicsSandbox.Scripting.SimulationLifecycle.runFor s seconds
/// <summary>Generate sequential body ID. See <see cref="M:PhysicsSandbox.Scripting.SimulationLifecycle.nextId"/>.</summary>
let nextId prefix = PhysicsSandbox.Scripting.SimulationLifecycle.nextId prefix
