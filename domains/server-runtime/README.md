# Server Runtime domain

## Mục tiêu và ranh giới

`backend/` là Python 3 + FastAPI modular monolith, một deployment unit ban đầu
với các module DDD có data ownership và API nội bộ rõ ràng. Server authoritative đối với
identity, character, vị trí, combat, inventory và economy; Unity chỉ dự đoán
để hiển thị rồi reconcile theo snapshot server.

| Module | Trách nhiệm | Owned tables | Giao tiếp |
| --- | --- | --- | --- |
| `account` | Tạo/login/logout account game | `accounts` | REST `/v1/account` |
| `role` | Tạo/chọn nhân vật | `roles` | REST `/v1/role` |
| `map` | Map catalog, scene và movement hiện tại | `role_scenes` | REST `/v1/map`, `/v1/movement` |
| `runtime` | Session, input ordering, tick 18 Hz, checkpoint | `[CẦN XÁC NHẬN]` | Realtime chưa triển khai |
| `inventory` | Item instance, slot và quantity invariant | `inventory_items` | command nội bộ |
| `skill-combat` | Skill state, cast gate, damage/buff | `character_skills` | command WSS |
| `economy` | Wallet, double-entry ledger, idempotency | `wallets`, `economy_*` | command nội bộ |
| `content` | Kích hoạt release config/Lua đã ký | `content_*`, `lua_modules` | bootstrap/read-only |
| `platform` | outbox, audit, observability | `outbox_events`, `audit_events`, `idempotency_keys` | worker nội bộ |

Không module nào được đọc/ghi trực tiếp bảng do module khác sở hữu. Query tổng
hợp dùng interface application hoặc projection; transaction xuyên module chỉ
được điều phối trong cùng process qua command rõ ràng và PostgreSQL transaction.

## Bố cục triển khai đích

```text
backend/app/main.py                         composition root FastAPI
backend/app/modules/<module>/domain         aggregate và invariant thuần Python
backend/app/modules/<module>/application    use case, schema và port
backend/app/modules/<module>/infrastructure SQLAlchemy/adapters
backend/app/modules/<module>/api/v1         FastAPI routers
backend/app/infrastructure                  UoW, database và platform adapters
```

Implementation hiện hữu nằm tại `backend/`. Dependency hướng vào
`domain`/`application`; module không import infrastructure concrete của module
khác. Request correlation/realm/content context đầy đủ còn phải được chứng minh
trước production.

## Invariant runtime

- Một character chỉ có tối đa một session WSS active; reconnect thay thế kết
  nối cũ bằng `session_epoch` tăng đơn điệu.
- Tick server cố định 18 Hz (`55,555,556 ns` theo bộ chia tích lũy); `tick` tăng
  đúng một. Không dùng timestamp client để sắp thứ tự command.
- `client_seq` tăng đơn điệu trong một `session_epoch`. Duplicate được ACK lại;
  gap được giữ tối đa trong cửa sổ 64 command rồi yêu cầu resync.
- Snapshot có `server_tick`, `baseline_tick` và `last_processed_client_seq`.
  Client không áp delta nếu thiếu baseline và phải gửi `ResyncRequest`.
- Thay đổi inventory/economy và checkpoint phát sinh trong một DB transaction;
  event tích hợp ghi cùng transaction qua transactional outbox.
- Runtime pin đúng một `content_release_id` trong suốt session. Release mới chỉ
  áp dụng sau reconnect hoặc ranh giới migration được công bố.

## Luồng chính

1. Production Editor client tạo/login account qua `/v1/account`.
2. Client tạo/chọn role qua `/v1/role`.
3. Client vào canonical map `53` qua `/v1/map/enter` và gửi vị trí qua
   `/v1/movement`.
4. Realtime 18 Hz, admission, resume và checkpoint là follow-up riêng; REST
   movement hiện tại không được xem là bằng chứng hoàn tất các seam đó.

## Failure và vận hành

- PostgreSQL unavailable: từ chối login/admission mới; session đang chạy chỉ
  tiếp tục trong grace window [CẦN XÁC NHẬN], không commit economy offline.
- Content digest/signature sai: release không được activate; giữ release active
  trước đó và phát alert.
- Tick lag: đo p50/p95/p99, giới hạn catch-up [CẦN XÁC NHẬN]; không bỏ qua
  command kinh tế/combat đã ACK.
- Shutdown: ngừng admission, drain WSS, flush checkpoint/outbox rồi thoát. Mốc
  drain và capacity cần benchmark trước production.

Normative contracts nằm tại `../../contracts/`; thiết kế dữ liệu CNPM nằm tại
`../../harness/specs/jx-pc-mobile-port/03-du-lieu.md`.
