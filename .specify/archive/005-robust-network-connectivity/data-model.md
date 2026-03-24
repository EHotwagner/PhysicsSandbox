# Data Model: 005-robust-network-connectivity

**Date**: 2026-03-24

## Entities

### ViewCommandSubscriber (server-side, new)

- `Id: Guid` — Unique subscriber identifier, created on gRPC stream connection
- `Channel: Channel<ViewCommand>` — Bounded channel (capacity 1024) for this subscriber's command queue
- Lifecycle: Created when `StreamViewCommands` RPC connects. Removed when RPC disconnects (cancellation token fires).

### MessageRouter (server-side, modified)

Existing fields (unchanged):
- `Subscribers: ConcurrentDictionary<Guid, TickState -> Task>`
- `PropertySubscribers: ConcurrentDictionary<Guid, PropertyEvent -> Task>`
- `CommandSubscribers: ConcurrentDictionary<Guid, CommandEvent -> Task>`
- `Metrics: ServiceMetrics`
- `StateCache: StateCache`
- `MeshCache: MeshCache`

Removed fields:
- ~~`ViewCommandChannel: Channel<ViewCommand>`~~ — Replaced by subscriber registry

New fields:
- `ViewCommandSubscribers: ConcurrentDictionary<Guid, Channel<ViewCommand>>` — Per-subscriber bounded channels for broadcast delivery

### NetworkProblemEntry (documentation, existing format)

- **Context**: What the developer was doing
- **Error**: Actual error message or log output
- **Root Cause**: What caused it (confirmed)
- **Hypothesis**: If root cause unknown, what is suspected
- **Resolution**: What fixed it
- **Prevention**: How to avoid in the future

### ContainerEnvironment (documentation, new section in NetworkProblems.md)

- **Runtime**: Podman (rootless)
- **Exposed Ports**: Table of port → typical use mapping
- **Networking Boundary**: All services internal (localhost), only Aspire dashboard external
- **Dashboard Port**: 18888 (pending container rebuild)

## State Transitions

### ViewCommandSubscriber Lifecycle

```
[StreamViewCommands RPC connects]
  → Create bounded Channel<ViewCommand>(1024)
  → Register in ViewCommandSubscribers with new Guid
  → Read loop: Channel.Reader.ReadAsync → responseStream.WriteAsync

[RPC cancellation / client disconnect]
  → Unregister from ViewCommandSubscribers
  → Channel disposed (GC)
```

### ViewCommand Publish Flow (new)

```
submitViewCommand(cmd) called
  → For each (guid, channel) in ViewCommandSubscribers:
      → channel.Writer.TryWrite(cmd)
      → If TryWrite returns false (channel full): skip (backpressure)
  → Publish to CommandSubscribers (audit stream, unchanged)
  → Return CommandAck
```

## Validation Rules

- ViewCommandSubscriber channel capacity: 1024 (matching existing channel capacity)
- Zero subscribers: commands silently dropped, no error
- Subscriber disconnection during publish: `TryWrite` on disposed channel returns false; catch and unregister
- NetworkProblems.md entries: must include all 6 structured fields
