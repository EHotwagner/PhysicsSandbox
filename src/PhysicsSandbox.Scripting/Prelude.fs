[<AutoOpen>]
module PhysicsSandbox.Scripting.Prelude

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

let ok r = PhysicsSandbox.Scripting.Helpers.ok r
let sleep ms = PhysicsSandbox.Scripting.Helpers.sleep ms
let timed label f = PhysicsSandbox.Scripting.Helpers.timed label f
let toVec3 v = PhysicsSandbox.Scripting.Vec3Builders.toVec3 v
let toTuple v = PhysicsSandbox.Scripting.Vec3Builders.toTuple v
let makeSphereCmd id pos radius mass = PhysicsSandbox.Scripting.CommandBuilders.makeSphereCmd id pos radius mass
let makeBoxCmd id pos halfExtents mass = PhysicsSandbox.Scripting.CommandBuilders.makeBoxCmd id pos halfExtents mass
let makeImpulseCmd bodyId impulse = PhysicsSandbox.Scripting.CommandBuilders.makeImpulseCmd bodyId impulse
let makeTorqueCmd bodyId torque = PhysicsSandbox.Scripting.CommandBuilders.makeTorqueCmd bodyId torque
let batchAdd (s: Session) (commands: SimulationCommand list) = PhysicsSandbox.Scripting.BatchOperations.batchAdd s commands
let resetSimulation (s: Session) = PhysicsSandbox.Scripting.SimulationLifecycle.resetSimulation s
let runFor (s: Session) (seconds: float) = PhysicsSandbox.Scripting.SimulationLifecycle.runFor s seconds
let nextId prefix = PhysicsSandbox.Scripting.SimulationLifecycle.nextId prefix
