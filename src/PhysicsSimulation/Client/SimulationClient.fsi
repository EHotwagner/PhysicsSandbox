module PhysicsSimulation.SimulationClient

open System.Threading

/// Run the simulation client. Connects to the server via SimulationLink,
/// processes commands, and streams state. Blocks until cancellation or
/// server disconnection.
val run : serverAddress: string -> CancellationToken -> Async<unit>
