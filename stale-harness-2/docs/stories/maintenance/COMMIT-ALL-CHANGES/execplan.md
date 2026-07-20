# Exec Plan — Commit and push all working-tree changes

## Goal

Commit every non-ignored change currently present in `/var/www/vltk-mobile` and push `dev` to `origin/dev`.

## Scope

In scope:

- Root `.gitignore` modification.
- Move of 103 exact-content spec files from `stale-harness/` to `harness/`.
- All 165 non-ignored files under `harness/`.
- Commit and push to existing `origin/dev`.

Out of scope:

- Ignored runtime files: `harness/harness.db`, `harness/.harness/`, `harness/scripts/bin/`.
- Unity `Library/`, `Temp/`, cache, and other ignored outputs.
- Any new source changes beyond current working tree.

## Risk Classification

Risk flags:

- Data loss: tracked `stale-harness` paths are deleted as part of the move.
- External system: push to GitHub remote.
- Existing behavior.
- Weak proof.
- Multi-domain.

Hard gates:

- Do not commit secrets.
- Do not overwrite remote history; push only fast-forward to `origin/dev`.
- Stop if staged content differs from reviewed working-tree scope.

## Work Phases

1. Inspect branch, upstream, status, diff, file sizes, and secret patterns.
2. Confirm 103 deleted files have exact-content replacements under `harness/`.
3. Stage all non-ignored changes and review cached diff.
4. Commit with one descriptive commit.
5. Push `dev` to `origin/dev` without force.
6. Verify clean tree and local/remote SHA equality.

## Stop Conditions

Stop if remote diverged, secrets are found, or staged file set contains unexpected generated/runtime artifacts.
