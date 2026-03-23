using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class MetricsIntegrationTests
{
    [Fact]
    public async Task GetMetrics_ReturnsNonZeroCounters_AfterSendingCommands()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
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
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Query metrics
        var response = await client.GetMetricsAsync(new MetricsRequest());

        // Pipeline timings should exist (may be zero if no simulation has stepped)
        Assert.NotNull(response.Pipeline);
    }
}
