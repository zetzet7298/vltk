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
- No fresh Unity Test Runner result artifact found. Old `1771/1771 EditMode` and `25/25 PlayMode` claims are **unverified** and removed.
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
trap scripts/actions: 817/817 script ids resolved, 804 deterministic trap actions, 13 deferred resolved actions, 0 unclassified
default map: 907 — Vượt ải Nhiếp Thí Trần
```

Deferred trap families still not complete:

```text
ClearSkillTeamEnterHole: 4
TongMapEntrance: 8
CityWarJoinRouter: 1
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
| Gold equip | `Reference/PcItemFull/goldequip.txt` | 5,346 rows | ✅ | data catalog |
| Platina equip | `Reference/PcItemFull/platinaequip.txt` | 5,336 rows | ✅ | data catalog |
| Armor/helm/boot/cuff/belt/ring/amulet/pendant | `PcItemFull/*.txt` | 290/140/40/20/20/10/20/20 | ✅ | data catalog |
| Melee/range/horse/potion | `PcItemFull/*.txt` | 60/30/350/40 | ✅ | data catalog |
| Magic attributes | `Reference/PcItemFull/magicattrib.txt` | 330 rows | ✅ | old 333 claim false |
| Compound recipes | `Reference/PcItemFull/atlas_compound.txt` | 1,294 rows | ✅ data / 🔄 craft | craft execution/UI still partial |
| Quest keys | `Reference/PcItemFull/questkey.txt` | 2,045 rows | ✅ | data/service |
| Hongbao data | `Reference/PcItemFull/hongbao.txt` | 69 rows | ✅ data / 🔄 runtime path | `Reference/PcHongbao` missing |
| Shop goods/buysell | `Reference/PcShop/goods.txt`, `buysell.txt` | 1,521 + 165 rows | ✅ | catalog; NPC shop UX not fully audited |
| Lottery | `Reference/PcLottery/lottery.txt` | 254 rows | ✅ | data/service |
| Adventure | `Reference/PcAdventure/adventure.txt` | 1,037 rows | ✅ | catalog; quest semantics partial |
| Player task defs | `Reference/PcMission/player_task_def.txt` | 656 rows | ✅ data | mission execution partial |
| Guild levels | `Reference/PcTong/tong_level_data.txt` | 6 rows | ✅ | guild scripts partial |
| City war config | `Reference/PcEvent/citywar.ini` | 90 data lines | ✅ data | join/router semantics partial |
| Waypoints | `Reference/PcMap/waypoint.txt` | 225 rows | 🔄 | old mobile 224 mismatch unresolved |
| Wharves | `Reference/PcMap/wharf.txt` | 11 rows | 🔄 | old mobile 10 mismatch unresolved |
| Revive positions | `Reference/PcMap/revivepos.ini` | 656 lines / 241 verifier revive entries previously claimed | 🔄 | exact runtime count needs dedicated proof |
| Scrolls | `Reference/PcMap/scroll.txt` | 2,600 rows | ✅ data | runtime behavior not fully audited |

## Missing or path-mismatch evidence

These old `✅` claims must stay downgraded until files/runtime are added and tested.

| System | Expected path/evidence | Current state | Status |
|---|---|---|---:|
| Faction maps 33 | `Reference/PcTong/faction_map.txt` | missing | ☐ |
| Special skills 58 | `Reference/PcSkill/specialskills.txt` | missing | ☐ |
| NPC/Boss skills 43 | `Reference/PcSkill/npcskills.txt` | missing | ☐ |
| Translife skills 9 | `Reference/PcSkill/translifeskill.txt` | missing; `PcTask/translife.txt` exists but service does not load it by default | ☐/🔄 |
| Item exchange | `Reference/PcItemExchange` | missing | ☐/🔄 framework only |
| Hongbao runtime dir | `Reference/PcHongbao` | missing; data exists under `PcItemFull/hongbao.txt` | 🔄 |
| Battlefield data dir | `Reference/PcBattlefield` | missing | 🔄/☐ |
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
| Default map 907 `Vượt ải Nhiếp Thí Trần` | ✅ | `Sandbox.unity defaultMapId: 907`; verifier default map fact | Manual movement/bounds still needs fix/proof |
| Region_C visual map art | ✅ known gaps | 95,246 Region_C; 2,785 SPR; 6 source-missing SPR refs documented | Do not fabricate missing refs |
| Server Region_S extraction | ✅ data / 🔄 gameplay | `MapSpawnCoverage.json` facts above | Spawn AI/scheduling/runtime parity |
| Region_S object catalog/action executor | 🔄 | 453 object records, 299 deterministic actions | Full object script semantics not globally proven |
| Region_S trap script resolver/executor | 🔄 | 817/817 resolved, 804 deterministic, 13 deferred | Port ClearSkill/Tong/CityWar deferred families |
| Minimap/click-to-move/bounds | 🔄 | services exist; user reported early blocking on 907 | Need reproduce/fix/verify full map 907 movement |
| Waypoint/wharf/revive/scroll runtime | 🔄 | reference data exists; mismatches for waypoint/wharf | Dedicated count parity tests + skipped row reasons |

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
| Special/NPC/translife skills | ☐/🔄 | expected files missing/default service counts likely 0 | Add correct data/files or downgrade permanently |
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
| Item exchange | ☐/🔄 | default data dir missing | Add source data/runtime |
| Hongbao | ✅ data / 🔄 runtime | data exists; default runtime dir missing | Fix service path/runtime tests |
| Drop rates | 🔄 | `PcDropRate` has many tables; services exist | Loot behavior parity tests |

### 6. Missions / quests

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Quest framework | 🔄 | `QuestService` exists; hard-coded/sample chains reported | PC mission quest import/execution |
| Mission script metadata | 🔄 | `MissionScriptService` is metadata/helper, not Lua VM | Implement/verify semantic executor or mark indexed only |
| Adventure entries 1,037 | ✅ data | exact row count | Runtime quest integration |
| Task/random/talk/event configs | 🔄 | config services/parsers exist | PC behavior and side-effect tests |
| Newtask/tollgate/mission arena/maze/qianchong configs | 🔄 | services exist | Full mission flow parity |

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
| Tống Kim maps/rebirth traps | 🔄 | map alias counts, some rebirth traps ported | Full battlefield state/scoring/join/award behavior |
| CityWar | 🔄 | config/service exists; `CityWarJoinRouter` deferred | Join card, Tong gates, mission state, rewards |
| Quốc Chiến/Hoa Sơn | 🔄/☐ | service hints exist; dedicated source proof weak/missing | Add PC source evidence/runtime tests |
| Battle scripts 183 | 🔄 indexed/mock | `BattleScriptRuntimeService` simplified/log behavior reported; dirs missing | Full PC battle Lua semantics |
| Battle awards/double EXP | 🔄 | services exist | PC event/schedule/effect parity |

### 9. Guild / Tong / party

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Guild level data | ✅ data | 6 rows in `tong_level_data.txt` | Runtime behavior parity |
| Guild creation/fund/contribution/workshop/task/rank/stunt | 🔄 | services/configs exist | PC server behavior and script side effects |
| Guild scripts 65 | 🔄 indexed/framework | PC `script/tong` Lua not executed; service mostly validate/log/return | Semantic Lua execution or explicit indexed-only status |
| Guild city war / Tong maps | 🔄 | `TongMapEntrance=8` and `CityWarJoinRouter=1` deferred | Tong ownership/ban/expire/enter-pos APIs |
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
| Region_S trap Lua subset | 🔄 partial runtime | 804/817 resolved trap actions deterministic; 13 deferred; 0 unclassified | Finish deferred families and broaden semantic executor as needed |

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

1. Fix/verify map 907 movement bounds/minimap/click-to-move after user reported early blocking.
2. Port remaining resolved deferred trap families from PC source: `ClearSkillTeamEnterHole`, then `TongMapEntrance`, then `CityWarJoinRouter`.
3. Add exact count tests for waypoint/wharf/revive mismatches.
4. Keep HUD/GM teleport out of map-port commits unless user explicitly asks.

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
