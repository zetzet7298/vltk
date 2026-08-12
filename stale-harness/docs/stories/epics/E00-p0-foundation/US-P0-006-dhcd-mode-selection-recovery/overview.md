# Overview

## Current Behavior

`NormalLevelLogic.IsMultiPlayer` is hash-locked to a native predicate that delegates to `ActorEntityMgr.IsMultiPlayer`, which tests `m_listPlayer.Count > 1`. The canonical fail-closed inspectors (mode-selection `schema_version` 6, deterministic JSON `185d8092…`; generic-context `schema_version` 1, deterministic JSON `53c2b741…`) also prove raw-`actorType == 1` and destroy-path `m_listPlayer` helper-call boundaries with **both helper full bodies hash-locked by unique generic-pointer-table slot** (`0x01C52F20` slot 11733 SHA `2f1c91a3…`, `0x01C545D0` slot 11741 SHA `9503daba…`; dispatch offsets +0x60/+0xF8/+0x108 byte-locked, generic binding unresolved), three hash-locked upstream `CreateActorEntity` caller slices (`LevelItemMgr.CreateMonster` def 24316/slot 3976, `WaveRefresh.BronMonster` def 24671/slot 4331, `NormalLevelLogic.OnGameStart` def 23530/slot 3190) found by scanning only the single file-backed executable (PF_X) PT_LOAD segment with bounded AAPCS64 x0/x1 provenance, the createData factories `ActorEntityCreateData.CreateMonsterCreateData` (def 20738/slot 398) and `CreatePlayerCreateData` (def 20736/slot 396), **proven factory-immediate provenance for `ActorEntityCreateData +0x14` on two locked paths** — `LevelItemMgr.CreateMonster` raw 2 (m_listPlayer helper skipped) and `NormalLevelLogic.OnGameStart` raw 1 (m_listPlayer helper reached) — via the hash-locked factory/field-method slices `SetBornPos` (def 20734/slot 394), `CreatePlayerCreateData` (def 20736/slot 396, raw 1), and the two `CreateMonsterCreateData` overloads (def 20737/slot 397 and def 20738/slot 398, both raw 2), plus `CreatePetCreateData` (def 20739/slot 399) recorded as a negative guard. A **complete static caller partition** of all 24 direct `CreateActorEntity` BL sites splits into 12 `CreatePlayerCreateData`-linked actor sites and 12 outside-factory sites (two already locked: `0x0165261C`, `0x01683E38`; the ten others deepened: 4 factory-return-proven, 2 local-init-unresolved-identity, 4 field-local-unresolved-factory). A fail-closed **generic-context inspector** enumerates edge `0x0186C0A8` as generic slot 133 = exactly 50 `System.Action<T>.Invoke` rows with `BattleCore.ActorEntity` exactly one candidate (not caller-selected) and caller `x2` decoding only to encoded placeholder `0xC0000183`. A fail-closed packaged-config decode covers the levelconfig and multiplayer-expression bundles including the levelconfig type-tree schema (`m_GameObject`/`m_Enabled`/`m_Script`/`m_Name`/`m_LevelData`; `m_LevelData` carries `m_LevelId`/`m_Monsters`/`m_Obstacles`). The earlier "bind `+0x14` to a config row" hypothesis is **disproven for both locked paths**: `+0x14` is a constant factory immediate, not a levelconfig row. Runtime factory/caller selection, list population semantics, helper semantics, the 0xC0xxxxxx MethodInfo binding, semantic meaning of raw values `1`/`2`, active catalog/load-order winner, and runtime parity are not recovered and are not promised by static analysis.

## Enumerated static callers

All 24 direct `CreateActorEntity` BL sites are statically partitioned: 12 `CreatePlayerCreateData`-linked actor sites (each with a bounded conditional-static chain; 11 include `SetBornPos`) and 12 outside-factory sites (2 locked, 10 deepened by createData provenance class). Static presence is not runtime reachability, mode selection, or parity.

## Target Behavior

Bound the static selection candidates and input provenance around solo/multiplayer mode without treating `NormalLevelLogic`, the caller method names, or raw actor-type values as a pilot selector. Concrete runtime selection requires an authorized same-build runtime capture and is out of scope for static analysis.

## Affected Users

- Gameplay and multiplayer owners.
- Reviewers checking mode-scope claims.

## Affected Product Docs

- `specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md`
- `specs/dhcd-jx-port/10-research/unresolved-rules.md`
- `/home/zet/Projects/dhcd/docs/evidence/r-dhcd-004-mode-selection.md`

## Non-Goals

- No mode default, co-op authority, or reward behavior.
- No Unity/server implementation.
- No semantic player classification from `ActorEntityType` names or raw values.
- No mode/select/runtime inference from caller method names, declaring types, bundle names, the `MultiplayerExpressionConfig` TextAsset name, catalog membership, or zero `m_LevelId` values.
- No player/monster/pet/gameplay-category/mode/solo/co-op/list inference from factory method names (`SetBornPos`, `CreatePlayerCreateData`, `CreateMonsterCreateData`), the provenance-class labels, the `w2` register constants, or raw values `1`/`2`; `+0x14` is a factory immediate, not a levelconfig-row interpretation target for the locked path.
- No runtime-selection or runtime-reachability claim from static analysis; resolving which of the 24 callers, which overload, or which of the 50 slot-133 candidates is runtime-selected requires an authorized same-build runtime capture.
