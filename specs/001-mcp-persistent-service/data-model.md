# Data Model: MCP Persistent Service

**Date**: 2026-03-21 | **Feature**: 001-mcp-persistent-service

## Entities

### CommandEvent (New Proto Message)

Wraps any command passing through the server for the audit stream.

| Field | Type | Description |
|-------|------|-------------|
| simulation_command | SimulationCommand (oneof) | A simulation command, if this event is a simulation command |
| view_command | ViewCommand (oneof) | A view command, if this event is a view command |

### GrpcConnection (Modified — MCP Singleton)

Shared connection state for all MCP sessions.

| Field | Type | Description |
|-------|------|-------------|
| Channel | GrpcChannel | Single gRPC channel to PhysicsServer |
| Client | PhysicsHub.PhysicsHubClient | gRPC client stub |
| LatestState | SimulationState option | Most recent simulation state |
| LastStateUpdate | DateTime | Timestamp of last state update |
| CommandLog | CommandEvent list | Bounded circular buffer of recent commands (most recent N) |
| IsConnected | bool | Whether the gRPC channel is healthy |
| ServerAddress | string | Resolved server address |

### MessageRouter (Modified — PhysicsServer)

Extended with command audit subscriber support.

| Field | Type | Description |
|-------|------|-------------|
| CommandSubscribers | ConcurrentDictionary<Guid, CommandEvent -> Task> | Audit stream subscribers |
| *(existing fields unchanged)* | | |

## State Transitions

### MCP Server Lifecycle

```
Starting → Running (no clients) → Running (with clients) → Running (no clients) → ...
                                                                    ↓
                                                              Shutting Down (AppHost stops)
```

- MCP server transitions freely between "with clients" and "no clients" states
- Only shuts down when the AppHost itself stops

### GrpcConnection Lifecycle

```
Disconnected → Connecting → Connected → Streaming
                   ↑                        ↓
                   └──── Reconnecting ←─────┘ (on stream error, exponential backoff)
```

- Three independent stream subscriptions (state, view commands, audit commands)
- Each stream reconnects independently on failure

## Relationships

- **MCP Server** 1 ←→ 1 **GrpcConnection** (singleton, shared across all sessions)
- **MCP Server** 1 ←→ N **MCP Sessions** (multiple AI assistants)
- **GrpcConnection** 1 ←→ 1 **PhysicsServer** (single upstream connection)
- **PhysicsServer.MessageRouter** 1 ←→ N **CommandSubscribers** (audit stream fan-out)
