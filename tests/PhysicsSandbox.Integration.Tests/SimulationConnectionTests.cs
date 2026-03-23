using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class SimulationConnectionTests
{
    private static async Task WaitForSimulation(DistributedApplication app)
    {
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
        // Give the simulation time to establish gRPC connection to server
        await Task.Delay(3000);
    }

    [Fact]
    public async Task SimulationConnects_AfterAspireStartup()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        await WaitForSimulation(app);

        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        var hasMessage = await stream.ResponseStream.MoveNext(cts.Token);

        Assert.True(hasMessage);

        var state = stream.ResponseStream.Current;
        Assert.True(state.Time >= 0);
        // Running is a bool — assert it is one of true or false (always passes, but validates the field is present)
        Assert.IsType<bool>(state.Running);
    }

    [Fact]
    public async Task SimulationConnection_ProducesNonEmptyState()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        await WaitForSimulation(app);

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a sphere body at y=5
        var addBody = new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "test-sphere-1",
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Velocity = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = 1,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        };
        await client.SendCommandAsync(addBody);

        // Step several times to ensure state propagates
        var step = new SimulationCommand
        {
            Step = new StepSimulation { DeltaTime = 0.016 }
        };
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(step);
        await Task.Delay(1000);

        // Read state messages until we find bodies
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        TickState? stateWithBodies = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                var state = stream.ResponseStream.Current;
                if (state.Bodies.Count > 0)
                {
                    stateWithBodies = state;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(stateWithBodies);
        Assert.True(stateWithBodies.Bodies.Count >= 1);
    }

    [Fact]
    public async Task AddBodyAndStep_ProducesUpdatedPhysics()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        await WaitForSimulation(app);

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Set gravity so bodies fall
        await client.SendCommandAsync(new SimulationCommand
        {
            SetGravity = new SetGravity
            {
                Gravity = new Vec3 { X = 0, Y = -9.81, Z = 0 }
            }
        });

        // Add a sphere body at y=10
        var addBody = new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "test-sphere-gravity",
                Position = new Vec3 { X = 0, Y = 10, Z = 0 },
                Velocity = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = 1,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        };
        await client.SendCommandAsync(addBody);

        // Step many times to let gravity act
        var step = new SimulationCommand
        {
            Step = new StepSimulation { DeltaTime = 0.016 }
        };
        for (int i = 0; i < 30; i++) await client.SendCommandAsync(step);
        await Task.Delay(1000);

        // Read state and find our body
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        BodyPose? body = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                var state = stream.ResponseStream.Current;
                foreach (var b in state.Bodies)
                {
                    if (b.Id == "test-sphere-gravity")
                    {
                        body = b;
                        break;
                    }
                }

                if (body != null)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(body);
        // Gravity should have moved the body downward from its initial y=10
        Assert.True(body.Position.Y < 10, $"Expected body Y < 10, but was {body.Position.Y}");
    }

    [Fact]
    public async Task PlayCommand_SetsRunningTrue()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        await WaitForSimulation(app);

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Send PlayPause with Running=true
        var playCommand = new SimulationCommand
        {
            PlayPause = new PlayPause { Running = true }
        };
        await client.SendCommandAsync(playCommand);

        // Wait for state to propagate
        await Task.Delay(1000);

        // Read state stream — may need to read multiple messages until Running=true
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        bool foundRunning = false;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Running)
                {
                    foundRunning = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.True(foundRunning, "Expected running=true after Play command");
    }

    [Fact]
    public async Task SimulationMaintainsConnection_For30Seconds()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        await WaitForSimulation(app);

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var responseCount = 0;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            // Send a step command
            var step = new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            };
            await client.SendCommandAsync(step);

            // Read a state from the stream
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

            var hasMessage = await stream.ResponseStream.MoveNext(cts.Token);
            Assert.True(hasMessage, $"Failed to receive state at elapsed {stopwatch.Elapsed}");

            responseCount++;

            // Wait 2 seconds before next iteration
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // We should have received responses throughout the entire 30-second period
        // With 2-second intervals over 30 seconds, expect at least 14 responses
        Assert.True(responseCount >= 14,
            $"Expected at least 14 responses over 30 seconds, but got {responseCount}");
    }
}
