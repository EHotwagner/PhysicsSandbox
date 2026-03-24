using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class ServerHubTests
{
    [Fact]
    public async Task SendCommand_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var command = new SimulationCommand
        {
            Step = new StepSimulation { DeltaTime = 0.016 }
        };

        var ack = await client.SendCommandAsync(command);

        Assert.True(ack.Success);
    }

    [Fact]
    public async Task StreamState_OpensWithoutError()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        // Stream should open without throwing — we don't expect data yet (no simulation connected)
        Assert.NotNull(stream);
    }

    [Fact]
    public async Task SendViewCommand_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            SetZoom = new SetZoom { Level = 2.0 }
        };

        var ack = await client.SendViewCommandAsync(viewCommand);

        Assert.True(ack.Success);
    }

    [Fact]
    public async Task StreamViewCommands_ReceivesSentViewCommand()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Start streaming view commands
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var stream = client.StreamViewCommands(new StateRequest(), cancellationToken: cts.Token);

        // Send a view command
        var viewCommand = new ViewCommand
        {
            SetZoom = new SetZoom { Level = 3.5 }
        };

        await client.SendViewCommandAsync(viewCommand);

        // Read the forwarded command from the stream
        var hasNext = await stream.ResponseStream.MoveNext(cts.Token);

        Assert.True(hasNext);
        Assert.Equal(3.5, stream.ResponseStream.Current.SetZoom.Level);
    }

    [Fact]
    public async Task StreamViewCommands_OpensWithoutError()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stream = client.StreamViewCommands(new StateRequest(), cancellationToken: cts.Token);

        // Stream should open without throwing
        Assert.NotNull(stream);
    }

    [Fact]
    public async Task SendViewCommand_SmoothCamera_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            SmoothCamera = new SmoothCamera
            {
                Position = new Vec3 { X = 5, Y = 5, Z = 5 },
                Target = new Vec3 { X = 0, Y = 0, Z = 0 },
                Up = new Vec3 { X = 0, Y = 1, Z = 0 },
                DurationSeconds = 2.0
            }
        };

        var ack = await client.SendViewCommandAsync(viewCommand);
        Assert.True(ack.Success);
    }

    [Fact]
    public async Task SendViewCommand_CameraLookAt_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            CameraLookAt = new CameraLookAt { BodyId = "test-body", DurationSeconds = 1.0 }
        };

        var ack = await client.SendViewCommandAsync(viewCommand);
        Assert.True(ack.Success);
    }

    [Fact]
    public async Task SendViewCommand_SetNarration_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            SetNarration = new SetNarration { Text = "Test narration" }
        };

        var ack = await client.SendViewCommandAsync(viewCommand);
        Assert.True(ack.Success);
    }

    [Fact]
    public async Task SendViewCommand_CameraStop_ReturnsSuccessAck()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            CameraStop = new CameraStop()
        };

        var ack = await client.SendViewCommandAsync(viewCommand);
        Assert.True(ack.Success);
    }

    [Fact]
    public async Task StreamViewCommands_BroadcastToMultipleSubscribers()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Two viewers subscribe to ViewCommands
        var stream1 = client.StreamViewCommands(new StateRequest(), cancellationToken: cts.Token);
        var stream2 = client.StreamViewCommands(new StateRequest(), cancellationToken: cts.Token);

        // Small delay to let both streams register
        await Task.Delay(200);

        // Send a view command
        var viewCommand = new ViewCommand
        {
            SetZoom = new SetZoom { Level = 4.2 }
        };

        await client.SendViewCommandAsync(viewCommand);

        // Both subscribers should receive the command
        var hasNext1 = await stream1.ResponseStream.MoveNext(cts.Token);
        var hasNext2 = await stream2.ResponseStream.MoveNext(cts.Token);

        Assert.True(hasNext1);
        Assert.True(hasNext2);
        Assert.Equal(4.2, stream1.ResponseStream.Current.SetZoom.Level);
        Assert.Equal(4.2, stream2.ResponseStream.Current.SetZoom.Level);
    }
}
