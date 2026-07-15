# Go Server Architecture

| Trường | Giá trị |
|---|---|
| Mục đích | Hợp đồng backend mới cho game, không tương thích DHCD server |
| Trạng thái | `not_started` |
| Owner / reviewer | Server lead / technical lead |
| Cập nhật | 2026-07-15 |

## Shape

Go modular monolith target tại `/var/www/vltk-mobile/server`, chia package:

`auth`, `player`, `catalog`, `inventory`, `run`, `room`, `verify`, `replay`, `admin`, `observability`.

Transport HTTPS JSON REST và WSS; contract OpenAPI 3.1 + AsyncAPI/JSON Schema. PostgreSQL database/role riêng trong `tt-docker`; không dùng harness SQLite cho game.

P0 gồm REST và WSS base transport có auth, reconnect/resume và run-verification events. Room/co-op channels vẫn feature-flag off tới P1 và chỉ mở sau `ADR-006`; việc hoãn co-op không được dùng để hoãn WSS/AsyncAPI base contract.

## Authority

- Go owns account, progression, inventory, config version, run seed, checkpoint verification và reward commit.
- Unity mirror owns presentation/local prediction; không canonicalize economy.
- Server không reverse/reuse DHCD binaries, DB layout hoặc wire contract; quyết định này được ghi tại `/home/zet/Projects/dhcd/docs/server-reverse-decision.md`.

## Security

TLS termination tại Caddy, service auth token rotation, per-account authorization, idempotency, rate limits, replay size limits, audit log và quarantine queue.

## Deployment target

Pilot 100 CCU, stateless API/relay replicas, PostgreSQL riêng, migrations forward/backward compatible. Health/readiness và graceful shutdown bắt buộc.

## Acceptance

- [ ] Go module tree, OpenAPI + AsyncAPI/JSON Schema, REST/WSS transport, auth boundaries và health endpoints tồn tại trong `server/`.
- [ ] PostgreSQL database/role riêng được provision và migration checksum pass.
- [ ] Integration/contract/load tests chứng minh 100 CCU pilot target và graceful shutdown.
