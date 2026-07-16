# Server Runtime domain

Go 1.26 modular monolith là một deployment unit, nhưng module không đọc/ghi
bảng của module khác. REST phục vụ auth/bootstrap/character; WSS Protobuf
`game.v1` phục vụ simulation server-authoritative 18 Hz.

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
cmd/server
internal/<module>/domain
internal/<module>/app
internal/<module>/infra
internal/transport/{rest,wss}
internal/platform
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

Mọi request context có request/trace/realm/actor/content release. Inventory,
economy, checkpoint và outbox liên quan commit transactionally. Shutdown ngừng
admission, drain WSS, flush checkpoint/outbox. Contract normative ở
`../../contracts/`; không có backend code trong domain tài liệu này.
