module PhysicsViewer.ViewerClient

open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts

/// Start streaming simulation state from the server.
/// Enqueues received states into the provided queue.
val streamState:
    serverAddress: string ->
    stateQueue: ConcurrentQueue<SimulationState> ->
    ct: CancellationToken ->
    Task<unit>

/// Start streaming view commands from the server.
/// Enqueues received commands into the provided queue.
val streamViewCommands:
    serverAddress: string ->
    commandQueue: ConcurrentQueue<ViewCommand> ->
    ct: CancellationToken ->
    Task<unit>
