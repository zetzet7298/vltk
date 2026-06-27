# Tasks — Bind Accessory Equipment Slots

> Change: `bind-accessory-equipment-slots` · Strict TDD (RED→GREEN→TRIANGULATE→REFACTOR).
> Project test_command: Unity EditMode via MCP `run_tests` with `category_names` filter.
> Source decisions: `proposal.md` (RESOLVED round), `spec.md`, `design.md` (DQ-1…DQ-5,
> slot binding matrix, residual risks). PC truth: `GameDataDef.h` `EQUIPDETAILTYPE`.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~360 (PR-1 ~190, PR-2 ~170) |
| 400-line budget risk | Low (each PR < 400) |
| Chained PRs recommended | Yes |
| Suggested split | PR-1 (domain+loader+tests) → PR-2 (UI bind+rename+fixture migrate) |
| Delivery strategy | auto-chain |
| Chain strategy | stacked-to-main |

```text
Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low
```

---

## PR-1 — Additive domain + loader fix + tests (independent, safe revert)

Strict TDD: write failing test first, implement to green, triangulate edge cases, refactor.
Run gate: `run_tests(mode="EditMode", category_names=["Equipment"])` in dev loop.

- [x] **T1 (RED)** — Create `Assets/Tests/EditMode/Sandbox/EquipmentBindingTests.cs` with
  `[TestFixture, Category("Equipment")]`. Add failing assertions that `PcItemCategory` has
  members `Mask`, `Pendant`, `Trinket` and that `EquipmentSlotMappingService.GetMapping` /
  `IsEquippable` return equippable for each (labels "Mặt Nạ"/"Hộ Thân Phù"/"Bội Kiện",
  `isEquippable=true`, `maxStackSize=1`). Confirm compile/RED.
- [x] **T2 (GREEN)** — In `Assets/Scripts/Sandbox/EquipmentSlotMappingService.cs`, append
  `PcItemCategory.Mask = 13`, `Pendant = 14`, `Trinket = 15` (append-only). Add 3
  `EquipmentSlotMapping` entries in `Mappings` (equippable, stack 1, VI labels). T1 → green.
- [x] **T3 (RED)** — In `EquipmentBindingTests`, add failing tests:
  `DetailTypeToCategory_Ring_ReturnsRing` (3), `_Nelace_ReturnsNecklace` (4),
  `_Pendant_ReturnsPendant` (9), `_Mask_ReturnsMask` (11), `_Trinket_ReturnsTrinket` (14),
  and a default→`Material` case. Confirm RED (method does not exist).
- [x] **T4 (GREEN)** — Add `EquipmentSlotMappingService.DetailTypeToCategory(int)` switch:
  3→Ring, 4→Necklace, 9→Pendant, 11→Mask, 14→Trinket, `_`→Material. This is the single source
  of truth (UI MUST NOT re-derive). T3 → green.
- [x] **T5 (TRIANGULATE)** — Add defensive test `ItemTypeToCategory_Int15_StillMaterial`
  asserting the existing `ItemTypeToCategory(15)` returns `Material` (guards against
  `PcItemCategory.Trinket=15` value collision with ItemType 15 fallback). Run category green.
- [x] **T6 (RED)** — In `EquipmentBindingTests`, add failing tests:
  `EquipSlot_ExistingValues_Unchanged` (Weapon=0…Mount=6) and
  `EquipSlot_NewMembers_Appended_AfterMount` (Ring2=7, Mask=8, Pendant=9, Belt=10,
  Trinket=11, Trinket2=12). Confirm RED (enum members absent).
- [x] **T7 (GREEN)** — In `Assets/Scripts/Sandbox/InventoryService.cs`, append `EquipSlot`
  members `Ring2=7, Mask=8, Pendant=9, Belt=10, Trinket=11, Trinket2=12` after `Mount=6`.
  Existing values MUST NOT shift. T6 → green.
- [x] **T8 (RED)** — In `EquipmentBindingTests`, add failing tests:
  `InventoryService_GetEquipped_Empty_ReturnsNull`, `InventoryService_Equipped_AccessorySlot_Readable`
  (equip into Mask, read it back), `InventoryService_Equip_Ring2_IndependentFromRing`
  (equip Ring2, assert Ring stays empty). Confirm RED (helpers absent).
- [x] **T9 (GREEN)** — In `InventoryService.cs`, expose `Equipped` as
  `IReadOnlyDictionary<EquipSlot, ItemDefinition>` (if not already) and add
  `GetEquipped(EquipSlot) -> ItemDefinition` (null if empty) and `IsSlotEquipped(EquipSlot) -> bool`.
  Safe empty state: never throws. T8 → green.
- [x] **T10 (RED)** — In `EquipmentBindingTests`, add failing tests:
  `PcItemBatchLoader_PendantFallback_IsNine` and `PcItemBatchLoader_Pendant_NotClassifiedAsHelm`
  (D9≠D7). Confirm RED (current fallback = 7).
- [x] **T11 (GREEN)** — In `Assets/Scripts/Sandbox/ItemData/PcItemBatchLoader.cs`, fix
  `ApplyCategoryIds` pendant fallback `7 → 9` (`equip_pendant`). Update stale header comment
  from `pendant.txt=7(after fix)` to `pendant.txt=9`, and the inline comment from "Ngọc bội"
  to "Hộ Thân Phù — equip_pendant (D9)". T10 → green.
- [x] **T12 (REFACTOR)** — Review PR-1 code for dead code / consistency; ensure
  `DetailTypeToCategory` and `ItemTypeToCategory` doc-comments note the two distinct axes
  (ItemType 1–12 vs EQUIPDETAILTYPE 0–16). Run `category_names=["Equipment"]` → all green.
- [x] **T13 (GATE — pre-push only)** — Run full EditMode suite (pendant fix touches shared
  loader). Record result; only pre-existing baseline failures allowed, 0 new failures in
  equipment/inventory paths.

PR-1 done: domain contract (enums, mapping, loader fix, read API) green; no UI change; safe
revert restores prior behavior.

---

## PR-2 — UI paperdoll bind + identifier rename + fixture migration (depends on PR-1)

Run gate: `run_tests(mode="EditMode", category_names=["Equipment"])`, then full suite pre-push.

- [x] **T14 (RED)** — In `Assets/Tests/EditMode/UI/CharacterInfoContentTests.cs`, add failing
  tests: `Paperdoll_TwoRings_BothPresent` (asserts both `Slot_ring` and `Slot_ring2` exist)
  and `Paperdoll_GameplaySlot_Equipped_ShowsEquippedClass` (inject `InventoryService` with a
  mask equipped, build paperdoll, assert mask cell has `equipped` class). Confirm RED.
- [x] **T15 (GREEN)** — In `Assets/Scripts/UI/CharacterInfo/CharacterInfoPaperdoll.cs`: extend
  `PaperdollSlot` struct with `readonly EquipSlot? gameplaySlot` (alongside existing
  `PlayerEquipSlot? equipmentSlot`). Add new slot `ring2` ("Nhẫn", gameplaySlot=Ring2); set
  `gameplaySlot` on mask=Mask, pendant=Pendant, belt=Belt, necklace=Necklace, boots=Boots,
  ring=Ring, trinket=Trinket slots per design slot matrix. T14 → green.
- [x] **T16 (RED)** — Add failing test asserting `Slot_amulet` and `Slot_charm` no longer
  exist, and `Slot_pendant` + `Slot_trinket2` exist (rename per DQ-5). Confirm RED (old keys
  still present).
- [x] **T17 (GREEN)** — In `CharacterInfoPaperdoll.Slots[]`: rename key `amulet`→`pendant`
  (label "Hộ Thân Phù", gameplaySlot=Pendant), rename `charm`→`trinket2` (label "Ngọc Bội",
  gameplaySlot=Trinket2). Keep other keys. T16 → green. Final 13 slots.
- [x] **T18 (RED)** — Add failing test `Paperdoll_GameplaySlot_EquippedViaDict` asserting a
  gameplay slot shows `equipped` only when the supplied equipped-dict contains its
  `EquipSlot` key, else `empty`; null dict → `empty` (never throws). Confirm RED (Build takes
  no dict).
- [x] **T19 (GREEN)** — In `CharacterInfoPaperdoll.Build(...)`: add optional
  `IReadOnlyDictionary<EquipSlot, ItemDefinition> equippedItems = null`. Binding logic:
  (1) `equipmentSlot` set AND `equipment.IsEquipped` → `equipped` (visual, checked FIRST);
  (2) else `gameplaySlot` set AND `equippedItems?.ContainsKey` → `equipped`;
  (3) else if `equipmentSlot` OR `gameplaySlot` set → `empty`;
  (4) else `framework`. Null-dict safe. T18 → green.
- [x] **T20 (GREEN)** — In `Assets/Scripts/UI/CharacterInfo/CharacterInfoContent.cs`: add
  optional `InventoryService inventory` (or snapshot provider) to constructor;
  `BuildEquipmentTab()`/`OnShow()` pass `inventory?.Equipped` as 3rd `Build()` arg. Null-safe
  (GM sandbox without inventory → gameplay slots show `empty`, visual slots still work).
- [x] **T21 (MIGRATE)** — Update `CharacterInfoContentTests.cs`:
  `Paperdoll_HasReferenceSlotCount` queries `Slot_amulet`→`Slot_pendant`,
  `Slot_charm`→`Slot_trinket2`, add `Slot_ring2`;
  `Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty` ring assertion `framework`→`empty`
  (ring now has gameplaySlot=Ring) plus positive `empty` checks for pendant/trinket/mask.
  Run category green.
- [x] **T22 (REGRESSION)** — Run `run_tests(mode="EditMode", category_names=["Equipment"])`.
  Verify non-accessory visual slots (helmet/weapon/armor/mount) still show `equipped` when
  `PlayerEquipmentService` reports equipped (regression guard per spec).
- [x] **T23 (GATE — pre-push)** — Run full EditMode suite. Record baseline; 0 new failures.

PR-2 done: paperdoll binds all 13 slots to real equipped state; slot identifiers follow PC
semantics; 2 rings; safe revert restores 12-slot framework paperdoll.

---

## Rollout & Follow-up

- [x] **T24** — Update task checkboxes as apply progresses; note known limitation in change
  docs: `PcMaskParser`/`PcShipinParser` do not exist yet → mask/trinket slots render `empty`
  until a follow-up parser change (pendant/amulet/ring parsers DO exist → those bind real
  items).
- [x] **T25** — Verify `framework` USS class is harmless now that all 13 slots have at least a
  gameplay binding (no CSS depends on it for specific slots).

### Follow-up (out of scope — separate changes)

- Add `PcMaskParser` (`mask.txt`, DetailType 11) and `PcShipinParser` (`shipin.txt`,
  DetailType 14) so mask/trinket items enter the runtime DB and those slots bind real items.
- Belt / cuff equip gameplay (belt now has `EquipSlot.Belt=10` but no equip interaction).
- Socket (`Đính`) gameplay — equip/unequip/socket interaction logic is explicitly deferred.

---

## skill_resolution

`paths-injected` — `jx-pc-port-rule/SKILL.md` and `jx-pc-resource-resolver/SKILL.md` were
available from the parent; the design/spec (already grounded in PC source) were the primary
inputs. No independent skill discovery performed.

## Review Workload Confirmation

- 2 PRs: PR-1 (~190 lines, domain+loader+tests), PR-2 (~170 lines, UI+rename+migrate).
- Each PR is under the 400-line review budget (risk: Low).
- Chained PRs recommended: Yes. Chain strategy: stacked-to-main. Delivery: auto-chain.
