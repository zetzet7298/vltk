# Scout Handoff — PC Popup/Panel System (inline; async runner broken)

> Compiled inline after 3 async scout/researcher failures (infra). Repo: `/var/www/vltk-mobile`. PC: `/var/www/vltksource_new`.

---

## Area 1 — PC popup resources & architecture

**Architecture (JX1/VLTK):** PC client has **no C++ source** in `vltksource_new` (only data + docs + 2 snippets). UI behavior lives in **GBK-encoded INI files** + **SPR graphics**. Each window/popup is an INI defining `[Main]` (background `Image=` SPR) + child sections (buttons, slots, lists).

**Main UI toolbar** (`_slistcache/unknown/dc11ac12.ini`, confirmed GBK):
- `Image=\spr\UI3\主界面\快捷栏(800).spr` (toolbar frame, hash `ebb69f9b`)
- Buttons (ASCII names): `Button0=Run, Button1=Sit, Button2=Status, Button3=Items, Button4=ItemEx, Button5=Horse, Button6=Skills, Button7=Exchange, Button8=Team, Button9=PK, Button10=Faction, Button11=Rec, Button12=ChatRoom`
- Sections `[Status] [Items] [ItemEx]` are label-only comments; actual windows are separate hashed INIs.

**Popup INIs found** (GBK, in `_slistcache/unknown/` unless noted):

| Hash INI | Purpose | Key SPR (CN path) | VI label |
|---|---|---|---|
| `dc11ac12.ini` | Main toolbar | `快捷栏(800).spr` | hotkey bar (done) |
| `13f5ce87.ini` | **Character paperdoll** (PlayerRoleBodyPart, RotateLeft/Right) | `\Spr\UI4\主界面\外装盒子\玲珑盒内框.spr` | 玲珑盒 (outfit box) — closest to ref image |
| `ebad2d8a.ini` | Attributes/equipment (TxtPropertyName/Part) | `\Spr\UI4\主界面\外装盒子\玲珑盒内框_1.spr` | 玲珑盒 inner |
| `1463f852.ini` | Treasure shop (奇珍阁) cart | `\spr\Ui3\买卖\新奇珍阁界面\购物车_vn.spr` | **_vn** VI variant |
| `94a9b42e.ini` | Companion bag (同伴背包) | `\Spr\Ui3\同伴背包\同伴背包界面.spr` | companion inv |
| `98523f6c.ini` | Task guide (任务指南) | `\spr\Ui4\主界面\任务指南资源\...`, `\Spr\Ui3\主界面\关闭_vn.spr` | **关闭_vn** close btn VI |
| `6b197d1f.ini` | Ranking (排名) | `\Spr\Ui3\排名\排名.spr` | ranking |
| `87248bea.ini` | Chat room members | `\Spr\Ui3\聊天室\成员列表\...` | chat |
| `92962c69.ini` | Anti-addiction panel | `\Spr\Ui3\主界面\防沉迷面板.spr` | system |
| `1a71ec0d.ini` | Login | `\Spr\Ui3\登入界面\...` | login |
| `05fc676a.ini` | Item forging (打造) | `\spr\Ui4\主界面\物品铸造\打造页面.spr` | forge |
| `8be0609f.ini` | Star-stone socket (星辰石镶嵌) | `\spr\Ui3\星辰石镶嵌.spr` | socket |
| `a054a8ba.ini` | Treasure chest (百宝箱) | `\spr\Ui3\TreasureChest\百宝箱底板.spr` | lottery |

**VI vs CN:** Vietnamese client SPRs carry `_vn` suffix (e.g. `购物车_vn.spr`, `关闭_vn.spr`). The toolbar frame itself was found in CN form only (`快捷栏.spr`).

**Reference popup** (`pc-evidence/hud/popup/khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png`) = Character Info, Trang bị (equipment) tab:
- Tabs: **Thuộc tính / Trang bị / Đánh giá** (Attributes/Equipment/Appraisal)
- Paperdoll ~12 slots: Helm, Armor, Belt, Mount (center col) + 2×Ring, 2×Amulet, Charm, Shield (left col) + Necklace, Weapon, Boots, 2×Trinket (right col)
- Buttons: **Khóa / Đính / Tháo / Đóng** (Lock/Embed/Unequip/Close)
- Character watermark + name (NgaMy) + PK + Trùng sinh (rebirth)

## Area 2 — PC popup logic

No C++ for popups. `KNpc.cpp` + `SceneDataDef.h` are combat/scene only (no OpenWindow/ShowPanel found). Behavior inferred from INI structure: `[Main]` background + child sections name child elements (buttons, slots, scrollbars); engine positions them by `Image=` + absolute coords inside each section (Left/Top/Width/Height commented as `;Left=N`). Window open/close, tab switch = engine-side; equip/unequip = item-slot index mapping (defined per-window, not global).

## Area 3 — Current vltk-mobile button wiring (`GameHudController.cs`)

| Button | Element | Handler | Status |
|---|---|---|---|
| Character Info | `BtnStatus` | `OnStatusClick` | **STUB** (log only) |
| Inventory/Bag | `BtnItems` | `OnItemsClick` | **STUB** (log only) |
| Skills | `BtnSkills` | `OnSkillsClick` | **WIRED** → `OpenSkillPanel()` |
| Team | `BtnTeam` | `OnTeamClick` | WIRED (toggle TeamPreview) |
| Faction | `BtnFaction` | `OnFactionClick` | WIRED (toggle StallCurrencySelector) |
| Treasure/Bảo Vật | `BtnTreasure` | `OnTreasureClick` | **STUB** (log only) |

**No popup/window infrastructure exists** — no base `PopupWindow` class, no `PopupManager`, no overlay host. Existing panels (SkillPicker, FacePicker, BuffPanel, TradeInfo, etc.) are ad-hoc `hidden` class toggles on pre-built `VisualElement`s, not a reusable window system.

## Area 4 — Existing UI assets

- `Assets/UI/HUD/` — GameHud.uxml/uss only (the toolbar + overlays). No popup UXML.
- `Assets/UI/HUD/Art/` — toolbar SPRs (frame, buttons) + `btn_treasure.png`. No popup background art.
- `Assets/StreamingAssets/Reference/PcUiConfig/` — `faces.ini`, `pc_miniskill.ini`, `team_info.ini`, `ranking.ini`, `pc_tradeinfo.ini` (partial popup configs, already extracted).

---

## Recommended SDD scope

**This is large** (13 toolbar buttons → up to 13 windows). Per `jx-pc-port-rule` + review-workload guard, build incrementally:

1. **First slice (this change): popup infrastructure + Character Info window** — the reference image. Deliver:
   - Reusable `PopupWindow` base (UXML template + USS + open/close/drag/focus) + `PopupManager` (overlay host, z-order, single-focus).
   - Character Info window: 3 tabs (Thuộc tính/Trang bị/Đánh giá), Trang bị paperdoll (~12 equip slots), Khóa/Đính/Tháo/Đóng buttons. Wire `BtnStatus`.
   - Extract Vietnamese popup SPRs: `\Spr\UI4\主界面\外装盒子\玲珑盒内框.spr` (frame), `\Spr\Ui3\主界面\关闭_vn.spr` (close btn), equipment-slot frames.
2. **Follow-up changes** (separate SDD): Inventory (`BtnItems`), Treasure (`BtnTreasure`), Task guide, Ranking, etc. — reuse the `PopupWindow` base.

**Blocker to resolve at proposal:** the exact Character Info window INI is uncertain (`13f5ce87.ini` = 玲珑盒 outfit box is closest but not a confirmed match; the classic status window may be engine-hardcoded). Need to either (a) decode more candidate INIs / inspect ref image slot count, or (b) reconstruct from the reference image + 玲珑盒 SPR since visual fidelity > exact INI.
