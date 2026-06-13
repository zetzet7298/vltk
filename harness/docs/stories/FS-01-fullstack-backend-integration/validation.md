# FS-01 — Validation

The full proof ladder, exact commands, and the story-update plan live in
[`proof-gates.md`](./proof-gates.md). This file is the short version for the
high-risk template; the long version is the durable reference.

## Proof Strategy

A **composite `verify_command`** runs four gates in order. Any gate failure exits non-zero.
The four gates are real artifacts (pytest logs, an EditMode Test Runner run, a PlayMode test
or a manual artifact) — never `true`, never `echo`, never a no-op.

| Gate | Who runs it | What it proves | Artifact |
| --- | --- | --- | --- |
| G1 Backend pytest | offline (any lane) | Backend domain authority is healthy; 722/722 holds | `tests/.pytest_log.txt` |
| G2 Backend health smoke | offline (any lane) | A real uvicorn serves `/health` and `/openapi.json` | `tests/.health_smoke.json` |
| G3 Unity EditMode (focused) | integration (`vltk-unity`) | The new `IGameBackend`/`RestBackend`/`MockBackend` slice compiles in the real Editor, the focused suite passes, and zero new CS errors appear | `tests/.editmode_results.xml` |
| G4 PlayMode or manual | integration (`vltk-unity`) | A real Editor PlayMode call to `RestBackend.HealthAsync` (via the fake HTTP boundary) returns the expected DTO, OR a manual artifact file is captured at `tests/.playmode_artifact.json` / screenshot path | `tests/.playmode_results.xml` or `tests/.playmode_artifact.json` |

## Test Plan

| Layer | Cases | Gate |
| --- | --- | --- |
| Unit | `tests/unit/modules/**` (618) | G1 |
| Integration | `tests/integration/**` (103) | G1 |
| E2E | `tests/e2e/**` (1) | G1 |
| Unity backend client | `Assets/Tests/EditMode/Backend/*.cs` — TBD in FS-01D | G3 |
| Unity PlayMode health | `Assets/Tests/PlayMode/Backend/*.cs` — TBD in FS-01D | G4 |

## Fixtures

- Backend: existing pytest fixtures (in-memory SQLite) plus a new TestClient test for the login → role → player round-trip that confirms the `Authorization` header (planned for FS-01D, gate G1 only after the new test is merged).
- Unity: a fake `IBackendHttp` that returns canned `DataResponse<T>` JSON for the focused EditMode suite. No real network in any test.
- Manual artifact: a captured PlayMode log + screenshot saved to `tests/.playmode_artifact.json` with `{"timestamp":..., "editor":"...", "result":"ok", "screenshot":"..."}`.

## Commands

The exact, copy-paste-ready commands are in [`proof-gates.md` § Commands](./proof-gates.md#commands).

## Done Definition (no false green)

FS-01 is DONE only when **all** of:

1. G1 + G2 pass on the integration lane with the artifact paths listed above populated.
2. G3 passes in the live Editor (real `TestResults.xml` from `mcp_unityMCP_run_tests`, no new CS errors).
3. G4 produces a real PlayMode artifact OR a manual artifact with the prescribed fields.
4. `harness-cli story verify FS-01` exits 0 (after the integration worker replaces the no-op `verify_command` with `bash harness/scripts/verify-fs01.sh`).
5. The integration worker applies the documented `harness-cli story update` commands from `proof-gates.md` with the real artifact paths in `--notes`.

Until (1)–(4) all hold, FS-01 flags stay `0` and `last_verified_result` stays empty.
