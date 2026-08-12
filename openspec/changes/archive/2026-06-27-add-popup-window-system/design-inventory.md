# Design — Inventory Window (HUD-003 slice 2) — BtnItems

> Slice 2 of `add-popup-window-system`. Reuses the slice-1 `PopupWindow`/`PopupManager`/`IPopupContent` base.
> Spec/scout reference: `scout-inventory-spr.md` (+ vision-verified corrections below).

## Vision-Verified SPR Findings (CORRECTED)

The scout report had **two wrong "verified" claims** that vision-MCP caught:

| SPR hash | Scout claimed | Vision reality | Verdict |
| --- | --- | --- | --- |
| `bc31847f` (角色信息底图_vn) | character-info unified panel | **"Hướng dẫn hoạt động"** Activity Guide (4 tabs, blank body) | ❌ wrong art |
| `da1f1d62` (道具面板) | inventory background | **stall/vendor** ("Đồ vật/Lời rao/Định giá/Rao bán", 8×10 grid, VI text baked in) | ❌ wrong art |

**Conclusion:** The PC *player inventory (背包) window SPR does NOT exist on disk** — it is engine-hardcoded (same situation as the character-status window). `da1f1d62` is a stall/vendor panel with baked-in Vietnamese text, not reusable as a clean inventory background. Both wrongly-decoded PNGs were deleted from `Assets/UI/Popup/Art/`.

## Approach: Reconstruct (consistent with Character Info)

Reuse the slice-1 reusable base + reconstruct the inventory body:
- **Window chrome**: reuse `PopupWindow` (already ships `玲珑盒内框.spr` blank border frame + `关闭_vn.spr` "Đóng" close button). One new content class, no new shell.
- **Grid**: built dynamically in C# from the PC INI geometry (verified from `94a9b42e.ini` / `b49267df.ini` `[ItemBox]`): **6 columns × 10 rows, cell 26×26, border 2px, box 170×280**. Slot cells are USS-styled empty frames (no PC slot-bg SPR exists).
- **Filter tabs**: PC inventory had NO category filtering (only pages 1/2/3); category tabs (Tất cả / Trang bị / Thuốc / Vật phẩm / Khác) are a **mobile-custom** feature, USS-styled to match the reference color scheme.

## Component Overview

```
PopupWindow (reused shell from slice 1)
└── body
    ├── InventoryTabBar      (Tất cả / Trang bị / Thuốc / Vật phẩm / Khác)   ← mobile-custom filters
    ├── InventoryGrid        (6×10 = 60 cells, PC geometry)
    │   └── InventoryCell×N  (icon + count badge; empty cells invisible)
    └── InventoryFooter      (slot count "12/28", money, Close handled by shell)
```

## Contracts (C#) — NEW

```csharp
namespace VLTK.UI.Inventory
{
    public sealed class InventoryContent : IPopupContent
    {
        public InventoryContent(VLTK.Sandbox.InventoryService inventory);
        public string TitleVi => "Hành Trang";
        public void Build(VisualElement body);   // tab bar + grid + footer
        public void OnShow();                     // re-read InventoryService.Inventory
        public void OnClose();
    }

    /// <summary>Grid builder from PC INI geometry (6×10, cell 26).</summary>
    internal static class InventoryGridBuilder
    {
        public const int Columns = 6, Rows = 10;
        public static void Build(VisualElement container,
            System.Collections.Generic.IReadOnlyList<VLTK.Sandbox.InventoryEntry> entries,
            VLTK.Model.PcItemCategory? filter);
    }
}
```

## File Layout (NEW)

```
Assets/Scripts/UI/Inventory/
  InventoryContent.cs        # IPopupContent impl: tabs + grid + footer, bind InventoryService
  InventoryGridBuilder.cs    # 6x10 grid from PC geometry; cell USS styling; filter
Assets/UI/Popup/Inventory/
  Inventory.uss              # tab bar, grid (6 cols flex), cell (icon+badge), footer
```

## EDIT (existing)

- **`GameHudController.cs`** `OnItemsClick` (line 1184 stub): → `PopupManager.Instance.Show(new InventoryContent(SandboxManager.Instance.InventoryService))`.

## Data Flow (read-only)

```
BtnItems tap → OnItemsClick
  → PopupManager.Show(InventoryContent)
    → InventoryContent.OnShow()
        ├─ read SandboxManager.Instance.InventoryService.Inventory (28 real entries)
        ├─ filter by active tab category (PcItemCategory)
        └─ InventoryGridBuilder.Build(grid, filtered, filter)
Close (Đóng / backdrop / manager.Close) → InventoryContent.OnClose (drop refs)
```

## Decision Records

### ADR-I1 — Reconstruct, reuse shell (consistent with Character Info)
Player inventory SPR doesn't exist (engine-hardcoded); the only "inventory-like" SPR found (`da1f1d62`) is a stall/vendor panel with baked VI text — not reusable. Reuse the slice-1 shell + build the grid in C#. Honest and consistent.

### ADR-I2 — PC geometry for the grid
6 columns × 10 rows, cell 26×26, border 2px — taken from the verified `[ItemBox]` section of companion/stash INIs. Mobile renders the same cell density; only the frame width scales to the design space (1280×720). Item icons fill cells; empty cells render as faint frames.

### ADR-I3 — Category filter tabs are mobile-custom
PC had no category filtering (only page tabs). Mobile adds Tất cả / Trang bị / Thuốc / Vật phẩm / Khác to make a 28-slot bag scannable on phone. Filter uses `EquipmentSlotMappingService.ItemTypeToCategory` / `ItemDefinition.genre` to classify entries. USS-styled to match the reference palette.

### ADR-I4 — Read-only bind; item USE/EQUIP deferred
Tapping a cell in slice 2 selects it (highlight) but performs no equip/use/drop action (logged intent only). Real item interactions come in a follow-up change. Keeps slice 2 a presentational+bind slice with zero mutation risk (same discipline as slice 1).

### ADR-I5 — Item icons via ItemDefinition.resId
Each cell binds an entry's `item.resId` → icon. Confirm `ItemDefinition` carries an icon-resolvable field; if the icon pipeline isn't available in EditMode, inject an `IItemIconResolver` and show a VI category label fallback (no fabricated art).

## Test Strategy (EditMode, category `Popup`)

- `InventoryContentTests`: seed `InventoryService` with a few items (AddItem) → grid shows their cells; slot count footer shows `N/28`; tab filter Trang bị hides non-equipment; tab Tất cả shows all; OnShow refreshes after AddItem.
- `InventoryGridBuilderTests`: 6×10 geometry (60 cells); empty cells present but empty-class; filter by PcItemCategory returns correct subset.
- Verify icon resolver injection for EditMode testability.

## Risks & Mitigations
- **Item icon pipeline** — confirm `ItemDefinition` icon field + whether ItemDb/icon-resolver works in EditMode. If not, inject `IItemIconResolver` (ADR-I5).
- **Review workload** — slice 2 ≈ 300-400 lines (one content class + grid builder + USS + wiring + tests). Single commit, within budget. If it exceeds 400, split tests into a second commit.
- **Vision gate** — before ship, screenshot the inventory open + run `vision_mcp_server_ui_diff_check` against the PC reference (`khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`) to confirm layout fidelity. User requirement: "chỉ đạt khi gần giống pc evidence".
