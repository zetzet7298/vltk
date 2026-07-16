# Versioning và compatibility

- REST major ở `/v1`; additive optional field không tăng major.
- Protobuf package/subprotocol `game.v1`; không reuse field number/name, enum 0
  là unspecified, field bỏ phải `reserved`.
- Content `schemaVersion=1`, immutable theo digest; session pin một release.
- DB PostgreSQL 16 dùng expand/backfill/switch/contract; app N/N-1 cùng chạy.
- Breaking semantics/required field/unit/order/auth tạo v2 và có telemetry,
  deprecation, retirement criteria.

Bootstrap công bố minimum/recommended client, protocol và content release.
Client thấp hơn minimum nhận `426`. Rollback không xóa ledger/event; dùng
forward-fix hoặc reversal.
