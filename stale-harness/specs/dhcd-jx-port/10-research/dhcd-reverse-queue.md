# DHCD Reverse Queue

| Trường | Giá trị |
|---|---|
| Mục đích | Danh sách reverse có thứ tự, không tự bịa behavior |
| Trạng thái | `provisional` |
| Owner / reviewer | Reverse owner / DHCD reviewer |
| Cập nhật | 2026-07-16 |

## Queue

| ID | Priority | Owner / research_state | Câu hỏi | Evidence hiện có | Cách làm/dependency | Gate |
|---|---|---|---|---|---|---|
| R-DHCD-001 | P0 | reverse-owner / in_progress | Card count/weight/cost/cap | `E-DHCD-R001` proves 1,879-row center `900` is an exact subset of 2,023-row battle `999`/Android `fp_`, adding 144 pool-`999` rows; both iOS `1.351` logical paths have fail-closed DODAB1 envelope evidence; exact named ISIL starts uniquely map all 26 `LevelRandomSkillCtrl` methods to BattleCore slots `4182`-`4207`; registration maps wrapper `0x01542908` to `LevelBootyHelper.RandomItem<RandomSkillConfig>` and six-parameter selection body `0x015429fc` to method definition `24134`; exact DodFixLib metadata/codegen slots identify threshold pair `0x00c4fba0`/`0x00cdf598` as `FP.op_Implicit(Int32)` and `TSRandom.Next(FP, FP)`, whose caller uses FP zero and cumulative total; the Android key/runtime still fails encrypted AssetBundles | Recover the `NextFP` exact endpoint/distribution boundary and card-path delegate/config bindings; recover the exact package/build key and selected config path; corroborate caller offer count, active weight, cost, and cap with versioned config or successful runtime; block deck constants | deck design |
| R-DHCD-002 | P0 | reverse-owner / queued | Modal queue và input lock | `docs/evidence/r-dhcd-002-modal-queue.md` proves role-keyed controller branches and pending-event call order; player-data callee bodies, FIFO, input lock, pause, and cross-player serialization remain unresolved | trace event order and modal show/close; do not assume serialization; hard dependency R-DHCD-003 | combat/UX |
| R-DHCD-003 | P0 | reverse-owner / in_progress | Pause/timeScale semantics | `docs/evidence/r-dhcd-003-pause-timescale.md` plus the hash-locked GameLogic pointer-table inspector prove `BattleSys.set_IsPause` counter updates and the normal card `OnVisible(true)` / `OnHidden(false)` caller path; Quick UI, the `ReCalcTimeScale` sink identity, global simulation scope, input lock, and timer effects remain unresolved | resolve native sink `0x0099B6B8`, remaining pause callers/Quick UI behavior, and an authorized runtime trace; do not infer global `Time.timeScale` or timer/input semantics from malformed C# | multiplayer |
| R-DHCD-004 | P0 | reverse-owner / in_progress | Normal solo vs multiplayer | `docs/evidence/r-dhcd-004-mode-selection.md` and the hash-locked inspectors (mode-selection `schema_version` 6, JSON `185d8092…`; generic-context `schema_version` 1, JSON `53c2b741…`) prove the raw predicate boundary, raw-value-gated helper-call boundaries with **both `m_listPlayer` helper full bodies hash-locked by unique generic-pointer-table slot** (11733/11741, dispatch offsets byte-locked), three individually locked `CreateActorEntity` callers, and a **complete static caller partition** of all 24 direct `CreateActorEntity` BL sites = 12 `CreatePlayerCreateData`-linked actor sites + 12 outside-factory sites (2 already locked, 10 deepened: 4 factory-return-proven, 2 local-init-unresolved, 4 field-local-unresolved). A fail-closed generic-context inspector enumerates edge `0x0186C0A8` as generic slot 133 = exactly 50 `System.Action<T>.Invoke` rows with `BattleCore.ActorEntity` one candidate (not caller-selected) and caller `x2` decoding only to encoded placeholder `0xC0000183`. The packaged-config schema is decoded but uninterpreted. Field/parameter labels remain ISIL-correlated; static call presence does not prove runtime reachability, actor semantics, mode selection, solo/co-op authority, parity, or a load-order winner. | Next (static selection candidates / input provenance only): reduce the 2 local-init-unresolved and 4 field-local-unresolved outside sites to a bounded static createData producer; decode runtime `MethodInfo` `0xC0000183` → one of 50 slot-133 rows; resolve the generic `x2` dispatch operands of the two helpers. Runtime selection/reachability requires an authorized same-build runtime capture and is not promised by static analysis; keep raw values `1`/`2`, method/declaring-type names, `w2` constants, zero `m_LevelId`, catalog membership, and the 50-row/1610/93 fan-out counts non-semantic. | mode catalog |
| R-DHCD-005 | P1 | gameplay-owner / blocked | Wave ownership/co-op | level/wave/player fields | trace room/event semantics; ADR-006 trước co-op | ADR-006 |
| R-DHCD-006 | P0 | economy-owner / queued | Drop/XP exact balance | `docs/evidence/r-dhcd-006-drop-xp.md` records hash-checked candidates and schema/caller surfaces; active bundle, reward constants, and formulas remain blocked | bind selected VFS/AssetBundle bytes, then trace `LevelExpCalc`/`LevelCollectItemMgr`; reject candidate rows as active balance without binding | reward |
| R-DHCD-007 | P1 | server-owner / deferred | Reconnect/matchmaking | client corpus gap | reverse only if scope changes; otherwise new contract | server |
| R-DHCD-008 | P3 | reverse-owner / deferred | PvP/royal lifecycle, authority và reward semantics | chưa có evidence đủ | dùng `/var/www/reverse-skill` khi P3 được product approve; sau đó security/economy ADR | PvP feature flag |
| R-DHCD-009 | P2 | reverse-owner / deferred | Boss/escort/tower lifecycle, AI, map và reward | chưa có evidence đủ | reverse từng mode trước khi thiết kế parity; cập nhật corpus và evidence card | special-mode gate |
| R-DHCD-010 | P3 | reverse-owner / deferred | Social/guild/leaderboard/faction-update behavior | chưa có evidence đủ | reverse chỉ khi claim DHCD parity; contract mới vẫn cần product/data/security ADR | social feature flag |
| R-DHCD-011 | P2 | reverse-owner / deferred | Mount/pet lifecycle, unlock, actor/equipment/skill interaction | chưa có evidence đủ | dùng `/var/www/reverse-skill`, cập nhật corpus trước mọi claim DHCD; JX identity/visual audit riêng | mount/pet feature flag |

## Protocol

`research_state` is independent of the Harness story lifecycle: an admitted
story may be `in_progress` while its linked research remains `queued`. Queue
transitions reflect reverse execution evidence, not packet admission.

Mỗi task lưu command, input hashes, output path, failed methods, confidence và reviewer. Không đưa result vào `verified` nếu chỉ có generated declaration.

## Acceptance

- [ ] Mỗi task có priority, owner/state, dependency và gate như bảng.
- [ ] Output reverse được ghi vào `/home/zet/Projects/dhcd` cùng input hash và failed-method log.
- [ ] Chỉ đóng task khi evidence card/ADR và test liên quan tồn tại.
