(**
---
title: Test Suite Documentation
category: Reference
categoryindex: 5
index: 5
description: 467 tests across 7 projects — unit tests, integration tests, and surface-area validation.
---
*)

(**
# Test Suite Documentation

The Physics Sandbox has 467 tests across 7 projects, covering unit tests for each
service layer, surface-area validation to guard against accidental API changes,
and end-to-end integration tests using .NET Aspire's distributed application testing
infrastructure.

## Summary

| Project | Language | Framework | Files | Tests |
|---|---|---|---|---|
| PhysicsSimulation.Tests | F# | xUnit | 11 | 114 |
| PhysicsViewer.Tests | F# | xUnit | 11 | 99 |
| PhysicsClient.Tests | F# | xUnit | 11 | 78 |
| PhysicsServer.Tests | F# | xUnit | 7 | 48 |
| PhysicsSandbox.Scripting.Tests | F# | xUnit | 5 | 26 |
| PhysicsSandbox.Mcp.Tests | F# | xUnit | 4 | 18 |
| PhysicsSandbox.Integration.Tests | C# | xUnit + Aspire | 14 | 84 |
| **Total** | | | **63** | **467** |

All F# unit test projects follow the same pattern: focused module-level tests
that exercise logic in isolation (no gRPC connections, no Aspire orchestrator),
plus surface-area tests that use reflection to verify public APIs have not
drifted from their baselines. The integration tests spin up the full Aspire
distributed application (server, simulation, viewer, MCP) and exercise
gRPC round-trips end to end.

---

## PhysicsClient.Tests (78 tests)

### GeneratorsTests.fs -- Body generator count computations (10 tests)

Tests the mathematical formulas underlying the `Generators` module: stack counts,
grid row-by-column products, pyramid triangular-number sums, and random-number
seeding. Since generators require a live gRPC session, only the pure computation
and validation logic is tested here.

### IdGeneratorTests.fs -- Sequential ID generation (4 tests)

Verifies that `nextId` produces sequential IDs per shape prefix, that different
shape kinds maintain independent counters, that `reset` clears all counters,
and that concurrent calls are thread-safe (100 parallel increments yield 100
unique IDs).

### PresetsTests.fs -- Preset body definitions (6 tests)

Validates that preset body names use the correct ID prefixes (`sphere-`, `box-`),
that sequential IDs are unique, and that the hardcoded physical constants (masses,
radii, half-extents) for all seven presets (marble, bowling ball, beach ball,
crate, brick, boulder, die) are positive and ordered by size.

### SessionTests.fs -- gRPC session lifecycle (3 tests)

Tests that `connect` creates a session reporting `isConnected = true` (gRPC
channels are lazy), that `disconnect` flips the flag to `false`, and that
a disconnected session correctly reports its state.

### SimulationCommandsTests.fs -- Proto message construction (8 tests)

Exercises the protobuf-generated `SimulationCommand` and `ViewCommand` wrappers:
verifying `Vec3` field storage, `AddBody` with sphere shape, `PlayPause` toggling,
`SetCamera` with position/target/up vectors, and ID generation integration.

### StateDisplayTests.fs -- Display formatting helpers (6 tests)

Tests `formatVec3` (2-decimal formatting, null handling), `velocityMagnitude`
(Euclidean norm, null safety), and `shapeDescription` (Sphere/Box label
rendering with dimension values).

### SteeringTests.fs -- Direction-to-vector mapping (6 tests)

Verifies that all six cardinal directions (`Up`, `Down`, `North`, `South`,
`East`, `West`) map to the correct unit-vector components in the physics
coordinate system.

### SurfaceAreaTests.fs -- Public API surface validation (9 tests)

Uses reflection to assert that every public module (`IdGenerator`, `Session`,
`SimulationCommands`, `ViewCommands`, `Presets`, `Generators`, `Steering`,
`StateDisplay`, `LiveWatch`) exposes exactly the expected set of public members.
These tests guard against accidental API removals or renames.

#### Representative test -- IdGenerator thread-safety
*)

(*** do-not-eval ***)
[<Fact>]
let ``nextId is thread-safe`` () =
    let shape = $"threadsafe-{System.Guid.NewGuid():N}"
    let count = 100
    let ids = System.Collections.Concurrent.ConcurrentBag<string>()
    System.Threading.Tasks.Parallel.For(0, count, fun _ ->
        ids.Add(nextId shape)
    ) |> ignore
    let unique = ids |> Seq.distinct |> Seq.length
    Assert.Equal(count, unique)
    Assert.Equal(count, ids.Count)

(**
---

## PhysicsServer.Tests (48 tests)

### BatchRoutingTests.fs -- Batch command dispatch (5 tests)

Tests that `sendBatchCommand` routes each command individually and returns
per-command results with correct indices, that batches exceeding 100 commands
are rejected with an error, that `sendBatchViewCommand` handles view commands,
and that total execution time is measured.

### MessageRouterTests.fs -- Server hub message routing (12 tests)

Covers the core `MessageRouter` module: command submission with and without a
connected simulation, state fanout to multiple subscribers, simulation connection
management (connect/reject-second/disconnect-reconnect), view command submission
and reading (including cancellation and blocking behavior), and command-event
pub/sub with subscriber lifecycle.

### MetricsCounterTests.fs -- Throughput metrics (6 tests)

Validates the `MetricsCounter` module: zero-initialization, accumulation of
sent/received message counts and byte totals, independence of sent vs. received
counters, point-in-time snapshot consistency, and thread-safety under 10,000
concurrent increments.

### StateCacheTests.fs -- Latest-state caching (4 tests)

Tests the `StateCache` module: empty cache returns `None`, updating stores the
latest state, a second update overwrites the first, and `clear` removes the
cached state.

#### Representative test -- State fanout to multiple subscribers
*)

(*** do-not-eval ***)
[<Fact>]
let ``State fanout delivers to multiple subscribers`` () =
    task {
        let router = create ()
        let received1 = ResizeArray<SimulationState>()
        let received2 = ResizeArray<SimulationState>()
        use cts = new CancellationTokenSource()

        let sub1 = subscribe router (fun state -> received1.Add(state); Task.CompletedTask)
        let sub2 = subscribe router (fun state -> received2.Add(state); Task.CompletedTask)

        let state = SimulationState(Time = 1.0, Running = true)
        do! publishState router state

        Assert.Single(received1) |> ignore
        Assert.Single(received2) |> ignore
        Assert.Equal(1.0, received1.[0].Time)
        Assert.Equal(1.0, received2.[0].Time)

        unsubscribe router sub1
        unsubscribe router sub2
    }

(**
---

## PhysicsSimulation.Tests (114 tests)

### CommandHandlerTests.fs -- Command dispatch (11 tests)

Tests that the `CommandHandler.handle` function correctly dispatches each
command type: `PlayPause` sets the running state, `StepSimulation` advances
time, `AddBody` creates a body (including error on invalid mass), `RemoveBody`
deletes it, `ApplyForce`/`ClearForces` work on existing bodies, and
`SetGravity` updates the world gravity. Also verifies graceful handling of
unknown commands and forces on non-existent bodies.

### ResetSimulationTests.fs -- Simulation reset (5 tests)

Verifies that `resetSimulation` clears all bodies, resets simulation time to
zero, sets running to false, clears active forces (allowing re-addition of
bodies with the same IDs), and returns a success acknowledgment with a
descriptive message.

### SimulationWorldTests.fs -- Physics world operations (23 tests)

The largest simulation test file, covering world creation (paused, time zero),
stepping (time advance, running flag), body management (add sphere/box, duplicate
ID rejection, zero/negative mass validation, removal), force/impulse/torque
application (persistent force vs. one-shot impulse, clearForces stopping
acceleration), gravity (downward acceleration, zero-gravity, mid-simulation
gravity change), and stress scenarios (empty world stepping, extremely large
forces not crashing, 100-body stable operation over 60 steps).

### StaticBodyTrackingTests.fs -- Static body lifecycle (7 tests)

Tests that planes are created as static bodies with `IsStatic = true` and zero
velocity, that static and dynamic bodies coexist in the state, that static
bodies can be removed, that `resetSimulation` clears static bodies, and that
duplicate static body IDs are rejected.

### SurfaceAreaTests.fs -- Public API surface validation (3 tests)

Uses reflection to verify the public APIs of `SimulationWorld` (14 members),
`CommandHandler` (1 member), and `SimulationClient` (1 member) match their
expected baselines.

#### Representative test -- 100-body stable operation
*)

(*** do-not-eval ***)
[<Fact>]
let ``100 bodies stable operation`` () =
    let world = create ()
    try
        for i in 0..99 do
            let y = float (i * 2)
            let cmd = AddBody(Id = $"body{i}", Mass = 1.0)
            cmd.Position <- Vec3(X = 0.0, Y = y, Z = 0.0)
            cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
            let ack = addBody world cmd
            Assert.True(ack.Success, $"Failed to add body{i}: {ack.Message}")

        // Step 60 times (1 second of simulation)
        for _ in 1..60 do
            let state = step world
            Assert.Equal(100, state.Bodies.Count)

        let finalState = currentState world
        Assert.Equal(100, finalState.Bodies.Count)
    finally
        destroy world

(**
---

## PhysicsViewer.Tests (99 tests)

### CameraControllerTests.fs -- Camera state management (6 tests)

Tests the pure-functional `CameraController` module: default camera position
`(10, 8, 10)`, origin target, zoom level 1.0, `applySetCamera` overriding
position and target from a `SetCamera` proto message, `applySetZoom` updating
the zoom level, and verifying that zoom is preserved across camera changes.

### FpsCounterTests.fs -- FPS smoothing and threshold detection (8 tests)

Verifies the exponential moving average (EMA) FPS counter: default 60 FPS
initialization, EMA smoothing at 60 FPS input, gradual smoothing of sudden FPS
drops, handling of very large deltas (window minimized), periodic logging
triggers with elapsed-time reset, low-FPS threshold detection, and graceful
handling of zero-delta frames.

### SceneManagerTests.fs -- Scene state and shape classification (7 tests)

Tests `classifyShape` for Sphere, Box, null, and unset shapes (returning the
correct `ShapeKind` enum), and validates initial `SceneState` values: zero
simulation time, `isRunning = false`, and `isWireframe = false`.

### SurfaceAreaTests.fs -- Public API surface validation (3 tests)

Uses reflection to verify the public APIs of `SceneManager` (7 members),
`CameraController` (8 members), and `ViewerClient` (2 members) match their
expected baselines.

#### Representative test -- FPS EMA smoothing
*)

(*** do-not-eval ***)
[<Fact>]
let ``update smooths FPS drop gradually`` () =
    let state = FpsCounter.create 30.0f
    // Start at 60 FPS
    FpsCounter.update (1.0f / 60.0f) state |> ignore
    // Sudden drop to 30 FPS
    let fps = FpsCounter.update (1.0f / 30.0f) state
    // EMA: 0.1 * 30 + 0.9 * ~60 = ~57 (not instant drop)
    Assert.True(fps > 50.0f, $"FPS should be smoothed, got {fps}")

(**
---

## PhysicsSandbox.Integration.Tests (84 tests)

These C# tests use `Aspire.Hosting.Testing` to spin up the full distributed
application (AppHost, server, simulation, viewer, MCP) and exercise gRPC
round-trips through the real server hub. Each test creates a fresh Aspire
application instance, waits for resources to become healthy, and communicates
via `PhysicsHub.PhysicsHubClient` over HTTPS with dev-certificate bypass.

### BatchIntegrationTests.cs -- End-to-end batch commands (2 tests)

Sends a batch of 10 AddBody commands and verifies all 10 return success with
correct indices and non-negative total time. Also verifies that batches
exceeding 100 commands are rejected.

### CommandAuditStreamTests.cs -- Audit stream availability (1 test)

Verifies that the MCP server reaches Running state alongside the server,
confirming the command audit streaming infrastructure is available.

### CommandRoutingTests.cs -- Full command routing pipeline (9 tests)

End-to-end tests for AddBody, ApplyForce, ApplyImpulse, ApplyTorque,
SetGravity, StepSimulation, PlayPause, RemoveBody, and ClearForces.
Each test sends commands via gRPC and reads the resulting state stream
to verify physics effects (velocity changes, position changes, body
presence/absence).

### ComparisonIntegrationTests.cs -- Batch vs. individual performance (1 test)

Sends 20 step commands individually, then 20 in a single batch, measuring
both execution times. Verifies both approaches succeed without asserting
strict performance ratios (to avoid CI flakiness).

### DiagnosticsIntegrationTests.cs -- Pipeline metrics (1 test)

Adds 10 bodies, enables play mode, waits for simulation ticks, then queries
`GetMetrics` and verifies the pipeline timings response contains non-negative
values.

### ErrorConditionTests.cs -- Graceful error handling (4 tests)

Tests sending commands without a simulation connected (expects "dropped" or
"forwarded"), sending empty commands (no exception), streaming state without
a simulation (empty or cached state), and firing 200 rapid concurrent step
commands without crashing the server.

### McpHttpTransportTests.cs -- MCP HTTP transport (2 tests)

Verifies the MCP server stays running as an HTTP service without clients
connected (unlike stdio transport), and that its `/health` endpoint returns
a success status code.

### McpOrchestrationTests.cs -- MCP Aspire orchestration (3 tests)

Tests that the MCP resource appears in the Aspire dashboard (reaches Running),
shuts down gracefully, and correctly waits for the server to become healthy
before starting.

### MetricsIntegrationTests.cs -- Metrics collection (2 tests)

Sends 5 step commands then queries `GetMetrics`, verifying the PhysicsServer
service report shows at least 5 messages received with non-zero bytes. Also
verifies that pipeline timings are present in the response.

### RestartIntegrationTests.cs -- Simulation reset (1 test)

Adds 5 bodies, sends a `ResetSimulation` command, then reads the state stream
to confirm 0 bodies and time reset to 0.0.

### ServerHubTests.cs -- Core gRPC service endpoints (5 tests)

Tests `SendCommand` returns success, `StreamState` opens without error,
`SendViewCommand` returns success, `StreamViewCommands` receives a forwarded
SetZoom command with the correct level, and the view command stream opens
without error.

### SimulationConnectionTests.cs -- Simulation lifecycle (5 tests)

Verifies the simulation connects after Aspire startup and produces state
messages, that AddBody commands create visible state, that gravity causes
bodies to fall, that PlayPause sets Running=true, and that the simulation
maintains its connection for 30 consecutive seconds (14+ successful
round-trips).

### StateStreamingTests.cs -- State streaming guarantees (4 tests)

Tests that 3 concurrent subscribers receive the same body count and time,
that a late-joining subscriber receives cached state with bodies, that state
updates arrive within 1 second of a step command, and that SetCamera view
commands are forwarded with correct position/target coordinates.

### StaticBodyTests.cs -- Static body integration (1 test)

Adds a static plane and a dynamic sphere via gRPC, then reads the state
stream to verify the plane has `IsStatic = true` and the sphere has
`IsStatic = false`.

### StressTestIntegrationTests.cs -- Batch scaling (1 test)

Sends a batch of 50 AddBody commands in a single request and verifies at
least 45 succeed, confirming the system handles moderate-scale batches.

#### Representative test -- Rapid concurrent commands
*)

(*** do-not-eval ***)
// C# integration test (shown here for reference)
//
// [Fact]
// public async Task RapidCommands_DoNotCrashServer()
// {
//     var (app, channel) = await StartAppAndConnect();
//     await using var _ = app;
//
//     var client = new PhysicsHub.PhysicsHubClient(channel);
//
//     var tasks = Enumerable.Range(0, 200).Select(_ =>
//     {
//         var command = new SimulationCommand
//         {
//             Step = new StepSimulation { DeltaTime = 0.016 }
//         };
//         return client.SendCommandAsync(command).ResponseAsync;
//     }).ToArray();
//
//     var acks = await Task.WhenAll(tasks);
//
//     Assert.All(acks, ack => Assert.True(ack.Success));
// }

(**
---

## Test Design Patterns

### Surface-area tests

Every F# project includes a `SurfaceAreaTests.fs` file that uses reflection to
enumerate public members of each module and assert they match an expected baseline.
This catches accidental API removals or renames at compile time, ensuring that
`.fsi` signature files, proto-generated types, and module implementations stay
in sync.

### Resource lifecycle

All simulation tests that create a `World` use `try`/`finally` to call `destroy`,
ensuring BepuPhysics2 native resources are released even when assertions fail.

### Integration test isolation

Each integration test creates its own Aspire `DistributedApplication` instance,
providing full isolation between tests at the cost of startup time. Tests wait
for resource health checks before exercising gRPC endpoints, and use
`CancellationTokenSource` timeouts to prevent hangs.

### Thread-safety validation

Both `IdGeneratorTests` and `MetricsCounterTests` use `Parallel.For` with
high iteration counts to verify that concurrent access to shared mutable state
produces correct results without data races.
*)
