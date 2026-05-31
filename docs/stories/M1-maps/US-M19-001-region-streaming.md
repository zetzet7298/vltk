# US-M19-001 M1.9 Region Streaming

## Status

implemented

## Lane

normal

## Intake

Intake #5 (spec_slice, normal). Flags: Existing behavior, Cross-platform.

## Product Contract

Active map regions stream around the camera/player so large VLTK maps stay within
a mobile memory budget. The runtime loads the active region plus a configurable
neighbor ring, loads/unloads deterministically on boundary crossing, color-codes
region state for a GM overlay, marks failed regions without aborting the runtime,
and never exceeds the configured max-loaded region budget.

## Relevant Product Docs

- `docs/spec.md` — "M1.9 — Region Streaming"
- `docs/ARCHITECTURE.md` — pure logic vs MonoBehaviour boundary

## Acceptance Criteria

- AC1: Player starts in a region; on map load the active region plus the
  configured neighbor ring loads.
- AC2: Player crosses a region boundary; neighbor regions load/unload
  deterministically.
- AC3: GM overlay enabled; as the player moves, loaded/loading/unloaded regions
  are color-coded.
- AC4: A region load fails; runtime continues and the failed region is marked
  and logged.
- AC5: A mobile memory budget is set; when many regions would load, streaming
  respects the max-loaded region budget.

## Design Notes

- `RegionStreamingService` (pure C#, no MonoBehaviour): grid math, desired-set
  computation, deterministic load/unload plan, sparse per-region state, budget
  cap, color mapping. Already skeletoned; finish + harden.
- `RegionStreamController` (MonoBehaviour): drives the service from player/camera
  world position each frame (throttled), invokes load via `MapRenderer`, reports
  load success/failure back to the service.
- `RegionStreamOverlayRenderer` (MonoBehaviour): AC3 color-coded region overlay
  in world space, toggled from the GM Map tab.
- Budget default tuned for mobile (`maxLoaded`), `ringRadius` configurable.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests for desired-set, boundary crossing load/unload, budget cap, fail handling, color mapping |
| Integration | Service drives plan from world positions deterministically (covered by unit-level harness) |
| E2E | Manual/Play Mode overlay check (documented; not automated in EditMode) |
| Platform | Max-loaded budget respected (asserted in unit) |
| Release | N/A this story |

## Harness Delta

New `docs/stories/M1-maps/` folder for M1 map stories. Story rows added to durable
layer for M1.8/M1.9/M1.11.

## Evidence

EditMode 199/199 pass (docs/evidence/editmode-results-2026-05-31-m1-streaming-minimap-golden.json).
`RegionStreamingService` (pure C#) hardened; `RegionStreamController` + `RegionStreamOverlayRenderer`
MonoBehaviours drive/render it. Suite `VLTK.Tests.Sandbox.RegionStreamingTests` (12 tests) covers
AC1 (active+ring load), AC2 (deterministic boundary load/unload), AC3 (overlay color mapping),
AC4 (failed region marked/logged, runtime continues), AC5 (max-loaded budget respected).
