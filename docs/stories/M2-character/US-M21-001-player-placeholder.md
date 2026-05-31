# US-M21-001 M2.1 Player Placeholder

## Status

implemented

## Lane

normal

## Intake

Intake #9 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

A controllable player placeholder spawns at a safe/default position, moves toward
a clicked walkable map cell, rejects or holds at blocked cells, and responds
immediately to GM speed changes — so map scale, camera, and obstacles can be
validated.

## Relevant Product Docs

- `docs/spec.md` — "M2.1 — Player Placeholder"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Map is loaded; player spawns; player appears at configured safe/default
  position.
- AC2: Developer clicks walkable map cell; input is processed; player moves toward
  target (and arrives without overshoot).
- AC3: Developer clicks blocked cell; input is processed; movement rejects or
  holds (no entry into a walk-blocked cell).
- AC4: GM changes speed; player moves; movement speed updates immediately.

## Design Notes

- `PlayerMovementService` (pure C#): owns position/target, `RequestMoveTo` rejects
  blocked destinations via `ObstacleQueryService`, `Step(dt, grid)` advances and
  guards against stepping into a blocked cell, `Speed` settable mid-flight.
- MonoBehaviour driver feeds deltaTime + click input (documented).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: spawn, move+arrive, no overshoot, blocked target rejected, blocked step held, immediate speed change |
| Integration | Movement uses ObstacleQueryService over ObstacleGrid (unit-covered) |
| E2E | GM click-to-move in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

First gameplay actor in the sandbox; reuses M1.5 obstacle query + M2.2 coords.

## Evidence

EditMode 248/248 pass (docs/evidence/editmode-results-2026-05-31-m2-character.json).
`PlayerMovementService` + `MoveStepResult`. Suite `VLTK.Tests.Sandbox.PlayerMovementTests`
(9 tests) covers AC1 (spawn), AC2 (move/arrive/no-overshoot), AC3 (blocked
target/step), AC4 (immediate speed change).
