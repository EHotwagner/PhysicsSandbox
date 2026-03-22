module PhysicsSandbox.Scripting.SimulationLifecycle

open PhysicsClient.Session

val resetSimulation : Session -> unit
val runFor : Session -> float -> unit
val nextId : string -> string
