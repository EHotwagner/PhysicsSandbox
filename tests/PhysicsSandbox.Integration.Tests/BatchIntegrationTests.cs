using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class BatchIntegrationTests
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
    public async Task SendBatchCommand_ExecutesAllCommands_ReturnsPerCommandResults()
    {
        var (app, channel) = await StartAppAndConnect();
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
        var (app, channel) = await StartAppAndConnect();
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
}
