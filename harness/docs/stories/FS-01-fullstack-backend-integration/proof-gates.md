# FS-01 Proof Gates — Design

> **Status: design only.** This document is the durable reference for what
> "FS-01 done" means. The integration worker (`vltk-unity`) is the only lane
> that can run all four gates end-to-end; the offline lane runs G1 + G2 only.
> Proof flags stay `0` until a real artifact is captured.

## 0. Context (so this isn't reading-required history)

- FS-01 is the tracking story in `harness.db` for the full-stack Unity ↔ FastAPI integration. Today its `verify_command` is the no-op `true`, so the matrix's `last_verified_result=pass` for FS-01 carries no parity weight (see `docs/PORT_STATUS.md` § "Harness matrix vs reality — audit 2026-06-12").
- FS-01A retry2 (`t_75a76d11`, DONE 2026-06-13 15:43) confirmed backend 722/722 pytest and produced a minimal Phase 1 surface (account/role/player/map/item/skill REST).
- FS-01B retry2 (`t_05f271a4`) is producing the file-level Unity `IGameBackend` / `RestBackend` / `MockBackend` plan.
- FS-01C retry2 (**this card**) is the design of the proof ladder.
- FS-01D retry2/3 (`t_e2d3ccb3`, `t_c2a0417c`) implements the smallest Unity REST slice, parents = A + B + C.
- FS-01E (not yet a card) is the integration worker that merges D, recompiles in the live Editor, runs every gate, applies the documented story-update commands **only after** the gates pass.

## 1. The four gates

### Gate G1 — Backend pytest (offline-runnable)

**What it proves:** backend domain authority is healthy; 722/722 holds; no regression introduced by the new test that confirms the `Authorization` header (added by FS-01D if needed).

**Who runs it:** any lane. Must run on `vltk-unity` for the canonical artifact.

**Exact commands (run from `/var/www/vltk-mobile`):**

```bash
cd /var/www/vltk-mobile/backend
.venv/bin/python -m pytest tests/unit -q > ../../tests/.pytest_unit.txt 2>&1
.venv/bin/python -m pytest tests/integration -q > ../../tests/.pytest_integration.txt 2>&1
.venv/bin/python -m pytest tests/e2e -q > ../../tests/.pytest_e2e.txt 2>&1
.venv/bin/python -m pytest tests/ -q > ../../tests/.pytest_log.txt 2>&1
```

**Pass criteria:**

- All four files end with `722 passed` (or `X passed, 0 failed, 0 error` for `X` matching the historical 722 — re-count is OK if the new test is added, but the failure count must be zero).
- Exit code of the final `tests/ -q` invocation = 0.

**Re-verified at this design time (2026-06-13):** `722 passed in 45.77s`.

**Pre-condition for the integration worker:** the venv exists at `/var/www/vltk-mobile/backend/.venv`; if not, `pip install -e ".[dev]"` first (per `backend/README.md`).

### Gate G2 — Backend health + OpenAPI smoke (offline-runnable)

**What it proves:** a real uvicorn can serve `/health` and the OpenAPI document describes the Phase 1 endpoints we plan to consume.

**Who runs it:** any lane. Must run on `vltk-unity` for the canonical artifact.

**Exact commands:**

```bash
cd /var/www/vltk-mobile/backend
.venv/bin/uvicorn app.main:app --host 127.0.0.1 --port 8020 > ../../tests/.uvicorn.log 2>&1 &
UVICORN_PID=$!
trap "kill $UVICORN_PID 2>/dev/null || true" EXIT
# Wait for readiness (up to 10s)
for i in $(seq 1 20); do
  if curl -sS http://127.0.0.1:8020/health > /dev/null 2>&1; then break; fi
  sleep 0.5
done
curl -sS http://127.0.0.1:8020/health > ../../tests/.health_smoke.json
curl -sS http://127.0.0.1:8020/openapi.json > ../../tests/.openapi.json
kill $UVICORN_PID
```

**Pass criteria:**

- `.health_smoke.json` parses as JSON with `status == "ok"`, `service == "vltk-game-server"`.
- `.openapi.json` parses as JSON, has `paths."/health"`, and contains the Phase 1 path keys:
  `/v1/account/login`, `/v1/role/by-account/{account}`, `/v1/player/by-role/{role_id}`, `/v1/map/enter`, `/v1/map/position/{role_id}`, `/v1/item/by-role/{role_id}`, `/v1/skill/by-role/{role_id}`, `/v1/skill/cast/check`, `/v1/skill/cast`.

**Pre-condition:** port 8020 free. If another uvicorn is already on 8020, the integration worker must stop it first (`fuser -k 8020/tcp 2>/dev/null` or `pkill -f 'uvicorn.*8020'`).

### Gate G3 — Unity EditMode (integration lane only)

**What it proves:** the new `IGameBackend` / `RestBackend` / `MockBackend` slice from FS-01D compiles in the live Editor, the focused suite passes, and zero new CS errors appear.

**Who runs it:** the `vltk-unity` integration worker, via `mcp_unityMCP_run_tests` (or `mcp_unityMCP_run_tests_for_job` with `includeFailedTests=true` to surface names).

**Exact commands (integration lane, MCP call):**

```text
# 1. Define VLTK_ENABLE_TESTS for the Standalone group (already in place as of
#    the 2026-06-12 audit, see PORT_STATUS.md "First real EditMode run").
#    Re-verify with:
mcp_unityMCP_get_settings  # confirm scriptingDefineSymbols has VLTK_ENABLE_TESTS

# 2. Force-recompile after the FS-01D branch is merged.
mcp_unityMCP_refresh_unity  # or just wait for the Editor auto-recompile

# 3. Check console for new CS errors.
mcp_unityMCP_read_console  types=["error"] count=200 includeStacktrace=false
#    Pass if zero new errors mention IGameBackend, RestBackend, MockBackend,
#    BackendConfig, BackendSession, BackendHttp, or Assets/Scripts/Backend/.

# 4. Run the focused EditMode suite. Use a name filter so we don't depend on
#    the unstable pre-existing 33-62 failures.
mcp_unityMCP_run_tests \
    testMode="EditMode" \
    assemblyNames=["VLTK.Tests.EditMode"] \
    testNames=["VLTK.Tests.Backend.*"] \
    writeResultsTo="tests/.editmode_results.xml"
```

**Pass criteria:**

- Console has 0 new CS errors referencing the new code.
- `tests/.editmode_results.xml` exists, parses, and the focused suite has `total == passed`, `failed == 0`, `skipped == 0` (any new test must not be skipped; if a test is intentionally opt-in, it must use `Assert.Ignore` and that fact must be noted in the story `evidence`).
- The full EditMode suite (run separately, NOT the focused run) is **not regressed**: total failures ≤ the post-2026-06-12 #5 baseline of 33. A spike to 50+ means the new code introduced a regression and FS-01 is **not** done.

**Pre-condition:** FS-01D's branch is merged into `dev`; the integration worker triggered a recompile; the Editor console is clean of pre-existing compile errors.

**Why focused, not full-suite, for the pass criterion:** the 33 pre-existing failures in `docs/PORT_STATUS.md` are not FS-01's responsibility to close. The focused run proves the new code; the full-suite regression check proves the new code did not break anything else.

### Gate G4 — Unity PlayMode or manual artifact (integration lane only)

**What it proves:** the REST path actually works end-to-end inside the Editor, not just at the unit level. Either an automated PlayMode test runs through the `RestBackend.HealthAsync` path (preferred) OR a captured manual artifact proves the same thing.

**Who runs it:** the `vltk-unity` integration worker.

**Exact commands (PlayMode path, preferred):**

```text
# Define a PlayMode test in FS-01D:
#   VLTK.Tests.PlayMode.Backend.RestBackendHealthPlayModeTests
#     [UnityTest] IEnumerator HealthAsync_ReturnsExpectedDto_WhenServerOk()
#       1. Spin up uvicorn on 127.0.0.1:8020 (a managed fixture).
#       2. Build RestBackend(config) + BackendHttp real (no fake here).
#       3. await HealthAsync().
#       4. Assert dto.Status == "ok" and dto.Service == "vltk-game-server".
#       5. Tear down uvicorn.

mcp_unityMCP_run_tests \
    testMode="PlayMode" \
    assemblyNames=["VLTK.Tests.PlayMode"] \
    testNames=["VLTK.Tests.PlayMode.Backend.*"] \
    writeResultsTo="tests/.playmode_results.xml"
```

**Pass criteria (PlayMode):** `tests/.playmode_results.xml` exists, parses, focused PlayMode suite has `total == passed`, `failed == 0`.

**Exact commands (manual artifact fallback, only if PlayMode fixture is not feasible in this iteration):**

```bash
# Integration worker, inside the Editor:
#   1. Open the project in the live Editor (already running).
#   2. Start uvicorn on 8020 (same as G2).
#   3. Open Window > General > Test Runner > PlayMode > run RestBackendHealthPlayModeTests.
#   4. After the test passes (or after manual verification of the call in
#      the console), save the artifact:
cat > tests/.playmode_artifact.json <<EOF
{
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "editor": "Unity 6000.4.7f1 (vltk-mobile@$(git rev-parse --short HEAD))",
  "result": "ok",
  "rest_backend_version": "$(grep -m1 AssemblyVersion Assets/Scripts/Backend/RestBackend.cs 2>/dev/null || echo 'n/a')",
  "screenshot": "tests/.playmode_screenshot.png",
  "console_log_excerpt": "tests/.playmode_console.log",
  "evidence_note": "RestBackend.HealthAsync() against uvicorn :8020 returned status=ok, service=vltk-game-server."
}
EOF
```

**Pass criteria (manual):** `tests/.playmode_artifact.json` exists, parses, `result == "ok"`, screenshot and console log exist, and the editor version matches the current `dev` HEAD.

**Pre-condition:** PlayMode suite is enabled (VLTK_ENABLE_TESTS is defined for the standalone group; the PlayMode asmdef is gated by it).

## 2. Composite `verify_command`

The composite command replaces the no-op `true` on FS-01. The script is the source
of truth; the `harness-cli story update` commands later quote the same gates.

### `harness/scripts/verify-fs01.sh`

The script lives at [`harness/scripts/verify-fs01.sh`](../../../../scripts/verify-fs01.sh) (relative
to this doc). It runs G1 + G2 inline and **delegates G3 + G4 to the integration
worker** because they need the live Editor.

**Use it on the integration lane:**

```bash
cd /var/www/vltk-mobile
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
    bash harness/scripts/verify-fs01.sh
```

**Use it on an offline lane (G1 + G2 only; G3 + G4 are reported as `skipped`):**

```bash
cd /var/www/vltk-mobile
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
    VLTK_FS01_SKIP_UNITY=1 \
    bash harness/scripts/verify-fs01.sh
```

**Exit codes:**

| Code | Meaning |
| ---: | --- |
| 0 | G1 + G2 pass; G3 + G4 pass OR were skipped because `VLTK_FS01_SKIP_UNITY=1` |
| 1 | G1 failed (backend pytest) |
| 2 | G2 failed (backend health smoke) |
| 3 | G3 failed (Unity EditMode) |
| 4 | G4 failed (Unity PlayMode / manual artifact) |
| 5 | Pre-condition failed (venv missing, port busy, no Editor) |

**`harness-cli story verify FS-01` will exit 0 only when the script exits 0.** The
integration worker replaces the no-op `verify_command` with this script as part
of FS-01E. Today the matrix shows `last_verified_result=pass` because
`verify_command=true` exits 0 — that is exactly the hollow-green failure mode
`docs/PORT_STATUS.md` flagged in 2026-06-12.

## 3. Story update recommendations (the integration worker runs these, not this card)

> **Do not run these from FS-01C.** The integration worker runs them only after
> every gate has a real artifact on disk. The commands are spelled out here so
> the next worker has the exact spec to copy.

### After G1 + G2 pass (offline lane can do this)

```bash
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
./harness/scripts/bin/harness-cli story update --id FS-01 \
    --unit 1 --integration 1 --e2e 1 --platform 0 \
    --notes "G1 backend pytest 722/722 passed (artifact: tests/.pytest_log.txt); G2 health smoke status=ok (artifact: tests/.health_smoke.json, tests/.openapi.json). G3/G4 deferred to FS-01E."
```

### After G3 passes (integration lane, real EditMode artifact)

```bash
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
./harness/scripts/bin/harness-cli story update --id FS-01 \
    --unit 1 --integration 1 --e2e 1 --platform 0 \
    --notes "G3 EditMode focused suite passed: <N>/<N> (artifact: tests/.editmode_results.xml); full suite regressions check <OK|NEW> (artifact: <path>). G4 pending."
```

### After G4 passes (integration lane, real PlayMode or manual artifact)

```bash
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
./harness/scripts/bin/harness-cli story update --id FS-01 \
    --unit 1 --integration 1 --e2e 1 --platform 1 \
    --notes "G4 PlayMode/manual artifact captured (artifact: tests/.playmode_results.xml OR tests/.playmode_artifact.json). All four gates green."
```

### Re-verify (any time after `verify_command` is replaced)

```bash
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
./harness/scripts/bin/harness-cli story verify FS-01
```

### Replace the no-op `verify_command` (FS-01E)

```bash
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
./harness/scripts/bin/harness-cli story update --id FS-01 \
    --verify "bash harness/scripts/verify-fs01.sh"
```

## 4. Commands (one consolidated copy-paste block)

```bash
# === Gate G1 (offline-runnable) ===
cd /var/www/vltk-mobile/backend
.venv/bin/python -m pytest tests/unit       -q > ../tests/.pytest_unit.txt         2>&1
.venv/bin/python -m pytest tests/integration -q > ../tests/.pytest_integration.txt 2>&1
.venv/bin/python -m pytest tests/e2e        -q > ../tests/.pytest_e2e.txt          2>&1
.venv/bin/python -m pytest tests/           -q > ../tests/.pytest_log.txt          2>&1

# === Gate G2 (offline-runnable) ===
.venv/bin/uvicorn app.main:app --host 127.0.0.1 --port 8020 > ../tests/.uvicorn.log 2>&1 &
UVICORN_PID=$!
trap "kill $UVICORN_PID 2>/dev/null || true" EXIT
for i in $(seq 1 20); do
  curl -sS http://127.0.0.1:8020/health > /dev/null 2>&1 && break
  sleep 0.5
done
curl -sS http://127.0.0.1:8020/health      > ../tests/.health_smoke.json
curl -sS http://127.0.0.1:8020/openapi.json > ../tests/.openapi.json
kill $UVICORN_PID

# === Gate G3 (integration lane, MCP) ===
# (1) Verify VLTK_ENABLE_TESTS is defined.
mcp_unityMCP_get_settings
# (2) Confirm console clean after FS-01D merge.
mcp_unityMCP_read_console types=["error"] count=200
# (3) Run focused EditMode suite.
mcp_unityMCP_run_tests \
    testMode="EditMode" \
    assemblyNames=["VLTK.Tests.EditMode"] \
    testNames=["VLTK.Tests.Backend.*"] \
    writeResultsTo="tests/.editmode_results.xml"

# === Gate G4 (integration lane, MCP) ===
mcp_unityMCP_run_tests \
    testMode="PlayMode" \
    assemblyNames=["VLTK.Tests.PlayMode"] \
    testNames=["VLTK.Tests.PlayMode.Backend.*"] \
    writeResultsTo="tests/.playmode_results.xml"
# (or capture manual artifact at tests/.playmode_artifact.json)

# === Composite verify (replaces no-op `true`) ===
cd /var/www/vltk-mobile
HARNESS_DB=/var/www/vltk-mobile/harness/harness.db \
    bash harness/scripts/verify-fs01.sh
```

## 5. What is NOT in this card

- Setting any FS-01 proof flag to `1`. That is the integration worker's job, after the artifacts exist.
- Replacing the no-op `verify_command` with `verify-fs01.sh` in the matrix. Same reason.
- Creating durable decision records (`docs/decisions/0008-…md`, `0009-…md`). Those are recorded by the integration worker at FS-01E time, after the auth round-trip test passes.
- Implementing `IGameBackend` / `RestBackend` / `MockBackend`. That is FS-01D.
- Closing the existing backlog item "Replace FS-01 no-op verify with real backend+Unity integration verifier". That closes when FS-01E runs `harness-cli story verify FS-01` and exits 0.

## 6. Cross-references

- High-risk story packet files: `execplan.md`, `overview.md`, `design.md`, `validation.md` in the same folder.
- AGENTS.md root rules: "Definition of done" (no false green), "Two lanes" (offline vs integration), "Worktree isolation" (this card lives on a worktree, not `dev`).
- `harness/docs/HARNESS.md` § "Story Verification" — `verify_command` semantics, exit codes, `last_verified_at` / `last_verified_result`.
- `harness/docs/CONTEXT_RULES.md` — what an agent must read at each phase; this card is in Planning/Validation phase.
- `harness/docs/PORT_STATUS.md` § "Harness matrix vs reality — audit 2026-06-12" — the false-green failure this card is designed to fix.
- `harness/.hermes/skills/vltk-port-status-audit/references/fullstack-backend-integration-kanban.md` — operational history and the recommended graph this design follows.
- `backend/AGENTS.md` — backend conventions the integration worker must respect (FastAPI DDD, pytest layout, port 8020, Vietnamese comments).

## 7. Artifact directory

`verify-fs01.sh` defaults to writing its artifacts to `${VLTK_ARTIFACT_DIR:-/var/www/vltk-mobile/tests}/`. This card adds `tests/` to the root `.gitignore` (with an FS-01 reference) so the artifacts stay on disk for review but never get committed. Override `VLTK_ARTIFACT_DIR` to point at a different scratch dir if needed.

Artifact filenames (all dotfiles; the leading `.` keeps the dir tidy when the integration worker eyeballs it):

```text
tests/.pytest_unit.txt
tests/.pytest_integration.txt
tests/.pytest_e2e.txt
tests/.pytest_log.txt
tests/.uvicorn.log
tests/.health_smoke.json
tests/.openapi.json
tests/.editmode_results.xml
tests/.playmode_results.xml
tests/.playmode_artifact.json
```
