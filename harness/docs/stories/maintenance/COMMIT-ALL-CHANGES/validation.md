# Validation — Commit and push all working-tree changes

## Proof Strategy

Confirm reviewed staged set, no high-confidence secret hits, commit success, fast-forward push, and local/remote SHA equality.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Not applicable |
| Integration | Staged diff review and `git diff --cached --check` |
| E2E | Push to `origin/dev` |
| Platform | Clean working tree after push |
| Logs/Audit | Harness trace with commit and push evidence |

## Fixtures

None.

## Commands

```bash
git diff --cached --check
git push origin dev
git rev-parse HEAD
 git rev-parse origin/dev
git status --porcelain
```

## Acceptance Evidence

Pending commit and push.
