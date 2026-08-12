# US-RUN-DATA-FOUNDATION Design

## Domain Model

Realm-scoped UUID account/character identity with explicit legacy IDs.

## Application Flow

Migration creates target identity foundation, backfills through explicit mapping,
validates counts/uniqueness, then enables RLS context; rollback preserves legacy data.

## Interface Contract

No endpoint switch until migration/shadow-read proof passes.

## Data Model

Normative `realms`, `accounts`, `characters` constraints and RLS; legacy tables
remain migration inputs, not aliases.

## UI / Platform Impact

Deployment requires isolated upgrade/rollback and later canary rollout.

## Observability

Record row counts, rejected mappings and rollback outcome without secrets.

## Alternatives Considered

1. FK runtime to `roles.id`: rejected by decision 0008.
