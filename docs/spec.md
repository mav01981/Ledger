# Ledger Demo — Technical Spec

## Overview
A double-entry ledger/wallet service built with CQRS and event sourcing. Every state change is captured as an
immutable domain event; current state is derived by replaying events (optionally
from a snapshot), and read models are built asynchronously from the same event
stream.

## Domain model

### Account (aggregate root)
- `AccountId` (Guid)
- `AccountType` (Standard, Overdraft)
- `Balance` (derived, not stored directly — computed from applied events)
- `Status` (Open, Closed)

### Transaction (aggregate, or part of Account depending on final design)
- `TransactionId` (Guid)
- `Lines[]` — each with `AccountId`, `Amount`, `Direction` (Debit/Credit)
- Invariant: for any transaction, sum(debits) == sum(credits)
- `Status` (Posted, Reversed)

## Commands
| Command | Description | Key validation |
|---|---|---|
| `OpenAccount` | Creates a new account | Account type must be valid |
| `Deposit` | Single-sided credit to an account | Account must be Open |
| `Withdraw` | Single-sided debit from an account | Sufficient balance unless Overdraft |
| `Transfer` | Two-sided posting between two accounts | Both accounts Open; net-zero lines |
| `ReverseTransaction` | Posts an offsetting transaction | Original transaction must be Posted |

## Domain events
- `AccountOpened { AccountId, AccountType, Timestamp }`
- `FundsDeposited { AccountId, TransactionId, Amount, Timestamp }`
- `FundsWithdrawn { AccountId, TransactionId, Amount, Timestamp }`
- `TransferPosted { TransactionId, Lines[], Timestamp }`
- `TransactionReversed { OriginalTransactionId, ReversalTransactionId, Timestamp }`

All events are versioned (`EventVersion`) and stored with a monotonically
increasing `StreamPosition` per aggregate stream.

## Event store
- Candidate 1: **EventStoreDB** — purpose-built, native stream/subscription model,
  good story for the "why not just use Postgres" discussion
- Candidate 2: **Postgres as event store** — `events` table keyed by
  `(StreamId, StreamPosition)`, append-only, unique constraint on
  `(StreamId, StreamPosition)` for optimistic concurrency
- Decision to be recorded as an ADR; demo should ideally show the trade-off
  discussion even if only one is implemented

## Snapshotting
- Snapshot table/stream: `{ AggregateId, Version, State (JSON), SnapshottedAt }`
- Rehydration algorithm:
  1. Load latest snapshot for aggregate (if any)
  2. Load events with `StreamPosition > snapshot.Version`
  3. Apply events on top of snapshot state to reach current state
- Snapshot frequency configurable (e.g., every 50 events) — documented as a
  tunable trade-off between replay cost and storage/staleness

## CQRS write side
- MediatR command handlers per command
- Handler flow: load aggregate (snapshot + replay) → validate/apply command →
  append new events with expected version (optimistic concurrency) → write to
  outbox in the same transaction
- Idempotency: commands carry a client-supplied idempotency key; handler checks
  a dedupe store before processing

## CQRS read side / projections
- Outbox relay publishes committed events to a message bus (MassTransit)
- Projection consumers build/update read models:
  - **Account balance view** — current balance per account
  - **Transaction history view** — flattened list of postings per account
  - **Statement view** — period-bounded summary (e.g., monthly statement)
- Projection rebuild tool: replays the full event stream into a fresh read model
  table — demonstrates both disaster recovery and schema evolution (rebuild into
  a *new* shape without touching the write side)

## Consistency model
- System is eventually consistent between write and read sides
- Client-facing mitigation: command response includes the new stream version;
  client can poll/subscribe until the read model reflects that version, or the
  UI can optimistically render the pending state
- This gap and its handling should be called out explicitly in the README/demo
  narrative — it's one of the more interesting trade-offs to discuss

## Showcase capability: point-in-time balance
- Given the full event log, reconstruct account balance as of any past timestamp
  by replaying events up to that point (bypassing snapshots newer than the
  target time, or using an older snapshot if available)
- Framed as an audit/compliance capability enabled "for free" by event sourcing

## Stretch: saga for cross-shard/multi-step transfers
- If ledger is partitioned/sharded, a transfer spanning shards needs
  orchestration (e.g., reserve → commit/compensate)
- Implement as a MassTransit saga if time allows; otherwise document as a
  "how I would extend this" section

## Testing
- Testcontainers spin up the event store (and Postgres for projections) for
  integration tests
- Test cases: aggregate invariant enforcement, snapshot/replay correctness,
  optimistic concurrency conflicts, idempotent command replay, projection
  rebuild correctness

## Frontend
- Angular minimal dashboard
- Screens: account list + balances, transfer form, transaction/statement view
- Calls write API (commands) and read API (projections) separately, making the
  CQRS split visible in the client too

## Deliverables
- Source repo with the above implemented (core + Phase 1–4 minimum)
- ADRs: event store choice, snapshot strategy, consistency model
- Architecture diagram (C4-style) and event flow / sequence diagrams (Mermaid)
- README narrating the "why event sourcing here" case
