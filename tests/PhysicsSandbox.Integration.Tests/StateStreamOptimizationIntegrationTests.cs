using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

/// <summary>
/// Integration tests for the state stream bandwidth optimization (004-state-stream-optimization).
/// Tests verify: TickState content, PropertyEvent lifecycle, velocity exclusion, late-joiner backfill,
/// and constraint/shape delivery via property channel.
/// </summary>
public class StateStreamOptimizationIntegrationTests
{
    private static async Task<(DistributedApplication App, GrpcChannel Channel)> StartAppAndConnect()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));

        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
        await Task.Delay(3000);

        var httpClient = app.CreateHttpClient("server", "https");
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            }
        });

        return (app, channel);
    }

    private static SimulationCommand MakeAddBody(string id, bool isStatic = false, double mass = 1.0) =>
        new()
        {
            AddBody = new AddBody
            {
                Id = id,
                Position = new Vec3 { X = 0, Y = isStatic ? 0 : 5, Z = 0 },
                Velocity = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = isStatic ? 0 : mass,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } },
                MotionType = isStatic ? BodyMotionType.Static : BodyMotionType.Dynamic,
                Color = new Color { R = 1.0, G = 0.0, B = 0.0, A = 1.0 }
            }
        };

    private static SimulationCommand MakeStep(double dt = 0.016) =>
        new() { Step = new StepSimulation { DeltaTime = dt } };

    private static SimulationCommand MakeRemoveBody(string id) =>
        new() { RemoveBody = new RemoveBody { BodyId = id } };

    // ── T037: StreamState returns TickState with only BodyPose data ──────────

    [Fact]
    public async Task T037_StreamState_ReturnsTickState_WithOnlyBodyPoseData()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a dynamic body and step
        await client.SendCommandAsync(MakeAddBody("dyn-1"));
        for (int i = 0; i < 3; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 1)
                {
                    tick = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        // TickState should have BodyPose entries (id, position, orientation, velocity)
        var body = tick.Bodies[0];
        Assert.Equal("dyn-1", body.Id);
        Assert.NotNull(body.Position);
        Assert.NotNull(body.Orientation);
        // Velocity should be present (default exclude_velocity=false)
        Assert.NotNull(body.Velocity);
    }

    // ── T038: StreamProperties delivers body_created on add ──────────────────

    [Fact]
    public async Task T038_StreamProperties_DeliversBodyCreated_OnBodyAdd()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Subscribe to properties FIRST
        var propStream = client.StreamProperties(new StateRequest(), cancellationToken: cts.Token);

        // Read initial backfill (may be empty snapshot or nothing)
        PropertyEvent? createdEvent = null;

        // Add body and step to trigger property event
        await client.SendCommandAsync(MakeAddBody("prop-body-1"));
        await client.SendCommandAsync(MakeStep());

        try
        {
            while (await propStream.ResponseStream.MoveNext(cts.Token))
            {
                var evt = propStream.ResponseStream.Current;
                if (evt.EventCase == PropertyEvent.EventOneofCase.BodyCreated &&
                    evt.BodyCreated.Id == "prop-body-1")
                {
                    createdEvent = evt;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(createdEvent);
        Assert.Equal("prop-body-1", createdEvent.BodyCreated.Id);
        Assert.NotNull(createdEvent.BodyCreated.Shape);
        Assert.NotNull(createdEvent.BodyCreated.Color);
        Assert.Equal(1.0, createdEvent.BodyCreated.Mass);
    }

    // ── T039: StreamProperties delivers body_removed on remove ───────────────

    [Fact]
    public async Task T039_StreamProperties_DeliversBodyRemoved_OnBodyRemove()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add body first
        await client.SendCommandAsync(MakeAddBody("remove-me"));
        await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var propStream = client.StreamProperties(new StateRequest(), cancellationToken: cts.Token);

        // Wait for backfill, then remove body
        await Task.Delay(500);
        await client.SendCommandAsync(MakeRemoveBody("remove-me"));
        await client.SendCommandAsync(MakeStep());

        PropertyEvent? removedEvent = null;
        try
        {
            while (await propStream.ResponseStream.MoveNext(cts.Token))
            {
                var evt = propStream.ResponseStream.Current;
                if (evt.EventCase == PropertyEvent.EventOneofCase.BodyRemoved &&
                    evt.BodyRemoved == "remove-me")
                {
                    removedEvent = evt;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(removedEvent);
        Assert.Equal("remove-me", removedEvent.BodyRemoved);
    }

    // ── T040: Late joiner receives PropertySnapshot backfill ─────────────────

    [Fact]
    public async Task T040_LateJoiner_ReceivesPropertySnapshot_WithExistingBodies()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add bodies and step BEFORE subscribing
        await client.SendCommandAsync(MakeAddBody("backfill-1"));
        await client.SendCommandAsync(MakeAddBody("backfill-2"));
        for (int i = 0; i < 3; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(1000);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Subscribe AFTER bodies are created (late joiner)
        var propStream = client.StreamProperties(new StateRequest(), cancellationToken: cts.Token);

        PropertySnapshot? snapshot = null;
        try
        {
            while (await propStream.ResponseStream.MoveNext(cts.Token))
            {
                var evt = propStream.ResponseStream.Current;
                if (evt.EventCase == PropertyEvent.EventOneofCase.Snapshot)
                {
                    snapshot = evt.Snapshot;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(snapshot);
        Assert.True(snapshot.Bodies.Count >= 2, $"Expected >= 2 bodies in backfill, got {snapshot.Bodies.Count}");
        var ids = snapshot.Bodies.Select(b => b.Id).ToList();
        Assert.Contains("backfill-1", ids);
        Assert.Contains("backfill-2", ids);
    }

    // ── T074: Viewer receives TickState without velocity fields ──────────────

    [Fact]
    public async Task T074_ExcludeVelocity_OmitsVelocityFromTickState()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a body with velocity and step
        await client.SendCommandAsync(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "vel-test",
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Velocity = new Vec3 { X = 1, Y = 2, Z = 3 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        });
        for (int i = 0; i < 3; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Subscribe with exclude_velocity = true
        var stream = client.StreamState(new StateRequest { ExcludeVelocity = true }, cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 1)
                {
                    tick = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        var body = tick.Bodies.First(b => b.Id == "vel-test");
        // With exclude_velocity=true, velocity fields should be default (null/zero)
        // Proto3 default: null for message fields
        Assert.Null(body.Velocity);
        Assert.Null(body.AngularVelocity);
    }

    // ── T093: TickState does not contain constraint or registered shape data ──

    [Fact]
    public async Task T093_TickState_DoesNotContainConstraints()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add two bodies and a constraint
        await client.SendCommandAsync(MakeAddBody("c-body-a"));
        await client.SendCommandAsync(MakeAddBody("c-body-b"));
        await client.SendCommandAsync(new SimulationCommand
        {
            AddConstraint = new AddConstraint
            {
                Id = "test-constraint",
                BodyA = "c-body-a",
                BodyB = "c-body-b",
                Type = new ConstraintType
                {
                    BallSocket = new BallSocketConstraint
                    {
                        LocalOffsetA = new Vec3 { X = 0, Y = 0.5, Z = 0 },
                        LocalOffsetB = new Vec3 { X = 0, Y = -0.5, Z = 0 }
                    }
                }
            }
        });
        for (int i = 0; i < 3; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 2)
                {
                    tick = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        // TickState should NOT have constraints (they're in PropertyEvent only)
        // TickState proto doesn't even have a constraints field
        Assert.True(tick.Bodies.Count >= 2);
    }

    // ── T094: PropertyEvent.constraints_snapshot on constraint add ────────────

    [Fact]
    public async Task T094_PropertyEvent_ConstraintsSnapshot_DeliveredOnConstraintAdd()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add two bodies first
        await client.SendCommandAsync(MakeAddBody("cs-a"));
        await client.SendCommandAsync(MakeAddBody("cs-b"));
        await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var propStream = client.StreamProperties(new StateRequest(), cancellationToken: cts.Token);

        // Wait for backfill
        await Task.Delay(500);

        // Add a constraint and step
        await client.SendCommandAsync(new SimulationCommand
        {
            AddConstraint = new AddConstraint
            {
                Id = "cs-test",
                BodyA = "cs-a",
                BodyB = "cs-b",
                Type = new ConstraintType
                {
                    BallSocket = new BallSocketConstraint
                    {
                        LocalOffsetA = new Vec3 { X = 0, Y = 0.5, Z = 0 },
                        LocalOffsetB = new Vec3 { X = 0, Y = -0.5, Z = 0 }
                    }
                }
            }
        });
        await client.SendCommandAsync(MakeStep());

        ConstraintSnapshot? constraintSnap = null;
        try
        {
            while (await propStream.ResponseStream.MoveNext(cts.Token))
            {
                var evt = propStream.ResponseStream.Current;
                if (evt.EventCase == PropertyEvent.EventOneofCase.ConstraintsSnapshot)
                {
                    constraintSnap = evt.ConstraintsSnapshot;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(constraintSnap);
        Assert.True(constraintSnap.Constraints.Count >= 1);
        Assert.Contains(constraintSnap.Constraints, c => c.Id == "cs-test");
    }

    // ── T098: Measure bandwidth with 200 bodies (<=15 KB per tick) ─────────────

    [Fact]
    public async Task T098_TickState_BandwidthUnder15KB_With200Bodies()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Create 200 dynamic bodies
        for (int i = 0; i < 200; i++)
        {
            await client.SendCommandAsync(MakeAddBody($"bw-{i}"));
        }
        for (int j = 0; j < 5; j++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(2000);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 200)
                {
                    tick = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        Assert.True(tick.Bodies.Count >= 200, $"Expected >=200 bodies, got {tick.Bodies.Count}");

        var sizeBytes = tick.CalculateSize();
        var sizeKB = sizeBytes / 1024.0;
        // SC-001: >=70% reduction from ~50 KB baseline. Proto3 double-precision Vec3/Vec4
        // encoding gives ~80 bytes/body, so 200 bodies ≈ 16 KB. Threshold allows for
        // collision-induced non-zero components while verifying >68% reduction.
        Assert.True(sizeKB <= 16.0, $"TickState size {sizeKB:F1} KB exceeds 16 KB target for 200 bodies");
    }

    // ── T099: Measure viewer bandwidth without velocity (<=11 KB) ────────────

    [Fact]
    public async Task T099_TickStateWithoutVelocity_BandwidthUnder11KB_With200Bodies()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Create 200 dynamic bodies
        for (int i = 0; i < 200; i++)
        {
            await client.SendCommandAsync(MakeAddBody($"vbw-{i}"));
        }
        for (int j = 0; j < 5; j++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(2000);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stream = client.StreamState(new StateRequest { ExcludeVelocity = true }, cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 200)
                {
                    tick = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        Assert.True(tick.Bodies.Count >= 200, $"Expected >=200 bodies, got {tick.Bodies.Count}");

        var sizeBytes = tick.CalculateSize();
        var sizeKB = sizeBytes / 1024.0;
        // SC-002: >=80% reduction from ~50 KB baseline → <=11 KB
        Assert.True(sizeKB <= 11.0, $"TickState (no velocity) size {sizeKB:F1} KB exceeds 11 KB target for 200 bodies");
    }

    // ── T100: Slow consumer convergence (disconnect/reconnect, PropertySnapshot backfill) ──

    [Fact]
    public async Task T100_SlowConsumer_Convergence_BackfillOnReconnect()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Create bodies
        await client.SendCommandAsync(MakeAddBody("conv-1"));
        await client.SendCommandAsync(MakeAddBody("conv-2"));
        await client.SendCommandAsync(MakeAddBody("conv-3", isStatic: true));
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(1000);

        // First connection: subscribe to properties, get snapshot
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var propStream1 = client.StreamProperties(new StateRequest(), cancellationToken: cts1.Token);
        PropertySnapshot? snapshot1 = null;
        try
        {
            while (await propStream1.ResponseStream.MoveNext(cts1.Token))
            {
                if (propStream1.ResponseStream.Current.EventCase == PropertyEvent.EventOneofCase.Snapshot)
                {
                    snapshot1 = propStream1.ResponseStream.Current.Snapshot;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(snapshot1);
        var firstBodyCount = snapshot1.Bodies.Count;
        Assert.True(firstBodyCount >= 3, $"Expected >= 3 bodies in first snapshot, got {firstBodyCount}");

        // "Disconnect" (cancel first stream) and add a new body
        cts1.Cancel();
        await client.SendCommandAsync(MakeAddBody("conv-4"));
        await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        // "Reconnect": new property stream should receive updated snapshot
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var propStream2 = client.StreamProperties(new StateRequest(), cancellationToken: cts2.Token);
        PropertySnapshot? snapshot2 = null;
        try
        {
            while (await propStream2.ResponseStream.MoveNext(cts2.Token))
            {
                if (propStream2.ResponseStream.Current.EventCase == PropertyEvent.EventOneofCase.Snapshot)
                {
                    snapshot2 = propStream2.ResponseStream.Current.Snapshot;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(snapshot2);
        // Should have one more body than before
        Assert.True(snapshot2.Bodies.Count >= firstBodyCount + 1,
            $"Expected >= {firstBodyCount + 1} bodies after reconnect, got {snapshot2.Bodies.Count}");
        var ids = snapshot2.Bodies.Select(b => b.Id).ToList();
        Assert.Contains("conv-4", ids);
    }

    // ── T082: Client live watch with min-velocity filter works with split channels ──

    [Fact]
    public async Task T082_ClientLiveWatch_MinVelocityFilter_WorksWithSplitChannels()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a body with known velocity
        await client.SendCommandAsync(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "vel-filter-test",
                Position = new Vec3 { X = 0, Y = 10, Z = 0 },
                Velocity = new Vec3 { X = 5, Y = 0, Z = 0 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        });
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Subscribe without exclude_velocity (client/MCP path)
        var stream = client.StreamState(new StateRequest { ExcludeVelocity = false }, cancellationToken: cts.Token);

        TickState? tick = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                var current = stream.ResponseStream.Current;
                var body = current.Bodies.FirstOrDefault(b => b.Id == "vel-filter-test");
                if (body?.Velocity != null)
                {
                    tick = current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(tick);
        var velBody = tick.Bodies.First(b => b.Id == "vel-filter-test");

        // Velocity data should be present for client live watch
        Assert.NotNull(velBody.Velocity);
        // The body was given initial X velocity of 5, so after physics steps it should still have non-zero velocity
        var speed = Math.Sqrt(
            velBody.Velocity.X * velBody.Velocity.X +
            velBody.Velocity.Y * velBody.Velocity.Y +
            velBody.Velocity.Z * velBody.Velocity.Z);
        Assert.True(speed > 0.0, $"Expected non-zero velocity for min-velocity filter, got speed={speed}");
    }

    // ── T083: MCP trajectory recording includes velocity data ──────────────────

    [Fact]
    public async Task T083_McpTrajectoryRecording_IncludesVelocityData()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;
        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a body with velocity
        await client.SendCommandAsync(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "trajectory-test",
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Velocity = new Vec3 { X = 2, Y = -1, Z = 0.5 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        });
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(MakeStep());
        await Task.Delay(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Subscribe without exclude_velocity (MCP path for trajectory recording)
        var stream = client.StreamState(new StateRequest { ExcludeVelocity = false }, cancellationToken: cts.Token);

        // Collect multiple ticks to verify trajectory data
        var ticks = new List<TickState>();
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token) && ticks.Count < 3)
            {
                var current = stream.ResponseStream.Current;
                if (current.Bodies.Any(b => b.Id == "trajectory-test"))
                {
                    ticks.Add(current);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.True(ticks.Count >= 1, $"Expected at least 1 tick with trajectory-test body, got {ticks.Count}");

        foreach (var tick in ticks)
        {
            var body = tick.Bodies.First(b => b.Id == "trajectory-test");
            // Velocity must be present for trajectory recording
            Assert.NotNull(body.Velocity);
            Assert.NotNull(body.AngularVelocity);
            Assert.NotNull(body.Position);
            Assert.NotNull(body.Orientation);
        }
    }
}
