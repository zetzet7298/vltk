# US-RUN-RUNTIME-CORE Build authoritative runtime core

## Status

implemented

## Lane

normal

## Product Contract

Provide a framework-free runtime core that owns session identity, epoch/sequence,
trusted 18 Hz tick, authoritative combat entity state, queued intent and snapshot
projection. It implements decision 0008 and exposes application ports for later
Protobuf WSS and durable adapters.

## Relevant Product Docs

- `docs/decisions/0008-game-v1-runtime-authority.md`
- `/var/www/vltk-mobile/domains/server-runtime/README.md`
- `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`
- `/var/www/vltk-mobile/backend/specs/domains/p0-runtime-protocol.md`

## Acceptance Criteria

- Runtime input contains command/target intent, epoch, sequence and target tick;
  it cannot carry life, mana, status, damage range or server clock.
- `RuntimeSession` binds realm, character, content release and one monotonic epoch;
  a replacement epoch invalidates the old session.
- Duplicate sequence is replay-only, a gap is buffered within window 64, overflow
  requests resync, and target tick is bounded to six future ticks.
- `(client_seq, input_seq)` is the authoritative execution order; `target_tick`
  is only a lower-bound eligibility gate, so a future or in-flight earlier
  command blocks later commands from overtaking it.
- An injected monotonic scheduler advances server tick at 18 Hz and queues input
  before the tick; a recording combat port proves state → command → status/action
  ordering without executing at packet admission.
- Snapshots/results derive only from server-owned state and carry trusted tick and
  last processed client sequence.
- Domain code imports no FastAPI, SQLAlchemy, generated Protobuf or Unity DTO.

## Design Notes

- Commands: enqueue intent; advance one tick; replace/resume session.
- Queries: authoritative snapshot and prior command outcome.
- API: application ports only; no route in this child.
- Tables: none; fake repositories only.
- Non-goals: WSS framing, generated codec, DB migration and full combat parity.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Epoch/seq/window/target-tick/tick cadence/order/snapshot cases. |
| Integration | Not applicable until durable/WSS children. |
| E2E | Not applicable. |
| Platform | Not applicable. |
| Release | Focused unit, changed-file Ruff/Black, strict specs, diff check. |

## Harness Delta

First completed bounded child of `US-RUN-COMBAT-AUTHORITY`; parent runtime
production authority remains open for Protobuf, data, durability and WSS children.

## Evidence

- Herdr run `orch-0c126904db5113af` finished verified with 14 collected attempts,
  14 clean boundary reports, zero unowned changes and zero warnings. `RESULT.json`
  SHA-256: `1c15bfa65552b544a03829baf164e91ce26f3c9f72e4f2c11fa673fa26650c76`.
- Pure runtime implementation is under `/var/www/vltk-mobile/backend/app/modules/runtime/`;
  executable proof is
  `/var/www/vltk-mobile/backend/tests/unit/modules/runtime/test_combat_authority.py`.
- Admission proves epoch/replay/window/target bounds without combat side effects;
  payload containers are detached recursively, mutable scalar subclasses fail
  closed, and queue/completion handling preserves `(client_seq, input_seq)` with
  at most one in-flight command.
- Fresh `story complete` verification collected `19 passed`; changed-scope Ruff
  and Black passed, strict specs returned
  `OK: inventory=106183 coverage=104655 strict=True`, and `git diff --check`
  passed.
- Final independent proof accepted the story only as a bounded executable core.
  API/WSS, generated Protobuf, DB/UoW, checkpoint/outbox, durable replay/restart,
  Unity transport, production game-loop composition and full PC combat parity
  remain explicit follow-up gaps.
