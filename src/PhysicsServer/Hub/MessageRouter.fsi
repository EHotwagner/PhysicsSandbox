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
val submitCommand: MessageRouter -> SimulationCommand -> CommandAck

/// Submit a view command. Returns an acknowledgment.
val submitViewCommand: MessageRouter -> ViewCommand -> CommandAck

/// Subscribe to lean tick state updates (60 Hz, dynamic body poses).
val subscribe: MessageRouter -> (TickState -> Task) -> SubscriptionId

/// Remove a tick state subscription.
val unsubscribe: MessageRouter -> SubscriptionId -> unit

/// Subscribe to property events (body lifecycle, semi-static changes).
val subscribeProperties: MessageRouter -> (PropertyEvent -> Task) -> SubscriptionId

/// Remove a property event subscription.
val unsubscribeProperties: MessageRouter -> SubscriptionId -> unit

/// Get the latest cached property snapshot (for late joiners).
val getPropertySnapshot: MessageRouter -> PropertySnapshot option

/// Subscribe to command audit events.
val subscribeCommands: MessageRouter -> (CommandEvent -> Task) -> System.Guid

/// Remove a command audit subscription.
val unsubscribeCommands: MessageRouter -> System.Guid -> unit

/// Publish a command event to all command audit subscribers.
val publishCommandEvent: MessageRouter -> CommandEvent -> Task<unit>

/// Publish a simulation state from the simulation upstream.
/// Decomposes into TickState + PropertyEvents for downstream clients.
val publishState: MessageRouter -> SimulationState -> Task<unit>

/// Get the latest cached tick state (for late joiners).
val getLatestState: MessageRouter -> TickState option

/// Try to register as the active simulation.
val tryConnectSimulation: MessageRouter -> bool

/// Unregister the active simulation.
val disconnectSimulation: MessageRouter -> unit

/// Get a snapshot of the server's performance metrics.
val getMetrics: MessageRouter -> ServiceMetricsReport

/// Get the metrics state for periodic logging setup.
val metricsState: MessageRouter -> MetricsCounter.MetricsState

/// Access the mesh cache for FetchMeshes RPC.
val meshCache: MessageRouter -> MeshCache.MeshCacheState

/// Submit a batch of simulation commands.
val sendBatchCommand: MessageRouter -> BatchSimulationRequest -> BatchResponse

/// Submit a batch of view commands.
val sendBatchViewCommand: MessageRouter -> BatchViewRequest -> BatchResponse

/// Read a pending simulation command.
val readCommand: MessageRouter -> CancellationToken -> Task<SimulationCommand option>

/// Submit a query through the command channel and wait for the response.
val submitQuery: MessageRouter -> QueryRequest -> CancellationToken -> Task<QueryResponse>

/// Process query responses from a simulation state update.
val processQueryResponses: SimulationState -> unit

/// Read a pending view command.
val readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>
