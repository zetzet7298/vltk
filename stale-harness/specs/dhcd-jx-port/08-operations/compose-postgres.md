# Compose Và PostgreSQL

| Trường | Giá trị |
|---|---|
| Mục đích | Provision server stack riêng, dùng PostgreSQL container hiện có an toàn |
| Trạng thái | `not_started` |
| Owner / reviewer | Ops owner / server reviewer |
| Cập nhật | 2026-07-15 |

## Target topology

```text
Caddy (TLS) -> Go API/WS -> PostgreSQL game DB
                         -> OTel Collector -> Prometheus/Grafana/Loki
```

Compose game là project riêng; không sửa stack không liên quan trong `/var/www/tt-docker`. Đọc `/var/www/tt-docker/.env` lúc provisioning, tạo database/role riêng, password riêng, network/volume policy riêng.

## Required services

- `server`: Go binary, non-root, health/readiness, graceful shutdown.
- `postgres`: dùng container/runtime được owner phê duyệt; version pin và migration check.
- `caddy`: TLS termination, HSTS/rate-limit policy.
- `otel-collector`, `prometheus`, `grafana`, `loki`: telemetry.

## Acceptance

- `compose config` không leak secret.
- Migrations forward/backward chạy trên database riêng.
- Health/readiness, restart, connection pool và backup hook pass.
- 100 CCU load test không vượt error/latency budget.
