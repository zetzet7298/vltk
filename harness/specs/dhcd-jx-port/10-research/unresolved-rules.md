# Unresolved Rules

| Trường | Giá trị |
|---|---|
| Mục đích | Một nơi duy nhất cho unknown/conflict không được hard-code |
| Trạng thái | `provisional` là inventory gap đã review; từng rule chưa resolved |
| Owner / reviewer | Reverse owner / gameplay reviewer |
| Cập nhật | 2026-07-15 |

| Rule | Hiện trạng | Impact | Quy tắc tạm thời | Owner/gate |
|---|---|---|---|---|
| Card count/weight/cost/cap | Chưa recovered đủ | deck/balance/replay | data versioned, không claim parity | R-DHCD-001 |
| Modal input lock/global pause | Có per-player state/waiting-list declaration; serialization và global pause chưa chứng minh | UX/co-op | queue/ordering là design tạm thời; không claim parity hoặc pause global | R-DHCD-002/003 |
| Normal solo | `IsMultiPlayer` tồn tại | mode scope | target pilot, không recovered fact | R-DHCD-004 |
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
