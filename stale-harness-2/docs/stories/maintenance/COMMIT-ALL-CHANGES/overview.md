# Overview — Commit and push all working-tree changes

## Current Behavior

`dev` has one root ignore-rule edit, 103 deleted files under `stale-harness`, and 165 non-ignored untracked files under `harness`.

## Target Behavior

All intended non-ignored changes are committed in `dev` and pushed fast-forward to `origin/dev`; ignored runtime artifacts remain local.

## Affected Users

- Human and agents using project branch `dev`.

## Affected Product Docs

- `harness/specs/jx-pc-mobile-port/*` becomes canonical tracked spec location.

## Non-Goals

- No force push.
- No commit of secrets, databases, binaries, caches, or Unity generated output.
