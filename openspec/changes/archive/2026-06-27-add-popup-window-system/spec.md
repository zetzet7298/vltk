# Spec — Popup Window System + Character Info (HUD-003)

> Delta spec for change `add-popup-window-system`. See `proposal.md` for motivation/decisions.
> UI Toolkit design space is **1280×720**. All UI labels Vietnamese. All shipped art = Vietnamese SPR variants.

## Requirements

### REQ-1 — Popup shell is reusable and content-agnostic
The system SHALL provide a `PopupWindow` shell (frame chrome + title bar + close button + focus/z-order) that renders any feature body supplied via an `IPopupContent` contract, with no feature-specific code inside the shell.

- **Scenario:** given a `PopupWindow` instantiated with any `IPopupContent`, the window renders the content's body inside the shell, and the content class owns its own tabs/slots/buttons while the shell owns the frame, title, and close affordance.

### REQ-2 — PopupManager hosts windows with single-focus
A `PopupManager` SHALL act as the single overlay host on the HUD root. It SHALL render a dim backdrop behind the active window and SHALL enforce single-focus: opening a window brings it to front; by default only one window is interactive at a time.

- **Scenario 1 (open):** calling `PopupManager.Show(content)` adds the backdrop + window to the HUD overlay, brings the window to front, and marks it focused.
- **Scenario 2 (single-focus):** when a window is already open and another is shown, the previously focused window is closed (or unfocused) so only one remains interactive.
- **Scenario 3 (close):** `PopupManager.Close()` (and the window's close button, and backdrop tap) remove the window + backdrop and restore HUD interactivity.

### REQ-3 — Close button is Vietnamese "Đóng" SPR
The close affordance SHALL use the decoded `关闭_vn.spr` art (renders the text **"Đóng"**), with normal/hover/press states. No Chinese-text art shall ship.

- **Scenario:** the close button background is `btn_close_vn` (from `关闭_vn.spr` frames) and its rendered text is "Đóng".

### REQ-4 — Character Info window matches the reference layout
The Character Info window SHALL reproduce the reference (`pc-evidence/hud/popup/khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`): 3 tabs (**Thuộc tính / Trang bị / Đánh giá**), a header (character name + PK + Trùng sinh + character watermark), an equipment paperdoll, and action buttons (**Khóa / Đính / Tháo**), all inside the ornate frame with the **Đóng** close button.

- **Scenario (tabs):** the window shows three tabs; tapping a tab switches the visible body to that tab's content; Trang bị is the initial/default tab.

### REQ-5 — Trang bị paperdoll binds real equipment data where it exists
The paperdoll SHALL display equipment slots laid out per the reference and SHALL bind real data for the slots that have a backing data source:
- **Bound (real data):** Weapon, Armor (Body), Helmet (Head), Mount — via `PlayerEquipmentService.GetVariant(slot)` + `SandboxManager.Instance.ItemDb.Resolve(itemId)` for the icon.
- **Framework slots (mapping-known):** Ring, Necklace, Belt, Boots — read from `EquipmentSlotMappingService`; rendered as labeled slots.
- **Display-only framework slots:** Mask, Amulet (×2), Charm, Trinket (×2) — visible, labeled, empty (data binding deferred to a follow-up change).

- **Scenario (bound slot):** when `PlayerEquipmentService` reports a Weapon variant, the Weapon slot resolves the item icon from `ItemDb` and shows it; an unequipped bound slot shows its empty labeled frame.

### REQ-6 — Thuộc tính tab binds player stats
The Thuộc tính tab SHALL display player attributes bound to `PlayerStateResponse` (level, exp, transLife/Trùng sinh, freePoint, magicPoint, strength, dexterity, vitality, spirit, series, money, repute). No fabricated/hardcoded stat values shall be shown as live data.

- **Scenario:** given a `PlayerStateResponse` with strength=35, the Thuộc tính list shows Sức Mạnh = 35 sourced from that response (or a clearly-labeled placeholder when no response is available).

### REQ-7 — Đánh giá tab present, content deferred
The Đánh giá tab SHALL be present and selectable. Its body in slice 1 is a clearly-marked "sắp ra mắt" (coming soon) placeholder — no appraisal logic.

- **Scenario:** selecting Đánh giá shows the placeholder body; selecting another tab restores that tab's real content.

### REQ-8 — BtnStatus opens Character Info
`GameHudController.OnStatusClick` SHALL open the Character Info window via `PopupManager.Show(new CharacterInfoContent(...))` instead of logging.

- **Scenario:** tapping `BtnStatus` opens exactly one Character Info window through the PopupManager; tapping it again or tapping close/dismiss closes it.

### REQ-9 — Action buttons present, non-destructive
Khóa / Đính / Tháo buttons SHALL be present and clickable. In slice 1 they SHALL be non-destructive: each logs its action via `SubsystemLog` and performs no gameplay mutation.

- **Scenario:** tapping "Tháo" logs the unequip intent and changes no equipment state.

### REQ-10 — EditMode test coverage
EditMode tests SHALL cover: PopupManager open/single-focus/close lifecycle; CharacterInfo binds real equipment (Weapon/Armor/Helmet/Mount) from a seeded `PlayerEquipmentService`; tab switching between the 3 tabs; Thuộc tính stat bind from a seeded `PlayerStateResponse`.

- **Scenario:** the change's EditMode tests (category `Popup`) pass with zero failures.

## Out of Scope (explicit)
- Inventory/Treasure/other toolbar windows (slice 2+).
- Equip/unequip/socket gameplay logic.
- Mask/Amulet/Charm/Trinket data binding.
- Drag-to-move, window resize, persistence.
- Migrating existing SkillPicker/Team/Faction panels onto the new base.
