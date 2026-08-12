# Overview

## Current Behavior

`reuse-inventory.md` previously named broad reuse candidates and exclusions, but
did not retain all six candidate modules with exact Unity revision/hash evidence,
bounded behavior, or explicit migration proof gaps. The existing code includes
runtime paths that must not be inferred to be canonical JX behavior.

## Target Behavior

`US-P0-003` completes a fail-closed inventory contract for `REQ-P0-011`,
enabling `DOC-CLIENT-01` and `DOC-CLIENT-04`. It records only what current
Unity source proves, preserves the parser-only `CityDefenceService` decision and
the audited-roster-only `MapEnemyDatabase` decision, and makes missing migration
proof visible. Completion proves the inventory contract, not a runtime migration.

## Affected Users

- Client lead selecting a safe adapter boundary.
- JX reviewer confirming that PC source evidence is still required.
- Technical reviewer checking a later migration story.

## Affected Product Docs

- `specs/dhcd-jx-port/06-client/reuse-inventory.md` (`REQ-P0-011`,
  `DOC-CLIENT-01`, `DOC-CLIENT-04` enabling evidence).
- `specs/dhcd-jx-port/06-client/reuse-migration-gates.md` (adapter, shadow,
  migration, flag, rollback, and retirement gates).

## Non-Goals

- No Unity code, scene, asset, runtime, or save migration.
- No Unity MCP operation and no modification of `/var/www/jx-pc`.
- No PC source/asset selection, resolver claim, visual parity claim, or runtime
  migration-complete claim.
