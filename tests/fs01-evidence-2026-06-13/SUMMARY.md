# FS-01 evidence (2026-06-13, integration lane)

Sources:
- Unity Editor: 6000.4.7f1, project /var/www/vltk-mobile
- Backend: FastAPI DDD on 127.0.0.1:8020
- Implementation branch merged: fullstack/fs01d-unity-rest-health-map-retry3 (commit 800990387)
- Integration merge commit: see `git log --oneline -1` in /var/www/vltk-mobile

## Gate G1 — Backend pytest
- unit: 618 passed (`_pytest_unit.txt`)
- integration: 103 passed (`_pytest_integration.txt`)

## Gate G2 — Backend live smoke
- /health: 200 OK, status=ok (`_health.json`)
- /v1/map?map_type=City: 200 OK, envelope with 5 city maps (`_v1_map_city.json`)
- /v1/map/1: 200 OK, envelope with 1 map (`_v1_map_id_1.json`)
- /openapi.json: 112954 bytes (`_openapi.json`)
- Uvicorn log: `_uvicorn.log`

## Gate G3 — Unity EditMode (Backend namespace)
- 22/22 [Test] methods passed
- Result state: Passed, total 4.24 s
- Filter: VLTK.Tests.Backend
- File: `_unity_editmode.json`
- TestRunner job_id: 1a285b208f7141cd980681b4356972cb

## Gate G4 — Unity PlayMode / manual artifact
- Skipped: FS-01D scope intentionally limits the slice to 2 endpoints
  (`/health` and `/v1/map`) and uses FakeHttpTransport for EditMode; no
  PlayMode scene wiring is part of FS-01D. The IHttpTransport boundary
  keeps UnityWebRequest out of EditMode tests so PlayMode smoke is
  deferred to the next slice that wires BackendClient into a runtime
  MonoBehaviour (per FS-01B plan, deferred).

## Notes / risks carried into next slice
- 2 integration fixes were required for the merged code to compile and
  test green:
  1. `RestGameBackend.cs` was missing `using UnityEngine.Networking;`
     for `UnityWebRequest.EscapeURL` (used in query string builder).
  2. `BackendClient` constructors used to auto-apply
     `BackendConfig.ApplyStreamingAssetsOverrideIfPresent()` which
     silently overrode test configs from `StreamingAssets/BackendConfig.json`.
     Removed auto-apply; runtime callers who want the override can call
     it explicitly after `BackendConfig.LoadOrDefault()`.
- TCVN3 mojibake still present in some map names returned by `/v1/map`
  (e.g. "Ph­îng T­êng" should be "Phượng Tường") — pre-existing data
  port issue in backend map module, out of FS-01D scope.
