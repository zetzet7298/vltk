# US-M34-001 M3.4 Trap Script Hook

## Status

implemented

## Lane

normal

## Intake

Intake #16 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

Trap triggers call stubbed or real Lua hooks so map interactions can be validated
incrementally: with the Lua bridge disabled a stub log records the trigger, with
the bridge enabled the configured Lua function is attempted, and a Lua failure
appears in the Logs tab tagged with the trap id.

## Relevant Product Docs

- `docs/spec.md` — "M3.4 — Trap Script Hook"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Player enters trap; Lua bridge disabled; stub log records trigger.
- AC2: Player enters trap; Lua bridge enabled; configured Lua function is attempted.
- AC3: Lua function fails; trigger fires; error appears in Logs tab with trap id.

## Design Notes

- `TrapTriggerService` (pure C#): `OnPlayerEnter(trap)` routes to stub log when
  `LuaBridgeEnabled` is false (AC#1), invokes `LuaScriptBridge.Run(scriptRef,
  EnterFunction, trapIndex)` when enabled (AC#2), records `TrapFireOutcome` and
  surfaces failures with the trap id (AC#3). Keeps a `TrapFireRecord` log for the GM
  Logs tab.
- Built on M1.6 `TrapDefinition` + M3.3 `LuaScriptBridge`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: stub when disabled, invoke when enabled, no-script, missing-fn failure, lua-failure with trap id, log accumulate/clear, toggle |
| Integration | Trap → Lua bridge dispatch (unit-covered via fake runtime) |
| E2E | Live trap entry in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Closes Phase M3: map-script interaction layer over the Lua bridge.

## Evidence

EditMode 287/287 pass (docs/evidence/editmode-results-2026-05-31-m3-npc-lua.json).
`TrapTriggerService` + `TrapFireRecord`/`TrapFireOutcome`. Suite
`VLTK.Tests.Sandbox.TrapTriggerServiceTests` (8 tests) covers AC1–AC3.
