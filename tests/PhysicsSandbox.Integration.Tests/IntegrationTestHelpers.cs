using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Grpc.Net.Client;

namespace PhysicsSandbox.Integration.Tests;

public static class IntegrationTestHelpers
{
    private static GrpcChannel CreateGrpcChannel(DistributedApplication app)
    {
        var httpClient = app.CreateHttpClient("server", "https");
        return GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
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
    }

    public static async Task<(DistributedApplication App, GrpcChannel Channel)> StartAppAndConnect()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));

        return (app, CreateGrpcChannel(app));
    }

    public static async Task<(DistributedApplication App, GrpcChannel Channel)> StartAppAndConnectWithSimulation()
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
            .WaitAsync(TimeSpan.FromSeconds(60));
        // Give the simulation time to establish gRPC connection to server
        await Task.Delay(3000);

        return (app, CreateGrpcChannel(app));
    }

    public static async Task<(DistributedApplication App, GrpcChannel Channel)> StartServerOnly()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();
        var app = await appHost.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));
        return (app, CreateGrpcChannel(app));
    }
}
