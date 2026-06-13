# FS-01 — Full-stack backend integration foundation

## Status

planned (tracking story; proof flags stay `0` until the integration worker runs every gate)

## Lane

high-risk

## Risk checklist

- Auth (account/login/session).
- Authorization (role-bound requests; token or session required for role-scoped endpoints).
- Data model (player state, inventory, skill list, map position persisted server-side).
- Audit/security (auth boundary; password hash expectation flagged by FS-01A).
- Public contracts (REST endpoint shape, response envelope `DataResponse<T>`, error format).
- Cross-platform (Unity mobile client + FastAPI server, two languages, two test runners).
- Existing behavior (Unity already has a working offline mock; integration must preserve it).
- Weak proof (no current `verify_command` is real; integration proof today is `true` or `echo`).
- Multi-domain (account/role/player/map/item/skill all change together if backend is wired).

10 flags / 5+ hard gates → **high-risk** (FS-01 must use the high-risk template and durable decision records).

## Goal of this design packet

Replace FS-01's no-op `verify_command` (`true`) with a real, runnable proof ladder. The
deliverable is design only — implementation lives in the dependent cards (FS-01D retry2/retry3).
The integration worker (`vltk-unity`) is the only lane that can run the Unity-side gates.

## Non-goals (FS-01 scope)

- Realtime MMO server: connection manager, server tick, AOI, entity snapshots, movement broadcast. Phase 1 is REST authority only. Realtime gets its own `app/modules/realtime/` module + later story.
- Replacing the binary TCP/UDP protocol in `Assets/Scripts/Network/`. The REST layer is additive — it exposes `IGameBackend` (HTTP) for the boot/login/role/player/map/item/skill path; the existing `INetworkClient` (binary) stays for combat/movement realtime once that is ported.
- A full OpenAPI-driven Unity client. Phase 1 ships the smallest slice: Health + MapList + one DTO roundtrip per domain, all behind a `MockBackend`/`RestBackend` abstraction with a fake HTTP boundary in tests.
- Marking any `unit/integration/e2e/platform` flag green before a real artifact proves it. This card ships the design + the `verify-fs01.sh` script; flags stay `0` until a real EditMode/PlayMode artifact and a real `pytest` log are captured by the integration worker.

## Pointer to proof gates

The full proof-gate design (backend pytest, Unity EditMode, PlayMode/manual artifact,
composite `verify_command`, exact `harness-cli` story-update commands) lives in
[`proof-gates.md`](./proof-gates.md) in this folder.
