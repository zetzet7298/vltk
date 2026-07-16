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

- Pre-commit staged review: 170 files, including 103 exact-content renames from `stale-harness/` to `harness/`, 66 additions, and one `.gitignore` modification.
- `git diff --cached --check`: pass after removing four trailing blank lines in copied Harness docs.
- High-confidence secret scan over 170 staged files: 0 hits.
- Commit: `ea5825da6` (`chore: track harness and move port specs`).
- Push: `origin/dev` fast-forwarded from `399a6c09c` to `ea5825da6`.
- Ignored runtime artifacts remained uncommitted.
