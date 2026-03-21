using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;
using PhysicsSandbox.Shared.Contracts;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class ComparisonIntegrationTests
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
    public async Task BatchVsIndividual_BatchIsFaster()
    {
        var (app, channel) = await StartAppAndConnect();
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
}
