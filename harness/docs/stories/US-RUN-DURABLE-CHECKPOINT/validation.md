# US-RUN-DURABLE-CHECKPOINT Validation

## Proof Strategy

Crash/retry tests prove no ACK without checkpoint, no duplicate mutation and
outbox uniqueness by `(realm, aggregate, version, event_type)`, matching the
canonical target SQL while allowing multiple distinct event types per version.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | 33 deterministic codec/hash + 35 UoW/validation/error tests. |
| Integration | 24 checkpoint transaction + 10 identity regression tests on disposable PostgreSQL 16.14. |
| E2E | Deferred to WSS child. |
| Platform | Upgrade/collision/RLS/data-guarded rollback/re-upgrade passed. |
| Performance | Explicitly deferred to production SLO/benchmark follow-up. |
| Logs/Audit | Production correlation/lag instrumentation deferred to WSS/outbox worker. |

## Fixtures

- Deterministic runtime state and duplicate/crash injection.

## Commands

```text
pytest -q tests/unit/modules/runtime/test_checkpoint_codec.py tests/unit/modules/runtime/test_runtime_uow.py
68 passed

GAME_V1_RUNTIME_TEST_URL='postgresql+psycopg://<ephemeral-user>:<redacted>@127.0.0.1:<random>/postgres' \
GAME_V1_RUNTIME_EXPECTED_PORT=<random> \
pytest -q tests/integration/modules/runtime/test_runtime_checkpoint_transaction.py
24 passed

GAME_V1_IDENTITY_TEST_URL='postgresql+psycopg://<ephemeral-user>:<redacted>@127.0.0.1:<random>/postgres' \
GAME_V1_IDENTITY_EXPECTED_PORT=<random> \
pytest -q tests/integration/modules/runtime/test_game_v1_identity_migration.py
10 passed
```

## Acceptance Evidence

Root proof on 2026-07-21 additionally passed 170 combined runtime unit/regression
tests, both proto codegen drift checks, Ruff, Black, strict specs and Git diff
checks. Independent review confirmed stale replacement, COMMIT uncertainty and
Python set-collapse repairs; load storage errors are finite and rollback-safe.
