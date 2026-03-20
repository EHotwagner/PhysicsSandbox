# Research: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20

## R1: Physics Engine — BepuFSharp

**Decision**: Use BepuFSharp (idiomatic F# wrapper for BepuPhysics2) via local NuGet package.

**Rationale**: BepuFSharp is already built, tested, and cloned as a sister repo at `/home/developer/projects/BPEWrapper`. It provides:
- `PhysicsWorld.create/step/destroy` lifecycle
- `PhysicsWorld.addBody/addStatic/removeBody` body management
- `PhysicsWorld.applyForce/applyLinearImpulse/applyAngularImpulse/applyTorque` force API
- `PhysicsWorld.getBodyPose/readPoses/readVelocities` state readout
- Full angular velocity/orientation support via `Velocity.Angular` and `Pose.Orientation`
- 8 shape types (Sphere, Box, Capsule, Cylinder, Triangle, ConvexHull, Compound, Mesh)
- `.fsi` signature files for all public modules (constitution compliant)
- Packable via `dotnet pack` → output to `~/.local/share/nuget-local/`

**Package**: `BepuFSharp` version 0.1.0, targeting .NET 10.0.

**Alternatives considered**:
- Hand-rolled Euler integrator: simpler but no collision support for future features, no angular dynamics
- Direct BepuPhysics2 reference: works but non-idiomatic F# API with mutable struct handles

## R2: gRPC Approach — Contract-First with fsGRPC Skills

**Decision**: Extend existing `.proto` contract in `PhysicsSandbox.Shared.Contracts`. Use installed fsGRPC skills (`/fsgrpc-proto`, `/fsgrpc-client`) for all gRPC work.

**Rationale**: The project already uses contract-first with `Grpc.Tools` code generation. The simulation service is a gRPC **client** connecting to the server's `SimulationLink` bidirectional stream. The `/fsgrpc-client` skill provides patterns for bidirectional streaming in F#.

**Alternatives considered**:
- Code-first with protobuf-net: would require rewriting existing contracts; breaks consistency
- New proto service: unnecessary; existing `SimulationLink.ConnectSimulation` is exactly the right interface

## R3: Proto Contract Extensions Needed

**Decision**: Extend the existing `physics_hub.proto` with new command types and body fields.

**New SimulationCommand variants** (appended to existing oneof):
- `RemoveBody` (field 6): body_id string
- `ApplyImpulse` (field 7): body_id string, impulse Vec3
- `ApplyTorque` (field 8): body_id string, torque Vec3
- `ClearForces` (field 9): body_id string

**Extended Body message**:
- `angular_velocity` (field 6): Vec3
- `orientation` (field 7): Vec4 (quaternion as x,y,z,w)

**New message**: `Vec4` for quaternion representation.

**Backward compatibility**: All additions are new fields with higher field numbers. Existing clients/server continue to work; new fields are zero-valued for old messages.

## R4: BepuFSharp NuGet Packaging

**Decision**: Pack BepuFSharp to local NuGet feed and reference from PhysicsSimulation project.

**Steps**:
1. Run `dotnet pack` in `/home/developer/projects/BPEWrapper/BepuFSharp/`
2. Output goes to `~/.local/share/nuget-local/` (configured in `Directory.Build.props`)
3. Add NuGet source in `NuGet.config`: `<add key="local" value="~/.local/share/nuget-local/" />`
4. Reference `BepuFSharp` package in PhysicsSimulation `.fsproj`

**Rationale**: Local NuGet feed follows the constitution's principle that every library must be packable. Avoids cross-repo project references (which would violate service independence).

## R5: Simulation Architecture

**Decision**: PhysicsSimulation is a background worker service (not a web server) that acts as a gRPC client to the PhysicsServer.

**Architecture**:
- On startup: create `PhysicsWorld`, connect to server via `SimulationLink.ConnectSimulation`
- Receive commands from the server's response stream (server → simulation direction)
- Process commands against the BepuFSharp world
- When playing: run a fixed-timestep loop (~60Hz), step the world, stream state
- When paused: only step on explicit `StepSimulation` commands
- On server disconnect: log and shut down cleanly
- State streaming: after every step, read all body poses/velocities, convert to proto `SimulationState`, send via request stream

**Command dispatch**: Each `SimulationCommand` variant maps to a BepuFSharp function call. A pure command handler module processes commands against the world. Force persistence is managed by a `Map<string, Vec3>` of active forces per body, applied each step before `PhysicsWorld.step`.

## R6: Gravity Runtime Update

**Decision**: BepuFSharp creates the world with `PhysicsConfig.Gravity`. To change gravity at runtime, the wrapper likely exposes a setter or the config can be mutated. Need to verify; if not exposed, we add a gravity override tracked locally and applied as a force to all bodies each step.

**Fallback**: Apply gravity as `mass * gravity_vector` force to each body per step. This is physically equivalent and avoids needing to modify BepuFSharp internals.

## R7: Aspire Registration

**Decision**: Register PhysicsSimulation in AppHost with `WithReference(server).WaitFor(server)`, matching the planned architecture from PhysicsSandbox.md.

**Configuration**: Set `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` on the server (already done). The simulation service only needs the server address via Aspire service discovery (`https+http://server`).
