# Validation

## Proof Strategy

Evidence is layered. This migration requires Python backend tests, Unity assembly
validation, focused EditMode/PlayMode coverage, and a stale-reference audit. These
checks can establish a local `FUNCTIONAL` / `UNVERIFIED` subset but cannot promote
proof flags without a live backend-to-Unity E2E artifact and production trust
material.

## Test Plan

| Layer | Cases | Current result |
| --- | --- | --- |
| Unit | FastAPI account/role/map/movement; Unity create/login/create-role/enter-map client and runner wiring | Relevant Python 37/37 PASS; Unity EditMode 10/10 PASS |
| Integration | Python FastAPI application against its configured test database | Relevant Python 37/37 PASS |
| E2E | Editor-first boot against a live FastAPI backend through REST | Local PASS on `127.0.0.1:8120`; dirty-revision proof flag remains false |
| Platform | Unity LinuxEditor production assemblies and scene | Compile/Console PASS; PlayMode 2/2 PASS; Android absent |
| Content | Ba Lăng map `53` artifact/provenance/digest and fail-closed fallback policy | PASS unsigned/fail-closed; production signature absent; 182 legacy SPR references unresolved |
| Logs/Audit | secret redaction tests and safe backend log sites | Local tests PASS; correlated runtime trace artifact absent |

## Commands

Migration verification commands:

```text
cd backend && pytest
cd backend && .venv/bin/pytest -q tests/integration/modules/account tests/integration/modules/role tests/integration/modules/map
python3 -m pytest -q scripts/map_runtime/test_map_runtime.py
python3 scripts/map_runtime/verify.py --pretty
python3 scripts/check_unity_assembly_boundaries.py --root .
python3 -m unittest -q scripts.test_check_unity_assembly_boundaries
git diff --check
```

Observed on 2026-07-20:

```text
Python relevant integration: 37 passed.
Python full suite: 18,258 passed; 5 unrelated failures because the canonical PC
  activitysys/config/12/variables.lua path is absent from /var/www/jx-pc.
Map tests: 6 passed.
Map verifier: verified_unsigned_fail_closed; productionSignatureVerified=false.
Unity compile: PASS, zero compiler errors.
Unity EditMode focused: 10 passed, 0 failed.
Unity PlayMode Production: 2 passed, 0 failed.
Unity live Production REST smoke: useMock=false -> create account -> login ->
  create role -> map 53 (48032,117504) -> movement sync; PASS.
Assembly boundaries: clean; 33 regression tests passed.
Spec validator premerge: PASS.
git diff --check: PASS.
Go server-runtime directory: deleted (52 files / 5.6 MB filesystem tree before cleanup).
```

## Residual blockers

- Map runtime trust is `verified_unsigned_fail_closed`; a production signature and
  key are unavailable.
- The map catalog has six unresolved unique SPR paths referenced 182 times.
- Local live REST E2E passed, but it is not captured against a clean immutable
  revision/artifact and does not prove realtime, production deployment, or load.
- Android device, performance, parity, rollback, restore, and release evidence are
  absent/out of scope.

## Acceptance Evidence

Before any future status or proof-flag upgrade, evidence must include an exact
command, timestamp, clean git/content revision, environment, pass/fail result,
artifact path/hash, secret-redaction proof, and a Harness row update through
`scripts/bin/harness-cli`. This run intentionally leaves story status
`in_progress` and every proof flag false.
