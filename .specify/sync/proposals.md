# Drift Resolution Proposals

Generated: 2026-03-21T12:00:00Z
Based on: drift-report from 2026-03-21T12:00:00Z

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 0 |
| Align (Spec → Code) | 1 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 002-performance-diagnostics/FR-011

**Direction**: ALIGN (Spec → Code)

**Current State**:
- Spec says: "System MUST provide pipeline diagnostics showing time breakdown across simulation, serialization, transfer, and rendering stages, accessible both via structured logs and on-demand MCP tools."
- Code does: Simulation tick, serialization, and gRPC transfer times are measured and reported correctly. The `viewer_render_ms` field exists in the `PipelineTimings` proto message but is never populated — it remains 0.0. The viewer has access to per-frame timing via Stride3D's `GameTime.Elapsed` but has no mechanism to report it back to the server.

**Context**:
- The implementation task (T065) explicitly noted: "leave `viewer_render_ms` as reported by viewer (or 0 if not available)" — this was a known gap at implementation time, not a bug.
- The proto field `viewer_render_ms` (field 4) already exists in `PipelineTimings`.
- The MCP tools (`get_metrics`, `get_diagnostics`) already display and format `ViewerRenderMs` — they just show 0.0.
- The `get_diagnostics` tool marks the "slowest stage" — with render always at 0.0, this misidentifies the bottleneck when the viewer is actually the slowest stage.

**Proposed Resolution**:

Implement viewer render time reporting. The viewer already measures frame time internally; it needs to send this back to the server. Two approaches:

**Option A — Viewer reports via a lightweight RPC** (recommended):
1. Add a `ReportRenderTime` unary RPC to `PhysicsHub` (or a new `ViewerMetrics` service) accepting a `double render_ms`.
2. The viewer calls this periodically (e.g., every 10 seconds with a smoothed average frame time).
3. The server caches the latest value and uses it when building `PipelineTimings`.

**Option B — Piggyback on existing view command stream**:
1. Add a `render_ms` field to an existing viewer→server message.
2. The viewer includes its latest frame time in each message.
3. Lower fidelity but avoids a new RPC.

**Estimated scope**: ~4 tasks (proto change, server handler, viewer reporting, update tests).

**Rationale**: The spec explicitly requires all four pipeline stages. The proto field already exists, the MCP tools already display it, and the diagnostics bottleneck detection depends on it. This is a gap that was deferred during implementation, not a deliberate design change. Aligning code to spec completes the diagnostics feature as designed.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
