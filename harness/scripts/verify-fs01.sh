#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# verify-fs01.sh — composite proof command for FS-01 (full-stack backend
# integration foundation). Replaces the no-op `true` that was previously the
# FS-01 verify_command and which gave the matrix a hollow `pass` flag.
#
# Gates:
#   G1 backend pytest
#   G2 backend health + OpenAPI smoke
#   G3 unity EditMode (integration lane only)
#   G4 unity PlayMode or manual artifact (integration lane only)
#
# Exit codes:
#   0  all required gates passed (Unity gates may be skipped via
#      VLTK_FS01_SKIP_UNITY=1 on the offline lane)
#   1  G1 failed
#   2  G2 failed
#   3  G3 failed
#   4  G4 failed
#   5  pre-condition failed (venv missing, port busy, etc.)
#
# Required environment (any lane):
#   HARNESS_DB=/var/www/vltk-mobile/harness/harness.db
#   VLTK_BACKEND_DIR=/var/www/vltk-mobile/backend  (default)
#   VLTK_ARTIFACT_DIR=/var/www/vltk-mobile/tests    (default)
#
# Required environment (integration lane only, ignored otherwise):
#   VLTK_FS01_SKIP_UNITY=0 (default) — run G3 + G4
#   VLTK_FS01_EDITMODE_RESULTS — path to the focused EditMode XML (defaults to
#                                $VLTK_ARTIFACT_DIR/.editmode_results.xml)
#   VLTK_FS01_PLAYMODE_RESULTS — path to the focused PlayMode XML (defaults to
#                                $VLTK_ARTIFACT_DIR/.playmode_results.xml)
#   VLTK_FS01_PLAYMODE_ARTIFACT — path to the manual artifact JSON (defaults
#                                  to $VLTK_ARTIFACT_DIR/.playmode_artifact.json)
#   VLTK_FS01_EDITMODE_FILTER    — test name filter (default "VLTK.Tests.Backend")
#   VLTK_FS01_PLAYMODE_FILTER    — test name filter (default "VLTK.Tests.PlayMode.Backend")
# -----------------------------------------------------------------------------

set -u
set -o pipefail

BACKEND_DIR="${VLTK_BACKEND_DIR:-/var/www/vltk-mobile/backend}"
ART_DIR="${VLTK_ARTIFACT_DIR:-/var/www/vltk-mobile/tests}"
SKIP_UNITY="${VLTK_FS01_SKIP_UNITY:-0}"

PYTEST_LOG="${ART_DIR}/.pytest_log.txt"
PYTEST_UNIT="${ART_DIR}/.pytest_unit.txt"
PYTEST_INTEGRATION="${ART_DIR}/.pytest_integration.txt"
PYTEST_E2E="${ART_DIR}/.pytest_e2e.txt"
HEALTH_JSON="${ART_DIR}/.health_smoke.json"
OPENAPI_JSON="${ART_DIR}/.openapi.json"
UVICORN_LOG="${ART_DIR}/.uvicorn.log"
EDITMODE_XML="${VLTK_FS01_EDITMODE_RESULTS:-${ART_DIR}/.editmode_results.xml}"
PLAYMODE_XML="${VLTK_FS01_PLAYMODE_RESULTS:-${ART_DIR}/.playmode_results.xml}"
PLAYMODE_ART="${VLTK_FS01_PLAYMODE_ARTIFACT:-${ART_DIR}/.playmode_artifact.json}"

mkdir -p "${ART_DIR}"

ok()   { printf "  [ OK ] %s\n" "$*"; }
fail() { printf "  [FAIL] %s\n" "$*"; }
info() { printf "  [----] %s\n" "$*"; }
hdr()  { printf "\n=== %s ===\n" "$*"; }

exit_code=0

# -----------------------------------------------------------------------------
# Pre-conditions
# -----------------------------------------------------------------------------
hdr "Pre-conditions"

if [[ ! -x "${BACKEND_DIR}/.venv/bin/python" ]]; then
  fail "backend venv missing: ${BACKEND_DIR}/.venv/bin/python"
  echo "Hint: cd ${BACKEND_DIR} && pip install -e \".[dev]\""
  exit 5
fi
ok "backend venv present"

if [[ "${SKIP_UNITY}" == "1" ]]; then
  info "VLTK_FS01_SKIP_UNITY=1 -> G3 + G4 will be reported as skipped (offline lane)"
else
  if ! command -v curl >/dev/null 2>&1; then
    fail "curl missing (needed for G2)"
    exit 5
  fi
fi

# -----------------------------------------------------------------------------
# Gate G1 — Backend pytest
# -----------------------------------------------------------------------------
hdr "Gate G1: backend pytest"

cd "${BACKEND_DIR}" || { fail "cannot cd to ${BACKEND_DIR}"; exit 5; }

.venv/bin/python -m pytest tests/unit        -q > "${PYTEST_UNIT}"         2>&1 || rc_unit=$?
.venv/bin/python -m pytest tests/integration -q > "${PYTEST_INTEGRATION}"  2>&1 || rc_int=$?
.venv/bin/python -m pytest tests/e2e         -q > "${PYTEST_E2E}"          2>&1 || rc_e2e=$?
.venv/bin/python -m pytest tests/            -q > "${PYTEST_LOG}"          2>&1 || rc_all=$?

rc_unit="${rc_unit:-0}"; rc_int="${rc_int:-0}"; rc_e2e="${rc_e2e:-0}"; rc_all="${rc_all:-0}"

if [[ "${rc_all}" -ne 0 ]]; then
  fail "G1 pytest failed (exit=${rc_all})"
  info "see ${PYTEST_LOG} (tail below)"
  tail -n 40 "${PYTEST_LOG}" || true
  exit 1
fi
ok "G1 pytest exited 0"

# Pass criterion: zero failed, zero error. Allow X passed; require 0 failed.
if ! grep -E '[0-9]+ failed' "${PYTEST_LOG}" >/dev/null 2>&1; then
  ok "G1 pytest summary line clean (no 'failed' substring)"
else
  failed_line=$(grep -E '[0-9]+ failed' "${PYTEST_LOG}" | tail -n 1 || true)
  info "G1 pytest summary: ${failed_line}"
  if echo "${failed_line}" | grep -qE '[^0-9]0 failed'; then
    ok "G1 zero failed"
  else
    fail "G1 has non-zero failures: ${failed_line}"
    exit 1
  fi
fi

# -----------------------------------------------------------------------------
# Gate G2 — Backend health + OpenAPI smoke
# -----------------------------------------------------------------------------
hdr "Gate G2: backend health + openapi smoke"

# Free port 8020 if something is squatting on it.
if command -v fuser >/dev/null 2>&1; then
  fuser -k 8020/tcp >/dev/null 2>&1 || true
elif command -v lsof >/dev/null 2>&1; then
  lsof -ti tcp:8020 | xargs -r kill -9 >/dev/null 2>&1 || true
fi

.venv/bin/uvicorn app.main:app --host 127.0.0.1 --port 8020 > "${UVICORN_LOG}" 2>&1 &
UVICORN_PID=$!
trap 'kill ${UVICORN_PID} 2>/dev/null || true' EXIT

ready=0
for _ in $(seq 1 20); do
  if curl -sS http://127.0.0.1:8020/health >/dev/null 2>&1; then ready=1; break; fi
  sleep 0.5
done

if [[ "${ready}" -ne 1 ]]; then
  fail "uvicorn on 127.0.0.1:8020 did not become ready within 10s"
  info "see ${UVICORN_LOG} (tail below)"
  tail -n 40 "${UVICORN_LOG}" || true
  kill "${UVICORN_PID}" 2>/dev/null || true
  exit 2
fi
ok "uvicorn ready on 127.0.0.1:8020"

curl -sS http://127.0.0.1:8020/health      > "${HEALTH_JSON}"   || { fail "curl /health failed"; kill "${UVICORN_PID}" 2>/dev/null || true; exit 2; }
curl -sS http://127.0.0.1:8020/openapi.json > "${OPENAPI_JSON}" || { fail "curl /openapi.json failed"; kill "${UVICORN_PID}" 2>/dev/null || true; exit 2; }

kill "${UVICORN_PID}" 2>/dev/null || true

if ! command -v python3 >/dev/null 2>&1; then
  fail "python3 missing; cannot validate health/openapi JSON"
  exit 2
fi

.venv/bin/python - <<PY
import json, sys
try:
    h = json.load(open("${HEALTH_JSON}"))
    o = json.load(open("${OPENAPI_JSON}"))
except Exception as e:
    print("FAIL: cannot parse health/openapi json:", e)
    sys.exit(2)

if h.get("status") != "ok":
    print("FAIL: health.status != ok:", h)
    sys.exit(2)
if h.get("service") != "vltk-game-server":
    print("FAIL: health.service != vltk-game-server:", h)
    sys.exit(2)
print("OK: health =", h)

paths = o.get("paths", {})
required = [
    "/health",
    "/v1/account/login",
    "/v1/role/by-account/{account}",
    "/v1/player/by-role/{role_id}",
    "/v1/map/enter",
    "/v1/map/position/{role_id}",
    "/v1/item/by-role/{role_id}",
    "/v1/skill/by-role/{role_id}",
    "/v1/skill/cast/check",
    "/v1/skill/cast",
]
missing = [p for p in required if p not in paths]
if missing:
    print("FAIL: openapi missing required paths:", missing)
    sys.exit(2)
print("OK: openapi has all", len(required), "required paths")
PY
if [[ $? -ne 0 ]]; then
  fail "G2 openapi/health validation failed"
  exit 2
fi
ok "G2 health + openapi validated"

# -----------------------------------------------------------------------------
# Gate G3 — Unity EditMode (integration lane only)
# -----------------------------------------------------------------------------
hdr "Gate G3: unity EditMode (focused)"

if [[ "${SKIP_UNITY}" == "1" ]]; then
  info "G3 skipped (VLTK_FS01_SKIP_UNITY=1)"
else
  if [[ ! -f "${EDITMODE_XML}" ]]; then
    fail "G3 results file not found: ${EDITMODE_XML}"
    info "the integration worker must run mcp_unityMCP_run_tests with writeResultsTo=${EDITMODE_XML} before invoking this script"
    exit 3
  fi
  ok "G3 results file present: ${EDITMODE_XML}"

  .venv/bin/python - <<PY
import sys, xml.etree.ElementTree as ET
try:
    tree = ET.parse("${EDITMODE_XML}")
except Exception as e:
    print("FAIL: cannot parse", "${EDITMODE_XML}", ":", e)
    sys.exit(3)
root = tree.getroot()
# NUnit3 schema: test-run / test-suite[type=TestSuite] / test-suite[type=TestFixture] / test-case
# Aggregate totals from the root attributes if present; fall back to summing.
def attr_int(node, name, default=0):
    v = node.attrib.get(name, default)
    try: return int(v)
    except Exception: return default

total  = attr_int(root, "total",  0)
passed = attr_int(root, "passed", 0)
failed = attr_int(root, "failed", 0)
skipped = attr_int(root, "skipped", 0)
if total == 0:
    # Sum over test-case nodes
    for tc in root.iter("test-case"):
        result = tc.attrib.get("result", "")
        if result == "Passed":   passed  += 1
        elif result == "Failed": failed  += 1
        elif result in ("Skipped", "Inconclusive"): skipped += 1
        total += 1

print(f"EditMode focused: total={total} passed={passed} failed={failed} skipped={skipped}")
if failed != 0:
    print(f"FAIL: focused EditMode has {failed} failures")
    sys.exit(3)
if total == 0:
    print("FAIL: no EditMode tests were collected for the focused filter")
    sys.exit(3)
print("OK: focused EditMode all passed")
PY
  if [[ $? -ne 0 ]]; then
    fail "G3 EditMode focused suite failed"
    exit 3
  fi
  ok "G3 EditMode focused suite all passed"
fi

# -----------------------------------------------------------------------------
# Gate G4 — Unity PlayMode or manual artifact (integration lane only)
# -----------------------------------------------------------------------------
hdr "Gate G4: unity PlayMode or manual artifact"

if [[ "${SKIP_UNITY}" == "1" ]]; then
  info "G4 skipped (VLTK_FS01_SKIP_UNITY=1)"
else
  if [[ -f "${PLAYMODE_XML}" ]]; then
    ok "G4 PlayMode results file present: ${PLAYMODE_XML}"
    .venv/bin/python - <<PY
import sys, xml.etree.ElementTree as ET
try:
    tree = ET.parse("${PLAYMODE_XML}")
except Exception as e:
    print("FAIL: cannot parse", "${PLAYMODE_XML}", ":", e)
    sys.exit(4)
root = tree.getroot()
def attr_int(node, name, default=0):
    v = node.attrib.get(name, default)
    try: return int(v)
    except Exception: return default

total  = attr_int(root, "total",  0)
failed = attr_int(root, "failed", 0)
if total == 0:
    for tc in root.iter("test-case"):
        if tc.attrib.get("result") == "Failed": failed += 1
        total += 1
print(f"PlayMode focused: total={total} failed={failed}")
if failed != 0:
    print(f"FAIL: focused PlayMode has {failed} failures")
    sys.exit(4)
if total == 0:
    print("FAIL: no PlayMode tests were collected for the focused filter")
    sys.exit(4)
print("OK: focused PlayMode all passed")
PY
    rc_g4=$?
    if [[ "${rc_g4}" -ne 0 ]]; then
      fail "G4 PlayMode focused suite failed"
      exit 4
    fi
    ok "G4 PlayMode focused suite all passed"
  elif [[ -f "${PLAYMODE_ART}" ]]; then
    ok "G4 manual artifact file present: ${PLAYMODE_ART}"
    .venv/bin/python - <<PY
import json, sys
try:
    a = json.load(open("${PLAYMODE_ART}"))
except Exception as e:
    print("FAIL: cannot parse", "${PLAYMODE_ART}", ":", e)
    sys.exit(4)
if a.get("result") != "ok":
    print("FAIL: artifact result != ok:", a)
    sys.exit(4)
required = ["timestamp", "editor", "result", "screenshot", "console_log_excerpt", "evidence_note"]
missing = [k for k in required if k not in a]
if missing:
    print("FAIL: artifact missing fields:", missing)
    sys.exit(4)
print("OK: manual artifact valid:", a.get("evidence_note"))
PY
    rc_g4=$?
    if [[ "${rc_g4}" -ne 0 ]]; then
      fail "G4 manual artifact invalid"
      exit 4
    fi
    ok "G4 manual artifact valid"
  else
    fail "G4 requires either ${PLAYMODE_XML} (PlayMode results) or ${PLAYMODE_ART} (manual artifact)"
    info "the integration worker must run mcp_unityMCP_run_tests with writeResultsTo=${PLAYMODE_XML} OR capture a manual artifact at ${PLAYMODE_ART}"
    exit 4
  fi
fi

# -----------------------------------------------------------------------------
# Final
# -----------------------------------------------------------------------------
hdr "Summary"
ok "G1 backend pytest"
ok "G2 backend health + openapi"
if [[ "${SKIP_UNITY}" == "1" ]]; then
  info "G3 (Unity EditMode) skipped — re-run on integration lane"
  info "G4 (Unity PlayMode/manual) skipped — re-run on integration lane"
else
  ok "G3 Unity EditMode (focused)"
  ok "G4 Unity PlayMode or manual artifact"
fi
echo
echo "All required FS-01 gates passed."
exit 0
