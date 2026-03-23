# Quickstart: State Stream Bandwidth Optimization

**Feature**: 004-state-stream-optimization

## Implementation Order

### Phase 1: Proto Contracts
1. Add new messages (`BodyPose`, `TickState`, `BodyProperties`, `PropertyEvent`, `PropertySnapshot`) to `physics_hub.proto`
2. Add `StreamProperties` RPC to `PhysicsHub` service
3. Add `exclude_velocity` field to `StateRequest`
4. Build to verify proto compilation

### Phase 2: Server-Side Split (PhysicsSimulation + PhysicsServer)

**Simulation side** (`SimulationWorld.fs`):
1. Add `buildTickState` function — builds `TickState` with only dynamic body poses
2. Add property change detection — track previous semi-static values per body, emit `PropertyEvent` when changed
3. Add lifecycle event emission — `body_created` on add, `body_removed` on remove
4. SimulationLink proto unchanged — simulation continues sending full `SimulationState` upstream. Server decomposes into TickState + PropertyEvents.

**Server side** (`MessageRouter.fs`, `PhysicsHubService.fs`):
1. Add `PropertySubscribers` to `MessageRouter` (same pattern as `Subscribers`)
2. Add `PropertyCache` for semi-static state (like `StateCache` but for properties)
3. Split `publishState` into `publishTick` + `publishPropertyEvent`
4. Implement `StreamProperties` RPC with late-joiner backfill via `PropertySnapshot`
5. Implement velocity exclusion based on `StateRequest.exclude_velocity`

### Phase 3: Client-Side Merging (Viewer, Client, MCP)

**Each client**:
1. Add local `BodyProperties` cache (`ConcurrentDictionary<string, BodyProperties>`)
2. Subscribe to `StreamProperties` in addition to `StreamState`
3. Process `PropertyEvent` messages (create/update/remove body cache entries)
4. Merge tick data with cached properties where full state is needed

**Viewer-specific**:
- Set `exclude_velocity = true` on `StateRequest`
- Cache shape + color from properties; use pose from tick stream

**MCP-specific**:
- Add `PropertyEvent` LogEntry type to recording Types, ChunkWriter, ChunkReader
- Add `OnPropertyEventReceived` callback to `RecordingEngine` — caches BodyProperties locally, records PropertyEvent to disk
- `RecordingEngine.OnStateReceived` accepts `TickState`, reconstructs full `SimulationState` by merging with cached BodyProperties, writes `StateSnapshot` entries (backward-compatible recording format)
- Query tools (query_trajectory, query_snapshots) unchanged — they read reconstructed `SimulationState` snapshots

### Phase 4: Constraints and Registered Shapes
1. Move constraints and registered shapes to `PropertyEvent.snapshot`
2. Server only publishes constraint/shape snapshots on add/remove/modify
3. Clients cache locally

### Phase 5: Tests and Validation
1. Update `.fsi` signature files for all changed modules
2. Update surface area baseline tests
3. Add unit tests for `buildTickState`, property change detection, state merging
4. Add integration tests for split-channel streaming, late-joiner backfill, body removal
5. Verify bandwidth reduction targets (SC-001, SC-002)
6. Run full test suite — all existing tests must pass

## Key Files to Modify

| File | Change |
|------|--------|
| `Protos/physics_hub.proto` | New messages, modified RPCs |
| `PhysicsSimulation/World/SimulationWorld.fs` | `buildTickState`, property change detection |
| `PhysicsSimulation/Client/SimulationClient.fs` | Send split state |
| `PhysicsServer/Hub/MessageRouter.fs` | Property subscribers, split publish |
| `PhysicsServer/Hub/StateCache.fs` | Dual cache (tick + properties) |
| `PhysicsServer/Services/PhysicsHubService.fs` | `StreamProperties` RPC, velocity exclusion |
| `PhysicsServer/Services/SimulationLinkService.fs` | Handle split state from simulation |
| `PhysicsViewer/Streaming/ViewerClient.fs` | Subscribe to properties, exclude velocity |
| `PhysicsViewer/Rendering/SceneManager.fs` | Merge tick + cached properties |
| `PhysicsClient/Connection/Session.fs` | Properties cache, dual subscription |
| `PhysicsClient/Display/StateDisplay.fs` | Read from merged state |
| `PhysicsSandbox.Mcp/GrpcConnection.fs` | Subscribe to properties stream |
| `PhysicsSandbox.Mcp/Recording/RecordingEngine.fs` | Reconstruct full state for recording |

## Verification

```bash
# Build after proto changes
dotnet build PhysicsSandbox.slnx

# Run all tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Manual bandwidth verification
# 1. Start system: ./start.sh
# 2. Create 200 bodies via MCP or client
# 3. Check MetricsCounter output for message sizes
# 4. Compare against baseline (~50 KB/tick → target <15 KB/tick)
```
