# US-CBT-LOSE-MANA-TICK Port canonical LoseMana runtime tick

## Status

implemented

## Lane

high-risk

## Product Contract

Port exactly the server-side `KNpc::ProcessState` LoseMana tick from canonical
`KNpc.cpp:1077-1085` through the existing server-owned combat runtime. Preserve
post-decrement modulo timing at `GAME_FPS=18`, integer subtraction and zero
clamp. Persist the timed state in the private runtime checkpoint. Do not add
client-authored combat fields and do not claim the separate
`Poison2DecManaP` application formula or full ProcessState parity.

## Canonical Evidence

- Nested revision: `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`.
- `KNpc.cpp` SHA-256:
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`.
- `KNpc.cpp:1077-1085`: active guard, decrement, modulo `GAME_FPS`,
  subtract `nValue[0]`, clamp negative mana to zero.
- `GameDataDef.h:86`: `GAME_FPS=18`.
- `KNpcAttribModify.cpp:892-900` sets time/value from a percent formula, but its
  content winner and division preconditions remain successor scope.
- `KNpc.cpp:6968-6988` clears LoseMana in `ClearNormalState`.

## Acceptance Criteria

- Server-owned runtime/checkpoint model gains one timed LoseMana field without
  API/request/public-proto changes.
- Real `status_effect.process_state` executes LoseMana after ManaState and before
  later status branches, using post-decrement modulo `18`.
- Tests prove non-due preservation, due subtraction, final tick, underflow clamp,
  inactive no-op, runtime projection and checkpoint round trip.
- Legacy checkpoint without LoseMana decodes to an inactive zero state.
- Codegen check, focused/full regression, scoped Ruff/Black, strict specs and
  independent reviewer pass.
- `Poison2DecManaP` application, other missing timed states and full combat
  parity remain explicit gaps.

## Non-Goals

- No REST/WSS request field or Unity DTO change.
- No shared PostgreSQL integration/e2e.
- No guessed default for PC content percent/duration or division-by-zero behavior.
- No refactor of unrelated status branches.

## Verification

```text
cd /var/www/vltk-mobile/backend
python3 scripts/generate_runtime_checkpoint_v1_proto.py --check
pass
pytest -q tests/unit/modules/combat/test_status_effect.py tests/unit/modules/combat/test_server_owned_runtime.py tests/unit/modules/runtime/test_checkpoint_codec.py
79 passed
pytest -q tests/unit/modules/combat tests/unit/modules/runtime
856 passed
ruff check <changed paths>
All checks passed!
black --check <changed paths>
11 files would be left unchanged.
python specs/scripts/validate.py --strict
OK: inventory=106183 coverage=104655 strict=True
git diff --check HEAD^ HEAD -- <15 story paths>
pass
```

- Backend commit:
  `4f310a3a9c42743ccf6d611492e523190564b706`.
- Herdr run `orch-0fc5627c261a303b` finished verified with `4/4` clean attempt
  boundaries, zero ownership violations and zero warnings. Both reviewers found
  no P0/P1; present-zero checkpoint canonicalization and Black P2 findings were
  repaired. `RESULT.json` SHA-256:
  `fea8cca3032325f058e2d2f3a7f07c29766181f224b3e8a321efc85c90c664cd`.
- Harness fresh `story complete` proof passed and atomically set the lifecycle
  to `implemented`. Evidence flags: unit `true`; integration/e2e/platform
  `false`. No database suite was run.
- `Poison2DecManaP` application/content/division/overflow semantics and global
  `GAP-CBT-001`/`GAP-CBT-002` remain open; this story claims only the bounded
  runtime-wired LoseMana tick.
