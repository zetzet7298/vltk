---
name: vltk-port-status-audit
description: Audit whether VLTK-mobile's reported port/completion status is TRUE, by reading code, data, and the Harness DB instead of trusting prose. Use when the user asks to audit/verify port status, check if "implemented"/"✅"/"pass" is real, reconcile the Harness matrix with docs/PORT_STATUS.md, confirm catalog row counts against PC Reference files, or suspects status is inflated or "khống". Covers the recurring traps that make a green status hollow — no-op verify_command, test asmdefs gated by an undefined define symbol, stale pak/manifest counts, and evidence pointers into dormant code. Also use before trusting PORT_STATUS for a high-stakes decision.
---

# VLTK port-status audit

Use when asked to audit/verify the **project port status** (not to port a feature — that's
`jx-pc-port-rule`; not to audit the skills themselves — that's `jx-pc-port-rule/references/skill-audit-playbook.md`).

Governing principle: **a status is a claim, not proof. "implemented", "✅", and "pass" mean
nothing until you trace each one to code/data/test that actually ran.** Every inflation found in
the 2026-06-12 audit was prose/metadata contradicting the real repo.

## Where truth lives (priority order)

1. `docs/PORT_STATUS.md` — the project's status authority. Generally honest (uses `🔄`/`☐`,
   warns against reading the matrix as completion), but its baked counts and paths drift.
2. PC Reference data: `Assets/StreamingAssets/Reference/**` (GBK/Chinese; many `*.txt` tables,
   some generated `*.json` catalogs).
3. The unpack manifest `/var/www/vltksource_new/pak_unpacked/_unpack_summary.json`
   — read live, never trust a baked count.
4. Harness DB `harness/harness.db` via `scripts/bin/harness-cli` — a story-slice/test matrix,
   **NOT** a port-completion measure. It is gitignored; backlog/notes live locally.

When (1) and (4) disagree, PORT_STATUS is the completion authority; the Harness matrix is just
which story slices were touched.

## The five checks (run all; each is fast, all read-only)

### 1. Harness matrix is not port-completion
```bash
cd <repo>/harness
./scripts/bin/harness-cli query matrix 2>&1 | grep -cE '^(ST|US)-'      # row count
./scripts/bin/harness-cli query matrix 2>&1 | grep -oE 'implemented|in_progress|planned' | sort | uniq -c
./scripts/bin/harness-cli audit                                          # internal drift (often 0 — misleading)
```
All-`implemented` + entropy 0 does NOT mean ported. The internal audit only checks its own
graph, not reality.

### 2. verify_command is the real tell (THE big trap)
```bash
./scripts/bin/harness-cli query sql "select distinct verify_command, count(*) from story group by verify_command"
./scripts/bin/harness-cli query sql "select id, coalesce(last_verified_result,'NEVER') from story order by id"
```
`last_verified_result='pass'` is worthless if `verify_command` is `true` or `echo '...verified...'`
or empty. A no-op command exits 0, so `pass` only proves the echo ran. Treat any story whose
verify_command is `true`/`echo`/NULL as **unverified**, regardless of status flag.

### 3. Do the test suites actually compile/run? (second big trap)
```bash
rg --files --no-ignore Assets/Tests | rg '\.asmdef$'
# inspect defineConstraints on each test asmdef:
cat Assets/Tests/EditMode/*.asmdef | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('defineConstraints'))"
# is that symbol actually defined anywhere?
rg -n 'scriptingDefineSymbols' ProjectSettings/ProjectSettings.asset
rg -rn 'YOUR_SYMBOL' --no-ignore ProjectSettings/ Packages/ Assets/ -g'!*.asmdef'
```
Trap seen: `VLTK.Tests.EditMode`/`PlayMode` are gated by `defineConstraints: ["VLTK_ENABLE_TESTS"]`,
and `VLTK_ENABLE_TESTS` was defined **nowhere** (empty `scriptingDefineSymbols`, no `csc.rsp`).
Result: ~2,281 EditMode test attrs never compile, never run. Only `Assets/Tests/PortFactorySmoke/`
(gated by the always-on `UNITY_INCLUDE_TESTS`) executes. So any evidence pointer into the gated
suite references dormant code, not a passing test. Always confirm the gating symbol is defined
before believing a test-backed `✅`.

### 4. Catalog counts vs real Reference files (fan-out friendly)
Counts in PORT_STATUS are usually accurate but paths/wording drift. Verify by re-counting the
actual file. This is the part to **fan out to subagents** by domain (skills, items, maps, npc,
events) — each gets the claim list and reports `claim | file_found | claimed | actual | MATCH/MISMATCH`.
Counting method that worked: `tr -d '\r' < f | rg -c '\S'` then subtract header rows; for `*.json`
catalogs the first array element is often a GBK header row (so `len-1` = data rows); for `.ini`
separate `[section]` count from `key=value` data-line count; sum weight columns with `awk -F'\t'`.
Note: `fd` is blocked by .gitignore under StreamingAssets — use `rg --files --no-ignore`.

### 5. pak/manifest counts are live, not baked
```bash
python3 -c "import json;m=json.load(open('/var/www/vltksource_new/pak_unpacked/_unpack_summary.json'));print(m['pak_count'],m['total_entries'],m.get('total_exported'),m['total_failed'])"
```
Stale snapshots (e.g. "401,281/401,640, 357 undecoded 0x11000000") recur in docs. Live truth:
46 paks, 403560/403560, 0 failed, 0 partial. `0x11000000` is raw-SPR byte-copy (stored, not
undecoded); the 5 `dmjx01` `0x10000000` entries are a repaired fragment-table. For the binary
proof behind these numbers see `jx-pc-port-rule/references/pak-format-internals.md`.

## Fix discipline

- Patch `docs/PORT_STATUS.md` for stale counts/paths and add a dated "Harness matrix vs reality"
  section spelling out what is hollow (don't silently flip statuses — that's a big decision).
- Log a Harness backlog item (`harness-cli backlog add`) describing the verification gap + the fix
  (define the symbol, run the real suite, replace no-op verify_command). The DB is gitignored, so
  the backlog is the durable in-DB record.
- `harness.db` has no git identity by default in fresh sibling repos; `git config user.email/name`
  locally before committing in repos like `~/Projects/vltktool` if a commit fails with "Author identity unknown".
- Don't mark a row `✅`/runtime until: define symbol set, real EditMode/PlayMode run via Unity Test
  Runner, artifact captured, and `verify_command` actually executes that suite.

## What "good" looks like

Data layer (catalog counts) can be 100% accurate while the verification layer is hollow. Report
the two separately: "counts MATCH across all domains; verification is no-op / suite gated off."
That distinction is the whole point — it tells the user the port data is sound but the *proof* isn't.

## Backend readiness reviews

When auditing whether the Python backend can be connected to Unity, separate **REST domain backend readiness** from **realtime MMO server readiness**. A FastAPI backend with account/role/player/item/skill/map/combat REST endpoints and green unit/integration/E2E tests is enough to start Phase 1 client integration, but it is not a complete realtime game server until websocket/UDP session handling, server ticks, AOI/interest management, server-owned entities, movement input, and snapshot broadcast exist and are tested. See `references/backend-readiness-review.md` for the concrete review commands, test evidence to collect, and phase decision checklist.

For the Harness + Kanban setup pattern used to start Unity↔backend Phase 1 (FS-01 tracking story, `vltk-fullstack-backend` board, fan-out/fan-in task graph, and worker-spawn pitfalls), see `references/fullstack-backend-integration-kanban.md`. The **pinned backend auth contract** (FS-02A: `accName` not `account`, `password` plaintext not MD5-uppercase, MD5-IN-HOA storage parity, no bearer/JWT) lives in `references/fs02-auth-contract.md` — read it before writing any Unity login code or any audit that touches `/v1/account/*`.

## Full-stack client↔backend Kanban kickoff

For VLTK full-stack integration, use a separate Kanban board rather than mixing with the existing PC-resource port board. Recommended shape:

1. Add a Harness tracking story first (example `FS-01`) and keep all proof flags red until real backend + Unity test artifacts exist. If the initial `verify_command` is a placeholder/no-op, immediately add a backlog item to replace it with a real verifier.
2. Create a board like `vltk-fullstack-backend` with default workdir `/var/www/vltk-mobile`.
3. Fan out three independent discovery cards:
   - backend contract audit (`vltkmobile-be`, `dir:/var/www/vltk-mobile/backend`);
   - Unity backend-client architecture discovery (`vltk-fixer`, worktree branch);
   - Harness proof/verify-command design (`vltk-fixer2`, worktree branch).
4. Gate implementation on those parents: minimal Unity REST health+map smoke first, preserving offline/mock runtime.
5. Gate integration on implementation: `vltk-unity` merges, starts/checks backend health, refreshes Unity, runs scoped EditMode/PlayMode evidence, updates Harness only with real outputs.

Do not connect gameplay broadly before the health/map slice proves the client abstraction and verifier path. This keeps backend authority introduction reversible and prevents false-green Harness rows.

## PlayMode runtime verification is now available

As of 2026-06-13, the game runs in PlayMode with full boot (profile=Full):
- EditMode suite: **2291/2291 passed** (0 failures)
- PlayMode: terrain renders (618 regions map 53), 812 enemies spawn, HUD works, player visual + mount functional
- Key enabler: `useFastEditorBoot = false` in `SandboxManager.cs` (was `true`, which silently skipped terrain/NPCs/items)

When auditing runtime claims, you can now verify in PlayMode directly:
1. `manage_editor(action="play")` → wait ~31s for full boot
2. `read_console` → check for `[MapRenderer] Rendered N regions` and `[MapEnemy] spawned N enemies`
3. `manage_camera(action="screenshot", include_image=True)` → visual proof
4. `vision_analyze` → verify terrain, NPC visibility, UI state

The FastEditor boot pitfall: if console shows `[SandboxBoot] FastEditor: skipped map visual rendering`, the game booted in FastEditor mode (no terrain, no NPCs, no items). Fix by ensuring `useFastEditorBoot = false`.

For the full boot log and screenshot evidence, see `unity-mcp-orchestrator/references/playmode-boot-verification.md`.

## After un-gating: driving the RED suite down without faking green

Once you (or a prior session) define the gating symbol and the suite actually runs, the audit turns
into a remediation sweep: classify each real failure TEST-DRIFT vs PROD-GAP, fix the tractable ones,
leave model-level ones honestly red with a backlog entry. The full playbook —
including the **deletion/mutation test** for empirically disproving a confident-but-wrong triage
(the single highest-leverage move), the rule to treat subagent triage as hypothesis not fact,
recurring ground-truth lookups (npcres man/woman tags, TCVN3-vs-GBK column shift, phantom ids), and
the Harness backlog `sqlite3` column/CHECK-constraint gotchas — is in
`references/failure-triage-doctrine.md`. Read it before starting any "drive the failures down" pass.
