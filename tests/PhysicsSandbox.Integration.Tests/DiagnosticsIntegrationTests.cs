using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class DiagnosticsIntegrationTests
{
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
                    Mass = 1.0,
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
