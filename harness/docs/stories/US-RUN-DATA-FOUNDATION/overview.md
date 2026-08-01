# US-RUN-DATA-FOUNDATION Overview

## Status

implemented

## Lane

high-risk

## Current Behavior

Production backend identity remains global/integer (`accounts`, `roles`,
`player_states`). A bounded Alembic revision now adds a separate
`identity_foundation` shadow schema with explicit realm-scoped UUID mappings and
fail-closed RLS; it does not mutate or backfill the legacy public tables.

## Target Behavior

An approved, reversible Alembic foundation maps legacy identity to target
`realms`/`accounts`/`characters` UUIDs without silent data loss or invented
password/realm semantics, on a proven disposable PostgreSQL database first.

## Implemented Outcome

- Pure mapping requires explicit realm/account/character UUID authority and an
  explicit credential decision; it rejects invalid types, PostgreSQL width
  overflow, NUL/non-UTF-8 text, ambiguous ownership and secret-bearing iterator
  failures through a finite error surface.
- Alembic `20260720_0001_identity_foundation` creates six shadow/mapping tables,
  named composite identity/ownership constraints, `ENABLE` + `FORCE` RLS and a
  data-preserving downgrade guard. Pre-existing namespace objects abort upgrade
  atomically; shared extensions and legacy `public.*` tables are not dropped.
- Root verified the slice on a dedicated PostgreSQL 16.14 container bound to a
  random localhost port with a tmpfs data directory and no volume mounts. Each
  database case used a nonce database and cleaned it in `finally`.

## Fresh Verification

- `51 passed`: `tests/unit/modules/runtime/test_identity_mapping.py`.
- `10 passed`: `tests/integration/modules/runtime/test_game_v1_identity_migration.py`.
- Scoped Ruff and Black passed; strict specs validation returned
  `OK: inventory=106183 coverage=104655 strict=True`.
- Herdr run `orch-8bb1c204806636e8` finished verified; RESULT SHA-256
  `061c15caadc25a2cae01f3e508382d178464af8782b340ce57f29a8bb2ad5b21`.

## Residual Gaps

- Production PostgreSQL 18 parity remains unproven.
- Realm authority, legacy password migration and canonical public-table cutover
  require explicit approval; no silent default or backfill was introduced.
- Runtime transaction/UoW propagation of `app.realm_id` remains a later slice.

## Affected Users

- Existing accounts/characters and runtime operators.

## Affected Product Docs

- `docs/decisions/0008-game-v1-runtime-authority.md`
- `/var/www/vltk-mobile/contracts/sql/game.v1.sql`
- `/var/www/vltk-mobile/backend/specs/03-du-lieu.md`

## Non-Goals

- Production data migration before mapping/backfill proof.
- Treating legacy integer role ID as normative runtime identity.
