# Error contract v1

REST lỗi dùng `application/problem+json` theo RFC 9457. HTTP status thể hiện
lớp lỗi; `code` ổn định cho client, `detail` chỉ dành cho người dùng và không
được parse. Mọi lỗi có `requestId`; `traceId` chỉ trả khi chính sách môi trường
cho phép.

```json
{
  "type": "https://errors.vltk.example/game/validation-failed",
  "title": "Request validation failed",
  "status": 422,
  "code": "VALIDATION_FAILED",
  "detail": "One or more fields are invalid.",
  "instance": "/v1/characters",
  "requestId": "018f...",
  "retryable": false,
  "violations": [{"field":"name","rule":"length","message":"..."}]
}
```

| HTTP/WSS code | Stable code | Retry | Ý nghĩa |
| --- | --- | --- | --- |
| 400 | `MALFORMED_REQUEST` | Không | JSON/Protobuf/frame không đọc được |
| 401 | `AUTH_INVALID_CREDENTIALS` | Không | Không tiết lộ account có tồn tại |
| 401 | `AUTH_TOKEN_EXPIRED` | Sau refresh | Access token hết hạn |
| 403 | `AUTH_ACCOUNT_DISABLED` | Không | Account bị khóa/ban |
| 403 | `REALM_ACCESS_DENIED` | Không | Actor không thuộc realm |
| 404 | `CHARACTER_NOT_FOUND` | Không | Resource không thuộc actor/realm |
| 409 | `CHARACTER_NAME_TAKEN` | Không | Unique active name trong realm |
| 409 | `VERSION_CONFLICT` | Có, sau reload | Optimistic version sai |
| 409 | `IDEMPOTENCY_CONFLICT` | Không | Cùng key nhưng request hash khác |
| 409 | `CONTENT_RELEASE_MISMATCH` | Sau bootstrap | Client dùng content khác session |
| 422 | `VALIDATION_FAILED` | Không | Vi phạm field/invariant |
| 429 | `RATE_LIMITED` | Theo `Retry-After` | Quá quota |
| 503 | `REALM_UNAVAILABLE` | Có | Realm chưa sẵn sàng/admission đóng |
| 503 | `DEPENDENCY_UNAVAILABLE` | Có | PostgreSQL/content dependency lỗi |
| 500 | `INTERNAL` | Có giới hạn | Không lộ stack/SQL/secret |

WSS `Error` trong `game.proto` dùng cùng stable code. Lỗi command recoverable
không đóng socket. Vi phạm auth/protocol/content đóng bằng WebSocket close code:
`4401` auth, `4403` access, `4409` session replaced/content conflict, `4422`
protocol validation, `4503` server unavailable. Unknown enum/field Protobuf
phải được bỏ qua theo compatibility; payload vượt limit hoặc decompression bomb
được xem là `MALFORMED_REQUEST`.
