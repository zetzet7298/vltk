# US-M33-001 M3.3 Lua Script Loader

## Status

implemented

## Lane

normal

## Intake

Intake #15 (spec_slice, normal). Flags: Existing behavior, New dependency (Lua runtime).

## Product Contract

Original map/NPC Lua scripts load through a controlled bridge: a registered script
loads or reports a syntax/encoding error, a GM can run a script function and see
the result logged, calls to unbound host APIs log a missing binding instead of
crashing, and a reload swaps in the changed script version.

## Relevant Product Docs

- `docs/spec.md` — "M3.3 — Lua Script Loader"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Lua script path registered; loader runs; script loads or reports
  syntax/encoding error.
- AC2: GM runs script function; function exists; function executes and logs result.
- AC3: Function calls unbound API; script runs; bridge logs missing binding instead
  of crashing.
- AC4: Script reload clicked; script changes; new script version is used.

## Design Notes

- `ILuaRuntime` (pluggable backend; MoonSharp/NLua later), `LuaLoadResult`/
  `LuaCallResult`/`LuaLoadStatus`.
- `LuaScriptBridge` (pure C# orchestration): `Bind` host APIs, `Load`/`Reload`
  (versioned, missing-source → Missing), `Run` (auto-load, missing-function +
  missing-binding logging, try/catch so a script fault never crashes the sandbox).
- Fully EditMode-testable via a fake runtime; no real VM dependency required yet.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: load ok/syntax/encoding/missing, run ok/missing-fn, missing-binding logged, runtime-throw caught, reload version bump |
| Integration | Bridge ↔ runtime contract exercised via fake (unit-covered) |
| E2E | Live MoonSharp/NLua execution (deferred; documented) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Establishes the runtime-agnostic Lua bridge primitive M3.4 + later script hooks reuse.

## Evidence

EditMode 287/287 pass (docs/evidence/editmode-results-2026-05-31-m3-npc-lua.json).
`ILuaRuntime`/`LuaScriptBridge` + result types. Suite
`VLTK.Tests.Sandbox.LuaScriptBridgeTests` (10 tests) covers AC1–AC4. A concrete
Lua VM backend is deferred to a future dependency story; the bridge is verified
against a fake runtime.
