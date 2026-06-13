# FS-01 — Execplan

## Phase 1 (this card and dependents)

| Card | Lane | Workspace | Output |
| --- | --- | --- | --- |
| FS-01A retry2 (`t_75a76d11`) | high-risk | `dir:/var/www/vltk-mobile/backend` | OpenAPI/endpoints, 722/722 pytest, missing endpoints, minimal Phase 1 surface. **DONE 2026-06-13.** |
| FS-01B retry2 (`t_05f271a4`) | normal | worktree branch | File-level plan for `IGameBackend`/`RestBackend`/`MockBackend`, DTOs, base URL config, first tests. (In progress on `fullstack/fs01b-unity-backend-discovery-retry2`.) |
| FS-01C retry2 (`t_4fbe26fd`) | high-risk | worktree branch | **This card.** Real proof gates for FS-01; no green flags. |
| FS-01D retry2/3 (`t_e2d3ccb3`, `t_c2a0417c`) | normal | worktree branch | Smallest Unity REST client slice (configurable base URL, health DTO, map list DTO + client call, tests via fake HTTP boundary). Parents: A + B + C. |
| FS-01E | high-risk | main workdir | Integration merge + Unity compile + backend smoke + Harness evidence update with real outputs. Parent: D. Assignee: `vltk-unity`. |

## Step-by-step

1. **FS-01A retry2 (DONE 2026-06-13, 15:43).** Confirmed backend 722/722 pytest, 86 active endpoints across 13 modules; minimal Phase 1 surface = `GET /health`, `POST /v1/account/login`, `GET /v1/role/by-account/{account}`, `GET /v1/player/by-role/{role_id}`, `POST /v1/map/enter`, `GET /v1/item/by-role/{role_id}`. Caveats: password hash expectation must be matched exactly (MD5 uppercase flagged), `/v1/skill/cast` is server-authoritative, encoding TCVN3/GB2312 must be normalized before display.

2. **FS-01B retry2 (in progress).** Plan where the REST client lives. Constraints from prior code: existing `INetworkClient` is binary protocol — keep it. New `IGameBackend` (HTTP) goes into `Assets/Scripts/Backend/` with a DTO folder, `RestBackend` (uses `UnityWebRequest`), `MockBackend` (returns canned responses), and a config struct (base URL, timeout, retry). Tests use a fake HTTP boundary (interface) so `RestBackend` is testable without a live server.

3. **FS-01C retry2 (this card).** Design the proof ladder. Output: `proof-gates.md` + `verify-fs01.sh`. Do **not** set FS-01 flags to `1`; do **not** call `harness-cli story update` to set `--unit/integration/e2e/platform` green. Provide the exact `harness-cli` story-update commands for the integration worker to run **after** every gate produces a real artifact.

4. **FS-01D retry2/3 (next, depends on A+B+C).** Implement the smallest Unity REST slice. Preserve current offline runtime. Add tests using the fake HTTP boundary. Commit on a worktree branch (`fullstack/fs01d-unity-rest-health-map-retryN`), summarize exact files/tests. Hand off as `review-required:` for integration lane.

5. **FS-01E (last, depends on D).** Integration worker merges D's branch into `dev`, recompiles in the live Editor with `VLTK_ENABLE_TESTS` defined, runs the focused EditMode suite, runs the PlayMode test or captures the manual artifact, runs the `verify-fs01.sh` script, then applies the documented `harness-cli story update` commands with the real artifact paths. Only then do flags flip to `1` for the layers that actually have a passing artifact.

## Lane classification rationale

- FS-01 = high-risk: auth, contract, audit, multi-domain, cross-platform all fire at once.
- FS-01A = normal/audit: read-only review, no code change.
- FS-01B = normal: plan + scaffold, no runtime code.
- FS-01C = high-risk (this card): the proof ladder itself changes what "done" means for every later integration card. Wrong design = false green for the whole FS-01 epic.
- FS-01D = normal: smallest vertical slice, fake HTTP boundary, preserve offline.
- FS-01E = high-risk: integration + first time the real `verify_command` is run. Errors here mean the entire proof ladder is suspect and must be re-validated.
