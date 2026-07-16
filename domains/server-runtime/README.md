# Server Runtime domain

## Mục tiêu và ranh giới

`server-runtime` là Go 1.26 modular monolith, một deployment unit nhưng các
module có data ownership và API nội bộ rõ ràng. Server authoritative đối với
identity, character, vị trí, combat, inventory và economy; Unity chỉ dự đoán
để hiển thị rồi reconcile theo snapshot server.

| Module | Trách nhiệm | Owned tables | Giao tiếp |
| --- | --- | --- | --- |
| `identity` | Login, refresh, revoke, WSS ticket | `accounts`, `auth_sessions` | REST `auth` |
| `realm` | Realm availability, admission | `realms` | bootstrap nội bộ |
| `character` | Tạo/chọn nhân vật, chỉ số lâu dài | `characters`, `character_stats` | REST `characters` |
| `runtime` | Session game, input ordering, tick 18 Hz, checkpoint | `runtime_checkpoints` | WSS `game.v1` |
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
cmd/server                 composition root duy nhất
internal/<module>/domain   aggregate, invariant, domain event thuần Go
internal/<module>/app      use case và port
internal/<module>/infra    PostgreSQL/crypto/content adapters
internal/transport/rest    OpenAPI adapters
internal/transport/wss     Protobuf framing, admission và tick loop
internal/platform          tx, outbox, clock, ids, telemetry
```

Đây là quy ước kiến trúc, không phải yêu cầu tạo backend code trong slice tài
liệu này. Import được phép hướng vào `domain`/`app`; module không import
`infra` của module khác. Mỗi request có `request_id`, `trace_id`, `realm_id`,
actor và content release trong context.

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

1. REST login cấp access token ngắn hạn và refresh token xoay vòng.
2. `GET /v1/bootstrap` trả realm, release content và WSS ticket dùng một lần.
3. Client chọn/tạo character bằng REST, sau đó mở `wss://.../v1/game` với
   subprotocol `game.v1` và gửi `ClientHello`.
4. Runtime nạp checkpoint, xác nhận `ServerHello`, nhận `InputBatch`, mô phỏng
   18 Hz và phát snapshot/delta.
5. Khi disconnect, runtime flush checkpoint; outbox worker phát sự kiện
   at-least-once, consumer dedupe bằng `event_id`.

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
