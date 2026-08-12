# Idempotency contract v1

## REST

Mọi POST có side effect nhận header `Idempotency-Key` 16..128 ký tự ASCII.
Scope lưu là `(realm_id, actor_id, operation, key)`. Server chuẩn hóa method,
path, content-type và canonical body thành SHA-256 `request_hash`.

- Key mới: lock/insert `in_progress`, chạy transaction, lưu status/body/header
  allowlist rồi đánh dấu `completed`.
- Key cũ, cùng hash, completed: trả nguyên response và header
  `Idempotency-Replayed: true`; không phát event lần hai.
- Key cũ, khác hash: `409 IDEMPOTENCY_CONFLICT`.
- Key đang xử lý: chờ ngắn hoặc `409 IDEMPOTENCY_IN_PROGRESS` kèm `Retry-After`.
- Bản ghi giữ tối thiểu 24 giờ; operation economy giữ bằng retention ledger.

`POST /auth/login` được dedupe theo rate-limit/credential attempt, không lưu
password trong idempotency table. Refresh xoay token bằng `refresh_token_id`
duy nhất; replay token cũ revoke cả token family.

## WSS và economy

Command realtime dedupe bởi `(session_id, session_epoch, client_seq)` trong cửa
sổ session. Command durable có thêm `command_id` UUID; reconnect có thể gửi lại
và nhận kết quả cũ.

Mọi mutation economy tạo `economy_transaction` với `idempotency_key` unique
trong `(realm_id, operation)`. Ledger entry và outbox event commit cùng một
PostgreSQL transaction. Transaction chỉ `posted` khi tổng `delta` theo từng
currency bằng 0; mint/burn dùng system wallet để vẫn cân bằng. Không UPDATE hay
DELETE ledger đã posted; reversal tạo transaction đối ứng liên kết
`reversal_of_id`.

Outbox là at-least-once. Producer chỉ đánh dấu `published_at` sau broker ACK;
consumer bắt buộc dedupe `event_id`. Ordering chỉ đảm bảo theo
`(realm_id, aggregate_type, aggregate_id, aggregate_version)`.
