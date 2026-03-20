using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class ServerHubTests
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

    [Fact]
    public async Task SendCommand_ReturnsSuccessAck()
    {
        var (app, channel) = await StartAppAndConnect();
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
        var (app, channel) = await StartAppAndConnect();
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
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var viewCommand = new ViewCommand
        {
            SetZoom = new SetZoom { Level = 2.0 }
        };

        var ack = await client.SendViewCommandAsync(viewCommand);

        Assert.True(ack.Success);
    }
}
