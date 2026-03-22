# Spec-Kit + Microservices: One Solution, One Spec-Kit

## The Design

One .NET solution, one spec-kit project, one constitution. Each microservice
is added as a **feature** through the normal spec-kit workflow. .NET Aspire
orchestrates everything — startup, shutdown, service discovery, observability.

```
Platform/
  Platform.sln
  .specify/
    memory/constitution.md            # fsMicroservices constitution
    specs/
      001-order-service/              # each service is a feature spec
      002-inventory-service/
      003-notification-service/
  CLAUDE.md                           # system architecture, service map

  src/
    Platform.AppHost/                 # Aspire orchestrator (C#, ~30 lines)
      Program.cs
    Platform.ServiceDefaults/         # Shared telemetry, health checks, resilience
    Platform.Shared.Contracts/        # Proto files, shared DTOs

    OrderService/                     # F# service project
      OrderService.fsproj
      OrderService.fsi
    InventoryService/
      InventoryService.fsproj
      InventoryService.fsi
    NotificationService/
      NotificationService.fsproj
      NotificationService.fsi

  tests/
    Platform.Integration.Tests/       # Cross-service Aspire tests
    OrderService.Tests/
    InventoryService.Tests/
```

---

## Why This Works

**Spec-kit does one feature at a time.** Its branch-name-as-state design
assumes sequential work. Adding services one at a time is exactly that flow.
Each `/speckit.specify` starts a fresh branch and fresh context.

**Context resets naturally.** When you start a new service/feature, you begin
a new conversation. The AI reads the constitution, the new spec, and only the
code it needs. It does not carry context from previous services. The
accumulated codebase on disk grows, but spec-kit's plan/tasks pipeline scopes
the work before implementation.

**Aspire grows incrementally.** Each new service adds a few lines to
`AppHost/Program.cs`. The contracts project accumulates proto files. `dotnet
run` on AppHost starts whatever exists so far.

**The constitution enforces consistency.** Every service gets the same `.fsi`
contracts, integration tests, observability — governed by the same
`fsMicroservices` constitution.

**No orchestration problem.** Everything is project references in one
solution. No submodules, no NuGet gymnastics, no container registries for
local dev.

---

## The Spec-Kit Workflow

### Adding a New Service

1. **`/speckit.specify`** — "Add OrderService: a gRPC service that handles
   order creation, retrieval, and status updates. Communicates with
   InventoryService for stock validation."

2. **`/speckit.plan`** — The plan identifies:
   - Proto contracts to add in `Platform.Shared.Contracts`
   - New F# project with `.fsi` signatures
   - Aspire AppHost registration
   - Integration tests

3. **`/speckit.tasks`** — Tasks ordered by dependency:
   - Contract definitions (proto files)
   - F# project scaffolding + `.fsi` signatures
   - Service implementation
   - AppHost registration (one `AddProject` + `WithReference` call)
   - Unit tests, then integration tests via Aspire

4. **`/speckit.implement`** — Execute tasks. Constitution enforces `.fsi`
   files, contract-first design, and test evidence.

### Adding a Feature to an Existing Service

Same workflow, but scoped to one service. The spec mentions which service,
the plan stays within that project directory. AppHost doesn't change.

### Cross-Service Features

Spec describes user-visible behavior spanning services. Plan identifies
all affected contracts and services. Tasks start with contract changes,
then per-service implementation, then integration tests.

---

## .NET Aspire in Detail

### What Aspire Is

Aspire is the orchestration layer for .NET distributed applications. It
replaces Docker Compose, Project Tye, and custom startup scripts. A single
`dotnet run` on the AppHost project starts all services, databases, message
queues — with automatic service discovery, health-check-based dependency
ordering, and a built-in observability dashboard.

### The AppHost

The AppHost is a small C# project (~30 lines) that defines the system
topology. Keep it in C# even though services are F# — it's trivial
boilerplate and avoids potential MSBuild-target issues with the
`Projects.*` type generation.

```csharp
// src/Platform.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("db")
    .AddDatabase("orders")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);  // survives AppHost restarts

var cache = builder.AddRedis("cache");

var rabbitmq = builder.AddRabbitMQ("messaging");

// Services
var orderService = builder.AddProject<Projects.OrderService>("order-service")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres);

var inventoryService = builder.AddProject<Projects.InventoryService>("inventory-service")
    .WithReference(postgres)
    .WithReference(cache)
    .WaitFor(postgres);

var notificationService = builder.AddProject<Projects.NotificationService>("notification-service")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.Build().Run();
```

**Key concepts:**

- **`AddProject<T>()`** — Registers a .NET project. The `Projects.*` type
  is auto-generated from project references at build time.
- **`.WithReference(resource)`** — Injects connection details (host, port,
  credentials) as environment variables. For infrastructure, this means
  connection strings. For services, this enables service discovery.
- **`.WaitFor(resource)`** — Delays startup until the dependency's health
  checks pass. Infrastructure integrations (Postgres, Redis) register their
  own health checks automatically.
- **`.WaitForCompletion(resource)`** — Waits for a resource to finish and
  exit. Use for migrations or seed scripts that must complete before
  services start.
- **`ContainerLifetime.Persistent`** — Container survives AppHost restarts.
  Aspire detects config changes and recreates only when necessary. Combined
  with `WithDataVolume()`, this gives fast startup + data safety.

### Service Discovery

Services find each other by logical name, not hardcoded URLs or ports.

**HTTP clients:**
```fsharp
// In OrderService's Program.fs
builder.Services.AddHttpClient("inventory", fun client ->
    client.BaseAddress <- Uri("https+http://inventory-service"))
|> ignore
```

**gRPC clients:**
```fsharp
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(fun options ->
    options.Address <- Uri("https+http://inventory-service"))
|> ignore
```

The `https+http://` prefix means "prefer HTTPS, fall back to HTTP."
`AddServiceDefaults()` automatically configures service discovery on all
HttpClient-based clients (including gRPC).

**How it works under the hood:** When the AppHost wires `.WithReference(orderService)`,
it injects environment variables like `services__order-service__https__0=https://localhost:54321`
into the consuming service. The .NET service discovery extension reads these
at runtime. Port randomization is on by default to prevent conflicts.

**Named endpoints:** For services with multiple endpoints:
```csharp
var basket = builder.AddProject<Projects.BasketService>("basket")
    .WithHttpsEndpoint(port: 9999, name: "dashboard");
// Consumed as: https+http://_dashboard.basket
```

### Platform.ServiceDefaults

A shared class library that every service references. Calling
`builder.AddServiceDefaults()` in each service's `Program.fs` configures:

- **OpenTelemetry** — Structured logging, distributed tracing, and runtime
  metrics. Traces automatically correlate across service boundaries.
  Instrumentation for ASP.NET Core, HttpClient, and .NET runtime. Health
  check endpoints are filtered out of traces.
- **Health checks** — `/health` (readiness, all checks) and `/alive`
  (liveness, self-check only). Aspire uses these for `WaitFor()` and
  dashboard status.
- **Service discovery** — Automatically added to all HttpClient instances.
- **Resilience** — `AddStandardResilienceHandler()` on all outgoing HTTP
  calls (retry, circuit breaker, timeout, rate limiting).

```fsharp
// In each service's Program.fs
let builder = WebApplication.CreateBuilder(args)
builder.AddServiceDefaults()

// ... register services ...

let app = builder.Build()
app.MapDefaultEndpoints()  // /health and /alive
app.Run()
```

### The Dashboard

Launches automatically at `https://localhost:15888` when you run the AppHost.

**Five views:**

1. **Resources** — All services, containers, executables. Shows state
   (running/stopped/error), start time, endpoints. Dependency graph view.
   **Stop, start, and restart individual services** via context menu.

2. **Console Logs** — Raw stdout/stderr per resource. Color-coded severity.
   Downloadable.

3. **Structured Logs** — Semantic logging via OpenTelemetry. Filter by
   service, level, or message. Links to related traces. JSON/XML detail
   views.

4. **Traces** — Distributed trace waterfall across services. Click a trace
   to see the full span tree with timing, errors, and per-service
   color-coding. Jump from a trace to related logs.

5. **Metrics** — Per-instrument charts and tables. Exemplar dots link
   metrics to specific traces for drill-down.

All telemetry is received via OTLP (port 4317). Aspire automatically sets
`OTEL_EXPORTER_OTLP_ENDPOINT` on every managed resource.

The dashboard also runs standalone as a container — any app sending OTLP
to it gets visualized, even non-.NET apps.

### Integration Testing

Aspire's testing infrastructure spins up the **entire distributed system**
for integration tests. No mocks, no stubs — real services, real databases.

```csharp
// tests/Platform.Integration.Tests/OrderFlowTests.cs
public class OrderFlowTests
{
    [Fact]
    public async Task CreateOrder_DeductsInventory()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Platform_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait for services to be healthy
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("order-service")
            .WaitAsync(TimeSpan.FromSeconds(30));
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("inventory-service")
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Create an order via HTTP
        using var orderClient = app.CreateHttpClient("order-service");
        var response = await orderClient.PostAsJsonAsync("/orders",
            new { ProductId = 1, Qty = 5 });
        response.EnsureSuccessStatusCode();

        // Verify inventory was updated via the other service
        using var inventoryClient = app.CreateHttpClient("inventory-service");
        var stock = await inventoryClient
            .GetFromJsonAsync<Stock>("/stock/1");
        Assert.Equal(95, stock.Quantity);
    }
}
```

**Key testing APIs:**

| Method | Purpose |
|--------|---------|
| `DistributedApplicationTestingBuilder.CreateAsync<T>()` | Spins up the full AppHost |
| `app.CreateHttpClient("name")` | Pre-configured client with service discovery |
| `WaitForResourceHealthyAsync("name")` | Blocks until health checks pass |
| `WaitForResourceAsync("name", KnownResourceStates.Running)` | Blocks until resource is running |

**Gotchas:**

- Tests launch the AppHost as a separate process — you **cannot** mock/substitute
  DI services within the services under test. Influence behavior via
  environment variables or configuration only.
- Always use timeouts (`.WaitAsync(TimeSpan)`) to prevent hanging tests.
- Port randomization is on by default (prevents CI conflicts).
- The dashboard is disabled in tests by default.

### gRPC Testing

```csharp
// Create a gRPC channel from the test HTTP client
using var httpClient = app.CreateHttpClient("order-service");
var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
{
    HttpHandler = new HttpClientHandler()
});
var client = new OrderService.OrderServiceClient(channel);
var response = await client.CreateOrderAsync(
    new CreateOrderRequest { ProductId = 1 });
```

### Podman Support

Since the host runs rootless Podman, set the container runtime in the
AppHost's `launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "environmentVariables": {
        "ASPIRE_CONTAINER_RUNTIME": "podman"
      }
    }
  }
}
```

Aspire then uses Podman for all container resources (Postgres, Redis,
RabbitMQ, etc.). .NET service projects still run as regular processes,
not containers.

### F# Services with Aspire

Service projects work perfectly in F#. The AppHost references `.fsproj`
files the same way as `.csproj`. Service discovery, health checks, and
OpenTelemetry all work through standard .NET APIs.

**Keep the AppHost in C#.** There are zero F# AppHost templates or samples
from Microsoft. The `Projects.*` types are generated by MSBuild targets in
the Aspire SDK, which haven't been tested with F# project files. Since the
AppHost is ~30 lines of pure boilerplate, writing it in C# costs nothing.

### Resource Lifecycle

1. **Discovery** — AppHost analyzes all resources and their dependency graph.
2. **Startup** — Resources launch in dependency order. Resources without
   dependencies start in parallel. `WaitFor` gates block dependents until
   health checks pass.
3. **Runtime** — Continuous health monitoring. Dashboard shows real-time
   status. Individual services can be stopped/started/restarted.
4. **Shutdown** — Resources torn down. Persistent containers
   (`ContainerLifetime.Persistent`) survive shutdown and are reused on
   next startup.

**Lifecycle events** for custom logic:
```csharp
builder.Eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
{
    // Run before any resource starts
    return Task.CompletedTask;
});

var cache = builder.AddRedis("cache");
cache.OnResourceReady((resource, @event, ct) =>
{
    // Redis is healthy and ready
    return Task.CompletedTask;
});
```

### Hot Reload

- Hot reload works for individual service projects for supported code changes.
- The dashboard provides **stop/start/restart buttons** per service — use
  these for changes that hot reload can't handle.
- Changes to the AppHost topology (adding/removing services) require a full
  AppHost restart.
-

## Independent Versioning and CI

Even in a mono-repo, services can be independently versioned and deployed.

**Per-service versioning** with
[Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning):

```json
// src/OrderService/version.json
{ "version": "1.2", "pathFilters": [ "." ] }
```

Version only bumps when files in that service's directory change.

**Path-filtered CI:**

```yaml
# .github/workflows/order-service.yml
on:
  push:
    paths:
      - 'src/OrderService/**'
      - 'src/Platform.Shared.Contracts/**'
```

Each service has its own workflow that triggers only on relevant changes.

---

## Tools and Resources

| Tool | Purpose | Link |
|------|---------|------|
| spec-kit | Spec-driven AI development | [github/spec-kit](https://github.com/github/spec-kit) |
| .NET Aspire | Orchestration, service discovery, observability | [aspire.dev](https://aspire.dev) |
| Buf Schema Registry | Protobuf contract management, breaking changes | [buf.build](https://buf.build) |
| PactNet | Consumer-driven contract testing | [pact-foundation/pact-net](https://github.com/pact-foundation/pact-net) |
| Nerdbank.GitVersioning | Independent per-service versioning in mono-repo | [dotnet/Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) |
| TestContainers | Real infrastructure in tests | [dotnet.testcontainers.org](https://dotnet.testcontainers.org) |

## Key Sources

- [Aspire AppHost Overview](https://aspire.dev/fundamentals/app-host-overview/)
- [Aspire Service Discovery](https://aspire.dev/fundamentals/service-discovery/)
- [Aspire Dashboard](https://aspire.dev/fundamentals/dashboard/)
- [Aspire Testing](https://aspire.dev/testing/write-your-first-test/)
- [Aspire Networking & Endpoints](https://aspire.dev/fundamentals/networking-overview/)
- [Aspire Container Resources](https://aspire.dev/fundamentals/add-docker-resources/)
- [Aspire Health Checks](https://aspire.dev/fundamentals/health-checks/)
- [Aspire Eventing](https://aspire.dev/fundamentals/eventing/)
- [Spec-Kit Monorepo Issue #1026](https://github.com/github/spec-kit/issues/1026)
- [Spec-Kit Multi-Repo Issue #1095](https://github.com/github/spec-kit/issues/1095)
- [Constitutional SDD Paper (arXiv:2602.02584)](https://arxiv.org/abs/2602.02584)
- [eShop Reference App (mono-repo pattern)](https://github.com/dotnet/eShop)
