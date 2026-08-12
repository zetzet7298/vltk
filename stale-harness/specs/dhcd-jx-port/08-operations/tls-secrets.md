# TLS Và Secrets

| Trường | Giá trị |
|---|---|
| Mục đích | Quản lý TLS, credentials và rotation không ghi secret vào repo |
| Trạng thái | `not_started` |
| Owner / reviewer | Ops owner / security reviewer |
| Cập nhật | 2026-07-15 |

## Rules

- Secret chỉ từ environment/secret store; docs ghi tên biến, không ghi value.
- Caddy tự renew hoặc quy trình manual có owner/expiry.
- DB password, signing key, session key, OTel credentials tách riêng.
- Rotation có overlap window, key version và revoke test.
- Logs redact token, cookie, replay PII.

## Acceptance

- TLS scan và expiry alert.
- Rotation không làm mất session hợp lệ trong overlap.
- Revoked key bị reject.
- Secret scan CI pass; backup encryption key không cùng volume DB.
