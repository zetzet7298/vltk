# S5 Implementation Plan — HUD Chrome Port (vltkunity uGUI → vltk-mobile UI Toolkit)

**Story:** `story-hud-chrome-s5` (intake #25, normal lane)
**Source of truth:** `/var/www/vltk-mobile/vltkunity/client/Assets` (Unity uGUI project) — **NOT** jx-source PC
**Target:** `/var/www/vltk-mobile/Assets` (Unity UI Toolkit)
**Recon:** `harness/intake/s5-source-recon.md`
**Research:** `harness/research/s5-research.md` (cross-framework uGUI→UI Toolkit API patterns)
**Mode:** PLANNING ONLY. No Unity code is edited by this deliverable.
**Date:** 2026-06-23

---

## 0. Framing — this is a cross-framework port

Source widgets are **uGUI** (Canvas, Image, Text, Slider, RectTransform, LayoutGroup). Target is **UI Toolkit** (VisualElement, Label, USS, flexbox/Yoga). Prefabs/RectTransform **cannot** be copied — the visual tree is rebuilt as UXML/USS + C# `VisualElement` manipulation. The existing mobile adapters (`MiniMapVltkUnityAdapter`, `TopBarVltkUnityAdapter`) already establish the pattern; the 4 gap-fill widgets follow it.

### Cross-framework mapping (authoritative — from recon §0–6 + research)

| vltkunity (uGUI) concept | vltk-mobile (UI Toolkit) target | Notes / gotchas |
|---|---|---|
| `Slider.m_FillRect` colored fill | child `VisualElement` + `style.width = Length(pct, Percent)` | Existing `SetBar()` is correct. Do **NOT** use built-in `ProgressBar`. (Research finding 1) |
| `CanvasScaler` (Scale With Screen Size) | `PanelSettings.scaleMode = ScaleWithScreenSize` + `referenceResolution` + `screenMatchMode` | Verify the existing mobile `PanelSettings` asset; only a read/verify here, change is a separate decision. (Finding 2) |
| `Image` sprite background | `style.backgroundImage` (USS `url("resource:...")` or C# `new StyleBackground(sprite)`) | Carve-progress/money/avatar sprites port as backgrounds. |
| 9-slice (`Image.Type=Sliced`, sprite borders) | Sprite's own 9-slice borders apply automatically under `stretch-to-fill`; override via `-unity-slice-*` USS | **No `-unity-background-scale-mode: sliced`** exists — only `stretch-to-fill / scale-and-crop / scale-to-fit`. (Finding 5) |
| `RectTransform` anchors/pivots | flexbox layout; absolute pos = `position: absolute` + `left`/`top` | **Origin is top-left, not pivot-centered.** Player dot must offset by `-w/2,-h/2` to center. (Finding 3) |
| `HorizontalLayoutGroup` + `ContentSizeFitter` | `flex-direction: row` + content auto-size (no explicit width) | Maps Money rows / DeviceStatus row. |
| `Image` w/ `PreserveAspect` | `-unity-background-scale-mode: scale-to-fit` | Avatar portrait `008.png`. |
| Runtime minimap texture (reparented into `mapMask`) | `style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt))` + `scale-and-crop` | **Gotcha:** transparent VE bg makes black render pixels transparent → set solid bg color (alpha 255). (Finding 4) |
| `Text (Legacy)` yellow `0.96/1/0.41`, `UTM Cafeta #19.ttf` | `Label` + USS `-unity-font` + color `rgba(245,255,105,255)` | Fonts must be added as Font Assets. |
| `m_IsActive` toggle / `localScale 0.5` | `style.display` / `transform: scale(0.5)` (or width/height) | DeviceStatus default-hidden + half-scale. |

---

## 1. Required data-contract changes (PREREQUISITE for TopBar parity)

The recon's two 🔴 High gaps (T1/T2) are **contract bugs**, not adapter bugs: `HudSnapshot` has no stamina fields and `IRuntimeStateProvider` exposes none. The adapter cannot be correct until the contract is widened. vltkunity reads `CurStamina/MaxStamina` (recon §2a). Also T3: MP max is hardcoded `100`; vltkunity uses `character.MaxInner`.

### Step 1.1 — Widen `IRuntimeStateProvider` (stamina + real MP/EXP maxima)
**File (modify):** `Assets/Scripts/Sandbox/HudDataBridge.cs`

Add to the interface (from vltkunity `TopBar.cs` data source `PhotonManager.Instance.character`):
```csharp
int PlayerCurrentStamina { get; }   // vltkunity CurStamina
int PlayerMaxStamina { get; }       // vltkunity MaxStamina
int PlayerMaxMana { get; }          // vltkunity MaxInner  (replaces hardcoded 100)
long PlayerMaxExp { get; }          // real EXP denominator (fixes ComputeExpFraction fudge)
```
> Source values: vltkunity `TopBar.cs` reads `CurStamina`/`MaxStamina`/`MaxInner`. `PlayerMaxMana` removes the `int maxMana = 100;` hardcode (recon §2c/T3).

### Step 1.2 — Widen `HudSnapshot` struct (same file)
Add fields mirroring life/mana:
```csharp
public int currentStamina;
public int maxStamina;
public float staminaFraction;
public long maxExp;
public float expFraction;
```

### Step 1.3 — Populate in `BuildSnapshot()`
- `maxStamina = Math.Max(1, _runtime.PlayerMaxStamina); curStamina = Clamp(...); staminaFraction = curStamina/maxStamina;`
- `maxMana = Math.Max(1, _runtime.PlayerMaxMana);` (delete the `=100` literal)
- `maxExp = Math.Max(1, _runtime.PlayerMaxExp); expFraction = Clamp01(currentExp/maxExp);`
- Add the new fields to `SnapshotsEqual` change-detection (so `RefreshAndPublish` fires on stamina/MP-max/EXP-max change).

### Step 1.4 — Update every `IRuntimeStateProvider` implementor
Search `semantic_grep "IRuntimeStateProvider"` for all implementors (test fakes + production provider) and add the 4 new members. The test fake `FullLifeRuntime` in `TopBarVltkUnityAdapterTests.cs` (and any fake in `MiniMapVltkUnityAdapterTests.cs`) must implement them.

> ⚠️ **Blast radius:** adding interface members is a compile break for every implementor. This is the riskiest step; the implementor sweep must be exhaustive or the assembly won't compile.

---

## 2. MiniMap parity fixes (existing adapter — modify)

Gaps: **M1** (dot formula divergence 🔴), **M3** (coord text order 🟡). M2 (runtime texture) is Phase-1 acknowledged → not required now but documented. M4 (extra buttons) are acceptable mobile additions.

### Step 2.1 — Port the position formula (M1)
**File (modify):** `Assets/Scripts/UI/MiniMapVltkUnityAdapter.cs` → `ApplyPlayerPosition`

vltkunity formula (recon §1a, CRITICAL parity):
```
xx = (left / 16f) + xRatio
yy = yRatio - (top / 16f)
anchoredPosition = (-xx, -yy)
```
Port to UI Toolkit (top-left origin, `position: absolute`):
```csharp
float xx = (position.left / 16f) + miniMapHandle.xRatio;   // xRatio = per-map horizontal offset
float yy = miniMapHandle.yRatio - (position.top / 16f);
_playerDot.style.left = xx;        // negate handled by origin convention; see note
_playerDot.style.top  = yy;
```
> ⚠️ The negation `(-xx,-yy)` in uGUI comes from its pivot/anchor convention. In UI Toolkit top-left origin the sign must be re-derived from the actual projection, not copied blindly. **Decision required (implementer):** either (a) derive xRatio/yRatio + sign from the mobile minimap projection and assert the dot lands on the player tile, or (b) if the mobile minimap uses a different correct projection, document why and keep raw coords. The recon flags this as 🔴 High specifically because the current code writes raw `playerPosition.x/y` with **no** ratio, **no** `/16f`, **no** consideration of sign — that is wrong regardless.
>
> **Plan requirement:** implementer must add a test (Step 6) asserting the dot position for a known `(left, top, xRatio, yRatio)` input equals the projected `(left/16+xRatio, yRatio−top/16)` value (offset for centering), proving the formula is ported and not regressed.

The adapter currently reads `snapshot.playerPosition` only. It needs per-map `xRatio`/`yRatio`. Add a small `MiniMapProjection` value (or fields on `MapDefinition`/snapshot) carrying `xRatio`, `yRatio`. Source: `miniMapHandle.xRatio/yRatio` (recon §1a).

### Step 2.2 — Fix coordinate text order (M3)
**Same file,** `ApplySceneText`:
- Current: `$"{(int)playerPosition.x}:{(int)playerPosition.y}"` → effectively `left:top`
- vltkunity: `$"{position.top}:{position.left}"` → `top:left`
- Change to: `$"{(int)playerPosition.y}:{(int)playerPosition.x}"` (top:left)

### Step 2.3 — (Optional, Phase-2) runtime minimap texture (M2)
Not in this slice's scope (Phase-1 acknowledged in adapter header). If pursued later: `style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt))` + `-unity-background-scale-mode: scale-and-crop` + solid bg color (alpha 255) to avoid the black-transparency gotcha (research finding 4).

---

## 3. TopBar parity fixes (existing adapter — modify)

Gaps: **T1** (stamina shows HP 🔴, unblocked by §1), **T4** (EXP fudge), **T5** (visual/sprite parity 🟡), **T3** (MP max, fixed by §1).

### Step 3.1 — Bind stamina + real EXP (T1/T4)
**File (modify):** `Assets/Scripts/UI/TopBarVltkUnityAdapter.cs` → `Apply`
```csharp
SetBar(_staminaFill, snapshot.staminaFraction);                 // was lifeFraction  (T1)
...
if (_staminaText != null)
    _staminaText.text = $"{snapshot.currentStamina}/{snapshot.maxStamina}";  // was life (T1)
SetBar(_expFill, snapshot.expFraction);                          // was ComputeExpFraction fudge (T4)
```
Delete `ComputeExpFraction` (replaced by snapshot's correctly-computed `expFraction`).

### Step 3.2 — vltkunity source bug: do NOT replicate
recon §2a notes vltkunity `UdpateUIMP` computes `MPPecent = CalculateHPPercentage(MPCurrent, HPMax)` (uses `HPMax`). **Port must fix** — `manaFraction` from the bridge already uses `maxMana` correctly. Add a test asserting MP bar fraction uses `maxMana`, not `maxLife`.

### Step 3.3 — Visual/sprite/font parity (T5)
This is USS/UXML work (no C# logic). Author in the HUD UXML/USS:
- HP fill background-image `url("WorldGameUI/Progress/hp.png")`, MP `img_carve_progress_blue.png`, Stamina `img_carve_progress_green.png`; bar bg `img_carve_progressbg.png` (recon §0 art assets + §6b).
- `-unity-background-scale-mode: stretch-to-fill` (9-slice via sprite borders).
- Per-bar `Label` `"current/max"` centered over bar (already present as `HpText` etc. ✓).
- Font `UTM Cafeta #19.ttf`, text color yellow `0.96/1/0.41` ≈ `rgba(245,255,105,255)`, size 16 (ProgressBar text) / per-bar.
- **PPU gotcha (research finding 5):** JX sprites with PPU≠100 scale by `sprite_PPU/100`. If bars render tiny, set `-unity-slice-scale` or sprite PPU=100.

> These are asset/USS changes. Confirm the sprites exist in `Assets/Resources/WorldGameUI/...` (port from vltkunity if missing). This step touches UXML/USS + asset importers, not adapter C#.

---

## 4. Gap-fill: Money widget (Y1/Y2/Y3 🔴)

vltkunity has **no `Money.cs`** (recon §3a) — purely structural prefab. Port = build adapter mirroring prefab layout.

### Step 4.1 — New adapter
**File (create):** `Assets/Scripts/UI/MoneyVltkUnityAdapter.cs`
- ctor `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, `IDisposable` (matches existing pattern).
- Cache 3 rows. Each row = icon `VisualElement` (background-image) + amount `Label` + add `Button`. Element names: `MoneyCopperRow` / `MoneyCopperIcon` / `MoneyCopperAmount` / `MoneyCopperAddBtn`, likewise `Gold`, `Silver`.
- Layout = `flex-direction: row` (maps vltkunity `HorizontalLayoutGroup` + `ContentSizeFitter`, recon §3b).

### Step 4.2 — Data binding
Currency amounts are **not** in `HudSnapshot` today. Two options (implementer picks, document in PR):
- (a) Add `copper`/`gold`/`silver` to `HudSnapshot` + `IRuntimeStateProvider` (consistent with §1 pattern), or
- (b) Inject a separate `ICurrencyProvider` to avoid bloating `HudSnapshot` (preferrable if currency has its own runtime source).

vltkunity prefab shows only numeric placeholder text `"151160"` (no source binding found) — recon §3a. **Decision needed:** confirm where currency values come from at runtime (bag/inventory controller). Until then, adapter exposes `SetAmounts(int copper, int gold, int silver)` for testability independent of binding choice.

### Step 4.3 — Art
Port sprites to `Assets/Resources/WorldGameUI/money/`: `tongqian.png` (Đồng tiền), `jinbi.png` (Vàng), `yinliang.png` (Bạc) + `Buttons/btn_plus.png` (recon §0). Assign as `background-image`.

### Step 4.4 — Add buttons (Y4)
`BtnAdd` → publish a new intent. Add to `IHudCommandBus`:
```csharp
event Action<CurrencyType> OnRechargeRequested;   // CurrencyType { Copper, Gold, Silver }
void PublishRechargeRequested(CurrencyType type);
```
Implementer confirms intent (recharge/shop?) with reviewer — recon §3d Y4 flags this as "clarify". Default: route to the existing recharge/shop panel via controller.

### Step 4.5 — Vietnamese
Filenames are pinyin/asset names (not user-facing). If any tooltip/label is added, use **Đồng tiền / Vàng / Bạc**. No zh strings in the prefab itself (recon §8).

---

## 5. Gap-fill: Avatar widget (A1/A2 🟡)

vltkunity has **no `Avatar.cs`** (recon §4a) — prefab-only.

### Step 5.1 — New adapter
**File (create):** `Assets/Scripts/UI/AvatarVltkUnityAdapter.cs`
- ctor `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, `IDisposable`.
- Cache: frame (`AvatarFrame`), portrait bg (`AvatarPortraitBg`), portrait (`AvatarPortrait`), level `Label` (`AvatarLevelText`).
- Nesting mirrors prefab (recon §4b): frame `btn_greencircular.png` (65×65) > portrait bg `btn_welfare_bg.png` (inset −10/−10) > portrait `008.png` (inset −30/−30, `scale-to-fit` = PreserveAspect).
- Level text bottom-right: USS `position: absolute; right:0; bottom:0`, yellow `0.96/1/0.41`, `UTM Cafeta` size 20. Binds `snapshot.level` (already in `HudSnapshot`). Note: `TopBarVltkUnityAdapter` also has a `LevelText` — de-duplicate (recon A3); keep level in the avatar frame per vltkunity, remove/repurpose the TopBar duplicate, or keep both but document.

### Step 5.2 — Portrait source (A4)
`008.png` is a placeholder. Real portrait comes from player face/SPR. Add `IPortraitProvider` (or field on snapshot) — confirm runtime source with reviewer. Until then `SetPortrait(Texture2D)` for testability.

### Step 5.3 — Art
Port `Bag/btn_greencircular.png`, `Bag/btn_welfare_bg.png`, `Bag/008.png`, `Buttons/img_levelbg.png` (recon §0).

---

## 6. Gap-fill: DeviceStatus widget (D1/D2/D3 🟡)

vltkunity `DeviceStatus.cs` is an **empty stub** (recon §5a) — the port must **implement** live-update logic (source has none).

### Step 6.1 — New adapter
**File (create):** `Assets/Scripts/UI/DeviceStatusVltkUnityAdapter.cs`
- ctor `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, `IDisposable`.
- Cache: `DeviceWifi` (icon), `DeviceBattery` (icon), `DeviceTime` (`Label`), `DeviceRtt` (`Label`).
- Layout: `flex-direction: row`, scale 0.5 (prefab `localScale 0.5`), semi-transparent black bg `rgba(0,0,0,40)` (alpha 0.157×255), padding 10/10/5/5, spacing 20, middle-center (recon §5b).

### Step 6.2 — Live updates (no source logic — implement)
- Time: `System.DateTime.Now.ToString("HH:mm:ss")` → `DeviceTime.text`. Placeholder `"07:36:50"`.
- RTT/ping: `Application.internetReachability` + ping → `"Xms"`. Placeholder `"40ms"`.
- Battery: `SystemInfo.batteryLevel` → drive Battery icon state/visibility.
- Update cadence: a `Tick()` method the controller calls each second (do **not** poll every frame — avoid the vltkunity `Update()`-per-frame antipattern; the HUD is event-driven elsewhere). Color green `0.0024/0.66/0` ≈ `rgba(0,168,0,255)`, font `hysz.ttf`, size 40 (recon §5b).

### Step 6.3 — Art (D4)
Wifi + Battery are `RawImage` textures (guids `3deaaa33…`, `656567d5…`) — locate in vltkunity and port. Battery prefab is squished `localScale 1,0.6,1`.

> **EditMode testability:** wrap system calls behind an `IDeviceStateProvider` (`Now`, `PingMs`, `BatteryLevel`, `InternetReachable`) so tests inject fakes. Do not call `DateTime.Now`/`SystemInfo` directly in the adapter — same pure-C# testability rule the other adapters follow.

---

## 7. Gap-fill: ProgressBar reusable widget (P1/P3 🟡)

vltkunity `ProgressBar` is prefab-only (no `.cs`, recon §6a), nested 43× in TopBar. Mobile inlines bars via `SetBar()`.

### Decision: extract a reusable bar helper
**File (create):** `Assets/Scripts/UI/HudProgressBar.cs` (static helper or small class)
- Encapsulates: bg `VisualElement` (sprite `img_carve_progressbg.png`) + fill `VisualElement` (per-bar color sprite) + `Label` (`"current/max"`, `UTM Cafeta`, yellow, size 16, centered).
- `Set(float fraction, int current, int max)` → fill width % + label text.
- Refactor `TopBarVltkUnityAdapter` to use it (reduces inline duplication, P1).
- Visual parity (P3): carve-progress sprites + font applied here, once.

> Non-goal: a full generic widget library. Just enough to dedupe the 3 TopBar bars and give Money/Avatar consistent styling hooks.

---

## 8. Wiring — GameHudController

**File (modify):** `Assets/Scripts/UI/GameHudController.cs` (existing TopBar at ~337, MiniMap ~340)
- Lazy-init the 4 new adapters alongside the existing 2.
- Call `Bind()` on each; dispose on `OnDisable`.
- Subscribe DeviceStatus `Tick()` to a per-second update (not per-frame).

---

## 9. Source-value reference (all values from recon — not invented)

| Value | Source | Used in |
|---|---|---|
| Position formula `xx=(left/16)+xRatio; yy=yRatio-(top/16); anchoredPosition(-xx,-yy)` | recon §1a (MiniMap.cs) | Step 2.1 |
| Coord text `"{top}:{left}"` | recon §1a | Step 2.2 |
| HP/MP/SP from `CurLife/MaxLife`, `CurInner/MaxInner`, `CurStamina/MaxStamina` | recon §2a (TopBar.cs) | Steps 1.1, 3.1 |
| Text format `"{current}/{max}"` | recon §2a | Steps 3.1, 7 |
| Bar sprites `hp.png`, `img_carve_progress_blue/green.png`, bg `img_carve_progressbg.png`, default `img_carve_progress_orange.png` | recon §0, §6b | Steps 3.3, 7 |
| Money: 3 rows, `tongqian`/`jinbi`/`yinliang` + `btn_plus`, text `"151160"`, font `UTM Cafeta` size 24, yellow `0.96/1/0.41`, align Right | recon §3b | Step 4 |
| Avatar: `btn_greencircular` 65×65, `btn_welfare_bg` inset −10/−10, `008.png` inset −30/−30 PreserveAspect, level `"93"` bottom-right size 20 yellow | recon §4b | Step 5 |
| DeviceStatus: hidden by default, scale 0.5, bg black a=0.157, padding 10/10/5/5 spacing 20; Wifi 60×60, Battery 80×75 scale 1/0.6/1, Time `"07:36:50"`/RTT `"40ms"` green `0.0024/0.66/0` `hysz.ttf` size 40 | recon §5b | Step 6 |
| ProgressBar: spacing −10, width 150, Slider width 100 height 25, Text `"6757/8969"` size 16 yellow | recon §6b | Step 7 |
| Fonts: `UTM Cafeta #19.ttf`, `hysz.ttf` | recon §0 | Steps 3.3, 4, 5, 6 |

---

## 10. Test approach

### Category policy (IMPORTANT)
The existing `TopBarVltkUnityAdapterTests` already declares `[Category("HUD")]` (verified in source). **Reuse `HUD`** — do **not** introduce a parallel `HUDChrome` category; that would split filterable tests and violate the "one category per area" convention (parallel to `CaiBang`/`Slow`). The task's "`HUDChrome`" suggestion is superseded by the pre-existing `HUD` category. All new fixtures use `[Category("HUD")]`.

### Run policy (from root AGENTS.md)
- **Dev loop:** `unityMCP___run_tests(mode="EditMode", category_names=["HUD"])` — NEVER the full 4049-test suite.
- **Pre-push gate only:** full EditMode suite (after touching shared `HudDataBridge`/adapters/asmdef).
- Shared-catalog cache rule does not apply (HUD tests use synthetic fakes, not `PcCombatCatalogFactory`).

### New/extended test fixtures — `Assets/Tests/EditMode/UI/`

| File (create/modify) | Covers | Key assertions |
|---|---|---|
| `TopBarVltkUnityAdapterTests.cs` (extend) | T1/T3/T4 + §1 contract | stamina bar width = staminaFraction; stamina text = `cur/max` stamina (NOT life); MP bar uses maxMana not maxLife; expFraction from snapshot; `IRuntimeStateProvider` fake implements new members |
| `HudDataBridgeTests.cs` (create/extend) | §1 | `BuildSnapshot` populates stamina/maxMana/maxExp/expFraction; `RefreshAndPublish` fires on stamina change; `SnapshotsEqual` detects new fields |
| `MiniMapVltkUnityAdapterTests.cs` (extend) | M1/M3 | dot `left/top` = projected `(left/16+xRatio, yRatio−top/16)` for known input (centered offset); coord text = `"{top}:{left}"` order |
| `MoneyVltkUnityAdapterTests.cs` (create) | Y1/Y2 | ctor null-arg throws; `SetAmounts` updates 3 labels; Add button publishes `OnRechargeRequested(type)` |
| `AvatarVltkUnityAdapterTests.cs` (create) | A1/A2 | ctor null-arg throws; `Apply(snapshot)` sets level text from `snapshot.level`; `SetPortrait` accepts texture |
| `DeviceStatusVltkUnityAdapterTests.cs` (create) | D1/D2/D3 | ctor null-arg throws; `Tick()` with fake provider sets Time=`HH:mm:ss`, RTT=`Xms`, battery visibility; no direct `DateTime.Now`/`SystemInfo` calls (pure C#) |
| `HudProgressBarTests.cs` (create) | P1/P3 | `Set(0.5f, 50, 100)` → fill width 50%, label `"50/100"`; clamp 0..100% |

### Fixture shape (follow existing `TopBarVltkUnityAdapterTests`)
- `[TestFixture] [Category("HUD")]` at class level.
- `SetUp`: build synthetic `VisualElement` tree by name (no UXML load), construct adapter, `Bind()`.
- `TearDown`: `Dispose()`.
- Private `IRuntimeStateProvider` fake per fixture (now with stamina/maxMana/maxExp members).
- Assert on `style.width.value`, `Label.text`, bus event hit-counts.

---

## 11. Validation criteria (definition of done for the implementation lane)

1. **Compile:** all `IRuntimeStateProvider` implementors updated; assembly compiles.
2. **TopBar:** stamina bar/text bound to stamina (not life); MP max from runtime; EXP fraction real. Tests green.
3. **MiniMap:** dot position uses ported formula; coord text `top:left`. Tests green.
4. **Money/Avatar/DeviceStatus/ProgressBar:** adapters created, registered in `GameHudController`, each with EditMode tests (category `HUD`) green.
5. **Command bus:** new intents (`OnRechargeRequested`, any portrait/device providers) added additively; existing handlers unchanged.
6. **Visual parity:** carve-progress sprites, `UTM Cafeta`/`hysz` fonts, yellow text colors applied via USS (verified by UXML/USS presence + manual look — EditMode tests do not assert pixels).
7. **Vietnamese:** no zh user-facing strings; currency labels = Đồng tiền/Vàng/Bạc.
8. **No regression:** existing `HUD`-category tests stay green; `HudDataBridge` change-detection still fires.
9. **Test command (dev):** `unityMCP___run_tests(mode="EditMode", category_names=["HUD"])` all green.
10. **Pre-push:** full EditMode suite green (shared `HudDataBridge` touched).

---

## 12. Non-goals (scope fence)

- **M2 runtime minimap texture** (Phase-1 acknowledged) — not required this slice.
- **Portrait runtime provider wiring** (A4) — adapter testable via `SetPortrait`; real SPR/face binding is a separate task.
- **Panel/ranking/etc.** — out of scope (other slices).
- **uGUI prefab reuse** — impossible cross-framework; not attempted.
- **Pixel-perfect vltkunity layout** — parity is structural + data + visual-style, not 1:1 pixel RectTransform (different framework). Mobile scale policy is a separate decision.

---

## 13. Residual decisions for the implementer/reviewer

1. **M1 sign/projection:** confirm mobile minimap projection; assert dot lands correctly (don't blindly copy uGUI negation).
2. **Currency binding:** `HudSnapshot` fields vs separate `ICurrencyProvider` (§4.2).
3. **Recharge intent target:** which panel `OnRechargeRequested` opens (§4.4).
4. **Level text de-dup:** avatar frame vs TopBar `LevelText` (§5.1).
5. **Portrait runtime source** (A4) and **Wifi/Battery art sourcing** (D4).
6. **PanelSettings verify:** confirm existing mobile `PanelSettings` uses `ScaleWithScreenSize` (research finding 2) — read-only check; if `ConstantPixelSize`, raise separately.

---

## 14. Implementation order (suggested, dependency-respecting)

1. §1 data-contract (`HudDataBridge` + all implementors) — **unblocks 3 & 4.**
2. §3 TopBar C# fixes (T1/T4) + extend tests.
3. §2 MiniMap fixes (M1/M3) + extend tests.
4. §7 `HudProgressBar` helper + §3.3 visual parity (USS).
5. §4 Money adapter + tests + art.
6. §5 Avatar adapter + tests + art.
7. §6 DeviceStatus adapter + `IDeviceStateProvider` + tests + art.
8. §8 `GameHudController` wiring.
9. Run `HUD` category green → then full suite (pre-push).
10. Commit + push.

> Each numbered item is independently testable. §1 is the critical path (compile break until all implementors updated).
