# ADR-0002: Go modular monolith cho realm đầu tiên

- **Trạng thái:** Chấp nhận
- **Quyết định:** Go 1.26, một process/realm, REST bootstrap và WSS Protobuf realtime, PostgreSQL durable store duy nhất.
- **Hệ quả:** Map/channel chạy mailbox/goroutine cô lập; giữ seam để tách gateway/worker sau P1.

## Ràng buộc PostgreSQL production

PostgreSQL 16 dùng stack/container hiện có tại `/var/www/tt-docker`; cấu hình kết
nối lấy từ secret/environment của stack, không sao chép credential vào specs hoặc
Unity. Môi trường validation dùng database disposable trong cùng major version;
không được âm thầm đổi sang database embedded, Redis hay một PostgreSQL deployment
khác nếu chưa có ADR thay thế được PO và SRE duyệt.
