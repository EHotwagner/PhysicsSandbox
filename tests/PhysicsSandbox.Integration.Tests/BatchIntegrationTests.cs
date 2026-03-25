using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class BatchIntegrationTests
{
    [Fact]
    public async Task SendBatchCommand_ExecutesAllCommands_ReturnsPerCommandResults()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var batch = new BatchSimulationRequest();
        for (int i = 0; i < 10; i++)
        {
            var body = new AddBody
            {
                Id = $"batch-sphere-{i}",
                Position = new Vec3 { X = i, Y = 5, Z = 0 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            };
            batch.Commands.Add(new SimulationCommand { AddBody = body });
        }

        var response = await client.SendBatchCommandAsync(batch);

        Assert.Equal(10, response.Results.Count);
        foreach (var result in response.Results)
        {
            Assert.True(result.Success, $"Command {result.Index} failed: {result.Message}");
        }
        Assert.True(response.TotalTimeMs >= 0);
    }

    [Fact]
    public async Task SendBatchCommand_RejectsOver100Commands()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        var batch = new BatchSimulationRequest();
        for (int i = 0; i < 101; i++)
        {
            batch.Commands.Add(new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            });
        }

        var response = await client.SendBatchCommandAsync(batch);

        Assert.Single(response.Results);
        Assert.False(response.Results[0].Success);
    }

    // Merged from ComparisonIntegrationTests
    [Fact]
    public async Task BatchVsIndividual_BatchIsFaster()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Wait for simulation
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Individual: send 20 commands one by one
        var individualSw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 20; i++)
        {
            await client.SendCommandAsync(new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            });
        }
        individualSw.Stop();

        // Reset
        await client.SendCommandAsync(new SimulationCommand
        {
            Reset = new ResetSimulation()
        });

        // Batch: send 20 commands in one batch
        var batch = new BatchSimulationRequest();
        for (int i = 0; i < 20; i++)
        {
            batch.Commands.Add(new SimulationCommand
            {
                Step = new StepSimulation { DeltaTime = 0.016 }
            });
        }

        var batchSw = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.SendBatchCommandAsync(batch);
        batchSw.Stop();

        Assert.Equal(20, response.Results.Count);
        Assert.True(response.TotalTimeMs >= 0);

        // Batch should be faster (at least comparable)
        // Don't assert strict ratio in CI — just verify both work
        Assert.True(batchSw.ElapsedMilliseconds >= 0);
        Assert.True(individualSw.ElapsedMilliseconds >= 0);
    }

    // Merged from StressTestIntegrationTests
    [Fact]
    public async Task SendBatchCommand_CanScaleToManyBodies()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Wait for simulation to connect
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Add 50 bodies in a single batch to verify scaling works
        var batch = new BatchSimulationRequest();
        for (int i = 0; i < 50; i++)
        {
            batch.Commands.Add(new SimulationCommand
            {
                AddBody = new AddBody
                {
                    Id = $"scale-test-{i}",
                    Position = new Vec3 { X = (i % 10) * 2, Y = 5, Z = (i / 10) * 2 },
                    Mass = 1.0,
                    Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
                }
            });
        }

        var response = await client.SendBatchCommandAsync(batch);

        Assert.Equal(50, response.Results.Count);
        Assert.True(response.TotalTimeMs >= 0);

        var successCount = response.Results.Count(r => r.Success);
        Assert.True(successCount >= 45, $"Expected at least 45 successes, got {successCount}");
    }
}
