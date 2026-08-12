# Proposal — Character Info Popup (HUD-003)

> Change: `add-character-info-popup`
> Scout handoff: `harness/planning/scout-popup-system.md`

## Why

The bottom toolbar has 13 feature buttons, but only Skills/Team/Faction open anything today.
`BtnStatus` (Character Info — the "Thông tin nhân vật" button, reference image
`pc-evidence/hud/popup/khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`) is a **stub** that
only logs. The user requirement: pressing a feature button must open the corresponding popup.

This change delivers two things:

1. **Reusable popup infrastructure** — today every "popup" is an ad-hoc `hidden` class toggle on
   a bespoke `VisualElement`, with no shared base class, no manager, no z-order/focus/drag handling.
   Building 13 windows that way is unsustainable. We add a `PopupWindow` base + `PopupManager`
   (overlay host, single-focus, open/close, modal scrim) so Inventory / Treasure / etc. reuse it.
2. **The Character Info window** as the first concrete consumer — reconstructed from the PC
   reference image + the closest matching PC frame SPR (`玲珑盒内框.spr`), in **Vietnamese**,
   matching the PC VLTK layout: 3 tabs, equipment paperdoll, Khóa/Đính/Tháo/Đóng buttons.

PC behaviour: windows are GBK-encoded INI layouts (`[Main]` background SPR + child sections) in
`jx-pc/pak_unpacked`. Vietnamese client SPRs carry a `_vn` suffix. No C++ popup source
exists; the classic status window appears engine-side, so we reconstruct from the reference image
with PC frame art.

## What Changes

- **ADD** `PopupWindow` base class — window chrome (frame SPR background, title bar, close
  button), open/close with fade, draggable header, focus z-order, `pickingMode` passthrough on
  scrim.
- **ADD** `PopupManager` — owns the overlay layer under `GameHud`, tracks open windows, enforces
  single-focus, raises `OnOpen`/`OnClose` events, ESC/back-tap closes topmost.
- **ADD** Character Info window UXML/USS — 3 tabs (Thuộc tính / Trang bị / Đánh giá), equipment
  paperdoll (~12 slots: Helm/Armor/Belt/Mount, 2×Ring/2×Amulet/Charm/Shield, Necklace/Weapon/
  Boots/2×Trinket), action buttons (Khóa/Đính/Tháo), Đóng button, name/PK/Trùng sinh header +
  character watermark placeholder.
- **ADD** `CharacterInfoPopupController` — tab switching, slot model (placeholder data for now),
  button handlers (Khóa/Đính/Tháo = no-op stubs with TODO; Đóng closes).
- **WIRE** `GameHudController.OnStatusClick` → `PopupManager.Open<CharacterInfoPopup>()`.
- **EXTRACT** Vietnamese SPR art via `jx-pc-resource-resolver`: window frame (`玲珑盒内框.spr`),
  close button (`关闭_vn.spr`), equipment slot frames, tab button frames.

## Impact

- **New files:**
  - `Assets/Scripts/UI/Popups/PopupWindow.cs`, `PopupManager.cs`
  - `Assets/Scripts/UI/Popups/CharacterInfo/CharacterInfoPopupController.cs`
  - `Assets/UI/Popups/CharacterInfo/CharacterInfoPopup.uxml`, `.uss`
  - `Assets/UI/Popups/Shared/PopupWindow.uss` (shared chrome)
  - `Assets/UI/Popups/Art/` — extracted SPR PNGs (window_frame_vn.png, btn_close_vn.png,
    equip_slot_frame.png, tab_*.png)
- **Modified files:**
  - `Assets/Scripts/UI/GameHudController.cs` — wire `BtnStatus`; add overlay host element.
  - `Assets/UI/HUD/GameHud.uxml` — add `<VisualElement name="PopupOverlay"/>` host.
- **New UI assets (SPR extraction):** window frame, close button, equip slot frames, 3 tab
  buttons — all **Vietnamese** (`_vn` variants where they exist).
- **Tests:** `CharacterInfoPopupTests` (EditMode, category `CharacterInfo`): manager open/close,
  single-focus, tab switching shows correct panel, Đóng closes, BtnStatus opens the window,
  popup registered in manager.
- **Out of scope (deferred to later SDD changes):**
  - Inventory window (`BtnItems`), Treasure (`BtnTreasure`), Task/Ranking/other popups.
  - Real equipment data binding (use placeholder mock data; backend wiring is a separate change).
  - Khóa/Đính/Tháo actual logic (no-op stubs here).
  - Character watermark avatar rendering (placeholder silhouette).
