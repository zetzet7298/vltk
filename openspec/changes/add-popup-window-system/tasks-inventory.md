# Tasks — Inventory Window (HUD-003 slice 2) — BtnItems

> Slice 2 of `add-popup-window-system`. Reuses slice-1 base. Design = `design-inventory.md`.
> Vietnamese UI. Reconstruct (player inventory SPR engine-hardcoded). Strict TDD.

## Review Workload Forecast
- One commit: `InventoryContent` + `InventoryGridBuilder` + USS + BtnItems wiring + tests ≈ **~350 lines** (within 400 budget). Single commit.

---

## Phase A — Icon pipeline confirm  [prep]
- [x] A1. Verify `ItemDefinition` has icon-resolvable fields (`resId`, `iconSourceId`, `iconResolved`). EditMode seeding uses `ItemContractImporter`.
- [x] A2. Decide icon source: injected `IItemIconResolver`; category-label fallback when no icon is resolved. No fabricated art.

## Phase B — InventoryGridBuilder  [TDD: RED first]
- [x] B1. Write `InventoryGridBuilderTests.cs` (RED): 6×10=60 cells; `empty`/`filled`; count badge; PcItemCategory filtering.
- [x] B2. Implement `InventoryGridBuilder.cs` (Columns=6, Rows=10, cell USS, count badge, filter). GREEN.

## Phase C — InventoryContent  [TDD]
- [x] C1. Write `InventoryContentTests.cs` (RED): TitleVi="Hành Trang"; seeded InventoryService → grid/footer; tab Tất cả/Trang Bị; OnShow refreshes.
- [x] C2. Implement `InventoryContent.cs` (tab bar + grid + footer; OnShow re-read; OnClose drop refs). GREEN.

## Phase D — USS + wire BtnStatus... BtnItems
- [x] D1. Author `Inventory.uss`: tab bar (5 tabs), grid (6-col flex-wrap), cell (icon/label+badge), footer (slot count).
- [x] D2. Link `<Style src>` Inventory.uss in GameHud.uxml.
- [x] D3. `GameHudController.OnItemsClick` (line 1184) → `PopupManager.Instance.Show(new InventoryContent(SandboxManager.Instance.InventoryService))`.
- [x] D4. Compile + run `category_names=["Popup"]` → green.

## Phase E — Vision verify (user gate) ★
- [x] E1. Play mode: opened BtnItems path via PopupManager → screenshot `Assets/Screenshots/inventory_popup_actual_v7.png`.
- [ ] E2. `vision_mcp_server_ui_diff_check` actual vs PC reference (`pc-evidence/hud/popup/khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`). Current caveat: reference is Character Info Hành Trang tab; this slice is standalone bag, so paperdoll mismatch remains by scope.
- [x] E3. Vision-check corrections: no Chinese SPR art; rejected wrong SPRs (`da1f1d62`, `bc31847f`); frame uses common popup shell + Vietnamese title/tabs in visual tree.

## Phase F — Commit + ship
- [x] F1. Update `pc-evidence/hud/README.md` §inventory SPR provenance (engine-hardcoded note + scout-correction).
- [ ] F2. Commit: `popup(SDD): Inventory window (6x10 grid, filter tabs, real data) wired to BtnItems`.
- [ ] F3. Push origin/dev.

## Follow-up (NOT this change)
- Item USE/EQUIP/DROP gameplay (tap-to-select → action menu).
- Inventory pagination (PC had page 1/2/3; mobile may need it if bag grows).
- Stack/split UI.
- Drag-to-reorder.
