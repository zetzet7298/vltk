# Apply Progress — bind-accessory-equipment-slots (PR-1)

> Change: `bind-accessory-equipment-slots` · PR-1 (additive domain + loader fix + tests)
> Strict TDD mode active. Test runner: Unity EditMode via MCP `category_names=["Equipment"]`.

## PR-1 Summary

PR-1 implements the additive domain contract (T1–T11): new `PcItemCategory` accessory
members, `DetailTypeToCategory` classifier, `EquipSlot` enum extension, `InventoryService`
read helpers, and the pendant loader fallback fix (7→9). No UI/paperdoll files touched.

## TDD Cycle Evidence

All tests written RED-first (referencing non-existent API surface → compile failure = RED),
then production code implemented (→ compile success + test pass = GREEN). Batch TDD approach:
test file authored complete against the design's API contract, then all production code
implemented in a single GREEN pass.

| Task | Phase | Evidence |
|------|-------|----------|
| T1 | RED | `EquipmentBindingTests.cs` written with 3 tests asserting `PcItemCategory.Mask/Pendant/Trinket` equippable + labels. Enum members absent → compile error. |
| T2 | GREEN | Appended `Mask=13, Pendant=14, Trinket=15` to `PcItemCategory`; added 3 `EquipmentSlotMapping` entries (equippable, stack 1, VI labels). |
| T3 | RED | 6 tests referencing `DetailTypeToCategory(int)` — method absent → compile error. |
| T4 | GREEN | Added `DetailTypeToCategory` switch: 3→Ring, 4→Necklace, 9→Pendant, 11→Mask, 14→Trinket, _→Material. Doc-comments note two distinct axes (ItemType vs EQUIPDETAILTYPE). |
| T5 | TRIANGULATE | Added `ItemTypeToCategory_Int15_StillMaterial` test asserting `ItemTypeToCategory(15)` returns Material (guards `Trinket=15` enum value collision). |
| T6 | RED | 2 tests asserting `EquipSlot.Ring2=7…Trinket2=12` appended values. Enum members absent → compile error. |
| T7 | GREEN | Appended `Ring2=7, Mask=8, Pendant=9, Belt=10, Trinket=11, Trinket2=12` after `Mount=6`. Existing values unchanged. |
| T8 | RED | 3 tests referencing `GetEquipped(EquipSlot)` + `IsSlotEquipped(EquipSlot)`. Methods absent → compile error. |
| T9 | GREEN | Added `GetEquipped(EquipSlot) → ItemDefinition` (null if empty, never throws) and `IsSlotEquipped(EquipSlot) → bool`. `Equipped` was already `IReadOnlyDictionary`. |
| T10 | RED | 2 tests calling `PcItemBatchLoader.ApplyCategoryIds` with stem="pendant" asserting detailType=9. Current fallback=7 → test would fail (also: method was `private`). |
| T11 | GREEN | Fixed `"pendant" => 9` (was `7`); updated stale header comment `pendant.txt=7(after fix)` → `pendant.txt=9`; updated inline comment "Ngọc bội" → "Hộ Thân Phù — equip_pendant (D9)". Made `ApplyCategoryIds` internal (added `AssemblyInfo.cs` with `InternalsVisibleTo`). |
| T12 | REFACTOR | Doc-comments on `ItemTypeToCategory` and `DetailTypeToCategory` note the two distinct axes (ItemType 1–12 vs EQUIPDETAILTYPE 0–16). Code reviewed for dead code — none found. **Test run pending parent verification (unityMCP unavailable in subagent).** |
| T13 | GATE | Full-suite pre-push gate — **deferred to parent** (pendant fix touches shared loader; project rule: full suite before push). |

## Files Changed

| File | Change |
|------|--------|
| `Assets/Scripts/Sandbox/AssemblyInfo.cs` | NEW — `[assembly: InternalsVisibleTo("VLTK.Tests.EditMode")]` for `ApplyCategoryIds` test access. |
| `Assets/Scripts/Sandbox/EquipmentSlotMappingService.cs` | +3 enum members (Mask=13, Pendant=14, Trinket=15), +3 mapping entries, +`DetailTypeToCategory(int)` method, doc-comment updates. |
| `Assets/Scripts/Sandbox/InventoryService.cs` | +6 `EquipSlot` members (Ring2=7…Trinket2=12), +`GetEquipped`/`IsSlotEquipped` read helpers. |
| `Assets/Scripts/Sandbox/ItemData/PcItemBatchLoader.cs` | pendant fallback 7→9, stale comment fix, `ApplyCategoryIds` private→internal. |
| `Assets/Tests/EditMode/Sandbox/EquipmentBindingTests.cs` | NEW — 17 tests `[TestFixture, Category("Equipment")]`. |

## Test Commands

```
run_tests(mode="EditMode", category_names=["Equipment"])
```
**NOTE:** unityMCP tools were not available in this subagent context (bash wrapper broken).
Test execution is deferred to the parent for verification. The code is designed to compile
and pass based on careful code review against existing test patterns and type signatures.

## Deviations from Design

- Added `Assets/Scripts/Sandbox/AssemblyInfo.cs` (not listed in design) — needed for
  `InternalsVisibleTo` to test `PcItemBatchLoader.ApplyCategoryIds` directly. This is a
  minimal, standard supporting change.

## Remaining Tasks

```text
- [ ] T12 (REFACTOR) — code review complete; test run pending parent unityMCP verification
- [ ] T13 (GATE) — full-suite pre-push gate (parent runs before commit)
```

PR-2 tasks (T14–T25) remain unchecked — separate PR slice (UI paperdoll + rename + migrate).

## Workload / PR Boundary

PR-1: ~5 files, ~200 lines (additive domain + loader fix + 17 tests). Under 400-line budget.

---

# Apply Progress — bind-accessory-equipment-slots (PR-2)

> Change: `bind-accessory-equipment-slots` · PR-2 (UI paperdoll bind + identifier rename + fixture migration)
> Strict TDD mode active. Test runner: Unity EditMode via MCP `category_names=["Equipment"]`.

## PR-2 Summary

PR-2 implements the UI consumer on top of committed PR-1 (`3b03d9a9c`):
- Character Info paperdoll now has 13 PC-semantic slots including `ring2`.
- `PaperdollSlot` now carries both gameplay `EquipSlot?` and visual `PlayerEquipSlot?` bindings.
- `CharacterInfoPaperdoll.Build(...)` accepts optional `IReadOnlyDictionary<EquipSlot, ItemDefinition>` and binds visual-first, then gameplay.
- `CharacterInfoContent` accepts optional `InventoryService` and passes `inventory?.Equipped` to the paperdoll on initial build and `OnShow()` refresh.
- Runtime HUD call site now passes `SandboxManager.InventoryService` so gameplay binding works in the live Character Info popup.
- `CharacterInfoContentTests` migrated old slot identifiers and adds Equipment-category PR-2 coverage.

No equip/unequip/socket gameplay was added. No `PcMaskParser`/`PcShipinParser` was added.

## TDD Cycle Evidence

Tests were authored/updated first against the PR-2 contract, then production code was changed to satisfy them. In this subagent environment Unity MCP was not available, so RED/GREEN execution is parent-deferred; RED evidence is compile/test-failure-by-construction before the production edits (missing `ring2`, old `Slot_amulet`/`Slot_charm`, and old `Build` signature with no equipped-items dictionary).

| Task | Phase | Evidence |
|------|-------|----------|
| T14 | RED | Added `Paperdoll_TwoRings_BothPresent` and `Paperdoll_GameplaySlot_Equipped_ShowsEquippedClass`. Before GREEN, `Slot_ring2` did not exist and `CharacterInfoContent` had no inventory input, so tests were failing/compile-failing by construction. |
| T15 | GREEN | Extended `PaperdollSlot` with `EquipSlot? gameplaySlot`; added `ring2` and assigned gameplay slots for all paperdoll slots per design matrix. |
| T16 | RED | Added `Paperdoll_SlotIdentifiers_FollowPcSemantics` asserting old `Slot_amulet`/`Slot_charm` absent and new `Slot_pendant`/`Slot_trinket2` present. Before GREEN, old keys existed and new keys did not. |
| T17 | GREEN | Renamed `amulet`→`pendant`, `charm`→`trinket2`; final slot count is 13. |
| T18 | RED | Added `Paperdoll_GameplaySlot_EquippedViaDict` calling `Build(..., equippedItems: ...)`. Before GREEN, `Build` had no dictionary parameter. |
| T19 | GREEN | Added optional `IReadOnlyDictionary<EquipSlot, ItemDefinition> equippedItems = null` to `CharacterInfoPaperdoll.Build`; visual binding checked first, gameplay binding second, null-dict safe. |
| T20 | GREEN | Added optional `InventoryService inventory` to `CharacterInfoContent`; `BuildEquipmentTab()` and `OnShow()` pass `_inventory?.Equipped`. Updated HUD runtime call site to pass live inventory. |
| T21 | MIGRATE | Migrated `Paperdoll_HasReferenceSlotCount` and `Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty`; ring/accessory empty checks now expect `empty`, not `framework`; added visual regression coverage for helmet/weapon/armor/mount. |
| T22 | REGRESSION | Parent-deferred: run `run_tests(mode="EditMode", category_names=["Equipment"])` with Unity MCP. |
| T23 | GATE | Parent-deferred: full EditMode pre-push gate. |
| T24 | DOC | Persisted task checkboxes updated for PR-2 completed implementation tasks; known limitation retained: no `PcMaskParser`/`PcShipinParser`. |
| T25 | CHECK | Verified by targeted search that `framework` references are only the fallback in `CharacterInfoPaperdoll` and test comments; no CSS dependency on specific framework slots was found. |

## Files Changed

| File | Change |
|------|--------|
| `Assets/Scripts/UI/CharacterInfo/CharacterInfoPaperdoll.cs` | Added gameplay slot binding, final 13-slot PC-semantic roster, Build optional equipped-items dictionary, visual-first/gameplay-second class logic. |
| `Assets/Scripts/UI/CharacterInfo/CharacterInfoContent.cs` | Added optional `InventoryService inventory`; passes equipped state on build and refresh. |
| `Assets/Scripts/UI/GameHudController.cs` | Passes live `SandboxManager.InventoryService` to `CharacterInfoContent` so runtime paperdoll can bind gameplay equipped state. |
| `Assets/Tests/EditMode/UI/CharacterInfoContentTests.cs` | Migrated paperdoll tests; added Equipment-category PR-2 coverage for two rings, renamed identifiers, gameplay dict binding, and visual regression guard. |
| `openspec/changes/bind-accessory-equipment-slots/tasks.md` | Marked completed PR-2 tasks T14–T21 and rollout T24–T25. T22/T23 remain unchecked pending parent Unity verification/pre-push gate. |

## Test Commands

Unity MCP is not available in this subagent context, so test execution is parent-deferred.

Commands/checks run here:

```text
read harness/AGENTS.md
read harness/.pi/skills/jx-pc-port-rule/SKILL.md
read harness/.pi/skills/jx-pc-resource-resolver/SKILL.md
read openspec/changes/bind-accessory-equipment-slots/tasks.md/design.md/spec.md/apply-progress.md
read CharacterInfoPaperdoll.cs, CharacterInfoContent.cs, CharacterInfoContentTests.cs, InventoryService.cs
grep call sites for CharacterInfoContent and CharacterInfoPaperdoll.Build
grep old slot keys Slot_amulet/Slot_charm/"amulet"/"charm"
grep framework in UI/test targets
git diff --check
```

Parent should run:

```text
run_tests(mode="EditMode", category_names=["Equipment"])
```

Then full EditMode suite before commit/push.

## Deviations from Design

- `Assets/Scripts/UI/GameHudController.cs` was updated in addition to the three expected PR-2 files. This is a small runtime wiring change: without passing `SandboxManager.InventoryService`, the live Character Info popup would keep gameplay slots empty even though tests could inject inventory. This remains within PR-2 UI binding scope and does not add gameplay behavior.

## Parent Verification Evidence

- `run_tests(mode="EditMode", category_names=["Equipment"])` via Unity MCP:
  **23/23 passed**, 0 failed, 0 skipped, duration 0.7905105s.
- Fresh diff review: `blind-hunter` read-only review **APPROVE**, no blockers; checked null
  safety, slot keys, live inventory wiring, and absence of old production `Slot_amulet` /
  `Slot_charm` references.
- Full EditMode pre-push gate via Unity MCP:
  **4097 tests executed**. Result state failed only because of the known baseline failures in
  Backend/Auth, Backend/Predict/Status, BaLang/CombatSkillSlot, InventoryService visual SPR,
  Mount/MalePlayerVisual, and PcWeaponThief source-path tests. No new failures were reported in
  Equipment, CharacterInfo, paperdoll, or accessory-binding paths.

## Remaining Tasks

```text
- [x] T22 (REGRESSION) — parent ran Unity MCP Equipment category tests: 23/23 passed.
- [x] T23 (GATE) — parent ran full EditMode suite: baseline failures only, 0 new PR-2 failures.
```

PR-1 T12/T13 were previously parent-verified/committed in `3b03d9a9c`; `tasks.md` now marks all T1–T25 complete.

## Workload / PR Boundary

PR-2 changed UI consumer/test files plus one HUD runtime wiring call site. It did not modify PR-1 domain files (`EquipmentSlotMappingService.cs`, `InventoryService.cs`, `PcItemBatchLoader.cs`, `EquipmentBindingTests.cs`).
