# ADR Policy

| Trường | Giá trị |
|---|---|
| Mục đích | Quản lý quyết định khó đảo ngược và deviation khỏi source |
| Trạng thái | `design` |
| Owner / reviewer | Technical lead / product owner |
| Cập nhật | 2026-07-15 |

## Khi nào phải tạo ADR

Tạo ADR khi thay đổi source authority, protocol/save compatibility, simulation/verifier boundary, asset licensing, map collision, wave ownership, migration strategy hoặc một rule chưa recovered nhưng cần ship.

Không tạo ADR cho rename nội bộ, refactor không đổi behavior, hoặc config test tạm thời.

## Quy trình

1. Mở ADR ở trạng thái `proposed`, nêu context/evidence và options.
2. Ghi decision, trade-off, impact tới replay/save/visual/ops.
3. Có product owner và technical reviewer approve.
4. Liên kết requirement, implementation, test và rollback.
5. Khi thay đổi, supersede ADR cũ; không sửa lịch sử im lặng.

## ADR bắt buộc của dự án

- [`ADR-001`](adr-001-source-authority.md): JX visual/identity và DHCD loop evidence.
- [`ADR-002`](adr-002-reuse-migration.md): reuse-first, inventory và migration gates.
- [`ADR-003`](adr-003-deterministic-authority.md): Unity mirror, Go verifier, checkpoint/replay.
- [`ADR-004`](adr-004-portrait-foundation.md): portrait adaptive baseline.
- [`ADR-005`](adr-005-legal-evidence-gate.md): legal/evidence gate.
- `ADR-006`: wave ownership và co-op relay (tạo trước khi làm co-op).

Mẫu dùng [adr](../templates/adr.md). ADR chưa approve không được chuyển requirement sang `verified`.

## Acceptance

- [ ] ADR IDs/owner/approver/status không trùng.
- [ ] Deviation JX/DHCD, simulation boundary, legal và migration đều có ADR khi áp dụng.
- [ ] Supersede giữ lịch sử, test và rollback link.
