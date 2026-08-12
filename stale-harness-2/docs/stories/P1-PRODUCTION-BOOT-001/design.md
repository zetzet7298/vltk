# Design

## Domain Model

Minimum entities for this implementation slice:

| Entity | Responsibility | Required invariant |
| --- | --- | --- |
| Account | login/register owner | active account identity never leaked on auth failure |
| Role | selected playable identity | account scoped, name unique, initial faction bounded |
| Role scene | current map and server position | canonical Ba Lăng map `53`; coordinates non-negative |
| Movement update | REST position command | role bounded, rejected when no scene exists |
| Audit/log event | operational evidence | account/role/map correlation without password output |

No schema migration is introduced by this client migration. Out-of-scope domain
rows remain password reset OTP, delete/restore lifecycle, combat, inventory,
equipment, wallet, economy, Android device profile, parity promotion, scale,
release, and backup/restore.

## Application Flow

1. `ProductionBootstrapper` creates its visual composition and one
   `BackendClientRunner`.
2. The runner loads `BackendConfig`, applies StreamingAssets overrides, and uses
   `RestGameBackend` when `useMock=false`.
3. Login failure `401` may trigger deterministic account creation and one login
   retry. Other login errors fail without provisioning.
4. An empty role list may trigger deterministic role creation.
5. The runner enters map `53` at the pinned positive PC/server coordinate and
   binds `MovementSyncMonoBehaviour` to the production avatar.

## Interface Contract

The Production slice uses the existing FastAPI routes:

- `POST /v1/account`, `POST /v1/account/login`;
- `GET /v1/role/by-account/{account}`, `POST /v1/role`;
- `POST /v1/map/enter`, `POST /v1/movement`.

Request/response DTOs remain camelCase at the Unity boundary and map to the
backend's `CamelCaseModel` schemas. Language-neutral proto files remain available
to other Unity features but are not part of this REST boot path.

## Data Model

The Python backend continues to own its existing account, role, and role-scene
tables. Auto-provisioning uses normal public application services and introduces
no direct database access or migration.

## UI / Platform Impact

This is Editor-first. The Production scene now requires the FastAPI service at
the configured URL for backend completion; connection failure is reported in the
Unity Console. Android, release registration UX, and credential storage are not
claimed.

## Trace and Logs

Required evidence fields for implementation and proof runs:

- configured backend URL/mode, never the password;
- account name, role ID, map ID `53`, and accepted spawn point;
- request status/code without response secrets;
- command, revision, environment, result, and artifact hash for proof runs.

## Alternatives Considered

1. Port the dormant Go P1 REST/WSS interface into FastAPI before switching.
   Rejected because it expands scope without a Production caller.
2. Keep mock mode in Production. Rejected because it does not exercise the
   selected Python server.
