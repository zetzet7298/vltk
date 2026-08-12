# Design — Bind Accessory Equipment Slots

> Change: `bind-accessory-equipment-slots` · Binding decisions from
> `proposal.md` (RESOLVED round) + `spec.md`. Ground-truth from direct source reads.
> `skill_resolution`: `paths-injected` (`jx-pc-port-rule`, `jx-pc-resource-resolver`).

## Context Recap (ground-truth verified)

Three distinct enums exist in the codebase:

| Enum | Location | Members | Purpose |
|---|---|---|---|
| `EquipSlot` | `InventoryService.cs` | Weapon=0…Mount=6 (7) | **Gameplay** equipped-item dict key |
| `PlayerEquipSlot` | `PlayerEquipmentService.cs` | Body/Head/Hair/Weapon/Offhand/Mount (6) | **Visual** SPR-layer variant selection |
| `PcItemCategory` | `EquipmentSlotMappingService.cs` | Weapon=1…Currency=12 (12) | PC ItemType classification |

The `CharacterInfoPaperdoll` is **procedural C#** (not UXML). It declares 12 `PaperdollSlot`s
but only binds 4 visual slots via `PlayerEquipmentService.IsEquipped`. The rest carry the
`framework` CSS class. `Build(container, PlayerEquipmentService)` takes only visual state.

`PcItemBatchLoader.ApplyCategoryIds` has pendant fallback = **7** (collides with helm) with a
stale comment claiming it was fixed. PC `EQUIPDETAILTYPE` defines `equip_pendant = 9`.

Equipment is **not serialized** (`PcSaveSlotService` persists inventory IDs only) → append-only
enum extension is save-safe.

---

## Data Flow

```
PC ItemList.txt DetailType column
      │
      ▼
PcItemBatchLoader.ApplyCategoryIds  (pendant 7→9 fix in this slice)
      │
      ▼
ItemDefinition.detailType  ──►  EquipmentSlotMappingService.DetailTypeToCategory(int)
  PC EQUIPDETAILTYPE:                D3→Ring, D4→Necklace, D9→Pendant,
  ring=3 amulet=4 pendant=9          D11→Mask, D14→Trinket (+fallback)
  mask=11 shipin=14
      │
      ▼
PcItemCategory enum (+Mask=13, Pendant=14, Trinket=15 appended)
      │
      ▼
InventoryService._equipped<EquipSlot, ItemDefinition>
  EquipSlot enum (+Ring2=7, Mask=8, Pendant=9, Belt=10, Trinket=11, Trinket2=12 appended)
      │  .Equipped  (IReadOnlyDictionary<EquipSlot, ItemDefinition>)
      ▼
CharacterInfoContent  ──────►  CharacterInfoPaperdoll.Build(container, equipment, equippedItems)
  passes BOTH:                    per PaperdollSlot:
  · PlayerEquipmentService          visual?    → equipment.IsEquipped()
  · equippedItems dict snapshot     gameplay?  → dict.ContainsKey(slot)
                                     neither?  → "framework"
      │
      ▼
VisualElement cells: USS "equipped" | "empty" | "framework"
```

---

## Design Decisions (resolving spec open questions)

### DQ-1: EquipSlot ring naming — append-only, keep `Ring` as Ring1

Keep existing `Ring = 5` unchanged. Append `Ring2 = 7` (after Mount=6). No existing integer
shifts (spec mandates stable values). The `_equipped` dictionary keys by `EquipSlot`, so
`EquipSlot.Ring` and `EquipSlot.Ring2` are independent dictionary entries — no keying conflict.

**Rationale:** Renaming `Ring`→`Ring1` would shift value 5 and break any persisted/internally-
cached references. Append-only is zero-risk and the spec explicitly requires stable values.

### DQ-2: Category classification — new `DetailTypeToCategory` method

Add a **new static method** `DetailTypeToCategory(int detailType)` on
`EquipmentSlotMappingService`, **separate from** the existing `ItemTypeToCategory(int)`.
The two axes are distinct: `ItemTypeToCategory` maps PC ItemType codes (1–12); the new
method maps PC `EQUIPDETAILTYPE` codes (0–16).

```
DetailTypeToCategory switch:
  3  → PcItemCategory.Ring        (equip_ring)
  4  → PcItemCategory.Necklace    (equip_amulet — PC amulet.txt IS necklace)
  9  → PcItemCategory.Pendant     (equip_pendant)
  11 → PcItemCategory.Mask        (equip_mask)
  14 → PcItemCategory.Trinket     (equip_shipin)
  _  → PcItemCategory.Material    (fallback, same as ItemTypeToCategory default)
```

**New `PcItemCategory` members appended (values 13–15):**
- `Mask = 13` — "Mặt Nạ"
- `Pendant = 14` — "Hộ Thân Phù"
- `Trinket = 15` — "Bội Kiện" / "Ngọc Bội"

Three new `EquipmentSlotMapping` entries in the `Mappings` dictionary (all `isEquippable = true`,
`maxStackSize = 1`, matching existing equippable items).

**Rationale:** `ItemTypeToCategory` is consumed by inventory/tooltip paths and should not be
polluted with a second axis. A dedicated `DetailTypeToCategory` is the single source of truth
for slot-classification, matching the spec's "UI MUST NOT re-derive category meaning" requirement.

### DQ-3: Paperdoll binding input — dual binding via struct extension + optional dict param

**`PaperdollSlot` struct gains a second optional slot binding:**

```
public readonly struct PaperdollSlot
{
    public readonly string key;
    public readonly string labelVi;
    public readonly EquipSlot? gameplaySlot;       // NEW — gameplay equipped-item binding
    public readonly PlayerEquipSlot? equipmentSlot; // existing — visual SPR variant binding
}
```

**`Build()` signature change:**

```
public static void Build(
    VisualElement container,
    PlayerEquipmentService equipment,
    IReadOnlyDictionary<EquipSlot, ItemDefinition> equippedItems = null)  // NEW optional
```

**Binding logic per slot (USS class decision):**

```
1. If slot.equipmentSlot set AND equipment.IsEquipped → "equipped"     (unchanged visual path)
2. Else if slot.gameplaySlot set AND equippedItems.ContainsKey → "equipped"  (new gameplay path)
3. Else if slot.equipmentSlot OR slot.gameplaySlot set → "empty"
4. Else → "framework"
```

Visual binding (helmet/weapon/armor/mount) is checked **first**, preserving the exact existing
behavior (regression guard). Gameplay binding is the fallback for accessory slots.

**`CharacterInfoContent` constructs the view:**
- Constructor gains an optional `InventoryService inventory` (or `Func<IReadOnlyDictionary<EquipSlot, ItemDefinition>>` snapshot provider).
- `BuildEquipmentTab()` and `OnShow()` pass `inventory?.Equipped` as the third `Build()` argument.
- If `inventory` is null (e.g. GM sandbox without inventory), `equippedItems` is null → gameplay
  slots show "empty", visual slots still work. **Never throws.**

**Rationale:** Using `IReadOnlyDictionary<EquipSlot, ItemDefinition>` (already exposed by
`InventoryService.Equipped`) avoids creating a new interface while keeping the paperdoll
decoupled from `InventoryService` internals. The optional parameter makes the change backward-
compatible with existing test constructors that pass only `PlayerEquipmentService`.

### DQ-4: Cuff/bracers — OUT of scope

PC `cuff.txt` (DetailType 8, `equip_cuff`) is loaded by `PcItemBatchLoader` but has **no
paperdoll slot**. The paperdoll layout (12 slots) does not include a cuff/bracer position.
Cuff stays unbound. No `EquipSlot.Cuff` is added. **Confirmed out of scope.**

### DQ-5: Identifier churn blast radius

**Slot key renames in `CharacterInfoPaperdoll.Slots[]`:**

| Old key | New key | Label (unchanged) | Reason |
|---|---|---|---|
| `amulet` | `pendant` | "Hộ Thân Phù" | Was mislabeled; binds `pendant.txt` (D9) |
| `charm` | `trinket2` | "Ngọc Bội" | Second ornament slot per decision 1 |

**New slot added:** `ring2` — "Nhẫn" (second ring per decision 2).

**Existing slot keys unchanged:** helmet, mask, weapon, armor, belt, necklace, boots, mount,
ring (→ ring1 conceptually, key stays `ring`), trinket.

**Files that MUST update in lockstep (highest-risk area):**

| File | Impact | Detail |
|---|---|---|
| `Assets/Scripts/UI/CharacterInfo/CharacterInfoPaperdoll.cs` | Edit `Slots[]` + struct + `Build()` | Rename 2 keys, add ring2, add gameplaySlot bindings |
| `Assets/Scripts/UI/CharacterInfo/CharacterInfoContent.cs` | Constructor + `BuildEquipmentTab` + `OnShow` | Pass equippedItems dict to `Build()` |
| `Assets/Tests/EditMode/UI/CharacterInfoContentTests.cs` | **BREAKS — must migrate** | See below |

**Specific test breakage in `CharacterInfoContentTests.cs`:**

1. **`Paperdoll_HasReferenceSlotCount`** — queries `body.Q("Slot_amulet")` and
   `body.Q("Slot_charm")` → these element names no longer exist after rename.
   **Fix:** assert `Slot_pendant` and `Slot_trinket2` instead; add `Slot_ring2`.

2. **`Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty`** — asserts
   `ringCell.ClassListContains("framework")`. After this change, ring has `gameplaySlot=Ring`
   so it becomes `"empty"` (not `"framework"`) when unequipped.
   **Fix:** assert `"empty"` for ring; add positive assertions for pendant/trinket/mask showing
   `"empty"` when nothing equipped.

3. **Constructor calls** — `new CharacterInfoContent(null, () => null)` still compiles (inventory
   param optional), but tests that want to verify gameplay binding must pass an `InventoryService`.

**No other files reference the paperdoll slot keys or element names** (verified by reading
`CharacterInfoContent.cs`, `CharacterInfoPaperdoll.cs`, and the test file — the paperdoll is
self-contained procedural C#). The GM items tab drives `InventoryService` by `EquipSlot` enum
members (all existing values stable).

**`EquipSlot` enum extension blast radius:** `InventoryServiceTests.cs` uses `EquipSlot.Weapon`,
`.Helmet`, `.Ring` — all existing members with unchanged values → **no breakage**.
`PlayerEquipmentChangeTests.cs` uses `PlayerEquipSlot` (visual enum), not `EquipSlot` → **no
breakage**.

---

## Pendant Loader Fix (7→9)

**File:** `Assets/Scripts/Sandbox/ItemData/PcItemBatchLoader.cs`, method `ApplyCategoryIds`.

**Current (buggy):**
```
"pendant" => 7,  // Ngọc bội   ← WRONG: collides with equip_helm=7
```
Note the stale comment says "Ngọc bội" (charm) — another confusion layer.

**Fix:**
```
"pendant" => 9,  // equip_pendant (Hộ Thân Phù) — PC GameDataDef.h EQUIPDETAILTYPE
```

Also update the header comment block that currently reads `pendant.txt=7(after fix)` to
`pendant.txt=9`.

**Shared-code gate:** This method is on the item-loading hot path consumed by
`PcItemBatchLoader.LoadAll` / `ImportInto` which feeds the entire item database. The full
EditMode suite (4076 tests) is a **required pre-push gate** after this change (project rule:
shared loader code → full suite).

---

## Slot Binding Matrix (final)

| PaperdollSlot key | Label VI | EquipSlot (gameplay) | PlayerEquipSlot (visual) | PC source |
|---|---|---|---|---|
| `helmet` | Mũ | Helmet=1 | Head | helm.txt (D7) |
| `mask` | Mặt Nạ | Mask=8 | — | mask.txt (D11) |
| `pendant` | Hộ Thân Phù | Pendant=9 | — | pendant.txt (D9) |
| `weapon` | Vũ Khí | Weapon=0 | Weapon | meleeweapon/rangeweapon.txt |
| `armor` | Giáp | Armor=2 | Body | armor.txt (D2) |
| `belt` | Đai Lưng | Belt=10 | — | belt.txt (D6) |
| `ring` | Nhẫn | Ring=5 | — | ring.txt (D3) |
| `ring2` | Nhẫn | Ring2=7 | — | ring.txt (D3) |
| `necklace` | Liên | Necklace=4 | — | amulet.txt (D4) |
| `boots` | Giày | Boots=3 | — | boot.txt (D5) |
| `mount` | Ngựa | Mount=6 | Mount | horse.txt (D10) |
| `trinket` | Bội Kiện | Trinket=11 | — | shipin.txt (D14) |
| `trinket2` | Ngọc Bội | Trinket2=12 | — | shipin.txt (D14) |

13 slots total (was 12; added `ring2`). Belt gets `EquipSlot.Belt=10` (append-only) so it can
bind gameplay state (spec lists belt in the "bind every slot" requirement).

---

## USS Class Strategy

**Unchanged:** `equipped`, `empty`, `framework` CSS classes. New accessory slots reuse them:
- Equipped accessory → `equipped`
- Unequipped accessory (has binding) → `empty`
- No applicable — all slots now have at least a gameplay binding, so `framework` may disappear
  entirely (belt/boots/necklace/ring previously `framework` now have gameplay slots → `empty`
  when unequipped). This is expected and tested.

**Empty-slot safety:** `Build()` checks `equippedItems?.ContainsKey(...)` with null-conditional;
null dict → all gameplay slots show `empty`. **Never throws, never collapses layout.**

---

## Vietnamese Labels

Stored in the `PaperdollSlot.labelVi` constructor field (already exists). No change to storage
mechanism. New/migrated labels match the spec mapping table exactly.

---

## Test Plan

**New test file:** `Assets/Tests/EditMode/Sandbox/EquipmentBindingTests.cs`
**Category:** `[TestFixture, Category("Equipment")]`

| Test | Covers |
|---|---|
| `DetailTypeToCategory_Ring_ReturnsRing` | D3→Ring |
| `DetailTypeToCategory_Necklace_ReturnsNecklace` | D4→Necklace |
| `DetailTypeToCategory_Pendant_ReturnsPendant` | D9→Pendant |
| `DetailTypeToCategory_Mask_ReturnsMask` | D11→Mask |
| `DetailTypeToCategory_Trinket_ReturnsTrinket` | D14→Trinket |
| `PcItemCategory_NewMembers_AreEquippable` | Mask/Pendant/Trinket in Mappings |
| `EquipSlot_NewMembers_Appended_AfterMount` | Ring2=7…Trinket2=12 stable |
| `EquipSlot_ExistingValues_Unchanged` | Weapon=0…Mount=6 unchanged |
| `PcItemBatchLoader_PendantFallback_IsNine` | pendant→9 not 7 |
| `PcItemBatchLoader_Pendant_NotClassifiedAsHelm` | D9≠D7 |
| `InventoryService_GetEquipped_Empty_ReturnsNull` | safe empty state |
| `InventoryService_Equipped_AccessorySlot_Readable` | equipped item per slot |
| `InventoryService_Equip_Ring2_IndependentFromRing` | two ring slots independent |

**Migrated test file:** `Assets/Tests/EditMode/UI/CharacterInfoContentTests.cs`

| Migrated test | Change |
|---|---|
| `Paperdoll_HasReferenceSlotCount` | `Slot_amulet`→`Slot_pendant`, `Slot_charm`→`Slot_trinket2`, add `Slot_ring2` |
| `Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty` | ring assertion `framework`→`empty`; add accessory empty checks |
| (new) `Paperdoll_GameplaySlot_Equipped_ShowsEquippedClass` | inject InventoryService with mask equipped → mask cell has `equipped` |
| (new) `Paperdoll_TwoRings_BothPresent` | `Slot_ring` and `Slot_ring2` both exist |

**Run discipline:** `run_tests(mode="EditMode", category_names=["Equipment"])` in dev loop.
Full suite (4076) only as pre-push gate (pendant fix touches shared loader).

---

## Review Workload Forecast — 2-PR Split

### PR-1: Additive domain + loader fix + tests (~190 lines)

| File | Change | Lines |
|---|---|---|
| `EquipmentSlotMappingService.cs` | +3 enum members, +3 Mappings entries, +15-line `DetailTypeToCategory` | ~25 |
| `InventoryService.cs` | +6 enum members (Ring2…Trinket2), +`GetEquipped`/`IsSlotEquipped` helpers | ~15 |
| `PcItemBatchLoader.cs` | Fix pendant 7→9 + comment update | ~3 |
| `EquipmentBindingTests.cs` (new) | 13 tests covering mapping/enum/loader/inventory | ~150 |

**Safe revert:** PR-1 is purely additive (new enum members, new method, loader fix). Reverting
restores exact prior behavior. No UI change. Pendant fix is the only behavior change but it
corrects a bug (7→9).

### PR-2: UI paperdoll bind + identifier rename + fixture migration (~170 lines)

| File | Change | Lines |
|---|---|---|
| `CharacterInfoPaperdoll.cs` | Struct +gameplaySlot, Slots[] rename/add, Build() +param +logic | ~45 |
| `CharacterInfoContent.cs` | Constructor +inventory, pass equippedItems | ~15 |
| `CharacterInfoContentTests.cs` (migrate) | 2 tests renamed, 2 new tests | ~30 |
| Paperdoll binding tests (in EquipmentBindingTests or separate) | gameplay binding + two rings | ~80 |

**Safe revert:** PR-2 revert restores the old 12-slot framework paperdoll. No data migration.

**Total: ~360 lines across 2 PRs, each under the 400-line review budget.**

Split boundary: PR-1 is the domain contract (enums, mapping, loader, read API). PR-2 is the UI
consumer. PR-1 can be reviewed/merged independently; PR-2 depends on PR-1 but is independently
revertible.

---

## skill_resolution

`paths-injected` — `harness/.pi/skills/jx-pc-port-rule/SKILL.md` and
`harness/.pi/skills/jx-pc-resource-resolver/SKILL.md` were read before writing this design. PC
source mapping (`EQUIPDETAILTYPE` values) verified via the scout handoff which read
`GameDataDef.h` directly. No SPR/PAK art resolution was needed (this slice reuses existing slot
frames; no new art).

## Residual Risks

1. **No `PcMaskParser`/`PcShipinParser` exist.** mask.txt and shipin.txt are NOT loaded by
   `PcItemBatchLoader` (no parsers, no `CategoryStems` entries). This means no actual mask/shipin
   items exist in the runtime item database. The `DetailTypeToCategory` mapping is correct, but
   binding will show `empty` for these slots until parsers are added (follow-up change). The
   pendant/amulet/ring parsers DO exist, so those slots can bind real items.

2. **Belt `EquipSlot.Belt=10` is new.** No existing equip code path handles Belt. In this
   binding-only slice, Belt shows `empty` (nothing equipped). The `InventoryService.Equip()` for
   Belt would just store `_equipped[Belt] = item` with no visual dispatch (same as current
   Necklace/Ring/Boots behavior). This is safe but belt equipping gameplay is deferred.

3. **`framework` class may become unused.** After binding all 13 slots, every slot has at least a
   gameplay binding, so `framework` is no longer assigned. USS still defines it (harmless). Verify
   no CSS depends on `framework` being present on specific slots.

4. **Pendant detailType comment in PcItemBatchLoader header** says `pendant.txt=7(after fix)` —
   this stale comment must be corrected to `pendant.txt=9` alongside the code fix.

5. **`PcItemCategory.Trinket=15` value collision check.** The existing `ItemTypeToCategory` switch
   maps ItemType 15 → `_ => PcItemCategory.Material` (no explicit case). Adding `Trinket=15` to
   the enum does NOT affect `ItemTypeToCategory` (which switches on ItemType integers, not enum
   values). No collision, but a defensive test should assert `ItemTypeToCategory(15)` still
   returns `Material` (not `Trinket`).
