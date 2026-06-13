# Full-stack backend integration via Harness + Kanban

Use when starting or auditing VLTK Mobile Unity↔backend integration. This is session-specific operational knowledge distilled from the first FS-01 setup.

## Harness shape

Create a tracking story before implementation, but do not mark proof flags green until real artifacts exist:

```bash
cd /var/www/vltk-mobile/harness
scripts/bin/harness-cli story add \
  --id FS-01 \
  --title "Full-stack backend integration foundation" \
  --lane high-risk \
  --contract "Unity client can call the local VLTK FastAPI backend through a centralized backend abstraction without breaking existing mock/offline runtime. Backend stays authoritative for account/role/player/map/item/skill state; Unity owns render/input/UI." \
  --verify "true" \
  --notes "Tracking story only. Replace no-op verify before completion."

scripts/bin/harness-cli backlog add \
  --title "Replace FS-01 no-op verify with real backend+Unity integration verifier" \
  --risk high-risk \
  --pain "A tracking story exists before the verifier; no-op verify must not become proof." \
  --suggestion "Set verify to backend pytest + Unity EditMode/PlayMode integration tests once the client slice lands."
```

## Kanban graph that worked

Use a dedicated board, separate from pure PC-port work:

```bash
hermes kanban boards create vltk-fullstack-backend \
  --name "VLTK Full-Stack Backend Integration" \
  --default-workdir /var/www/vltk-mobile
export HERMES_KANBAN_BOARD=vltk-fullstack-backend
```

Decompose Phase 1 as fan-out/fan-in:

1. **Backend contract audit** (`/var/www/vltk-mobile/backend`): endpoint/OpenAPI inventory, backend pytest, missing endpoints.
2. **Unity architecture discovery** (worktree): where to place `IGameBackend`/`RestBackend`/`MockBackend`, DTOs, config, tests.
3. **Harness proof design** (worktree): exact proof gates and verify command plan; do not set flags green.
4. **Implementation** (worktree): minimal REST health + map smoke, fake HTTP boundary tests, preserve offline runtime. Parents: 1+2+3.
5. **Integration** (`vltk-unity`/main workdir): merge, backend health, Unity compile/tests, update evidence with real outputs. Parent: 4.

## Important worker pitfall

Do not blindly trust dashboard `running`. In the first run:

- cards showed `running` although logs contained only `hermes-tui: no TTY` or `Error: Unknown skill(s)`;
- PIDs were already gone;
- dependencies would have waited forever.

Before telling the user workers are active, check logs:

```bash
export HERMES_KANBAN_BOARD=vltk-fullstack-backend
hermes kanban list
for t in <ids>; do
  hermes kanban runs "$t"
  hermes kanban log "$t" | tail -120
  hermes kanban show "$t" | sed -n '1,120p'
done
```

If failed:

```bash
hermes kanban reclaim <task> --reason "worker process exited/failed but task stayed running"
hermes kanban block <task> "reclaimed by orchestrator: spawn failed before useful work; superseded by retry card"
```

Then create a fresh retry card. If `--skill` preload caused `Unknown skill(s)`, omit `--skill` and put in the body: "read AGENTS.md and load/use relevant skills available in-session." If a backend profile launches TUI in Kanban, use another headless profile or set `use_tui: false` in the actual profile config path printed by `hermes -p <profile> config path`.

## Branch cut timing and the FS-02 evidence merge trap

When the same orchestrator session is running fan-out cards on `dev` while
the integrator card is also being prepared, you can hit this exact failure
mode (seen 2026-06-13 with FS-02A + CTS-01/06):

- Worker A lands commit `c5a34d73b` on `dev` that adds
  `harness/docs/fs02-evidence-2026-06-13/{01..13}_*.json` +
  `contract.md` + `smoke_test.sh` + `openapi.json` + 2 pytest logs.
- Worker B's branch was cut from an earlier `dev` HEAD *before* A landed.
  When the integrator runs `git diff dev..B`, the stat shows ~14 deletes
  for the FS-02 evidence files — even though B is a test-only change and
  has no opinion about those files.
- A naive `git merge --no-ff` deletes the FS-02 evidence.

**Fix for the integrator card (FS-02E / CTS-07 / any "merge N branches"):**

```bash
# 1. Rebase each branch onto current dev first (preferred).
git checkout fullstack/<branch> && git rebase dev
# resolve any conflict, repeat for each branch

# 2. Or merge with -X ours for evidence-only conflicts:
git merge --no-ff -X ours fullstack/<branch>
# Then verify the FS-02 evidence still on disk:
ls /var/www/vltk-mobile/harness/docs/fs02-evidence-2026-06-13/
git status --short --branch
```

**Fix for the orchestrator card that approves B**: when closing a
`blocked` task whose branch was cut from a stale `dev`, ALWAYS add a
comment instructing the integrator about the rebase / `-X ours` need.
Example comment (from CTS-06 closure 2026-06-13):

> **Integrator note**: branch was cut before FS-02A evidence (`c5a34d73b`)
> landed; the diff against `dev` will show deletes on
> `harness/docs/fs02-evidence-2026-06-13/*`. CTS-07 must rebase onto
> `dev` first or use `git merge --no-ff -X ours` to preserve FS-02 evidence.

Don't rely on the worker to remember — the orchestrator owns the
integration handoff.

## Backend contract result from first audit

Backend Phase 1 is viable. Backend audit reported **722/722 tests passed** and recommended minimal initial surface:

- `GET /health`
- `POST /v1/account/login`
- `GET /v1/role/by-account/{account}`
- `GET /v1/player/by-role/{role_id}`
- `POST /v1/map/enter`
- `GET /v1/item/by-role/{role_id}`

Caveats — these were **guesses** in FS-01A, now superseded by the FS-02A pinned contract (see `references/fs02-auth-contract.md`):

- ~~Unity login should match backend password expectation (MD5 uppercase was flagged by audit; verify exact schema before coding).~~ **WRONG.** Real contract: `POST /v1/account/login` body is `{accName, password, otp?, clientIp?}` with `extra=forbid`. `password` is **PLAINTEXT** on the wire; server hashes to `hashlib.md5(p.encode("utf-8")).hexdigest().upper()` for storage parity with PC `account_tong.cPassWord varchar(32)`. Pre-hashing on the client returns 401. No Authorization header, no bearer, no JWT in FS-02; `accName` is the session id. Test: `backend/tests/integration/modules/account/test_fs02a_auth_contract.py`.
- `/v1/skill/cast` is server-authoritative; client may predict UI but must reconcile backend result.
- Encoding remains a risk: TCVN3/GB2312 mojibake can leak into names if backend strings are displayed directly.
