# PORT_STATUS.md — Evidence Audit PC → Mobile

> **Audit date**: 2026-06-08
> **PC source of truth**: `/var/www/vltksource_new/vl_update_27`
> **Mobile repo**: `/var/www/vltk-mobile` (`dev` at `0480502` when audit started)
> **Scope**: toàn bộ codebase/status, không chỉ map.
> **Dirty excluded**: `Assets/Scripts/UI/CharacterPanelService.cs`, `Assets/Scripts/UI/GameHudController.cs`, `harness/item_spr_img/` là HUD/UI WIP của human/parallel work, không dùng làm proof.

## Status legend — strict

| Status | Meaning |
|---|---|
| ✅ | Verified by current file/catalog/verifier/test/source evidence for the stated narrow scope. |
| 🔄 | Partial: data/service/parser/runtime exists, but PC semantic parity or end-to-end proof is missing. |
| ☐ | Missing/not ported/client-out-of-scope. |
| ⚠️ | Old `PORT_STATUS.md` claim was overbroad or false under this audit. |

**Hard rule:** A parser/service/test skeleton is **not** completion. Mark `✅` only when the row states the exact verified scope, e.g. “data catalog loaded”, not “PC behavior complete”.

## Audit evidence actually collected

- `git status --short --branch`: repo dirty only with excluded HUD/UI files above.
- `srcwalk map --scope Assets/Scripts/Sandbox --depth 1`: codebase inventory.
- Python evidence scan:
  - `Assets/Scripts/**/*.cs` = 691 C# scripts.
  - `Assets/Tests/**/*.cs` = 172 test files.
  - Rough classes: 316 `*Service`, 246 `*Parser`, 91 `*Panel*`.
  - Rough test attributes: 2,027; inconclusive/ignore/explicit markers: 48.
- `python3 scripts/jx_map_port_verify.py --include-missing-spr-region-refs --pretty`:
  - `status=pass_with_known_gaps`, `errors=[]`.
- Unity console check: only known Addressables GUID conflict noise during this audit.
- Batch 3b adds a fresh isolated Unity Test Runner artifact for `PortFactorySmoke`: EditMode `2/2 passed` via Unity MCP job `5d7958b697ad4565aab8a1e409430c05`. Existing `VLTK.Tests.EditMode`/PlayMode asmdefs remain gated/stale; this is not a full-suite claim.
- Exa research timed out twice; DeepWiki fetched UnityCsReference overview. This audit relies on current repo files and PC source-of-truth, not external guesses.

## Current high-confidence map verifier facts

```text
visual aliases: 1,005 PC map aliases
unique visual geometries: 332
Region_C files: 95,246
map SPR files: 2,785
known missing map SPR refs: 6 unique paths / 182 refs, all source-missing or engine-default fallback
server Region_S: 330/332 geometries; missing static Region_S aliases 134 and 1007
Region_S files: 84,019
Region_S records: 67,680 NPC / 8,692 trap / 453 object
NPC sprite staging: 375/375 resTypes, 1,314 staged sprites
object visuals/actions: 35/35 templates, 34/34 SPR paths, 299 deterministic object actions
trap scripts/actions: 817/817 script ids resolved; runtime action catalog loads 804 deterministic + 13 host-limited routed actions; verifier still reports those 13 as deferred PC-runtime gaps; 0 unclassified
default map: 907 — Vượt ải Nhiếp Thí Trần
```

Deferred trap families after Batch 3 integration remain host-limited, not full PC parity:

```text
ClearSkillTeamEnterHole: 4 routed as partial runtime; Batch 2 service model proves captain/team/CSP_CheckValid/free-map scan/OpenMission/RunMission/AddMSPlayer/SetTempRevPos plan semantics; Batch 3 proves ClearSkill mission lifecycle constants/model, but real host mission/runtime execution remains incomplete.
TongMapEntrance: 8 routed as host-limited partial hook; Batch 2 service model proves default/cn_ib banned/expire/near-expiry SetPos/SetFightState/message decisions, but PC Tong ownership/ban/expire/product-region/template-map host APIs remain incomplete.
CityWarJoinRouter: 1 routed as partial runtime; Batch 2 service model proves mission state/ticket/card/Tong/camp join plan semantics; Batch 3 proves card/token constants and transfer-route split 222/223→221, but full mission lifecycle/rewards/live runtime remain incomplete.
```

## Verified data files and exact counts

These are **data/catalog facts only** unless the “Runtime parity” column says otherwise.

| Domain | File/artifact | Verified count | Status | Runtime parity |
|---|---|---:|---:|---|
| NPC templates | `Reference/PcNpc/npcs.txt` | 2,000 rows | ✅ | 🔄 behavior/scripts/AI not proven full PC |
| Rare spawns | `Reference/PcNpc/rare.txt` | 480 rows | ✅ | 🔄 spawn scheduling not fully audited |
| Gold bosses | `Reference/PcNpc/goldboss.txt` | 32 rows | ✅ | 🔄 event/schedule behavior not fully audited |
| Base skills | `Reference/PcSkill/skills.txt` | 1,216 rows | ✅ | 🔄 skill behavior/formulas/scripts partial |
| Weapon skills | `Reference/PcSkill/clientweaponskill.txt` | 32 rows | ✅ | 🔄 behavior not audited |
| Thief skills | `Reference/PcSkill/thiefskill.txt` | 4 rows | ✅ | 🔄 behavior not audited |
| Mod skills local | `Reference/ModSkills.txt` | 1,554 rows | 🔄 | PC `skills1.txt` 1,712 is not fully represented/proven |
| Missiles local | `Reference/PcMissles.txt`, `Reference/ModMissles.txt` | 441 + 441 rows | 🔄 | old “480 effects” claim not proven |
| Meridian levels | `Reference/PcMeridian/meridian_level.txt` | 128 rows | ✅ | 🔄 full UX/effect parity not audited |
| Translife level table | `Reference/PcTask/translife.txt` | 41 rows / levels 160..200 | ✅ data/service | Chuyển Sinh level bonus table only; skill unlock/effect application runtime not proven |
| Gold equip | `Reference/PcItemFull/goldequip.txt` | 5,346 rows | ✅ | data catalog |
| Platina equip | `Reference/PcItemFull/platinaequip.txt` | 5,336 rows | ✅ | data catalog |
| Armor/helm/boot/cuff/belt/ring/amulet/pendant | `PcItemFull/*.txt` | 290/140/40/20/20/10/20/20 | ✅ | data catalog |
| Melee/range/horse/potion | `PcItemFull/*.txt` | 60/30/350/40 | ✅ | data catalog |
| Magic attributes | `Reference/PcItemFull/magicattrib.txt` | 330 rows | ✅ | old 333 claim false |
| Compound recipes | `Reference/PcItemFull/atlas_compound.txt` | 1,294 rows | ✅ data / 🔄 craft | craft execution/UI still partial |
| Quest keys | `Reference/PcItemFull/questkey.txt` | 2,045 rows | ✅ | data/service |
| Hongbao data | `Reference/PcItemFull/hongbao.txt` | 69 rows | ✅ data / 🔄 service path | parser/service now targets PC `settings/item/hongbao.txt`; weighted claim/item-award/costly/message/log runtime semantics still partial |
| Item exchange source catalog | `Reference/PcItemExchange` | 7,334 normal / 480 rare / 200 level_exp / 100 level_lead_exp / 35 rolevalue keys | ✅ catalog / 🔄 runtime | Source catalog only; exchange rules/inventory/economics/log side effects not proven |
| Shop goods/buysell | `Reference/PcShop/goods.txt`, `buysell.txt` | 1,521 + 165 rows | ✅ | catalog; NPC shop UX not fully audited |
| Lottery | `Reference/PcLottery/lottery.txt` | 254 rows | ✅ | data/service |
| Adventure | `Reference/PcAdventure/adventure.txt` | 1,037 rows | ✅ | catalog; quest semantics partial |
| Player task defs | `Reference/PcMission/player_task_def.txt` | 656 rows | ✅ data | mission execution partial |
| Guild levels | `Reference/PcTong/tong_level_data.txt` | 6 rows | ✅ | guild scripts partial |
| City war config | `Reference/PcEvent/citywar.ini` | 90 data lines | ✅ data | join/router semantics partial |
| MissionBattle combo/scores matrix | `Reference/PcBattlefield/MissionBattle/combo.txt`, `scores.txt` | 5 ranks / 25 combo cells / 25 score cells | ✅ data/service subset | Runtime scoring integration, kill/death award flow, mission lifecycle not proven |
| Waypoints | `Reference/PcMap/waypoint.txt` | 225 rows | ✅ data / 🔄 runtime | parser preserves exact PC count; `PcMapTravelRuntimeService` lookup proof covers representative IDs/maps; end-to-end travel UX/runtime still partial |
| Wharves | `Reference/PcMap/wharf.txt` | 11 rows / 16 SECT slots | ✅ data / 🔄 runtime | parser preserves row 3 `COUNT=1` with 2 real SECT slots; `PcMapTravelRuntimeService` lookup proof covers service lookup; end-to-end wharf travel UX/runtime still partial |
| Revive positions | `Reference/PcMap/revivepos.ini` | 139 map sections / 241 coordinate rows | ✅ data / 🔄 runtime | parser preserves section `[949]` `region=1,3` with 1 real coordinate; `PcMapTravelRuntimeService` lookup proof covers default/map revive lookup; in-scene revive behavior still partial |
| Scrolls | `Reference/PcMap/scroll.txt` | 2,600 rows | ✅ data / 🔄 runtime | `PcMapTravelRuntimeService` proof preserves 2,600 value rows and no fabricated map rows; scroll item consumption/teleport UX still not fully audited |

## Missing or path-mismatch evidence

These old `✅` claims must stay downgraded until files/runtime are added and tested.

| System | Expected path/evidence | Current state | Status |
|---|---|---|---:|
| Faction maps 33 | `Reference/PcTong/faction_map.txt` | missing | ☐ |
| Special skills 58 | `Reference/PcSkill/specialskills.txt` | missing | ☐ |
| NPC/Boss skills 43 | `Reference/PcSkill/npcskills.txt` | missing | ☐ |
| Translife skills 9 | `Reference/PcSkill/translifeskill.txt` | missing; separate `PcTask/translife.txt` level table is loaded/proven | ☐ skills / ✅ level table |
| Item exchange source catalog | `Reference/PcItemExchange` | top-level `itemexchange_setting` subset present; `rolevalue_log` excluded | ✅ catalog / 🔄 runtime |
| Hongbao runtime dir | `Reference/PcHongbao` | legacy fallback only; default service path fixed to `PcItemFull/hongbao.txt` | ✅ path fixed / 🔄 runtime |
| Battlefield data dir | `Reference/PcBattlefield` | `MissionBattle/combo.txt` + `scores.txt` subset present | ✅ subset / 🔄 runtime |
| Battle script dir | `Reference/PcBattleScript` or `Reference/PcEvent/Battle` | missing | 🔄/☐ |
| Server event index | `Reference/PcServerEvent` or `Reference/PcEvent/Index` | missing | 🔄/☐ |
| VNG event dir | `Reference/PcVngEvent` or `Reference/PcEvent/Vng` | missing | 🔄/☐ |
| FlipCard dedicated dir | `Reference/PcFlipCard` | missing | 🔄 |
| Compensation dir | `Reference/PcCompensation` | missing | 🔄/☐ |

## Section-by-section truth matrix

### 1. Maps / world

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| 1,005 PC map aliases and 332 visual geometries | ✅ | `MapAliasCatalog.json`, `MapGeometryCatalog.json`, verifier pass | Keep generated artifacts reproducible on clean clone |
| Default map 907 `Vượt ải Nhiếp Thí Trần` | ✅ data/default scene | `Sandbox.unity defaultMapId: 907`; verifier default map fact; `Map907RuntimeSmokeService` proves catalog/name/geometry key | Runtime movement/minimap proof is tracked below; do not treat this row as all-map runtime parity |
| Region_C visual map art | ✅ known gaps | 95,246 Region_C; 2,785 SPR; 6 source-missing SPR refs documented | Do not fabricate missing refs |
| Server Region_S extraction | ✅ data / 🔄 gameplay | `MapSpawnCoverage.json` facts above | Spawn AI/scheduling/runtime parity |
| Region_S object catalog/action executor | 🔄 | 453 object records, 299 deterministic actions | Full object script semantics not globally proven |
| Region_S trap script resolver/executor | 🔄 partial runtime | 817/817 resolved; runtime catalog loads 804 deterministic + 13 host-limited routed actions. Batch 2 adds standalone PC semantic plan proofs for `ClearSkillTeamEnterHole`, `TongMapEntrance`, and `CityWarJoinRouter` | Verifier still reports those 13 as deferred PC-runtime gaps; close real host/API/mission/runtime gaps before broad runtime `✅` |
| Minimap/click-to-move/bounds | 🔄 service/runtime-surface proof | map 907 target clamp + minimap RectTransform offset fix/tests; `Map907RuntimeSmokeService` proves map 907 bounds, minimap roundtrip, controller clamp surface, and 16 resolved traps via Unity execute proof | Need player/in-editor feel smoke before `✅ runtime` for map 907 movement; all-map parity remains partial |
| Waypoint/wharf/revive/scroll runtime | 🔄 service proof only | exact parser/count tests plus `PcMapTravelRuntimeService`/`PcMapTravelActionService` proof cover representative waypoint 225, wharf 3 SECT data, map 949 revive, scroll 2600 value rows, and no fabricated scroll map teleport | Need end-to-end travel/revive/scroll behavior proof in runtime scene before `✅ runtime` |

### 2. Factions

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| 10 faction enum/static runtime | 🔄 | `CombatDefinition`, `CombatFactionExt`, `SkillSectCatalog` exist | Parse/verify against PC faction data; fix UI text/element mismatches |
| Faction selection UI | 🔄 | `FactionScreen` exists | PC text/layout/selection flow tests |
| Ngũ hành mapping | 🔄 | core mapping exists | Full 10-faction PC element audit; known UI/catalog mismatches |
| Chính/Tà/Trung lập alignment | 🔄/☐ | relation/framework services exist | PC alignment source-of-truth and tests |
| Faction titles 81 | ✅ data/service | `Reference/PcTitle/factiontitle.txt`, parser/service/tests exist | Visual/effect parity not fully audited |
| Faction maps 33 | ☐ | expected `faction_map.txt` missing | Add correct PC source + parser + count test |

### 3. Skills / missiles

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Base skill catalog 1,216 | ✅ data | `PcSkill/skills.txt` 1,216 rows | Full skill behavior/formula/script parity |
| Extended skills | 🔄 | local `ModSkills.txt` 1,554 rows | Import/verify PC `skills1.txt` 1,712 rows or document scope |
| Skill templates 219 | ☐/🔄 | expected source missing; parser path suspect | Copy correct PC `skilltemplate.txt`; parser/test exact 219 |
| Weapon/thief skill data | ✅ data | 32 + 4 rows | Runtime behavior tests |
| 10 faction skill sets | 🔄 | static `SkillSectCatalog` + tests | PC skill tree/script parity and catalog text fixes |
| Special/NPC/translife skills | ☐/🔄 | expected skill files missing; separate Translife level table now proven at `PcTask/translife.txt` | Add correct skill data/files; do not treat level table as `translifeskill.txt` parity |
| Skill level up | 🔄 | level/progression services exist | PC formula/Lua parity proof |
| Missile effects | 🔄 | local missile rows 441+441 | Verify correct PC missile count and effect SPR coverage |
| Skill icons/animations | 🔄 | partial visual services/tests | All-skill icon/animation provenance audit |
| Skill damage formula | 🔄 | `PcSkillDamageService` and `DamageFormulaService` partial | KNpc/KSkill parity and broader formula tests |
| Meridian 128 | ✅ data/service | `meridian_level.txt` 128 rows; service/tests | Full effect/UX parity audit |

### 4. NPCs / monsters

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| NPC templates 2,000 | ✅ data | `PcNpc/npcs.txt` 2,000 rows; full parser exists | Behavior/scripts/AI hooks |
| Region_S static spawns | ✅ data / 🔄 runtime | 67,680 NPC records; 375/375 resType sprites | AI, respawn, level scripts, combat behavior |
| `normal.txt` spawn table 5,384 | 🔄/☐ | only `normal_sample.txt` seen locally | Add/audit full table or remove claim |
| Rare/gold boss tables | ✅ data | 480 rare, 32 goldboss rows | Schedule/event behavior |
| NPC dialog | 🔄 | `NpcDialogueService` exists but hard-coded/default assumptions reported | PC `script/dailogsys` semantic execution |
| NPC level scripts 58 | 🔄 | parser/service exists | Execute PC level script semantics |
| Enemy AI | 🔄 | generic `EnemyAiService` exists | PC AI/script parity |
| NPC visual | ✅ data / 🔄 full visual | `NpcSpriteCoverage.json` 375/375 | All NPC animation/state verification |

### 5. Items / economy

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Core item/equipment data | ✅ data | exact counts in verified data table | Behavior/economy parity by subsystem |
| Magic attributes | ✅ data | 330 rows; old 333 false | Formula/application parity |
| Set bonus/enhance/refine | 🔄 | services/tests exist | PC source exact formula audit |
| Compound/recipe | ✅ data / 🔄 craft | 1,294 recipes; craft UI/execution partial | Inventory consume/result execution |
| Quest items | ✅ data/service | 2,045 quest keys | Full quest usage semantics |
| Shop/auction/stall/economy | 🔄 | data/services exist | NPC shop UX and server economy behavior parity |
| Item exchange | ✅ source catalog / 🔄 runtime | Batch 3 imports/parses top-level PC `itemexchange_setting`: normal 7,334 rows, rare 480 rows, level_exp 200 rows, level_lead_exp 100 rows, rolevalue.ini 35 keys; `rolevalue_log` excluded | Implement/verify exchange rules, inventory mutation, role value/evaluate formulas, server log/runtime side effects |
| Hongbao | ✅ data/service path / 🔄 runtime | `hongbao.txt` 69 rows parsed from `PcItemFull`; default service load points at `Reference/PcItemFull` with legacy fallback | Prove real claim flow: item grant, probability/costly/message/log semantics, inventory mutation, UI/server side effects |
| Drop rates | 🔄 | `PcDropRate` has many tables; services exist | Loot behavior parity tests |

### 6. Missions / quests

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Quest framework | 🔄 | `QuestService` exists; hard-coded/sample chains reported | PC mission quest import/execution |
| Mission script metadata | 🔄 | `MissionScriptService` is metadata/helper, not Lua VM | Implement/verify semantic executor or mark indexed only |
| Adventure entries 1,037 | ✅ data | exact row count | Runtime quest integration |
| Task/random/talk/event configs | 🔄 | config services/parsers exist | PC behavior and side-effect tests |
| Newtask/tollgate/mission arena/maze/qianchong configs | 🔄 | services exist | Full mission flow parity |
| ClearSkill mission lifecycle constants | 🔄 proof model | Batch 3 proves PC mission id/timer/camp NPC/init/end/onleave/timer constants and operation plans | Wire to real mission host executor, party APIs, scene/runtime proof |

### 7. Events / activities

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Activity/HuoYueDu services | 🔄 | services/reference config files exist | Exact PC count/source and runtime parity |
| Server events 455 | 🔄/☐ | PC has Lua; local dedicated event index dirs missing | Add index/source and semantic executor |
| VNG events/features | 🔄/☐ | expected local dirs missing | Add source/index and tests |
| Seasonal/compensation/bingo/flipcard | 🔄/☐ | services exist but dedicated dirs missing for several | Add data/runtime proof |
| Event scripts | 🔄 indexed only | `EventScriptService` metadata registry | Full Lua semantic execution |

### 8. Combat / PvP / battles

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Core combat/damage/projectile/buff/death/PK services | 🔄 | services and tests exist | PC formula/skill/PvP semantic parity |
| Tống Kim maps/rebirth traps | 🔄 | map alias counts, some rebirth traps ported; Batch 3 MissionBattle combo/scores subset proves 5 ranks and 25/25 matrix cells | Full battlefield state/scoring/join/award behavior |
| CityWar | 🔄 partial runtime/model/constants proof | `citywar.ini` data exists; `CityWarJoinRouter` routed; Batch 2 model covers join plan branches; Batch 3 constants prove PC card table, card prices, challenge token, task ids/caps/reward constants; transfer-route split model proves NPC route 222/223 vs trap join 221 | Full CityWar mission lifecycle, Tong gate host integration, inventory/life APIs, rewards, capacity gates, and live scene proof still missing |
| Quốc Chiến/Hoa Sơn | 🔄/☐ | service hints exist; dedicated source proof weak/missing | Add PC source evidence/runtime tests |
| Battle scripts 183 | 🔄 indexed/mock | `BattleScriptRuntimeService` simplified/log behavior reported; dirs missing | Full PC battle Lua semantics |
| Battle awards/double EXP | 🔄 | services exist | PC event/schedule/effect parity |

### 9. Guild / Tong / party

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Guild level data | ✅ data | 6 rows in `tong_level_data.txt` | Runtime behavior parity |
| Guild creation/fund/contribution/workshop/task/rank/stunt | 🔄 | services/configs exist | PC server behavior and script side effects |
| Guild scripts 65 | 🔄 indexed/framework | PC `script/tong` Lua not executed; service mostly validate/log/return | Semantic Lua execution or explicit indexed-only status |
| Guild city war / Tong maps | 🔄 partial hooks/model/constants proof | `TongMapEntrance=8` routed as host-limited hook with Batch 2 default/cn_ib branch model; `CityWarJoinRouter=1` routed/tested as partial runtime/model proof; Batch 3 adds CityWar card/token constants and transfer-route split | Implement/prove PC Tong ownership/ban/expire/product-region/template-map APIs, CityWar gate integration, and Unity runtime behavior before `✅` |
| Party system | 🔄 | party services/panels exist | Full PC team behavior, team trap entry, UI proof |

### 10. Other systems

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Activity, partner/pet, title, shop, stall, foundry, lottery, flip card, honor, shitu, bonus, trip, guide, world rank, city defence | 🔄 | many services/tests exist | Row-by-row PC source/count/runtime parity not fully audited |
| Weather/music/audio | 🔄 | services/config parsers exist | Map-specific PC parity and runtime proof |
| GM tools | 🔄 excluded | user-owned GM teleport/browser changes are out of audit scope | Do not use as port completion proof without explicit user request |
| Dialog system | 🔄 | service exists | PC dialog Lua execution |

### 11. Player/NPC visuals and SPR runtime

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| SPR decoder/runtime/atlas core | ✅ core | `SprRuntimeService`, `SprDecoder`, `SprAtlasPacker`, tests exist | Direct PC PAK runtime loading not globally audited |
| Male/female player visual | 🔄 | visual classes/catalog JSON/tests exist | Full PC equipment/layer/hair/weapon/action coverage |
| Mount/horse visual | 🔄 | horse/mount services/catalogs/tests exist | Full palette/animation parity |
| NPC visual | ✅ staged data / 🔄 runtime | `NpcSpriteCoverage` 375/375 | Full animation/state parity |

### 12. Client / UI

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Mobile HUD baseline | 🔄 | committed services/tests exist; current HUD files dirty and excluded | Rerun tests/smoke after human HUD WIP is resolved |
| HUD PC art | 🔄 | partial PC-derived subset exists | Not 100% PC HUD art/layout parity; verify asset provenance row-by-row |
| Minimap/quest/inventory/map/chat/party/faction/shop panels | 🔄 | panels/services exist | Full PC behavior/UI parity not globally proven |
| Touch input/camera rig | 🔄 | services exist | runtime/mobile tests needed |
| Client skill scripts 722 | 🔄 indexed | service/tests exist | semantic script execution not proven |
| Vietnamese text | 🔄 | many labels exist | full localization audit missing |

### 13. Infrastructure / network / server-side

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Network protocol DTO/router | ✅ narrow | `NetworkMessageTypes.cs`, `MessageRouter.cs`, `NetworkProtocolTests.cs`; old 46 opcodes claim has code evidence | Integration with real server not audited |
| Level/EXP | 🔄 | `PlayerLevelService` exists | PC formula/config parity proof |
| Resource SPR loading | ✅ core / 🔄 PAK | SPR decoder/runtime tested | Full live PAK loading/provenance not globally audited |
| Gateway/Bishop/S3Relay/Docker/DB/PaySys/backup | ☐ | server-side/outside Unity client | Requires server project/scope |

### 14. GBK script directories

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| GBK area/town/faction/tong script registries | 🔄 indexed | `AreaScriptService`, `GbkMapScriptService`, `TownScriptService`, `FactionQuestAreaService`, `TongBattleScriptService` exist | Semantic script execution not proven for the 9 dirs/counts |

### 15. Server scripts

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Library/activity/mission/global/item/skill/event/task/battle/guild/VNG scripts | 🔄 indexed/cataloged | service/parser classes exist; many old counts are metadata, not executable proof | Full Lua semantic runtime still incomplete |
| Region_S trap Lua subset | 🔄 partial runtime | 804 deterministic trap actions plus 13 Batch 1 host-limited routed actions; verifier still lists the 13 as deferred PC-runtime gaps; 0 unclassified | Broaden semantic executor/host APIs and mission/Tong runtime before any runtime `✅` |

## Old false-confidence claims now invalid

Never restore these without stronger proof:

- `Tổng thể ~100%`.
- `1771/1771 EditMode` or `25/25 PlayMode` without fresh Unity Test Runner artifact.
- Section 15 server scripts as `✅` complete.
- Section 14 GBK dirs as semantic `✅`.
- Mission/event/battle/guild scripts as full runtime `✅` merely because metadata services exist.
- Faction maps 33 `✅` while `faction_map.txt` is missing.
- Extended skills 1,712 `✅` while local data is 1,554 rows and PC `skills1.txt` full import is unproven.
- Skill templates 219, special skills 58, NPC skills 43, translife skills 9 as `✅` while expected source files are missing.
- Item exchange and hongbao runtime as `✅` while expected runtime dirs are missing/path-mismatched.
- HUD/UI/GM teleport claims based on dirty or user-owned changes.

## Completion criteria before future `✅`

For each future status row, cite at least one of:

1. Exact PC file path and exact parsed count, with current file present in `Assets/StreamingAssets/Reference` or generated catalog.
2. Runtime code path that consumes the data.
3. Test/verifier that asserts the exact count and one representative behavior.
4. For Lua/scripts: semantic side effects implemented and tested, not just script name/function metadata.
5. For visual/UI: exact PC asset provenance and rendered/smoke proof.

## Immediate map-port priorities

1. Map 907 movement bounds/minimap/click-to-move: target clamp/minimap fixes plus `Map907RuntimeSmokeService` prove catalog/bounds/minimap/controller-clamp/trap readiness; still needs player/in-editor feel smoke before marking runtime `✅`.
2. Continue deferred trap family deepening: Batch 2 added standalone PC semantic plan proofs for `ClearSkillTeamEnterHole`, `TongMapEntrance`, and `CityWarJoinRouter`; keep them `🔄 partial runtime/model proof` until verifier + host/API/mission gaps are closed.
3. Waypoint/wharf/revive/scroll parser/count/action-service proofs are added; still needs end-to-end travel/revive/scroll runtime proof.
4. Keep HUD/GM teleport out of map-port commits unless user explicitly asks.

## Port Factory Batch 1 audit queue — 2026-06-09

Integrated Batch 1 evidence:

- `d960ca7 chore: scaffold trap action port hooks` was followed by worker patches for the scoped families plus main integration wiring.
- `PcTrapActionCatalogRuntime.LoadFromStreamingAssets()` now augments `MapTrapActionCatalog.json` with the 13 host-limited deferred entries from `MapTrapScriptCatalog.json`, so loaded runtime catalog count is 817 (`804` deterministic + `13` routed partial actions).
- `PcMapTravelRuntimeService` adds service-level lookup proof for PC waypoint/wharf/revive/scroll data.
- Unity compile is clean; Unity Editor execute proof returned `catalog=817 tong=8 clear=4 city=1 travel=225/11/241/2600`. Unity Test Runner discovery returned 0 tests for this project/assembly, so this is not claimed as a Test Runner artifact.
- `jx_map_port_verify.py --include-missing-spr-region-refs --pretty` passes with known map-SPR gaps and still reports the 13 deferred family counts, because the verifier tracks PC-runtime semantic gaps rather than Unity loader augmentation.
- Therefore no broad `✅ runtime` row is upgraded by Batch 1. Conservative status remains `✅ data / 🔄 runtime` or `🔄 partial hook/runtime`.

Conservative row update queue:

| Batch 1 scope | PORT_STATUS row(s) to revisit after integration | Required proof before changing status |
|---|---|---|
| `ClearSkillTeamEnterHole` | Deferred trap family list; Region_S trap Lua subset | Routed as `🔄 partial runtime`; need captain/team validation, `CSP_CheckValid`, mission/free-map allocation, `OpenMission/RunMission/AddMSPlayer`, `SetTempRevPos`, and Test Runner/runtime proof before `✅`. |
| `TongMapEntrance` | Deferred trap family list; Guild city war / Tong maps; Region_S trap Lua subset | Routed as host-limited `🔄 partial hook`; need PC host APIs for product region, map params, Tong ownership/ban/template/expire semantics and runtime proof before `✅`. |
| `CityWarJoinRouter` | Deferred trap family list; City war config; Region_S trap Lua subset | Routed as `🔄 partial runtime`; need full mission lifecycle, Tong gate/current-map integration, card expiry, rewards, and runtime proof before `✅`. |
| Travel runtime proof | Waypoints, Wharves, Revive positions, Scroll items | Service lookup proof added; still needs end-to-end travel/revive/scroll behavior proof in runtime scene before `✅ runtime`. |

## Port Factory Batch 2 integration — 2026-06-09

Integrated Batch 2 evidence:

- Service/model proof commits integrated for travel action lookup, ClearSkill TeamEnterHole/JoinHole, TongMapEntrance, CityWarJoinRouter, map 907 runtime-surface smoke, and Test Runner discovery diagnosis.
- Unity compile is clean except known Addressables GUID conflict noise. Unity Editor execute proof returned `travel wp225=340/1853/3446 wharf3=2/DataOnly revive949=51264/102368 scroll=2600/DataOnly; clear=True/249/2; tong=cn_ib-banned-non-owner/3; city=True/True/1; map907=True/16/0`.
- Non-Unity verification passed: `git diff --check`, `python3 scripts/jx_map_port_verify.py --include-missing-spr-region-refs --pretty` (`pass_with_known_gaps`), and `cargo test -p harness-cli` (`20 passed`).
- Unity Test Runner still has no fresh passing artifact; root cause is the missing `VLTK_ENABLE_TESTS` define constraint. Do not reuse old `1771/1771` or `25/25` claims.
- Batch 2 does not promote broad rows to `✅ runtime`: proofs are service/model/runtime-surface only until live host/API/scene behavior is exercised.

Conservative Batch 2 queue:

| Batch 2 scope | Current evidence | Still required before `✅ runtime` |
|---|---|---|
| Travel action lookup | `PcMapTravelActionService` proves waypoint 225, wharf 3 data-only SECT, revive map 949, scroll 2600 value table/no fabricated teleport | In-scene waypoint/wharf/revive/scroll behavior, UI/cost/item-consumption restrictions |
| ClearSkill TeamEnterHole | PC constants and deterministic plan for captain/CSP/team size/clear map/free test map/OpenMission/RunMission/JoinHole side effects | Real party host, mission allocation, AddMSPlayer/SetTempRevPos execution, scene/runtime proof |
| TongMapEntrance | PC default/cn_ib plan for banned, expired, near-expiry, SetFightState/SetPos/message branches | Real product-region/map-param/Tong owner/ban/template/expire host APIs and runtime proof |
| CityWarJoinRouter | PC plan for mission state, existing ticket, card/no-card/expired-card, Tong direct join, camp join side effects | Full mission lifecycle, Tong gate host integration, item inventory/life APIs, rewards/capacity, runtime proof |
| Map 907 smoke | catalog/name/geometry/bounds/minimap roundtrip/controller-clamp/trap readiness proof | Player/in-editor movement feel smoke and broader all-map parity |
| Test Runner | root cause diagnosed; isolated `PortFactorySmoke` asmdef added and passed 2/2 EditMode | Keep full stale root test asmdefs gated until they compile; expand isolated smoke coverage without claiming full-suite health |

## Port Factory Batch 3 integration — 2026-06-09

Integrated Batch 3 evidence:

- Data/service proof commits integrated for Translife level table, Hongbao PC schema/path, ItemExchange source catalog, MissionBattle combo/scores, CityWar card/token constants, ClearSkill lifecycle constants, CityWar transfer-route split, and isolated Test Runner smoke.
- Unity compile is clean except known Addressables GUID conflict noise. Unity Editor execute proof returned `translife=41; hongbao=69; itemex=7334/480/200/100/35; battle=5/25/25; clearskill=5/4; citycards=14/def1=362; route=True/222/1/2`.
- Unity Test Runner scoped artifact: `PortFactorySmoke` EditMode job `5d7958b697ad4565aab8a1e409430c05` succeeded with `2 passed / 0 failed / 0 skipped`. This proves isolated runner discovery only; stale root test asmdefs remain gated by `VLTK_ENABLE_TESTS`.
- Non-Unity verification passed: `git diff --check`, `python3 scripts/jx_map_port_verify.py --include-missing-spr-region-refs --pretty` (`pass_with_known_gaps`), and `cargo test -p harness-cli` (`20 passed`).
- Batch 3 does not promote broad rows to `✅ runtime`: proofs are data/service/model/test-smoke only until live host/API/scene behavior is exercised.

Conservative Batch 3 queue:

| Batch 3 scope | Current evidence | Still required before `✅ runtime` |
|---|---|---|
| Translife level table | PC `Client 6.0/settings/task/metempsychosis/translife.txt` imported as 41 levels 160..200 | Do not conflate with missing `translifeskill.txt`; prove skill unlock/effect application runtime |
| Hongbao | PC `settings/item/hongbao.txt` 69 rows parsed via default `PcItemFull` service path | Weighted random select, inventory preflight, Type 1/2 reward grant, costly message/log semantics, UI/server side effects |
| CityWar constants/routes | PC card table/token/task constants and transfer maps 222/223 to camp join 221 modeled | Full mission lifecycle, Tong host APIs, inventory/life APIs, rewards/capacity/live scene proof |
| ItemExchange | PC `itemexchange_setting` source catalog imported/parses top-level tables | Exchange formulas/rules, inventory mutation, role value/evaluate, logs/runtime side effects |
| MissionBattle combo/scores | PC `combo.txt` + `scores.txt` service proves 5 ranks and 25/25 cells | Runtime scoring integration, kill/death awards, mission lifecycle |
| ClearSkill lifecycle | PC mission timer/camp NPC/init/end/onleave/timer operation model proven | Wire to real mission host executor and scene/team runtime |
| Test Runner | Isolated `PortFactorySmoke` EditMode 2/2 passed | Keep full legacy tests gated until compile errors are fixed; expand isolated smoke slices gradually |

## Harness DB sync audit — 2026-06-08

Command evidence:

```bash
scripts/harness query stats
scripts/harness query sql "select status,count(*) from story group by status;"
scripts/harness query sql "select sum(unit_proof),sum(integration_proof),sum(e2e_proof),sum(platform_proof),count(*) from story;"
scripts/harness query traces
scripts/harness query backlog --open
```

Current Harness DB facts:

```text
stats: intakes=15, stories=32, decisions=6, backlog_items=2 before adding sync backlog, traces=41
story status counts: implemented=32
story proof flags: unit=32, integration=32, e2e=0, platform=0, total=32
latest traces: ST-06.1 HUD work at trace #40/#41; map 389 traces #38/#39 have no story_id
open backlog after audit: #2 Align root harness wrapper..., #3 Align Harness matrix semantics with PORT_STATUS truth matrix
```

Sync verdict:

- **Not semantically synced** if Harness matrix `implemented` is read as “full PC port complete”. It is a historical story-slice/test matrix.
- `PORT_STATUS.md` remains the port-completion authority. It intentionally marks many domains `🔄/☐` even when Harness story rows are `implemented`, because story rows prove older implementation slices, not full PC data/visual/Lua parity.
- Do **not** use `scripts/harness query matrix` alone to claim port completeness or `% complete`.
- Backlog #3 records the required Harness follow-up: align matrix semantics with this truth matrix or add explicit story notes/status categories distinguishing “story slice implemented” from “PC parity complete”.
