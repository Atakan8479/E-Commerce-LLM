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

Browser-level end-to-end tests:

- `frontend/flashsale-web/e2e`

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
```

Run focused tests first when useful, then the complete regression suite before
the milestone is considered complete.

After meaningful frontend changes, from:

`frontend/flashsale-web`

run:

```text
npm test
npm run test:coverage
npm run build
npm run audit:dependencies
```

The primary combined frontend verification command is:

```text
npm run verify
```

Current frontend coverage gates must not be weakened merely to make CI pass.

Do not remove, skip, or weaken tests solely to make a change succeed.

### Full-stack E2E Validation

Browser-level end-to-end tests live under:

`frontend/flashsale-web/e2e`

They exercise the real system boundary:

Angular
→ API
→ SQL Server
→ transactional outbox
→ Kafka / Redpanda
→ Worker
→ recommendation pipeline
→ recommendation persistence
→ Angular polling
→ rendered recommendation.

From `frontend/flashsale-web`, run:

```text
npm run e2e
```

E2E tests require the Docker-backed API, Worker, SQL Server, Redpanda, and
Redis services to be running.

The Playwright configuration owns the Angular development server used by the
browser tests. Do not require a separately started Angular server for the
normal E2E workflow.

The E2E database fixture must remain deterministic and isolated to the
dedicated E2E product IDs.

Keep E2E test execution serial while tests mutate the shared SQL fixture.
Do not increase Playwright workers or enable fully parallel execution unless
fixture isolation is redesigned.

Do not make E2E tests depend on a live external LLM provider.

The CI environment intentionally configures the LLM provider as unavailable
so the real recommendation pipeline exercises its deterministic fallback
behavior without requiring external credentials or provider availability.

E2E tests should continue to verify, where applicable:

- catalog retrieval from the real API;
- inventory mutation through the real API;
- stock depletion;
- asynchronous recommendation completion;
- recommendation rendering;
- recommendation source rendering;
- workflow correlation ID propagation in both the query parameter and
  `X-Correlation-ID` header.

Playwright traces, videos, screenshots, HTML reports, and test-results output
are generated diagnostics and must not be committed.

Always run:

```text
git diff --check
```

before considering a development milestone ready for commit.

Inspect the final diff for accidental scope expansion.

---

## CI Expectations

The repository CI validates backend, frontend, and browser-level full-stack
behavior independently.

### Backend CI

Backend CI covers:

- restore;
- Release build;
- integration dependencies;
- full regression;
- NuGet vulnerability auditing;
- Docker image build verification.

### Frontend CI

Frontend CI covers:

- deterministic `npm ci`;
- Angular/Vitest tests;
- coverage gates;
- production Angular build;
- npm dependency vulnerability auditing;
- coverage artifact generation.

Frontend unit/component CI must not depend on SQL Server, Kafka, Redis, or the
Worker.

### Full-stack E2E CI

Full-stack E2E CI covers:

- deterministic frontend dependency installation;
- Chromium installation;
- Docker-backed SQL Server startup;
- Redpanda startup and topic initialization;
- Redis startup;
- explicit one-shot database migration;
- API startup;
- Worker startup;
- API and Worker readiness;
- deterministic SQL fixture setup;
- real catalog retrieval;
- real inventory mutation;
- stock-depletion event propagation;
- transactional outbox processing;
- Kafka delivery;
- Worker recommendation processing;
- deterministic fallback when the external LLM provider is unavailable;
- recommendation persistence;
- Angular recommendation polling;
- rendered recommendation verification;
- workflow correlation propagation;
- Playwright traces, screenshots, and videos on failure;
- Docker service logs on failure;
- full Docker environment teardown.

The E2E CI environment must not require real LLM credentials or external LLM
availability.

Failure diagnostics should be retained as CI artifacts where practical.

---

## Git and Scope Discipline

- Do not commit directly to `main`.
- Use one branch for each coherent development milestone or independently
  reviewable feature area.
- Prefer milestone-sized commits and pull requests that group closely related
  implementation, tests, CI, configuration, and documentation.
- Avoid micro-commits or micro-PRs whose only purpose is changing one small file
  or implementation detail when those changes belong to the same deliverable.
- A larger commit must still have one understandable purpose and remain
  independently reviewable.
- Do not combine unrelated features, speculative refactoring, or future roadmap
  work into the active branch.
- Do not commit generated secrets, local configuration, build outputs, coverage
  output, Playwright reports, test results, or temporary analysis files.
- Do not commit changes unless explicitly requested.
- Do not amend existing commits unless explicitly requested.

When working on a named milestone, complete the approved cohesive milestone and
stop before unrelated roadmap work.

---

## Definition of Done

A milestone is complete only when applicable checks have passed and the final
diff has been reviewed.

At minimum:

- affected code builds;
- focused tests pass;
- relevant full regression passes;
- required frontend coverage gates pass;
- browser-level E2E tests pass when the milestone affects the integrated flow;
- dependency vulnerability gates pass where applicable;
- `git diff --check` is clean;
- generated outputs are not accidentally staged;
- secrets are not present;
- working behavior matches the intended contract;
- documentation or CI is updated when the milestone changes developer or
  operational behavior.

Do not claim a command or test passed unless its output was actually observed.
