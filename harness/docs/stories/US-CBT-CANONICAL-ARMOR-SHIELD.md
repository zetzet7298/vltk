# US-CBT-CANONICAL-ARMOR-SHIELD Correct armor and mana-shield semantics

## Status

planned

## Lane

normal (strong validation)

## Product Contract

Correct the existing backend combat subset to match current canonical PC
`KNpc::CalcDamage`: elemental armor is a persistent flat damage subtraction,
not a consumable timed pool; mana shield remains configured after mana
exhaustion and has no ProcessState timer decay. Only `ClearNormalState` performs
the explicit full reset.

## Relevant Product Docs

- `/var/www/vltk-mobile/backend/specs/domains/p1-skill-combat.md`
- `/var/www/vltk-mobile/backend/specs/06-gap-checklist.md`
- `/var/www/vltk-mobile/backend/specs/07-provenance.md`
- `/var/www/vltk-mobile/backend/specs/08-acceptance-audit.md`
- `/var/www/vltk-mobile/domains/server-runtime/README.md`

## Canonical Evidence

- Nested revision: `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`.
- `KNpc.cpp` SHA-256:
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`.
- `KNpc.cpp:2498-2587`: subtract matching armor `nValue[0]` without mutation.
- `KNpc.cpp:2624-2639`: positive shield percent converts damage to mana; on
  insufficient mana, preserve shield field and clamp only mana to zero.
- `KNpc.cpp:787-1099`: no armor or mana-shield timer decay in ProcessState.
- `KNpc.cpp:6968-6988`: explicit full-state clear.
- `KNpcAttribModify.cpp:376-575`: armor/shield modifiers add only `nValue[0]`.

## Acceptance Criteria

- Repeated damage sees the same flat armor until an explicit clear or future
  server-owned modifier changes it; armor/time metadata is not consumed/reset.
- ProcessState leaves armor values/times and mana-shield percent/time unchanged.
- Positive shield percent matches enough/insufficient mana ordering and integer
  arithmetic; zero/negative shield percent is inactive; mana exhaustion does
  not clear configured shield metadata.
- Real combat service/server-owned runtime and checkpoint tests prove mutation,
  retry and persistence outcomes without public schema changes.
- Focused/full regressions, scoped Ruff/Black, strict specs, staged srcwalk and
  fresh reviewer pass.

## Non-Goals

- No armor/shield setup pipeline, public REST/WSS/Unity contract change, DB
  migration, or removal of legacy time fields from checkpoint/model.
- No full resist/five-elements/PK/RNG/reflection/attacker-global CalcDamage
  parity and no client-authored legacy endpoint redesign.
- No guessed overflow behavior; uncovered C++ overflow remains explicit.

## Design Notes

- Legacy time fields remain inert compatibility data until a separately proven
  migration/removal story.
- Backend-only diagnostics may remain if they do not alter canonical state or
  damage outcome and have executable meaning.
- Intake: `#14`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Pure armor/shield arithmetic and no-mutation invariants |
| Integration | Existing application/server-owned runtime path without database |
| E2E | Not applicable: no public contract change |
| Platform | Not applicable |
| Release | Full combat + runtime unit regression and strict specs |

## Harness Delta

- Add one bounded correction story and detailed trace.

## Evidence

Pending implementation and fresh proof.
