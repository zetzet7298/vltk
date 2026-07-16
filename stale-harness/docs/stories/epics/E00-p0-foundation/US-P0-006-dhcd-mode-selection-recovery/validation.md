# Validation

## Proof Strategy

`scripts/verify-us-p0-006.sh` validates all three canonical inspector hashes and deterministic JSON outputs, native/metadata/global-manager hashes, durable and generated ISIL method markers, legacy predicate slices, the three upstream caller slices with bounded AAPCS64 x0/x1 provenance and locked dereference chains, the executable-only (PF_X) caller scan scope (1 segment, 37,899,396 bytes, 24 callers), metadata tokens/pointer slots for all resolved methods, the createData factory edges (`CreateMonsterCreateData` def 20738/slot 398 raw 2 and `CreatePlayerCreateData` def 20736/slot 396 raw 1), the **proven factory-immediate `+0x14` provenance on two locked paths** (hash-locked `SetBornPos`/`CreatePlayerCreateData`/`CreateMonsterCreateData`-overload slices; the locked `LevelItemMgr` raw-2 chain `0x0165256C`→factory→`0x016525F4 bl SetBornPos`→`0x0165261C`→`0x01862B48` with CMP `w20,#1` skipping the `m_listPlayer` helper for raw 2; and the locked `NormalLevelLogic.OnGameStart` def 23530/slot 3190 raw-1 chain `0x0160A8D0`→`0x01860B8C`→`0x0160A8DC mov x22,x0`→`0x0160A974 bl SetBornPos`→`0x0160A994 bl CreateActorEntity` (`w2=1`)→`0x01862B48` with CMP `w20,#1` EQUAL reaching the `m_listPlayer` helper for raw 1; plus the `CreatePetCreateData` def 20739/slot 399 negative guard), the **complete static caller partition** (24 = 12 `CreatePlayerCreateData`-linked + 12 outside-factory; 10 deepened — 4 factory-return-proven, 2 local-init-unresolved, 4 field-local-unresolved), the **hash-locked `m_listPlayer` helper full bodies** (generic slots 11733/11741, dispatch offsets +0x60/+0xF8/+0x108 byte-locked), the **generic-context enumeration** of edge `0x0186C0A8` (slot 133 = 50 `Action<T>.Invoke` rows, `BattleCore.ActorEntity` one non-caller-selected candidate, caller `x2` = `0xC0000183`), the packaged-config bundle/catalog hashes, the decoded levelconfig type-tree schema keys, and the multiplayer TextAsset payload. No-pilot/no-parity/no-load-order-winner/no-zero-ID-inference gates hold.

The verifier pins deterministic JSON `185d8092…` (mode-selection, `schema_version` 6), `9268a894…` (packaged-config, `schema_version` 1), and `53c2b741…` (generic-context, `schema_version` 1), runs all three inspectors twice, and fails closed on output drift. `scripts/prepare-r-dhcd-004-isil.sh` reproduces the volatile generated ISIL from hash-pinned Cpp2IL/native/metadata inputs before verification.

## Enumeration gates

Verification locks the complete PF_X segment object, all 24 unique `CreateActorEntity` caller ranges (12 factory-linked with their conditional-static chains, 12 outside-factory with the 10 deepened provenance classes), the two hash-locked helper bodies, and the 50-row generic slot-133 enumeration. It requires `mode_selector`, `solo_coop_authority`, `runtime_parity`, and `load_order_winner` to remain unresolved, caller-specific slot-133 instantiation to remain unresolved, and field/parameter provenance to remain ISIL-correlated.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Input hashes, ISIL markers, slice ranges, metadata identities/pointer slots, executable-only caller scan scope, packaged-config counts/schema/payload, and negative claims. |
| Integration | `CreateActorEntity` register/branch map, upstream caller identity/provenance, and factory/config/player-list binding. |
| E2E | Pending one-player/two-player runtime capture. |
| Platform | Android ARM64. |
| Performance | Not applicable. |
| Logs/Audit | Detailed trace and unresolved queue row. |

## Fixtures

The hash-locked ISIL files, the canonical Android inputs, and the levelconfig/multiplayer-expression bundles plus their index_0/index_1 catalogs listed in the evidence card.

## Commands

```text
scripts/prepare-r-dhcd-004-isil.sh
scripts/verify-us-p0-006.sh
scripts/bin/harness-cli story verify US-P0-006
scripts/bin/harness-cli story verify-all
scripts/bin/harness-cli audit
git diff --check
```

## Acceptance Evidence

- Predicate identity, the raw-value-gated `m_listPlayer` helper-call boundary, the three locked caller slices with bounded AAPCS64 provenance, the executable-only caller scan scope, the createData factory identities, and the packaged-config schema decode are proven only with hash-locked inputs and native/bundle bytes.
- Mode binding remains `in_progress`; the next exact target is **static selection candidates / input provenance** (reduce the 2 local-init-unresolved and 4 field-local-unresolved outside sites to a bounded static createData producer; decode runtime `MethodInfo` `0xC0000183` → one of 50 slot-133 rows; resolve the generic `x2` dispatch operands of the two helpers), not levelconfig-row interpretation and not a runtime-selection claim.
- The generic edge `0x0186C0A8` is enumerated to slot 133 (50 `Action<T>.Invoke` rows, one `ActorEntity` candidate) but caller-specific instantiation remains unresolved; runtime selection/reachability requires an authorized same-build runtime capture. Verification fails if a name/token, pilot/parity claim, load-order-winner claim, interpretation of decoded entries/zero IDs, a caller-specific slot-133 instantiation claim, or a player/monster/pet/mode inference from factory method names, declaring types, `w2` constants, fan-out counts, or raw values `1`/`2` is introduced without evidence.
