# US-RUN-DURABLE-CHECKPOINT Design

## Domain Model

Versioned runtime checkpoint and immutable command outcome/outbox records exposed
through application ports. A private protobuf schema preserves exact runtime state
without changing public `game.v1`.

## Application Flow

Runtime UoW validates and encodes before DB work, requires an idle connection,
locks the character, resolves existing idempotency, rejects stale epoch/tick/seq,
then commits checkpoint, outcome and outbox once. Only committed or stored replay
outcomes are ACK-safe; uncertain COMMIT is explicit.

## Interface Contract

No public API change.

## Data Model

Bounded `identity_foundation.runtime_checkpoints`, `idempotency_keys` and
`outbox_events` with named constraints/indexes, forced RLS, collision-safe upgrade
and data-guarded rollback. Public target cutover is not claimed.

## UI / Platform Impact

Validated on disposable PostgreSQL 16.14. WSS transport, public cutover and outbox
publisher remain follow-up work.

## Observability

The private result model exposes finite state/error/checkpoint/outbox identifiers.
Production correlated logs, metrics, lag and latency SLOs are explicitly deferred
to WSS/outbox operations work and are not acceptance for this bounded slice.

## Alternatives Considered

1. Commit checkpoint after ACK: rejected because it can lose acknowledged state.
2. Reuse public `game.v1` for storage: rejected because transport and persistence
   evolve independently.
