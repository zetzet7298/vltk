# Versioning và compatibility

| Surface | Quy tắc v1 |
| --- | --- |
| REST | Major trong path `/v1`; additive optional field không tăng major |
| Protobuf | Package `game.v1`; không đổi/reuse field number, enum 0 là unspecified |
| WSS | Subprotocol `game.v1`; negotiate trong HTTP upgrade và `ClientHello` |
| Content | `schemaVersion=1`, release immutable theo digest; session pin release |
| Database | Expand/migrate/contract; app N và N-1 cùng chạy trong rollout |

Breaking gồm đổi semantics, required field mới, bỏ field/endpoint, đổi unit,
đổi ordering hoặc authentication. Breaking tạo `/v2`/`game.v2`; v1 có lịch
deprecation, telemetry consumer và tiêu chí retirement. Field Protobuf bị bỏ
phải ghi `reserved` cả number lẫn name. Không truyền Go enum ordinal hay DB ID
nội bộ nếu contract đã định nghĩa stable enum/UUID.

REST gửi `X-API-Version: 1`, `X-Request-ID`; response bootstrap công bố
`minClientVersion`, `recommendedClientVersion`, `protocolVersions` và release
content. Client thấp hơn minimum nhận `426 CLIENT_UPGRADE_REQUIRED`.

Migration DB luôn: (1) expand nullable/new table/index concurrently, (2) dual
read hoặc backfill có checkpoint, (3) chuyển read, (4) dừng old write, (5)
contract sau ít nhất một release. Rollback không được yêu cầu xóa ledger/event;
forward-fix hoặc reversal được ưu tiên.
