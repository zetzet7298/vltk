# Idempotency contract v1

## REST

`Idempotency-Key` dài 16..128 ký tự ASCII hiển thị. Scope là
`(realm_id, actor_id, operation, key)`; SHA-256 canonical request là
`request_hash`. Cùng key/hash trả nguyên status/body đã lưu và header
`Idempotency-Replayed: true`; response thực thi lần đầu không gửi header này.
Khác hash trả `409 IDEMPOTENCY_CONFLICT`; record `in_progress` trả
`409 IDEMPOTENCY_IN_PROGRESS`. Giữ tối thiểu 24 giờ; economy giữ theo ledger.

| Operation | Header | Lý do/biện pháp thay thế |
| --- | --- | --- |
| `POST /bootstrap`, create/restore/select character; `DELETE` character; logout | Bắt buộc | Side effect hoặc phát ticket; replay đúng response |
| Register | Không nhận | Response chứa token; UQ account/email chặn duplicate, client quay lại login |
| Login | Không nhận | Không cache password/token response; rate limit và audit |
| Refresh | Không nhận | Refresh-token rotation/reuse detection là authority |
| Password-reset request | Không nhận | Enumeration-safe response, rate limit theo identity/IP |
| Password-reset confirm | Không nhận | OTP/reset token single-use, lần lặp trả kết quả trung tính |
| `GET` và POST thuần kiểm tra không tạo state | Không nhận | Safe/read-only; key bị bỏ qua là contract error thay vì silently cache |

Server chỉ lưu request hash canonical và response cần replay, không lưu password,
OTP, refresh/access token hoặc WSS ticket trong idempotency record. Với response
phát WSS ticket, payload replay được mã hóa ở application layer và hết hạn cùng
ticket; sau expiry trả stable conflict để client bootstrap/select lại.

## WSS và ledger

WSS dedupe `(session_id, session_epoch, client_seq)`; command durable có UUID
`command_id`. `last_processed_client_seq` chỉ xác nhận transport đã parse/xử lý
theo thứ tự, không xác nhận accepted/committed. Kết quả business dùng
`CommandResult.outcome`; economy chỉ thành công khi transaction `POSTED`.

Economy dùng unique `(realm_id, operation, idempotency_key)`. Transaction phải
được tạo `pending`; ledger entry, wallet balance, trạng thái `posted` và outbox
commit cùng transaction. Transaction posted phải cân bằng `sum(delta)=0` theo
currency; mint/burn qua system wallet. Không sửa/xóa entry posted; reversal tạo
transaction đối ứng rồi đánh dấu original `reversed`. Outbox at-least-once,
consumer dedupe `event_id`.
