# Release Checklist

| Trường | Giá trị |
|---|---|
| Mục đích | Checklist trước internal pilot và các staged release |
| Trạng thái | `design` |
| Owner / reviewer | Release owner / product owner |
| Cập nhật | 2026-07-15 |

## Build/content

- [ ] Commit/version/config/catalog/map conversion/replay schema pinned.
- [ ] Asset manifest, SHA-256, resolver/decode, legal state complete.
- [ ] Three factions/arena/starter gear content gate pass.
- [ ] Vietnamese localization missing-key scan pass.

## Runtime/data

- [ ] Go/Unity cross-language vectors, E2E, reconnect/quarantine pass.
- [ ] PostgreSQL migration, backup/restore, RPO/RTO evidence.
- [ ] TLS/secrets/health/telemetry/alerts/runbooks ready.
- [ ] Load/performance budget pass.

## Rollout

- [ ] Feature flags, canary, rollback trigger/owner.
- [ ] Mọi kênh của pilot là internal-only, kể cả khi legal đã cleared; public distribution là gate hậu pilot riêng.
- [ ] Internal pilot chỉ được bật khi legal approval còn hạn, ghi rõ scope/owner/expiry và cấm public distribution.
- [ ] Incident/support contact and replay diagnostic process.
- [ ] Post-release review date and expiry for provisional decisions.

## Acceptance

- [ ] Release owner tick từng mục và đính kèm artifact/reference.
- [ ] Legal, security, backup, observability, CI và performance gates đều có kết quả.
- [ ] Canary/rollback/incident contact được xác nhận trước promotion.
