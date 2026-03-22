module PhysicsSandbox.Scripting.BatchOperations

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

val batchAdd : Session -> SimulationCommand list -> unit
