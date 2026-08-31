# Ledger Demo — CQRS + Event Sourcing

A double-entry ledger/wallet service built with CQRS and event sourcing.

## Why Event Sourcing?

Most CRUD apps store only current state — history is lost. Event sourcing stores
every state change as an immutable domain event. This gives us:

- **Complete audit trail**: Reconstruct account balance as of any past timestamp
- **Temporal queries**: "What was the balance last Tuesday?"
- **Projection rebuild**: Derive new read models from the event log without touching the write side
- **Debugging**: Replay events to understand how the system reached its current state

The trade-off: eventual consistency, more complex infrastructure, and a steeper
learning curve. This demo makes those trade-offs explicit.

## Architecture

See [docs/architecture.md](docs/architecture.md) for C4 diagrams and sequence diagrams.

### Write Side
1. Client sends a command (e.g., `Deposit`)
2. MediatR handler loads the aggregate (replay events from event store)
3. Handler invokes domain logic on the aggregate
4. New events are appended with optimistic concurrency (expected version)
5. Events are written to the outbox in the same transaction

### Read Side
1. Outbox Relay polls for unprocessed messages
2. Projection Updater updates read models (balances, transaction history)
3. Queries read from projected read models

### Consistency Model
The system is **eventually consistent**. The command response includes the new
stream version. Clients can poll until the read model reflects that version.

## Quick Start

### Prerequisites
- .NET 10.0 SDK (for local development)
- Docker & Docker Compose (for containerized run)

### Run with Docker Compose (recommended)

```bash
# Start PostgreSQL and the API
docker compose up --build
```

The API will be available at `http://localhost:5001` with Swagger UI.

### Run locally

```bash
# Start PostgreSQL (example with Docker)
docker run -d --name ledger-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16

# Run the API
cd src/Ledger.Api
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI.

### Example Requests

> **Note**: When running via Docker Compose, use `http://localhost:5001`. For local `dotnet run`, use `https://localhost:5001`.

```bash
# Open an account
curl -X POST http://localhost:5001/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"accountId":"<guid>","accountType":0,"idempotencyKey":"key-1"}'

# Deposit funds
curl -X POST http://localhost:5001/api/accounts/<guid>/deposit \
  -H "Content-Type: application/json" \
  -d '{"amount":100.00,"idempotencyKey":"key-2"}'

# Withdraw funds
curl -X POST http://localhost:5001/api/accounts/<guid>/withdraw \
  -H "Content-Type: application/json" \
  -d '{"amount":50.00,"idempotencyKey":"key-3"}'

# Transfer between accounts
curl -X POST http://localhost:5001/api/accounts/transfer \
  -H "Content-Type: application/json" \
  -d '{"fromAccountId":"<guid>","toAccountId":"<guid>","amount":25.00,"idempotencyKey":"key-4"}'

# Get balance (read model - eventually consistent)
curl http://localhost:5001/api/accounts/<guid>

# Get point-in-time balance (replays events up to timestamp)
curl "http://localhost:5001/api/accounts/<guid>/balance-at?asOf=2026-08-30T12:00:00Z"
```

## Decisions (ADRs)

- [ADR 001: Event Store Choice](docs/adr/001-event-store-choice.md) — PostgreSQL over EventStoreDB
- [ADR 002: Snapshot Strategy](docs/adr/002-snapshot-strategy.md) — Interface defined, implementation deferred
- [ADR 003: Consistency Model](docs/adr/003-consistency-model.md) — Eventual consistency with version token

## Testing

```bash
dotnet test
```

### Unit Tests (`Ledger.Tests`)
- Aggregate invariant enforcement (no negative balances for standard accounts)
- Event replay / `LoadFromHistory` correctness
- Overdraft account behavior

### Integration Tests (`Ledger.IntegrationTests`)
End-to-end tests using Testcontainers (real PostgreSQL) and WebApplicationFactory (full API stack). These tests verify the complete CQRS + Event Sourcing workflow from HTTP request to projection update.

```bash
# Run integration tests (requires Docker for Testcontainers)
dotnet test tests/Ledger.IntegrationTests
```

Integration tests cover:
- **Account Lifecycle**: Open account, deposit, withdraw with read model verification
- **Validation**: Insufficient funds, duplicate idempotency keys, non-existent accounts
- **Transfers**: Fund movement between accounts, duplicate detection
- **Reversals**: Transaction reversal with balance restoration
- **Read Models**: Balance queries, transaction history, statements
- **Event Sourcing**: Point-in-time balance reconstruction via event replay
- **End-to-End Flows**: Full lifecycle scenarios with multiple accounts and transactions

## Project Structure

| Project | Responsibility |
|---|---|
| `Ledger.Domain` | Aggregates, events, value objects, interfaces |
| `Ledger.Application` | MediatR commands, queries, handlers |
| `Ledger.Infrastructure` | EF Core, event store, outbox, projections |
| `Ledger.Api` | REST controllers, DI configuration |
| `Ledger.Tests` | Domain unit tests |
| `Ledger.IntegrationTests` | End-to-end integration tests with Testcontainers |

## Tech Stack
- C# / .NET 10.0
- MediatR (CQRS pipeline)
- EF Core + PostgreSQL (event store, outbox, read models)
- xUnit (testing)
- Testcontainers + WebApplicationFactory (integration testing)
- MassTransit (referenced, outbox relay implemented as BackgroundService)
- Docker & Docker Compose (containerized deployment)
