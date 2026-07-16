# Exec Plan

## Goal

Recover concrete solo/multiplayer mode binding or preserve the exact predicate boundary.

## Scope

In scope: hash-locked ISIL/native slices, `CreateActorEntity` first-callee control flow, three hash-locked upstream caller slices with proven metadata identity and bounded AAPCS64 x0/x1 provenance, an executable-only (PF_X) direct-caller scan with a bounded scan-scope fact, the createData factory identities (`CreateMonsterCreateData` raw 2 and `CreatePlayerCreateData` raw 1), raw-value-gated `m_listPlayer` field/helper-call analysis, and a fail-closed packaged-config decode (levelconfig type-tree schema + multiplayer TextAsset) of the bundles and their catalog inputs.

Out of scope: mode implementation, networking, reward authority, JX source, interpreting decoded schema entries/values into binding, resolving the active config load-order winner, and decoding the multiplayer resbin payload.

## Risk Classification

Risk flags: multiplayer contract, cross-platform native behavior, weak reverse proof, packaged-config drift.

Hard gates: ISIL markers, native/metadata hashes, bundle/catalog hashes, object counts, type-tree schema keys, TextAsset payload, UnityPy/Unity versions, and the executable-only caller scan scope must match; no pilot claim from class/property/bundle names, raw values, or zero IDs.

## Work Phases

1. Verify the canonical slices, durable ISIL markers, the two caller slices, and the executable-only scan scope.
2. Lock `CreateActorEntity` argument setup, named callees, return branches, bounded x0 dereference chains, upstream caller identity/provenance, and the unresolved generic edge.
3. Lock the factory-immediate provenance of `+0x14` — the `SetBornPos` (def 20734/slot 394), `CreatePlayerCreateData` (def 20736/slot 396, raw 1), and `CreateMonsterCreateData` overload (def 20737/slot 397, def 20738/slot 398, raw 2) slices plus the two locked paths (`LevelItemMgr`: `0x0165256C`→`0x01860CA4`→`0x016525F4 bl SetBornPos`→`0x0165261C`→`0x01862B48`, CMP skips helper for raw 2; `NormalLevelLogic.OnGameStart` def 23530/slot 3190: `0x0160A8D0`→`0x01860B8C`→`0x0160A8DC mov x22,x0`→`0x0160A974 bl SetBornPos`→`0x0160A994 bl CreateActorEntity` (`w2=1`)→`0x01862B48`, CMP EQUAL reaches helper for raw 1) and the `CreatePetCreateData` (def 20739/slot 399) negative guard; the "bind `+0x14` to a config row" hypothesis is disproven for both locked paths.
4. Complete the static caller partition: all 24 direct `CreateActorEntity` BL sites split into 12 `CreatePlayerCreateData`-linked actor sites and 12 outside-factory sites (2 locked, 10 deepened by createData provenance — 4 factory-return-proven, 2 local-init-unresolved, 4 field-local-unresolved); lock both `m_listPlayer` helper full bodies by generic-pointer-table slot (11733/11741) with byte-locked dispatch offsets; enumerate edge `0x0186C0A8` as generic slot 133 (50 `Action<T>.Invoke` rows, one `ActorEntity` candidate, caller `x2` = `0xC0000183`).
5. Next (static selection candidates / input provenance only): reduce the 2 local-init-unresolved and 4 field-local-unresolved outside sites to a bounded static createData producer; decode runtime `MethodInfo` `0xC0000183` → one of 50 slot-133 rows; resolve the generic `x2` dispatch operands of the two helpers. Runtime selection/reachability is out of scope for static analysis and requires an authorized same-build runtime capture.
6. Decode packaged-config schema/count/catalog facts with a fail-closed inspector; record schema entries uninterpreted and catalog membership non-authoritative.
7. Run the verifier and Harness gates.

## Enumeration result

The direct caller set is complete at 24, statically partitioned into 12 `CreatePlayerCreateData`-linked actor sites and 12 outside-factory sites; the 12 factory-linked sites each have a bounded conditional-static chain (11 include `SetBornPos`, one does not) and the 10 deepened outside sites are classified by createData provenance. Next target is static selection candidates / input provenance and the `0xC0000183` MethodInfo binding, not runtime caller-name semantics.

## Stop Conditions

Stop at the proven predicate, the complete static caller partition (24 = 12 + 12), the hash-locked helper bodies, the bounded generic-context enumeration, factory-immediate provenance, and packaged-config schema decode if caller-specific instantiation (the `0xC0000183` MethodInfo), runtime selection, or helper semantics remain non-unique. Runtime selection/reachability requires an authorized same-build runtime capture and must not be claimed by static analysis. Do not turn names, declaring types, raw values, `w2` constants, fan-out counts, catalog membership, or static call presence into semantics.
