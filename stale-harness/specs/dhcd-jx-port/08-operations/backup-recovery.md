# Backup Và Recovery

| Trường | Giá trị |
|---|---|
| Mục đích | Đảm bảo pilot có RPO/RTO đo được |
| Trạng thái | `not_started` |
| Owner / reviewer | Ops owner / data owner |
| Cập nhật | 2026-07-15 |

## Contract

- Backup PostgreSQL hằng ngày, retention tối thiểu 30 ngày; encrypted và immutable/off-host.
- Replay/checkpoint blob backup theo policy; không backup secret cùng DB.
- Pilot target RPO <= 24h, RTO <= 4h.
- Migration/version/checksum lưu cùng backup metadata.

## Drill

Hàng quý hoặc trước release: restore vào isolated environment, chạy schema/checksum, sample account/inventory/replay verification, đo thời gian và ghi gap. Backup failure là release blocker.

## Acceptance

- [ ] Backup encrypted/off-host, key recovery và retention 30 ngày được chứng minh.
- [ ] Restore drill đo RPO <= 24h và RTO <= 4h trên isolated environment.
- [ ] Restore sample reward/replay và migration checksum pass.
