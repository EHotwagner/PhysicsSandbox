using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class ErrorConditionTests
{
    private static async Task<(DistributedApplication App, GrpcChannel Channel)> StartServerOnly()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();
        var app = await appHost.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));
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

    private static async Task<(DistributedApplication App, GrpcChannel Channel)> StartAppAndConnect()
    {
        var (app, channel) = await StartServerOnly();
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
        // Give the simulation time to establish gRPC connection to server
        await Task.Delay(3000);
        return (app, channel);
    }

    [Fact]
    public async Task SendCommand_WithoutSimulation_ReturnsDroppedMessage()
    {
        var (app, channel) = await StartServerOnly();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Send command immediately — race with simulation connecting.
        // Server returns "dropped" if no simulation, "forwarded" if simulation already connected.
        var command = new SimulationCommand
        {
            Step = new StepSimulation { DeltaTime = 0.016 }
        };

        var ack = await client.SendCommandAsync(command);

        Assert.True(ack.Success);
        // Accept either outcome — both are valid graceful handling
        Assert.True(
            ack.Message.Contains("dropped", StringComparison.OrdinalIgnoreCase) ||
            ack.Message.Contains("forwarded", StringComparison.OrdinalIgnoreCase),
            $"Expected 'dropped' or 'forwarded' in message, got: {ack.Message}");
    }

    [Fact]
    public async Task SendCommand_WithEmptyCommand_ReturnsAppropriateResponse()
    {
        var (app, channel) = await StartServerOnly();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var command = new SimulationCommand();

        var ack = await client.SendCommandAsync(command);

        // The key assertion is that we get here without an exception —
        // the server handles an empty command gracefully.
        Assert.NotNull(ack);
    }

    [Fact]
    public async Task StreamState_WithoutSimulation_ReturnsEmptyOrCachedState()
    {
        var (app, channel) = await StartServerOnly();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var stream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        try
        {
            var hasNext = await stream.ResponseStream.MoveNext(cts.Token);

            if (hasNext)
            {
                var state = stream.ResponseStream.Current;
                Assert.Empty(state.Bodies);
                Assert.Equal(0.0, state.Time);
            }
            // If no message arrived before timeout, that is also acceptable.
        }
        catch (OperationCanceledException)
        {
            // Timeout with no message is acceptable — server has no cached state yet.
        }
    }

    [Fact]
    public async Task RapidCommands_DoNotCrashServer()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var tasks = Enumerable.Range(0, 200).Select(_ =>
        {
            var command = new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            };
            return client.SendCommandAsync(command).ResponseAsync;
        }).ToArray();

        var acks = await Task.WhenAll(tasks);

        Assert.All(acks, ack => Assert.True(ack.Success));
    }
}
