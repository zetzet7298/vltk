# 0008 Standalone Game Server on FastAPI + tt-fw + PostgreSQL

Date: 2026-06-12

## Status

Accepted

## Context

Cần port game server VLTK PC (Server 6.0 — `jx_linux_y` + Lua) sang backend mới.
Yêu cầu: tech stack FastAPI, dùng framework tt-fw có sẵn (tt-cli), DB chính
PostgreSQL, chạy trên hạ tầng tt-docker đang chạy. Câu hỏi kiến trúc: backend này
có nên là một microservice trong hệ thống ERP tt (dùng auth-be/user-be) hay là
một game server độc lập?

## Decision

Xây dựng **game server độc lập**:

- Scaffold bằng `tt init greenfield` (fastapi-ddd, postgresql) tại
  `/var/www/vltk-mobile/backend`, dùng `cores` submodule của tt-fw làm khung.
- DB chính: PostgreSQL `vltk_game` trên container `postgres` của tt-docker
  (role `vltk`). Cache/state: Redis tt-docker.
- **KHÔNG** phụ thuộc auth-be / user-be / notifier. Account/login là hệ thống
  riêng của game, port từ PaySys + bishop + MSSQL `account_tong`.
- Bỏ các middleware/coupling kiểu ERP (vd `check_permission_middleware` gọi sang
  auth service). Health endpoint tự quản.

## Alternatives Considered

1. Làm microservice ERP (tái dùng auth-be/user-be): bị loại — game có vòng đời
   account/nhân vật riêng, mô hình quyền khác hẳn ERP, sẽ tạo coupling sai.
2. Viết from scratch không dùng tt-fw: bị loại — yêu cầu dùng framework có sẵn,
   và cores cung cấp sẵn UoW/repository/logging/DB.

## Consequences

Positive:

- Ranh giới rõ ràng, không kéo theo phụ thuộc ERP không liên quan.
- Tận dụng được scaffolding, UoW, repository, config DB của tt-fw/cores.

Tradeoffs:

- Phải tự port tầng account/login thay vì tái dùng auth-be.
- Phụ thuộc `cores` submodule (gitlab nội bộ) cho tầng hạ tầng chung.

## Follow-Up

- 0002 protocol porting strategy.
- Di trú schema account_tong (MSSQL) → PostgreSQL.
