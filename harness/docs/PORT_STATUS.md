# PORT_STATUS.md — Evidence Audit PC → Mobile

> **Audit date**: 2026-06-09; source-of-truth refresh: 2026-06-10
> **PC source of truth**: loose source under `/var/www/vltksource_new/vl_update_27` **plus** canonical unpacked PAK tree `/var/www/vltksource_new/vl_update_27/pak_unpacked`
> **Unpack manifest**: `/var/www/vltksource_new/vl_update_27/pak_unpacked/_unpack_summary.json` — read live, do not bake counts. Current manifest (re-verified 2026-06-12): `pak_count=46`, `total_entries=403560`, `total_exported=403560`, `total_failed=0`, `partial=0`, all 46 items `status=ok` (incl. `dmjx01.pak` 1621/1621). The earlier "401,281/401,640, 357 undecoded `0x11000000`" line was a pre-repair snapshot and is now stale: method `0x11000000` (352 entries) is raw-SPR stored-as-is (byte-copied, not undecoded), and the 5 `dmjx01` `0x10000000` fragment-table entries were repaired. Method distribution across all 46 paks: `0x20`=252000, `0x01`=149697, `0x00`=1506, `0x11`=352, `0x10`=5 (binary-verified vs engine.dll/represent3.dll 2026-06-12).
> **Mobile repo**: `/var/www/vltk-mobile` (`dev` at `0480502` when audit started; source-truth alignment commits: `0638487`, `794f7ca`)
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

**Kanban provenance warning:** board `vltk-port-wave1` is historical/pre-`pak_unpacked` migration. Its 30 audit outputs and blocked implementation tasks may be useful as hints, but they are not sufficient evidence after the 2026-06-10 source-of-truth refresh unless rechecked against `/var/www/vltksource_new/vl_update_27/pak_unpacked`. Board `vltk-port` is abandoned/corrupt. New work should use clean board `vltk-port-pakunpacked` and update this file only with fresh evidence.

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
- Batch 4 local integration adds pure model/service/catalog proofs for Translife bonus lookup/diff, Hongbao weighted-open command surface, City Hongbao schema, MissionBattle scoring lookup, ClearSkill lifecycle plan replay, CityWar challenge-token turn-in, ItemExchange rolevalue facts, NPC/Boss `skills1.txt` subset catalog, Tong/faction map catalog, and expanded `PortFactorySmoke`. Current Unity execute proof returned `hongbao=69/1000000/t42+27/costly15/log69; hbType1=1/AddItem/True; hbType2=2/AddGoldItem/True; hbBlocked=InsufficientInventorySpace/None; cityHongbao=67/1010000/4681/10000; battle=5/25/25/idx1:1:75; translife=41/29/7/d15:80:4:1; clearskillInit=True/5/4321; clearskillLeave=True/8/CloseMission; rolevalue=4/35/5000/27/400-4000/True; npcskills=158/158/145/21/13/54/64; tongFactionMap=33/7/11/1712:3330/329:1561:2942`.
- Batch 5 local integration adds source catalogs/models for FlipCard protocol, City Hongbao weighted-open command surface, PC `skilltemplate.txt` schema, PC battle script file index, server event file index, ItemExchange normal/rare typed lookups, Translife skill source 9-row subset, Tong map enter plan, NPC skill script availability index, and Special skill script 576-row subset. Current Unity execute proof returned `flip=6; cityOpen=67/1010000/13+54/67; skilltemplate=67/318/220; battleScripts=183/182/1/10; serverEvents=455/427/28; itemLookup=78/7334/29/480/False; translifeSkill=9; tongEnter=33/11/7/True/591:1712:3330; npcSkillScripts=158/145/49/42/7; special=576/575/84`. Scoped `PortFactorySmoke` passed `10/10`; full legacy test asmdefs remain gated/stale.
- Exa research timed out twice; DeepWiki fetched UnityCsReference overview. This audit relies on current repo files and PC source-of-truth, not external guesses.
- **Integration batch 2026-06-12 (vltk-unity, single-Editor oracle, instance `vltk-mobile@244c0d539f780309`, Unity 6000.4.7f1):** merged 3 offline Tier0 data-loader fix branches into `dev` (`--no-ff`), each recompiled in the live Editor (0 new CS errors) and proven by real EditMode Test Runner artifacts:
  - `port/fix-maplist-ini` (Backlog #16) → `PcMapListFullParser` TSV→INI rewrite; `MapDataParserTests`+`MapDataServiceTests` green within job `8d4ddbeb7b424fdc8fd3ee76d2dc4776` (61/61, 0 failed).
  - `port/fix-horse-columns` (Backlog #15) → `PcHorseParser` col0=Name off-by-one + `IntegrationTests` key fix; same job `8d4ddbeb7b424fdc8fd3ee76d2dc4776` 61/61 passed — `IntegrationTests.Test_HorseService_Plus_MountService_Workflow` now passes (was horse valid=0).
  - `port/fix-rare-schema` (Backlog #14) → `rare.txt` re-modeled as 29-col weapon-enchant table (renamed PcRareSpawnParser→PcRareEnchantParser, RareSpawnService→RareEnchantService); `PcRareEnchantParserTests` job `b630f10b67fb4d7c92fb905ea3e06d3f` = 7/7 passed.
  - **Rejected:** `port/fix-meridian-128` (Backlog #6) — compiled clean but FAILED its own `MeridianServiceTests` job `5c6e89a582244d7f886afde3b767fb5d` (8/13 passed, 5 failed: registry yields 118 acupoints across 13 meridians, not the claimed 128 across 8×16). Reverted via `git reset --hard` to the pre-merge HEAD; #6 stays red. Composite-key refactor is incomplete.
  - Full-suite EditMode run on the 3-merge `dev` (job `7b41122c36174c0c87b89f680c71a098`, 2284 tests) shows only pre-existing known-red backlog failures (#13 visual SPR part-count, #11 SPR path-UID, #7 TitlePanel stub, #6 Meridian, GameHud/SandboxManager runtime NREs) — none in the merged parser/service files, so no regression introduced by this integration.
- **Integration batch 2026-06-12 #2 (vltk-unity, same single-Editor oracle `vltk-mobile@244c0d539f780309`, Unity 6000.4.7f1):** merged 2 of 3 offline fix branches into `dev` (`--no-ff`), each recompiled in the live Editor (0 new CS errors) and proven by real EditMode Test Runner artifacts:
  - `port/fix-meridian-tcvn3` (Backlog #6) → `PcMeridianParser` now reads `meridian_level.txt` via `ReadLinesTcvn3` (TCVN3, not GBK auto-detect) + composite `(meridian,level)` key preserving all 128 acupoints; `MeridianServiceTests` job `cb900328a5464313a6868c2bc0eb28a3` = **13/13 passed, 0 failed** (was 8/13 with the earlier `port/fix-meridian-128` attempt). **#6 now GREEN.**
  - `port/fix-test-drift-counts` (Backlog #10 partial) → comment-only correction of the fabricated "1,521 cửa hàng" note on `PcShopRegistryTests.LoadFromStreamingAssets_LoadsShops` (buysell.txt ships 166 lines ≈ 159 shops; threshold `>=100` unchanged); `PcShopRegistryTests` job `c798e541b29c428db848c3e656e16678` = **4/4 passed, 0 failed.**
  - **Rejected:** `port/fix-title-loader` (Tier1 #7) — merged clean and compiled clean (0 CS errors) but FAILED 5/42 of its OWN new EditMode tests (job `512964cd1bbf422582c61097d7913bd4`): `PcPlayerTitleParser` decodes playertitle.txt as GB2312/mojibake not TCVN3 ("Binh s圻" vs "Binh sĩ") and loads only 335/363 entries; `MeridianPanelService.GetPcMeridianOrder` returns empty; `DailyTaskPanelService` snapshot wiring returns False. Reverted via `git reset --hard` to post-meridian HEAD; #7 stays red. Sent fix list back to source card `t_38f6bd23`.
- **Integration batch 2026-06-12 #3 (vltk-unity, same single-Editor oracle `vltk-mobile@244c0d539f780309`, Unity 6000.4.7f1):** merged 2 of 2 offline fix branches into `dev` (`--no-ff`), each recompiled in the live Editor (0 new CS errors) and proven by real EditMode Test Runner artifacts. Final `dev` HEAD `537eb8772`.
  - `port/fix-title-tcvn3` (Tier1 #7-redo) → `PcPlayerTitleParser`/`PcFactionTitleParser` now read `playertitle.txt`/`factiontitle.txt` via `ReadLinesTcvn3` (TCVN3, not GBK auto-detect); `TitlePanelService` implemented from real PC title data. `TitleServiceTests` + `UIPanelServiceTests` job `fdee06b999e848c69bbc9dcc8c45e498` = **8/8 passed, 0 failed** (was 5/42 fail: 335/363 + mojibake). **#7 now GREEN.**
  - `port/audit-decode-systemic` (systemic #12) → forces `ReadLinesTcvn3` on `PcStringResourceParser` (stringresource.txt 5645), `PcNpcSFullParser` (npcs.txt 2001), `PcRankSettingParser` (ranksetting.txt 90). `StringResourceCatalogServiceTests` + `PcNpcSFullParserTests` + `RankSettingServiceTests` job `a0534a991f0647288ac258890dc29a84` = **6/6 passed, 0 failed.** **#12 GBK→TCVN3 mojibake closed for these 3 high-impact parsers.**

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
| Rare weapon-enchant | `Reference/PcNpc/rare.txt` | 29-col table | ✅ schema/parser/service (2026-06-12) | `rare.txt` re-modeled as weapon-enchant table (NOT NpcS spawn); `PcRareEnchantParser`/`RareEnchantService`; `PcRareEnchantParserTests` 7/7 (Unity job `b630f10b67fb4d7c92fb905ea3e06d3f`). 🔄 enchant-effect runtime parity not audited. Backlog #14 closed |
| Gold bosses | `Reference/PcNpc/goldboss.txt` | 32 rows | ✅ | 🔄 event/schedule behavior not fully audited |
| Base skills | `Reference/PcSkill/skills.txt` | 1,216 rows | ✅ | 🔄 skill behavior/formulas/scripts partial |
| Weapon skills | `Reference/PcSkill/clientweaponskill.txt` | 32 rows | ✅ | 🔄 behavior not audited |
| Special skill script catalog | `Reference/PcSkill/specialskills.txt` | 576 rows / 575 unique skill ids / 84 scripts | ✅ catalog / 🔄 runtime | Derived from PC `skills1.txt` `LvlSetScript` prefix `\\script\\skill\\special`; old standalone 58-row claim not source-proven |
| Translife skill source | `Reference/PcSkill/translifeskill.txt` | 9 rows | ✅ source catalog / 🔄 runtime | Derived from PC `skills.txt` `translife4th.lua` rows; do not conflate with `PcTask/translife.txt` level bonus table |
| Skill template schema | `Reference/PcSkill/skilltemplate.txt` | 67 fields / 220 non-empty lines | ✅ schema catalog / 🔄 runtime | PC `skilltemplate.txt` is schema metadata; old 219-row template claim not source-proven |
| NPC/Boss skill catalog | `Reference/PcSkill/npcskills.txt` | 158 rows / 145 NPC-script / 21 boss-name | ✅ catalog / 🔄 runtime | Derived from PC `skills1.txt`; old standalone 43-row claim not source-proven |
| Thief skills | `Reference/PcSkill/thiefskill.txt` | 4 rows | ✅ | 🔄 behavior not audited |
| PC Skills1 full catalog | `Reference/PcSkill/Skills1FullCatalog.json` | 1,712 rows | ✅ catalog | Full 1712 rows PC catalog parsed; combat effects/hit zones not decoded |
| Missiles catalog | `Reference/PcMissilesParserEvidence.json` + `Reference/PcMissileSourceAudit.json` | PC `missles1.txt`: 467 rows / 466 unique ids; runtime loader: 466 unique ids | ✅ catalog / 🔄 runtime | `PcMissileRegistry` now prefers full PC `Reference/PcAttrib/missles1.txt`, with duplicate id 408 last-row-wins and ids 442/443/467 resolved. Speed/lifetime proof is representative only; effect SPR rendering and combat side-effects are not full parity |
| Meridian levels | `Reference/PcMeridian/meridian_level.txt` | 128 rows | ✅ | 🔄 full UX/effect parity not audited |
| Translife level table | `Reference/PcTask/translife.txt` | 41 rows / levels 160..200 | ✅ data/service lookup | Batch 4 adds pure 7-group bonus lookup/diff proof; skill unlock/effect application runtime not proven |
| Gold equip | `Reference/PcItemFull/goldequip.txt` | 5,346 rows | ✅ | data catalog |
| Platina equip | `Reference/PcItemFull/platinaequip.txt` | 5,336 rows | ✅ | data catalog |
| Armor/helm/boot/cuff/belt/ring/amulet/pendant | `PcItemFull/*.txt` | 290/140/40/20/20/10/20/20 | ✅ | data catalog |
| Melee/range/horse/potion | `PcItemFull/*.txt` | 60/30/350/40 | ✅ | data catalog |
| Magic attributes | `Reference/PcItemFull/magicattrib.txt` | 330 rows | ✅ | old 333 claim false |
| Compound recipes | `Reference/PcItemFull/atlas_compound.txt` | 1,294 rows | ✅ data / 🔄 craft-plan | atlas_compound parser now preserves 1,294 rows and PC `atlas.lua` material validation/result shape; `CompoundRecipeService` builds a host-op craft plan (100000 Pay, RNG by source/destination item value, WriteCompoundLog/Remove/AddItemEx operations). Inventory/UI/server side effects still partial. |
| Quest keys | `Reference/PcItemFull/questkey.txt` | 2,045 rows | ✅ | data/service |
| Hongbao data | `Reference/PcItemFull/hongbao.txt` | 69 rows | ✅ data/open-model path / 🔄 runtime | Batch 4 weighted-open model proves PC raw weights. `HongbaoRuntimeBehaviorService` wires a subset of reward commands to `InventoryService`, but item consumption, logs, global news, UI, and server side effects are not proven full PC runtime. |
| City Hongbao data | `Reference/PcItemFull/chengshidahongbao.txt` | 67 rows / total weight 1,010,000 | ✅ data/reward-command path / 🔄 runtime | Full PC schema parsed and type 1/2 reward command path exists, but item consumption/log/news/UI/server side effects are not proven full PC runtime. |
| Item exchange source catalog | `Reference/PcItemExchange` | 7,334 normal / 480 rare / 200 level_exp / 100 level_lead_exp / 35 rolevalue keys | ✅ catalog+rolevalue+lookup facts / 🔄 runtime plan | Batch 4 exposes rolevalue typed facts; Batch 5 adds typed normal/rare table lookups (78/29 headers, 7,334/480 rows); current implementation adds representative host-command plans for `exchange_olditem`, `exchange_lingpai`, and `jinglianshixiang` PutIn. Real inventory host execution/economics/log persistence/UI side effects are not proven full PC runtime |
| Shop goods/buysell | `Reference/PcShop/goods.txt`, `buysell.txt` | 1,521 + 165 rows | ✅ | catalog; NPC shop UX not fully audited |
| Lottery | `Reference/PcLottery/lottery.txt` | 254 rows | ✅ | data/service |
| Adventure | `Reference/PcAdventure/adventure.txt` | 1,037 rows | ✅ | catalog; quest semantics partial |
| Player task defs | `Reference/PcMission/player_task_def.txt` | 647 nonblank data rows after exactly 2 PC header rows / 645 numeric first-id metadata rows | ✅ data/display metadata | Parser skips blank separator rows without fabricating ids and preserves SYNC_FLAG/CLIENT_FLAG metadata; QuestService imports PC task metadata for display and keeps built-in sample quests opt-in/test-only; mission execution partial |
| Guild levels | `Reference/PcTong/tong_level_data.txt` | 6 rows | ✅ | guild scripts partial |
| City war config | `Reference/PcEvent/citywar.ini` | 90 data lines | ✅ data | join/router semantics partial |
| MissionBattle combo/scores matrix | `Reference/PcBattlefield/MissionBattle/combo.txt`, `scores.txt` | 5 ranks / 25 combo cells / 25 score cells | ✅ data/scoring lookup subset | Batch 4 lookup proves PC title-index/rank scoring facts; Batch 5 battle script index catalogs 183 `script/battles` files; kill/death award mutation and mission lifecycle not proven |
| Battle script source catalog | `Reference/PcBattleScript/battle_scripts.txt` | 183 files / 182 active Lua / 1 backup / 10 dirs | ✅ file catalog / 🔄 runtime | File availability/index only; Lua battle semantics not executed |
| Server event source index | `Reference/PcServerEvent/server_event_index.txt` | 455 files / 427 Lua / 28 CVS metadata | ✅ file catalog / 🔄 runtime | File availability/index only; event semantics, rewards, schedules, UI/server side effects not executed |
| FlipCard protocol facts | `Reference/PcFlipCard/flipcard_protocol.txt` | 6 constants/functions | ✅ protocol catalog / 🔄 runtime | Protocol/open UI function facts only; card UI/gameplay flow not executed |
| Tong/faction map catalog | `Reference/PcTong/faction_map.txt` | 33 rows: 4 public / 7 dynamic templates / 11 citymap / 4 building / 7 city altar NPC | ✅ data/service | Batch 4 imports PC `script/tong/addtongnpc.lua` map arrays and `tong_mix.lua` level-10 enter gate; ownership/build/ban/expire/runtime movement not proven |
| Waypoints | `Reference/PcMap/waypoint.txt` | 225 rows | ✅ data / 🔄 action-service runtime | parser preserves exact PC count; `PcMapTravelRuntimeService`/`PcMapTravelActionService` lookup proof covers representative IDs/maps. End-to-end scene travel UX/wiring is not proven for all destination maps. |
| Wharves | `Reference/PcMap/wharf.txt` | 11 rows / 16 SECT slots | ✅ data / ☐ teleport runtime | parser preserves row 3 `COUNT=1` with 2 real SECT slots; current action service returns `DataOnly` because `wharf.txt` preserves service positions but not a proven destination list. |
| Revive positions | `Reference/PcMap/revivepos.ini` | 139 map sections / 241 coordinate rows | ✅ data / 🔄 generic behavior path | parser preserves section `[949]` `region=1,3` with 1 real coordinate; service lookup and generic teleport host path exist, but in-scene revive lifecycle/wiring is not proven full PC runtime. |
| Scrolls | `Reference/PcMap/scroll.txt` | 2,600 rows | ✅ data / ☐ teleport runtime | `PcMapTravelRuntimeService` proof preserves 2,600 value rows and no fabricated map rows; current action service treats the table as value/data-only unless real map rows are proven. Scroll item consumption/teleport UX is not ported. |
| Normal Spawn data | `Reference/PcNormalSpawn/normal.json` | 5,384 rows | ✅ data / 🔄 runtime | 5,385 JSON array elements = 1 GBK header row + 5,384 data rows; loaded by `PcNormalSpawnRuntimeService` (Batch 7). Path is `PcNormalSpawn/normal.json`, NOT `PcNpc/normal.txt` (no such file exists). Full AI spawn loop parity still partial. |

## Missing or path-mismatch evidence

These old `✅` claims must stay downgraded until files/runtime are added and tested.

| System | Expected path/evidence | Current state | Status |
|---|---|---|---:|
| Special skills 58 | `Reference/PcSkill/specialskills.txt` | PC `skills1.txt` special-script subset present: 576 rows; no standalone 58-row table found | ✅ catalog / 🔄 runtime |
| Translife skills 9 | `Reference/PcSkill/translifeskill.txt` | PC `skills.txt` translife4th subset present: 9 rows; separate `PcTask/translife.txt` level table is loaded/proven | ✅ source / ✅ level table / 🔄 runtime |
| Item exchange source catalog | `Reference/PcItemExchange` | top-level `itemexchange_setting` subset present; rolevalue typed facts exposed; `rolevalue_log` excluded | ✅ catalog+rolevalue / 🔄 runtime |
| Hongbao runtime dir | `Reference/PcHongbao` | legacy fallback only; default service path fixed to `PcItemFull/hongbao.txt` | ✅ path fixed / 🔄 runtime |
| Battlefield data dir | `Reference/PcBattlefield` | `MissionBattle/combo.txt` + `scores.txt` subset present | ✅ subset / 🔄 runtime |
| Battle script dir | `Reference/PcBattleScript` | PC `script/battles` file catalog present: 183 files, 182 active Lua, 1 backup | ✅ catalog / 🔄 runtime |
| Server event index | `Reference/PcServerEvent` | PC `script/event` file catalog present: 455 files, 427 Lua, 28 CVS metadata | ✅ catalog / 🔄 runtime |
| VNG event dir | `Reference/PcVngEvent` | ✅ index / 🔄 runtime | PC VNG event index currently records `TotalFiles=195`; do not use stale 201-script count without a fresh parser artifact | Execute VNG event mechanics |
| FlipCard dedicated dir | `Reference/PcFlipCard` | protocol facts present: 6 constants/functions | ✅ protocol catalog / 🔄 runtime |
| Compensation dir | `Reference/PcCompensation` | ✅ index / 🔄 runtime | PC compensation scripts indexed (9 files); current runtime service is index/lookup only | Execute compensation mechanics |
| Quoc Chien / Hoa Son dir | `Reference/PcQuocChienHoaSon` | ✅ index / 🔄 runtime | PC Quoc Chien / Hoa Son scripts indexed (122 files) | Execute event mechanics |
| Tong War dir | `Reference/PcTongWar` | ✅ index / 🔄 runtime | PC Tong/faction war scripts indexed (10 files) | Execute Tong war mechanics |

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
| Minimap/click-to-move/bounds | 🔄 runtime (map 907) / 🔄 partial (others) | map 907 target clamp + minimap RectTransform offset fix/tests; `Map907RuntimeSmokeService` proves catalog/bounds/minimap/controller-clamp facts | Click-to-move/player-feel and in-editor scene smoke are still required before broad map-907 runtime `✅`; all-map parity remains partial |
| Waypoint/wharf/revive/scroll runtime | 🔄 partial runtime | exact parser/count tests plus `PcMapTravelRuntimeService`/`PcMapTravelActionService` proof cover representative waypoint 225, wharf 3 SECT data, map 949 revive, and 2,600 scroll value rows. Generic `PcMapTravelBehaviorService` exists, but wharf/scroll are data-only in current action service and end-to-end Sandbox wiring is not proven complete. | Prove actual scene/UX wiring, destination-map coverage, revive lifecycle, item consumption, and wharf/scroll teleport semantics before runtime `✅` |

### 2. Factions

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| 10 faction enum/static runtime | 🔄 | `CombatDefinition`, `CombatFactionExt`, `SkillSectCatalog` exist | Parse/verify against PC faction data; fix UI text/element mismatches |
| Faction selection UI | 🔄 | `FactionScreen` exists | PC text/layout/selection flow tests |
| Ngũ hành mapping | 🔄 | core mapping exists | Full 10-faction PC element audit; known UI/catalog mismatches |
| Chính/Tà/Trung lập alignment | 🔄 indexed/source-note / 🔄 runtime | relation/framework services exist; current repo evidence indexes player relation and PK configs, but exact C++ source/member proof for hardcoded PC `camp` must be cited before `✅ source` | PC alignment source citation and runtime tests |
| Faction titles 81 | ✅ data/service | `Reference/PcTitle/factiontitle.txt`, parser/service/tests exist | Visual/effect parity not fully audited |
| Faction maps 33 | ✅ data/service | `Reference/PcTong/faction_map.txt` 33 rows from PC `script/tong/addtongnpc.lua`; parser/service/count tests present | Prove Tong map ownership/build/ban/expire/runtime movement before `✅ runtime` |

### 3. Skills / missiles

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Base skill catalog 1,216 | ✅ data | `PcSkill/skills.txt` 1,216 rows | Full skill behavior/formula/script parity |
| Extended skills | ✅ catalog / 🔄 runtime | PC `skills1.txt` 1,712 rows parsed and proven via `Skills1FullCatalog.json` | Execute behaviors and scripts |
| Skill templates 219 | ✅ schema catalog / 🔄 runtime | PC `skilltemplate.txt` imported as 67 field sections / 220 non-empty lines; old 219-row table claim not PC-confirmed | Use schema to validate real skill table parsing/runtime; do not mark as 219 data rows |
| Weapon/thief skill data | ✅ data | 32 + 4 rows | Runtime behavior tests |
| NPC/Boss skill catalog | ✅ catalog / 🔄 runtime | PC `skills1.txt` subset in `Reference/PcSkill/npcskills.txt`: 158 rows, 145 NPC-script, 21 boss-name, 13 boss-only; old 43-row standalone claim is not PC-proven | Execute skill scripts/AI/combat behavior |
| 10 faction skill sets | 🔄 | static `SkillSectCatalog` + tests | PC skill tree/script parity and catalog text fixes |
| Special/translife skills | ✅ source catalog / 🔄 runtime | `specialskills.txt` now proves 576 PC `skills1.txt` special-script rows (not old 58); `translifeskill.txt` proves 9 PC `skills.txt` translife4th rows; separate Translife level table remains proven at `PcTask/translife.txt` | Execute/apply skill effects, unlock flow, formulas, scripts, UI/runtime side effects |
| Skill level up | 🔄 representative rules | `SkillLevelUpScriptCatalog` preserves PC `LevelUpScript` paths and representative prereq/point-pool rules for ids 332/351/390/391/394/1110/1123-1130; parser preserves `ReqLevel`/`MaxLevel`/`LvlSetScript`/`LevelUpScript` fields | Broader Lua level-up scripts, UI wiring, and full formula parity remain unproven |
| Missile effects | ✅ catalog / 🔄 runtime | PC `missles1.txt` full catalog is 467 rows / 466 unique ids; runtime registry now loads full `Reference/PcAttrib/missles1.txt`, preserves duplicate id 408 as last-row-wins, resolves ids 442/443/467, decodes missile SPR paths as GB2312, maps PC paths to signed-byte PAK UIDs, and stages 190 PC missile/effect SPR assets copied from canonical `pak_unpacked` into `Assets/StreamingAssets/Sprites`. Unity execute proof: core catalog 173 skills, 79 skills with PC flight path, 72/79 resolved to real PC SPR, 7 source-missing/still unresolved. | Verify remaining missing SPR refs, rendered scene smoke, and combat side-effects before broad runtime `✅` |
| Skill icons/animations | ✅ assets / 🔄 runtime | 228 unique PC icon SPRs decoded to `PcSkillIcons/` via RE signed-byte UID hash covering 1,002 skills | UI runtime patched to resolve PC icons via `PcSkillIconArtResolver`, tests and animation behavior parity remaining |
| Skill damage formula | 🔄 | `PcSkillDamageService` and `DamageFormulaService` partial | KNpc/KSkill parity and broader formula tests |
| Meridian 128 | ✅ data/service | `meridian_level.txt` 128 rows; service/tests | Full effect/UX parity audit |

### 4. NPCs / monsters

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| NPC templates 2,000 | ✅ data | `PcNpc/npcs.txt` 2,000 rows; full parser exists | Behavior/scripts/AI hooks |
| Region_S static spawns | ✅ data / 🔄 runtime | 67,680 NPC records; 375/375 resType sprites | AI, respawn, level scripts, combat behavior |
| `normal.txt` spawn table 5,384 | ✅ data / 🔄 runtime | Exactly 5,384 valid rows verified and parsed to JSON | Add/audit full table runtime behavior |
| Rare/gold boss tables | ✅ data | 480 rare, 32 goldboss rows | Schedule/event behavior |
| NPC dialog | ✅ index / 🔄 runtime | `DialogSysRuntimeService` integration (Batch 7) | PC `script/dailogsys` semantic execution |
| NPC level scripts 58 | 🔄 | parser/service exists | Execute PC level script semantics |
| Enemy AI | 🔄 | generic `EnemyAiService` exists | PC AI/script parity |
| NPC visual | ✅ staged data / 🔄 full visual | `NpcSpriteCoverage.json` 375/375 | All NPC animation/state verification |

### 5. Items / economy

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Core item/equipment data | ✅ data | exact counts in verified data table | Behavior/economy parity by subsystem |
| Magic attributes | ✅ data | 330 rows; old 333 false | Formula/application parity |
| Set bonus/enhance/refine | 🔄 | services/tests exist | PC source exact formula audit |
| Compound/recipe | ✅ data / 🔄 craft-plan | 1,294 atlas_compound rows parsed; PC `atlas.lua` validation/result and `compound_header.lua` host-op transaction plan modeled (100000 fee, RNG by item value, log/remove/add ops) | Wire real InventoryService item instances, UI, money, server/global-value limits, and log side effects before runtime `✅` |
| Quest items | ✅ data/service | 2,045 quest keys | Full quest usage semantics |
| Shop/auction/stall/economy | 🔄 | data/services exist | NPC shop UX and server economy behavior parity |
| Item exchange | ✅ source catalog/rolevalue facts / 🔄 runtime | Batch 3 imports/parses top-level PC `itemexchange_setting`: normal 7,334 rows, rare 480 rows, level_exp 200 rows, level_lead_exp 100 rows, rolevalue.ini 35 keys; Batch 4 exposes typed `rolevalue.ini` facts: 4 sections, 27 Jxb server values, skill=5000, create date 20160301; `rolevalue_log` excluded | Implement/verify exchange rules, inventory mutation, role value/evaluate formulas, server log/runtime side effects |
| Hongbao | ✅ data/open-model path / 🔄 runtime | `hongbao.txt` 69 rows parsed from `PcItemFull`; Batch 4 proves KBonus-style inclusive raw-weight selection, 6 free-cell preflight, Type 1/2 reward commands, Costly global-news flag, and Log flag | Wire commands to real inventory/item grant, consume item, UI/server side effects, and logs before runtime `✅` |
| City Hongbao data | ✅ data/reward-command path / 🔄 runtime | `chengshidahongbao.txt` 67 rows / total weight 1,010,000 | Full PC schema parsed and type 1/2 reward command path exists, but item consumption/log/news/UI/server side effects are not proven full PC runtime. |
| Drop rates | 🔄 | `PcDropRate` has many tables; services exist | Loot behavior parity tests |

### 6. Missions / quests

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Quest framework | 🔄 | `QuestService` exists; hard-coded/sample chains reported | PC mission quest import/execution |
| Mission script metadata | 🔄 | `MissionScriptService` is metadata/helper, not Lua VM | Implement/verify semantic executor or mark indexed only |
| Adventure entries 1,037 | ✅ data | exact row count | Runtime quest integration |
| Player task defs | ✅ data | `player_task_def.txt` 647 nonblank data rows after exactly 2 PC header rows; parser skips blank separator rows and preserves SYNC_FLAG/CLIENT_FLAG | mission execution partial |
| Task/random/talk/event configs | 🔄 | config services/parsers exist | PC behavior and side-effect tests |
| Newtask/tollgate/mission arena/maze/qianchong configs | 🔄 | services exist | Full mission flow parity |
| ClearSkill mission lifecycle constants | 🔄 partial runtime/model proof | Batch 3 proves PC mission id/timer/camp NPC/init/end/onleave/timer constants and operation plans; Batch 4 replays plans into a host interface and proves call order/result forwarding | Real host mission/runtime execution and stable SandboxManager wiring are not fully proven; do not mark broad runtime `✅` |

### 7. Events / activities

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Activity/HuoYueDu services | 🔄 | services/reference config files exist | Exact PC count/source and runtime parity |
| Server events 455 | ✅ file catalog / 🔄 runtime | `Reference/PcServerEvent/server_event_index.txt` catalogs 455 PC `script/event` files (427 Lua, 28 CVS metadata) | Parse/execute semantic event Lua, schedules, rewards, and side effects |
| VNG events/features | ✅ index / 🔄 runtime | `PcVngEvent/vng_event_index.txt` currently records `TotalFiles=195`; stale 201-script count must be re-proven by parser artifact before use | Add tests and execute |
| Seasonal/compensation/bingo/flipcard | ✅ index / 🔄 runtime lookup | `PcCompensation` integration via `CompensationIndexRuntimeService` (Batch 7) is index/lookup only; flipcard protocol facts are catalog/UI helper only | Add semantic parsers/runtime for event scripts, rewards, UI, and server side effects |
| Event scripts | 🔄 indexed only | `EventScriptService` metadata registry | Full Lua semantic execution |

### 8. Combat / PvP / battles

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Core combat/damage/projectile/buff/death/PK services | 🔄 | services and tests exist | PC formula/skill/PvP semantic parity |
| Tống Kim maps/rebirth traps | 🔄 | map alias counts, some rebirth traps ported; Batch 3 MissionBattle combo/scores subset proves 5 ranks and 25/25 matrix cells; Batch 4 scoring lookup proves representative PC title-index/rank scores | Full battlefield state, kill/death award mutation, join/award behavior, mission lifecycle |
| CityWar | 🔄 partial runtime/model/constants/token proof | `citywar.ini` data exists; `CityWarJoinRouter` routed; Batch 2 model covers join plan branches; Batch 3 constants prove PC card table, card prices, challenge token, task ids/caps/reward constants; transfer-route split model proves NPC route 222/223 vs trap join 221; Batch 4 challenge-token turn-in model proves token tuple, daily cap, exp, task/league/Tong-total command surface | Full CityWar mission lifecycle, Tong gate host integration, real inventory/life APIs, real command execution, rewards, capacity gates, and live scene proof still missing |
| Quốc Chiến/Hoa Sơn | ✅ index / 🔄 runtime | PC Quoc Chien / Hoa Son indexed (122 files) | Execute event mechanics |
| Battle scripts 183 | ✅ file catalog / 🔄 runtime | `Reference/PcBattleScript/battle_scripts.txt` catalogs PC `script/battles`: 183 files, 182 active Lua, 1 backup, 10 dirs; old settings-file/mock claim corrected | Execute full PC battle Lua semantics and wire runtime mission side effects |
| Battle awards/double EXP | 🔄 | services exist | PC event/schedule/effect parity |

### 9. Guild / Tong / party

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Guild level data | ✅ data | 6 rows in `tong_level_data.txt` | Runtime behavior parity |
| Guild creation/fund/contribution/workshop/task/rank/stunt | 🔄 | services/configs exist | PC server behavior and script side effects |
| Guild scripts 65 | 🔄 indexed/framework | PC `script/tong` Lua not executed; service mostly validate/log/return | Semantic Lua execution or explicit indexed-only status |
| Guild city war / Tong maps | 🔄 partial hooks/model/constants/catalog proof | `TongMapEntrance=8` routed as host-limited hook with Batch 2 default/cn_ib branch model; `CityWarJoinRouter=1` routed/tested as partial runtime/model proof; Batch 3 adds CityWar card/token constants and transfer-route split; Batch 4 adds challenge-token turn-in command surface and 33-row Tong/faction map catalog | Implement/prove PC Tong ownership/ban/expire/product-region/template-map APIs, CityWar gate integration, map ownership/build/recharge, real inventory/Tong task APIs, and Unity runtime behavior before `✅` |
| Party system | 🔄 | party services/panels exist | Full PC team behavior, team trap entry, UI proof |

### 10. Other systems

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Activity, partner/pet, title, shop, stall, foundry, lottery, flip card, honor, shitu, bonus, trip, guide, world rank, city defence | 🔄 | many services/tests exist | Row-by-row PC source/count/runtime parity not fully audited |
| Weather/music/audio | 🔄 | services/config parsers exist | Map-specific PC parity and runtime proof |
| GM tools | 🔄 excluded | user-owned GM teleport/browser changes are out of audit scope | Do not use as port completion proof without explicit user request |

### 11. Player/NPC visuals and SPR runtime

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| SPR decoder/runtime/atlas core | ✅ core | `SprRuntimeService`, `SprDecoder`, `SprAtlasPacker`, tests exist | Direct PC PAK runtime loading not globally audited |
| Male/female player visual | ✅ | visual classes/catalog JSON/tests exist; dynamic PC resId equipment variants and GM Panel tab implemented and verified | Full palette/animation parity |
| Mount/horse visual | 🔄 | horse/mount services/catalogs/tests exist; mounted shadows for male/female and layered horse parts for female configured and verified | Full palette/animation parity |
| NPC visual | ✅ staged data / 🔄 runtime | `NpcSpriteCoverage` 375/375 | Full animation/state parity |

### 12. Client / UI

| Item | Status | Evidence | Required next proof/work |
|---|---:|---|---|
| Mobile HUD baseline | 🔄 | committed services/tests exist; current HUD files dirty and excluded | Rerun tests/smoke after human HUD WIP is resolved |
| HUD PC art | 🔄 | partial PC-derived subset exists | Not 100% PC HUD art/layout parity; verify asset provenance row-by-row |
| Minimap/quest/inventory/map/chat/party/faction/shop panels | 🔄 | panels/services exist | Chat panel aligned to input row, scroll rail aligned to messages, channel identity icon dynamically switches. Full PC behavior/UI parity not globally proven |
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
- Faction maps 33 as full runtime `✅`; current proof is data/service only, not Tong map ownership/build/ban/expire/runtime movement.
- Extended skills 1,712 as full runtime `✅`; current proof is catalog import only, not behavior/script parity.
- Skill templates 219 as data rows, special skills 58, and old NPC skills 43 as `✅`; current proven facts are schema metadata, 576 special-script rows, 158 NPC/Boss rows, and 9 translife source rows, all runtime-partial.
- Item exchange, hongbao, City Hongbao, waypoint/wharf/revive/scroll, and missile effects as broad runtime `✅` without real inventory/economy/teleport/server-side semantics and fresh scene/runtime proof.
- HUD/UI/GM teleport claims based on dirty or user-owned changes.

## Harness matrix vs reality — audit 2026-06-12

A fresh cross-check of the Harness DB (`harness.db`) against the actual repo found the
Harness story matrix overstates verification. Do **not** read `query matrix` status as port truth:

1. **All 35 stories are `status=implemented` with `last_verified_result=pass`, but the proof is hollow.**
   `verify_command` is a no-op for every row: 25 stories run literally `true`, 7 run an
   `echo '...verified...'` string, 3 (US-100/101/102) have no command and were never verified.
   A `pass` here means "the echo/true exited 0", not "tests ran". These flags carry no parity weight.
2. **The headline EditMode/PlayMode suites do not compile or run by default.**
   `Assets/Tests/EditMode/VLTK.Tests.EditMode.asmdef` and the PlayMode asmdef are gated by
   `defineConstraints: ["VLTK_ENABLE_TESTS"]`, and `VLTK_ENABLE_TESTS` is **not defined anywhere**
   (empty `scriptingDefineSymbols`, no `csc.rsp`). So ~2,281 EditMode `[Test]`/`[TestCase]`/`[UnityTest]`
   attributes are excluded from compilation. Only `Assets/Tests/PortFactorySmoke/` (gated by the
   always-on `UNITY_INCLUDE_TESTS`) actually runs. Any evidence pointer into `VLTK.Tests.EditMode`
   is currently a reference to dormant code, not a passing test.
3. **Catalog COUNTS are, however, accurate.** A 5-domain fan-out re-counted the Reference data
   files against this doc's claims: skills/missiles (10/10), items/economy (29/29), maps/travel
   (8/8 + manifest), npc/spawn, and events/battle all MATCH. The two wording fixes applied:
   `normal` path corrected to `PcNormalSpawn/normal.json` (above), and `citywar.ini` "90 data lines"
   is 90 *total* lines / 31 key=value data lines.

To raise a story to a truthful `✅`/runtime claim: define `VLTK_ENABLE_TESTS`, run the real
EditMode/PlayMode suite via Unity Test Runner, capture the artifact, and set a `verify_command`
that actually executes that suite — not `true`/`echo`.

### First real EditMode run after enabling the suite (2026-06-12)

`VLTK_ENABLE_TESTS` was defined (Standalone/Android/iOS) and the suite was un-gated for the first
time. Two findings:

**(a) The suite did not even compile.** 67 `error CS` across 7 bit-rotted test files referenced
production APIs that had since been renamed/removed (e.g. `PcAdventureEntry.advId` → `.id`,
`MapListFullService.Get` → `.GetMap`, `ItemDetailService.GetItemDetail` → `.GetDetail`,
`CompoundRecipeService.Count` → `.RegisteredCount`, `GmItemActionResult.isSuccess` → `.success`,
a missing `using VLTK.Sandbox;`, a `99999`→`ushort` overflow, and `PcRareSpawn/PcGoldBoss` schema
drift). These were fixed at the test call-sites only (no production code touched; absent APIs
marked `Assert.Ignore` + TODO, not silently dropped). This is the real reason the suite was gated
off: it was abandoned when the production API moved.

**(b) Once compiling, the real result is NOT green.** Unity Test Runner (job `29fe63c1…`,
artifact `TestResults.xml`, 112s):

```text
EditMode total=2283  passed=2180  failed=97  skipped=6   (result=Failed)
```

Failure clusters (97): player/mount visual SPR staging 41 (FemalePlayerVisual 20, MalePlayerVisual
14, MountVisual 7), Title/faction-title 5, `Pc*Parser` 9, UIPanelService 5, plus ~37 spread across
services (Auction expire, CoverageSmoke 235→171 instantiable, DailyTask/RandomTask level filter,
Meridian, Hongbao event, Inventory equip, Quest PC import, perf budgets, etc.). These are exactly
the runtime/parity gaps `PORT_STATUS` marks `🔄` — now confirmed by execution, not assumed. So
"35/35 implemented + pass" in Harness was hollow: the real suite is 2180/2283 with 97 genuine
runtime failures. Closing those 97 is the concrete runtime-parity backlog.

### Triage of the 97 failures (2026-06-12) — TEST-DRIFT vs PROD-GAP

Each failure was read against the real production code and the actual shipped reference file to
decide whether the **test expectation is wrong** (TEST-DRIFT — fix the test to match reality
without weakening intent) or **production is wrong** (PROD-GAP — leave the test red, fix the code).
Logged as Harness backlog #6–#13.

**PROD-GAP (code is wrong — tests stay red until fixed):**

| Cluster | Tests | Root cause | Backlog |
| --- | --- | --- | --- |
| GBK→TCVN3 mojibake | ~8 + 151/154 parity rows | `PcText.DecodeBest.Score()` weights Vietnamese TCVN3 +8/char vs CJK hanzi +3/char, so GB2312 files mis-decode (`宝箱1`→`±Ưẽọ1`). GB2312/CP936 is available; the correct candidate is just out-scored. One-line rebalance fixes all. | #12 |
| Meridian collapse | 3 | `meridian_level.txt` has 128 acupoint rows (8×16); service returns 16. | #6 |
| Title/FactionTitle loader | ~8 | `factiontitle.txt` (80) + `playertitle.txt` (363) exist and are populated, loader returns 0. Likely shares the GB2312 decode root. | #7 |
| Mission parser | 2 | `player_task_def.txt` has 645 numeric rows; parser registers 634 (drops 11). | #8 |
| CoverageSmoke | 2 | 64 `*Service` types lack a public parameterless ctor; `Activator.CreateInstance()` throws. 171/235 already follow the dual-ctor convention (`HongbaoService` is the template). | #9 |
| Visual staging | ~10 of 41 | Female SPRs entirely unstaged (`female_*_sprites.json` empty), male `MA_RW_000_ST04/ST05` + `MA_LW_000_ST06` missing from manifest (only ST01–03 staged; body variants exist so filenames are correct → extraction incomplete). | #13 |

**TEST-DRIFT (test expectation is wrong — fix the test, do not weaken assertions):**

| Cluster | Tests | Why the test is wrong | Backlog |
| --- | --- | --- | --- |
| Count vs shipped stub | Shop (≥500 vs 165 shipped), Tong (≥30 vs 6), Partner (≥10 vs 4) | Tests demand full PC-source counts but only stub reference files ship. | #10 |
| NpcSFull template id | 1 | `npcs.txt` is keyed by Name and has no template-id column; asserting `templateId>0` contradicts the schema. | #10 |
| Self-contradicting service-logic asserts | 8 | Auction (negative duration now rejected by design), DailyTask/RandomTask (level 15 legitimately matches 2 incl. open-range), Hongbao/VngEvent/SjBattle (assertion contradicts its own inline comment), Meridian.TryUpgrade + Perf.TryUpgrade (compares wrong entry / uses empty registry). | #10 |
| Mount layer count | ~24 of 41 | Catalog was redesigned 3-part→9-part layered horse+rider; `MountVisualTests` + female 5-part asserts still encode the old contract and now contradict `MalePlayerVisualTests`. | #10 |

**Unresolved decision:** `SprRuntimeServiceTests` CJK path-UID expects `bccbbad2` (the on-disk
staged filename, **unsigned** GB2312 hash) but the C# default `signedBytes:true` yields `bc9bc73d`
(which has no file on disk). Note the two-hash distinction: `engine.dll g_FileName2Id` (internal
PAK lookup) is **signed**, the **staged-filename** hash is **unsigned**. Decide whether to flip the
staged-lookup overload default or make callers pass `signedBytes:false`. Backlog #11.

### Fixes landed (2026-06-12) — 97 → 72 failures

Real EditMode failures dropped from 97 to 72 via root-cause fixes (verified by
re-running affected tests, not by weakening them):

| Commit | Fix | Tests recovered | Backlog |
| --- | --- | --- | --- |
| `319ce3b27` | `PcText.Score()` rebalanced VN+4 / CJK+8 — GBK files decode as hanzi again (validated on 198 ref files: 7 GBK corrected, 0 Vietnamese regressed) | ObjectSetting, NormalSpawn, QuestImport, SkillCatalogParity (151 rows), ThiefSkill-count | #12 closed |
| `f5d22a8dc` | `PcFactionTitleParser` column map corrected (RANKID/RANKSTR/FACTION; was reading id from name col → 0 rows) | TitleServiceTests (4), PcFactionTitleParserTests | #7 partial |
| `4bc729843` | `PcMissionRegistry` keeps all 645 rows (was deduping 11 tournament tasks that share TASK_ID_FIRST) | PcMissionParserTests, QuestServicePcImportTests | #8 closed |
| `9eb2fc019` | parameterless ctor added to 64 registry-only services | both CoverageSmokeTests (was 171/235 + 64 throwing) | #9 closed |
| `e6c55cece` | Auction+DailyTask fixtures aligned to real production contract (no weakening) | IsExpired_TrueForPastTimestamp, GetTasksForLevel_FiltersCorrectly | #10 partial |
| `75638c5b5` | MapPanelService GetMapsByType/GetMapIconPath + NpcDialog snapshot id implemented | MapPanel x2, NpcDialog null-snapshot | #10 partial |
| `8686e508f` | FemalePlayerSpriteCatalog shadow not-required + FM_ shadow path (verified vs PC npcres/woman) | Catalog_ShadowAndWeapons_AreNotRequired, Catalog_EmptyHandMove_HasFullFemaleLayerSet | #13 partial |
| `48e77f156` | RandomTask/Shop/Tong/Partner count expectations corrected to PC ground truth (buysell=166→159 shops, tong=7→5 levels, partner=5→4 chars; fabricated 1521/33/10 removed) | 4 parser/count tests | #10 partial |
| `a31831c08` | 3 self-contradicting service-logic asserts fixed vs production (VngEvent requiredVip filter, Hongbao no claim-dedup, SjBattle fallback threshold rejects low level) | VngEvent, Hongbao, SjBattle | #10 partial |

### Fixes landed (2026-06-12, batch #10 cont.) — 72 → 62 failures

Continued root-cause sweep. Real EditMode failures dropped 72 → 62 (job `187cffbbb…`,
`total=2283 passed=2215 failed=62 skipped=6`), verified by re-running affected tests, not weakened.

| Commit | Fix | Tests recovered | Backlog |
| --- | --- | --- | --- |
| `337e0ea18` | ObjData TCVN3 decode + TaskRandom level-range bounds | ObjData/TaskRandom cluster | #10 |
| `fa0b6dd98` | PcNpcSFull synthetic id (npcs.txt keyed by Name, no template-id col — synthesize stable id) | NpcSFull | #10 |
| `1e5436308` | PcMapTravel drift (worldY sign convention, BaLang validator is ground truth: -3328 correct) | PcMapTravel | #10 |
| `0bb920a4c` | PcSkillSourceLink GB2312 explicit + first-non-empty LvlSetScript (row#2 col70 no longer clobbers) | WeaponThief skill-source | #10 |
| `5045a17ea` | Missile: missles1.txt TCVN3 runtime load via `PcText.ReadLinesTcvn3` (tab-safe; GBK ate tab → col shift → speed -40960 vs 156); dropped phantom id 445 from `ExistingModMisslesTxt` expected array (source jumps 444→446) | 2 missile tests | #10 |
| `a610d5556` | HUD: minimap flag-click panel surfaces canonical `Cắm cờ` action token (was `Đã cắm cờ`, past-tense mid-sentence, did not contain action token) | MinimapPcButtons_ExposeFlagMarkerActionOnly | #10 |

**Newly triaged this batch (verified root cause, logged backlog, left red — model-level):**
- ~~`PcHorseParser` column off-by-one~~ **FIXED + integrated 2026-06-12** (`port/fix-horse-columns`, merged to `dev`): col0=Name not ItemGenre; parser + `IntegrationTests` key fixed together; `Test_HorseService_Plus_MountService_Workflow` now passes (Unity job `8d4ddbeb7b424fdc8fd3ee76d2dc4776`, 61/61). Backlog #15 closed.
- ~~`PcMapListFullParser` assumes 8-col TSV~~ **FIXED + integrated 2026-06-12** (`port/fix-maplist-ini`, merged to `dev`): rewritten as INI `key=value` section parser (1005 maps, MapType string→enum); registry Count now non-zero, `GetMap(id)` resolves; `MapDataParserTests`+`MapDataServiceTests` green (Unity job `8d4ddbeb7b424fdc8fd3ee76d2dc4776`, 61/61). Backlog #16 closed.
- `PcIconBarButtons_OpenRuntimeBackedPanels` NRE: `BuildIconBarRows` dereferences `SandboxManager.Instance` runtime services that are null under EditMode (no booted manager). Test-harness/runtime model-level.
- `SandboxManagerFastBootTests`: `ActiveBootProfile` stays `Full` (expected `FastEditor`) — `InitializeSubsystems` skipped because a stale `Instance` from a prior test trips the Awake guard; test-isolation/runtime model-level. Backlog #18.

**Still open (harder PROD-GAPs, left red — not faked green):**
- Visual SPR cluster (Female/Male/Mount `Visual_LoadsXxx`, 39 tests): model-level, NOT staging-orphan. **Disproven 2026-06-12**: deleting the orphan SPRs `48fa4044`/`b4196106` (= unsigned-GB2312 hashes of `spr\npcres\woman\FM_LW_000_RN01.spr` / `FM_RW_000_RN01.spr`, which do not exist in PC `npcres/woman` — verified 440 files = BD/HD/HR/LH/RH ×88, no LW/RW/YY; `npcres/man` likewise has no MA_LW/MA_RW) left `LoadedPartCount` unchanged (still 6/7), proving the `*_FromStagedSprFiles` runtime does NOT resolve parts from `StreamingAssets/Sprites/<uid>.spr`. The +1/+2 over-count comes from the visual builder's part-count counting non-required spec slots. Real fix is in the visual part-count model (gate by `spec.required` against per-gender PC tag availability), not staging. Backlog #13.
- Meridian model refactor: `acupointId` is actually a per-meridian level (1–16) × 8 meridians = 128; registry collapses to 16. Composite-key refactor needed. Backlog #6.
- `TitlePanelService` is a full stub (every method returns empty/false). Backlog #7.
- SPR path-UID signed/unsigned decision (ComputePathUidHex bc9bc73d vs on-disk bccbbad2). Backlog #11.
- Remaining TEST-DRIFT (Shop/Tong/Partner counts, NpcSFull, GameHud minimap buttons). Backlog #10.

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
3. Waypoint/wharf/revive/scroll parser/count/action-service proofs are added, but end-to-end travel/revive/scroll runtime proof is **not** complete; wharf and scroll remain data-only until real teleport/item-consumption semantics are proven.
4. Keep HUD/GM teleport out of map-port commits unless user explicitly asks.

## Port Factory Batch 7 integration — 2026-06-09

Scope: Integration of runtime components for Normal Spawn, DialogSys, and Compensation indices into `SandboxManager`. Fix compilation issue with `IMapTeleportHost`. Execute `PortFactorySmoke` test.

Integrated commits/evidence on local `dev`:

- `Assets/Scripts/Sandbox/PcNormalSpawnRuntimeService.cs`: Integrated `normal.json` data (5,384 rows) to runtime lookup.
- `Assets/Scripts/Sandbox/DialogSysRuntimeService.cs`: Integrated DialogSys index lookup.
- `Assets/Scripts/Sandbox/CompensationIndexRuntimeService.cs`: Integrated Compensation index lookup.
- `Assets/Scripts/Sandbox/SandboxManager.cs`: Implement `IMapTeleportHost` methods (`HasMap`, `SwitchMapAndPlacePlayer`) to resolve compilation error CS0535.
- Unity compile completed without errors. Only known Addressables GUID conflict and Vulkan GPU memory warnings.
- `PortFactorySmoke` EditMode job passed via `mcp_unityMCP_run_tests`.
- 5,384 rows `normal.txt` spawn data verified available at runtime (`✅ data / 🔄 runtime` updated).
- DialogSys and Compensation indexing wired into runtime container (`✅ index / 🔄 runtime` updated).

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

### Harness matrix ↔ real-suite sync (2026-06-12)

After the first real EditMode run exposed 62 failures, the Harness `story` proof flags were
synced to execution truth so the matrix stops over-claiming. Method: each failing test-class
from `TestResults.xml` (job `187cffbbb…`, 2215/2283) was mapped to the stories whose `evidence`
column cites it; any such story that still claimed `unit_proof=1`/`integration_proof=1` had both
flags reset to `0`, `last_verified_result='fail'`, and a dated note added naming the failing class.

9 stories were corrected:

| Story | Failing evidence test | Action |
| --- | --- | --- |
| ST-00.2 | SprRuntimeServiceTests | proof→0, result=fail |
| ST-02.1 | MalePlayerVisualTests | proof→0, result=fail |
| ST-02.1.1 | FemalePlayerVisualTests | proof→0, result=fail |
| ST-02.1.2 | FemalePlayerVisualTests | proof→0, **status implemented→in_progress** |
| ST-02.1.3 | MountVisualTests | proof→0, result=fail |
| ST-02.1.4 | MountVisualTests | proof→0, **status implemented→in_progress** |
| ST-04.1.1 | WuDangCombatCatalogTests | proof→0, result=fail |
| ST-05.3 | InventoryServiceTests | proof→0, result=fail |
| ST-06.1 | GameHudControllerTests | proof→0, result=fail |

Resulting DB totals: `status` implemented 28→26 / in_progress 7→9; proof flags `unit` 35→26,
`integration` 35→26 (`e2e`=2, `platform`=0 unchanged). Invariant now holds: **no story claims
unit/integration proof while a test-class named in its own evidence is failing.** These flags
must only return to `1` when the named test goes green in a real Test Runner artifact.
