# 0010 Python FastAPI Production Runtime

Date: 2026-07-20

## Status

Accepted

## Context

The repository had two server implementations: the long-lived Python FastAPI
backend under `backend/` and a newer Go proof runtime under `server-runtime/`.
The Production Unity scene did not actually invoke the Go boot coordinator, while
the shared `VLTK.Backend` assembly already implemented the Python REST contracts
for account login, roles, map entry, movement, skills, combat, and items.

Maintaining both servers would split ownership of auth, character, map, and
movement behavior. The project owner explicitly selected the Python backend and
authorized permanent removal of the Go backend.

## Decision

- `backend/` is the game-server implementation used by the Production Unity
  scene.
- Production boot uses `BackendClientRunner` with real REST mode
  (`useMock=false`) against the FastAPI `/v1` API.
- Editor-first boot may create its deterministic development account and role
  when they do not exist, then enters canonical Ba Lăng map `53` and binds REST
  movement synchronization to the production avatar.
- `server-runtime/` is deleted. Its P1 REST/WSS behavior is not reimplemented as
  a compatibility layer in Python during this migration.
- Language-neutral protobuf contracts and generated C# types remain where other
  Unity runtime code still consumes them; they are not evidence of a live Go
  server.

## Alternatives Considered

1. Keep both Python and Go servers. Rejected because it duplicates domain
   ownership and leaves Production boot ambiguous.
2. Port the Go P1 REST/WebSocket API into Python before switching. Rejected for
   this slice because Production never called that coordinator and the existing
   Python REST client already covers the playable Editor-first path.
3. Keep Production in mock mode. Rejected because it would not exercise the
   selected server implementation.

## Consequences

Positive:

- One backend owns ongoing server porting from `/var/www/jx-source`.
- Production and Sandbox can share the existing typed FastAPI client surface.
- The first Editor run can provision deterministic development identity data.

Tradeoffs:

- The FastAPI service and its database must be running for real Production boot.
- This migration does not preserve Go admission-ticket or `game.v1` WebSocket
  behavior; any future realtime transport is a separate Python story.
- Auto-provisioning is an Editor-first development convenience, not a release
  account-registration UX.

## Follow-Up

- Add a live FastAPI-to-Unity Production PlayMode/E2E proof before promoting the
  P1 story beyond `FUNCTIONAL / UNVERIFIED`.
- Design the eventual Python realtime transport separately when multiplayer
  synchronization enters scope.
