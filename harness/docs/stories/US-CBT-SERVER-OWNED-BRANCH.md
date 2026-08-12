# US-CBT-SERVER-OWNED-BRANCH Port one server-owned combat branch

## Status

implemented

## Lane

normal

## Product Contract

Select and port exactly one bounded behavior from the canonical PC
`KNpc::CalcDamage` or `KNpc::ProcessState` path whose required state is already
owned by the backend server. Preserve the current PC ordering, integer/time/RNG
semantics and engine dependencies that can be proven from exact source. Do not
add request fields that let a client author combat state, and do not claim full
combat parity from this slice.

## Relevant Product Docs

- `/var/www/vltk-mobile/backend/specs/domains/p1-skill-combat.md`
- `/var/www/vltk-mobile/backend/specs/06-gap-checklist.md`
- `/var/www/vltk-mobile/backend/specs/07-provenance.md`
- `/var/www/vltk-mobile/backend/specs/08-acceptance-audit.md`

## Acceptance Criteria

- Fresh read-only PC, backend, and proof lanes identify one exact branch whose
  source order, dependencies, units and state ownership are proven; otherwise
  the story remains open with one precise blocker.
- The chosen branch records current PC revision, exact path, SHA-256,
  function/line mapping, relevant caller/dependency evidence and unresolved
  engine semantics.
- Backend behavior is wired through a real domain/application call path using
  server-owned state, with no new client-authoritative combat fields.
- Executable focused tests cover the canonical boundary and ordering, including
  a negative or no-op case, and invoke the real implementation.
- Remaining `CalcDamage`/`ProcessState` branches stay explicit gaps; no aggregate
  parity, typed REST transport, or Unity representation claim is introduced.

## Design Notes

- Commands: one bounded combat domain/application mutation selected after
  evidence collection.
- Queries: existing server-owned combat state only.
- API: no endpoint, request DTO, response envelope, or OpenAPI shape change.
- Tables: none; do not run shared PostgreSQL integration/e2e suites.
- Domain rules: preserve exact PC ordering and bounded integer/time/RNG semantics;
  leave unproven engine globals as gaps.
- UI surfaces: Not applicable.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Focused combat domain and application/service cases for the selected branch. |
| Integration | Not required unless a disposable isolated PostgreSQL environment is first proven. |
| E2E | Not applicable to this bounded domain wave. |
| Platform | Not applicable. |
| Release | `srcwalk review`, changed-scope Ruff/Black, combat unit tests, and strict specs validation. |

## Harness Delta

Intake #7 and this story track the bounded delivery wave. Herdr run
`orch-af8b569f8cc3db42` collected three fresh read-only discovery panes before
granting one non-overlapping writer scope.

## Evidence

- Discovery run `orch-31a5f09ec965eec4` collected three clean read-only lanes
  and was cancelled without a writer because no candidate met the story
  acceptance. PC parent HEAD is `7b24ff93702c7eeb272ecb63716baba579975434`,
  while the nested canonical source repo remains
  `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`; `KNpc.cpp` still hashes
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`.
- Parent `US-RUN-COMBAT-AUTHORITY` is implemented at backend commit `a3a2989`:
  runtime actor/status, trusted tick, checkpoint and WSS caller are server-owned.
- Fresh PC/backend/proof lanes selected canonical poison final-tick expiry as the
  smallest branch whose complete required state is already checkpointed. Natural
  regen remains deferred because replenish/sit/PK fields are not all in the
  runtime actor/checkpoint.
- Canonical `KNpc.cpp:972-988` order is exact: decrement poison time; normalize
  zero interval to one; execute any modulo damage tick; then when time is zero
  clear `nValue[0]`, `nValue[1]` and `nValue[2]`. Therefore starting at time one
  performs final damage before cleanup. Time zero is a no-op.
- A noncanonical `Assets/StreamingAssets/Reference/KNpc.cpp` copy lacks the
  cleanup branch; it is recorded only as variant drift and cannot override the
  `/var/www/jx-pc` source of truth.
- Delivery implementation proof passed `41` focused domain/runtime/checkpoint
  tests; full combat regression passed `669`. Changed-scope Ruff/Black, strict
  specs and diff check passed. Broad combat Ruff/Black retains legacy failures
  outside this story's seven changed backend paths.
- Harness `story complete` reran the configured release command after evidence
  and proof-flag refresh: `669 passed`, scoped Ruff/Black pass, strict
  `inventory=106183 coverage=104655`, diff check pass; it then atomically marked
  the story `implemented`.
- Delivery run `orch-af8b569f8cc3db42` was cancelled after root spec edits
  overlapped a read-only reviewer boundary; its discovery/writer artifacts are
  retained but not used as terminal receipt. Clean successor run
  `orch-25ecf7c87aa41619` finished verified with `2/2` clean boundaries and
  terminal P0/P1 clear. `RESULT.json` SHA-256 is
  `5d679444a9aaccbea9ffaf530321cbe9380a2f23f91cf065e0c6a90300d57c7e`.
- Full `ProcessState`/`CalcDamage`, poison attacker/global index,
  RNG/aura/broadcast/death hooks remain explicit GAP-CBT-001 and GAP-CBT-002.
