# US-RUN-DURABLE-CHECKPOINT Overview

## Status

implemented

## Lane

high-risk

## Current Behavior

The bounded backend slice now has a private deterministic checkpoint codec,
application durability contract, psycopg adapter and Alembic shadow tables for
checkpoint, command outcome/idempotency and transactional outbox.

## Target Behavior

One runtime UoW atomically persists deterministic checkpoint bytes/hash, command
outcome and one-or-more event-type-unique outbox events against realm-scoped UUID
character identity. It rejects stale replacement, exposes only finite non-ACK
failure states and replays completed duplicate outcomes.

## Affected Users

- Reconnecting players and runtime operators.

## Affected Product Docs

- `docs/decisions/0008-game-v1-runtime-authority.md`
- `/var/www/vltk-mobile/contracts/sql/game.v1.sql`

## Non-Goals

- WSS transport and production rollout.
- Outbox publisher, retention policy, production PostgreSQL 18 parity and
  performance/observability SLOs.
