using Aspire.Hosting;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class BatchResultTests
{
    private static SimulationCommand MakeSphere(string id, double x, double y, double z, double radius, double mass)
    {
        return new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = id,
                Position = new Vec3 { X = x, Y = y, Z = z },
                Mass = mass,
                Shape = new Shape { Sphere = new Sphere { Radius = radius } }
            }
        };
    }

    [Fact]
    public async Task BatchCommand_AllSuccess_ReportsAllPassing()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Confirmed reset to start clean
        client.ConfirmedReset(new ConfirmedResetRequest());

        // Batch with 5 unique IDs
        var batch = new BatchSimulationRequest();
        for (int i = 1; i <= 5; i++)
            batch.Commands.Add(MakeSphere($"batch-ok-{i}", i * 2.0, 5.0, 0.0, 0.5, 1.0));

        var response = client.SendBatchCommand(batch);

        Assert.Equal(5, response.Results.Count);
        Assert.True(response.Results.All(r => r.Success));
    }

    [Fact]
    public async Task BatchCommand_ExceedsMaxSize_ReportsFailure()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Confirmed reset to start clean
        client.ConfirmedReset(new ConfirmedResetRequest());

        // Batch with 101 commands (exceeds max of 100)
        var batch = new BatchSimulationRequest();
        for (int i = 1; i <= 101; i++)
            batch.Commands.Add(MakeSphere($"batch-max-{i}", i * 0.5, 5.0, 0.0, 0.3, 1.0));

        var response = client.SendBatchCommand(batch);

        // Server rejects the entire batch
        Assert.Single(response.Results);
        Assert.False(response.Results[0].Success);
        Assert.Contains("exceeds maximum", response.Results[0].Message);
    }

    [Fact]
    public async Task BatchCommand_ReportsPerCommandResults()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Confirmed reset to start clean
        client.ConfirmedReset(new ConfirmedResetRequest());

        // Batch with mixed valid and invalid commands
        var batch = new BatchSimulationRequest();
        batch.Commands.Add(MakeSphere("batch-mix-1", 0.0, 5.0, 0.0, 0.5, 1.0)); // valid
        // Add a command with invalid mass (0 for dynamic body)
        batch.Commands.Add(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "batch-mix-2",
                Position = new Vec3 { X = 2.0, Y = 5.0, Z = 0.0 },
                Mass = -1.0, // invalid mass
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        });
        batch.Commands.Add(MakeSphere("batch-mix-3", 4.0, 5.0, 0.0, 0.5, 1.0)); // valid

        var response = client.SendBatchCommand(batch);

        // Each command should have a result with the correct index
        Assert.Equal(3, response.Results.Count);
        Assert.Equal(0, response.Results[0].Index);
        Assert.Equal(1, response.Results[1].Index);
        Assert.Equal(2, response.Results[2].Index);
    }
}
