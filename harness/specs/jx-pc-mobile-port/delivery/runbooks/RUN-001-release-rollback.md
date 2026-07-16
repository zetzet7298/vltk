# RUN-001: Rollback release

- **Trigger:** crash/error/SLO/parity regression sau deploy.
- **Owner:** release + SRE.
- **Thực hiện:** dừng admission, drain realm, pin server/app-compatible content trước đó, chạy schema compatibility check, activate, smoke auth/WSS/map53/economy.
- **Không được làm:** rollback database destructive hoặc sửa ledger thủ công.
- **Xác minh:** reconnect, checkpoint, economy reconciliation, content hashes và SLO trở lại bình thường.
