# US-M64-001 M6.4 Production HUD Bridge

## Status

implemented

## Lane

normal

## Intake

Intake #26 (spec_slice, normal). Flags: Release, Existing behavior.

## Product Contract

The production HUD consumes sandbox-proven systems so debug and production surfaces
do not diverge: the HUD reads map/player data from runtime systems (not conversion
internals), the GM Panel can still be opened in dev builds, and release builds keep
debug controls hidden/protected.

## Relevant Product Docs

- `docs/spec.md` — "M6.4 — Production HUD Bridge"
- Reuses M6.3 GM-exposure guard

## Acceptance Criteria

- AC1: HUD is implemented; it needs map/player data; it reads from runtime systems,
  not conversion internals.
- AC2: GM Panel exists; production HUD enabled; GM can still be opened in dev builds.
- AC3: Release build configured; GM disabled or protected; debug controls are not
  exposed unintentionally.

## Design Notes

- `IRuntimeStateProvider` — stable runtime contract (active map, player pos/level/
  life) the HUD reads instead of parsers/importers (AC#1).
- `HudDataBridge` (pure C#): `BuildSnapshot` from the provider (clamps life, guards
  zero max), `CanOpenGmPanel`/`DebugControlsAllowed` gated on `IsDevelopmentBuild`
  (AC#2), `TryRunDebugAction` blocks debug actions in release builds (AC#3).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: snapshot from runtime, no-map/null invalid, life clamp, zero-max guard, GM dev open + action, release hide + blocked action |
| Integration | HUD reads runtime provider, not conversion (interface enforced) |
| E2E | Production HUD on device (documented; not automated in EditMode) |
| Platform | N/A |
| Release | Debug-control guard asserted |

## Harness Delta

Closes Phase M6: production/debug share one runtime contract, preventing divergence.

## Evidence

EditMode 386/386 pass (docs/evidence/editmode-results-2026-05-31-m6-mobile.json).
`IRuntimeStateProvider` + `HudDataBridge` + `HudSnapshot`. Suite
`VLTK.Tests.Sandbox.HudDataBridgeTests` (9 tests) covers AC1–AC3.
