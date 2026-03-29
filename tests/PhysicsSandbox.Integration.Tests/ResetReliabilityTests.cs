using Aspire.Hosting;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class ResetReliabilityTests
{
    private static async Task WaitForSimulation(DistributedApplication app)
    {
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
        await Task.Delay(3000);
    }

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
    public async Task ConfirmedReset_RemovesAllBodies()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add 5 spheres
        var batch = new BatchSimulationRequest();
        for (int i = 1; i <= 5; i++)
            batch.Commands.Add(MakeSphere($"reset-test-{i}", i * 2.0, 5.0, 0.0, 0.5, 1.0));
        var addResult = client.SendBatchCommand(batch);
        Assert.True(addResult.Results.All(r => r.Success));

        // Confirmed reset
        var resetResponse = client.ConfirmedReset(new ConfirmedResetRequest());
        Assert.True(resetResponse.Success);

        // Overlap query — should find zero bodies (no stale results)
        var overlapResponse = client.Overlap(new OverlapRequest
        {
            Shape = new Shape { Sphere = new Sphere { Radius = 100.0 } },
            Position = new Vec3 { X = 0, Y = 5, Z = 0 }
        });

        Assert.Empty(overlapResponse.BodyIds);
    }

    [Fact]
    public async Task ConfirmedReset_AllowsReusedIdsAfterReset()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a sphere with a specific ID
        var cmd = MakeSphere("reuse-id-1", 0.0, 5.0, 0.0, 0.5, 1.0);
        var ack = client.SendCommand(cmd);
        Assert.True(ack.Success);

        // Confirmed reset
        var resetResponse = client.ConfirmedReset(new ConfirmedResetRequest());
        Assert.True(resetResponse.Success);

        // Add a sphere with the same ID — should succeed after confirmed reset
        var cmd2 = MakeSphere("reuse-id-1", 0.0, 5.0, 0.0, 0.5, 1.0);
        var ack2 = client.SendCommand(cmd2);
        Assert.True(ack2.Success, $"Expected success but got: {ack2.Message}");
    }
}
