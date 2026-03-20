# Research: Client REPL Library

**Feature**: 004-client-repl | **Date**: 2026-03-20

## R1: gRPC Client Approach for F# Library

**Decision**: Contract-first with standard Grpc.Net.Client, consuming C# generated types from PhysicsSandbox.Shared.Contracts.

**Rationale**: The project already uses this approach for PhysicsSimulation and PhysicsViewer. The proto-generated C# types (PhysicsHub.PhysicsHubClient, SimulationCommand, ViewCommand, etc.) are already available via project reference. No new tooling needed. The fsgrpc-client skill confirms this is the standard pattern: `GrpcChannel.ForAddress` → `PhysicsHub.PhysicsHubClient(channel)`.

**Alternatives considered**:
- Code-first (protobuf-net): Would require redefining all contracts as F# records — redundant since proto types already exist.
- FsGrpc: Would give idiomatic F# records but adds buf CLI dependency and a parallel generated codebase for no gain.

## R2: FSI Loadability

**Decision**: Build as a standard F# class library (`Microsoft.NET.Sdk`). Users load it in FSI via `#r "path/to/PhysicsClient.dll"` (or `#r "nuget: ..."` if packaged). Provide a convenience `.fsx` script that loads the DLL and opens the namespace.

**Rationale**: FSI can load any .NET DLL. The library avoids top-level async initialization, static mutable state, or host builder patterns that don't work in FSI. All functions take an explicit `Session` parameter. The only async operations (connect, command sending) use `Async.RunSynchronously` internally for REPL convenience, with async variants available for scripts.

**Alternatives considered**:
- Source-only (.fsx): Would avoid DLL but can't reference proto-generated types easily; no .fsi enforcement.
- .NET tool: Heavier than needed; user wants a loadable library, not a CLI.

## R3: Spectre.Console for Display

**Decision**: Use Spectre.Console for all formatted terminal output — tables for body lists, panels for body inspection, live display for watch mode.

**Rationale**: User explicitly requested Spectre.Console for TUI display. Spectre.Console provides rich table formatting, color support, and a `Live` context that can update in-place — perfect for the live-watch mode that prints updates until cancelled. It works in standard terminals and FSI. Single NuGet dependency with no native requirements.

**Key patterns**:
- `Table` for body list display (columns: ID, Shape, Position, Velocity, Mass)
- `Panel` for single-body inspection
- `AnsiConsole.Live()` with `CancellationToken` for watch mode — updates a table in-place, user presses Ctrl+C to stop
- `AnsiConsole.MarkupLine()` for colored status messages

**Alternatives considered**:
- Plain `printfn` with manual formatting: Works but ugly, hard to align columns.
- Terminal.Gui: Full TUI framework — overkill for print functions + live watch.

## R4: Session Design and Error Handling

**Decision**: Session is a record holding GrpcChannel, PhysicsHubClient, CancellationTokenSource, and mutable state (body ID registry, cached latest SimulationState). Functions return `Result<'T, string>` for REPL friendliness.

**Rationale**: The fsgrpc-client skill recommends reusing a single GrpcChannel (thread-safe, multiplexed). The session manages one channel and one background state-streaming task. Using Result types (not exceptions) prevents stack traces from cluttering the REPL. The session caches the latest SimulationState received from the StreamState subscription for synchronous queries.

**Key patterns from existing services**:
- Channel creation: `GrpcChannel.ForAddress(serverAddress)` (from SimulationClient.fs)
- SSL handling: `SocketsHttpHandler` with `RemoteCertificateValidationCallback` returning true (from ViewerClient.fs, needed for dev certs)
- State streaming: Subscribe to `StreamState`, drain into cached state (from ViewerClient.fs pattern)
- Error handling: Catch `RpcException`, map `StatusCode` to friendly strings (from fsgrpc-client skill)

**Alternatives considered**:
- Disposable session (IDisposable): Good practice but awkward in REPL (must `use` or manually dispose). Instead, provide explicit `disconnect` function.

## R5: Body ID Generation

**Decision**: Thread-safe counter per shape type. Format: `"{shapeType}-{counter}"` (e.g., "sphere-1", "box-3"). Counter is per-session, resets on reconnect.

**Rationale**: Human-readable IDs are essential for REPL use — the user needs to reference bodies by ID for steering, inspection, and removal. Per-shape counters give immediate context about what the body is. Thread-safe because the background state stream and user commands may interact concurrently.

**Alternatives considered**:
- GUIDs: Unreadable in REPL.
- Global counter (body-1, body-2): Loses shape context.

## R6: Steering Math

**Decision**: Steering functions operate on named directions (Up, Down, North, South, East, West mapped to ±Y and ±X/Z axes) and target positions. They compute the appropriate Vec3 and delegate to existing ApplyForce/ApplyImpulse/ApplyTorque proto commands.

**Rationale**: The simulation uses Y-up coordinate system (ground grid at Y=0, gravity default (0, -9.81, 0)). Named directions make the REPL experience intuitive. `launch` computes direction vector from current body position (from cached state) to target, normalizes, and scales by a magnitude parameter.

**Coordinate mapping**:
- Up/Down: ±Y
- North/South: ±Z
- East/West: ±X

## R7: Aspire Integration

**Decision**: Register PhysicsClient as an Aspire project in AppHost for orchestrated runs. The library also works standalone (user provides server address manually).

**Rationale**: Follows the existing pattern — all services are registered in AppHost. For the client, this means Aspire can start it with service discovery injected. But since the primary use case is FSI, the connect function accepts an explicit server address parameter, defaulting to Aspire's service discovery URL pattern.

**AppHost addition**:
```csharp
builder.AddProject<Projects.PhysicsClient>("client")
    .WithReference(server)
    .WaitFor(server);
```
