# ADR 002: Snapshot Strategy

## Status
Accepted (interface defined, implementation deferred)

## Context
Event-sourced aggregates replay events to reconstruct state. For accounts with many events, this becomes expensive. Snapshots store periodic state to reduce replay cost.

## Decision
Define the `ISnapshotStore` interface and `AccountSnapshot` record. Make snapshot frequency configurable.

## Rationale
- **Interface-first**: The `ISnapshotStore` is in the Domain layer, keeping persistence ignorant.
- **Configurable frequency**: A setting like "snapshot every N events" lets operators tune the trade-off between replay cost and storage/staleness.
- **Defer implementation**: For the demo, the event store + outbox + projections are the priority. The snapshot interface shows the pattern without the implementation overhead.

## Consequences
- Positive: Clear extension point for performance optimization.
- Positive: Rehydration algorithm (load snapshot → replay newer events) is documented.
- Negative: Without implementation, the demo doesn't show snapshot/replay working end-to-end.
