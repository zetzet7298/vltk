# US-RUN-DURABLE-CHECKPOINT Exec Plan

## Goal

Implement atomic durable runtime persistence.

## Scope

In scope:

- Private checkpoint proto, hash, models, repositories, UoW, migration and tests.

Out of scope:

- Public WSS route and outbox delivery worker.

## Risk Classification

Risk flags:

- Data migration, durability, RLS and replay safety.

Hard gates:

- Identity foundation and disposable PostgreSQL proof must pass first.

## Work Phases

1. Completed: private deterministic checkpoint codec and drift check.
2. Completed: application model plus Alembic 0002 shadow tables/RLS.
3. Completed: psycopg UoW with monotonic replacement and finite commit/load errors.
4. Completed: atomic commit/replay/rollback/race/tamper/migration proof.

## Stop Conditions

- Checkpoint cannot round-trip all authoritative state.
- Atomic transaction or rollback proof is unavailable.

Neither stop condition remains: codec round-trip and disposable PostgreSQL proof
passed. Production WSS/publisher/retention/SLO work stays out of scope.
