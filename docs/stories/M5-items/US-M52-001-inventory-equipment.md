# US-M52-001 M5.2 Inventory and Equipment Sandbox

## Status

implemented

## Lane

normal

## Intake

Intake #21 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

Sandbox inventory/equipment tools let item data be tested before production UI:
items in the database are searchable, a developer can add an item to a test
inventory, equipping an item updates a character stat preview, and a missing item
icon shows a diagnostic.

## Relevant Product Docs

- `docs/spec.md` — "M5.2 — Inventory and Equipment Sandbox"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Item database exists; GM opens Items tab; items are searchable.
- AC2: Developer adds item; command runs; item appears in test inventory.
- AC3: Developer equips item; command runs; character stats preview updates.
- AC4: Item icon missing; item displayed; missing icon diagnostic is shown.

## Design Notes

- `InventoryService` (pure C#): `Search` by id/name (empty = all sorted), `AddItem`
  (stacks), `Equip`/`Unequip` recompute `StatPreview` (sum of equipped stat deltas by
  attr code), `MissingIconItems`/`HasMissingIcon` for the diagnostic.
- Built on M5.1 `ItemContractImporter` database.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: search by name/id/empty, add+stack, unknown add, equip preview/replace/unequip, missing icon |
| Integration | Reads item DB from importer (unit-covered) |
| E2E | GM Items tab in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Sandbox inventory layer over M5.1 item DB; preview feeds M5.3 set/refine.

## Evidence

EditMode 345/345 pass (docs/evidence/editmode-results-2026-05-31-m5-items.json).
`InventoryService` + `EquipSlot`/`InventoryEntry`. Suite
`VLTK.Tests.Sandbox.InventoryServiceTests` (11 tests) covers AC1–AC4.
