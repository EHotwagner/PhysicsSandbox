using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class McpOrchestrationTests
{
    [Fact]
    public async Task McpResource_AppearsInAspireDashboard()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));

        // If we reach here without timeout, MCP resource started successfully
    }

    [Fact]
    public async Task McpResource_ShutsDownGracefully()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));

        // Graceful shutdown — DisposeAsync should complete without hanging
        await app.StopAsync();
    }

    [Fact]
    public async Task McpResource_WaitsForServer()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Server must be healthy before MCP can reach Running
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));

        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
    }
}
