# Architecture

## C4 Context Diagram

```mermaid
graph TB
    User["👤 User / Client"]
    API["Ledger API<br/>(ASP.NET Core)"]
    WriteSide["Write Side<br/>(MediatR Handlers)"]
    EventStore["Event Store<br/>(PostgreSQL)"]
    Outbox["Outbox<br/>(PostgreSQL)"]
    Relay["Outbox Relay<br/>(BackgroundService)"]
    ReadModels["Read Models<br/>(PostgreSQL)"]
    Bus["Message Bus<br/>(MassTransit - deferred)"]

    User -->|Commands| API
    API -->|MediatR| WriteSide
    WriteSide -->|Append events| EventStore
    WriteSide -->|Write to outbox| Outbox
    Outbox -->|Poll| Relay
    Relay -->|Publish events| ReadModels
    ReadModels -->|Queries| API
```

## Event Flow (Command Handling Sequence)

```mermaid
sequenceDiagram
    participant Client
    participant API as API Controller
    participant Handler as Command Handler
    participant ES as Event Store
    participant O as Outbox
    participant Relay as Outbox Relay
    participant RM as Read Model

    Client->>API: POST /api/accounts/deposit
    API->>Handler: Send(DepositCommand)
    Handler->>Handler: Load aggregate (replay events)
    Handler->>Handler: Apply business logic
    Handler->>ES: AppendToStreamAsync(events, expectedVersion)
    Handler->>O: Write events to outbox
    Handler-->>API: CommandResult(Ok, newVersion)
    API-->>Client: 200 OK { aggregateId, newVersion }

    Relay->>O: Poll for unprocessed messages
    Relay->>RM: Update projection (balance, history)
    Relay->>O: Mark as processed
```

## CQRS Read/Write Separation

```mermaid
graph LR
    subgraph Write Side
        C[Commands] --> H[MediatR Handlers]
        H --> A[Account Aggregate]
        A --> ES[(Event Store)]
    end

    subgraph Read Side
        O[(Outbox)] --> R[Outbox Relay]
        R --> P[Projection Updater]
        P --> RM[(Read Models)]
        Q[Queries] --> RM
    end

    ES -->|events| O
```

## Project Structure

```
src/
├── Ledger.Domain/          # Domain model, events, interfaces
│   ├── Account.cs          # Aggregate root
│   ├── Events/             # Domain events
│   ├── Models/             # DTOs for read models
│   └── IEventStore.cs      # Abstractions
├── Ledger.Application/     # CQRS handlers & queries
│   ├── Commands/           # Command records + handlers
│   ├── Queries/            # Query records + handlers
│   └── Handlers/           # MediatR handlers
├── Ledger.Infrastructure/  # Persistence & messaging
│   ├── Data/               # EF Core entities & DbContext
│   ├── EventStore/         # Postgres event store
│   ├── Outbox/             # Outbox relay & projection updater
│   ├── ReadModel/          # PostgreSQL read model
│   └── Snapshots/          # Snapshot store
└── Ledger.Api/             # ASP.NET Core API
    ├── Controllers/        # REST endpoints
    └── Program.cs          # DI configuration
```
