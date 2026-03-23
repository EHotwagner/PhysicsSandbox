module PhysicsViewer.ViewerClient

open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts

/// Start streaming lean tick state from the server.
/// Enqueues received TickState messages into the provided queue.
val streamState:
    serverAddress: string ->
    stateQueue: ConcurrentQueue<TickState> ->
    excludeVelocity: bool ->
    ct: CancellationToken ->
    Task<unit>

/// Start streaming property events from the server.
/// Enqueues received PropertyEvent messages into the provided queue.
val streamProperties:
    serverAddress: string ->
    eventQueue: ConcurrentQueue<PropertyEvent> ->
    ct: CancellationToken ->
    Task<unit>

/// Start streaming view commands from the server.
/// Enqueues received commands into the provided queue.
val streamViewCommands:
    serverAddress: string ->
    commandQueue: ConcurrentQueue<ViewCommand> ->
    ct: CancellationToken ->
    Task<unit>
