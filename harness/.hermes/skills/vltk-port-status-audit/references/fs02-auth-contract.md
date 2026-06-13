# VLTK backend auth contract — FS-02A pinned (2026-06-13)

Pinned by FS-02A worker after running the contract end-to-end against the real
backend (uvicorn :8020 + PostgreSQL `vltk_game` DB). Source of truth for any
Unity client (or downstream backend) that needs to talk to `/v1/account/*`.
Replaces the **guess** in `fullstack-backend-integration-kanban.md` that said
"Unity login should match backend password expectation (MD5 uppercase)" — that
was wrong on two counts.

## POST /v1/account/login — the real shape

```jsonc
// request body (extra=forbid)
{
  "accName":  "string",   // REQUIRED. Camelcase. snake_case `acc_name` also
                          // accepted (populate_by_name=True on the schema),
                          // but the canonical client name is `accName`.
  "password": "string",   // REQUIRED. PLAINTEXT. Server hashes on receipt.
  "otp":      "string",   // optional. Only required if account is OTP-enabled.
  "clientIp": "string"    // optional. Used for LimitAccountPerIP throttling.
}

// 200 response (no token, no Authorization header)
{
  "accName":     "string",  // session id. NO bearer / NO JWT in FS-02.
  "serviceFlag": <int>,
  "extPoint":    <int>
}
```

## Password handling — DO NOT pre-hash on the client

| Step | Where | What |
|---|---|---|
| 1. Client sends | Unity `BackendClient.LoginAsync` | `password = "hunter2"` (plaintext over HTTPS) |
| 2. Server hashes | `app/modules/account/application/service.py` | `hashlib.md5(password.encode("utf-8")).hexdigest().upper()` |
| 3. Server stores | `accounts.password` column | 32-char hex **IN HOA** (parity PC `account_tong.cPassWord varchar(32)`) |
| 4. Client compares | n/a | Client never sees the hash; client never computes MD5 |

**Trap**: a client that pre-hashes (MD5 uppercase) and sends the hash as
`password` gets **401 — "sai tên HOẶC sai pw"** (same message as wrong
password, no enumeration). The contract test `test_login_with_md5_of_password_fails`
locks this. If a future change needs pre-hashing, the test must be updated
*and* the storage scheme changed in lockstep.

## Status codes

| Code | When |
|---|---|
| 200 | Login OK. `accName` is the session id. |
| 401 | Wrong `accName` OR wrong `password`. Same message — do not enumerate. |
| 403 | Account banned. |
| 422 | Missing required field OR unknown extra field (extra=forbid). |
| 429 | `LimitAccountPerIP` exceeded for this `clientIp`. |
| 501 | Account is OTP-enabled but engine OTP subsystem not configured. |

## The 11-endpoint surface pinned for FS-02 (no Authorization header)

```
GET    /health
POST   /v1/account                   # create account
POST   /v1/account/login
POST   /v1/account/logout
GET    /v1/role/by-account/{account}
POST   /v1/role                      # create role
GET    /v1/role/{role_id}
POST   /v1/player                    # create player state
GET    /v1/player/by-role/{role_id}
POST   /v1/player/by-role/{role_id}/exp
POST   /v1/player/by-role/{role_id}/translife
```

No bearer, no JWT, no session cookie. `accName` itself is the only session
identifier. Any future auth upgrade (FS-03) must extend the test file
`backend/tests/integration/modules/account/test_fs02a_auth_contract.py`
without removing existing cases.

## Working MD5-IN-HOA reference (offline-verified)

```
MD5("hunter2") = "2AB96390C7DBE3439DE74D0C9B0B1767"   # 32-char hex IN HOA
DB stored     = "2AB96390C7DBE3439DE74D0C9B0B1767"   # match
```

If the equality ever drifts, parity with the PC `account_tong.cPassWord`
column is broken — flag it before merging anything that touches the
account/role/player stack.

## Evidence locations

- `backend/tests/integration/modules/account/test_fs02a_auth_contract.py`
  (14 tests, backend repo commit `1625566`, branch `main`).
- `/var/www/vltk-mobile/harness/docs/fs02-evidence-2026-06-13/contract.md`
  (full schema pin + handoff, harness repo commit `c5a34d73b`).
- `tests/fs01-evidence-2026-06-13/_v1_map_*.json` (FS-01 map smoke for
  parity context).
- Re-runnable end-to-end smoke: `harness/docs/fs02-evidence-2026-06-13/smoke_test.sh`.

## Why this file exists

Workers auditing FS-NN auth slices in the future will repeat the
"MD5-uppercase" guess unless they read this. The guess was wrong, the test
fixture catches the wrong case, and any Unity client that pre-hashes
sends 401. Pin it here.
