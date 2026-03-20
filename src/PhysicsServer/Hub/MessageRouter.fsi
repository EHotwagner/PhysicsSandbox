module PhysicsServer.Hub.MessageRouter

open System.Threading
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts
open StateCache

/// Opaque handle to the message router.
type MessageRouter

/// Opaque handle to a state stream subscription.
type SubscriptionId

/// Create a new message router with an internal state cache.
val create: unit -> MessageRouter

/// Submit a simulation command. Returns an acknowledgment.
/// Commands are buffered for a connected simulation; dropped if none connected.
val submitCommand: MessageRouter -> SimulationCommand -> CommandAck

/// Submit a view command. Returns an acknowledgment.
/// View commands are buffered for a connected viewer; dropped if none connected.
val submitViewCommand: MessageRouter -> ViewCommand -> CommandAck

/// Subscribe to state updates. The callback is invoked for each new state.
/// Returns a subscription id for later unsubscription.
val subscribe: MessageRouter -> (SimulationState -> Task) -> SubscriptionId

/// Remove a state stream subscription.
val unsubscribe: MessageRouter -> SubscriptionId -> unit

/// Publish a simulation state to all subscribers and update the cache.
val publishState: MessageRouter -> SimulationState -> Task<unit>

/// Get the latest cached simulation state (for late joiners).
val getLatestState: MessageRouter -> SimulationState option

/// Try to register as the active simulation. Returns true if successful, false if already occupied.
val tryConnectSimulation: MessageRouter -> bool

/// Unregister the active simulation.
val disconnectSimulation: MessageRouter -> unit

/// Read a pending simulation command. Blocks until one is available or cancellation.
val readCommand: MessageRouter -> CancellationToken -> Task<SimulationCommand option>

/// Read a pending view command. Blocks until one is available or cancellation.
val readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>
