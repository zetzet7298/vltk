---
name: git-workspace-safety
description: Safely commit/push in the VLTK multi-repo workspace where several git repos are nested or sit side-by-side (vltk-mobile client, backend game server, harness-be, harness, vltktool). Use whenever the user says "commit all", "commit và push", "commit everything", or you are about to run `git add -A` / `git add .` in a directory that may contain another repo or secrets. Covers the embedded-repo gitlink trap, secret leakage, and the bundle-backup move that makes any risky repo operation reversible. The user's hard rule is "tránh làm mất code" — treat code loss as the one unforgivable outcome.
---

# Git workspace safety (VLTK multi-repo)

The `/var/www/vltk-mobile` tree is NOT a single repo. It contains/neighbours several independent
git repos: `vltk-mobile` (Unity client, the outer repo), `backend/` (Python game server, its own
`.git` + `.env`), `harness-be/` (a plain dir, already committed into the client), `harness/`, and
`/var/www/vltktool`. AGENTS.md mandates "commit all change + push" after every task — so a blind
`git add -A` here is genuinely dangerous. Before any bulk commit, run the checks below.

User's governing rule: **"tránh làm mất code"** — never take an action that could lose a repo's
history or contents. Reversibility first.

## Pre-commit checklist (run before `git add -A` / "commit all")

1. **Detect embedded repos** (the gitlink trap):
   ```bash
   git add -A 2>&1 | grep -i "embedded git repository"   # git warns if you stage a nested repo
   # or proactively:
   find . -name .git -maxdepth 3 -not -path './.git' 2>/dev/null
   ```
   If a subdir has its own `.git`, staging it from the parent creates a **gitlink** — the parent
   records only a commit hash, NOT the files. A clone of the parent gets an empty directory. This
   silently "loses" the nested repo's code from the parent's perspective. **Un-stage it:**
   ```bash
   git rm --cached <subdir> >/dev/null 2>&1; git reset -q <subdir>
   ```
   The nested repo must be committed in ITS OWN repository, never as a gitlink in the parent.

2. **Block secrets**: confirm nothing sensitive is staged.
   ```bash
   git diff --cached --name-only | grep -iE '\.env|secret|credential|\.key|\.pem|\.db$'
   ```
   `.env` lives in `backend/`; a per-repo `.gitignore` only protects when committing from inside
   that repo — staging from the parent can bypass it. Verify, don't assume.

3. **Don't commit local DBs**: `harness.db` / `*.db-wal` / `*.db-shm` are gitignored on purpose
   (durable backlog lives in them locally). Confirm they're not in the staged set.

## Bundle-backup: the zero-risk move before ANY risky repo operation

Before re-homing a repo, rewriting history, changing remotes, or any operation you're unsure about,
snapshot the FULL history into a single file. It touches nothing in the working repo and restores
100%:
```bash
mkdir -p /var/www/_repo_backups
git -C <repo> bundle create /var/www/_repo_backups/<name>-$(date +%Y%m%d-%H%M%S).bundle --all
git bundle verify /var/www/_repo_backups/<name>-*.bundle   # must say "records a complete history"
# restore later (if needed):  git clone /var/www/_repo_backups/<name>-….bundle restored/
```
A repo with **no remote** (e.g. `backend/` had 11 commits and zero remotes) has no off-machine
backup at all — bundle it immediately, then sort out the remote. A 221 MB working dir is usually
venv/log/data behind `.gitignore`; the bundle of real source history is tiny (hundreds of KB).

## gh CLI vs SSH (auth reality in this workspace)

- SSH to GitHub works here (`git@github.com:zetzet7298/vltk.git` pushes fine) — so pushing to an
  **existing** repo needs no extra auth.
- Creating a **new** GitHub repo needs the gh API, which needs a token. `gh` is often NOT logged in
  (`gh auth status` → "not logged into any GitHub hosts"). SSH auth does NOT satisfy gh. To create
  a repo you must either `export GH_TOKEN=…` (scope `repo`) or have the user create the empty repo
  and hand you the SSH URL, then you `git remote add` + `git push -u`.
- Don't guess remote names/URLs or invent a repo — ask which name and whether to gh-create vs
  user-creates. Re-homing a server repo and merging harness dirs are topology decisions; confirm
  the target layout with the user before moving anything.

## Commit message discipline

When the bulk commit legitimately spans unrelated areas (it shouldn't, but "commit all" forces it),
list each area in the body and explicitly note what you deliberately LEFT OUT and why (e.g.
"backend/ is a separate repo with its own .env and is intentionally left untracked — must be
committed in its own repository, not as a gitlink").
