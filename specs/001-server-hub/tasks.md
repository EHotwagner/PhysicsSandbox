# Tasks: Contracts and Server Hub

**Input**: Design documents from `/specs/001-server-hub/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — constitution (Principle VI) requires test evidence for all behavior-changing code.

**Organization**: Tasks are grouped by user story. Note that US1, US2, and US4 are all P1/P2 foundational stories that must complete before US3 (Server Hub) can be implemented.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Solution Structure)

**Purpose**: Create the .NET solution and directory structure

- [X] T001 Create PhysicsSandbox.sln solution file and top-level directory structure (`src/`, `tests/`) per plan.md project structure
- [X] T002 Add .gitignore for .NET projects (bin/, obj/, .vs/, *.user) at repository root

---

## Phase 2: User Story 1 — Solution Foundation and Orchestrator (Priority: P1)

**Goal**: Create the Aspire AppHost orchestrator so that running it launches the dashboard — even with no services registered yet.

**Independent Test**: Run the AppHost and verify the Aspire dashboard launches at the configured URL with an empty resource list.

### Implementation for User Story 1

- [X] T003 [US1] Create `src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj` with Aspire AppHost SDK and hosting references
- [X] T004 [US1] Create `src/PhysicsSandbox.AppHost/Program.cs` with minimal Aspire builder (no services registered yet — just `builder.Build().Run()`)
- [X] T005 [US1] Create `src/PhysicsSandbox.AppHost/Properties/launchSettings.json` with Podman runtime (`ASPIRE_CONTAINER_RUNTIME=podman`) and dashboard URL configuration
- [X] T006 [US1] Add AppHost project to `PhysicsSandbox.sln`

**Checkpoint**: `dotnet run --project src/PhysicsSandbox.AppHost` launches the Aspire dashboard with an empty resource list.

---

## Phase 3: User Story 2 — Shared Communication Contracts (Priority: P1)

**Goal**: Define all gRPC message types and service interfaces in a shared contracts project so that any service can reference them.

**Independent Test**: Build the contracts project and verify it compiles, generating typed C# stubs for all messages and services.

### Implementation for User Story 2

- [X] T007 [US2] Create `src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj` with Grpc.AspNetCore, Google.Protobuf, and Grpc.Tools dependencies; configure proto compilation
- [X] T008 [US2] Copy proto contract from `specs/001-server-hub/contracts/physics_hub.proto` to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [X] T009 [US2] Add Contracts project to `PhysicsSandbox.sln`
- [X] T010 [US2] Verify `dotnet build src/PhysicsSandbox.Shared.Contracts` succeeds and generates typed stubs for `PhysicsHub`, `SimulationLink`, and all message types

**Checkpoint**: `dotnet build src/PhysicsSandbox.Shared.Contracts` succeeds. Generated code includes `PhysicsHub.PhysicsHubBase`, `SimulationLink.SimulationLinkBase`, `SimulationCommand`, `ViewCommand`, `SimulationState`, `Body`, `Vec3`, `Shape`, `CommandAck`, and `StateRequest`.

---

## Phase 4: User Story 4 — Shared Service Defaults (Priority: P2)

**Goal**: Create the shared ServiceDefaults project that provides health checks, OpenTelemetry, service discovery, and resilience to all services.

**Independent Test**: Reference ServiceDefaults from any ASP.NET project and verify that `/health` and `/alive` endpoints become available.

> **Note**: US4 is implemented before US3 because the Server Hub depends on ServiceDefaults.

### Implementation for User Story 4

- [X] T011 [US4] Create `src/PhysicsSandbox.ServiceDefaults/PhysicsSandbox.ServiceDefaults.csproj` with OpenTelemetry, health check, service discovery, and resilience NuGet dependencies
- [X] T012 [US4] Create `src/PhysicsSandbox.ServiceDefaults/Extensions.cs` with `AddServiceDefaults()` extension method (OpenTelemetry tracing/metrics/logging, health checks, service discovery, standard resilience handler) and `MapDefaultEndpoints()` method (`/health` and `/alive` endpoints)
- [X] T013 [US4] Add ServiceDefaults project to `PhysicsSandbox.sln`

**Checkpoint**: ServiceDefaults builds. Any ASP.NET project referencing it gains health check endpoints and OpenTelemetry instrumentation via `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.

---

## Phase 5: User Story 3 — Server Hub with Message Routing (Priority: P1) 🎯 MVP

**Goal**: Create the PhysicsServer service — the central message router. It accepts commands from clients, receives state from the simulation (via bidirectional stream), caches latest state, and fans out state updates to all subscribers.

**Independent Test**: Start the AppHost, connect a gRPC test client, send a SimulationCommand, and receive an acknowledgment. Subscribe to StreamState and verify late-joiner caching works.

### Project Scaffolding for User Story 3

- [X] T014 [US3] Create `src/PhysicsServer/PhysicsServer.fsproj` (F#) with references to Contracts, ServiceDefaults, and Grpc.AspNetCore; add to solution. Include a minimal `Program.fs` stub so the project compiles (needed before test projects can reference it).

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T015 [P] [US3] Create `tests/PhysicsServer.Tests/PhysicsServer.Tests.fsproj` (F#, xUnit) with project reference to PhysicsServer; add to solution
- [X] T016 [P] [US3] Create `tests/PhysicsSandbox.Integration.Tests/PhysicsSandbox.Integration.Tests.csproj` (C#, xUnit) with Aspire testing SDK, Grpc.Net.Client, and project reference to AppHost; add to solution
- [X] T017 [P] [US3] Write unit tests in `tests/PhysicsServer.Tests/StateCacheTests.fs`: test that cache returns None when empty, returns latest state after update, and overwrites previous state on subsequent update
- [X] T018 [P] [US3] Write unit tests in `tests/PhysicsServer.Tests/MessageRouterTests.fs`: test command acceptance with no simulation connected (drop gracefully), test state fanout to multiple subscribers, test single-simulation enforcement (reject second connection)
- [X] T019 [US3] Write integration test in `tests/PhysicsSandbox.Integration.Tests/ServerHubTests.cs`: start AppHost, wait for server healthy, connect gRPC client, call `SendCommand` and assert `CommandAck.Success`, subscribe to `StreamState` and verify stream opens without error

### Implementation for User Story 3
- [X] T020 [US3] Create `src/PhysicsServer/Hub/StateCache.fsi` — signature file declaring: `module PhysicsServer.Hub.StateCache` with functions to get current state (option), update state, and clear
- [X] T021 [US3] Create `src/PhysicsServer/Hub/StateCache.fs` — implementation: thread-safe in-memory cache of the latest `SimulationState` (single mutable reference with locking)
- [X] T022 [US3] Create `src/PhysicsServer/Hub/MessageRouter.fsi` — signature file declaring: `module PhysicsServer.Hub.MessageRouter` with types and functions for registering/unregistering state subscribers, publishing state to all subscribers, submitting commands, consuming commands (for simulation), and simulation connection management (connect/disconnect/isConnected)
- [X] T023 [US3] Create `src/PhysicsServer/Hub/MessageRouter.fs` — implementation: manages subscriber set, command channel, single-simulation lock, state fanout with latest-state caching via StateCache, graceful handling of commands when no simulation connected
- [X] T024 [US3] Create `src/PhysicsServer/Services/PhysicsHubService.fsi` — signature file declaring the gRPC service class inheriting `PhysicsHub.PhysicsHubBase`
- [X] T025 [US3] Create `src/PhysicsServer/Services/PhysicsHubService.fs` — implement `SendCommand` (accept command, route via MessageRouter, return ack), `SendViewCommand` (accept view command, route via MessageRouter, return ack), `StreamState` (register subscriber, send cached state first, then stream live updates)
- [X] T026 [US3] Create `src/PhysicsServer/Services/SimulationLinkService.fsi` — signature file declaring the gRPC service class inheriting `SimulationLink.SimulationLinkBase`
- [X] T027 [US3] Create `src/PhysicsServer/Services/SimulationLinkService.fs` — implement `ConnectSimulation` (check single-simulation lock, reject with ALREADY_EXISTS if occupied; otherwise read incoming state stream → update StateCache + fanout via MessageRouter, write outgoing command stream from MessageRouter command channel)
- [X] T028 [US3] Create `src/PhysicsServer/Program.fs` — configure host with `AddServiceDefaults()`, register gRPC services (PhysicsHubService, SimulationLinkService), register MessageRouter and StateCache as singletons, `MapGrpcService`, `MapDefaultEndpoints`, `app.Run()`
- [X] T029 [US3] Update `src/PhysicsSandbox.AppHost/Program.cs` — register PhysicsServer with `AddProject<Projects.PhysicsServer>("server")`, add AppHost project reference to PhysicsServer
- [X] T030 [US3] Verify all unit tests pass: `dotnet test tests/PhysicsServer.Tests`
- [X] T031 [US3] Verify all integration tests pass: `dotnet test tests/PhysicsSandbox.Integration.Tests`

**Checkpoint**: AppHost starts, dashboard shows server as healthy. gRPC client can send commands and receive acks. StreamState delivers cached state to late joiners. Second simulation connection is rejected. All tests green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T032 [P] Verify surface-area baseline: create baseline snapshot of PhysicsServer public API from `.fsi` files in `tests/PhysicsServer.Tests/` (constitution Principle V)
- [X] T033 [P] Validate quickstart.md: follow `specs/001-server-hub/quickstart.md` steps on a clean build and confirm all instructions work
- [X] T034 Run full solution build and all tests from repository root: `dotnet build PhysicsSandbox.sln && dotnet test PhysicsSandbox.sln`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (US1 — AppHost)**: Depends on Phase 1
- **Phase 3 (US2 — Contracts)**: Depends on Phase 1; can run in parallel with Phase 2
- **Phase 4 (US4 — ServiceDefaults)**: Depends on Phase 1; can run in parallel with Phases 2 and 3
- **Phase 5 (US3 — Server Hub)**: Depends on Phases 2, 3, and 4 (needs AppHost, Contracts, ServiceDefaults)
- **Phase 6 (Polish)**: Depends on Phase 5

### User Story Dependencies

- **US1 (AppHost)**: Independent — needs only the solution file
- **US2 (Contracts)**: Independent — needs only the solution file
- **US4 (ServiceDefaults)**: Independent — needs only the solution file
- **US3 (Server Hub)**: Depends on US1 (AppHost registration), US2 (proto contracts), US4 (service defaults reference)

### Within User Story 3

- Project stub T014 first (test projects need a reference target)
- Test projects (T015–T016) and test writing (T017–T019) before implementation (TDD)
- `.fsi` signature files before `.fs` implementations
- StateCache (T020–T021) before MessageRouter (T022–T023) — router depends on cache
- MessageRouter before gRPC services (T024–T027) — services delegate to router
- gRPC services before Program.fs (T028) — replaces stub from T014, host registers services
- Program.fs before AppHost registration (T029) — AppHost references the project
- Implementation before test verification (T030–T031)

### Parallel Opportunities

```text
After Phase 1 completes, three stories can run in parallel:
  ├── US1: T003 → T004 → T005 → T006
  ├── US2: T007 → T008 → T009 → T010
  └── US4: T011 → T012 → T013

After T014 (PhysicsServer stub), test project creation is parallel:
  ├── T015 (F# test project)
  └── T016 (C# integration test project)

Within US3, test writing is parallel:
  ├── T017 (StateCache tests)
  └── T018 (MessageRouter tests)
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US4 → US3)

1. Complete Phase 1: Setup (solution file)
2. Complete Phases 2–4 in parallel: AppHost, Contracts, ServiceDefaults
3. Complete Phase 5: Server Hub (the core deliverable)
4. **STOP and VALIDATE**: Run AppHost, verify dashboard, run all tests
5. Complete Phase 6: Polish

### Incremental Delivery

1. Setup → Solution exists, builds
2. US1 → AppHost runs, dashboard visible (demo: "orchestration works")
3. US2 → Contracts compile, stubs generated (demo: "API defined")
4. US4 → ServiceDefaults available (demo: "health checks work")
5. US3 → Server hub routes messages (demo: "hub accepts commands and streams state")
6. Polish → Baselines, quickstart validated, full test suite green

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution requires `.fsi` files for every public F# module — signature tasks precede implementation tasks
- Constitution requires test evidence — TDD within US3 (tests written before implementation)
- AppHost and ServiceDefaults are C# (justified exception documented in plan.md)
- Commit after each phase or logical group of tasks
