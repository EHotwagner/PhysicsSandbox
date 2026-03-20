# Proto Contract Extension: StreamViewCommands

## Change to `physics_hub.proto`

Add one new RPC to the existing `PhysicsHub` service:

```protobuf
service PhysicsHub {
  // ... existing RPCs unchanged ...

  // Server streams view commands to the viewer.
  // Viewer subscribes to receive camera, wireframe, and zoom commands
  // forwarded from the client via SendViewCommand.
  rpc StreamViewCommands (StateRequest) returns (stream ViewCommand);
}
```

**No new messages needed** — `StateRequest` (empty, used for subscription) and `ViewCommand` (with SetCamera, ToggleWireframe, SetZoom oneof) already exist.

## Compatibility

- **Wire-compatible**: Adding a new RPC to an existing service is a non-breaking proto change
- **Existing clients unaffected**: StreamState, SendCommand, SendViewCommand unchanged
- **Server must implement**: new override in PhysicsHubService

## Server-Side Changes Required

### MessageRouter.fsi — add `readViewCommand`

```fsharp
/// Read a pending view command. Blocks until one is available or cancellation.
val readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>
```

Implementation mirrors existing `readCommand` — reads from `ViewCommandChannel.Reader`.

### PhysicsHubService — add `StreamViewCommands` override

```fsharp
override StreamViewCommands:
    request: StateRequest *
    responseStream: IServerStreamWriter<ViewCommand> *
    context: ServerCallContext ->
        System.Threading.Tasks.Task
```

Implementation mirrors `StreamState` — loops reading from `readViewCommand` and writing to responseStream until cancellation.
