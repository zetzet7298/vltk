# VLTK full-stack client↔backend Kanban kickoff

Use when the user asks to start connecting the Unity client to `/var/www/vltk-mobile/backend` while staying inside Harness/Kanban.

## Preconditions

- Root `AGENTS.md` must describe the project as full-stack and point backend work to `/var/www/vltk-mobile/backend/AGENTS.md`.
- Backend is a separate repo; do not stage it from the Unity repo.
- Backend readiness must be split into:
  - Phase 1 REST domain backend: account/role/player/map/item/skill/combat REST endpoints, pytest green.
  - Later realtime MMO server: sessions, server tick, AOI, entity snapshots, movement input/broadcast.

## Harness setup

Create a story before implementation, but do not mark proof green until real evidence exists:

```bash
cd /var/www/vltk-mobile/harness
scripts/bin/harness-cli story add \
  --id FS-01 \
  --title "Full-stack backend integration foundation" \
  --lane high-risk \
  --contract "Unity client can call the local VLTK FastAPI backend through a centralized backend abstraction without breaking existing mock/offline runtime. Backend stays authoritative for account/role/player/map/item/skill state; Unity owns render/input/UI." \
  --verify "true" \
  --notes "Tracking story only. Replace no-op verify before completion; do not set proof flags without backend pytest + Unity evidence."

scripts/bin/harness-cli backlog add \
  --title "Replace FS-01 no-op verify with real backend+Unity integration verifier" \
  --while "Creating full-stack client-backend integration story" \
  --pain "A no-op verify can create false green." \
  --suggestion "After BackendClient/Health/Map/Skill integration lands, set FS-01 verify to run backend pytest plus Unity tests/smoke." \
  --risk high-risk
```

## Kanban board and task graph

Use a separate board so client-backend work does not collide with PC-resource port cards:

```bash
cd /var/www/vltk-mobile
hermes kanban boards create vltk-fullstack-backend \
  --name "VLTK Full-Stack Backend Integration" \
  --description "Client↔backend integration work: FastAPI game-server authority, Unity REST client, Harness proof, then realtime gateway planning." \
  --default-workdir /var/www/vltk-mobile
export HERMES_KANBAN_BOARD=vltk-fullstack-backend
```

Recommended graph:

1. `FS-01A backend contract audit for Unity boot flow`
   - assignee: `vltkmobile-be`
   - workspace: `dir:/var/www/vltk-mobile/backend`
   - output: OpenAPI/endpoints needed for Unity boot/login/role/player/map/item/skill; missing endpoints; backend pytest results.

2. `FS-01B Unity backend client architecture discovery`
   - assignee: `vltk-fixer`
   - workspace: worktree branch
   - output: file-level plan for `MockBackend`/`RestBackend`, DTO placement, base URL config, first tests.

3. `FS-01C Harness story/proof design for client-backend integration`
   - assignee: `vltk-fixer2`
   - workspace: worktree branch
   - output: real proof gates and future verify command; no green flags.

4. `FS-01D implement minimal Unity REST health+map smoke`
   - parents: FS-01A/B/C
   - assignee: `vltk-fixer`
   - output: smallest Unity REST client slice with fake/mock HTTP boundary; current offline runtime preserved.

5. `FS-01E integration merge+Unity compile+backend smoke`
   - parent: FS-01D
   - assignee: `vltk-unity`
   - output: merged branch, backend health, Unity compile/tests/smoke, Harness evidence updated only with real outputs.

Dispatch only the three discovery parents initially:

```bash
hermes kanban dispatch --max 3
```

## Pitfalls

- `hermes kanban boards create --switch` may print a created board but the shell/env may still show the previous board in that process. Set `HERMES_KANBAN_BOARD=vltk-fullstack-backend` explicitly before `create/list/dispatch` commands.
- Do not broadly connect gameplay in the first implementation card. Prove the REST abstraction with health/map first.
- Do not mark FS-01 proof flags green while `verify_command` is `true` or any echo/no-op.
- Backend authority does not mean backend parity is complete; audit against PC Server 6.0/jx_linux_y before claiming behavior parity.
