module PhysicsSandbox.Scripting.CommandBuilders

open PhysicsSandbox.Shared.Contracts

val makeSphereCmd : id: string -> pos: (float * float * float) -> radius: float -> mass: float -> SimulationCommand
val makeBoxCmd : id: string -> pos: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> SimulationCommand
val makeImpulseCmd : bodyId: string -> impulse: (float * float * float) -> SimulationCommand
val makeTorqueCmd : bodyId: string -> torque: (float * float * float) -> SimulationCommand
