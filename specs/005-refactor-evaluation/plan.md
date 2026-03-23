# Implementation Plan: Refactor Evaluation Analysis

**Branch**: `005-refactor-evaluation` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-refactor-evaluation/spec.md`

## Summary

Produce a comprehensive refactor evaluation report (`reports/refactorEvaluation.md`) that analyses all 9 source projects, 6 test projects, and 2 scripting layers for code quality, duplication, complexity, and architectural issues. The report evaluates 4 alternative approaches (targeted refactor, major refactor, full rewrite, partial rewrite) and delivers a prioritized 13-item refactoring roadmap. The recommended approach (Alternative A: Targeted Refactoring) requires 5-6 days for the top 5 priorities and would raise solution quality from 7.4/10 to 8.5+/10.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, MCP, client, scripting), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts), Python 3.10+ (demo scripts)
**Primary Dependencies**: .NET Aspire 13.1.3, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console, xUnit 2.x
**Storage**: In-memory (physics world, shape cache, constraint registry, metrics counters)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (275 tests across 6 projects)
**Target Platform**: Linux with GPU passthrough (container)
**Project Type**: Distributed physics simulation platform (gRPC microservices + 3D viewer + MCP server)
**Performance Goals**: N/A for evaluation report; existing system targets 60 FPS viewer, real-time physics stepping
**Constraints**: No breaking changes to proto contracts or public APIs during refactoring
**Scale/Scope**: ~8,657 LOC source, ~4,816 LOC tests, ~4,667 LOC scripting, 81 proto messages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Service Independence** | PASS | Report is a standalone artifact; no cross-service coupling introduced |
| **II. Contract-First** | PASS | No new contracts needed — this is an analysis deliverable |
| **III. Shared Nothing** | PASS | No new project references or shared dependencies |
| **IV. Spec-First Delivery** | PASS | Spec created before analysis; plan documents approach |
| **V. Compiler-Enforced Structural Contracts** | N/A | No new F# modules with public APIs |
| **VI. Test Evidence** | N/A | Report deliverable, not behavior-changing code |
| **VII. Observability by Default** | N/A | No runtime services modified |

All gates pass. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/005-refactor-evaluation/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: Analysis methodology and findings
├── data-model.md        # Phase 1: Quality model and rating criteria
├── quickstart.md        # Phase 1: How to use the report
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
reports/
└── refactorEvaluation.md    # Primary deliverable — comprehensive evaluation report
```

**Structure Decision**: Single report file in a `reports/` directory at repository root. No source code changes — this feature produces a documentation artifact, not code modifications.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
