# Proposal — Bind Accessory Equipment Slots

> Change: `bind-accessory-equipment-slots`  
> SDD mode: auto · Artifact store: both · Chained PR strategy: auto-forecast · Review budget: 400 changed lines  
> Skill resolution: `paths-injected` (`jx-pc-port-rule`, `jx-pc-resource-resolver` read).  
> Runtime UI guidance: avoid Unity 6-only runtime `DataBinding`; use the existing Unity-compatible pattern of model/service state plus popup presenter/controller updating `VisualElement` trees and event callbacks.

## Intent

Character Info already presents an equipment paperdoll, but accessory-like equipment slots are not yet backed by canonical runtime equipment data. In particular, Mask, Amulet, Charm, and Trinket display/binding are incomplete or placeholder-like. This blocks the next gameplay layer: equipping, unequipping, socketing, and validating item actions against real slots.

This change establishes accessory equipment slots as first-class data concepts and binds currently equipped accessory items into the existing Character Info / equipment UI. The goal is to create a reliable data contract before implementing active equip/unequip/socket gameplay.

## Problem

Today the Character Info equipment UI can show slot frames, but it does not have a complete authoritative mapping from PC item categories to runtime equipment slots for accessory slots. That causes several product and engineering gaps:

- The UI cannot reliably render equipped Mask/Amulet/Charm/Trinket items from player state.
- Gameplay services cannot validate future equip/unequip/socket operations against canonical slots.
- Tests cannot assert parity between `PcItemCategory`, `PlayerEquipSlot`, equipment mapping, and visible UI state.
- Any future socket implementation would risk hardcoding slot names or duplicating mapping logic in UI code.

Because this is a data/model prerequisite, treating it as a UI-only patch would create fragile behavior and make later gameplay harder to review.

## Scope

### In scope

- Add or extend canonical `PlayerEquipSlot` entries for accessory slots required by the PC/VLTK equipment model, including Mask, Amulet, Charm, and Trinket.
- Add or extend `PcItemCategory` mapping so PC item data resolves to the correct runtime equipment slot(s).
- Update `PlayerEquipmentService` or its equivalent mapping/lookup layer so consumers can:
  - determine whether an item category can occupy a given accessory slot;
  - read the currently equipped item for those slots;
  - expose stable slot ordering/names for UI binding.
- Bind the existing Character Info / equipment popup to real equipped accessory state for those slots.
- Preserve Vietnamese UI labels for user-facing slot names.
- Add focused EditMode tests covering:
  - enum/category/slot mapping for Mask/Amulet/Charm/Trinket;
  - service lookup behavior for equipped accessory items;
  - Character Info UI rendering/binding for those slots.
- Keep the change additive where possible: introduce canonical mappings and bind existing UI without changing unrelated equipment behavior.

### Out of scope / non-goals

- No socket gameplay implementation yet (`Đính` remains a later behavior slice unless existing code already supports a no-op display state).
- No equip/unequip interaction implementation yet beyond data binding needed to display current state.
- No item stat math overhaul unless a minimal accessor is strictly required to identify/display equipped items.
- No inventory drag/drop, item comparison tooltip, or hot action menu.
- No new UI art unless an existing accessory slot frame is genuinely missing; if PC art must be resolved, use `jx-pc-resource-resolver` and verify Vietnamese assets.
- No broad refactor of the whole item system; only the minimum canonical slot/category/service surface needed for accessory binding.

## Affected Areas

- Equipment domain model:
  - `PlayerEquipSlot` or equivalent slot enum/model.
  - `PcItemCategory` or equivalent PC item classification.
  - Category-to-slot mapping helpers.
- Equipment/player state services:
  - `PlayerEquipmentService` or equivalent read/lookup API.
  - Save/load compatibility layer if equipped slots are serialized.
- Character Info popup:
  - equipment slot query names / presenter binding;
  - accessory slot labels and icon/item-name rendering;
  - empty-slot behavior.
- Tests:
  - mapping tests for accessory categories;
  - service tests for equipped accessory lookup;
  - popup/content tests for Character Info slot rendering.

## Design Direction

1. **Single source of truth for slot mapping**  
   PC item category interpretation should live in a domain/service mapping layer, not inside Character Info UI. The UI should ask for slot view models or equipped item state by canonical `PlayerEquipSlot`.

2. **Unity-compatible manual UI binding**  
   Follow the existing popup pattern: query named `VisualElement`s, update icons/text/classes from a presenter/content class, and respond to events via callbacks. Do not depend on Unity 6 runtime `DataBinding` APIs.

3. **Stable empty states**  
   Empty Mask/Amulet/Charm/Trinket slots should remain visible as slot frames with Vietnamese labels or existing empty-slot affordance. Missing items should not throw and should not collapse layout.

4. **PC parity before invention**  
   Slot/category semantics must be checked against PC source/data under `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem` and/or project reference files before implementation. Do not invent category meanings. If slot art or labels need PC resources, resolve via `jx-pc-resource-resolver` rather than guessing hashed SPR names.

5. **Save compatibility first**  
   If equipment state is serialized by enum names/integers, adding slots must avoid corrupting existing saves. Prefer append-only enum changes or explicit serialization keys if current code requires it.

## Risks and Mitigations

- **PC data category ambiguity**  
  Accessory categories may not map one-to-one to mobile-facing slot labels. Mitigation: inspect PC item source/data before implementation and document the mapping in tests.

- **Save-state compatibility**  
  Adding enum values can break serialized data if numeric enum order is persisted. Mitigation: verify serialization format; use append-only values or explicit stable IDs.

- **UI layout/name mismatch**  
  Existing Character Info UXML may use placeholder or PC-derived names that do not match canonical slots. Mitigation: create a small binding map from slot enum to UI element names and test it.

- **Blast radius to services/tests**  
  Equipment categories may be shared by inventory, item tooltip, GM tools, or tests. Mitigation: keep mapping changes narrow and add regression tests for existing equipment slots.

- **Art/resource uncertainty**  
  Slot frames may already exist; if not, resolving PC art incorrectly can introduce Chinese or wrong-client assets. Mitigation: no new art unless necessary; if necessary, use the resource resolver workflow and verify Vietnamese labels.

## Rollback

The change should be structured so rollback is safe:

- Domain additions are additive mappings and slot definitions; reverting removes accessory binding without touching unrelated item data.
- UI binding changes should only affect Character Info accessory slot population; reverting restores placeholder/empty behavior.
- No persistent migration should be required in this proposal. If implementation discovers a save migration is unavoidable, it must be split into an explicit design decision before apply.

## Success Criteria

- `PlayerEquipSlot` (or equivalent) has canonical Mask, Amulet, Charm, and Trinket slots with stable display labels.
- `PcItemCategory` mapping can resolve accessory item categories to the correct slot(s), with tests covering each target accessory slot.
- `PlayerEquipmentService` can expose equipped accessory items by canonical slot without UI-specific logic.
- Character Info / equipment UI renders currently equipped Mask/Amulet/Charm/Trinket items and shows a safe empty state when absent.
- Existing non-accessory equipment behavior continues to pass tests.
- No socket/equip/unequip gameplay is introduced in this slice.

## Proposal Question Round — RESOLVED

Grounded recon (`harness/planning/scout-accessory-equipment-slots.md`) found the real
structure differs from the original assumptions: three distinct enums exist
(`PcItemCategory`, `PlayerEquipSlot` = visual, `EquipSlot` = gameplay in
`InventoryService._equipped`); the Character Info paperdoll is built procedurally in C#
(`CharacterInfoPaperdoll.cs`) and already DECLARES 12 slots (incl. mask/amulet/charm/
trinket) but only 4 visual slots are bound today; PC source-of-truth is
`GameDataDef.h` `ITEM_PART` / `EQUIPDETAILTYPE`. Four product decisions were raised and
resolved with the user. These are binding inputs for spec/design:

1. **Slot "Ngọc Bội" (charm) — no native PC file.**
   DECISION: bind it as a **second Trinket** sourced from `shipin.txt` (DetailType 14,
   equip_shipin). Two ornament slots share one PC data source. Keeps the "Ngọc Bội" /
   "Bội Kiện" ornament family together.
2. **Ring cardinality — PC `itempart_ring1`+`itempart_ring2` vs current single slot.**
   DECISION: **two rings (Ring 1 + Ring 2)** for full PC parity. This extends the
   gameplay `EquipSlot` enum and the paperdoll slot list.
3. **PC naming collision — `amulet.txt` is Necklace ("Liên") but the paperdoll slot named
   "amulet" is labeled "Hộ Thân Phù" (= sachet/pendant).**
   DECISION: **rename slot identifiers to match PC semantics.** `amulet` slot → bound to
   `amulet.txt` as Necklace; the sachet slot → `pendant` bound to `pendant.txt`. Vietnamese
   display labels follow PC category meaning. (More identifier churn, chosen for clarity.)
4. **Pendant loader bug — `PcItemBatchLoader` fallback detailType for pendant = 7 (collides
   with helm) instead of PC-correct 9.**
   DECISION: **fix 7 → 9 in this slice** because it directly blocks pendant classification.
   This touches shared loader code → full EditMode suite must pass before push.

Note: empty-slot display, tooltip-on-tap, and save migration remain as stated above (stable
empty frames; details deferred to item-tooltip work; no save migration needed since
equipment is not currently serialized).

## Implementation Forecast

Likely one bounded apply slice if existing model/service seams are clean. Split into two commits/PR slices if the diff approaches the 400-line review budget:

1. Domain/service slice: slot/category mapping plus tests.
2. UI binding slice: Character Info binding plus UI tests.

No code should be written until the spec/design phases confirm PC mapping details and save-state behavior.
