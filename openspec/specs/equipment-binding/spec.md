# Equipment Binding Specification

> Change: `bind-accessory-equipment-slots` · Domain: **equipment-binding** (new domain — no
> canonical spec existed under `openspec/specs/`; this is written as a full domain spec).
> Source of truth: PC `/var/www/jx-pc` `GameDataDef.h` `ITEM_PART` +
> `EQUIPDETAILTYPE`, and item text tables under `settings/item/004`
> (`amulet.txt`, `pendant.txt`, `mask.txt`, `shipin.txt`, `ring.txt`).
> `default_locale: vi`. This slice is **binding/render only** (no equip/unequip/socket logic).

## Purpose

Establish accessory equipment slots (Mask, Pendant/sachet, Necklace, Ring ×2, Trinket ×2)
as first-class canonical data concepts and bind currently equipped accessory items into the
Character Info paperdoll, so a reliable data contract exists before active
equip/unequip/socket gameplay is built.

## PC reference mapping (binding for all requirements)

| Slot (canonical) | PC source file | PC `EQUIPDETAILTYPE` | Vietnamese label |
|---|---|---|---|
| Necklace | `amulet.txt` | `equip_amulet` = 4 | Liên / Dây Chuyền |
| Ring1 | `ring.txt` | `equip_ring` = 3 | Nhẫn |
| Ring2 | `ring.txt` | `equip_ring` = 3 | Nhẫn |
| Mask | `mask.txt` | `equip_mask` = 11 | Mặt Nạ |
| Pendant | `pendant.txt` | `equip_pendant` = 9 | Hộ Thân Phù |
| Trinket (Bội Kiện) | `shipin.txt` | `equip_shipin` = 14 | Bội Kiện |
| Trinket2 (Ngọc Bội) | `shipin.txt` | `equip_shipin` = 14 | Ngọc Bội |

> The PC `amulet.txt` file is the Necklace category (`equip_amulet=4`, "Liên"). The
> paperdoll slot previously keyed `amulet` was mislabeled "Hộ Thân Phù" (sachet semantics);
> per the resolved decision, slot identifiers follow PC semantics: the `necklace` slot binds
> `amulet.txt`; the sachet slot (`pendant`) binds `pendant.txt`. The two ornament slots
> (Bội Kiện + Ngọc Bội) both source `shipin.txt` (`equip_shipin=14`).

## Requirements

### Requirement: Canonical Gameplay Equipment Slots

The system MUST expose a canonical gameplay equipment-slot enum (`EquipSlot`) that, in
addition to the existing slots (Weapon, Helmet, Armor, Boots, Necklace, Ring, Mount),
supports Necklace, Ring1, Ring2, Mask, Pendant, Trinket, and Trinket2 as distinct,
addressable slots. New enum members MUST be appended (append-only numeric ordering) so
existing integer values are unchanged and serialized state (none currently, per
`PcSaveSlotService`) is not corrupted.

#### Scenario: Accessory slots are distinct and addressable

- GIVEN the `EquipSlot` gameplay enum
- WHEN a consumer enumerates equipment slots
- THEN the enum contains distinct members for Necklace, Ring1, Ring2, Mask, Pendant, Trinket, and Trinket2
- AND each is independently addressable (no two accessory concepts share one slot)

#### Scenario: Existing slot integer values are stable

- GIVEN the existing `EquipSlot` values (Weapon=0, Helmet=1, Armor=2, Boots=3, Necklace=4, Ring=5, Mount=6)
- WHEN new accessory members are added
- THEN no existing member's numeric value changes
- AND `EquipSlot.Ring` remains usable as the primary ring (Ring1) without renumbering existing values

### Requirement: Two Ring Slots

The gameplay equipment model MUST distinguish two ring slots (Ring1 and Ring2) for PC parity
with `ITEM_PART` `itempart_ring1` / `itempart_ring2`, and the Character Info paperdoll MUST
present both ring slots.

#### Scenario: Two distinct ring slots exist in gameplay model

- GIVEN the `EquipSlot` enum
- WHEN ring slots are queried
- THEN exactly two distinct ring slots exist (Ring1 and Ring2)
- AND both can hold an item independently of the other

#### Scenario: Paperdoll shows two ring slots

- GIVEN the Character Info paperdoll slot list
- WHEN the paperdoll is built
- THEN two ring slots are present, each labeled with the Vietnamese ring name

### Requirement: PC Item Category and Detail-Type Classification

`PcItemCategory` MUST include accessory categories (Mask, Pendant, Trinket/Ornament) beyond
the existing Necklace/Ring categories, and the system MUST provide a classification that maps
PC `EQUIPDETAILTYPE` codes to `PcItemCategory` covering at least ring (D3), amulet/necklace
(D4), pendant/sachet (D9), mask (D11), and shipin/ornament (D14). The classification MUST be
the single source of truth consulted by the UI (the UI MUST NOT re-derive category meaning).

#### Scenario: DetailType resolves to correct category

- GIVEN the DetailType-to-category classification
- WHEN DetailType 3, 4, 9, 11, and 14 are classified
- THEN they resolve to Ring, Necklace, Pendant, Mask, and Trinket (Ornament) respectively

#### Scenario: Accessory categories are equippable

- GIVEN `PcItemCategory` accessory categories Mask, Pendant, and Trinket
- WHEN equippability is queried for each
- THEN each is marked equippable
- AND each carries a Vietnamese slot name matching its PC category meaning

### Requirement: Pendant Loader Detail-Type Correctness

`PcItemBatchLoader` MUST assign the PC-correct fallback detailType for the pendant stem.
The fallback for `pendant` MUST be 9 (`equip_pendant`), NOT 7 (`equip_helm`). This corrects
the existing collision where pendant items were classified into the helm slot.

#### Scenario: Pendant fallback is equip_pendant (9)

- GIVEN `PcItemBatchLoader` applying fallback detailType for a pendant row whose parser did not set detailType
- WHEN the fallback is applied
- THEN the pendant item's detailType is 9
- AND it is NOT 7 (which collides with helm)

#### Scenario: Pendant no longer classifies as helm

- GIVEN a pendant item imported from `pendant.txt`
- WHEN it is classified by detailType
- THEN it classifies as Pendant (Hộ Thân Phù), not Helmet (Mũ)

> Implementation note (scout-verified): `PcItemBatchLoader.ApplyCategoryIds` currently has
> `"pendant" => 7` with a stale comment claiming the fix was applied. This touches shared
> loader code → the full EditMode suite is a required push gate.
    
### Requirement: PC Mask Item Parser
    
The runtime item database MUST parse PC `mask.txt` rows into `ItemDefinition` objects using
the established 46-column PC equipment item format. Parsed mask items MUST keep PC identity
columns (`ItemGenre`, `DetailType`, `ParticularType`) so DetailType 11 remains classifiable
as Mask and `ParticularType=0` remains a valid PC key.
    
#### Scenario: Mask parser reads item identity and detail type
    
- GIVEN a valid PC mask row with `ItemGenre=0`, `DetailType=11`, and `ParticularType=0`
- WHEN the mask parser parses the row
- THEN the resulting item keeps `itemGenre=0`, `detailType=11`, and `particularType=0`
- AND the item has non-empty raw/normalized Vietnamese name fields
- AND icon source id is derived from the PC SPR path column
    
### Requirement: PC Shipin Item Parser
    
The runtime item database MUST parse PC `shipin.txt` rows into `ItemDefinition` objects using
the established 46-column PC equipment item format. Parsed shipin/trinket items MUST keep PC
identity columns (`ItemGenre`, `DetailType`, `ParticularType`) so DetailType 14 remains
classifiable as Trinket/ornament.
    
#### Scenario: Shipin parser reads item identity and detail type
    
- GIVEN a valid PC shipin row with `ItemGenre=0`, `DetailType=14`, and `ParticularType=0`
- WHEN the shipin parser parses the row
- THEN the resulting item keeps `itemGenre=0` and `detailType=14`
- AND it has non-empty raw/normalized Vietnamese name fields
- AND icon source id is derived from the PC SPR path column
    
### Requirement: Batch Loader Includes Mask and Shipin
    
`PcItemBatchLoader.LoadAll` MUST include `mask` and `shipin` in the per-file load result and
imported item bundle so the canonical Mask and Trinket slots have runtime item definitions
available from PC source data.
    
#### Scenario: Reference batch loads sixteen item files
    
- GIVEN the reference PC item sample directory
- WHEN `PcItemBatchLoader.LoadAll` is called
- THEN `perFileCounts` contains 16 keys including `mask` and `shipin`
- AND both new keys have at least five parsed rows
    
### Requirement: Mask ParticularType Zero Is Preserved
    
`PcItemBatchLoader.ApplyCategoryIds` MUST NOT rewrite `particularType=0` for mask rows
because PC `mask.txt` uses zero as a valid unique particular type.
    
#### Scenario: Mask row zero remains zero
    
- GIVEN a mask item with `detailType=11` and `particularType=0`
- WHEN category IDs are applied for stem `mask`
- THEN the item keeps `particularType=0`
    
### Requirement: Shipin Rows Remain Importable Despite Repeated Zero
    
`PcItemBatchLoader.ApplyCategoryIds` MUST keep the existing zero-to-row-index fallback for
shipin rows because sample PC `shipin.txt` rows repeat `ParticularType=0` and importer tuple
keys must remain unique.
    
#### Scenario: Shipin zero receives row index fallback
    
- GIVEN two shipin items with `detailType=14` and `particularType=0`
- WHEN category IDs are applied for stem `shipin`
- THEN their resulting `particularType` values differ and can be imported without tuple collision
    
### Requirement: InventoryService Equipped-Item Lookup by Slot

`InventoryService` MUST expose the currently equipped `ItemDefinition` per canonical
equipment slot (including all accessory slots) via a read by slot. Reading an unequipped
slot MUST return a safe empty state and MUST NOT throw or collapse UI layout.

#### Scenario: Equipped accessory item is readable by slot

- GIVEN an accessory item equipped into a canonical slot (e.g. Mask)
- WHEN the equipped item for that slot is read
- THEN the equipped `ItemDefinition` for that slot is returned

#### Scenario: Unequipped slot returns safe empty state

- GIVEN a canonical slot with no equipped item
- WHEN the equipped item for that slot is read
- THEN a safe empty/null state is returned
- AND no exception is thrown
- AND (for UI consumers) the slot retains its frame and does not collapse the layout

### Requirement: CharacterInfoPaperdoll Binds All Slots to Equipped State

The Character Info paperdoll MUST bind every slot (helmet, weapon, armor, mount, belt, boots,
necklace, ring1, ring2, mask, pendant, trinket, trinket2) to real equipped state sourced from
`InventoryService`, replacing the current display-only/`framework` behavior for the newly
bound accessory slots. A slot with an equipped item MUST be marked equipped; an unequipped
slot MUST keep the empty/`framework` affordance. Slot Vietnamese labels MUST follow PC
category semantics.

#### Scenario: Equipped accessory slot is marked equipped

- GIVEN a mask item equipped in the Mask slot
- WHEN the paperdoll is built against the inventory/equipment state
- THEN the mask slot is marked equipped (carries the `equipped` class)
- AND its Vietnamese label is "Mặt Nạ"

#### Scenario: Unequipped accessory slot keeps empty affordance

- GIVEN the Pendant slot with no equipped item
- WHEN the paperdoll is built
- THEN the pendant slot keeps the empty/`framework` class and remains visible
- AND no exception is thrown

#### Scenario: Slot identifiers follow PC semantics

- GIVEN the paperdoll slot roster
- WHEN slot sources are resolved
- THEN the necklace slot binds `amulet.txt` (DetailType 4, "Liên")
- AND the pendant slot binds `pendant.txt` (DetailType 9, "Hộ Thân Phù")
- AND the mask slot binds `mask.txt` (DetailType 11, "Mặt Nạ")
- AND the two ornament slots (Bội Kiện, Ngọc Bội) both bind `shipin.txt` (DetailType 14)

### Requirement: Non-Accessory Visual Binding Regression Guard

The existing visual equipment bindings (helmet→Head, weapon→Weapon, armor→Body,
mount→Mount via `PlayerEquipmentService`) MUST continue to function unchanged after this
change.

#### Scenario: Visual slots still bind after change

- GIVEN the four previously-bound visual slots (helmet, weapon, armor, mount)
- WHEN the paperdoll is built with those slots equipped
- THEN each is still marked equipped exactly as before the change

### Requirement: Binding and Render Only (No Gameplay Logic)

This slice MUST NOT introduce equip, unequip, or socket interaction logic. It only
establishes the data contract and renders/binds current equipped state. The `Đính` (socket)
affordance remains a later behavior slice.

#### Scenario: No equip/unequip/socket logic added

- GIVEN this change's diff
- WHEN reviewed
- THEN no new equip-to-slot, unequip, or socket gameplay action is introduced
- AND only slot/category classification, lookup, and UI binding/render code is present

### Requirement: Vietnamese Slot Labels

All user-facing equipment slot names in the Character Info UI MUST be Vietnamese and MUST
match PC category meaning.

#### Scenario: All slot labels are Vietnamese

- GIVEN the paperdoll slot roster
- WHEN slot labels are rendered
- THEN every slot label is a Vietnamese string
- AND accessory labels match: Mask="Mặt Nạ", Pendant="Hộ Thân Phù", Necklace="Liên", Ring="Nhẫn", Trinket="Bội Kiện", Trinket2="Ngọc Bội"

### Requirement: Test Categorization and Run Discipline

Tests added by this change MUST use EditMode `[Category(...)]` (e.g. an `Equipment` category
or an extension of an existing equipment-related category) so they can be run via the
`category_names` filter in the dev loop. The full EditMode suite is only required as a
push gate (especially because the pendant loader fix touches shared code).

#### Scenario: New tests are category-filterable

- GIVEN the new equipment tests
- WHEN run via `run_tests(mode="EditMode", category_names=["Equipment"])`
- THEN the equipment tests execute and pass without running the entire suite

---

## skill_resolution

`paths-injected` — `jx-pc-port-rule/SKILL.md` and `jx-pc-resource-resolver/SKILL.md` were
read before writing this spec. No independent skill discovery was performed.

## Open Design Questions (for the design phase)

1. **`EquipSlot` ring naming:** Keep the existing `Ring` member as Ring1 (append `Ring2`),
   or rename `Ring`→`Ring1`? Spec requires two distinct ring slots either way; the rename
   trade-off (churn vs. clarity) is a design call.
2. **Category classification surface:** Implement the DetailType→`PcItemCategory` resolver as
   a new method on `EquipmentSlotMappingService` (e.g. `DetailTypeToCategory`), or extend the
   existing `ItemTypeToCategory`? Design to choose the seam; spec only requires the mapping
   exists and is the single source of truth.
3. **Paperdoll binding input:** `CharacterInfoPaperdoll.Build` currently takes
   `PlayerEquipmentService` (visual). Binding accessory gameplay state requires an
   `InventoryService` (or equipped-state view) input. Design must decide how the paperdoll
   receives both visual and gameplay equipped state without coupling.
4. **Cuff/bracers (DetailType 8):** PC `cuff.txt` is loaded by `PcItemBatchLoader` but has no
   paperdoll slot. Confirm cuff stays out of scope this slice (no slot to bind).
5. **Identifier churn blast radius:** Renaming the paperdoll slot key `amulet`→`pendant`
   (and adding `trinket2`) may affect tests/GM code that reference `Slot_amulet`/
   `Slot_charm`. Design must enumerate references before renaming.
