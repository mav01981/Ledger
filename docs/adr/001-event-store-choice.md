# ADR 001: Event Store Choice

## Status
Accepted

## Context
The ledger demo needs an event store to persist domain events. We evaluated two options:
1. **EventStoreDB** - Purpose-built event streaming database
2. **PostgreSQL as event store** - Using a traditional RDBMS with an append-only events table

## Decision
Use **PostgreSQL as the event store**.

## Rationale
- **Simplified infrastructure**: The demo already uses PostgreSQL for projections/read models. Using it for the event store means one less moving part for someone running the demo locally.
- **Familiarity**: Most developers understand SQL. The "events table with a unique constraint on (StreamId, StreamPosition)" pattern is easy to explain and debug.
- **Trade-off discussion**: The code structure (IEventStore interface) keeps the door open to swap in EventStoreDB later. The ADR documents the "why not" for interview/demo discussion.
- **Optimistic concurrency**: PostgreSQL's unique constraint on (StreamId, StreamPosition) gives us optimistic concurrency control for free.

## Consequences
- Positive: Single database to run, back up, and monitor.
- Positive: Easy to query events directly with SQL for debugging.
- Negative: Not as optimized for append-only workloads as EventStoreDB.
- Negative: No built-in subscriptions (we implement outbox pattern instead).
