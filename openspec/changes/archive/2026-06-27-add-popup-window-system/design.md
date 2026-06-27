# Design — Popup Window System + Character Info (HUD-003)

> Technical design for `add-popup-window-system`. Spec = `spec.md`. Design space = **1280×720** UI Toolkit.

## Component Overview

```
GameHud (root, pickingMode=Ignore)              ← GameHudController owns
├── … existing HUD (topbar, bottom strip, minimap, chat) …
├── SkillPickerOverlay / FacePickerOverlay / … (existing ad-hoc overlays)
└── PopupOverlay  (NEW — full-screen, PopupManager-owned)   ★
    ├── PopupBackdrop   (dim, click-to-close, pickingMode=Position)
    └── PopupWindow (active, single-focus)
        ├── PopupFrameChrome  (frame art: 玲珑盒 border + reconstructed title/corners)
        │   ├── PopupTitle      (VI label, e.g. "Thông Tin Nhân Vật")
        │   ├── PopupCloseBtn   (btn_close_vn = "Đóng" SPR, 3 states)
        │   └── PopupBody       ← IPopupContent.RootView mounted here
        └── (z-order: only one PopupWindow child at a time)
```

## Contracts (C#)

```csharp
namespace VLTK.UI.Popup
{
    // A feature window supplies its body; the shell supplies chrome.
    public interface IPopupContent
    {
        string TitleVi { get; }                 // shown in title bar
        void Build(VisualElement body);          // populate body once (tabs, slots, buttons)
        void OnShow();                           // refresh live data on open
        void OnClose();                          // release listeners/data refs
    }

    // Singleton-ish host on the HUD root. Read-only data bind only.
    public sealed class PopupManager
    {
        public static PopupManager Instance { get; }       // set by GameHudController init
        public bool IsOpen { get; }
        public void Show(IPopupContent content);           // single-focus: closes prior
        public void Close();                               // backdrop / close-btn / Esc-equiv
    }

    // Pure-presentational shell; no feature data.
    public sealed class PopupWindow : VisualElement
    {
        public PopupWindow(IPopupContent content, System.Action onClose);
    }
}
```

```csharp
namespace VLTK.UI.CharacterInfo
{
    public sealed class CharacterInfoContent : IPopupContent
    {
        public CharacterInfoContent(
            VLTK.Sandbox.PlayerEquipmentService equipment,
            VLTK.Sandbox.EquipmentSlotMappingService mappings,
            System.Func<VLTK.Backend.Dto.PlayerStateResponse> statsProvider);
        public string TitleVi => "Thông Tin Nhân Vật";
        public void Build(VisualElement body);   // 3 tabs + paperdoll + action buttons
        public void OnShow();                    // re-read equipment/stats
        public void OnClose();
    }
}
```

## File Layout (NEW)

```
Assets/UI/Popup/
  PopupWindow.uxml          # shell template (chrome + empty body slot)
  PopupWindow.uss           # frame chrome, title, close button, backdrop
  CharacterInfo/
    CharacterInfo.uxml      # 3-tab body + paperdoll + action buttons
    CharacterInfo.uss       # paperdoll layout, slot styling, tabs
Assets/Scripts/UI/Popup/
  IPopupContent.cs
  PopupManager.cs
  PopupWindow.cs            # builds shell from UXML, wires close/backdrop
  PopupWindowBinder.cs      # UXML→VisualElement resolution + class toggles
Assets/Scripts/UI/CharacterInfo/
  CharacterInfoContent.cs   # IPopupContent impl: build tabs, bind equipment/stats
  CharacterInfoPaperdoll.cs # slot layout + data bind (Weapon/Armor/Helmet/Mount real;
                            # Ring/Necklace/Belt/Boots = mapping framework;
                            # Mask/Amulet/Charm/Trinket = display-only)
Assets/UI/Popup/Art/        # decoded Vietnamese SPRs → PNG
  btn_close_vn.png          # 关闭_vn.spr frame0 (normal) — "Đóng"
  btn_close_vn_h.png        # hover
  btn_close_vn_p.png        # press
  popup_frame_inner.png     # 玲珑盒内框.spr (blank ornate border, 476×449)
```

## EDIT (existing files)

- **`Assets/UI/HUD/GameHud.uxml`** — add `<VisualElement name="PopupOverlay" class="hud-popup-overlay hidden" />` as last child of `GameHud`.
- **`Assets/Scripts/UI/GameHudController.cs`**:
  - Init: `PopupManager.Instance` bound to the `PopupOverlay` element (line ~265 area, after root resolve).
  - `OnStatusClick()` (line 1158): stub → `PopupManager.Instance.Show(new CharacterInfoContent(equipment, mappings, GetPlayerState));`

## Data Flow (read-only)

```
BtnStatus tap → GameHudController.OnStatusClick
  → PopupManager.Show(CharacterInfoContent)
    → PopupWindow builds chrome (UXML), mounts content.Build(body)
    → CharacterInfoContent.OnShow()
        ├─ Trang bị: PlayerEquipmentService.GetVariant(slot) → ItemDb.Resolve(itemId) → icon
        ├─ Thuộc tính: statsProvider() → PlayerStateResponse → stat rows
        └─ Đánh giá: placeholder
Close (Đóng btn / backdrop / manager.Close)
  → CharacterInfoContent.OnClose() (drop refs)
  → PopupWindow removed + backdrop removed
```

## Decision Records (technical)

### ADR-1 — PopupManager as static instance, owned by GameHudController
Single HUD → single manager. `GameHudController.Awake` sets `PopupManager.Instance` and points it at `PopupOverlay`. Avoids a separate MonoBehaviour and keeps popup state testable via injected content. `Instance` is a convenience; tests construct `PopupManager` directly against a temp panel.

### ADR-2 — IPopupContent.Build once, OnShow/OnClose for lifecycle
Build the UXML body once (heavy); refresh data in OnShow (cheap, called each open). Matches the existing `OpenSkillPanel` populate-then-show pattern. Keeps re-open fast.

### ADR-3 — Frame chrome = 玲珑盒 border + USS-reconstructed title/corners
`玲珑盒内框.spr` (476×449) is a blank gold-bordered panel → use as the window's base texture (scaled to fit, preserving aspect). Title bar + corner medallion + color scheme reconstructed in USS to match the reference. No standalone character-window SPR exists (engine-hardcoded; hashes NOT FOUND).

### ADR-4 — Paperdoll slot categories
- Bound real data: `PlayerEquipSlot.Weapon/Body/Head/Mount` → icon via `ItemDb.Resolve`.
- Mapping framework: `PcItemCategory.Ring/Necklace/Belt/Boots` from `EquipmentSlotMappingService`.
- Display-only (reference-matched, empty): Mask, Amulet×2, Charm, Trinket×2.
Layout grid positions taken from the reference screenshot.

### ADR-5 — Single-focus default
`Show()` closes the currently focused window before opening the new one. Matches PC behavior; simplest correct mobile UX. Multi-window is a non-goal for slice 1.

### ADR-6 — Action buttons non-destructive in slice 1
Khóa/Đính/Tháo wire to `SubsystemLog.Info` only (no `PlayerEquipmentService.Unequip` etc.). Real gameplay in a follow-up change. Keeps slice 1 a presentational+bind slice with zero mutation risk.

## Test Strategy (EditMode, category `Popup`)

- `PopupManagerTests`: Show adds backdrop+window; second Show closes first (single-focus); Close removes both; IsOpen toggles correctly. Construct manager against a temp `Panel`/UIDocument in EditMode.
- `CharacterInfoContentTests`: seed a `PlayerEquipmentService` (Weapon+Armor variants) + mock `ItemDb` → Trang bị Weapon/Armor slots show resolved icons, unequipped slots show empty frames. Seed a `PlayerStateResponse` → Thuộc tính shows strength=35 etc. Tab switch Thuộc tính↔Trang bị↔Đánh giá toggles visibility.
- `PopupShellCloseTests`: close button + backdrop tap invoke onClose.
- No PlayMode tests required for slice 1 (visual verification via screenshots during apply).

## Risks & Mitigations
- **SPR decode** — proven (`关闭_vn`/`玲珑盒` already decoded clean). Low risk.
- **ItemDb in EditMode** — verify `SandboxManager.Instance.ItemDb` is usable in EditMode tests; if not, inject an `IItemResolver` interface into `CharacterInfoPaperdoll` for testability.
- **Review workload** — forecast ~600 changed lines. If apply exceeds 400 in one commit, split into commit-1 (PopupWindow/Manager/contract + tests) and commit-2 (CharacterInfo + wiring + tests).
