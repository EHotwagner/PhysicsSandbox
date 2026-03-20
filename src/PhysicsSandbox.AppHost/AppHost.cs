var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.PhysicsServer>("server")
    .WithEnvironment("ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS", "Http1AndHttp2");

builder.AddProject<Projects.PhysicsSimulation>("simulation")
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();
