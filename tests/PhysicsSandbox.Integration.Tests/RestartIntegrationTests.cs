using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class RestartIntegrationTests
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
    public async Task ResetSimulation_ClearsAllBodies()
    {
        var (app, channel) = await StartAppAndConnect();
        await using var _ = app;

        var client = new PhysicsHub.PhysicsHubClient(channel);

        // Wait for simulation to connect
        await app.ResourceNotifications
            .WaitForResourceAsync("simulation", "Running")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Add 5 bodies
        for (int i = 0; i < 5; i++)
        {
            await client.SendCommandAsync(new SimulationCommand
            {
                AddBody = new AddBody
                {
                    Id = $"reset-test-{i}",
                    Position = new Vec3 { X = i, Y = 5, Z = 0 },
                    Mass = 1.0,
                    Shape = new Shape { Sphere = new Sphere { Radius = 0.5 } }
                }
            });
        }

        // Small delay for state propagation
        await Task.Delay(500);

        // Send reset command
        var ack = await client.SendCommandAsync(new SimulationCommand
        {
            Reset = new ResetSimulation()
        });
        Assert.True(ack.Success);

        // Wait for state propagation
        await Task.Delay(500);

        // Check state via streaming — should have 0 bodies
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var call = client.StreamState(new StateRequest(), cancellationToken: cts.Token);
        var stream = call.ResponseStream;

        if (await stream.MoveNext(cts.Token))
        {
            var state = stream.Current;
            Assert.Equal(0, state.Bodies.Count);
            Assert.Equal(0.0, state.Time);
        }
    }
}
