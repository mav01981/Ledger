# ADR 003: Consistency Model

## Status
Accepted

## Context
CQRS separates write and read models. Events are written to the event store; projections are built asynchronously. This creates an eventual consistency gap between command execution and read model updates.

## Decision
The system is **eventually consistent**. The command response includes the new stream version; clients can poll until the read model reflects that version.

## Rationale
- **Explicit trade-off**: Strong consistency would require distributed transactions (2PC) across the event store and read model, defeating the purpose of CQRS separation.
- **Mitigation strategy**: The `CommandResult.NewVersion` token gives clients a way to verify their write is visible.
- **Outbox pattern**: Ensures events are reliably published to projections even if the process crashes between writing events and updating projections.
- **Demo narrative**: This gap is one of the most interesting trade-offs to discuss — we surface it explicitly rather than hiding it.

## Consequences
- Positive: Write side remains fast and doesn't block on projection updates.
- Positive: Read models can be rebuilt independently.
- Negative: Clients may read stale data immediately after writing.
- Mitigation: Return version token, document polling strategy, mention websocket push as future improvement.
