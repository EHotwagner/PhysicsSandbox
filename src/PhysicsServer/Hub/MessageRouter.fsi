module PhysicsServer.Hub.MessageRouter

open System
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

/// Subscribe to command audit events. The callback is invoked for each command.
/// Returns a subscription id for later unsubscription.
val subscribeCommands: MessageRouter -> (CommandEvent -> Task) -> System.Guid

/// Remove a command audit subscription.
val unsubscribeCommands: MessageRouter -> System.Guid -> unit

/// Publish a command event to all command audit subscribers.
val publishCommandEvent: MessageRouter -> CommandEvent -> Task<unit>

/// Publish a simulation state to all subscribers and update the cache.
val publishState: MessageRouter -> SimulationState -> Task<unit>

/// Get the latest cached simulation state (for late joiners).
val getLatestState: MessageRouter -> SimulationState option

/// Try to register as the active simulation. Returns true if successful, false if already occupied.
val tryConnectSimulation: MessageRouter -> bool

/// Unregister the active simulation.
val disconnectSimulation: MessageRouter -> unit

/// Get a snapshot of the server's performance metrics.
val getMetrics: MessageRouter -> ServiceMetricsReport

/// Get the metrics state for periodic logging setup.
val metricsState: MessageRouter -> MetricsCounter.MetricsState

/// Access the mesh cache for FetchMeshes RPC.
val meshCache: MessageRouter -> MeshCache.MeshCacheState

/// Submit a batch of simulation commands. Returns per-command results.
val sendBatchCommand: MessageRouter -> BatchSimulationRequest -> BatchResponse

/// Submit a batch of view commands. Returns per-command results.
val sendBatchViewCommand: MessageRouter -> BatchViewRequest -> BatchResponse

/// Read a pending simulation command. Blocks until one is available or cancellation.
val readCommand: MessageRouter -> CancellationToken -> Task<SimulationCommand option>

/// Submit a query through the command channel and wait for the response.
val submitQuery: MessageRouter -> QueryRequest -> CancellationToken -> Task<QueryResponse>

/// Process query responses from a simulation state update.
val processQueryResponses: SimulationState -> unit

/// Read a pending view command. Blocks until one is available or cancellation.
val readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>
