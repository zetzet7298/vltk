# US-001 Port account/auth (login flow)

## Status

in_progress

## Lane

high-risk

## Product Contract

Client gửi account + password → backend xác thực theo đúng logic server PC
(PaySys + bishop + bảng `Account_Info` của `account_tong`), kiểm tra ban, trả về
kết quả đăng nhập và (bước sau) danh sách nhân vật của account.

## Relevant Product Docs

- `docs/product/overview.md`
- `docs/product/api-conventions.md`
- `docs/stories/initiatives/INIT-001-port-pc-server.md`

## Provenance (nguồn PC)

- Login server: `home_jxser/gateway/bishop.cfg` (CheckAccount=1, cổng acc 5002).
- Auth: PaySys (`Sword3PaySys.exe`) ↔ MSSQL `account_tong`.
- Bảng `Account_Info` (account_tong):
  - `cAccName varchar(32)` PK — tên tài khoản.
  - `cPassWord varchar(32)` — **MD5(password) hex IN HOA** (vd MD5("1") =
    `C4CA4238A0B923820DCC509A6F75849B`).
  - `cSecPassWord varchar(32)` — mật khẩu cấp 2 (tùy chọn).
  - `bIsBanned bit` — tài khoản bị khóa.
  - `bIsUseOTP bit`, `iServiceFlag`, `nExtPoint*` — cờ phụ.
- Liên kết role: `server1.Role.Account varchar(32)` → danh sách nhân vật.

## Acceptance Criteria

- Xác thực đúng: account tồn tại + MD5(uppercase) khớp `cPassWord` → thành công.
- Sai mật khẩu / account không tồn tại → thất bại (không lộ account có tồn tại hay không).
- `bIsBanned=1` → từ chối đăng nhập với lý do banned.
- Endpoint `/v1/account/login` trả response envelope chuẩn.
- Logic hash MD5-uppercase khớp 100% dữ liệu PC (test bằng account gm001/test).

## Design Notes

- Commands: `login(account, password)`.
- Queries: `get_account(account)`, (sau) `list_roles(account)`.
- API: `POST /v1/account/login` → `emSCRIPT/bishop login` tương đương.
- Tables: `accounts` (port từ `Account_Info`); migration di trú từ MSSQL sau.
- Domain rules: MD5-uppercase password; ban check; không tiết lộ tồn tại account.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | hash MD5-uppercase + ban logic + auth pass/fail |
| Integration | login API ↔ PostgreSQL accounts |
| E2E | (sau) login → list role |
| Platform | n/a |

## Harness Delta

Tạo module `account` đầu tiên, thiết lập pattern port cho các domain sau.

## Evidence

(điền sau khi chạy test)
