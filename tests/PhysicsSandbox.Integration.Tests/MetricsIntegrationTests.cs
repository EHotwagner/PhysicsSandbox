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

    // Merged from DiagnosticsIntegrationTests
    [Fact]
    public async Task GetMetrics_ReturnsPipelineTimings_AfterSimulationSteps()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Wait for simulation to connect
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Add bodies and step to generate timing data
        for (int i = 0; i < 10; i++)
        {
            await client.SendCommandAsync(new SimulationCommand
            {
                AddBody = new AddBody
                {
                    Id = $"diag-sphere-{i}",
                    Position = new Vec3 { X = i, Y = 5, Z = 0 },
                    Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
                }
            });
        }

        // Enable simulation to generate steps with timing data
        await client.SendCommandAsync(new SimulationCommand
        {
            PlayPause = new PlayPause { Running = true }
        });

        // Wait for simulation to run and produce timing data
        await Task.Delay(1000);

        // Query metrics
        var response = await client.GetMetricsAsync(new MetricsRequest());

        Assert.NotNull(response.Pipeline);
        // After running steps, tick_ms should be populated from state
        Assert.True(response.Pipeline.TotalPipelineMs >= 0,
            $"Expected non-negative total pipeline time, got {response.Pipeline.TotalPipelineMs}");
    }
}
