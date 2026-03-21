using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class StressTestIntegrationTests
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
    public async Task SendBatchCommand_CanScaleToManyBodies()
    {
        var (app, channel) = await StartAppAndConnect();
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
