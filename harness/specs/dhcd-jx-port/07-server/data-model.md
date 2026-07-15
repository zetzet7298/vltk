# PostgreSQL Data Model

| Trường | Giá trị |
|---|---|
| Mục đích | Schema canonical cho account/progression/inventory/run/replay |
| Trạng thái | `not_started` |
| Owner / reviewer | Data owner / server reviewer |
| Cập nhật | 2026-07-15 |

## Database boundary

Dùng database và role riêng trong `/var/www/tt-docker/`; đọc `.env` tại runtime, không commit secret. Harness SQLite (`scripts/schema/001-init.sql`) chỉ chứa metadata test.

## Core tables (to-be)

`accounts`, `sessions`, `characters`, `faction_catalog_versions`, `skill_defs`, `item_defs`, `inventory_items`, `equipment`, `config_snapshots`, `runs`, `run_inputs`, `run_checkpoints`, `replay_blobs`, `reward_proposals`, `idempotency_keys`, `audit_events`, `quarantine_cases`.

## Invariants

- Immutable IDs và catalog version.
- Inventory/equipment transaction atomic; optimistic `row_version`.
- Unique `(account_id, idempotency_key, operation)`.
- Reward commit chỉ từ verified run.
- `runs.config_snapshot_id` trỏ tới snapshot bất biến có hash; snapshot/replay tồn tại suốt retention của reward.
- Replay retention tối thiểu 30 ngày; diagnostic payload size limit.
- Soft delete/account erasure policy phải được legal/security approve.

## Migration

Versioned SQL, expand/contract, backward reader trong retention, checksum và rollback script. Chưa tạo schema; không gọi persistence hiện tại là PostgreSQL.

## Acceptance

- [ ] SQL migration tạo đủ core tables/constraints/indexes và chạy trên database/role riêng.
- [ ] Transaction/idempotency/version-conflict tests pass.
- [ ] Config snapshot/replay retention và rollback/restore được kiểm chứng.
