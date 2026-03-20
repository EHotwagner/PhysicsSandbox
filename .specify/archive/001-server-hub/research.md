# Research: Contracts and Server Hub

**Feature**: 001-server-hub | **Date**: 2026-03-20

## R-001: gRPC Service Design for Hub Pattern

**Decision**: Single `PhysicsHub` gRPC service with role-specific methods, plus a separate `SimulationLink` service for the simulation-to-server interface.

**Rationale**: The PhysicsHub service serves clients and viewers (send commands, stream state). The simulation needs a different interaction pattern — it pushes state and receives commands via bidirectional or dual-streaming. Separating into two services keeps each interface clean and allows independent evolution. However, both live in the same proto file since they're part of the same system boundary.

**Alternatives considered**:
- Single service with all methods: Simpler but conflates client and simulation concerns. A `SendCommand` method makes sense for clients but not for the simulation receiving commands.
- Bidirectional streaming on PhysicsHub: Overloads the client-facing service with simulation-specific semantics.

## R-002: Simulation-to-Server Communication Pattern

**Decision**: The simulation calls `ConnectSimulation` on the server, establishing a bidirectional stream. The simulation pushes `SimulationState` messages upstream; the server pushes `SimulationCommand` messages downstream through the same stream.

**Rationale**: Bidirectional streaming is the natural fit for a persistent connection where both sides need to send messages continuously. The simulation connects to the server (not the other way around) because the server is the stable hub that starts first. This matches the Aspire `WaitFor(server)` dependency pattern.

**Alternatives considered**:
- Two separate unary RPCs (PushState, PollCommands): Higher latency, polling overhead.
- Server-initiated connection to simulation: Violates the hub pattern where the server is passive and services connect to it.
- Message queue (RabbitMQ): Overkill for a local sandbox with one simulation instance.

## R-003: State Caching for Late Joiners

**Decision**: The server maintains a single in-memory reference to the most recently received `SimulationState`. When a new subscriber connects to `StreamState`, the cached state is sent as the first message before live updates begin.

**Rationale**: Simple and sufficient. The physics sandbox has one simulation producing state. Caching the latest snapshot means late-joining viewers see the current world immediately instead of a blank screen.

**Alternatives considered**:
- Ring buffer of N recent states: Unnecessary complexity. Clients only need the current state.
- No caching (wait for next update): Poor UX — viewer would show nothing until next simulation tick.

## R-004: Aspire AppHost and ServiceDefaults Setup

**Decision**: Use the standard .NET Aspire 9.x project templates. AppHost in C#, ServiceDefaults in C#. The AppHost registers PhysicsServer only (no other services yet). Podman runtime configured via `launchSettings.json`.

**Rationale**: Standard Aspire templates are well-tested and provide health checks, OpenTelemetry, service discovery, and resilience out of the box. No customization needed beyond Podman configuration for the host environment.

**Alternatives considered**:
- Custom orchestration (Docker Compose, Project Tye): Aspire is the modern .NET standard and provides the dashboard, health-based startup ordering, and testing infrastructure that alternatives lack.
- F# AppHost: No Microsoft templates or tested MSBuild targets exist. Risk of build issues for zero benefit.

## R-005: F# Server Structure and .fsi Contracts

**Decision**: PhysicsServer project organized into `Services/` (gRPC service implementations) and `Hub/` (domain logic — routing and caching). Each public module has an `.fsi` file. The gRPC service layer is thin — it delegates to the `MessageRouter` and `StateCache` modules.

**Rationale**: Separating gRPC plumbing from domain logic makes the router testable without gRPC infrastructure. The `.fsi` files enforce the constitution's compiler-verified contracts and make the public API explicit.

**Alternatives considered**:
- Single file with everything: Doesn't scale as the server grows. Harder to test routing logic in isolation.
- Separate library project for hub logic: Over-engineering for a sandbox. The internal module split achieves the same separation without an extra project.

## R-006: Testing Strategy

**Decision**: Two test projects. `PhysicsServer.Tests` (F#, xUnit) for unit testing `MessageRouter` and `StateCache` in isolation. `PhysicsSandbox.Integration.Tests` (C#, xUnit, Aspire) for end-to-end tests that start the AppHost, connect a gRPC client, and verify command/state flows.

**Rationale**: Unit tests validate routing logic without gRPC overhead. Integration tests validate the real gRPC service running in Aspire. Constitution requires both test evidence and integration-first testing.

**Alternatives considered**:
- Integration tests only: Slower feedback loop for logic changes. Constitution requires unit tests supplement integration tests.
- F# integration tests: Aspire's `DistributedApplicationTestingBuilder` works but has more C# examples and documentation. C# is pragmatic here since the test project contains no domain logic.

## R-007: Single Simulation Enforcement

**Decision**: The `ConnectSimulation` RPC on the server tracks whether a simulation is already connected. If a second simulation calls `ConnectSimulation`, the server returns an error status (ALREADY_EXISTS) rejecting the connection.

**Rationale**: The spec requires single-simulation support (FR-014). Rejecting (rather than replacing) is safer — it prevents accidental disconnection of a running simulation. The existing simulation must disconnect before a new one can take over.

**Alternatives considered**:
- Replace existing simulation: Risks disrupting an active simulation unexpectedly.
- Allow multiple and merge: Conflicting state streams would produce incoherent physics. No use case for this in a sandbox.
