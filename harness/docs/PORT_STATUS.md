# PORT_STATUS.md — Audit trạng thái port PC → Mobile

> **Audit lại**: 2026-06-08
> **PC source of truth**: `/var/www/vltksource_new/vl_update_27`
> **Mobile codebase**: `/var/www/vltk-mobile` (`dev` @ `6fb8ee3`, dirty HUD files excluded)
> **Quy tắc mới**: `✅` chỉ dùng khi có bằng chứng hiện tại trong code/catalog/verifier/tests. Có service/parser nhưng chưa chứng minh đủ hành vi PC thì là `🔄`, không được ghi `✅`.

## Chú thích

| Ký hiệu | Ý nghĩa |
|---|---|
| ✅ | Đã verify bằng artifact/command hiện tại; còn có thể có known gaps được nêu rõ |
| 🔄 | Có framework/data/parser/runtime một phần, nhưng chưa đủ parity PC hoặc chưa có proof end-to-end |
| ☐ | Chưa port / ngoài scope client |
| ⚠️ | Claim cũ trong tài liệu trước bị audit là quá tay, đã downgrade |

## Bằng chứng audit đã chạy

- `git status --short --branch`: còn dirty **HUD/UI** `Assets/Scripts/UI/CharacterPanelService.cs`, `Assets/Scripts/UI/GameHudController.cs`, `harness/item_spr_img/`; không tính vào status audit này.
- Đã restore WIP ClearSkill trap dở về HEAD để tránh compile/interface sai; patch tạm lưu ngoài repo tại `/tmp/vltk_clearskill_wip.patch`.
- `python3 scripts/jx_map_port_verify.py --include-missing-spr-region-refs --pretty` → `status=pass_with_known_gaps`, `errors=[]`.
- Unity console hiện chỉ có Addressables GUID conflict known-noise; chưa rerun full EditMode/PlayMode suite trong audit này, nên mọi claim `1771/1771` hoặc `25/25` cũ bị bỏ khỏi trạng thái hiện tại.

## 1. Map / World — trạng thái đáng tin

| Hạng mục | Status | Bằng chứng hiện tại | Gaps còn lại |
|---|---:|---|---|
| PC map aliases + visual geometry | ✅ | Verifier: `1,005` aliases, `332` geometries, `95,246` Region_C, `2,785` map SPR; `MapAliasCatalog.json`, `MapGeometryCatalog.json`; runtime merge ở `MapManager.MergeGeneratedBulkCatalogs()` | Generated assets nằm dưới ignored `Assets/StreamingAssets/Generated`; clean clone phải chạy regenerate commands từ verifier |
| Default map `Vượt ải Nhiếp Thí Trần` | ✅ | `Sandbox.unity` có `defaultMapId: 907` x2; verifier `defaultMap.mapId=907`, `nameVi=Vượt ải Nhiếp Thí Trần`, `killbossMissionBossTemplates=10` | Chưa audit manual movement toàn map sau feedback bị chặn sớm |
| Missing map SPR provenance | ✅ known gaps | Verifier còn đúng 6 unique missing SPR refs / 182 refs; tất cả có provenance `presentInClientPak=false`, label hit = 0; `RegionTileDefault` là engine fallback | Không được tự bịa SPR thay thế |
| Server Region_S extraction | ✅ data / 🔄 gameplay | `MapServerRegionCatalog.json`, `MapSpawnCoverage.json`: `332` geometries, `330` có Region_S, `84,019` Region_S, `67,680` NPC records, `8,692` trap records, `453` object records; aliases thiếu static Region_S: `134`, `1007` | Đây là data extract, chưa chứng minh AI/spawn scheduling/gameplay đầy đủ PC |
| NPC visual staging from Region_S | ✅ data | `NpcSpriteCoverage.json`: `375/375` resTypes covered, `1,314` staged sprite files, missing `0` | Visual staged không đồng nghĩa full AI/combat behavior |
| Region_S objects/traps | 🔄 | `MapInteractiveCoverage.json`: `35/35` object templates resolved, `34/34` object SPR paths staged, `299` deterministic object actions; `MapTrapScriptCoverage.json`: `817/817` trap ids resolved, `804` deterministic trap actions | `13` resolved trap scripts still deferred: `TongMapEntrance=8`, `ClearSkillTeamEnterHole=4`, `CityWarJoinRouter=1`; no `IsClearSkillTeamEnterHole` runtime in HEAD |
| Minimap / click-to-move / bounds | 🔄 | Services exist: `MinimapService`, `MinimapPanel`, `PlayerMovementService`, `CoordinateService`, map bounds from geometry catalog | Needs manual + PlayMode proof on full map 907; earlier user report means old ✅ is not trustworthy |
| Travel data (waypoint/wharf/scroll/revive) | 🔄 | `MapManager.MergePcMapData()` loads `Reference/PcMap`; parsers/services exist | Counts/behavior not re-audited this pass; keep partial until verifier/test proves PC parity |

## 2. Data/service systems — audited downgrade

| Domain | Status | What is actually proven | Why not ✅ full parity yet |
|---|---:|---|---|
| NPC templates | ✅ data / 🔄 behavior | `Reference/PcNpc/npcs.txt` has 2,000 data rows; `PcFullNpcParser`, `MapEnemyDatabase` exist | Spawn AI, script hooks, combat behaviors not fully proven by current audit |
| Factions/titles/faction maps | 🔄 | Faction/title/map services and tests exist | Full PC faction quest/skill/UI behavior not audited end-to-end |
| Skills/missiles | 🔄 | `Reference/PcSkill/skills.txt` has 1,216 data rows; weapon/thief/missile refs exist; many skill services/tests exist | `SkillScriptService` is metadata registry (`GetScript`, `GetBySkill`), not a full PC Lua/skill VM; visual/effect parity not fully audited |
| Items/economy/drop | 🔄 | Large PC item/drop reference sets exist (`PcItemFull`, `PcDropRate`); inventory/economy/shop services exist | `ItemScriptService` is metadata registry; only selected item actions (e.g. GM token path) are ported, not all item Lua behaviors; GM teleport changes are user-owned and not touched/audited here |
| Missions/quests/tasks | 🔄 | `MissionScriptService`, `TaskScriptService`, `QuestService` exist; `Reference/PcMission/player_task_def.txt` present | `MissionScriptService` only exposes metadata/progress helpers; not a full executor for 985 PC mission Lua/scripts |
| Events/activities/VNG | 🔄 | Event/activity services and reference data exist | `EventScriptService` is metadata registry; event Lua/runtime side effects not proven complete |
| Battles/PvP/Tống-Kim/CityWar | 🔄 | Battle/guild/city-war services exist; some trap families ported (`SongJinRebirthCampState`, city-war camp gates) | `BattleScriptRuntimeService` executes simplified conditions/actions, not full PC battle scripts; `CityWarJoinRouter` trap still deferred |
| Guild/Tong | 🔄 | Guild/Tong data/services exist | `GuildScriptService.ExecuteScript()` currently validates/logs only; `TongMapEntrance` 8 traps deferred pending ownership/ban/expire/runtime APIs |
| Partner/pet/meridian/titles/misc systems | 🔄 | Services/reference files/tests exist for many systems | Current audit did not prove full PC behavior; old `~100%` totals were overclaimed |

## 3. UI/client/player visual/server infra

| Domain | Status | Evidence | Gaps |
|---|---:|---|---|
| HUD/UI panels | 🔄 | Many UI services/tests exist; recent commits improve specific PC controls | Worktree currently has dirty HUD files, excluded from audit; full PC UI parity not verified |
| Player/NPC visual runtime | 🔄 | `MalePlayerVisual`, `FemalePlayerVisual`, sprite catalogs, NPC sprite staging exist | Full equipment/layer/action parity not re-audited |
| Client resource/SPR loading | 🔄 | `SprRuntimeService` and decoded/staged assets exist | Not all UI/SPR assets have audited provenance in this pass |
| Network protocol/client services | 🔄 | Message router/opcode services exist | Server integration not audited |
| Gateway/Bishop/S3Relay/DB/PaySys/ops | ☐ | Server-side systems, not ported into Unity client | Keep outside client scope unless server project is added |
| GBK/server script directories | 🔄 metadata | Many `*ScriptService` classes count/lookup scripts | Treat as metadata indexes, not completed Lua port, until each action path is executed/tested |

## 4. Current trusted map verifier facts

```text
visual: 1,005 aliases / 332 geometries / 95,246 Region_C / 2,785 map SPR / 6 missing unique SPR paths
server Region_S: 330/332 geometries, 84,019 Region_S, 67,680 NPC, 8,692 trap, 453 object records
interactive: 817/817 trap scripts resolved, 804 deterministic trap actions, 13 deferred resolved actions, 0 unclassified
objects: 35 templates resolved, 34 SPR paths staged, 299 deterministic object actions
default map: 907 — Vượt ải Nhiếp Thí Trần
```

## 5. Priority tiếp theo sau audit

1. Verify/fix manual movement bounds trên map 907 (user report: minimap còn rộng nhưng player bị chặn sớm).
2. Port remaining deferred trap families with PC source proof, in order: `ClearSkillTeamEnterHole` → `TongMapEntrance` → `CityWarJoinRouter`.
3. Add automated proof for map 907 full walkable bounds/minimap/click-to-move before marking movement/minimap ✅.
4. Convert script-service claims from metadata counts to real PC behavior only when each Lua/API family is implemented and tested.
5. Keep HUD/GM teleport work separate; do not stage or audit dirty UI changes unless user explicitly asks.

## 6. Removed false confidence from previous file

- Removed old global claims `1771/1771 EditMode`, `25/25 PlayMode`, `Tổng thể ~100%`, and blanket `✅` for scripts/systems.
- Old counts can still be useful as leads, but they are no longer treated as completion proof.
- Future edits to this file must cite command/file evidence and must not mark `✅` merely because a parser/service exists.

## 7. Downgrade cụ thể theo audit subagent

Các dòng dưới đây là các claim cũ cần nhớ khi đọc lịch sử commit: **không được nâng lại `✅` nếu chưa có evidence mới**.

### 7.1 Map section cũ

| Claim cũ | Audit result |
|---|---|
| `MapCatalog.json + PC maplist.ini -> 1,005` | Wording sai: raw `MapCatalog.json` chỉ là legacy catalog; `1,005` đến từ runtime merge `MapAliasCatalog`/`MapGeometryCatalog`/`MapServerRegionCatalog` + PC map data. |
| Map 907 raw name | Raw alias row còn label khác; runtime `MapPortManifest` override tên hiển thị thành `Vượt ải Nhiếp Thí Trần`. |
| Map 389 `Map_389_C 516 Region_C` | Đúng cho historical standalone TestData; current bulk geometry shared map IDs 387-389 có count khác. Không dùng làm current runtime proof. |
| Waypoint `225 -> 224 ✅` và Wharf `11 -> 10 ✅` | Mismatch denominator; giữ `🔄` cho tới khi document exact skipped row/reason. |
| Region_S trap runtime | Không full complete: 804 deterministic actions, 13 resolved deferred (`TongMapEntrance=8`, `ClearSkillTeamEnterHole=4`, `CityWarJoinRouter=1`). |

### 7.2 Faction / skill / item claims cũ

| Claim cũ | Audit result |
|---|---|
| 10 phái / UI chọn phái / ngũ hành / alignment đều `✅` | Chỉ có static runtime/catalog; faction UI có hard-code và một số text/element không chứng minh PC parity. Giữ `🔄`, riêng faction titles 81 có data proof tốt. |
| Faction Maps 33 `✅` | Overclaim rõ: `FactionMapService` tìm `Reference/PcTong/faction_map.txt` nhưng file không tồn tại; mobile count thực tế cần coi là 0/unverified. |
| Base skills 1,216 `✅` | Có data catalog proof (`Reference/PcSkill/skills.txt` 1,216 rows). Chỉ nghĩa là catalog/lookup, không nghĩa full skill behavior. |
| Extended skills 1,712 `✅` | Overclaim: mobile `ModSkills.txt` subset, không thấy PC `skills1.txt` full 1,712 được copy/parse. Giữ `🔄`. |
| Skill templates 219 `✅` | Overclaim: không thấy đúng `skilltemplate.txt`; parser có nguy cơ đọc nhầm file missile. Giữ `☐/🔄`. |
| Special skills 58 / NPC skills 43 / Translife skills 9 `✅` | Source files default không tồn tại hoặc service count có thể 0. Giữ `☐/🔄`. |
| Missile effects 480 `✅` | Count chưa khớp audited files (`missles.txt`, `missles1.txt`, template). Giữ `🔄` tới khi có verifier. |
| Magic attributes 333 `✅` | Count audit là 330 data rows, không phải 333. |
| Compound/Recipe 1,294 `✅` | Recipe data có; craft execution/UI còn stub/không chứng minh inventory consume/result. Giữ `🔄`. |
| Item Exchange / Hongbao `✅` | Runtime default path/data mismatch hoặc missing directory; giữ `☐/🔄`. |

### 7.3 NPC / quest / event / battle / guild claims cũ

| Claim cũ | Audit result |
|---|---|
| NPC definitions 2,000 `✅` | Data-level OK (`npcs.txt` 2,000 rows), nhưng dialog/level-script/AI không full PC behavior. |
| Monster spawns 5,384 `✅` | Current proof là Region_S `67,680` records + visuals; `settings/normal.txt` 5,384 table chưa được chứng minh full port. Giữ `🔄`. |
| NPC Dialog / NPC Level Scripts / Enemy AI `✅` | Overclaim: service/framework/generic AI, chưa execute PC Lua/semantic scripts. Giữ `🔄`. |
| Mission Scripts 985 `✅` | Overclaim: `MissionScriptService` là metadata registry/helper, không full Lua executor. Giữ `🔄`. |
| QuestService full quests `✅` | Contains hard-coded/sample quest chains; không phải full PC mission script parity. Giữ `🔄`. |
| Server/VNG/Seasonal/Compensation events `✅` | Missing dedicated reference dirs/indexes for many event systems; parser/service skeleton only. Giữ `☐/🔄`. |
| Tống Kim / Quốc Chiến / Hoa Sơn / CityWar `✅` | Core services exist, but full PC battle join/state/item/guild gates chưa proven; `CityWarJoinRouter` còn deferred. Giữ `🔄`. |
| Battle Scripts 183 `✅` | Runtime has simplified/log/mock behavior; not full PC script semantics. Giữ `🔄`. |
| Guild Scripts 65 `✅` | PC `script/tong` Lua not executed; `GuildScriptService.ExecuteScript()` mostly validate/log/return. `TongMapEntrance` traps deferred. Giữ `🔄`. |

### 7.4 UI / visual / infrastructure / scripts claims cũ

| Claim cũ | Audit result |
|---|---|
| Test count `1771/1771 EditMode`, `25/25 PlayMode` | Not verified; no TestResults artifact in audit. Must rerun Unity tests before recording exact pass count. |
| HUD Art / Client UI `✅` | Dirty HUD WIP exists and is excluded; committed baseline has many assets/tests but not 100% PC HUD parity. Giữ `🔄`. |
| GM teleport/browser evidence | User-owned/out-of-scope; không dùng làm proof cho map/UI port audit. |
| Male/Female/Mount/NPC visual `✅` | Base runtime/catalog/tests exist; full PC equipment/layer/wardrobe/mount coverage chưa audit. Giữ `🔄`, except SPR decoder/runtime core can be `✅`. |
| Multi-language toàn UI `✅` | Overclaim; many Vietnamese labels exist, full localization audit chưa làm. Giữ `🔄`. |
| Section 14 GBK dirs `✅` | Treat as registry/parser coverage, not semantic script execution. Giữ `🔄`. |
| Section 15 Server Scripts `✅` | Contradicts old summary “Lua Scripts ~0%”; use `🔄 indexed/cataloged, semantic runtime partial` until full Lua semantics are implemented. |
