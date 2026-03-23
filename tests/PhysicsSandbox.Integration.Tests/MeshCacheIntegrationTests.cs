using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class MeshCacheIntegrationTests
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

        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));
        await Task.Delay(3000);

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

    private static SimulationCommand MakeConvexHullBody(string id)
    {
        var hull = new ConvexHull();
        hull.Points.Add(new Vec3 { X = 0, Y = 0, Z = 0 });
        hull.Points.Add(new Vec3 { X = 1, Y = 0, Z = 0 });
        hull.Points.Add(new Vec3 { X = 0, Y = 1, Z = 0 });
        hull.Points.Add(new Vec3 { X = 0, Y = 0, Z = 1 });

        return new SimulationCommand
        {
            AddBody = new AddBody
            {
                Id = id,
                Position = new Vec3 { X = 0, Y = 5, Z = 0 },
                Mass = 1.0,
                Shape = new Shape { ConvexHull = hull }
            }
        };
    }

    private static SimulationCommand MakeStepCommand() =>
        new() { Step = new StepSimulation { DeltaTime = 0.016 } };

    [Fact]
    public async Task ConvexHullBody_StateContainsCachedShapeRef_And_FetchMeshesReturnsGeometry()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add a ConvexHull body and step to generate state
        await client.SendCommandAsync(MakeConvexHullBody("hull1"));
        await client.SendCommandAsync(MakeStepCommand());
        await Task.Delay(500);

        // Stream state and verify CachedShapeRef
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var stateStream = client.StreamState(new StateRequest(), cancellationToken: cts.Token);

        Assert.True(await stateStream.ResponseStream.MoveNext(cts.Token));
        var state = stateStream.ResponseStream.Current;

        // Find the hull body
        var hullBody = state.Bodies.FirstOrDefault(b => b.Id == "hull1");
        Assert.NotNull(hullBody);
        Assert.Equal(Shape.ShapeOneofCase.CachedRef, hullBody.Shape.ShapeCase);

        var meshId = hullBody.Shape.CachedRef.MeshId;
        Assert.False(string.IsNullOrEmpty(meshId));
        Assert.Equal(32, meshId.Length); // 128-bit hash = 32 hex chars

        // Verify bounding box is valid
        var bbox = hullBody.Shape.CachedRef;
        Assert.True(bbox.BboxMin.X <= bbox.BboxMax.X);
        Assert.True(bbox.BboxMin.Y <= bbox.BboxMax.Y);
        Assert.True(bbox.BboxMin.Z <= bbox.BboxMax.Z);

        // Call FetchMeshes to retrieve the full geometry
        var fetchResponse = await client.FetchMeshesAsync(new MeshRequest { MeshIds = { meshId } });
        Assert.Single(fetchResponse.Meshes);
        Assert.Equal(meshId, fetchResponse.Meshes[0].MeshId);
        Assert.Equal(Shape.ShapeOneofCase.ConvexHull, fetchResponse.Meshes[0].Shape.ShapeCase);
        Assert.Equal(4, fetchResponse.Meshes[0].Shape.ConvexHull.Points.Count);
    }

    [Fact]
    public async Task LateJoiner_CanFetchMeshes_WithinTimeout()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Add ConvexHull body and let it run for a few ticks
        await client.SendCommandAsync(MakeConvexHullBody("hull-late"));
        for (int i = 0; i < 5; i++)
            await client.SendCommandAsync(MakeStepCommand());
        await Task.Delay(1000);

        // Late joiner: create new channel and stream state
        var httpClient2 = app.CreateHttpClient("server", "https");
        var channel2 = GrpcChannel.ForAddress(httpClient2.BaseAddress!, new GrpcChannelOptions
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
        var lateClient = new PhysicsHub.PhysicsHubClient(channel2);

        // Verify late joiner gets state with CachedShapeRef
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var lateStream = lateClient.StreamState(new StateRequest(), cancellationToken: cts.Token);

        Assert.True(await lateStream.ResponseStream.MoveNext(cts.Token));
        var state = lateStream.ResponseStream.Current;

        var hullBody = state.Bodies.FirstOrDefault(b => b.Id == "hull-late");
        Assert.NotNull(hullBody);
        Assert.Equal(Shape.ShapeOneofCase.CachedRef, hullBody.Shape.ShapeCase);

        // Late joiner fetches the mesh — should succeed within 5 seconds (SC-002)
        var meshId = hullBody.Shape.CachedRef.MeshId;
        var fetchResponse = await lateClient.FetchMeshesAsync(new MeshRequest { MeshIds = { meshId } });
        Assert.Single(fetchResponse.Meshes);
        Assert.Equal(Shape.ShapeOneofCase.ConvexHull, fetchResponse.Meshes[0].Shape.ShapeCase);
    }
}
