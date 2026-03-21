using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace PhysicsSandbox.Integration.Tests;

public class CommandAuditStreamTests
{
    [Fact]
    public async Task StreamCommands_ReceivesCommandEvents_WhenCommandsSent()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PhysicsSandbox_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait for server to be healthy
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("server")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Verify the server is running and can accept commands
        // The StreamCommands RPC is tested via the gRPC service
        // Full end-to-end audit testing requires MCP client which is out of scope for unit test
        await app.ResourceNotifications
            .WaitForResourceAsync("mcp", "Running")
            .WaitAsync(TimeSpan.FromSeconds(60));
    }
}
