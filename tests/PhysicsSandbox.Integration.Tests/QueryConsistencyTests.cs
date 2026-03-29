using Aspire.Hosting;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class QueryConsistencyTests
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
    public async Task OverlapAndRaycast_AgreeOnBodyExistence_AfterReset()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Confirmed reset to start clean
        var resetResponse = client.ConfirmedReset(new ConfirmedResetRequest());
        Assert.True(resetResponse.Success);

        // Add 5 spheres at known positions along the X axis, y=5
        var batch = new BatchSimulationRequest();
        for (int i = 0; i < 5; i++)
            batch.Commands.Add(MakeSphere($"qc-sphere-{i}", i * 3.0, 5.0, 0.0, 0.5, 1.0));
        var addResult = client.SendBatchCommand(batch);
        Assert.True(addResult.Results.All(r => r.Success));

        // Pause to freeze physics state
        client.SendCommand(new SimulationCommand { PlayPause = new PlayPause { Running = false } });

        // Overlap: large sphere centered on the group
        var overlapResponse = client.Overlap(new OverlapRequest
        {
            Shape = new Shape { Sphere = new Sphere { Radius = 50.0 } },
            Position = new Vec3 { X = 6.0, Y = 5.0, Z = 0.0 }
        });
        var overlapIds = overlapResponse.BodyIds.OrderBy(id => id).ToList();

        // Raycast: sweep downward through each known position
        var raycastIds = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var rayResponse = client.Raycast(new RaycastRequest
            {
                Origin = new Vec3 { X = i * 3.0, Y = 20.0, Z = 0.0 },
                Direction = new Vec3 { X = 0.0, Y = -1.0, Z = 0.0 },
                MaxDistance = 30.0,
                AllHits = true
            });
            foreach (var hit in rayResponse.Hits)
            {
                if (hit.BodyId.StartsWith("qc-sphere-"))
                    raycastIds.Add(hit.BodyId);
            }
        }
        raycastIds = raycastIds.Distinct().OrderBy(id => id).ToList();

        // Both queries should find the same 5 bodies
        Assert.Equal(5, overlapIds.Count);
        Assert.Equal(overlapIds, raycastIds);
    }

    [Fact]
    public async Task OverlapReturnsNoRemovedBodies_AfterRemoval()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnectWithSimulation();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Confirmed reset to start clean
        client.ConfirmedReset(new ConfirmedResetRequest());

        // Add 3 spheres
        var batch = new BatchSimulationRequest();
        batch.Commands.Add(MakeSphere("qc-rem-1", 0.0, 5.0, 0.0, 0.5, 1.0));
        batch.Commands.Add(MakeSphere("qc-rem-2", 3.0, 5.0, 0.0, 0.5, 1.0));
        batch.Commands.Add(MakeSphere("qc-rem-3", 6.0, 5.0, 0.0, 0.5, 1.0));
        client.SendBatchCommand(batch);

        // Remove one body
        client.SendCommand(new SimulationCommand { RemoveBody = new RemoveBody { BodyId = "qc-rem-2" } });

        // Brief delay to let the simulation process the removal
        await Task.Delay(200);

        // Overlap should find exactly 2 bodies
        var overlapResponse = client.Overlap(new OverlapRequest
        {
            Shape = new Shape { Sphere = new Sphere { Radius = 50.0 } },
            Position = new Vec3 { X = 3.0, Y = 5.0, Z = 0.0 }
        });

        var ids = overlapResponse.BodyIds.Where(id => id.StartsWith("qc-rem-")).OrderBy(id => id).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Contains("qc-rem-1", ids);
        Assert.Contains("qc-rem-3", ids);
        Assert.DoesNotContain("qc-rem-2", ids);
    }
}
