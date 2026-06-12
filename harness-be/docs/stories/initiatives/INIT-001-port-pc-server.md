# Initiative: Port VLTK PC Game Server → FastAPI Backend

Status: active
Intake: #1 (new-initiative, lane high-risk)
Owner: backend port team (harness-be)

## Goal

Port 100% logic và behavior của game server VLTK PC (Server 6.0 — binary
`jx_linux_y` + Lua scripts) sang một backend **độc lập** viết bằng FastAPI trên
nền framework tt-fw, dùng PostgreSQL, chạy trên hạ tầng tt-docker đang có.

Target backend: `/var/www/vltk-mobile/backend`.

## Nguyên tắc bất biến (source of truth)

1. **Source of truth = server PC**. Mọi hành vi phải khớp với:
   - Lua scripts: `"/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script"`
   - Config/INI: `.../server1/settings`, `.../server1/*.ini`
   - Gateway/login: `.../home_jxser/gateway` (bishop/goddess) + paysys
   - DB seed: `.../home_database_backups` (account_tong MSSQL, server1 MySQL)
   - Binary behavior: `jx_linux_y` (đọc qua log + reverse khi cần).
2. **KHÔNG suy diễn**. Khi thiếu thông tin, đọc Lua/INI/DB gốc trước khi code.
3. **Backend độc lập** — KHÔNG phụ thuộc auth-be / user-be hay microservice ERP.
   Account/login là hệ thống riêng của game (port từ account_tong + bishop).
4. **Không động vào client Unity** trong lúc build BE (chạy song song).

## Kiến trúc server PC (đã khảo sát)

```
Client
  → Bishop (login server)    acc 5002 / role 5001 / client 5622 / gamesvr 5632
  → PaySys (Sword3PaySys)    xác thực account (MSSQL account_tong)
  → Goddess / S3Relay        gateway relay
  → jx_linux_y (GameServer)  port 6666 — gameplay engine + Lua scripts
       ├─ server1 (MySQL)    role/character/item/... persistence
       └─ script/*.lua       toàn bộ logic gameplay
```

Protocol client↔server: `script/protocol.lua` (ScriptProtocol enum),
`script/requesttable.lua` (RequestTable dispatch), `script/script_protocol/`.

## Phân rã domain (port theo thứ tự phụ thuộc)

| # | Domain | Nguồn PC chính | Phụ thuộc |
|---|--------|----------------|-----------|
| 1 | account/auth | paysys, bishop.cfg, account_tong (MSSQL) | — |
| 2 | role/character | server1 DB (role tables), gateway roleback | 1 |
| 3 | player-state | script/player, attribconstdata.ini, settings | 2 |
| 4 | item/inventory | script/item, settings/item, goods.txt | 3 |
| 5 | skill | script/skill, Skills.txt, tyleskill.lua | 3 |
| 6 | map/scene | script/maps, settings/maps, maplist.ini | 3 |
| 7 | combat/battle | script/battles, script/skill battles | 3,5 |
| 8 | task/mission | script/task, script/missions | 3 |
| 9 | shop/economy | script/shop, buysell.txt, settings/shop | 4 |
| 10 | social (tong/team/chat/friend) | script/tong, goddess | 2 |
| 11 | activity/event | script/activitysys, script/event | nhiều |

Mỗi domain là một module DDD trong `app/modules/<domain>` (scaffold bằng
`tt scaffold module`), kèm story trong harness + proof matrix.

## Chiến lược validation

- Unit: domain rules thuần (tính toán exp/level/damage/giá...) so với công thức Lua.
- Integration: repository ↔ PostgreSQL (vltk_game), service ↔ DB thật.
- Parity: so output backend với hành vi server PC (log, DB diff, giá trị Lua).
- E2E: luồng client-protocol (login → chọn role → vào map) khi đủ domain.

## Exit criteria (100%)

- Tất cả domain trong bảng trên có module + story `done` + proof đạt.
- Toàn bộ ScriptProtocol message client cần đều có handler tương đương.
- Parity check pass cho từng domain so với server PC.
- `tt validate` + test suite xanh; server boot + luồng E2E cơ bản chạy.

## Open decisions

- 0001: Tech stack & standalone game server (xem docs/decisions/0001).
- 0002: Protocol porting strategy — REST/JSON vs binary TCP parity.
- Cách map MSSQL account_tong → PostgreSQL (di trú schema account).
