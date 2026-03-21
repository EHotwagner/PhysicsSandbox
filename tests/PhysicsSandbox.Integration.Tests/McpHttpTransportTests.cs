using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class McpHttpTransportTests
{
    [Fact]
    public async Task McpServer_RunsAsHttpService_StaysRunningWithoutClients()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // MCP server should reach Running state as an HTTP service
        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));

        // Wait a moment to verify it stays running without any client connections
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Verify MCP is still running (not exited like stdio transport would)
        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task McpServer_AcceptsHttpConnections()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));

        // Get the MCP server's HTTP endpoint and verify it responds
        var httpClient = app.CreateHttpClient("mcp");
        var response = await httpClient.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode,
            $"MCP health endpoint returned {response.StatusCode}");
    }
}
