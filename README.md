# E-Commerce-LLM

LLM-assisted flash-sale orchestration system built with .NET 10, SQL Server, Redpanda/Kafka, Redis, and an OpenAI-compatible LLM endpoint.

The project focuses on a common flash-sale failure scenario: a product becomes unavailable while customers are actively interacting with limited inventory. Instead of stopping at stock depletion, the system asynchronously creates an alternative recommendation plan using only valid in-stock candidates.

The architecture combines transactional consistency, event-driven processing, idempotent message consumption, semantic caching, resilient LLM integration, and containerized local execution.

## Core Flow

```text
Client
  |
  v
ASP.NET Core API
  |
  | inventory decrease
  v
SQL Server
  |
  | same transaction
  +--> Inventory update
  |
  +--> Transactional Outbox
          |
          v
     Outbox Publisher
          |
          v
   Redpanda / Kafka
          |
          v
        Worker
          |
          +--> Inbox / idempotency
          |
          +--> Candidate retrieval
          |
          +--> Semantic cache lookup
          |       |
          |       +--> Redis cache hit
          |
          +--> LLM generation on cache miss
          |       |
          |       +--> validation
          |       +--> timeout / retry
          |       +--> deterministic fallback
          |
          v
Alternative Recommendation Plan
          |
          v
      SQL Server
```

## Main Capabilities

The current implementation includes:

- inventory-aware flash-sale processing
- Domain Events
- Transactional Outbox
- Kafka-compatible asynchronous messaging through Redpanda
- bounded retry and Dead Letter Queue support
- Inbox-based consumer idempotency
- correlation ID propagation
- deterministic alternative-candidate retrieval
- OpenAI-compatible LLM integration
- strict recommendation validation against supplied candidates
- deterministic fallback when LLM execution is unavailable
- Redis-backed semantic recommendation caching
- persisted alternative recommendation plans
- structured logging
- application metrics
- SQL Server retry handling
- Redis and LLM timeout handling
- liveness and readiness health checks
- Docker-based local orchestration
- automated database migration ownership
- GitHub Actions CI configuration
- architecture, domain, application, worker, and infrastructure integration tests
- NuGet vulnerability auditing
- Docker image build verification

## Technology Stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 10 |
| API | ASP.NET Core |
| Persistence | Entity Framework Core |
| Database | SQL Server |
| Messaging | Redpanda / Kafka |
| Cache | Redis |
| LLM integration | OpenAI-compatible endpoint |
| AI orchestration | Semantic Kernel |
| Containers | Docker / Docker Compose |
| Testing | xUnit |
| CI | GitHub Actions |

## Solution Structure

```text
src/
├── ECommerce.FlashSaleOrchestrator.Api
├── ECommerce.FlashSaleOrchestrator.Application
├── ECommerce.FlashSaleOrchestrator.Domain
├── ECommerce.FlashSaleOrchestrator.Infrastructure
└── ECommerce.FlashSaleOrchestrator.Worker

tests/
├── ECommerce.FlashSaleOrchestrator.Application.Tests
├── ECommerce.FlashSaleOrchestrator.Architecture.Tests
├── ECommerce.FlashSaleOrchestrator.Domain.Tests
├── ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests
└── ECommerce.FlashSaleOrchestrator.Worker.Tests
```

The dependency direction follows the application boundaries:

```text
Domain
  ^
  |
Application
  ^
  |
Infrastructure
  ^
  |
API / Worker
```

The Domain layer remains independent from infrastructure and AI-specific implementation concerns.

## Event-Driven Recommendation Flow

When an inventory decrease depletes a product:

1. the inventory state is updated;
2. a `StockDepletedDomainEvent` is generated;
3. the corresponding outbox message is persisted in the same SQL transaction;
4. the outbox publisher publishes the integration event to Redpanda;
5. the Worker consumes the event;
6. Inbox processing prevents duplicate execution;
7. valid in-stock alternatives are retrieved deterministically;
8. semantic cache lookup is performed;
9. a compatible Redis cache hit can directly provide the recommendation;
10. otherwise the LLM receives only the supplied candidate set;
11. the generated result is validated;
12. resilient fallback behavior protects the flow from LLM failures;
13. the final recommendation plan is persisted and can be queried through the API.

The LLM is therefore not allowed to invent arbitrary product identifiers. Recommendations must reference products from the candidate set supplied by the application.

## Resilience Model

The recommendation pipeline is designed so that optional AI infrastructure does not become a hard dependency for the core application.

### SQL Server

SQL Server is the authoritative persistence layer and is required for application readiness.

### Redpanda / Kafka

The Worker requires access to:

- the broker;
- the source topic;
- the Dead Letter Queue topic.

These dependencies participate in Worker readiness.

### Redis

Redis accelerates repeated recommendation scenarios through semantic caching.

Cache failures do not make the application unavailable. The recommendation pipeline can bypass the cache and continue through the primary generation path.

### LLM Provider

The system uses an OpenAI-compatible endpoint.

LLM execution is protected through timeout, bounded retry, validation, and deterministic fallback behavior. The LLM endpoint is intentionally not part of readiness because recommendation processing can degrade gracefully when the provider is unavailable.

## Health Endpoints

### API

```text
GET /health/live
GET /health/ready
```

API readiness verifies SQL Server connectivity.

### Worker

```text
GET /health/live
GET /health/ready
```

Worker readiness verifies:

- SQL Server;
- Kafka/Redpanda broker connectivity;
- source topic availability;
- DLQ topic availability.

Redis and the LLM provider are intentionally excluded from readiness because both have fallback behavior.

## Local Prerequisites

Install:

- .NET SDK compatible with `global.json`
- Docker Desktop or Docker Engine with Docker Compose
- an OpenAI-compatible LLM endpoint if the real LLM path will be exercised

The repository currently targets .NET 10.

Verify the SDK:

```bash
dotnet --version
```

Verify Docker:

```bash
docker version
docker compose version
```

## Environment Configuration

Create a local `.env` file from:

```text
.env.example
```

Required configuration includes:

```text
MSSQL_SA_PASSWORD
REDIS_PASSWORD

FLASHSALE_OPENAI_MODEL_ID
FLASHSALE_OPENAI_ENDPOINT
FLASHSALE_LLM_REQUEST_TIMEOUT_SECONDS
FLASHSALE_OPENAI_API_KEY

FLASHSALE_EMBEDDING_MODEL_ID

FLASHSALE_REDIS_CONNECT_TIMEOUT_SECONDS
FLASHSALE_REDIS_OPERATION_TIMEOUT_SECONDS
```

Do not commit `.env` or real credentials.

The example configuration is intentionally safe for source control and contains placeholders only.

For a locally hosted OpenAI-compatible service running on the Docker host, the containerized Worker can access it through:

```text
host.docker.internal
```

## Running the Complete Stack

Start the application stack with:

```bash
docker compose up -d --build
```

The Compose topology includes:

```text
sqlserver
redpanda
redpanda-init
redis
migrate
api
worker
```

The `migrate` service is the single database migration owner.

It uses the API image in one-shot migration mode and exits after applying pending Entity Framework Core migrations.

The long-running API and Worker services do not independently run database migrations during startup.

Inspect the stack:

```bash
docker compose ps
```

Inspect logs:

```bash
docker compose logs api
docker compose logs worker
docker compose logs migrate
```

Stop the stack:

```bash
docker compose down
```

To remove the local Docker volumes as well:

```bash
docker compose down -v
```

## Local Ports

| Service | Host Port |
| --- | ---: |
| API | `5185` |
| Worker health endpoint | `8081` |
| SQL Server | `1433` |
| Redis | `6379` |
| Redpanda external Kafka listener | `19092` |

Container-to-container communication uses Docker service names rather than host addresses.

Examples:

```text
sqlserver:1433
redis:6379
redpanda:9092
```

## API Examples

Check an inventory item:

```http
GET /api/inventory/{productId}
```

Decrease inventory:

```http
POST /api/inventory/{productId}/decrease
Content-Type: application/json
```

Example request body:

```json
{
  "quantity": 1
}
```

A depletion response indicates whether asynchronous recommendation processing has been requested.

Retrieve persisted recommendation plans using a correlation ID:

```http
GET /api/recommendation-plans?correlationId={correlationId}
```

## Database Migrations

Database migration execution is explicit.

For containerized execution, Compose runs the dedicated one-shot migration service before the API and Worker start.

The API image also exposes the migration mode directly:

```bash
dotnet ECommerce.FlashSaleOrchestrator.Api.dll --migrate
```

Application services themselves are not migration owners.

## Testing

Restore and build the full solution:

```bash
dotnet restore ECommerce.FlashSaleOrchestrator.slnx

dotnet build \
  ECommerce.FlashSaleOrchestrator.slnx \
  --configuration Release \
  --no-restore
```

Process-local test projects can run without external infrastructure.

The Infrastructure integration suite additionally requires:

- SQL Server on `localhost:1433`;
- Redis on `localhost:6379`;
- Redpanda/Kafka on `localhost:19092`.

Relevant environment variables include:

```text
FLASHSALE_SQL_CONNECTION
REDIS_PASSWORD
FLASHSALE_KAFKA_BOOTSTRAP_SERVERS
```

Once the required infrastructure is available, run the complete regression suite:

```bash
dotnet test \
  ECommerce.FlashSaleOrchestrator.slnx \
  --configuration Release \
  --no-build
```

Integration tests use isolated test databases, Kafka topics, and Redis test structures where appropriate.

A real external LLM is not required by the automated integration test suite.

## NuGet Vulnerability Audit

The repository can audit both direct and transitive NuGet dependencies:

```bash
dotnet package list \
  --project ECommerce.FlashSaleOrchestrator.slnx \
  --vulnerable \
  --include-transitive
```

CI additionally treats NuGet audit warnings as quality-gate failures.

## Docker Image Verification

Build the API image:

```bash
docker build \
  --file src/ECommerce.FlashSaleOrchestrator.Api/Dockerfile \
  --tag flashsale-api:local \
  .
```

Build the Worker image:

```bash
docker build \
  --file src/ECommerce.FlashSaleOrchestrator.Worker/Dockerfile \
  --tag flashsale-worker:local \
  .
```

Both runtime images execute using the non-root `app` user.

The API exposes container port `8080`, while the Worker health host exposes container port `8081`.

## Continuous Integration

The GitHub Actions workflow is located at:

```text
.github/workflows/ci.yml
```

The pipeline is configured to perform:

```text
checkout
  ↓
.NET setup
  ↓
restore
  ↓
Release build
  ↓
ephemeral CI credential preparation
  ↓
SQL Server + Redis + Redpanda startup
  ↓
dependency health verification
  ↓
full solution regression
  ↓
NuGet vulnerability gate
  ↓
API Docker image build
  ↓
Worker Docker image build
  ↓
dependency cleanup
```

CI credentials are generated only for the workflow execution and are not stored as repository secrets or hard-coded application credentials.

The final workflow execution is validated on GitHub as part of the branch closure process.

## Design Principles

The project follows several implementation constraints:

- inventory mutation and outbox persistence are atomic;
- event consumers are idempotent;
- retries are bounded;
- poison messages can be routed to a DLQ;
- candidate selection is deterministic;
- the LLM can only select from supplied candidates;
- semantic cache compatibility is explicitly governed;
- Redis failure does not stop recommendation generation;
- LLM failure does not prevent deterministic fallback;
- database migration ownership is explicit;
- health checks reflect actual critical dependencies;
- secrets remain outside source control;
- architecture boundaries are enforced through automated tests.

## Current Scope

The repository intentionally focuses on the flash-sale recommendation orchestration problem.

The current scope does not introduce:

- Kubernetes deployment;
- cloud-specific infrastructure;
- unrelated business modules;
- a separate Contracts project.

The emphasis is on correctness, resilience, reproducibility, and maintainable application boundaries.