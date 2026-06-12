# API & Code Conventions — VLTK Backend

## Stack

- FastAPI (async) + tt-fw scaffolding (DDD module layout).
- PostgreSQL `vltk_game` qua `cores.component.sqlalchemy` (async engine, asyncpg).
- Redis (tt-docker) cho cache/state runtime khi cần.
- Cấu hình qua `.env` (pydantic-settings): `DB_DIALECT=postgresql`, `POSTGRES_*`,
  `REDIS_*`. `cores.config.database_config` đọc các biến này.

## Bố cục module (DDD, sinh bằng `tt scaffold module`)

```
app/modules/<domain>/
  api/v1/router.py              # FastAPI endpoints
  application/schemas.py        # pydantic request/response
  application/service.py        # use-case orchestration
  domain/models.py              # SQLAlchemy entity + domain model
  infrastructure/repository.py  # data access (SqlAlchemy<Domain>Repository)
tests/unit/modules/<domain>/test_service.py
tests/integration/modules/<domain>/test_api.py
```

- UnitOfWork (`app/infrastructure/unit_of_work.py`) gom repository theo property.
- Dependency injection trong `app/dependencies.py` (UoWDep, service deps).
- Router tự động đăng ký qua `app/api/register_module_routers` (quét `router.py`).

## Quy ước

- Endpoint domain nằm dưới prefix `/v1`.
- snake_case cho hàm/biến, PascalCase cho class. Type hints bắt buộc.
- Comment/docstring tiếng Việt (theo cấu hình tt-cli language=vi).
- Soft delete (`is_active`) mặc định, id kiểu int (theo config tt-cli).
- `ruff` + `black` line-length 120; chạy `tt validate` trước khi đóng story.

## Provenance (bắt buộc cho mỗi module port)

Mỗi module phải ghi rõ nguồn PC trong docstring/story:
- File Lua/INI gốc + đường dẫn.
- Bảng MySQL `server1` (hoặc MSSQL `account_tong`) tương ứng.
- Công thức/hằng số trích từ đâu (vd `attribconstdata.ini`, `tyleskill.lua`).

Không hard-code giá trị "đoán"; phải truy ngược về nguồn PC.

## Mapping dữ liệu PC → PostgreSQL

- MySQL `server1` (role/item/...) → các bảng trong `vltk_game`.
- MSSQL `account_tong` (account/billing) → schema account trong `vltk_game`.
- Giữ nguyên ý nghĩa cột; đặt tên bảng theo domain (vd `roles`, `items`,
  `skills`). Ghi lại ánh xạ cột trong story của domain.

## Protocol

- Client↔server gốc dùng binary TCP (ScriptProtocol enum trong
  `script/protocol.lua`, dispatch qua `requesttable.lua`).
- Backend mới expose REST/JSON tương đương từng message; quyết định cách giữ
  parity protocol nằm ở `docs/decisions/0002-protocol-porting-strategy.md`.
