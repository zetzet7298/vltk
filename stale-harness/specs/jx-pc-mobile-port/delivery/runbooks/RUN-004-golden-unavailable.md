# RUN-004: MinIO golden unavailable hoặc corrupt

- **Trigger:** object thiếu, version sai hoặc SHA-256 mismatch.
- **Owner:** QA infrastructure.
- **Thực hiện:** fail closed release gate, không dùng cache chưa verify; kiểm bucket/version/object key, phục hồi immutable object từ replica/backup.
- **Xác minh:** stream object và kiểm SHA-256/bytes/metadata đúng manifest.
