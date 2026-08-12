# ADR-0008: Python FastAPI backend cho game runtime

- **Trạng thái:** Chấp nhận
- **Ngày:** 2026-07-20
- **Quyết định:** `backend/` (Python 3 + FastAPI, DDD modules, SQLAlchemy/UoW)
  là game backend duy nhất. Production Unity dùng typed REST client hiện hữu;
  Go `server-runtime/` bị xóa. Realtime 18 Hz là seam Python tiếp theo và không
  được tuyên bố hoàn tất trong migration này.
- **Hệ quả:** Mọi port server tiếp theo từ `/var/www/jx-pc` đi vào
  `backend/app/modules/`; contract camelCase của FastAPI/Unity phải giữ test.
  Protobuf language-neutral/C# có thể tồn tại cho feature khác nhưng không chứng
  minh một Go runtime còn hoạt động.

PostgreSQL 16 vẫn là production data target. Migration này không đổi schema và
không hạ yêu cầu transaction, backup/restore, idempotency hoặc audit.
