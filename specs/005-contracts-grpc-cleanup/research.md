# Research: Contracts gRPC Package Cleanup

**Feature**: 005-contracts-grpc-cleanup
**Date**: 2026-03-27

## Research Summary

No unknowns or NEEDS CLARIFICATION items exist in this feature. Research confirms the change is safe.

## R1: Grpc.AspNetCore vs Grpc.Net.Client Package Contents

**Decision**: Replace `Grpc.AspNetCore` with `Grpc.Net.Client` in Shared.Contracts.

**Rationale**:
- `Grpc.AspNetCore` is a metapackage that bundles: `Grpc.AspNetCore.Server`, `Grpc.Net.Client`, `Grpc.Net.ClientFactory`, and transitively pulls in the ASP.NET Core framework.
- `Grpc.Net.Client` provides only the client-side gRPC channel and call types — exactly what generated proto client stubs need.
- `Grpc.Tools` (already a direct dependency with `PrivateAssets="All"`) drives proto compilation and is unaffected by this change.
- The `GrpcServices="Both"` Protobuf item setting generates both client and server stubs. Server stub generation is controlled by `Grpc.Tools`, not by runtime package references. The generated server base class types come from `Grpc.Core.Api` (a transitive dependency of `Grpc.Net.Client`), so server stubs will still compile.

**Alternatives considered**:
- Keep `Grpc.AspNetCore` — rejected: unnecessarily broad, pulls ASP.NET server into a library project.
- Use only `Grpc.Tools` + `Google.Protobuf` (no `Grpc.Net.Client`) — rejected: generated client stubs reference `Grpc.Net.Client` types (`GrpcChannel`, `CallInvoker`).

## R2: Downstream Impact Analysis

**Decision**: No downstream projects need changes.

**Rationale**:
- 7 projects reference Shared.Contracts (PhysicsServer, PhysicsSimulation, PhysicsViewer, PhysicsClient, PhysicsSandbox.Mcp, PhysicsClient.Tests, Integration.Tests).
- PhysicsServer already has its own direct `Grpc.AspNetCore.Server` reference — it does not rely on Contracts for server types.
- All other downstream projects are gRPC *clients* — they need `Grpc.Net.Client` types, which will now flow transitively from Contracts (same as before, since `Grpc.AspNetCore` bundled `Grpc.Net.Client`).
- No project uses ASP.NET Core server types transitively through Contracts.

**Alternatives considered**: N/A — the analysis is clear.
