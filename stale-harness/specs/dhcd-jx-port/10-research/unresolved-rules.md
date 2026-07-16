# Unresolved Rules

| Trường | Giá trị |
|---|---|
| Mục đích | Một nơi duy nhất cho unknown/conflict không được hard-code |
| Trạng thái | `provisional` là inventory gap đã review; từng rule chưa resolved |
| Owner / reviewer | Reverse owner / gameplay reviewer |
| Cập nhật | 2026-07-16 |

| Rule | Hiện trạng | Impact | Quy tắc tạm thời | Owner/gate |
|---|---|---|---|---|
| Card count/weight/cost/cap | Server role binaries prove a 1,879-row center `900` subset and byte-identical 2,023-row battle `999`/Android `fp_` candidate with 144 pool-`999` additions. Both iOS `1.351` logical paths exist inside undecoded DODAB1 envelopes. Named ISIL starts uniquely map all 26 `LevelRandomSkillCtrl` methods to native pointer slots; exact slices prove caller-provided counts, `Math.Min` clamping, and a remaining-count loop. Registration resolves wrapper `0x01542908` and six-parameter selection body `0x015429fc`; the exact body proves cumulative delegate weights, signed-less-than hit testing, predicate-controlled put-back, and zero/subtract updates when not put back. The threshold calls are exactly `FP.op_Implicit(Int32)` at `0x00c4fba0` and `TSRandom.Next(FP, FP)` at `0x00cdf598`, called with FP zero and cumulative total; `NextFP` endpoint/distribution, active config/weight binding, offer count, cost, and cap remain unresolved. The Android embedded/bootstrap key yields invalid UnityFS magic and the captured runtime fails encrypted loads | deck/balance/replay | weighted selection with per-item replacement is a high-confidence reconstruction, not active parity; do not claim inclusive/exclusive threshold bounds, candidate data, role paths, row counts, `md5_ex`, DODAB1 sizes, or failed-key runtime as active selection/offer proof; `E-DHCD-R001` remains overall `unresolved` | R-DHCD-001 |
| Modal input lock/global pause | Native controller proves role-keyed lookup plus conditional pending-event calls. A hash-locked GameLogic mapping additionally proves the normal card UI acquires/releases a `BattleSys` pause counter, but Quick UI, the `ReCalcTimeScale` sink identity, modal input lock, global simulation scope, timer effects, FIFO, and cross-player serialization remain unresolved | UX/co-op | do not promote the normal-card counter path to global `Time.timeScale`, FIFO, timer, or input-lock parity; use `docs/evidence/r-dhcd-002-modal-queue.md` and `r-dhcd-003-pause-timescale.md` as fail-closed evidence | R-DHCD-002/003 |
| Normal solo | `ActorEntityMgr.IsMultiPlayer` is proven as `m_listPlayer.Count > 1`. The static caller set is now fully partitioned: all 24 direct `CreateActorEntity` BL sites = 12 `CreatePlayerCreateData`-linked actor sites (distinct conditional-static chains, 11 include `SetBornPos`) + 12 outside-factory sites (2 already locked, 10 deepened: 4 factory-return-proven, 2 local-init-unresolved-identity, 4 field-local-unresolved-factory). Both `m_listPlayer` helper full bodies are hash-locked by unique generic-pointer-table slot (11733/11741, dispatch offsets byte-locked, generic binding unresolved). Edge `0x0186C0A8` is enumerated as generic slot 133 = exactly 50 `System.Action<T>.Invoke` rows with `BattleCore.ActorEntity` one non-caller-selected candidate and caller `x2` decoding only to encoded placeholder `0xC0000183`. Runtime reachability and semantics remain unproven | mode scope | no pilot or solo/co-op parity inference; no selector, authority, runtime-parity, or load-order-winner inference from names, declaring types, raw values, `w2` constants, fan-out counts (50/1610/93), static call presence, or catalogs; field/parameter labels are ISIL-correlated; runtime selection/reachability requires an authorized same-build runtime capture; see `r-dhcd-004-mode-selection.md` | R-DHCD-004 |
| Drop/XP exact balance | Hash-checked candidate/config surfaces exist, but active VFS/bundle selection and `LevelExpCalc`/drop formulas are not proven | reward balance | candidate rows are not active reward constants; keep blocked until selected bytes and native/runtime trace agree | R-DHCD-006 |
| Arena map ID/collision | candidate names only | map/camera/spawn | không chọn pilot trước Region_C audit | arena audit |
| NPC/item mappings | một phần synthetic/provisional | visual/stats/drop | content gate blocked | JX roster manifests |
| Skill visual linkage | catalog chưa full | missing effect/audio | fail closed, không fallback | skill manifest |
| Reconnect/matchmaking | chưa có server artifact | online reliability | design new Go contract | server shards |
| PvP/royal | chưa scope/evidence | security/economy/reward | P3 off; không claim parity | R-DHCD-008 + product/security ADR |
| Boss/escort/tower | chưa scope/evidence từng mode | AI/map/reward | P2 off; reverse từng mode trước parity design | R-DHCD-009 |
| Mount/pet | chưa có lifecycle/interaction evidence và chưa audit đủ JX visual | actor/equipment/skill/replay | P2 off; reverse trước, JX resolver manifest riêng, không synthesize | R-DHCD-011 |
| Social/guild/leaderboard/faction update | chưa scope/evidence | data/privacy/economy | P3 off; contract mới hoặc parity đều cần gate | R-DHCD-010 + product/data/security ADR |

Unknown chỉ đóng khi reverse/source audit đã tạo evidence card và test liên quan. Nếu evidence vẫn inconclusive mà cần ship deviation, phải có thêm ADR được approve; ADR không thay thế reverse/source audit.

## Acceptance

- [ ] Mỗi unknown có owner/gate và trạng thái tương ứng trong ledger.
- [ ] Không còn claim `verified` cho rule chỉ có declaration hoặc synthetic mapping.
- [ ] Khi đóng unknown, cập nhật traceability, ADR/evidence và regression test.
