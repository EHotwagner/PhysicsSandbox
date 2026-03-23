var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.PhysicsServer>("server")
    .WithEnvironment("ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS", "Http1AndHttp2");

builder.AddProject<Projects.PhysicsSimulation>("simulation")
    .WithReference(server)
    .WaitFor(server);

builder.AddProject<Projects.PhysicsViewer>("viewer")
    .WithReference(server)
    .WaitFor(server)
    .WithEnvironment("DISPLAY", Environment.GetEnvironmentVariable("DISPLAY") ?? ":0");

builder.AddProject<Projects.PhysicsClient>("client")
    .WithReference(server)
    .WaitFor(server);

builder.AddProject<Projects.PhysicsSandbox_Mcp>("mcp")
    .WithReference(server)
    .WaitFor(server)
    .WithHttpEndpoint(port: 5199, name: "http");

builder.Build().Run();
