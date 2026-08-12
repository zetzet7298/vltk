# Design

## Domain Model

`ModeBindingEvidence` links a concrete level/config identifier to player-list construction and native mode predicates. The current evidence records a raw-value-gated `m_listPlayer` helper-call boundary with **both helper full bodies hash-locked by unique generic-pointer-table slot** (11733/11741, dispatch byte-locked, generic binding unresolved), a **complete static caller partition** of all 24 direct `CreateActorEntity` BL sites into 12 `CreatePlayerCreateData`-linked actor sites and 12 outside-factory sites (10 deepened: 4 factory-return-proven, 2 local-init-unresolved, 4 field-local-unresolved), an executable-only direct-caller scan scope, a generic-context enumeration of edge `0x0186C0A8` (slot 133 = 50 `Action<T>.Invoke` rows, one `ActorEntity` candidate, caller `x2` = placeholder `0xC0000183`), and a packaged-config decode whose entries remain uninterpreted. Runtime reachability and semantics remain unproven; none is a mode selection.

## Bounded caller rule

All 24 direct `CreateActorEntity` BL sites are statically partitioned and the 12 factory-linked sites are uniquely resolved and hash-locked; their native chains are conditional static control flow only. The 10 deepened outside-factory sites are classified by `ActorEntityCreateData` provenance; 2 of the 12 remain with producer identity unresolved and 4 with field/local provenance and factory unresolved. Field and parameter labels remain ISIL-correlated, not metadata-layout-proven.

## Application Flow

Map level factory/dispatcher, the `CreateActorEntity` raw argument source, the two locked upstream callers and their bounded x0 dereference chains, the `CreateMonsterCreateData` createData factory, raw-value-gated `m_listPlayer` field/helper-call boundaries, the decoded levelconfig schema, and `IsMultiPlayer` consumers using hash-locked native, metadata, pointer-table, ISIL, bundle, and catalog evidence.

## Interface Contract

Evidence-only; no mode API or pilot configuration is created.

## Data Model

No database/config mutation.

## UI / Platform Impact

Android ARM64 native/ISIL analysis plus read-only AssetBundle/catalog decode only.

## Observability

Record input hashes, ISIL markers, metadata tokens, pointer slots, VA ranges, bounded decoded branches, `m_listPlayer` accesses, upstream caller identity/provenance, the executable-only caller scan scope, bundle object counts, the decoded type-tree schema keys, TextAsset payload, and catalog membership.

## Alternatives Considered

1. Use the `NormalLevelLogic` name as the pilot: rejected because many special levels reuse it.
2. Treat `Count > 1` as co-op authority: rejected without factory and list-population evidence.
3. Treat raw `actorType == 1` as a player/mode label: rejected because the native slice supplies no semantic binding.
4. Treat `CreateMonster`/`BronMonster` as monster/mode semantics: rejected; the names are metadata facts and the slices supply no mode/selective evidence.
5. Treat catalog membership of `MultiplayerExpressionConfig` as a mode binding: rejected; membership is a string presence, not a load-order winner, and the resbin payload is not decoded.
6. Infer mode from decoded levelconfig `m_LevelId`/`m_Monsters`/`m_Obstacles` entries or zero `m_LevelId` values: rejected; the schema is decoded but entries/values are not interpreted into binding.
7. Bind `ActorEntityCreateData +0x14` to a levelconfig row on the locked path: rejected; `+0x14` is a constant factory immediate (`mov w8,#2`; `stp …,[x0,#0x10]` at `0x01860D10`/`0x01860D14`), and no packaged-config row supplies it.
8. Infer a runtime player or mode selector from the metadata name `CreatePlayerCreateData` or raw value `1`/`2`: rejected; the method names are metadata facts and the raw values are immediates, with no semantic binding.
9. Reduce the 50 generic slot-133 candidates to a caller-specific instantiation by static analysis: rejected; the caller `x2` (`0xC0000183`) is an undecoded encoded `MethodInfo` placeholder and the 0xC0xxxxxx encoding has no static decoder in the registered layouts.
10. Treat helper fan-out counts (1610/93 direct BL callers) or the 50 slot-133 rows as identity: rejected; counts are scanner facts, not method identity.
