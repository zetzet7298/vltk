# Explore — Popup Window System + Character Info (HUD-003)

> Compiled by parent from scout handoff + PC SPR resolution. Authoritative input for proposal/spec/design.

## PC Architecture (jx-pc — NO C++ source, data + INI + SPR only)

JX1/VLTK PC windows are **INI-driven + engine-assembled**:
- Each window = a GBK-encoded `.ini` with `[Main]` (background `Image=` SPR, Left/Top/Width/Height) + child sections (buttons, slots, lists, text). The engine reads the INI and composes the window by positioning each child over the background.
- The toolbar (`dc11ac12.ini`) defines button→window links: `Button2=Status, Button3=Items, Button6=Skills, ...` — buttons open separate window INIs.

**Critical finding:** There is **NO standalone "character status window" SPR** on disk. Hashes for `\Spr\UI3\主界面\人物状态栏.spr`, `\Spr\UI3\主界面\角色面板.spr`, `\Spr\UI3\主界面\玩家属性.spr` → **NOT FOUND**. The classic character status window is **engine-hardcoded / dynamically composed** from a generic frame + positioned slot/button elements.

Closest reusable frame art found: **`玲珑盒内框.spr`** (`a210b99e.spr`) = 476×449 **blank inner frame** (gold double-line border, rounded corners, solid grey fill). No pre-rendered slots/tabs/text — elements drawn on top by engine. → Mobile must **reconstruct** the character window layout (reference-driven), not 1:1 copy a single SPR.

## Vietnamese SPR assets RESOLVED (verified via jx-pc-resource-resolver hash)

Hash algo: normalize path lowercase + backslash, GBK-encode, JX Pack Hash. All verified FOUND on disk + decoded:

| SPR (CN path) | hash | file | decoded | VI? |
|---|---|---|---|---|
| `\Spr\Ui3\主界面\关闭_vn.spr` | `962ab518` | `mapquyenchien/unknown/962ab518.spr` | 155×20, **"Đóng"** text (3 frames: normal/hover/press) | **VI ✓** |
| `\spr\Ui3\买卖\新奇珍阁界面\通用按键.spr` | `5a25df5b` | `updatejx05/unknown/5a25df5b.spr` | 53×20 generic button (3 frames) | — |
| `\Spr\UI4\主界面\外装盒子\玲珑盒内框.spr` | `a210b99e` | `updatejx14/unknown/a210b99e.spr` | 476×449 blank inner frame | — |

Decoded PNGs: `pc-evidence/hud/popup/pc_spr_decode/{close_vn,generic_btn,linglong_frame}_f*.png`.

`_vn` suffix = Vietnamese-variant SPRs (confirmed: `关闭_vn` renders "Đóng"). When resolving any window art, prefer `*_vn.spr` variants for Vietnamese text.

## Reference window — Character Info, "Trang bị" (Equipment) tab

`pc-evidence/hud/popup/khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`:
- **Tabs:** Thuộc tính / Trang bị / Đánh giá (Attributes / Equipment / Appraisal)
- **Header:** character name (green), PK value, Trùng sinh (rebirth), character watermark/silhouette
- **Equipment paperdoll (~12 slots)** around center silhouette:
  - Center column: Helm, Armor, Belt, Mount
  - Left column: Mask, 2×Ring, 2×Amulet, Charm, Shield/Insignia
  - Right column: Necklace, Weapon, Boots, 2×Trinket
- **Action buttons:** Khóa (Lock) / Đính (Embed) / Tháo (Unequip) / **Đóng** (Close)
- Frame: dark metallic, ornate silver corner brackets, bronze medallions flanking title/close.

## Current vltk-mobile state (GameHudController.cs)

Toolbar buttons wired to the HUD. Popup/button status:

| Button | Element | Handler | Status |
|---|---|---|---|
| Character Info | `BtnStatus` | `OnStatusClick` | **STUB** (log only) |
| Inventory/Bag | `BtnItems` | `OnItemsClick` | **STUB** (log only) |
| Skills | `BtnSkills` | `OnSkillsClick` | **WIRED** → OpenSkillPanel |
| Team | `BtnTeam` | `OnTeamClick` | WIRED (toggle TeamPreview) |
| Faction | `BtnFaction` | `OnFactionClick` | WIRED (toggle StallCurrencySelector) |
| Treasure/Bảo Vật | `BtnTreasure` | `OnTreasureClick` | **STUB** (log only) |

**No reusable popup/window infrastructure exists.** Existing panels (SkillPicker, FacePicker, BuffPanel, TradeInfo, etc.) are ad-hoc `hidden`-class toggles on pre-built `VisualElement`s inside `GameHud.uxml` — not a reusable, instantiable window system. There is no base `PopupWindow` class, no `PopupManager`/overlay host, no focus/z-order management.

UI Toolkit design space is **1280×720** (verified live via UIDocument resolvedStyle; NOT 1920×1080).

## Scope decision (user-approved)

**Slice 1 = popup infrastructure + Character Info window.** Build the reusable base so slices 2+ (Inventory, Treasure, Task guide, Ranking, ...) reuse it.

Out of scope (follow-up changes): Inventory grid population, Treasure window, other toolbar windows, drag-to-move polish, server-side equip/unequip logic.

## Open design questions (resolve in proposal/design)

1. **Window background art:** reconstruct from reference (USS-drawn frame + corners) vs reuse `玲珑盒` blank frame vs find richer frame SPR. Recommend: reconstruct ornate frame in USS using the 玲珑盒 border + reference corner style; inject character watermark as separate asset.
2. **Equipment slot data model:** paperdoll has fixed slot types (helm/armor/.../weapon). Need a slot-type → equipment-position enum mapping. Is there existing equipment data in the project to bind, or is slice 1 display-only (empty slots)?
3. **Tab content:** Thuộc tính (stats list) + Trang bị (paperdoll) + Đánh giá. Slice 1 may stub Đánh giá. Stats data source?
4. **PopupManager lifetime:** singleton on HUD root, or per-window? Single-focus (close others on open) vs multi?
