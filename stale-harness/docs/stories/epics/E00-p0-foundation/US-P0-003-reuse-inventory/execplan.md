# Exec Plan

## Goal

Deliver the bounded, high-risk reuse inventory needed by `REQ-P0-011` and its
`DOC-CLIENT-01`/`DOC-CLIENT-04` enabling work without claiming runtime
migration completion.

## Scope

Included:

- Exact source evidence for `CityDefenceService`, `MapEnemyDatabase`,
  `PcPortraitParser`, `HudDataBridge`, `GoldenSnapshotComparer`, and
  `CombatRuntimeService`.
- Source authority, allowed reuse, forbidden path, and migration-control fields.
- The high-risk packet, durable story registration, prerequisite dependency, and
  a fail-closed verifier.

Excluded:

- Unity runtime, scene, asset, test, or editor changes.
- Unity MCP operations.
- Changes under `/var/www/jx-pc`.
- Arena dependency or any arena implementation.

## Risk Classification

High-risk because future reuse can alter runtime contracts, save/replay behavior,
and PC-authority claims. The inventory contains no approval to alter these
surfaces. `US-P0-001` must complete first; no dependency on `US-P0-002` is
created.

## Work Phases

1. Record exact, revisioned Unity evidence and source-authority limitations.
2. Add the fail-closed inventory and high-risk packet.
3. Register `US-P0-003`, add `US-P0-001 -> US-P0-003`, and set verifier/proof
   fields.
4. Run the verifier, Harness verification/completion, `srcwalk review`, and
   `git diff --check`.

## Stop Conditions

- Stop if a source line/hash cannot be reproduced at the stated revision.
- Stop if PC evidence is absent but a row would need a parity or selection claim.
- Stop if a later change requires a Unity runtime/scene/asset modification.
- Stop if the verifier detects a missing inventory field, weakened exclusion, or
  a runtime migration-complete assertion.
