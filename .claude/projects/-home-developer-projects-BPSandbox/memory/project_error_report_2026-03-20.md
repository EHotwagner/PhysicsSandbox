---
name: 2026-03-20 Demo Error Report
description: Findings from first demo run against live Aspire instance — all critical bugs now fixed in 005-mcp-server-testing
type: project
---

Error report from 2026-03-20 demo run archived to `.specify/archive/error-reports/2026-03-20-Error-Report.md`.

## All Issues Resolved

| # | Problem | Fixed In |
|---|---------|----------|
| 1 | Simulation SSL connection failure — `SimulationClient.fs` lacked SSL bypass | 005-mcp-server-testing |
| 2 | Empty state stream — consequence of #1 | Automatic with #1 |
| 3 | False disconnection from state stream | 005-mcp-server-testing (backoff retry in Session.fs) |
| 4 | Viewer DISPLAY env missing from Aspire | 005-mcp-server-testing (AppHost passes DISPLAY) |
| 5 | FSI script scoping | Consolidated into single-file runner |
| 6 | gRPC over plain HTTP | Use HTTPS endpoints (documented workaround) |
| 7 | F# namespace collisions | Qualify ambiguous calls (documented workaround) |

**Why:** These findings came from the first real end-to-end demo run and revealed integration gaps between specs 002 (simulation) and later specs.
**How to apply:** All critical issues resolved. Problems 6 and 7 are documented workarounds in CLAUDE.md's Known Issues section.
