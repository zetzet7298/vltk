# US-M24-001 M2.4 Pathfinding Prototype

## Status

implemented

## Lane

normal

## Intake

Intake #10 (spec_slice, normal). Flags: Existing behavior, Cross-platform.

## Product Contract

A pathfinding prototype over converted obstacle cells finds a path that avoids
walk-blocked cells, logs and surfaces a failure when no path exists, returns a
node list for a debug overlay, and considers neighbor-region obstacles when a path
crosses a region boundary.

## Relevant Product Docs

- `docs/spec.md` — "M2.4 — Pathfinding Prototype"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Obstacle grid exists; path requested; path avoids walk-blocked cells.
- AC2: No valid path exists; path requested; failure is logged and shown in GM
  diagnostics (failureReason).
- AC3: Path debug overlay enabled; path requested; nodes/segments are drawn (cell
  list returned, contiguous single-step).
- AC4: Region boundary path requested; path crosses region; neighbor region
  obstacles are considered.

## Design Notes

- `PathfindingService` (pure C#): deterministic A* (Manhattan heuristic, closed
  set + lazy priority queue), budget cap, failure reporting.
- `IWalkabilityProvider`: `GridWalkability` (single grid, AC1) and
  `RegionedWalkability` (multi-region via `CoordinateService`, AC4; missing
  neighbor region = blocked).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: shortest path, avoid blocked, no-path failure+log, blocked start, OOB goal, contiguity, cross-region, missing-neighbor blocked |
| Integration | Regioned provider stitches grids via CoordinateService (unit-covered) |
| E2E | GM path overlay in Play Mode (documented; not automated in EditMode) |
| Platform | Budget cap bounds search (asserted) |
| Release | N/A |

## Harness Delta

Reusable A* primitive for NPC/movement work in later phases.

## Evidence

EditMode 248/248 pass (docs/evidence/editmode-results-2026-05-31-m2-character.json).
`PathfindingService` + `IWalkabilityProvider`/`GridWalkability`/`RegionedWalkability`.
Suite `VLTK.Tests.Sandbox.PathfindingTests` (9 tests) covers AC1–AC4.
