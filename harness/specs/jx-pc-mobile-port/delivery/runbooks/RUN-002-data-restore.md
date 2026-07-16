# RUN-002: PostgreSQL restore và reconciliation

- **Trigger:** mất/corrupt dữ liệu, host failure hoặc restore drill.
- **Owner:** data + SRE.
- **Thực hiện:** cô lập writer, chọn base backup/WAL theo RPO, PITR vào instance mới, chạy integrity/cross-realm/ledger checks, switch endpoint và mở admission dần.
- **Xác minh:** committed receipt tồn tại, balanced ledger, checkpoint age hợp lệ, không duplicate item/session.
- **Gate:** hoàn tất trong RTO 60 phút; ghi artifact/timestamp/hash của drill.
