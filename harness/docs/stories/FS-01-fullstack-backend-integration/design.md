# FS-01 — Design

## Domain Model (Phase 1 minimal)

- **Account** (server-authoritative): account_id, username, password_hash, created_at, last_login_at. Backend owns registration, login (MD5 uppercase per FS-01A caveat), logout. Source of truth: backend.
- **Role** (server-authoritative): role_id, account_id, role_name, faction, level, last_played_at. One account can have many roles. Backend owns list, create, delete, select.
- **PlayerState** (server-authoritative): role_id, hp, mp, stamina, level, exp, money, position{x,y,z,map_id,dir}, status_flags, last_updated_at. Backend owns state transitions. Client predicts locally for UI smoothness; reconciles on `GET /v1/player/by-role/{role_id}`.
- **Map metadata** (server-authoritative): map_id, name (Vietnamese; **must be normalized** — see Encoding note below), aliases, geometry key. Backend owns list, lookup, position snapshot.
- **Inventory** (server-authoritative): role_id, items[], equipment_slots{}, capacity. Backend owns list, equip/unequip, use, repair, drop.
- **Skill** (server-authoritative): role_id, learned[], active_loadout, cooldowns. Backend owns list, learn, equip_loadout, cast (server-authoritative cooldown + resource cost), cast/check (pre-flight validation).

> **Encoding note.** Server responses may carry TCVN3/GB2312 bytes. Client must decode via `PcText.ReadLines` style logic before displaying in UI. Phase 1 keeps the **server** name as a debug/log field and continues to drive UI text from the **Unity** Vietnamese catalog (already verified by the decode wave 2026-06-12, 221 TCVN3 files). Server-side display names are NOT a Phase 1 UI dependency.

## Application Flow (Unity side)

```text
App boot
  → MockBackend.Default (offline mode)        # preserves current offline runtime
  → if BackendUrl configured:
        RestBackend.Connect(baseUrl)
        -> GET /health
        -> POST /v1/account/login
        -> GET /v1/role/by-account/{account}
        -> user picks role
        -> GET /v1/player/by-role/{role_id}
        -> GET /v1/map/... (enter + position)
        -> resume SandboxManager with server PlayerState
```

The Unity abstraction lives in `Assets/Scripts/Backend/`:

- `IGameBackend.cs` — interface: `HealthAsync`, `LoginAsync`, `ListRolesAsync`, `GetPlayerAsync`, `EnterMapAsync`, `ListItemsAsync`, `ListSkillsAsync`, `CastSkillAsync`.
- `BackendConfig.cs` — base URL, timeout, retry count, `UseRealBackend` flag.
- `RestBackend.cs` — `UnityWebRequest` implementation; serializes requests, parses `DataResponse<T>` envelope, raises typed exceptions.
- `MockBackend.cs` — canned data, no network. Stays as the default.
- `BackendHttp.cs` — internal HTTP boundary (interface) so tests can substitute a fake.
- DTOs in `Assets/Scripts/Backend/Dto/`: `HealthDto`, `LoginRequest/Response`, `RoleDto`, `PlayerStateDto`, `MapDto`, `MapListDto`, `ItemDto`, `SkillDto`, `SkillCastRequest/Response`, plus shared `DataResponse<T>` and `ErrorResponse`.

## Interface Contract (REST)

All endpoints are confirmed present in backend (FS-01A audit, 13 modules, 86 active endpoints). Phase 1 minimum set:

| Verb + Path | Module | Status | Auth? |
| --- | --- | --- | --- |
| `GET  /health` | (main) | confirmed | no |
| `POST /v1/account/login` | account | confirmed | no (returns session token) |
| `GET  /v1/role/by-account/{account}` | role | confirmed | yes |
| `GET  /v1/player/by-role/{role_id}` | player | confirmed | yes |
| `POST /v1/map/enter` | map | confirmed | yes |
| `GET  /v1/item/by-role/{role_id}` | item | confirmed | yes |
| `GET  /v1/skill/by-role/{role_id}` | skill | confirmed | yes |
| `POST /v1/skill/cast/check` | skill | confirmed | yes (pre-flight) |
| `POST /v1/skill/cast` | skill | confirmed | yes (server-authoritative) |

Auth: backend returns a session token in `LoginResponse`; client passes it as `Authorization: Bearer <token>` for role-scoped calls. The exact header name and token lifetime must be re-verified by FS-01D (FS-01A flagged password hash expectation; the auth header itself is an unverified spot). Add a FastAPI `TestClient` test on the backend side that round-trips login → role → player to confirm the header before any Unity `RestBackend` call.

## Data Model

- `BackendConfig` is `ScriptableObject` at `Assets/Scripts/Backend/Resources/BackendConfig.asset` (or, with Addressables as the project's standard, an Addressable SO). It carries base URL, timeout, retry, `UseRealBackend`.
- `BackendSession` (runtime, in `SandboxManager`) holds the token + the active role_id + cached DTOs. SandboxManager FastEditor path keeps `MockBackend` only.

## UI / Platform Impact

- No new UI in Phase 1 beyond what `SandboxManager` already shows. Phase 1 wires data sources; the visible HUD/UI keeps using its current offline state.
- Mobile platform check: REST base URL is read from `BackendConfig` which can be overridden per-build via Gradle template / iOS plist. Default `http://127.0.0.1:8020` for local desktop Editor.

## Observability

- `RestBackend` logs `request_id`, `method`, `path`, `duration_ms`, `status_code`, `message` (per `docs/ARCHITECTURE.md` observability contract).
- `MockBackend` log lines marked with `[mock]` prefix so logs and audits can tell them apart.
- Errors carry the FastAPI `ErrorResponse` payload (when present) and a synthesized request_id.

## Alternatives Considered

1. **Wire Unity directly to FastAPI without an abstraction.** Rejected: every UI/render component would have to know about HTTP. AGENTS.md explicit ban: "do not hard-code server calls into UI/render components."
2. **Replace the existing `INetworkClient` (binary protocol) with HTTP for everything.** Rejected: the binary protocol is for combat/movement realtime; HTTP is for REST domain authority. They serve different roles and Phase 1 only adds HTTP.
3. **Use NGO (`com.unity.netcode.gameobjects`).** Rejected by `harness/AGENTS.md`: VLTK uses a custom binary TCP/UDP protocol, NGO is not compatible.
4. **One mega-`verify_command` that calls Unity from bash.** Rejected: Unity needs the live Editor. The composite `verify-fs01.sh` script delegates the Unity gate to the integration worker and exits with a structured per-gate result; the integration worker invokes the Editor via `mcp_unityMCP_run_tests` and the offline lane runs only Gate 1.

## Durable Decisions (to be created at FS-01E time, not now)

- **`0008-fs01-verify-command.md`** — formalize the composite `verify-fs01.sh` as the FS-01 `verify_command`, replace the no-op `true`, set its expected exit codes.
- **`0009-fs01-auth-boundary.md`** — exact `Authorization` header name, token lifetime, password hash expectation, role-scope rule. This is the one FS-01A flagged as unverified.
- These are NOT created by FS-01C. They are decisions the integration worker records **after** the real artifact proves the assumption.
