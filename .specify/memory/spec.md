# PhysicsSandbox — Main Specification

**Last Updated**: 2026-03-20
**Revision**: Bootstrapped from first feature archival (001-server-hub)

## Overview

PhysicsSandbox is an Aspire-orchestrated distributed application for real-time physics simulation. Four services communicate through a central server hub via gRPC. Each service is added incrementally as a feature spec.

## User Stories

### US-001: Solution Foundation and Orchestrator (P1)
A developer runs the Aspire AppHost to launch the orchestration dashboard, confirming the foundation is operational. [Source: specs/001-server-hub]

### US-002: Shared Communication Contracts (P1)
A developer defines shared gRPC message contracts (SimulationCommand, ViewCommand, SimulationState) in a contracts project so all services agree on message structure. [Source: specs/001-server-hub]

### US-003: Server Hub with Message Routing (P1)
A developer creates the PhysicsServer — the central hub that accepts commands from clients, receives simulation state, caches latest state for late joiners, and fans out updates to all subscribers. Only one simulation source at a time. [Source: specs/001-server-hub]

### US-004: Shared Service Defaults (P2)
A developer sets up standardized health checks, OpenTelemetry, service discovery, and resilience patterns via a shared ServiceDefaults project. [Source: specs/001-server-hub]

## Functional Requirements

- **FR-001**: Solution structure with Aspire AppHost, shared contracts, service defaults, and server hub. [Source: specs/001-server-hub]
- **FR-002**: Contracts define `PhysicsHub` service (SendCommand, SendViewCommand, StreamState) and `SimulationLink` service (ConnectSimulation bidirectional stream). [Source: specs/001-server-hub]
- **FR-003**: `SimulationCommand` with variants: AddBody, ApplyForce, SetGravity, StepSimulation, PlayPause. [Source: specs/001-server-hub]
- **FR-004**: `ViewCommand` with variants: SetCamera, ToggleWireframe, SetZoom. [Source: specs/001-server-hub]
- **FR-005**: `SimulationState` with bodies (id, position, velocity, mass, shape), time, running flag. [Source: specs/001-server-hub]
- **FR-006**: Server hub accepts simulation commands and forwards to simulation. [Source: specs/001-server-hub]
- **FR-007**: Server hub accepts view commands and forwards to viewer. [Source: specs/001-server-hub]
- **FR-008**: Server hub fans out simulation state to all subscribers. [Source: specs/001-server-hub]
- **FR-009**: Graceful handling when downstream services not connected (ack without error, drop commands). [Source: specs/001-server-hub]
- **FR-010**: AppHost registers server hub as first service. [Source: specs/001-server-hub]
- **FR-011**: ServiceDefaults provides health endpoints, OpenTelemetry, service discovery, resilience. [Source: specs/001-server-hub]
- **FR-012**: Server hub references ServiceDefaults for health and observability. [Source: specs/001-server-hub]
- **FR-013**: Server hub caches latest state and delivers it immediately to late-joining subscribers. [Source: specs/001-server-hub]
- **FR-014**: Single simulation source enforcement — reject second connection with ALREADY_EXISTS. [Source: specs/001-server-hub]

## Key Entities

- **SimulationCommand**: User command to control physics (add body, apply force, set gravity, step, play/pause). [Source: specs/001-server-hub]
- **ViewCommand**: User command to control 3D viewer (camera, wireframe, zoom). [Source: specs/001-server-hub]
- **SimulationState**: Snapshot of physics world — bodies, time, running flag. [Source: specs/001-server-hub]
- **Body**: Physical object — id, Vec3 position, Vec3 velocity, mass, Shape. [Source: specs/001-server-hub]
- **Vec3**: 3D vector (x, y, z doubles). [Source: specs/001-server-hub]
- **Shape**: Geometric descriptor — Sphere (radius), Box (half_extents), Plane (normal). [Source: specs/001-server-hub]
- **CommandAck**: Acknowledgment with success flag and message. [Source: specs/001-server-hub]
- **PhysicsHub**: Client/viewer-facing gRPC service. [Source: specs/001-server-hub]
- **SimulationLink**: Simulation-facing gRPC service (bidirectional streaming). [Source: specs/001-server-hub]

## Edge Cases

- Commands with no simulation connected: acknowledged, dropped gracefully. [Source: specs/001-server-hub]
- No state subscribers: state from simulation accepted but discarded. [Source: specs/001-server-hub]
- Server shutdown during streaming: streams terminated cleanly. [Source: specs/001-server-hub]
- Malformed commands: rejected with descriptive error. [Source: specs/001-server-hub]
- Second simulation connection: rejected with ALREADY_EXISTS. [Source: specs/001-server-hub]

## Success Criteria

- **SC-001**: Clone, build, and run AppHost in under 2 minutes. [Source: specs/001-server-hub]
- **SC-002**: Dashboard shows server hub as healthy in Development mode. [Source: specs/001-server-hub]
- **SC-003**: Command acknowledgment within 1 second. [Source: specs/001-server-hub]
- **SC-004**: State stream updates with <100ms latency. [Source: specs/001-server-hub]
- **SC-005**: Health check endpoints (/health, /alive) operational. [Source: specs/001-server-hub]
- **SC-006**: Contracts buildable by any referencing project. [Source: specs/001-server-hub]
- **SC-007**: No errors when sending commands with no downstream. [Source: specs/001-server-hub]
