# Kế hoạch phát hành

## Hạ tầng dữ liệu được chốt

- Production/staging dùng PostgreSQL 16 từ stack `/var/www/tt-docker`; secret chỉ
  được inject qua environment/secret store của stack.
- Release gate phải ghi DSN target không chứa credential, server major version và
  migration result. Drift sang deployment khác chặn G6 cho đến khi có ADR thay thế.

## Môi trường

- `dev`: Unity, Go, PostgreSQL database/role riêng và DevHarness profiles.
- `pre-prod ephemeral`: CI dựng stack theo cấu hình production, restore dữ liệu đã ẩn danh, chạy migration/load/compat/restore rồi hủy.
- `production`: một realm ban đầu; artifact được promote, không rebuild giữa các môi trường.

## Versioning

- Release pin app build, Go image, protocol version, content version, database migration set và golden manifest hash.
- Server hỗ trợ `N` và `N-1` tối đa 7 ngày với thay đổi additive; content không tương thích sẽ chặn gameplay và yêu cầu update.
- Core + Ba Lăng đóng gói để first-playable; content lớn qua Addressables/platform delivery, cache khoảng 12GB và cho phép quản lý pack.

## SLO và recovery

| Chỉ số | Mục tiêu |
| --- | --- |
| Availability | 99.5% mỗi tháng |
| RPO | <=5 phút |
| RTO | <=60 phút |
| Reconnect grace | 30 giây |
| Checkpoint age | <=5 giây |

PostgreSQL 16 hiện có được dùng với database, role và migrations riêng. Backup tự động, WAL archive, PITR và restore drill là gate; không đặt credential vào specs.

## Release checklist

1. `spec-validator --mode release` pass.
2. OpenAPI/Proto breaking check, deterministic replay và visual gates pass.
3. Migration dry-run, restore và rollback rehearsal pass.
4. Load/soak/device gate pass trên release manifest.
5. Promote immutable artifacts; theo dõi error/tick/checkpoint/economy; rollback theo runbook nếu breach.
