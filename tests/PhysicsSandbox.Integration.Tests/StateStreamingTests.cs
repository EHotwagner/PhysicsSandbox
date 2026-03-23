using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class StateStreamingTests
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
            .WaitAsync(TimeSpan.FromSeconds(30));
        // Give the simulation time to establish gRPC connection to server
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

    private static SimulationCommand MakeAddBodyCommand(string id = "test-body") =>
        new()
        {
            AddBody = new AddBody
            {
                Id = id,
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Velocity = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        };

    private static SimulationCommand MakeStepCommand(double dt = 0.016) =>
        new()
        {
            Step = new StepSimulation { DeltaTime = dt }
        };

    [Fact]
    public async Task MultipleConcurrentSubscribers_ReceiveSameState()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // First, add a body and step several times to ensure state is populated
        await client.SendCommandAsync(MakeAddBodyCommand());
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(MakeStepCommand());
        await Task.Delay(1000);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Open 3 concurrent StreamState subscriptions (they should get cached state immediately)
        var stream1 = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        var stream2 = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        var stream3 = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        // Read one message from each stream
        Assert.True(await stream1.ResponseStream.MoveNext(cts.Token));
        Assert.True(await stream2.ResponseStream.MoveNext(cts.Token));
        Assert.True(await stream3.ResponseStream.MoveNext(cts.Token));

        var state1 = stream1.ResponseStream.Current;
        var state2 = stream2.ResponseStream.Current;
        var state3 = stream3.ResponseStream.Current;

        // All 3 should have the same body count
        Assert.Equal(state1.Bodies.Count, state2.Bodies.Count);
        Assert.Equal(state1.Bodies.Count, state3.Bodies.Count);
        Assert.True(state1.Bodies.Count >= 1, "Expected at least 1 body in state");

        // All 3 should have the same time value (within 0.5s tolerance)
        Assert.InRange(Math.Abs(state1.Time - state2.Time), 0, 0.5);
        Assert.InRange(Math.Abs(state1.Time - state3.Time), 0, 0.5);
    }

    [Fact]
    public async Task LateJoiner_ReceivesCachedState()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Send AddBody + multiple Steps first (before subscribing)
        await client.SendCommandAsync(MakeAddBodyCommand());
        for (int i = 0; i < 5; i++) await client.SendCommandAsync(MakeStepCommand());

        // Wait for state to be cached on server
        await Task.Delay(TimeSpan.FromSeconds(2));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // THEN open a new StreamState subscription (late joiner)
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        // Read messages until we find one with bodies (cached state should arrive quickly)
        TickState? stateWithBodies = null;
        try
        {
            while (await stream.ResponseStream.MoveNext(cts.Token))
            {
                if (stream.ResponseStream.Current.Bodies.Count >= 1)
                {
                    stateWithBodies = stream.ResponseStream.Current;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled) { }

        Assert.NotNull(stateWithBodies);
        Assert.True(stateWithBodies.Bodies.Count >= 1, "Late joiner should receive cached state with bodies");
    }

    [Fact]
    public async Task StateStream_DeliversUpdatesWithin1s()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Open StreamState subscription
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        // Send Step command and measure time until next message arrives
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await client.SendCommandAsync(MakeStepCommand());

        Assert.True(await stream.ResponseStream.MoveNext(cts.Token));
        stopwatch.Stop();

        // Assert the message arrived within 1 second
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"State update took {stopwatch.ElapsedMilliseconds}ms, expected under 1000ms");
    }

    [Fact]
    public async Task StreamViewCommands_ReceivesForwardedSetCamera()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Open StreamViewCommands subscription
        var stream = client.StreamViewCommands(new StateRequest(), cancellationToken: cts.Token);

        // Give the subscription time to register on the server
        await Task.Delay(500);

        // Send a SetCamera view command
        var viewCommand = new ViewCommand
        {
            SetCamera = new SetCamera
            {
                Position = new Vec3 { X = 1, Y = 2, Z = 3 },
                Target = new Vec3 { X = 4, Y = 5, Z = 6 }
            }
        };

        await client.SendViewCommandAsync(viewCommand);

        // Read from stream
        Assert.True(await stream.ResponseStream.MoveNext(cts.Token));

        var received = stream.ResponseStream.Current;
        Assert.NotNull(received.SetCamera);
        Assert.Equal(1, received.SetCamera.Position.X);
        Assert.Equal(2, received.SetCamera.Position.Y);
        Assert.Equal(3, received.SetCamera.Position.Z);
        Assert.Equal(4, received.SetCamera.Target.X);
        Assert.Equal(5, received.SetCamera.Target.Y);
        Assert.Equal(6, received.SetCamera.Target.Z);
    }
}
