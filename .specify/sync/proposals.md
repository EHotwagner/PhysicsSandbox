# Drift Resolution Proposals

Generated: 2026-03-21T08:00:00Z
Based on: drift-report from 2026-03-21T08:00:00Z

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 1 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 001-mcp-persistent-service/US2-Scenario-2

**Direction**: BACKFILL

**Current State**:
- Spec says: "it sees the raw command that was sent (command type, parameters, sender)"
- Code does: "CommandEvent proto wraps SimulationCommand or ViewCommand with full type and parameters, but no sender field"

**Proposed Resolution**:

Update US2 Acceptance Scenario 2 from:

> **Given** a client sends a simulation command, **When** the AI assistant observes the command feed, **Then** it sees the raw command that was sent (command type, parameters, sender).

To:

> **Given** a client sends a simulation command, **When** the AI assistant observes the command feed, **Then** it sees the raw command that was sent (command type and parameters).

**Rationale**: The PhysicsServer routes all commands through a single `PhysicsHub` service without tracking which client originated each command. Adding sender identification would require protocol-level changes (client registration, session IDs) beyond the scope of this feature. The current implementation delivers the core value — observing what commands flow through the system. The "sender" detail was aspirational in the user story and was never part of the formal requirements (FR-004 does not mention sender).

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
