# Proposal — Popup Window System + Character Info (HUD-003)

> Change ID: `add-popup-window-system` · Slice 1 of the popup-system effort.
> Explore input: `explore.md` (PC architecture, SPR resolution, current-state audit).

## Why

The bottom toolbar has 8 menu buttons + Bảo Vật, but 3 of the main feature buttons (`BtnStatus`, `BtnItems`, `BtnTreasure`) are **stubs that only log** — tapping them does nothing. The user's requirement: *when you tap a feature button it must show that feature's popup/panel*. There is **no reusable popup/window infrastructure** today; existing panels (SkillPicker, BuffPanel, etc.) are ad-hoc `hidden`-class toggles hardcoded inside `GameHud.uxml`, impossible to reuse for 13 windows.

Slice 1 fixes this by building the reusable base **and** delivering one real window (Character Info) end-to-end, wiring `BtnStatus`. Slice 2+ reuse the base.

## What Changes (high level)

1. **`PopupWindow` base** — reusable window: ornate frame background, title bar, close button, open/close, focus/z-order, modal-ish dim backdrop. UI Toolkit (UXML template + USS), pure-presentational, no feature data inside it.
2. **`PopupManager`** — singleton overlay host on the HUD root. Owns the dim backdrop, renders windows in z-order, single-focus (opening a new window brings it forward; only one interactive window at a time by default), exposes `Show(IPopupContent)/Close()`.
3. **`IPopupContent` contract** — a window's body (tabs, paperdoll, buttons) is provided by a content class; the base supplies the chrome. Keeps feature code out of the window shell.
4. **Character Info window** (`CharacterInfoContent`) — 3 tabs (Thuộc tính / Trang bị / Đánh giá), equipment paperdoll, Khóa/Đính/Tháo/Đóng buttons. Binds to existing `PlayerEquipmentService` + `EquipmentSlotMappingService` (real data exists).
5. **Wire `BtnStatus`** → `PopupManager.Show(new CharacterInfoContent(...))`.
6. **PC art** — Vietnamese SPRs resolved via `jx-pc-resource-resolver`: `关闭_vn.spr` ("Đóng" close button, 3 states) + `通用按键.spr` (generic button) + `玲珑盒内框.spr` (blank ornate inner frame, reused as window chrome). No Chinese-text SPRs shipped.

## Scope

**In (slice 1):**
- `PopupWindow` base + `PopupManager` + `IPopupContent` contract + UXML template + USS.
- Character Info window: frame chrome, title, 3-tab switcher, Trang bị paperdoll (~12 slots laid out per reference), Khóa/Đính/Tháo/Đóng buttons, close.
- Paperdoll **binds real equipment data** where it exists: Weapon, Armor/Body, Helmet/Head, Mount (via `PlayerEquipmentService.GetVariant()` + `ItemDb.Resolve()` for icon). Ring/Necklace/Belt/Boots slots read from `EquipmentSlotMappingService` (framework slots, populated when data lands).
- Mask/Amulet/Charm/Trinket slots = display-only framework slots (visible, empty, labeled VI), data binding deferred.
- Thuộc tính tab: stats list bound to whatever player-stat source exists (TODO confirm at design; fallback = display-only labeled rows).
- Đánh giá tab: tab present, content stubbed (placeholder), clearly marked not-final.
- `BtnStatus` wired. Close button (Đóng SPR) + backdrop-click + Esc-equivalent close.
- EditMode tests: PopupManager lifecycle, single-focus, CharacterInfo bind-to-real-equipment, tab switching.

**Out (follow-up changes):**
- Inventory window (`BtnItems`), Treasure window (`BtnTreasure`), other toolbar windows.
- Drag-to-move window, resize, window persistence.
- Server-side equip/unequip, socket/embed (Đính) gameplay logic — buttons present + clickable but no-op/log in slice 1.
- Mask/Amulet/Charm/Trinket/second-Ring/second-Amulet data binding.
- Full Thuộc tính stat pipeline (if no stat source exists, slice 1 shows labeled rows).

## Key Design Decisions

### D1 — Window chrome is reconstructed, not a single SPR
PC has **no standalone character-window SPR** (engine-hardcoded; hashes NOT FOUND). Reconstruct the ornate frame from the reference + reuse `玲珑盒内框.spr` (blank gold-bordered inner frame) as the base panel, with USS-drawn title/corner-medallion styling and reference-matched color scheme. Rationale: fidelity to reference > forcing a non-matching SPR.

### D2 — One reusable shell, content via `IPopupContent`
The window shell (frame, title, close, focus, backdrop) is generic; each feature supplies its body via `IPopupContent`. This is why 13 future windows are cheap. Avoids repeating the SkillPicker ad-hoc pattern.

### D3 — Vietnamese art only
Shipped art uses `_vn` SPR variants. `关闭_vn.spr` = "Đóng" (verified decoded). Close-button text and all UI labels are Vietnamese. No Chinese-text SPRs in the build.

### D4 — Paperdoll binds real data where it exists
Reference shows ~12 slots; `PlayerEquipSlot` (SPR layers) has 6, `EquipmentSlotMappingService` knows 7 equippable categories. Slice 1 binds the 4 with real data (Weapon/Armor/Helmet/Mount) + shows 3 framework slots (Ring/Necklace/Belt/Boots) read from the mapping service; remaining slots (Mask/Amulet/Charm/Trinket) are visible-but-empty framework slots so the layout matches the reference. Honest about what has data vs not.

### D5 — Single-focus default
`PopupManager` opens one focused window at a time by default (opening Character Info closes any other open window). Rationale: matches PC behavior and keeps mobile screen real estate sane. Multi-window can be a later opt-in.

## Impact / Risks

- **New files**: `PopupWindow` (UXML/USS), `PopupManager.cs`, `IPopupContent.cs`, `CharacterInfoContent.cs`, CharacterInfo UXML/USS. No changes to combat/sandbox data services (read-only bind).
- **Edit**: `GameHudController.cs` (`OnStatusClick` stub → `PopupManager.Show`), `GameHud.uxml` (add overlay host).
- **Risk: stat source unknown** — Thuộc tính tab may have no existing player-stat pipeline. Mitigation: design phase confirms; fallback is labeled display-only rows (no fabricated data).
- **Risk: review workload** — base + one window is ~500-700 changed lines, near the 400-line budget. Mitigation: split into 2 commits (infrastructure commit, then Character Info commit) if it exceeds budget.
- **Risk: SPR decode pipeline** — `关闭_vn`/`玲珑盒` decoded fine via `extract_item_spr.py`; pipeline is proven. Low risk.

## Non-Goals

- Not porting all 13 windows in this change.
- Not implementing equip/unequip gameplay logic.
- Not matching PC INI 1:1 (engine-hardcoded window has no INI; reference reconstruction is the source of truth).
- Not redesigning the existing SkillPicker/Team/Faction panels to the new base (those can migrate later).
