# US-M22-001 M2.2 Coordinate System Parity

## Status

implemented

## Lane

normal

## Intake

Intake #8 (spec_slice, normal). Flags: Existing behavior, Cross-platform.

## Product Contract

PC source coordinates (pixel / obstacle cell / region) map to deterministic Unity
world coordinates and back. A debug inspector can decompose any Unity world
position into its equivalent pixel, global cell, region, and local-cell
coordinates, and the conversion stays continuous across region boundaries.

## Relevant Product Docs

- `docs/spec.md` — "M2.2 — Coordinate System Parity"
- `docs/ARCHITECTURE.md` — pure logic vs MonoBehaviour boundary

## Acceptance Criteria

- AC1: Source region/cell/pixel coordinate; conversion runs; Unity world
  coordinate is deterministic (and round-trips).
- AC2: Unity coordinate; debug inspector opens; equivalent map/region/cell
  coordinate is shown.
- AC3: Region boundary crossed; player moves; coordinate conversion remains
  continuous (no jump at the boundary).

## Design Notes

- `CoordinateConfig`: pixelsPerUnit, cellSizePixels (32), regionCells (16x32 from
  PC `cGround` 7x7 grounds + 32px cells), optional Y flip.
- `CoordinateService` (pure C#): pixel↔world (single linear transform → AC3
  continuity), pixel↔cell, cell↔region/local via floor-division (handles negative
  coords), `Inspect()` full decomposition for the GM inspector (AC2).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: deterministic mapping, round-trip, region/local decomposition, boundary continuity, negative coords |
| Integration | Cell↔world↔region round-trips covered at unit level |
| E2E | GM coordinate inspector (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Establishes the coordinate primitive M2.1/M2.4 (movement, pathfinding) build on.

## Evidence

EditMode 248/248 pass (docs/evidence/editmode-results-2026-05-31-m2-character.json).
`CoordinateService` + `CoordinateConfig`. Suite `VLTK.Tests.Sandbox.CoordinateServiceTests`
(11 tests) covers AC1–AC3 plus Y-flip and negative-coordinate decomposition.
