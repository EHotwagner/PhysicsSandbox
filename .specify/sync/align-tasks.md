# Alignment Tasks

Generated: 2026-03-21
Source: Proposal 1 — 002-performance-diagnostics/FR-011 (ALIGN)

---

## Task 1: Add `ReportRenderTime` RPC to `PhysicsHub` proto

**Spec Requirement**: FR-011 — pipeline diagnostics must include viewer rendering stage
**Current Code**: `viewer_render_ms` field exists in `PipelineTimings` but is never populated
**Required Change**: Add a unary RPC for the viewer to report its frame render time

**Files to Modify**:
- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

**Details**:
Add to the `PhysicsHub` service:
```proto
rpc ReportRenderTime (RenderTimeReport) returns (CommandAck);
```

Add message:
```proto
message RenderTimeReport {
  double render_ms = 1;
}
```

**Estimated Effort**: small

### Acceptance Criteria
- [ ] `ReportRenderTime` RPC defined in `PhysicsHub` service
- [ ] `RenderTimeReport` message defined with `render_ms` field
- [ ] Proto compiles without errors

---

## Task 2: Handle `ReportRenderTime` on the server — cache latest value

**Spec Requirement**: FR-011
**Current Code**: `PhysicsHubService.GetMetrics` sets `ViewerRenderMs` to default 0.0
**Required Change**: Server caches the latest viewer render time and includes it in `PipelineTimings`

**Files to Modify**:
- `src/PhysicsServer/Hub/MessageRouter.fs` — add mutable field for cached render time
- `src/PhysicsServer/Hub/MessageRouter.fsi` — expose accessor and setter
- `src/PhysicsServer/Services/PhysicsHubService.fs` — implement `ReportRenderTime` override, use cached value in `GetMetrics`
- `src/PhysicsServer/Services/PhysicsHubService.fsi` — add override signature

**Details**:
- Add `mutable ViewerRenderMs: float` to `MessageRouter` record
- Add `setViewerRenderMs: MessageRouter -> float -> unit` and `getViewerRenderMs: MessageRouter -> float` functions
- In `PhysicsHubService.ReportRenderTime`: call `setViewerRenderMs router request.RenderMs`, return success ack
- In `PhysicsHubService.GetMetrics`: replace `timings.ViewerRenderMs` with `getViewerRenderMs router`

**Estimated Effort**: small

### Acceptance Criteria
- [ ] Server accepts `ReportRenderTime` RPC calls
- [ ] Latest render time cached in `MessageRouter`
- [ ] `GetMetrics` response includes actual `viewer_render_ms` value
- [ ] `total_pipeline_ms` calculation includes real render time

---

## Task 3: Viewer reports smoothed frame render time periodically

**Spec Requirement**: FR-011
**Current Code**: Viewer measures FPS via `FpsCounter` but never sends timing data to the server
**Required Change**: Viewer periodically calls `ReportRenderTime` with smoothed frame time

**Files to Modify**:
- `src/PhysicsViewer/Program.fs` — add periodic render time reporting

**Details**:
- The viewer already has `FpsCounter.SmoothedFps` which gives smoothed FPS. Render time in ms = `1000.0 / fps`.
- In the `start` function (where the metrics timer is already created at line 134), add a second periodic task or extend the existing timer to also call `client.ReportRenderTimeAsync(RenderTimeReport(RenderMs = renderMs))` every 10 seconds.
- Reuse the existing `createGrpcChannel`/`PhysicsHubClient` pattern. A persistent channel can be created once and reused (similar to the state stream channel, but for sending).
- Handle errors gracefully (log and continue if server is unreachable).

**Estimated Effort**: small

### Acceptance Criteria
- [ ] Viewer sends smoothed render time to server every 10 seconds
- [ ] Reporting uses the same server address resolution as existing streams
- [ ] Errors in reporting do not crash the viewer
- [ ] Render time reflects actual frame duration (derived from smoothed FPS)

---

## Task 4: Add tests for viewer render time pipeline

**Spec Requirement**: FR-011
**Current Code**: `DiagnosticsIntegrationTests` verifies non-zero tick and serialization times but not render time
**Required Change**: Add unit and integration tests verifying render time flows through the pipeline

**Files to Modify**:
- `tests/PhysicsServer.Tests/` — unit test: `ReportRenderTime` RPC caches value, `GetMetrics` returns it
- `tests/PhysicsSandbox.Integration.Tests/DiagnosticsIntegrationTests.cs` — integration test: verify `viewer_render_ms` is populated after viewer reports

**Details**:
- **Unit test**: Create a `MessageRouter`, call `setViewerRenderMs` with a known value, verify `getViewerRenderMs` returns it. Then verify `GetMetrics` includes it in `PipelineTimings.ViewerRenderMs`.
- **Integration test**: After AppHost is running with viewer, wait for viewer to report at least one render time sample, then call `GetMetrics` and assert `ViewerRenderMs > 0.0`.

**Estimated Effort**: small

### Acceptance Criteria
- [ ] Unit test verifies render time caching in MessageRouter
- [ ] Unit test verifies GetMetrics includes cached render time
- [ ] Integration test verifies end-to-end render time reporting
- [ ] All existing tests continue to pass
