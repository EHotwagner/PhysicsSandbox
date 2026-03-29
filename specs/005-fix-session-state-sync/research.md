# Research: Fix Session State and Cache Synchronization

## R1: Why does `overlapSphere` return stale bodies after reset?

**Decision**: The root cause is NOT client-side caching in `overlapSphere`. The function queries the server directly via gRPC (`PhysicsClient.SimulationCommands.overlap` → `PhysicsHub.Overlap` RPC). The stale results come from the **server itself** because `resetSimulation` returns before the server finishes processing the reset command.

**Evidence**:
- `QueryBuilders.overlapSphere` calls `SimulationCommands.overlap` which issues a synchronous `Overlap` gRPC call (SimulationCommands.fs:400-428)
- `MessageRouter.submitCommand` writes the reset command to a channel and returns `CommandAck(Success=true, Message="Command forwarded to simulation")` immediately (MessageRouter.fs:93-120)
- The simulation processes commands asynchronously from the channel, meaning the reset may not complete before the next client operation

**Alternatives considered**:
- Client-side cache issue → ruled out; overlap queries don't use cache
- Server bug in reset → ruled out; `SimulationWorld.resetSimulation` correctly clears all bodies (SimulationWorld.fs:521-538)

## R2: How to implement confirmed reset?

**Decision**: Add a dedicated `ConfirmedReset` RPC that returns only after the simulation has processed the reset. Use the existing query-response infrastructure (`submitQuery` with `TaskCompletionSource` and correlation ID) as the mechanism for waiting.

**Rationale**: The query infrastructure already solves the "wait for simulation to process and respond" problem. It has a 30-second timeout, correlation IDs, and proper async handling. Adding a similar path for reset avoids duplicating this machinery.

**Alternatives considered**:
1. **Poll-and-verify**: After fire-and-forget reset, poll via overlap query until 0 bodies returned → fragile, adds latency, doesn't guarantee timing
2. **Sleep longer**: Increase the 100ms sleep → unreliable, wastes time in fast cases
3. **New channel + TCS for commands**: Build separate confirmed-command infrastructure → over-engineered for one use case
4. **Reuse query infrastructure**: Submit a "reset query" that the simulation processes and responds to after completing the reset → chosen approach, minimal new code

## R3: What client caches need clearing on reset?

**Decision**: Three caches must be cleared eagerly on the client side when reset is confirmed:
1. `Session.BodyRegistry` (ConcurrentDictionary<string, string>) — already cleared by `SimulationCommands.reset` (line 270)
2. `Session.BodyPropertiesCache` (ConcurrentDictionary<string, BodyProperties>) — NOT currently cleared by client reset
3. `Session.CachedConstraints` and `Session.CachedRegisteredShapes` — NOT currently cleared

**Evidence**: `SimulationCommands.reset` (line 264-272) only clears `BodyRegistry`. The `BodyPropertiesCache` and constraint/shape caches are only cleared when a `Snapshot` PropertyEvent arrives via the background stream — which has unpredictable timing.

**Alternatives considered**:
- Wait for Snapshot PropertyEvent → current behavior, unreliable timing
- Clear all caches eagerly on client side → chosen, deterministic

## R4: Should `batchAdd` return results or throw on failure?

**Decision**: Change `batchAdd` to return a result summary. The server already returns per-command `CommandResult` in `BatchResponse` with `Success`, `Message`, and `Index` fields. The scripting layer currently discards this (only printing failures).

**Rationale**: Returning results lets callers decide how to handle failures (ignore, retry, abort). Throwing would break existing scripts that rely on fire-and-forget semantics.

**Design**:
- New type `BatchResult = { Succeeded: int; Failed: (int * string) list }`
- `batchAdd` returns `BatchResult` (aggregated across all chunks)
- Failures still printed to console for interactive use AND returned for programmatic use
- This is a breaking change to the `.fsi` signature (return type `unit` → `BatchResult`)

**Alternatives considered**:
1. `batchAddChecked` variant that throws → API proliferation, two ways to do the same thing
2. Return `Result<unit, string list>` → loses success count information
3. Return full `BatchResponse list` → leaks proto types into scripting API

## R5: Does `Session.connect` have stale cache issues?

**Decision**: No. `Session.connect` creates all caches empty and populates them via background PropertyEvent stream. The reported issue of "stale bodies after reconnect" is actually the same root cause as R1 — the server still has bodies because the previous reset didn't complete, and the property stream faithfully reports them.

**Evidence**: Session.connect (Session.fs:209-230) initializes `BodyRegistry`, `BodyPropertiesCache`, `CachedConstraints`, and `CachedRegisteredShapes` as empty. The `startPropertyStream` background task receives a `Snapshot` backfill from the server on connect.

## R6: Impact on existing demo scripts and NuGet packages

**Decision**: The `batchAdd` return type change is the only breaking change. All 44 demo scripts (22 F# + 22 Python) call `batchAdd` but ignore the return value, so they will continue to work. The `resetSimulation` signature stays `Session -> unit`.

**Mitigation**:
- `batchAdd` callers that ignore the return value get a compiler warning (unused result) but no error
- PhysicsClient NuGet version bump to 0.7.0 (minor version for new RPC)
- Scripting NuGet version bump with the `batchAdd` return type change
