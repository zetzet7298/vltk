# US-CBT-SERVER-TIMER-STATES Port canonical server timer-only states

## Status

implemented

## Lane

normal (strong validation)

## Product Contract

Port exactly the `_SERVER` `KNpc::ProcessState` timer-only branches for
`m_FrozenAction`, `m_HideState`, `m_SilentState`, and `m_WalkRun` through the
existing server-owned combat runtime and private runtime checkpoint. Each active
state decrements once per ProcessState frame; inactive zero is a no-op; natural
expiry preserves stored values; `ClearNormalState` clears the full state.

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
- `KNpc.cpp:1013-1016`: FrozenAction active guard and decrement.
- `KNpc.cpp:1087-1098`: Hide, Silent, and WalkRun active guards/decrements.
- `KNpc.cpp:6968-6988`: full-state clear for all four fields.
- `KNpc.h:190-201`: all four use the PC `KState` shape.

## Acceptance Criteria

- Combat/runtime models and private checkpoint gain four additive timed fields;
  no request, REST/WSS input, Unity DTO, public `game.v1`, or DB schema changes.
- Real `status_effect.process_state` executes the four decrements in canonical
  order around the existing RandMove/Stun/LoseMana seams.
- Tests prove time `0`, `1`, and `2`; final decrement preserves value fields;
  `ClearNormalState` resets values/time for all four.
- Real runtime conversion/projection persists the state through an application
  tick; private checkpoint proves active roundtrip, legacy absence/default
  omission, and rejection of present-but-all-zero optional fields.
- Codegen, focused and combat/runtime regressions, scoped Ruff/Black, strict
  specs, staged `srcwalk review`, and a fresh independent reviewer pass.

## Non-Goals

- No FrozenAction application formula, Hide visibility/render/targeting,
  Silent skill enforcement, WalkRun shadow consumer, or RandMove RNG/AI logic.
- No PAK/SPR/DAT/content selection and no shared PostgreSQL integration/e2e.
- No claim of full `KNpc::ProcessState` or `CalcDamage` parity.

## Design Notes

- Domain rules stay pure in combat; runtime adapter owns conversion/projection;
  protobuf remains private checkpoint v1 with additive field numbers.
- Absent optional field is the only canonical inactive representation. A
  present all-zero field must fail with `NONCANONICAL`.
- Intake: `#13`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Domain boundaries/clear, runtime tick/projection, checkpoint compatibility |
| Integration | In-process real runtime application service; no database |
| E2E | Not applicable: no public/client contract delta |
| Platform | Not applicable |
| Release | Full combat + runtime unit regression and strict specs |

## Harness Delta

- Add one bounded normal-lane story and a detailed completion trace.

## Evidence

- Backend commit:
  `d3af38f541afede27e7484efe672679db318caa6`.
- Canonical revision/hashes and exact function/line mapping are recorded in
  backend `specs/07-provenance.md`; setup/consumer/RandMove gaps remain open.
- Root proof: checkpoint codegen `--check`; `83/83` focused tests; `860/860`
  combat+runtime regression tests; scoped Ruff and Black; strict specs
  `OK: inventory=106183 coverage=104655 strict=True`; staged `srcwalk review`;
  commit-scoped whitespace check.
- Herdr `orch-08a3ccddd48be501` finished verified with `6/6` clean attempt
  boundaries, zero ownership violations and zero warnings. After three
  proof-only P2 fixes, the terminal reviewer reported no P0/P1/P2.
  `RESULT.json` SHA-256:
  `ea3480890f8f2b03233c3da9bdb576c7ac5a92060d12bdd2bdf023cfeacca16a`.
- Harness fresh `story complete` proof passed and atomically set this story to
  `implemented`. Unit proof is true; integration/e2e/platform are false; no
  database test ran.
