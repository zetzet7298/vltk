# DHCD Reverse Queue

| Trường | Giá trị |
|---|---|
| Mục đích | Danh sách reverse có thứ tự, không tự bịa behavior |
| Trạng thái | `provisional` |
| Owner / reviewer | Reverse owner / DHCD reviewer |
| Cập nhật | 2026-07-15 |

## Queue

| ID | Priority | Owner/state | Câu hỏi | Evidence hiện có | Cách làm/dependency | Gate |
|---|---|---|---|---|---|---|
| R-DHCD-001 | P0 | reverse-owner / queued | Card count/weight/cost/cap | `LevelRandomSkillCtrl`, `BattleLearnSkillCtrl` declaration/IL | Dùng `/var/www/reverse-skill`, cập nhật corpus; block deck constants | deck design |
| R-DHCD-002 | P0 | reverse-owner / queued | Modal queue và input lock | per-player request/state evidence | trace event order; không assume serialization; phụ thuộc R-DHCD-003 | combat/UX |
| R-DHCD-003 | P0 | reverse-owner / queued | Pause/timeScale semantics | malformed branch warning | native/IL corroboration; không suy diễn | multiplayer |
| R-DHCD-004 | P0 | reverse-owner / queued | Normal solo vs multiplayer | `NormalLevelLogic.IsMultiPlayer` | map call sites/config/mode | mode catalog |
| R-DHCD-005 | P1 | gameplay-owner / blocked | Wave ownership/co-op | level/wave/player fields | trace room/event semantics; ADR-006 trước co-op | ADR-006 |
| R-DHCD-006 | P0 | economy-owner / queued | Drop/XP exact balance | manager declarations | find serialized config/runtime values | reward |
| R-DHCD-007 | P1 | server-owner / deferred | Reconnect/matchmaking | client corpus gap | reverse only if scope changes; otherwise new contract | server |
| R-DHCD-008 | P3 | reverse-owner / deferred | PvP/royal lifecycle, authority và reward semantics | chưa có evidence đủ | dùng `/var/www/reverse-skill` khi P3 được product approve; sau đó security/economy ADR | PvP feature flag |
| R-DHCD-009 | P2 | reverse-owner / deferred | Boss/escort/tower lifecycle, AI, map và reward | chưa có evidence đủ | reverse từng mode trước khi thiết kế parity; cập nhật corpus và evidence card | special-mode gate |
| R-DHCD-010 | P3 | reverse-owner / deferred | Social/guild/leaderboard/faction-update behavior | chưa có evidence đủ | reverse chỉ khi claim DHCD parity; contract mới vẫn cần product/data/security ADR | social feature flag |
| R-DHCD-011 | P2 | reverse-owner / deferred | Mount/pet lifecycle, unlock, actor/equipment/skill interaction | chưa có evidence đủ | dùng `/var/www/reverse-skill`, cập nhật corpus trước mọi claim DHCD; JX identity/visual audit riêng | mount/pet feature flag |

## Protocol

Mỗi task lưu command, input hashes, output path, failed methods, confidence và reviewer. Không đưa result vào `verified` nếu chỉ có generated declaration.

## Acceptance

- [ ] Mỗi task có priority, owner/state, dependency và gate như bảng.
- [ ] Output reverse được ghi vào `/home/zet/Projects/dhcd` cùng input hash và failed-method log.
- [ ] Chỉ đóng task khi evidence card/ADR và test liên quan tồn tại.
