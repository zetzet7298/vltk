# US-RUN-DATA-FOUNDATION Exec Plan

## Goal

Prove a reversible target identity migration foundation.

## Scope

In scope:

- Mapping decision details, Alembic bootstrap/revision, RLS context and isolated proof.

Out of scope:

- Destructive production cutover and runtime checkpoint tables.

## Risk Classification

Risk flags:

- Auth and data migration.
- Authorization/RLS.
- Existing behavior.

Hard gates:

- Disposable PostgreSQL must be proven before integration commands.
- Ambiguous password/realm/backfill values remain fail-closed.

## Work Phases

1. Prove disposable DB.
2. Pin mapping/backfill fixtures.
3. Implement upgrade/rollback.
4. Verify constraints/RLS/counts.

## Stop Conditions

- Any required legacy value has no authoritative mapping.
- Database isolation is not proven.
