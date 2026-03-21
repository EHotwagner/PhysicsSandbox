using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class MetricsIntegrationTests
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
    public async Task GetMetrics_ReturnsNonZeroCounters_AfterSendingCommands()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Send a few commands to generate metrics
        for (int i = 0; i < 5; i++)
        {
            await client.SendCommandAsync(new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            });
        }

        // Query metrics
        var response = await client.GetMetricsAsync(new MetricsRequest());

        // Verify we got at least one service report
        Assert.NotEmpty(response.Services);

        // Find PhysicsServer metrics
        var serverMetrics = response.Services.FirstOrDefault(s => s.ServiceName == "PhysicsServer");
        Assert.NotNull(serverMetrics);

        // Server should have received our 5 commands
        Assert.True(serverMetrics.MessagesReceived >= 5,
            $"Expected at least 5 messages received, got {serverMetrics.MessagesReceived}");
        Assert.True(serverMetrics.BytesReceived > 0,
            $"Expected non-zero bytes received, got {serverMetrics.BytesReceived}");
    }

    [Fact]
    public async Task GetMetrics_ReturnsPipelineTimings()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Query metrics
        var response = await client.GetMetricsAsync(new MetricsRequest());

        // Pipeline timings should exist (may be zero if no simulation has stepped)
        Assert.NotNull(response.Pipeline);
    }
}
