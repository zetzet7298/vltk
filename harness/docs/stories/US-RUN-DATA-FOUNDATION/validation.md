# US-RUN-DATA-FOUNDATION Validation

## Proof Strategy

Upgrade and rollback must run on a disposable PostgreSQL clone and preserve
legacy rows while enforcing target UUID/realm/RLS constraints.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Mapping, duplicate, missing realm/password inputs. |
| Integration | Alembic upgrade/rollback/RLS on disposable PostgreSQL. |
| E2E | Deferred. |
| Platform | Migration runbook and isolation guard. |
| Performance | Backfill batch bounds. |
| Logs/Audit | Counts and rejected rows without secrets. |

## Fixtures

- Isolated legacy account/role rows including duplicates and invalid encodings.

## Commands

```text
Blocked until a disposable PostgreSQL URL is proven.
```

## Acceptance Evidence

Pending implementation and isolated database proof.
