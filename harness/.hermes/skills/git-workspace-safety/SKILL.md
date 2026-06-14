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

1. **Detect embedded repos** (the gitlink trap) without accidentally staging them:
   ```bash
   # Dry-run first; do not use plain `git add -A` just to look for warnings.
   git add -A -n 2>&1 | grep -i "embedded git repository" || true
   # Proactively list nested repos/worktrees:
   /usr/bin/find . -maxdepth 4 -name .git -not -path './.git' -print 2>/dev/null
   ```
   If a subdir has its own `.git`, staging it from the parent creates a **gitlink** — the parent
   records only a commit hash, NOT the files. A clone of the parent gets an empty directory. This
   silently "loses" the nested repo's code from the parent's perspective. **Un-stage it:**
   ```bash
   git rm --cached <subdir> >/dev/null 2>&1; git reset -q <subdir>
   ```
   After staging, verify no gitlink is present by checking the staged raw modes exactly:
   ```bash
   if git diff --cached --raw | awk '$1 ~ /^:160000/ || $2 == "160000" {bad=1; print} END{exit bad?0:1}'; then
     echo 'BLOCKED: gitlink staged' >&2
     exit 1
   fi
   ```
   Do not use a loose condition like `$2 ~ /^160000/` against the raw diff without understanding
   the field layout; it can misclassify normal `100644` file edits as blocked. The nested repo must
   be committed in ITS OWN repository, never as a gitlink in the parent.

2. **Block secrets**: confirm nothing sensitive is staged.
   ```bash
   git diff --cached --name-only | grep -iE '\.env|secret|credential|\.key|\.pem|\.db$'
   ```
   `.env` lives in `backend/`; a per-repo `.gitignore` only protects when committing from inside
   that repo — staging from the parent can bypass it. Verify, don't assume.

3. **Don't commit local DBs**: `harness.db` / `*.db-wal` / `*.db-shm` are gitignored on purpose
   (durable backlog lives in them locally). Confirm they're not in the staged set.

4. **Untrack large generated assets/directories (e.g., html/ static galleries)**:
   When generating HTML galleries, asset dumps, or local visual audit tools, they should typically be ignored to keep the repository clean.
   - Add the target folder (e.g., `html/`) to `.gitignore`.
   - If the folder was already tracked/committed, run `git rm -r --cached <dir>` to remove it from Git's index while preserving the files locally on disk.
   - Commit the deletion from tracking alongside the updated `.gitignore` to prevent future agents/runs from staging those files.

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

## Emergency recovery of deleted files via state.db

If an untracked file (such as a user-uploaded requirement file, a plan, or code) got deleted by a destructive git command (e.g. `git reset --hard` or `git checkout`), you can recover its exact content from the Hermes persistent SQLite state database!

Hermes caches all message payloads (including tool invocation inputs and outputs) in `state.db`. If you recently read the file using `read_file` or processed it, the entire content is stored in the database.

### Recovery script (Python)

Run a Python script to search for the deleted file path in the `messages` table of the active profile's `state.db` and write its contents back to disk:

```python
import sqlite3
import json

db_path = "/home/zet/.hermes/profiles/vltkmobile/state.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Search for the target filename or contents in the messages table
cursor.execute("SELECT id, content FROM messages WHERE content LIKE '%target_file_name%'")
rows = cursor.fetchall()
for row_id, content_json in rows:
    try:
        data = json.loads(content_json)
        if "content" in data and "target marker" in data["content"]:
            print(f"Found match in row {row_id}!")
            with open("/var/www/vltk-mobile/path/to/file", "w") as f:
                f.write(data["content"])
            break
    except Exception as e:
        continue
```

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

## Kanban multi-branch merge: C# duplicate-method trap

When the orchestrator merges multiple feature branches that each modify the same backend
interface files (`IGameBackend.cs`, `BackendClient.cs`, `RestGameBackend.cs`,
`MockGameBackend.cs`), a naive conflict resolution using `git checkout --theirs` /
`git checkout --ours` **creates duplicate method definitions**.

**Symptom:** Unity compile errors:
- `CS0111: Type already defines a member called 'X' with the same parameter types`
- `CS0102: Type already contains a definition for 'X'`
- `CS0535: Type does not implement interface member 'X'`

**Root cause:** Each branch adds methods to the same interface and implementation. Blind
`--theirs` takes ALL of theirs (including duplicate shared helpers) while dropping ours.

**Fix — manual union merge:**
1. Do NOT use `git checkout --theirs/--ours` for C# files with method definitions.
2. Open each conflicted file. For each conflict marker: keep methods from BOTH sides.
   Shared private helpers (`ExecuteAsync`, `ParseBody`) — keep ONE copy.
3. Verify method count: `grep -c 'public Task.*Async' IGameBackend.cs` should equal the sum.
4. Verify no markers: `grep -rn '<<<<<<<' .` must return nothing.
5. `refresh_unity` + `read_console(types=["error"])` to confirm 0 compile errors.

**Example (FS-03E, 2026-06-13):** RestGameBackend.cs was 854 lines with every method
duplicated. Fix: kept lines 1-502, deleted 503-854, added 3 missing auth methods.
IGameBackend.cs needed 3 combat interface declarations added. Result: 0 compile errors.

## Multi-repo push discipline

When the user says "commit all change và push" in the VLTK workspace, check all known
sibling/nested repos, not only the current repo:

```bash
git -C /var/www/vltk-mobile status --short --branch
git -C /var/www/vltk-mobile/backend status --short --branch
git -C /var/www/vltk-mobile/backend/cores status --short --branch
git -C /var/www/vltk-mobile/harness-be status --short --branch
```

- Commit dirty work in each repo separately, respecting that repo's own `AGENTS.md` and `.gitignore`.
- If a nested repo is clean but `ahead N`, push it too when the user requested "push all".
- Never add `/var/www/vltk-mobile/backend`, `backend/cores`, or `harness-be` from the parent repo.
