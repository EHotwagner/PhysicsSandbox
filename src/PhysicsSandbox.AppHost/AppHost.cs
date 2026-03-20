var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.PhysicsServer>("server")
    .WithEnvironment("ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS", "Http1AndHttp2");

builder.Build().Run();
