# Project Constitution — F# Microservices

## Core Principles

### I. Service Independence
Each service MUST be independently deployable. No shared mutable state
between services. Communication only via gRPC or HTTP APIs. Each service
owns its own data store; direct database sharing across services is
prohibited.

Rationale: Independent deployability is the defining property of a
microservice. Without it, you have a distributed monolith with all the
operational cost and none of the benefits.

### II. Contract-First
Every service boundary MUST be defined by a contract (proto file or OpenAPI
spec) before any implementation begins. Contracts live in
`Platform.Shared.Contracts` and are the only shared artifact between
services.

- **Real-time communication** MUST use gRPC. Proto files define the
  canonical interface.
- **Non-real-time communication** MUST use OpenAPI. An OpenAPI specification
  MUST be authored and versioned alongside the service.

Cross-service contracts MUST be versioned, reviewed, and treated as
first-class artifacts. Breaking changes MUST include migration guidance.

Rationale: Contracts are the public API of the system. Defining them before
implementation forces clarity about service boundaries and prevents
accidental coupling.

### III. Shared Nothing (Except Contracts)
Services MUST NOT reference each other's projects directly. The only
permitted cross-service dependency is `Platform.Shared.Contracts` (proto
files, DTOs, shared interfaces). For multi-repo scenarios, this project
is published as a NuGet package.

Rationale: Direct project references between services create compile-time
coupling that defeats independent deployment. Contracts-only sharing keeps
services loosely coupled while maintaining type safety.

### IV. Spec-First Delivery
Every non-trivial change MUST map to a current feature spec and
implementation plan before coding starts. Work items MUST remain traceable
from requirement to task to code. Implementation-only changes without
documented user value, acceptance criteria, and scope boundaries are
non-compliant.

Trivial changes (internal bug fixes with no public API impact, dependency
patch bumps, typo fixes) do not require a spec but MUST include a commit
message explaining the *why*.

Rationale: Spec-first execution reduces rework, keeps scope explicit, and
ensures decisions are reviewable.

### V. Compiler-Enforced Structural Contracts
Every public F# module MUST have a corresponding `.fsi` signature file that
declares its public API surface. The `.fsi` file serves as a structural
contract: the compiler MUST verify that the implementation (`.fs`) conforms
to its signature before the build succeeds. Any symbol omitted from the
`.fsi` file becomes module-private by design.

Surface area baselines MUST be maintained for public API modules. A
serialized snapshot of the public API surface MUST be stored as a baseline
file and validated by automated tests. Any divergence between actual API
surface and baseline MUST fail the build until the change is explicitly
reviewed and the baseline updated.

Rationale: F# signature files extend the spec-first philosophy into the
type system. The compiler becomes a structural quality gate that prevents
undocumented API drift, enforces encapsulation by default, and provides an
implicit implementation to-do list.

### VI. Test Evidence and Integration-First Testing
Behavior-changing code MUST include automated tests that fail before the
fix/feature and pass after implementation. Each user story MUST define
independent verification criteria and corresponding test coverage.
Unverified behavior changes MUST NOT be merged.

Prefer real databases and real service instances over mocks. Use Aspire's
`DistributedApplicationTestingBuilder` for cross-service integration tests.
Unit tests supplement but do not replace integration tests. Contract tests
MUST verify that services conform to their proto/OpenAPI contracts.

Rationale: Mandatory test evidence prevents regressions. Integration-first
testing catches the failures that matter most in distributed systems —
the ones at service boundaries that mocks silently hide.

### VII. Observability by Default
All services use `Platform.ServiceDefaults` for OpenTelemetry, health
checks, and structured logging. No service may opt out. Operationally
significant events (startup, subsystem failure, asset/IO failure, recovery
paths) MUST emit structured diagnostics with actionable context. Errors
MUST fail fast or degrade explicitly; silent failure and swallowed
exceptions are prohibited in critical paths.

Rationale: In a distributed system, observability is not optional. Correlated
traces, structured logs, and health checks are the only way to understand
cross-service behavior in production.

## Engineering Constraints

- **F# on .NET is the exclusive stack** within each service. Multi-language
  needs MUST be addressed by separate services communicating via gRPC or
  OpenAPI.
- Every public `.fs` module MUST have a curated `.fsi` signature file.
- Surface-area baseline files MUST exist for each public module.
- Public API changes MUST document compatibility impact and migration
  guidance.
- Dependencies MUST be minimized; each new dependency requires a stated
  need, version pinning strategy, and maintenance owner.
- Every dotnet project that produces a library MUST be packable via
  `dotnet pack`.
- gRPC services MUST use proto files or code-first contracts as the
  canonical interface definition.
- OpenAPI specs MUST be stored in the repository. Server endpoints MUST
  conform to the spec; clients MUST be generated from the spec.
- Cross-service communication MUST go through defined contracts — no
  backdoor project references, shared databases, or file-system coupling.

## Workflow and Quality Gates

1. **Specify** — produce the feature spec with testable user stories.
2. **Plan** — MUST pass Constitution Check gates before implementation
   begins. Plan MUST define `.fsi` signature contracts for new or changed
   public modules. For cross-service features, plan MUST identify affected
   contracts and services.
3. **Tasks** — produce story-grouped tasks including verification and `.fsi`
   tasks. Contract changes come first, then per-service implementation.
4. **Implement** — execute tasks phase-by-phase. TDD: tests before
   implementation code.
5. Pull requests MUST include: linked spec/plan/tasks, test evidence, and
   updated `.fsi`/surface-area baselines when public API surface changes.

Trivial changes (internal fixes, no public API impact) skip steps 1-3.
Implement directly, ensure tests pass, commit with a *why* message.

## Governance

This constitution is the authoritative governance source for F# microservice
projects that adopt it.

Amendment procedure:
- Propose changes via PR.
- Include rationale, migration impact.
- Approval requires maintainer review.

Versioning policy:
- MAJOR for incompatible governance changes or principle removals.
- MINOR for new principle/section additions or expanded obligations.
- PATCH for clarifications and wording refinements.

**Version**: 1.0.0
