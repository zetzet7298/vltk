# Acceptance Gates

| Trường | Giá trị |
|---|---|
| Mục đích | Điều kiện chuyển P0 từ design/provisional sang pilot verified |
| Trạng thái | `design` |
| Owner / reviewer | QA owner / product owner |
| Cập nhật | 2026-07-15 |

## P0 gate

- [ ] Source hierarchy và evidence register complete; không có claim critical không provenance.
- [ ] Legal clearance hoặc approval internal-only được ghi rõ; public release bị chặn nếu chưa cleared.
- [ ] Một arena có Region_C/collision/map golden.
- [ ] Ba phái có một build verified và starter gear verified.
- [ ] Portrait flow/HUD/input/safe-area golden.
- [ ] Normal solo loop wave/card/reroll chỉ dùng rule đã reverse/xác minh; deviation có reverse evidence và ADR đã approve.
- [ ] Go verifier + C# mirror golden vectors pass.
- [ ] Guest account, inventory transaction, reward idempotency pass.
- [ ] PostgreSQL migration/backup/restore, RPO/RTO evidence.
- [ ] 100 CCU load smoke đạt các threshold REST/WSS/tick/verifier/5xx/unexpected-quarantine trong [performance](performance.md), và client đạt gate 60 FPS trên device manifest máy tầm trung đã pin.
- [ ] Named security negatives: forged/altered replay, sequence/tick violation, duplicate completion, oversized upload, revoked key, authorization/account takeover attempt, quarantine auditability.
- [ ] `pilot_deliverables` chứa roster/starter gear, gameplay loop, portrait/HUD/localization và reuse/migration gates; mọi ID runtime/content đều đạt `verified`.

## Phase Requirements

Mỗi ID trong `status.yaml:phase_requirements` phải có row tương ứng theo priority trong [traceability](../01-governance/traceability.md), lặp provenance/legal/contract/test/rollback gate và thêm security/anti-cheat nếu competitive. P0 dùng pilot gates; P1-P3 phải giữ feature flag off cho tới khi gate tương ứng đạt. Roadmap bullet một mình không đủ làm acceptance contract.

## Acceptance

- [ ] Gate checklist được ký bởi owner/reviewer và liên kết build/config/test artifacts.
- [ ] `pilot_deliverables` không bao gồm template/governance-only docs.
- [ ] Mọi unchecked item có blocker, owner và ngày xem lại.
