module PhysicsSimulation.CommandHandler

open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

/// Process a simulation command against the world.
/// Returns a CommandAck indicating success or failure.
val handle : World -> SimulationCommand -> CommandAck
