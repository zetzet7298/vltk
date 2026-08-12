# US-RUN-WSS-VERTICAL Validation

## Proof Strategy

Fresh contract/integration proof must exercise the mounted binary WSS path through
authoritative mutation and durable result/snapshot.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Admission/ordering, boundary mapping, missing-resource fail-closed, replacement/drain fence. |
| Integration | Binary WSS → runtime → PostgreSQL checkpoint/idempotency/outbox → ACK/resources snapshot. |
| E2E | Injected fake client passes; Unity canary remains follow-up. |
| Platform | Same-process higher-epoch replacement, resync freshness and shutdown drain pass; restart resume/HA remain follow-up. |
| Performance | Frame limits are enforced; sustained 18 Hz/catch-up benchmark remains follow-up. |
| Logs/Audit | No raw ticket/frame is emitted; correlated metrics/logs remain follow-up. |

## Fixtures

- Disposable PostgreSQL, deterministic clock/content/RNG and binary envelopes.

## Commands

```text
cd /var/www/vltk-mobile/backend
pytest -q tests/unit/modules/runtime
GAME_V1_RUNTIME_TEST_URL=<guarded-disposable-url> GAME_V1_RUNTIME_EXPECTED_PORT=<non-default-port> pytest -q tests/integration/modules/runtime/test_game_v1_wss_vertical.py tests/integration/modules/runtime/test_runtime_checkpoint_transaction.py
GAME_V1_IDENTITY_TEST_URL=<guarded-disposable-url> GAME_V1_IDENTITY_EXPECTED_PORT=<non-default-port> pytest -q tests/integration/modules/runtime/test_game_v1_identity_migration.py
ruff check app/main.py app/modules/runtime tests/unit/modules/runtime/test_runtime_uow.py tests/unit/modules/runtime/test_wss_admission.py tests/integration/modules/runtime/test_game_v1_wss_vertical.py
black --check app/main.py app/modules/runtime tests/unit/modules/runtime/test_runtime_uow.py tests/unit/modules/runtime/test_wss_admission.py tests/integration/modules/runtime/test_game_v1_wss_vertical.py
python specs/scripts/validate.py --strict
git diff --check
```

## Acceptance Evidence

Fresh 2026-07-21 proof:

- 181 runtime unit tests passed.
- 31 disposable-PostgreSQL WSS/checkpoint tests passed.
- 10 disposable-PostgreSQL identity tests passed.
- Real WSS proof verifies visible rows after ACK, exact replay row stability,
  altered-frame conflict before second combat, `COMMIT_UNKNOWN` no success with
  committed rows, and pre-commit rollback with zero row delta.
- Ruff, Black, strict spec validation and diff check passed.
- Fresh Herdr regression review reported no remaining P0/P1 for the bounded
  single-process vertical.
