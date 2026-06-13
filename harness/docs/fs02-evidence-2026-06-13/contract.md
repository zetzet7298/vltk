# FS-02A — Auth Contract (Account / Login / Role / Player)

**Date pinned:** 2026-06-13
**Lane:** backend (`/var/www/vltk-mobile/backend`)
**Backend:** FastAPI + PostgreSQL `vltk_game` (uvicorn :8020)
**Test verdict:** 736/736 pytest pass (722 cũ + 14 mới trong `test_fs02a_auth_contract.py`)
**Smoke test verdict:** 13/13 curl step pass trên live server

---

## TL;DR — auth contract (FS-02)

| Trục | Giá trị pin |
| --- | --- |
| Endpoint | `POST /v1/account/login` |
| Body | `{"accName": "<string 1..32>", "password": "<string 1..>"}` |
| Body optional | `otp?: string` (chỉ bắt buộc khi account bật `isUseOtp`); `clientIp?: string` (áp `LimitAccountPerIP`) |
| `password` shape | **PLAINTEXT**. Client KHÔNG hash trước. |
| Server storage | `MD5(plaintext)` hex IN HOA, 32 ký tự, cột `accounts.password` (`varchar(32)`) — parity cột `cPassWord` của `account_tong` PC. |
| Authorization header | **KHÔNG yêu cầu** cho `/login`. Không bearer, không JWT trong FS-02. |
| Login response | `{"code":"200","message":"Success","data":{"accName":"...","serviceFlag":0,"extPoint":0}}` — KHÔNG có token. `accName` là session id duy nhất cho FS-02. |
| 200 happy path | `data.accName` = tài khoản đăng nhập. |
| 401 | Sai tên HOẶC sai mật khẩu HOẶC account không tồn tại — CÙNG một lỗi chung, không lộ tồn tại. |
| 403 | Account bị banned. |
| 422 | Body thiếu field bắt buộc (`accName`/`password`) hoặc field không nằm trong schema (`extra=forbid`). |
| 429 | Account vượt `LimitAccountPerIP` (`SoLuongAccGioiHan=4`) trên cùng IP. |
| 501 | Account bật `isUseOtp` mà OTP engine chưa cấu hình. |

> **Quan trọng — sửa lại task body.** Task body ban đầu nói: *"JSON body {account, password} where password is MD5-uppercase"*. **Sai cả hai ý.** Thực tế:
> 1. Field là `accName` (camelCase), không phải `account`.
> 2. `password` là **plaintext**, server mới băm MD5-IN-HOA và so với cột `accounts.password`. Gửi sẵn MD5-IN-HOA → 401 (test `test_login_with_md5_of_password_fails` khoá điều này).

---

## 1. Pinned schemas (OpenAPI snapshot 2026-06-13)

Nguồn: `curl http://127.0.0.1:8020/openapi.json` (lưu tại `openapi.json` cùng thư mục này).

### 1.1 `LoginRequest` (`POST /v1/account/login`)

```json
{
  "accName": "alice",        // required, string, 1..32
  "password": "plaintext",    // required, string, minLength 1
  "otp": "123456",            // optional, required iff account.isUseOtp=true
  "clientIp": "203.0.113.5"   // optional, cho LimitAccountPerIP
}
```

`extraProperties: false` → gửi field lạ → 422.

### 1.2 `LoginResponse`

```json
{
  "code": "200",
  "message": "Success",
  "data": {
    "accName": "alice",
    "serviceFlag": 0,
    "extPoint": 0
  }
}
```

KHÔNG có `token` / `bearerToken` / `accessToken`. Đây là điểm khác biệt lớn với các API
chuẩn OAuth. Cho FS-02 client dùng `accName` trong body/path các call sau (xem § 2).

### 1.3 `AccountCreate` (`POST /v1/account`)

```json
{
  "accName": "alice",        // required, 1..32, unique
  "password": "plaintext",    // required, sẽ được băm MD5-IN-HOA
  "secPassword": null,        // optional, mật khẩu cấp 2
  "serviceFlag": 0            // default 0
}
```

Response 200: `{"id", "accName", "isBanned":false, "isUseOtp":false, "serviceFlag", "extPoint"}`.
Response 409: account đã tồn tại.
Lưu ý: KHÔNG trả hash mật khẩu trong response (`AccountResponse` không có field `password`).

### 1.4 `AccountResponse`

```json
{
  "id": 2,
  "accName": "alice",
  "isBanned": false,
  "isUseOtp": false,
  "serviceFlag": 0,
  "extPoint": 0
}
```

### 1.5 `RoleCreate` (`POST /v1/role`)

```json
{
  "account": "alice",        // required, FK logic đến accounts.acc_name
  "roleName": "Vo_Si",       // required, 1..64, unique toàn server
  "faction": 0               // -1..9; 0=Kim, 1=Mộc, 2=Thủy, 3=Hỏa, 4=Thổ;
                             // 5..9 = bonus môn phái; -1 = chưa nhập môn phái
}
```

Giới hạn: tối đa `MAX_ROLES_PER_ACCOUNT` (xem `role/domain/constants.py`).
Faction name mapping: xem `FACTION_NAMES` (0 → "Thiếu Lâm" …).

### 1.6 `RoleResponse` / `RoleListResponse`

```json
{
  "id": 2,
  "roleName": "Vo_Si",
  "account": "alice",
  "faction": 0,
  "factionName": "Thiếu Lâm",
  "level": 1
}
```

`RoleListResponse`:
```json
{
  "account": "alice",
  "roles": [ /* RoleResponse[] */ ]
}
```

### 1.7 `PlayerStateCreate` (`POST /v1/player`)

```json
{
  "roleId": 2,        // required, ≥1
  "level": 1,         // optional, 1..200, default 1
  "series": 0         // optional, 0..4 (Kim/Mộc/Thủy/Hỏa/Thổ); default 0
}
```

### 1.8 `PlayerStateResponse`

```json
{
  "id": 1,
  "roleId": 2,
  "level": 1,
  "exp": 0,
  "transLife": 0,
  "freePoint": 0,
  "magicPoint": 0,
  "strength": 35,    // base theo series (task_head.lua:79-82)
  "dexterity": 25,
  "vitality": 25,
  "spirit": 15,
  "series": 0,
  "money": 0,
  "repute": 0
}
```

`strength/dexterity/vitality/spirit` được tính từ `base_attributes(series)` ở
`player/domain/progression.py`. Series 0 (Kim) = 35/25/25/15.

---

## 2. Endpoints dùng cho FS-02 (auth → role → player)

| # | Method | Path | Body / Path | Auth? | Returns |
| - | ------ | ---- | ----------- | ----- | ------- |
| 1 | GET    | `/health` | — | no | `{status:"ok", service, version, timestamp}` |
| 2 | POST   | `/v1/account` | `AccountCreate` | no | `AccountResponse` (200) / 409 dup |
| 3 | POST   | `/v1/account/login` | `LoginRequest` | no | `LoginResponse` (200) / 401 / 403 / 422 / 429 / 501 |
| 4 | POST   | `/v1/account/logout` | `{accName}` | no | `{accName, logoutDate}` (200) / 401 |
| 5 | GET    | `/v1/role/by-account/{account}` | path | no (FS-02) | `RoleListResponse` |
| 6 | POST   | `/v1/role` | `RoleCreate` | no (FS-02) | `RoleResponse` (200) / 409 / 422 |
| 7 | GET    | `/v1/role/{role_id}` | path | no (FS-02) | `RoleResponse` (200) / 404 |
| 8 | POST   | `/v1/player` | `PlayerStateCreate` | no (FS-02) | `PlayerStateResponse` (200) / 409 / 422 |
| 9 | GET    | `/v1/player/by-role/{role_id}` | path | no (FS-02) | `PlayerStateResponse` (200) / 404 |
| 10 | POST  | `/v1/player/by-role/{role_id}/exp` | `{amount}` | no (FS-02) | `AddExpResponse` (200) / 404 / 422 |
| 11 | POST  | `/v1/player/by-role/{role_id}/translife` | — | no (FS-02) | `TransLifeResponse` (200) / 404 / 409 |

Tất cả response dùng envelope `DataResponse<T>`:
```json
{ "code": "200", "message": "Success", "data": <T> }
```

> **FS-03 sẽ bổ sung auth.** Từ FS-03 trở đi, các endpoint role/player sẽ yêu cầu
> bearer/JWT. FS-02 client **không** được hard-code assumption "không cần token cho mọi
> endpoint sau này" — xem handoff § 6.

---

## 3. MD5-IN-HOA storage (parity với PC `account_tong`)

**Algorithm (đã verify 2026-06-13):**

```python
import hashlib
hash_password(p) = hashlib.md5(p.encode("utf-8")).hexdigest().upper()
# → chuỗi 32 ký tự hex IN HOA, vd MD5("1") = "C4CA4238A0B923820DCC509A6F75849B"
```

Cột DB:
```sql
accounts.password varchar(32) NOT NULL  -- MD5 hex IN HOA
```

**Test parity:** `test_password_stored_as_md5_uppercase_hex_32_chars` tạo account với
`password="Pw_42_secret"`, query DB, assert stored == `MD5("Pw_42_secret").hexdigest().upper()`,
assert `stored.isupper()`, `len(stored) == 32`. Kết quả PASS.

**Compare:** `verify_password` dùng `hmac.compare_digest` (constant-time) để chống
timing attack. `verify_password` cũng chuẩn hoá `stored_hash.upper()` để tương thích dữ
liệu cũ lỡ lưu chữ thường.

**Client lưu ý:** KHÔNG được gửi sẵn MD5. Server hash plaintext nhận được → nếu client
đã hash, server hash lại lần nữa → MD5(MD5(p)) ≠ MD5(p) → 401. Test
`test_login_with_md5_of_password_fails` khoá hành vi này.

---

## 4. Smoke test outputs (live uvicorn :8020)

Script: `smoke_test.sh` cùng thư mục này. Re-run được bất kỳ lúc nào
(đảm bảo uvicorn :8020 đang chạy + `vltk_game` PostgreSQL reachable).

Kết quả 2026-06-13 18:52 UTC+7 (account `fs02a_1781351546`, roleId `2`):

| Step | Method | Path | Status | Body excerpt |
| ---- | ------ | ---- | ------ | ------------ |
| 1 | GET | `/health` | 200 | `status:"ok"`, service=`vltk-game-server` |
| 2 | POST | `/v1/account` | 200 | `id:2, accName:"fs02a_…"` |
| 3 | POST | `/v1/account/login` (plaintext) | 200 | `accName, serviceFlag:0, extPoint:0` |
| 4 | POST | `/v1/account/login` (sai pw) | 401 | `"Tên đăng nhập hoặc mật khẩu không đúng"` |
| 5 | POST | `/v1/account/login` (acc không tồn tại) | 401 | (cùng message — không lộ tồn tại) |
| 6 | GET | `/v1/role/by-account/fs02a_…` | 200 | `roles:[]` |
| 7 | POST | `/v1/role` (faction=0 Kim) | 200 | `id:2, roleName, factionName:"Thiếu Lâm", level:1` |
| 8 | GET | `/v1/role/by-account/fs02a_…` | 200 | `roles:[{id:2, factionName:"Thiếu Lâm"}]` |
| 9 | POST | `/v1/player` (level=1, series=0) | 200 | `roleId:2, level:1, exp:0, …` |
| 10 | GET | `/v1/player/by-role/2` | 200 | same as step 9 |
| 11 | POST | `/v1/account/login` (password = MD5 hex) | **401** | **khoá: client phải gửi plaintext** |
| 12 | POST | `/v1/account/login` (không Authorization) | **200** | **khoá: không cần header, không cần token** |
| 13 | POST | `/v1/account/logout` | 200 | `logoutDate:"2026-06-13T18:52:26.585065"` |

DB state verify sau smoke test (asyncpg direct):
```
acc_name='fs02a_1781351546'
password='2AB96390C7DBE3439DE74D0C9B0B1767'   # MD5("hunter2") IN HOA, 32 chars
is_banned=False, is_use_otp=False
login_date=2026-06-13 18:52:26, logout_date=2026-06-13 18:52:26
match MD5("hunter2").hexdigest().upper()? True
```

---

## 5. Pytest coverage

**File mới:** `tests/integration/modules/account/test_fs02a_auth_contract.py` — 14 tests, 100% pass.

Các test pin contract (1-1 với § 1-§ 3 ở trên):

| # | Test | Pin |
| - | ---- | --- |
| 1 | `test_login_accepts_camelcase_accname_and_plaintext_password` | spec chính thức |
| 2 | `test_login_accepts_both_camelcase_and_snake_case` | `populate_by_name=True` cho phép cả hai |
| 3 | `test_login_rejects_extra_field` | `extra=forbid` |
| 4 | `test_password_stored_as_md5_uppercase_hex_32_chars` | MD5-IN-HOA parity |
| 5 | `test_login_with_md5_of_password_fails` | double-hash fail |
| 6 | `test_login_does_not_require_authorization_header` | no bearer |
| 7 | `test_login_response_has_no_token_field` | no token field |
| 8 | `test_wrong_password_returns_401` | 401 sai pw |
| 9 | `test_unknown_account_returns_401_same_as_wrong_password` | không lộ tồn tại |
| 10 | `test_banned_account_returns_403` | 403 banned |
| 11 | `test_logout_writes_logout_date_and_returns_200` | logout semantics |
| 12 | `test_logout_unknown_account_returns_401` | logout sai tên |
| 13 | `test_full_happy_path_create_account_login_list_roles_get_player_state` | full happy path |
| 14 | `test_happy_path_stored_password_is_md5_upper` | re-verify storage |

**Test cũ (giữ nguyên, vẫn pass):** `test_login.py` (5 tests) + `test_account.py` (4 tests).

**Tổng pytest run:** 736/736 pass trong 194.21s (xem `pytest_full.log`).

---

## 6. Handoff cho các card sau (FS-02B / FS-02C / FS-03)

1. **Client Unity (FS-02B / FS-01D):**
   - DTO `LoginRequest` = `LoginRequest.cs { string accName; string password; string? otp; string? clientIp; }`
   - Gọi `POST /v1/account/login` với JSON body như trên; **KHÔNG** gửi MD5, **KHÔNG** gửi Authorization header.
   - Đọc `response.data.accName` làm session id; lưu trong `BackendSession` runtime.
   - Khi cần truy cập role/player: gọi `GET /v1/role/by-account/{accName}`, `POST /v1/role { account: accName, …}`, `GET /v1/player/by-role/{roleId}`.
   - **`Config.BASE_URL` mặc định** cho Editor: `http://127.0.0.1:8020`. Mobile build đọc từ `BackendConfig.asset` (override Gradle/iOS plist).

2. **FS-02C (server-authoritative skill cast):** đã có `POST /v1/skill/cast/check` + `POST /v1/skill/cast`. Contract sẽ pin trong card FS-02C riêng.

3. **FS-03 (auth nâng cấp — bearer/JWT hoặc session token):** sẽ thay đổi contract. Mọi assumption "FS-02 không cần token" chỉ đúng cho tới khi FS-03 merge. Các test FS-02A sẽ được mở rộng (không phá) để cover token path.

4. **FS-01B Unity discovery worker** cần biết: `account.accName` (KHÔNG phải `account.name`) là primary key logic, mọi FK role/player đều tham chiếu theo string này.

5. **Encoding note:** server trả tiếng Việt (faction name = "Thiếu Lâm"). Đã là UTF-8 ở JSON. Khi hiển thị trong Unity, dùng catalog có sẵn (đã verified bởi decode wave 2026-06-12). KHÔNG cần thêm bước encoding từ phía server.

---

## 7. Files trong evidence bundle

```
fs02-evidence-2026-06-13/
├── contract.md                            ← file này
├── smoke_test.sh                          ← bash, 13 step, chạy lại được
├── openapi.json                           ← snapshot OpenAPI từ /openapi.json
├── 01_health.json .. 13_logout.json       ← raw response từng step
├── pytest_fs02a_auth_contract.log         ← log 14/14 contract tests
├── pytest_full.log                        ← log 736/736 toàn bộ backend
└── (test source) tests/integration/modules/account/test_fs02a_auth_contract.py
    (commit trong /var/www/vltk-mobile/backend, branch dưới)
```

**Git:** đã thêm `tests/integration/modules/account/test_fs02a_auth_contract.py` ở
backend repo. Commit + push xem comment kanban (sẽ ghi `commit:<sha>` sau khi push).
