# E-Commerce Flash Sale Orchestrator — Codex Repository Instructions

## Project Context

This repository contains a .NET 10 event-driven flash-sale orchestration system
with an Angular frontend.

The primary runtime flow is:

Angular catalog
→ HTTP inventory decrease
→ domain mutation
→ domain event
→ transactional outbox
→ Kafka / Redpanda
→ Worker consumer
→ Inbox / idempotency
→ deterministic candidate retrieval
→ semantic cache
→ LLM generation
→ validation
→ bounded retry / deterministic fallback
→ recommendation plan persistence
→ HTTP polling
→ Angular recommendation UI

Preserve the existing architecture unless a concrete defect or approved
milestone justifies a change.

---

## Repository Structure

Primary production projects:

- `ECommerce.FlashSaleOrchestrator.Domain`
- `ECommerce.FlashSaleOrchestrator.Application`
- `ECommerce.FlashSaleOrchestrator.Infrastructure`
- `ECommerce.FlashSaleOrchestrator.Api`
- `ECommerce.FlashSaleOrchestrator.Worker`

Primary test projects:

- `ECommerce.FlashSaleOrchestrator.Domain.Tests`
- `ECommerce.FlashSaleOrchestrator.Application.Tests`
- `ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests`
- `ECommerce.FlashSaleOrchestrator.Worker.Tests`
- `ECommerce.FlashSaleOrchestrator.Architecture.Tests`

Frontend:

- `frontend/flashsale-web`

There is intentionally no separate Contracts project.

---

## Architectural Boundaries

### Domain

`ECommerce.FlashSaleOrchestrator.Domain`

- Must remain independent from Infrastructure, API, Worker, EF Core, Kafka,
  Redis, Semantic Kernel, HTTP, and ASP.NET Core.
- Contains domain entities, value objects, invariants, domain events, and domain
  exceptions.
- Do not introduce persistence or transport concerns into Domain.

### Application

`ECommerce.FlashSaleOrchestrator.Application`

- May depend on Domain.
- Contains use cases, commands, queries, handlers, application models, and
  abstractions/ports.
- Must not depend directly on EF Core, Kafka, Redis, Semantic Kernel, HTTP
  clients, or ASP.NET Core infrastructure.
- Do not introduce a mediator framework unless a concrete requirement
  justifies it.

### Infrastructure

`ECommerce.FlashSaleOrchestrator.Infrastructure`

- Implements Application abstractions.
- Owns EF Core persistence, SQL Server access, Kafka publishing, Redis semantic
  caching, embeddings, and LLM adapters.
- Technology-specific code must remain behind explicit application-facing
  abstractions.

### API

`ECommerce.FlashSaleOrchestrator.Api`

- Is a composition root and HTTP boundary.
- Owns API contracts, HTTP concerns, ProblemDetails mapping, correlation
  propagation, health endpoints, and API-specific configuration.
- Do not move API contracts into a separate Contracts project without a
  concrete cross-service versioning requirement.

### Worker

`ECommerce.FlashSaleOrchestrator.Worker`

- Is a composition root and Kafka consumer host.
- Owns message consumption, retry coordination, DLQ behavior, Worker health
  endpoints, and runtime composition.
- Preserve Kafka acknowledgement, retry, and DLQ semantics when refactoring.

### Angular Frontend

`frontend/flashsale-web`

- Uses Angular standalone APIs.
- Prefer signals for local application state.
- Preserve strict TypeScript and strict template checking.
- Keep HTTP interaction behind focused API services.
- Keep orchestration/state behavior in focused stores rather than components.
- Components should primarily render state and emit user intent.
- Do not duplicate backend business rules in the frontend.
- Preserve the stable backend ProblemDetails contract.
- Preserve correlation IDs through the recommendation workflow.

---

## Critical Behavioral Contracts

### Transactional Outbox

- Inventory mutation and `OutboxMessage` persistence must remain atomic within
  the same database transaction.
- Do not publish Kafka events directly from the HTTP request as a replacement
  for the transactional outbox.
- Preserve safe handling of unpublished and retried outbox messages.

### Inbox and Idempotency

- Kafka processing is at-least-once.
- Inbox/idempotency protection must remain durable and database-backed.
- Business processing and Inbox state must retain their transaction guarantees.
- Do not replace durable duplicate handling with an in-memory mechanism.

### Kafka Consumer

- Preserve manual acknowledgement / offset commit semantics.
- Do not commit an offset before the message has reached its intended terminal
  state.
- Preserve poison-message and DLQ behavior.
- Preserve bounded message-level retry.

### Recommendation Pipeline

The conceptual pipeline is:

resilient generator
→ semantic-cache decorator
→ timeout-controlled uncached generator
→ LLM implementation

with deterministic generation as the fallback strategy.

Preserve separation between:

- recommendation / LLM retry;
- integration-event retry.

Do not merge these failure domains into one generic retry policy.

### Candidate Grounding

- Alternative ProductIds must come only from the deterministic candidate set
  supplied by the application.
- The LLM must never invent or modify ProductIds.
- LLM output must continue to be validated against the supplied candidate set.
- Prompt instructions alone are not sufficient validation.

### Semantic Cache

- Redis is an optimization, not a core availability dependency.
- Redis/cache failures must bypass the cache path rather than stop
  recommendation processing.
- Preserve candidate fingerprint and compatibility/version checks used for
  safe cache reuse.

### LLM Resilience

- LLM provider availability is not Worker-readiness-critical.
- Preserve bounded timeout, retry, validation, and deterministic fallback
  behavior.
- Do not remove deterministic fallback as part of cleanup or simplification.

### Recommendation Polling

- Poll recommendation plans by the workflow correlation ID.
- Preserve the same correlation ID in both the query parameter and
  `X-Correlation-ID` header.
- An HTTP 200 response with `plans: []` means the workflow is still pending.
- Polling must be bounded.
- Polling requests must not overlap.
- Retry must reuse the existing correlation ID and must not start another
  inventory mutation.

---

## Health Check Semantics

Preserve the following contracts.

### API readiness

Required:

- SQL Server

Not required:

- Redis
- LLM

### Worker readiness

Required:

- SQL Server
- Kafka broker
- source topic
- dead-letter topic

Not required:

- Redis
- LLM

Do not add Redis or the LLM to Worker readiness unless the architecture is
explicitly changed.

---

## Database Migration Ownership

Database migrations have a single explicit owner.

- Normal API startup must not automatically run migrations.
- Normal Worker startup must not run migrations.
- Preserve the explicit one-shot migration mode and Compose migration service.
- Avoid competing migration runners.

---

## Error Contract

Public HTTP errors use stable ProblemDetails responses.

Preserve stable public error semantics, including currently used codes such as:

- `validation-failed`
- `product-not-found`
- `inventory-item-not-found`
- `cart-not-found`
- `insufficient-stock`
- `inventory-concurrency-conflict`
- `invalid-request`
- `internal-server-error`

Do not expose internal exception messages as public API details.

Server-side logging may retain diagnostic exception information.

---

## Development Workflow

Before modifying code:

1. Inspect the relevant implementation and tests.
2. Inspect the current Git status and active branch.
3. Understand the existing contract before proposing a replacement.
4. Identify the cohesive milestone being changed.
5. Avoid speculative architecture changes outside that milestone.

For implementation:

- Work on one approved milestone or cohesive development batch at a time.
- Group directly related implementation, tests, CI, configuration, and
  documentation when they form one reviewable deliverable.
- Prefer substantial, cohesive work over micro changes.
- Do not implement unrelated future milestones opportunistically.
- Do not perform unrelated renames, folder moves, formatting, or cleanup.
- Preserve public API and persisted-data contracts unless the task explicitly
  changes them.
- Preserve current tests and architectural constraints.
- Add or update tests whenever externally observable behavior changes.
- Do not introduce abstractions only to make the design appear more layered.

---

## File Editing Rules

- Use normal file editing or patch mechanisms for source and configuration
  changes.
- Do not create or rewrite source/configuration files through shell redirection
  or generated terminal scripts unless explicitly requested.
- Shell commands are appropriate for:
  - build;
  - test;
  - Git;
  - Docker;
  - package management;
  - migrations;
  - runtime verification.
- Keep closely related source changes in one development batch.
- Do not scatter one cohesive task across unnecessary micro commits.

---

## Patterns to Avoid Without Concrete Need

Do not introduce these by default:

- mediator frameworks;
- generic repository abstractions;
- generic Result frameworks across all layers;
- generic event-bus frameworks;
- generalized pipeline frameworks;
- distributed transaction coordinators;
- workflow engines;
- a separate Contracts project;
- wrapper abstractions whose only purpose is wrapping another abstraction.

Prefer explicit, focused code.

---

## Configuration and Secrets

- Never commit credentials, API keys, passwords, tokens, or connection-string
  secrets.
- Keep secrets in environment variables or appropriate secret stores.
- Preserve existing environment-variable compatibility when refactoring
  configuration unless explicitly approved otherwise.
- Prefer typed, validated configuration for sufficiently complex configuration
  groups.
- Do not silently change configuration defaults.
- Never copy real local secret values into tests, documentation, examples, or
  commits.

---

## Code Quality

- Favor explicit responsibility boundaries and readable naming.
- Keep async operations cancellation-aware where appropriate.
- Preserve dependency-injection lifetime correctness.
- Avoid global mutable state.
- Avoid unnecessary reflection and hidden runtime behavior.
- Do not optimize database or messaging paths without evidence or measurement
  when the change affects delivery guarantees.
- A large class alone is not sufficient justification for decomposition.
- Split code when doing so produces a clearer responsibility boundary with
  independently testable behavior.

---

## Testing and Validation

After meaningful backend changes:

```text
dotnet restore ECommerce.FlashSaleOrchestrator.slnx
dotnet build ECommerce.FlashSaleOrchestrator.slnx
dotnet test ECommerce.FlashSaleOrchestrator.slnx