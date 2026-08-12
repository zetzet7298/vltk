# Validation

## Proof Strategy

`scripts/verify-us-p0-003.sh` proves only the documentation/inventory contract:
the four high-risk packet files exist, all six revisioned evidence rows are
present, the mandatory parser-only/audited-roster-only exclusions remain, and
runtime migration remains explicitly unproven. It cannot prove a Unity runtime
migration, PC parity, save migration, feature rollout, or visual golden result.

## Test Plan

| Layer | Cases |
|---|---|
| Unit | Shell verifier checks packet/inventory literal anchors and current source hashes. |
| Integration | Harness stores `US-P0-003` as high-risk with `US-P0-001` as its only dependency. |
| E2E | Not applicable: no runtime or editor flow is introduced. |
| Platform | Not applicable: no Unity/mobile/server surface changes. |
| Logs/Audit | Inventory retains exact revision/blob/SHA-256 and source-authority limits. |

## Negative Validation

- Fail if any of the six source paths, line ranges, blobs, or SHA-256 values is
  absent or differs from the recorded snapshot.
- Fail if `CityDefenceService` is not parser-only or its wall-clock/reward path
  is allowed.
- Fail if `MapEnemyDatabase` is not audited-roster-only or curated/default/
  fallback paths are allowed.
- Fail if adapter, shadow, migration, flag, rollback, or retirement evidence is
  omitted.
- Fail if the packet or inventory claims runtime migration complete.

## Fixtures

No Unity, PC, asset, or save fixture is introduced. The verifier reads existing
source solely to confirm the recorded source snapshot and does not mutate it.

## Commands

```text
scripts/verify-us-p0-003.sh
scripts/bin/harness-cli story verify US-P0-003
scripts/bin/harness-cli story complete US-P0-003
```

## Acceptance Evidence

- The packet identifies `REQ-P0-011`, `DOC-CLIENT-01`, and `DOC-CLIENT-04`.
- The inventory contains all six exact source-evidence records and explicit
  PC-authority limitations.
- The verifier proves the inventory contract only; runtime migration is still
  unproven.
- Harness records the high-risk story and `US-P0-001 -> US-P0-003` dependency.
