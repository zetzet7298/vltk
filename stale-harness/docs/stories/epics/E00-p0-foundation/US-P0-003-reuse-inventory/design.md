# Design

## Domain Model

`ReuseInventoryRow` is an evidence record, not a migration approval. It is valid
only when it has `module_id`, exact path/line, Unity revision/blob/SHA-256,
bounded current behavior, source authority, allowed reuse, forbidden path,
adapter gap, shadow gap, migration-test gap, feature flag, rollback, and
retirement criteria. Missing PC or migration evidence keeps the row
`provisional`.

The authoritative PC corpus is read-only `/var/www/jx-pc`. Current Unity
source is implementation evidence only; it cannot establish JX behavior,
identity, visual parity, or a selected PC resource.

## Application Flow

1. Capture the exact current Unity source evidence at one revision.
2. Classify the minimum reusable seam and explicitly exclude unsafe runtime or
   default paths.
3. Identify the required PC authority and unproven adapter/shadow/migration work.
4. Require flag, rollback, and retirement criteria before any later rollout.
5. Fail verification if a packet/inventory anchor is missing or a prohibited
   completion claim appears.

## Interface Contract

This story adds no runtime interface. The documented contract is:

- `CityDefenceService` remains `parser-only`; `DateTimeOffset.UtcNow`, host
  orchestration, and reward grant are forbidden.
- `MapEnemyDatabase` remains `audited-roster-only`; curated/default/fallback
  roster and spawn paths are forbidden.
- Every other candidate is limited to the evidence-backed reuse boundary in the
  inventory.

## Data Model

No Unity, game persistence, Harness schema, or PC-source data changes occur.
The Harness story record stores the verifier outcome only. The inventory records
source-file hashes, not copied PC bytes or selected resource evidence.

## UI / Platform Impact

None. No mobile, server, editor, scene, asset, or UI behavior changes.

## Observability

`scripts/verify-us-p0-003.sh` checks all packet files, the inventory evidence
anchors, the exact Unity revision/blob/SHA-256 values, and the two mandatory
reuse exclusions. It also rejects a runtime migration-complete claim. It does
not run Unity or establish runtime parity.

## Alternatives Considered

1. Treat current Unity comments as PC proof: rejected because the PC corpus is
   authoritative and Unity is only an implementation clue.
2. Approve all candidates as reusable: rejected because runtime/default paths
   lack deterministic, adapter, shadow, and migration proof.
3. Block documentation until migration tests exist: rejected because the purpose
   is to expose those missing gates before a later migration story begins.
