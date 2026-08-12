# Observability

| Trường | Giá trị |
|---|---|
| Mục đích | Có tín hiệu vận hành cho pilot và verifier/replay |
| Trạng thái | `not_started` |
| Owner / reviewer | Ops owner / technical lead |
| Cập nhật | 2026-07-15 |

## Signals

- Metrics: request rate/latency/error, active rooms, tick lag, checkpoint mismatch, quarantine, reward latency, DB pool, memory/GC.
- Logs: structured JSON, `trace_id`, `run_id`/`room_id` đã redact, severity và retention.
- Traces: Caddy -> Go handler -> DB/verifier/replay.
- Dashboards: API SLI, battle verification, DB, resource, backup.

## Alerts

Alert theo threshold có owner/runbook: error budget burn, verifier mismatch spike, quarantine spike, DB disk/connection, backup failure, TLS expiry, tick lag.

Không log replay payload đầy đủ trong log stream; lưu blob encrypted theo retention.

## Acceptance

- [ ] Dashboard/alert/runbook tồn tại cho verifier mismatch, quarantine, DB, backup và TLS.
- [ ] Trace từ request tới verifier/DB có correlation ID và redact test pass.
- [ ] Retention/PII policy được security review.
