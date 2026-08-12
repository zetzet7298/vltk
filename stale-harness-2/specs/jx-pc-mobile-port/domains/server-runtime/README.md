# Server Runtime domain

Python 3 + FastAPI modular monolith trong `backend/` là backend game duy nhất.
Module giữ ranh giới domain/application/infrastructure/API và không đọc/ghi bảng
của module khác. REST hiện phục vụ account, role, map, movement và các slice
gameplay đã port; realtime server-authoritative 18 Hz vẫn là mục tiêu chưa hoàn tất.

| Module | Owned data |
| --- | --- |
| identity | `accounts`, `auth_sessions` |
| realm | `realms` |
| character | `characters`, stats/position |
| runtime | `runtime_checkpoints`, session/18 Hz loop |
| inventory/skill-combat | item/skill state và command |
| economy | wallet, double-entry ledger |
| content | release/artifact/config/Lua provenance |
| platform | idempotency, outbox, audit |

```text
backend/app/modules/<module>/domain
backend/app/modules/<module>/application
backend/app/modules/<module>/infrastructure
backend/app/modules/<module>/api/v1
backend/app/infrastructure
```

Tick là 18 Hz; `client_seq` tăng trong `session_epoch`, duplicate được ACK lại,
gap window tối đa 64. Snapshot có tick/baseline/last processed/checksum. Một
character tối đa một WSS session active. Runtime pin content release đến khi
reconnect. Checkpoint durable tối đa mỗi 90 tick (5 giây) và khi disconnect;
blob phải qua SHA-256 trước hydrate.

Account có tối đa 3 character active/realm; delete là soft-delete 7 ngày.
Lua mặc định tối đa 100.000 instruction, 5 ms và 8 MB/invocation, không có
filesystem/network/process/wall-clock/random không seed. Backup PostgreSQL 16
có RPO 5 phút, RTO 60 phút.

Mọi request context đích có request/trace/realm/actor/content release. Inventory,
economy, checkpoint và outbox liên quan phải commit transactionally. Realtime,
drain session và checkpoint đầy đủ còn `[CẦN XÁC NHẬN]`; không được suy diễn từ
REST movement hiện tại. Contract normative ở `../../contracts/`; implementation
nằm trong `backend/` ở repository root.
