# E-Commerce Flash Sale Orchestrator — GitHub Copilot Instructions

## Project Context

This repository contains a .NET 10 event-driven flash-sale orchestration system.

The primary runtime flow is:

HTTP inventory decrease
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
→ HTTP query

Preserve the existing architecture unless a concrete defect justifies a change.

---

## Architectural Boundaries

### Domain

`ECommerce.FlashSaleOrchestrator.Domain`

- Must remain independent from Infrastructure, API, Worker, EF Core, Kafka, Redis, Semantic Kernel, HTTP, and ASP.NET Core.
- Contains domain entities, value objects, invariants, domain events, and domain exceptions.
- Do not introduce persistence or transport concerns into Domain.

### Application

`ECommerce.FlashSaleOrchestrator.Application`

- May depend on Domain.
- Contains use cases, commands, queries, handlers, application models, and abstractions/ports.
- Must not depend directly on EF Core, Kafka, Redis, Semantic Kernel, HTTP clients, or ASP.NET Core infrastructure.
- Do not introduce a mediator framework unless a concrete requirement justifies it.

### Infrastructure

`ECommerce.FlashSaleOrchestrator.Infrastructure`

- Implements Application abstractions.
- Owns EF Core persistence, SQL Server access, Kafka publishing, Redis semantic caching, embeddings, and LLM adapters.
- Technology-specific code must remain behind explicit application-facing abstractions.

### API

`ECommerce.FlashSaleOrchestrator.Api`

- Is a composition root and HTTP boundary.
- Owns API contracts, HTTP concerns, ProblemDetails mapping, correlation propagation, health endpoints, and API-specific configuration.
- Do not move API contracts into a separate Contracts project without a concrete cross-service versioning requirement.

### Worker

`ECommerce.FlashSaleOrchestrator.Worker`

- Is a composition root and Kafka consumer host.
- Owns message consumption, retry coordination, DLQ behavior, Worker health endpoints, and runtime composition.
- Preserve Kafka acknowledgement, retry, and DLQ semantics when refactoring.

---

## Critical Behavioral Contracts

### Transactional Outbox

- Inventory mutation and OutboxMessage persistence must remain atomic within the same database transaction.
- Do not publish Kafka events directly from the HTTP request as a replacement for the transactional outbox.
- Preserve safe handling of unpublished and retried outbox messages.

### Inbox and Idempotency

- Kafka processing is at-least-once.
- Inbox/idempotency protection must remain durable and database-backed.
- Business processing and Inbox state must retain their transaction guarantees.
- Do not replace durable duplicate handling with an in-memory mechanism.

### Kafka Consumer

- Preserve manual acknowledgement / offset commit semantics.
- Do not commit an offset before the message has reached its intended terminal state.
- Preserve poison-message and DLQ behavior.
- Preserve bounded message-level retry.

### Recommendation Pipeline

The conceptual pipeline is:

Resilient generator
→ semantic-cache decorator
→ timeout-controlled uncached generator
→ LLM implementation

with deterministic generation as the fallback strategy.

Preserve separation between:

- recommendation/LLM retry;
- integration-event retry.

Do not merge these failure domains into one generic retry policy.

### Candidate Grounding

- Alternative ProductIds must come only from the deterministic candidate set supplied by the application.
- The LLM must never invent or modify ProductIds.
- LLM output must continue to be validated against the supplied candidate set.
- Prompt instructions alone are not considered sufficient validation.

### Semantic Cache

- Redis is an optimization, not a core availability dependency.
- Redis/cache failures must bypass the cache path rather than stop recommendation processing.
- Preserve candidate fingerprint and compatibility/version checks used for safe cache reuse.

### LLM Resilience

- LLM provider availability is not Worker-readiness-critical.
- Preserve bounded timeout, retry, validation, and deterministic fallback behavior.
- Do not remove deterministic fallback as part of cleanup or simplification.

---

## Health Check Semantics

Preserve the following contracts:

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

Do not add Redis or the LLM to Worker readiness unless the architecture is explicitly changed.

---

## Database Migration Ownership

Database migrations have a single explicit owner.

- Normal API startup must not automatically run migrations.
- Normal Worker startup must not run migrations.
- Preserve the explicit one-shot migration mode and Compose migration service.
- Avoid competing migration runners.

---

## Refactoring Rules

For architecture or refactoring work:

1. Inspect the existing implementation before proposing changes.
2. Explain the concrete problem being solved.
3. Prefer the smallest behavior-preserving change.
4. Work on one explicitly approved logical batch at a time.
5. Do not implement later batches opportunistically.
6. Do not perform unrelated renames, folder moves, formatting, or cleanup.
7. Preserve public API and persisted-data contracts unless the task explicitly changes them.
8. Preserve current tests and architectural constraints.
9. Add or update tests when externally observable behavior changes.
10. Stop after the approved batch and report what changed.

Do not introduce a design pattern merely because it is available.

Prefer explicit, simple code over speculative abstraction.

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
- additional abstraction layers whose only purpose is wrapping another abstraction.

Existing focused abstractions should be preferred.

---

## Configuration and Secrets

- Never commit credentials, API keys, passwords, tokens, or connection-string secrets.
- Keep secrets in environment variables or appropriate secret stores.
- Preserve existing environment-variable compatibility when refactoring configuration unless explicitly approved otherwise.
- Prefer typed, validated configuration for sufficiently complex configuration groups.
- Do not silently change configuration defaults.

---

## Code Quality

- Favor clear responsibility boundaries and readable naming.
- Keep async operations cancellation-aware when appropriate.
- Preserve dependency-injection lifetime correctness.
- Avoid static global mutable state.
- Avoid unnecessary reflection and hidden runtime behavior.
- Do not optimize database or messaging paths without evidence or measurement when the change affects delivery guarantees.

A large class alone is not sufficient justification for decomposition. Split code only when responsibility separation becomes clearer and behavior can remain safely testable.

---

## Testing and Validation

After meaningful implementation changes:

- build the affected projects;
- run focused tests for the changed behavior;
- run the full solution regression suite before considering the batch complete;
- run `git diff --check`;
- inspect the final diff for accidental scope expansion.

Do not weaken or remove tests merely to make a refactoring pass.

Preserve architecture tests and integration tests that protect important runtime contracts.

---

## Git and Scope Discipline

- Do not commit directly to `main`.
- Use a focused branch for each logical change.
- Keep commits and pull requests narrowly scoped.
- Do not commit generated secrets, local configuration, build outputs, or temporary analysis files.
- Do not commit changes unless explicitly requested.

When asked to implement a named refactoring batch, implement only that batch and stop.