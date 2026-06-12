# Product Overview — VLTK Mobile Game Server (Backend)

## Sản phẩm là gì

Backend game server cho VLTK Mobile: cổng dịch vụ phía server cho client mobile,
port 100% logic/behavior từ game server VLTK PC (Server 6.0). Đây là một **game
server độc lập**, không phải microservice trong hệ thống ERP tt.

## Bối cảnh

- Bản gốc PC: binary `jx_linux_y` + bộ Lua scripts điều khiển toàn bộ gameplay,
  cộng tầng gateway/login (bishop, goddess, paysys) và 2 DB (MSSQL `account_tong`
  cho account, MySQL `server1` cho role/character/item/...).
- Bản port: FastAPI + tt-fw + PostgreSQL (`vltk_game`), chạy trên hạ tầng
  tt-docker (postgres/redis/rabbitmq).

## Ranh giới (boundaries)

- KHÔNG phụ thuộc auth-be / user-be / notifier hay bất kỳ service ERP nào.
- Account/đăng nhập là hệ thống riêng của game (port từ account_tong + bishop).
- KHÔNG sửa client Unity (`/var/www/vltk-mobile/Assets`, `*.csproj`, ...).

## Người dùng / actor

- **Client mobile**: gửi protocol message (login, chọn nhân vật, gameplay).
- **GM/admin**: công cụ vận hành (port từ `script/gm_tool`, gmscript.lua) — sau.

## Tầng dịch vụ (port từ PC)

| Tầng PC | Vai trò | Tương ứng backend |
|--------|---------|-------------------|
| PaySys + account_tong | xác thực account | module `account` |
| Bishop / Goddess | login, chọn role, relay | module `account`, `role` |
| jx_linux_y + script/*.lua | gameplay engine | các module domain |
| server1 (MySQL) | persistence role/item/... | PostgreSQL `vltk_game` |

## Hợp đồng vận hành hiện tại

- `GET /health` — liveness probe (đã có, đã verify).
- `GET /docs`, `/openapi.json` — OpenAPI (đã có).
- Các endpoint domain sẽ thêm theo từng story (prefix `/v1`).

## Nguồn sự thật

Xem `docs/stories/initiatives/INIT-001-port-pc-server.md` để biết nguyên tắc
source-of-truth và phân rã domain. Mọi hành vi phải khớp server PC.
