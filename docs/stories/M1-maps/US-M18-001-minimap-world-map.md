# US-M18-001 M1.8 Minimap and World Map Data

## Status

implemented

## Lane

normal

## Intake

Intake #6 (spec_slice, normal). Flags: Cross-platform.

## Product Contract

Each converted map can register a minimap/world-map overview artifact so
navigation UI can be validated early. The GM Map tab can toggle a minimap
preview, shows a player marker positioned in the correct scale, and surfaces a
visible missing state (with source id) when the minimap asset is absent.

## Relevant Product Docs

- `docs/spec.md` — "M1.8 — Minimap and World Map Data" and section 4.5
  (`MapDefinition.minimapRef`)
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Map has an overview image; conversion runs; a minimap/world-map artifact
  is registered (in the asset registry, addressable by source id).
- AC2: GM Panel Map tab shows the loaded map; with the minimap toggle enabled a
  minimap preview appears.
- AC3: Player placeholder moves; with the minimap displayed the marker position
  updates in the correct scale (world bounds -> minimap UV).
- AC4: Minimap asset missing; with the toggle enabled the missing state is
  visible with its source id.

## Design Notes

- `MapModel`: add `MinimapRef` (source id + artifact status) and wire
  `MapDefinition.minimapRef`.
- `MinimapService` (pure C#): world position -> normalized minimap coordinate
  using map source bounds; resolve/register minimap artifact via asset registry;
  expose missing state + source id. Fully EditMode-testable.
- `GMMapTab`: minimap toggle, `RawImage` preview, marker `RectTransform`, missing
  label. UI wiring documented; logic asserted via service tests.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests for marker scale mapping, artifact registration, missing-state reporting |
| Integration | Minimap artifact resolves through AssetRegistry by source id |
| E2E | Manual GM toggle/preview (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Adds `MapDefinition.minimapRef` to satisfy spec 4.5 required field.

## Evidence

EditMode 199/199 pass (docs/evidence/editmode-results-2026-05-31-m1-streaming-minimap-golden.json).
`MinimapService` (pure C#) + `MapDefinition.minimapRef`/`MinimapRef` model + `GMMapTab` minimap
toggle/RawImage/marker/missing-label wiring. Suite `VLTK.Tests.Sandbox.MinimapTests` (9 tests)
covers AC1 (artifact registration), AC3 (marker scale world->minimap UV), AC4 (missing state +
source id). AC2 GM toggle/preview is documented UI, logic asserted via service tests.
