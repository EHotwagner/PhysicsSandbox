using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class StaticBodyTests
{
    [Fact]
    public async Task StaticPlane_AppearsInState_WithIsStaticTrue()
    {
        var (app, channel) = await IntegrationTestHelpers.StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Wait for simulation to connect
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Add a static plane
        await client.SendCommandAsync(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "ground-plane",
                Position = new Vec3 { X = 0, Y = 0, Z = 0 },
                Mass = 0,
                Shape = new Shape { Plane = new Plane { Normal = new Vec3 { X = 0, Y = 1, Z = 0 } } }
            }
        });

        // Add a dynamic sphere above it
        await client.SendCommandAsync(new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = "falling-sphere",
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Mass = 1.0,
                Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
            }
        });

        // Small delay for state propagation
        await Task.Delay(500);

        // Static bodies are no longer in the tick stream (TickState only has dynamic BodyPose).
        // Use StreamProperties to get the PropertySnapshot backfill which includes all bodies.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var propCall = client.StreamProperties(new StateRequest(), cancellationToken: cts.Token);
        var propStream = propCall.ResponseStream;

        Assert.True(await propStream.MoveNext(cts.Token));
        var backfill = propStream.Current;
        Assert.NotNull(backfill.Snapshot);
        var snapshot = backfill.Snapshot;

        Assert.True(snapshot.Bodies.Count >= 2, $"Expected at least 2 bodies in snapshot, got {snapshot.Bodies.Count}");

        var plane = snapshot.Bodies.FirstOrDefault(b => b.Id == "ground-plane");
        Assert.NotNull(plane);
        Assert.True(plane.IsStatic, "Plane should have IsStatic=true");

        var sphere = snapshot.Bodies.FirstOrDefault(b => b.Id == "falling-sphere");
        Assert.NotNull(sphere);
        Assert.False(sphere.IsStatic, "Sphere should have IsStatic=false");

        // Also verify that the tick stream does NOT contain the static body
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var tickCall = client.StreamState(new StateRequest(), cancellationToken: cts2.Token);
        if (await tickCall.ResponseStream.MoveNext(cts2.Token))
        {
            var tickState = tickCall.ResponseStream.Current;
            Assert.DoesNotContain(tickState.Bodies, b => b.Id == "ground-plane");
        }
    }
}
