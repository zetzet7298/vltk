# Agent Instructions

## Scope (READ FIRST)

This harness is scoped to **`harness-be`** only (path: `/var/www/vltk-mobile/harness-be`).

- The durable DB is **`harness-be/harness.db`** (repo-local). Do NOT set `HARNESS_DB`
  to point elsewhere; the CLI must use `$REPO_ROOT/harness.db` inside this folder.
- Always run the CLI as `./scripts/bin/harness-cli` from **inside `harness-be/`**.
- This is a **separate, unrelated harness** from the sibling `../harness/` directory.
  `../harness/` is the Rust development repo of the harness framework itself and has
  its own `harness.db`. Never read, write, query, or migrate `../harness/harness.db`
  from here, and never mix stories/decisions/backlog between the two.
- All docs, stories, decisions, and tooling referenced below live under
  `harness-be/` — resolve every relative path against this folder, not the repo root
  (`/var/www/vltk-mobile`) and not `../harness/`.

<!-- HARNESS:BEGIN -->
## Harness

This repo uses Harness. Before work, read:

- `README.md`
- `docs/HARNESS.md`
- `docs/FEATURE_INTAKE.md`
- `docs/ARCHITECTURE.md`
- `docs/CONTEXT_RULES.md`
- `scripts/bin/harness-cli query matrix` on macOS/Linux, or `.\scripts\bin\harness-cli.exe query matrix` on Windows

Use the Rust Harness CLI at `scripts/bin/harness-cli` on macOS/Linux or
`scripts/bin/harness-cli.exe` on Windows as the main operational tool.
<!-- HARNESS:END -->
