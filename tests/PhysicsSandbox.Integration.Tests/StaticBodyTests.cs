using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class StaticBodyTests
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
    public async Task StaticPlane_AppearsInState_WithIsStaticTrue()
    {
        var (app, channel) = await StartAppAndConnect();
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

        // Check state
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var call = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        var stream = call.ResponseStream;

        if (await stream.MoveNext(cts.Token))
        {
            var state = stream.Current;
            Assert.True(state.Bodies.Count >= 2, $"Expected at least 2 bodies, got {state.Bodies.Count}");

            var plane = state.Bodies.FirstOrDefault(b => b.Id == "ground-plane");
            Assert.NotNull(plane);
            Assert.True(plane.IsStatic, "Plane should have IsStatic=true");

            var sphere = state.Bodies.FirstOrDefault(b => b.Id == "falling-sphere");
            Assert.NotNull(sphere);
            Assert.False(sphere.IsStatic, "Sphere should have IsStatic=false");
        }
    }
}
