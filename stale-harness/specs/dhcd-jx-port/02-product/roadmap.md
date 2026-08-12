# Roadmap Và Ưu Tiên

| Trường | Giá trị |
|---|---|
| Mục đích | Chia delivery thành slice có dependency và rollback |
| Trạng thái | `design` |
| Owner / reviewer | Product owner / technical lead |
| Cập nhật | 2026-07-15 |

## P0: evidence và playable pilot

1. Source pinning, evidence register, legal request, full catalog audit ba phái.
2. Arena audit và chọn một map có exact collision.
3. Portrait foundation, player/HUD, starter gear và guest account.
4. DHCD-style normal solo loop: wave, drop, XP, card/reroll theo rule đã reverse/xác minh; deviation chỉ được dùng sau reverse evidence và ADR đã approve.
5. Go verifier, checkpoint/replay, PostgreSQL riêng, feature flags và internal pilot.

Exit internal-only: toàn bộ P0 acceptance gate pass, mọi kênh pilot vẫn internal-only, legal owner có approval nội bộ ghi phạm vi/thời hạn, rollback đã diễn tập. Exit public distribution là gate hậu pilot riêng và chỉ đạt khi clearance quyền phân phối asset không còn blocker cùng release approval.

## P1: coverage và co-op

- Mở rộng skill/item đã audit.
- Inventory/equipment portrait hoàn chỉnh.
- Co-op relay/verify chỉ sau khi wave ownership và reconnect được reverse/design.
- Performance hardening và [content release pipeline](../08-operations/ci.md#p1-content-release-pipeline).

## P2/P3

- P2: mode đặc biệt, boss/escort/tower, mount/pet, thêm arena/item sau evidence gate.
- P3: PvP/royal, social/guild/leaderboard, faction updates khi có security/product decision.

## Rule delivery

Mỗi slice có feature flag, migration test, telemetry, rollback trigger và expiry date. Không merge content mới nếu provenance hoặc legal state chưa đạt.

## Acceptance

- [ ] Mỗi phase có owner, dependency, exit signal và rollback trigger.
- [ ] Pilot deliverables khớp `status.yaml:pilot_deliverables`.
- [ ] P0/P1/P2/P3 requirement IDs khớp `status.yaml:phase_requirements` và [traceability](../01-governance/traceability.md).
- [ ] Internal/public legal exit không mâu thuẫn với [legal-clearance](../10-research/legal-clearance.md).
