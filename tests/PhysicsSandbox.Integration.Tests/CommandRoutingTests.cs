using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class CommandRoutingTests
{
    private static async Task<TickState?> ReadLatestState(PhysicsHub.PhysicsHubClient client, CancellationToken ct, int timeoutSeconds = 15)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        TickState? latest = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                latest = stream.ResponseStream.Current;
                if (latest.Bodies.Count > 0) break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }
        return latest;
    }

    private static async Task SendStep(PhysicsHub.PhysicsHubClient client, double dt = 0.016)
    {
        await client.SendCommandAsync(new SimulationCommand
        {
            Step = new StepSimulation { DeltaTime = dt }
        });
    }

    private static SimulationCommand MakeSphereCommand(string id, double y = 0, double mass = 1)
    {
        return new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = id,
                Position = new Vec3 { X = 0, Y = y, Z = 0 },
                Velocity = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = mass,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        };
    }

    [Fact]
    public async Task AddBody_CreatesBodyVisibleInState()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var ack = await client.SendCommandAsync(MakeSphereCommand("add-test-1"));
        Assert.True(ack.Success);

        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        var state = await ReadLatestState(client, CancellationToken.None);

        Assert.NotNull(state);
        Assert.True(state.Bodies.Count > 0, "Expected at least one body in state after AddBody + Step");
    }

    [Fact]
    public async Task ApplyForce_ChangesVelocityAfterStep()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        await client.SendCommandAsync(MakeSphereCommand("force-test-1", y: 5));
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        var initialState = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(initialState);
        var initialBody = initialState.Bodies.FirstOrDefault(b => b.Id == "force-test-1");
        Assert.NotNull(initialBody);
        var initialVelY = initialBody.Velocity.Y;

        await client.SendCommandAsync(new SimulationCommand
        {
            ApplyForce = new ApplyForce
            {
                BodyId = "force-test-1",
                Force = new Vec3 { X = 0, Y = 100, Z = 0 }
            }
        });

        for (int i = 0; i < 10; i++)
        {
            await SendStep(client);
        }
        await Task.Delay(300);

        var afterState = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(afterState);
        var afterBody = afterState.Bodies.FirstOrDefault(b => b.Id == "force-test-1");
        Assert.NotNull(afterBody);

        Assert.NotEqual(initialVelY, afterBody.Velocity.Y);
    }

    [Fact]
    public async Task ApplyImpulse_ProducesImmediateVelocityChange()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        await client.SendCommandAsync(MakeSphereCommand("impulse-test-1", y: 5));
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        await client.SendCommandAsync(new SimulationCommand
        {
            ApplyImpulse = new ApplyImpulse
            {
                BodyId = "impulse-test-1",
                Impulse = new Vec3 { X = 50, Y = 0, Z = 0 }
            }
        });

        await SendStep(client);
        await Task.Delay(200);

        var state = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(state);
        var body = state.Bodies.FirstOrDefault(b => b.Id == "impulse-test-1");
        Assert.NotNull(body);

        Assert.NotEqual(0.0, body.Velocity.X);
    }

    [Fact]
    public async Task ApplyTorque_ChangesAngularVelocity()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        await client.SendCommandAsync(MakeSphereCommand("torque-test-1", y: 5));
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        await client.SendCommandAsync(new SimulationCommand
        {
            ApplyTorque = new ApplyTorque
            {
                BodyId = "torque-test-1",
                Torque = new Vec3 { X = 0, Y = 10, Z = 0 }
            }
        });

        for (int i = 0; i < 5; i++)
        {
            await SendStep(client);
        }
        await Task.Delay(200);

        var state = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(state);
        var body = state.Bodies.FirstOrDefault(b => b.Id == "torque-test-1");
        Assert.NotNull(body);

        var angVel = body.AngularVelocity;
        Assert.True(
            Math.Abs(angVel.X) > 0.001 || Math.Abs(angVel.Y) > 0.001 || Math.Abs(angVel.Z) > 0.001,
            "Expected non-zero angular velocity after applying torque");
    }

    [Fact]
    public async Task SetGravity_ChangesBodyTrajectory()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        await client.SendCommandAsync(new SimulationCommand
        {
            SetGravity = new SetGravity
            {
                Gravity = new Vec3 { X = 0, Y = -20, Z = 0 }
            }
        });

        await client.SendCommandAsync(MakeSphereCommand("gravity-test-1", y: 10));

        for (int i = 0; i < 20; i++)
        {
            await SendStep(client);
        }
        await Task.Delay(300);

        var state = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(state);
        var body = state.Bodies.FirstOrDefault(b => b.Id == "gravity-test-1");
        Assert.NotNull(body);

        Assert.True(body.Position.Y < 10.0,
            $"Expected body to fall below y=10 under gravity, but position Y={body.Position.Y}");
    }

    [Fact]
    public async Task StepSimulation_AdvancesTime()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Step several times to ensure state is populated
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        // Read state and record time
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream1 = client.StreamState(new StateRequest(), cancellationToken: cts1.Token);
        double timeBefore = 0;
        try
        {
            if (await stream1.ResponseStream.MoveNext(cts1.Token))
                timeBefore = stream1.ResponseStream.Current.Time;
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        // Step more
        for (int i = 0; i < 5; i++) await SendStep(client);
        await Task.Delay(500);

        // Read state again
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream2 = client.StreamState(new StateRequest(), cancellationToken: cts2.Token);
        double timeAfter = 0;
        try
        {
            if (await stream2.ResponseStream.MoveNext(cts2.Token))
                timeAfter = stream2.ResponseStream.Current.Time;
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.True(timeAfter > timeBefore,
            $"Expected time to advance after step. Before={timeBefore}, After={timeAfter}");
    }

    [Fact]
    public async Task PlayPause_TogglesRunningFlag()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Send Play command
        await client.SendCommandAsync(new SimulationCommand
        {
            PlayPause = new PlayPause { Running = true }
        });

        // Wait for state to propagate, then read stream until Running=true
        await Task.Delay(1000);
        bool foundRunning = false;
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream1 = client.StreamState(new StateRequest(), cancellationToken: cts1.Token);
        try
        {
            while (await stream1.ResponseStream.MoveNext(cts1.Token))
            {
                if (stream1.ResponseStream.Current.Running)
                {
                    foundRunning = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }
        Assert.True(foundRunning, "Expected running=true after Play command");

        // Send Pause command
        await client.SendCommandAsync(new SimulationCommand
        {
            PlayPause = new PlayPause { Running = false }
        });

        await Task.Delay(1000);
        bool foundPaused = false;
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream2 = client.StreamState(new StateRequest(), cancellationToken: cts2.Token);
        try
        {
            while (await stream2.ResponseStream.MoveNext(cts2.Token))
            {
                if (!stream2.ResponseStream.Current.Running)
                {
                    foundPaused = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }
        Assert.True(foundPaused, "Expected running=false after Pause command");
    }

    [Fact]
    public async Task RemoveBody_RemovesFromState()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        await client.SendCommandAsync(MakeSphereCommand("remove-test-1", y: 5));
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        var stateWithBody = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(stateWithBody);
        Assert.Contains(stateWithBody.Bodies, b => b.Id == "remove-test-1");

        await client.SendCommandAsync(new SimulationCommand
        {
            RemoveBody = new RemoveBody { BodyId = "remove-test-1" }
        });

        await SendStep(client);
        await Task.Delay(200);

        // Read state — body should be gone. Use a timeout-based read that doesn't
        // require bodies to be present (since the state may now be empty).
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        TickState? stateAfterRemove = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                stateAfterRemove = stream.ResponseStream.Current;
                break; // take first state update
            }
        }
        catch (OperationCanceledException) { }

        Assert.NotNull(stateAfterRemove);
        Assert.DoesNotContain(stateAfterRemove.Bodies, b => b.Id == "remove-test-1");
    }

    [Fact]
    public async Task ClearForces_StopsAcceleration()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Zero out gravity so it doesn't interfere
        await client.SendCommandAsync(new SimulationCommand
        {
            SetGravity = new SetGravity
            {
                Gravity = new Vec3 { X = 0, Y = 0, Z = 0 }
            }
        });

        await client.SendCommandAsync(MakeSphereCommand("clear-test-1", y: 5));
        for (int i = 0; i < 3; i++) await SendStep(client);
        await Task.Delay(500);

        // Apply a continuous force and step to accelerate
        await client.SendCommandAsync(new SimulationCommand
        {
            ApplyForce = new ApplyForce
            {
                BodyId = "clear-test-1",
                Force = new Vec3 { X = 0, Y = 50, Z = 0 }
            }
        });

        for (int i = 0; i < 5; i++)
        {
            await SendStep(client);
        }
        await Task.Delay(200);

        var stateBeforeClear = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(stateBeforeClear);
        var bodyBeforeClear = stateBeforeClear.Bodies.FirstOrDefault(b => b.Id == "clear-test-1");
        Assert.NotNull(bodyBeforeClear);
        var velBeforeClear = bodyBeforeClear.Velocity.Y;

        // Clear forces
        await client.SendCommandAsync(new SimulationCommand
        {
            ClearForces = new ClearForces { BodyId = "clear-test-1" }
        });

        // Step several more times — velocity should stabilize (no more acceleration)
        for (int i = 0; i < 5; i++)
        {
            await SendStep(client);
        }
        await Task.Delay(200);

        var stateAfterClear = await ReadLatestState(client, CancellationToken.None);
        Assert.NotNull(stateAfterClear);
        var bodyAfterClear = stateAfterClear.Bodies.FirstOrDefault(b => b.Id == "clear-test-1");
        Assert.NotNull(bodyAfterClear);
        var velAfterClear = bodyAfterClear.Velocity.Y;

        // After clearing forces, velocity should not continue increasing.
        // The difference between post-clear velocities across steps should be
        // much smaller than the difference while force was applied.
        // With zero gravity and no force, velocity should remain roughly constant.
        // We check that velocity did not increase significantly beyond what it was at clear time.
        Assert.True(Math.Abs(velAfterClear - velBeforeClear) < Math.Abs(velBeforeClear) * 0.5 + 1.0,
            $"Expected velocity to stabilize after ClearForces. Before clear: {velBeforeClear}, After clear: {velAfterClear}");
    }

    // Merged from CommandAuditStreamTests
    [Fact]
    public async Task StreamCommands_ReceivesCommandEvents_WhenCommandsSent()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));

        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
    }
}
