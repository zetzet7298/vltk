# 0010 — Tối ưu engine DB runtime + baseline load test

- **Status:** accepted
- **Date:** 2026-06-12
- **Story:** post US-001..US-011 (hardening)

## Bối cảnh

Sau khi port xong 11 domain, dựng baseline load test (`tests/load/load_test.py`,
httpx async concurrent vào uvicorn live) để phát hiện hồi quy hiệu năng và xác
nhận server chịu tải đồng thời.

Lần đo đầu (50 VU): 150 req/s, p95 565ms, **0 lỗi**. Request đơn lẻ < 10ms →
nút thắt là ở đồng thời, không phải logic.

## Chẩn đoán

Hai nút thắt, cả hai nằm trong submodule dùng chung `cores` (KHÔNG sửa trực tiếp):

1. `cores.component.sqlalchemy._engine` dùng `poolclass=NullPool` → mở kết nối
   PostgreSQL MỚI cho mỗi request.
2. `BasePostgreSQLRepository._ensure_schema()` chạy `create_all` (phản chiếu
   schema) ở truy vấn đầu của mỗi repository instance; repo tạo mới mỗi request
   nên `create_all` chạy lại liên tục.

## Quyết định

Vá ở **tầng ứng dụng game server**, không chạm mã submodule:

- `app/infrastructure/database.py::configure_runtime_engine()` tạo engine CÓ
  POOL thật (`pool_size=20, max_overflow=30, pool_pre_ping=True`), tạo schema
  MỘT LẦN, rồi rebind `cores_sa._engine` + `cores_sa.async_session` và thay
  `_ensure_schema` bằng no-op. UoW lấy `async_session` từ cores ở call-time nên
  rebind có hiệu lực ngay.
- Gọi qua FastAPI `lifespan` startup hook trong `app/main.py`.

An toàn vì UnitOfWork resolve `async_session` lúc gọi (không bind sớm), và toàn
bộ thay đổi giới hạn trong tầng app — không phân nhánh submodule.

## Kết quả đo (cùng máy, đọc thuần)

| Cấu hình            | Throughput | p50    | p95     | Errors |
|---------------------|-----------:|-------:|--------:|-------:|
| Trước (50 VU)       | 150 req/s  | 305ms  | 565ms   | 0      |
| Sau, 1 worker 50 VU | 269 req/s  | 121ms  | 497ms   | 0      |
| Sau, 1 worker 10 VU | 526 req/s  | 14ms   | 50ms    | 0      |
| Sau, 4 worker 50 VU | 373 req/s  | 87ms   | 383ms   | 0      |

Throughput tăng ~80% sau khi sửa pool + schema-reflection. Ở mức không bão hòa
(10 VU) server sạch (p95 50ms). Đuôi trễ ở 50/100 VU là bão hòa event-loop một
worker — **0 lỗi xuyên suốt**; khắc phục production bằng nhiều worker (đã chứng
minh với `--workers 4`).

## Hệ quả

- Load test là regression guard: `--users 10 --p95-budget 100` cho baseline,
  tăng `--users`/`--p95-budget` khi đo bão hòa cao.
- Production nên chạy uvicorn/gunicorn nhiều worker; pool size mỗi worker = 20.
