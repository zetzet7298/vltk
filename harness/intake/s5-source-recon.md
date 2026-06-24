# S5 Source Recon — vltkunity HUD chrome → vltk-mobile

**Task:** Port HUD chrome (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar) from `vltkunity` to `vltk-mobile` at 100% UI/UX parity.
**Source of truth:** `/var/www/vltk-mobile/vltkunity/client/Assets` (NOT jx-source).
**Mode:** Read-only recon. No files were edited.
**Date:** 2026-06-23

---

## 0. Method & GUID legend

Prefab widgets are prefab-only (no backing C#) unless noted. Script attachments were resolved by matching each
`m_Script.guid` against `*.meta` files. Built-in UGUI package scripts (no `.meta` under `Assets/`) resolved to:

| GUID (short)      | UGUI component            |
| ----------------- | ------------------------- |
| `fe87c0e1cc20…`   | Image                     |
| `5f7201a12d95…`   | Text (Legacy)             |
| `1344c3c82d62…`   | RawImage                  |
| `67db9e8f0e2a…`   | Slider                    |
| `4e29b1a8efbd…`   | Button                    |
| `59f8146938ff…`   | HorizontalLayoutGroup     |
| `30649d3a9faa…`   | HorizontalLayoutGroup (v2)|
| `3245ec927659…`   | ContentSizeFitter         |
| `306cc8c2b49d…`   | LayoutElement             |

Custom script GUIDs resolved:
- `88718ef74bf95984da22c079c97e3eba` → `Scripts/UI/MiniMap.cs`
- `b5418d47dd7a1496bb5a5b2d5a26b9f9` → `Scripts/UI/TopBar.cs`
- `a39d609b48bbc41a18e351323a78c34b` → `Resources/WorldGameUI/Prefabs/ProgressBar.prefab` (referenced 43× in TopBar = nested prefab instances)

Art assets referenced (resolved from `.meta`):
- `Resources/WorldGameUI/Progress/hp.png` (HP fill, orange) — `eb09ce7d1b9eb41eea5d0ee975f94f1b`
- `Resources/WorldGameUI/Progress/img_carve_progress_blue.png` (MP fill) — `db24ddec04088447fbddb3d195e3a76e`
- `Resources/WorldGameUI/Progress/img_carve_progress_green.png` (Stamina fill) — `71d810315f39c49ed8fe467d65369fba`
- `Resources/WorldGameUI/Progress/img_carve_progressbg.png` (bar bg) — `3b9726e840f934d8bb7ecac2f36eda63`
- `Resources/WorldGameUI/Progress/img_carve_progress_orange.png` (default ProgressBar fill) — `88f5d73d957a544feaf710ca6d366c58`
- `Resources/WorldGameUI/money/jinbi.png` (gold coin) — `86e5828c9bc074f72aa55bd247b1e339`
- `Resources/WorldGameUI/money/yinliang.png` (silver ingot) — `92d5e833f308948e491e97872b1d2b6a`
- `Resources/WorldGameUI/money/tongqian.png` (copper coin) — `0d8eb845d021943cba47b6a094d80877`
- `Resources/WorldGameUI/Buttons/btn_plus.png` (add button) — `f502ab941b88f46d98e2f634a154b76f`
- `Resources/WorldGameUI/Bag/btn_greencircular.png` (avatar frame) — `cea9c138f97aa417ab88a490bb4dd0cf`
- `Resources/WorldGameUI/Bag/btn_welfare_bg.png` (portrait bg) — `5830b0e569bf94160b46c9693bb8fe80`
- `Resources/WorldGameUI/Bag/008.png` (portrait) — `bd0ba60cf82484ae5ac190552f64a150`
- `Resources/WorldGameUI/Buttons/btn_photo.png` — `cc74c411410e74abab1572306d5e146f`
- `Resources/WorldGameUI/Buttons/btn_common1_hud.png` — `fad790d69c34a42f3aa294a4a04ff5e0`
- `Resources/WorldGameUI/Buttons/img_levelbg.png` — `e71fb39a898334130a6b0f75b58fe1e4`
- `Resources/WorldGameUI/Buttons/top_bar.png` — `6f618c81ab05d48ec85c4cd9edb4de67`
- Fonts: `UTM Cafeta #19.ttf` (`6c045cd5…`, used by Money/Avatar/ProgressBar text), `hysz.ttf` (`c3a348a8…`), `btn_hydl.ttf` (`13fb86b4…`)

---

## 1. MiniMap

### 1a. vltkunity script — `Scripts/UI/MiniMap.cs` (namespace `game.scene.world.userInterface`)
- `[SerializeField] Image mapMask;` — parent that the runtime minimap texture is reparented into.
- `[SerializeField] Text mapName;` — scene name.
- `[SerializeField] Text mapPos;` — coordinate text.
- `SetHandle(game.resource.map.MiniMap)` — attaches a runtime minimap handle; reparents `miniMapHandle.go` under `mapMask`.
- `SetMapPosition(Position)` — **position formula (CRITICAL parity):**
  ```
  mapPos.text = $"{position.top}:{position.left}";
  float xx = (position.left / 16f) + miniMapHandle.xRatio;
  float yy = miniMapHandle.yRatio - (position.top / 16f);
  miniMapHandle.compRect.anchoredPosition = new Vector2(-xx, -yy);
  ```
- `SetMapName(string)` sets `mapName.text`.

### 1b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/MiniMap.prefab`
Hierarchy (names + sample text):
- `MiniMap` (root; MiniMap.cs attached)
  - `PanelName` (Image bg, sprite null) → contains `Name` Text `"(80,73)"` (placeholder)
  - `ImageMiniMap` (Image, default Unity knob sprite `fileID:10917` — the minimap viewport/mask)
  - `PanelPosition` (Image bg, sprite null) → contains `Position` Text `"(80,73)"`
- Text fields use default alignment; no custom font GUID on MiniMap text.

### 1c. vltk-mobile target — `Assets/Scripts/UI/MiniMapVltkUnityAdapter.cs` (UI Toolkit, pure C#)
Caches: `MinimapContent`, `PlayerDot`, `SceneName` (Label), `ScenePos` (Label),
`MinimapMarkerBtn`, `ToggleMapBtn`, `WorldMapBtn`, `CaveMapBtn`.
- Player dot placement: `_playerDot.style.left = snapshot.playerPosition.x; _playerDot.style.top = snapshot.playerPosition.y;`
- Scene pos text: `$"{(int)snapshot.playerPosition.x}:{(int)snapshot.playerPosition.y}"`.
- Buttons wired through `IHudCommandBus` (Marker/Toggle/WorldMap/CaveMap).

### 1d. GAPS — MiniMap
| # | Gap | vltkunity | vltk-mobile | Severity |
|---|-----|-----------|-------------|----------|
| M1 | **Position formula divergence** | `xx=(left/16)+xRatio`, `yy=yRatio-(top/16)`, anchoredPosition `(-xx,-yy)` — uses per-map `xRatio/yRatio` and `/16f` tile scale, negated. | Writes raw `playerPosition.x/y` to `style.left/top` with no ratio, no `/16f`, no negation. Dot will not align with the PC minimap. | 🔴 High |
| M2 | **No runtime minimap texture handle** | `SetHandle()` reparents a generated map texture into `mapMask` (the visible map image). | Static background only (per adapter header comment: "static minimap background applied in GameHud.uss/uxml, not swapped at runtime in Phase 1"). No live map render. | 🟡 Med (Phase-1 acknowledged) |
| M3 | **Coordinate text order** | `"{top}:{left}"`. | `"{x}:{y}"` where x≈left, y≈top → effectively `"{left}:{top}"`. **Order swapped** vs vltkunity. | 🟡 Med |
| M4 | Extra buttons (Marker/Toggle/WorldMap/CaveMap) | Not present in vltkunity MiniMap prefab. | Present. These are mobile additions — acceptable enhancement, but note they are NOT from the source of truth. | 🟢 Low (info) |

---

## 2. TopBar (HP / MP / Stamina bars + profile/screenshot)

### 2a. vltkunity script — `Scripts/UI/TopBar.cs`
- SerializeFields: `Slider SliderHp`, `Text TextHp`, `Slider SliderMana`, `Text TextMana`,
  `Slider SliderSatamina` (sic, typo), `Text TextSatamina`, `GameObject BtnOpenProfile`, `GameObject BtnScreenShot`.
- Data source: `PhotonManager.Instance.character`:
  - HP: `CurLife` / `MaxLife`
  - MP: `CurInner` / `MaxInner`
  - SP: `CurStamina` / `MaxStamina`
- `Update()` polls every frame → calls `UdpateUIHP/UdpateUIMP/UdpateUISP` (sic typos).
- `SetUpHp/Mana/Satamina(percent, title)`: sets `Text.text = title` and `Slider.value = percent`.
- Text format: `"{current}/{max}"` (e.g. `HPCurrent + "/" + HPMax`).
- **BUG in vltkunity source:** `UdpateUIMP` computes `MPPecent = CalculateHPPercentage(MPCurrent, HPMax)` — uses `HPMax` instead of `MPMax`. (Port should fix, not replicate.)
- Buttons: `BtnOpenProfile` → `MainCanvas.instance.OpenProfileDetail()`; `BtnScreenShot` → `ScreenCapture.CaptureScreenshot(...)` to `Assets/Screenshots/`.

### 2b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/TopBar.prefab`
- Root `TopBar` has `TopBar.cs` attached (guid `b5418d47…` at prefab line 221).
- Contains nested `ProgressBar.prefab` instances (43 references to `a39d609b…`):
  - `ProgressBarHp` (Fill sprite = `hp.png`, orange)
  - `ProgressBarMana` (Fill sprite = `img_carve_progress_blue.png`)
  - `ProgressBarStamina` (Fill sprite = `img_carve_progress_green.png`)
- Also: `Panel`, `Camera`, `Avatar` (nested), `Button (Legacy)` ×2 (BtnOpenProfile/BtnScreenShot with `btn_common1_hud.png` / `btn_photo.png`), `Image` with `img_levelbg.png`, background `top_bar.png`.
- Each bar = Slider (non-interactable, `m_FillRect` set, no handle) with Background (`img_carve_progressbg.png`) + Fill (colored) + Text.

### 2c. vltk-mobile target — `Assets/Scripts/UI/TopBarVltkUnityAdapter.cs` (UI Toolkit, pure C#)
Caches fills: `HpBarFill`, `MpBarFill`, `StaminaBarFill`, `ExpBarFill`; labels: `LevelText`, `HpText`, `MpText`, `StaminaText`, `ExpText`, `RankText`.
- Event-driven via `HudDataBridge.SnapshotChanged` (no Update polling — improvement).
- `SetBar(fill, fraction)` → `fill.style.width = Length(pct, Percent)`.
- `RequestProfile()` / `RequestScreenshot()` publish via bus.

### 2d. GAPS — TopBar
| # | Gap | Detail | Severity |
|---|-----|--------|----------|
| T1 | **🔴 DATA BUG: Stamina bar shows HP data.** | `SetBar(_staminaFill, snapshot.lifeFraction)` and `_staminaText = $"{snapshot.currentLife}/{snapshot.maxLife}"`. Uses life for stamina because **`HudSnapshot` has no stamina fields** (no `currentStamina`/`maxStamina`/`staminaFraction`). vltkunity reads `CurStamina/MaxStamina`. | 🔴 High |
| T2 | **Snapshot lacks stamina contract.** | `HudDataBridge.HudSnapshot` struct has only life/mana/exp. `BuildSnapshot()` does not query stamina. Adapter cannot be correct until snapshot gains stamina fields + `IRuntimeStateProvider` exposes `PlayerCurrentStamina/PlayerMaxStamina`. | 🔴 High (blocks T1) |
| T3 | **MP max hardcoded 100.** | `BuildSnapshot`: `int maxMana = 100;` — vltkunity uses `character.MaxInner`. Real max MP ignored. | 🟡 Med |
| T4 | **EXP bar is a mobile addition.** | vltkunity TopBar has HP/MP/SP only (no EXP slider). Mobile adds `ExpBarFill`/`ExpText`. `ComputeExpFraction` is a fudge (`currentExp/(currentExp+1)`), no real max-exp denominator. | 🟡 Med (parity: extra element + wrong math) |
| T5 | **No art/sprite parity.** | vltkunity bars use `hp.png`/blue/green carve-progress sprites + `img_carve_progressbg.png` bg + `UTM Cafeta` font, yellow text `0.96,1,0.41`. Mobile fills are plain colored VisualElements (style.width %). | 🟡 Med (visual parity) |
| T6 | **Profile/Screenshot wiring.** | vltkunity: `BtnOpenProfile`→`MainCanvas.instance.OpenProfileDetail()` (currently a no-op stub in vltkunity), `BtnScreenShot`→`ScreenCapture`. Mobile publishes intents via bus; verify controller actually captures screenshot + opens profile. | 🟢 Low (verify) |

---

## 3. Money (currency display)

### 3a. vltkunity script
**No `Money.cs` exists** — confirmed by full scan (`class Money` = 0 matches). The prefab is purely structural; currency values are set externally (likely by a bag/inventory controller not in scope).

### 3b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/Money.prefab`
Root `Money` (HorizontalLayoutGroup, ContentSizeFitter) → 3 currency rows:
- `Panel` → `ImgUnit` (sprite `tongqian.png` = copper/铜钱) + `Text (Legacy)` amount `"151160"` + `BtnAdd` (Button, `btn_plus.png`, LayoutElement 44×44)
- `Panel (1)` → `ImgUnit` (sprite `jinbi.png` = gold/金币) + `Text` + `BtnAdd`
- `Panel (2)` → `ImgUnit` (sprite `yinliang.png` = silver/银两) + `BtnAdd` + `Text`
- Text: `UTM Cafeta #19.ttf`, size 24, color yellow `0.96/1/0.41`, alignment Right (`m_Alignment: 5`).
- Amount placeholder text all `"151160"`.
- `BtnAdd` buttons have empty `m_OnClick` persistent calls (wired at runtime or not at all).

### 3c. vltk-mobile target
**No Money adapter/port exists.** Full scan for `MoneyAdapter`/`MoneyView`/`class Money` in `Assets/` = 0 matches.

### 3d. GAPS — Money
| # | Gap | Severity |
|---|-----|----------|
| Y1 | **Entire component missing.** No Money widget in vltk-mobile. | 🔴 High |
| Y2 | Need 3 currency rows (copper `tongqian`, gold `jinbi`, silver `yinliang`) each = icon + amount text + add-button, in a horizontal layout. | 🔴 High |
| Y3 | Art: `money/tongqian.png`, `money/jinbi.png`, `money/yinliang.png`, `Buttons/btn_plus.png` must be ported. | 🟡 Med |
| Y4 | `BtnAdd` onClick wiring — source has empty persistent calls; needs a backend hook (open recharge/shop?). Confirm intent. | 🟢 Low (clarify) |
| Y5 | Vietnamese: filenames are pinyin/Chinese (`tongqian`=铜钱, `jinbi`=金币, `yinliang`=银两). User-facing labels (if any tooltips/labels) must be Việt hóa: Đồng tiền / Vàng / Bạc. Prefab itself has only numeric text — no Chinese strings to translate in-widget. | 🟢 Low |

---

## 4. Avatar (player portrait + level)

### 4a. vltkunity script
**No `Avatar.cs` exists** — full scan `class Avatar` = 0 matches. Prefab-only.

### 4b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/Avatar.prefab`
Root `Avatar` (Image = `btn_greencircular.png` frame, LayoutElement 65×65):
- `Image` (z=5, sprite `btn_welfare_bg.png`, SizeDelta `-10,-10`, stretch anchors) — portrait background/frame inset
- `Image` (sprite `008.png` from `Bag/`, SizeDelta `-30,-30`, `PreserveAspect:1`) — the actual portrait
- `Text (Legacy)` `"93"` — level number, `UTM Cafeta` font size 20, yellow `0.96/1/0.41`, anchored bottom-right (`AnchorMin/Max = 1,0`), ContentSizeFitter.

### 4c. vltk-mobile target
**No Avatar adapter/port exists.** (The mobile `TopBarVltkUnityAdapter` does cache a `LevelText`, which partially covers the level-number role, but there is no portrait/frame widget.)

### 4d. GAPS — Avatar
| # | Gap | Severity |
|---|-----|----------|
| A1 | **Portrait widget missing.** No avatar frame/portrait in vltk-mobile. | 🟡 Med |
| A2 | Need: green-circular frame (`btn_greencircular.png`) + portrait bg (`btn_welfare_bg.png`) + portrait sprite (`008.png`, PreserveAspect) + level Text bottom-right. | 🟡 Med |
| A3 | Level text exists in TopBar adapter but not in the avatar frame position/ style (yellow, UTM Cafeta, bottom-right). | 🟢 Low |
| A4 | Portrait sprite source: `008.png` is a placeholder; real portrait should come from player face/ SPR. Confirm runtime portrait provider. | 🟢 Low (clarify) |

---

## 5. DeviceStatus (Wifi / Battery / Time / RTT)

### 5a. vltkunity script — `Scripts/UI/DeviceStatus.cs`
**Empty stub** — only default `Start()`/`Update()` with no logic. The prefab is driven by some other runtime updater (or is purely cosmetic with placeholder text).

### 5b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/DeviceStatus.prefab`
Root `DeviceStatus` (`m_IsActive: 0` — hidden by default; localScale `0.5,0.5,1`; Image bg color black `a=0.157`; HorizontalLayoutGroup padding 10/10/5/5, spacing 20, ChildAlignment MiddleCenter; ContentSizeFitter HorizontalFit=PreferredSize):
- `Wifi` (RawImage, texture guid `3deaaa33cbc5…`, SizeDelta 60×60)
- `Battery` (RawImage, texture guid `656567d5…`, SizeDelta 80×75, localScale `1,0.6,1`)
- `Time` (Text `"07:36:50"`, font `hysz.ttf`, size 40, green `0.0024/0.66/0`, ContentSizeFitter)
- `RTT` (Text `"40ms"`, font `hysz.ttf`, size 40, green, ContentSizeFitter)

### 5c. vltk-mobile target
**No DeviceStatus port exists.**

### 5d. GAPS — DeviceStatus
| # | Gap | Severity |
|---|-----|----------|
| D1 | **Entire component missing.** No device-status widget. | 🟡 Med |
| D2 | Need Wifi icon (RawImage), Battery icon (RawImage, squished 0.6y), Time text (HH:MM:SS, green), RTT/ping text (ms, green) in a horizontal layout, scale 0.5, semi-transparent black bg. | 🟡 Med |
| D3 | No backing script logic in vltkunity — port must implement live updates (system time, `Application.internetReachability`/ping, battery level) since source has none. | 🟡 Med (implement) |
| D4 | Art: Wifi + Battery are RenderTexture/`RawImage` assets (`3deaaa33…`, `656567d5…`) — locate & port. | 🟢 Low |

---

## 6. ProgressBar (reusable bar widget)

### 6a. vltkunity script
**No `ProgressBar.cs` exists** — full scan = 0 matches. Prefab-only; used as a nested base for TopBar's 3 bars.

### 6b. vltkunity prefab — `Resources/WorldGameUI/Prefabs/ProgressBar.prefab`
Root `ProgressBar` (HorizontalLayoutGroup spacing `-10`, ChildControlWidth/Height, LayoutElement PreferredWidth 150; no ContentSizeFitter on root):
- `Slider` (non-interactable Slider; LayoutElement PreferredWidth 100, Height 25; `m_Direction: 0` L→R; `m_FillRect` = Fill, no handle)
  - `Background` (Image, sprite `img_carve_progressbg.png`, anchors `0,0.25 – 1,0.75`)
  - `Fill Area` → `Fill` (Image, sprite `img_carve_progress_orange.png`)
- `Text (Legacy)` `"6757/8969"` (`UTM Cafeta` font, size 16, yellow `0.96/1/0.41`, alignment MiddleCenter)

### 6c. vltk-mobile target
**No reusable ProgressBar component.** Mobile inlines bar rendering via `SetBar()` on `VisualElement` fills (width %). No background sprite, no text label per bar (text is in separate labels).

### 6d. GAPS — ProgressBar
| # | Gap | Severity |
|---|-----|----------|
| P1 | No shared reusable bar widget; logic is inlined per-adapter. Parity risk: bg sprite + carved fill sprite + per-bar "current/max" text label are absent. | 🟡 Med |
| P2 | Per-bar text format vltkunity = `"6757/8969"` (current/max). Mobile `HpText` = `"{currentLife}/{maxLife}"` ✓ matches, but ProgressBar's own text node is not represented as a widget. | 🟢 Low |
| P3 | Visual: carved-progress sprites (`img_carve_progressbg`, orange/blue/green/hp) + `UTM Cafeta` font not applied. | 🟡 Med (visual) |

---

## 7. PlayerPopUp (context, not in adapter list — noted for completeness)

### vltkunity `Scripts/UI/PlayerPopUp.cs`
Buttons (all **already Vietnamese**, no Chinese): `Mật`, `Thông tin`, `Tổ đội`, `Vào đội`, `Hảo hữu`, `Vào bang`, `Mời bang`, `Sổ đen`, `Bái Sư`, `Giao dịch` (→ `PopUpCanvas.instance.OpenTrade()`), `Cừu Sát`.
Uses a `ButtonPrefab` + `GridLayoutGroup`. Most handlers are `Debug.Log` stubs. **No Chinese text here.** Not currently ported to mobile adapters (out of the listed scope but referenced by `PopUpCanvas`).

---

## 8. Chinese → Vietnamese (Việt hóa) notes

Scanned all in-scope prefabs/scripts for Chinese text:
- **No Chinese strings found** in MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar prefabs — all text is either numeric (`151160`, `6757/8969`, `93`, `40ms`, `07:36:50`, `(80,73)`) or already Vietnamese (PlayerPopUp).
- **Map names** (`MiniMap.SetMapName`) come from PC map data at runtime — those may be Chinese and require Việt hóa at the data layer (separate from this chrome port).
- Currency filenames are pinyin (`tongqian`/`jinbi`/`yinliang`) but are asset names, not user-facing strings. Any user-facing currency labels added must be Vietnamese: **Đồng tiền / Vàng / Bạc**.

---

## 9. Consolidated gap summary (priority for port)

### 🔴 High (blocks 100% parity)
1. **T1+T2 — Stamina bar shows HP data.** `HudSnapshot` has no stamina fields; `TopBarVltkUnityAdapter` falls back to life. Fix: add `currentStamina`/`maxStamina`/`staminaFraction` to `HudSnapshot`, populate in `HudDataBridge.BuildSnapshot()` from `IRuntimeStateProvider`, then bind in adapter.
2. **M1 — MiniMap dot position formula divergence.** Port the `xRatio/yRatio` + `/16f` + negation formula (or confirm the mobile minimap uses a different, correct projection and document why).
3. **Y1+Y2 — Money widget entirely missing.** Port 3 currency rows (copper/gold/silver + add buttons).

### 🟡 Medium
4. **M3 — MiniMap coordinate text order** (`top:left` vs mobile `left:top`).
5. **T3 — MP max hardcoded 100** (should read real MaxInner).
6. **T4 — EXP bar is an extra** not in vltkunity; `ComputeExpFraction` math is a fudge.
7. **T5/P3 — Visual/sprite/font parity** (carve-progress sprites, `UTM Cafeta` font, yellow text color) for bars.
8. **A1/A2 — Avatar portrait widget missing.**
9. **D1/D2/D3 — DeviceStatus widget missing** (Wifi/Battery/Time/RTT) + needs live-update logic (source stub is empty).
10. **P1 — No reusable ProgressBar widget.**

### 🟢 Low / verify
11. M4 — MiniMap extra buttons are mobile additions (ok, document).
12. T6 — Verify profile/screenshot intents are actually handled by controller.
13. A4/D4 — Portrait + Wifi/Battery art asset sourcing.
14. Y4 — `BtnAdd` onClick intent (recharge/shop?) needs clarification.

---

## 10. Files inspected (read-only)

**vltkunity scripts:** `Scripts/UI/MiniMap.cs`, `TopBar.cs`, `DeviceStatus.cs`, `PlayerPopUp.cs`, `ControlTop.cs`, `MainCanvas.cs`, `PopUpCanvas.cs`.
**vltkunity prefabs:** `MiniMap.prefab`, `TopBar.prefab`, `Money.prefab`, `Avatar.prefab`, `DeviceStatus.prefab`, `ProgressBar.prefab`.
**vltk-mobile targets:** `Assets/Scripts/UI/MiniMapVltkUnityAdapter.cs`, `TopBarVltkUnityAdapter.cs`, `GameHudController.cs`, `Assets/Scripts/Sandbox/HudDataBridge.cs`.

**No files were created or edited except this recon document.**
