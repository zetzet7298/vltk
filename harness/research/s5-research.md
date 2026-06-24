# Research: Unity APIs/patterns for HUD chrome port (uGUI → UI Toolkit)

**Date:** 2026-06-23
**Context:** Port HUD chrome (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar) from **vltkunity** (source, uses **uGUI**) to **vltk-mobile** (target, uses **UI Toolkit**). This is a **cross-framework** port, not same-framework — the source uses Canvas/Image/Text/Slider/RectTransform; the target uses VisualElement/Label/USS.

> **Note:** This research is genuinely needed because the port crosses two Unity UI frameworks (uGUI → UI Toolkit). The mapping is non-trivial (anchoring vs flexbox, Slider fill vs width-%, CanvasScaler vs PanelSettings).

---

## Research questions

1. How to implement a Slider-style fill bar (uGUI `Slider.m_FillRect`) in UI Toolkit?
2. What is the UI Toolkit equivalent of uGUI `CanvasScaler` (Scale With Screen Size) for mobile responsive scaling?
3. How to map uGUI `RectTransform` anchors/pivots + `HorizontalLayoutGroup`/`ContentSizeFitter` to UI Toolkit layout?
4. How to display a runtime minimap texture (RenderTexture) and position a player dot in UI Toolkit?
5. How to apply carved-progress sprite fills + 9-slice backgrounds in UI Toolkit?

---

## Key findings

1. **Fill-bar = child VisualElement with `style.width` in Percent (NOT the built-in `ProgressBar`).** Community + Unity's own minimap sample converge on manually building a bar from a background VisualElement + a child fill VisualElement, then setting `fill.style.width = new StyleLength(Length.Percent(pct))`. Unity's built-in `ProgressBar` control is poorly suited for game HUD bars: its child elements are non-persistent (generated at runtime), so they can only be styled via USS selectors, not inline; multiple bars with different fill colors require per-instance USS classes; and it shows an unwanted title/border by default. [Source: SO](https://stackoverflow.com/questions/66936669/unity-new-ui-toolkit-progress-bar), [Unity Discourse](https://discussions.unity.com/t/the-image-in-ui-toolkit-progress-bar-cant-fill-up-like-ugui-image/1701882), [Reddit](https://www.reddit.com/r/unity/comments/1p630f/no_idea_how_to_edit_progress_bars_in_ui_toolkit/). **The mobile adapter already uses this pattern** (`SetBar(fill, fraction)` → `fill.style.width = Length(pct, Percent)`) — confirmed correct.

2. **`PanelSettings` ScaleMode = `ScaleWithScreenSize` is the direct equivalent of uGUI `CanvasScaler`.** Set `ScaleMode = Scale With Screen Size`, define a `Reference Resolution`, and choose `Screen Match Mode` (`Match Width or Height` with a 0–1 Match value, or `Expand`/`Shrink`). The `UIDocument` component references the `PanelSettings` asset (analogous to Canvas referencing CanvasScaler). For the HUD port, the existing mobile `PanelSettings` should be verified to use `ScaleWithScreenSize` so pixel values authored against a reference resolution scale consistently across devices. [Source: Unity Manual — Panel Settings](https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-Runtime-Panel-Settings.html), [Unity Manual — Designing UI for Multiple Resolutions](https://docs.unity3d.com/2022.3/Documentation/Manual/HOWTO-UIMultiResolution.html).

3. **uGUI anchoring/pivots have NO direct equivalent in UI Toolkit** — the layout is flexbox (Yoga), where layout affects siblings by default (comparable to every element being inside a uGUI `LayoutGroup` with a `LayoutElement`). To place an element at an absolute position (e.g., minimap player dot), use `position: absolute` + `left`/`top` USS properties. **VisualElements use top-left as the origin by default** (unlike uGUI pivots), so centering a marker requires offsetting by half its size. [Source: Unity Manual — Migrate from uGUI to UI Toolkit](https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-Transitioning-From-UGUI.html), [Unity minimap sample feedback](https://discussions.unity.com/t/seeking-feedback-on-upcoming-sample-ui-toolkit-minimap/1518824/).

4. **Minimap runtime texture = `Background.FromRenderTexture(rt)` on `style.backgroundImage`.** The canonical pattern: render an overhead camera into a `RenderTexture`, then assign it via `visualElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture))`. For dynamic resizing, recreate the RenderTexture on `GeometryChangedEvent`. Set `-unity-background-scale-mode: scale-and-crop` to avoid stretching when map aspect ratio differs. **Gotcha:** a transparent VisualElement background makes pure-black render pixels appear transparent — set a solid background color with alpha=255. **Editor-only note:** runtime panels repaint the RenderTexture every frame, but Editor windows only repaint on demand (call `MarkDirtyRepaint()`). [Source: Unity Discourse](https://discussions.unity.com/t/how-to-set-a-rendertexture-as-a-background-image-at-runtime/906830), [Unity Manual — Set background images](https://docs.unity3d.com/6000.4/Documentation/Manual/UIB-styling-ui-backgrounds.html), [LlamAcademy minimap tutorial](https://www.youtube.com/watch?v=XJuqif5wdus).

5. **9-slice / carved sprite backgrounds work via `-unity-slice-*` USS properties**, but there is NO `-unity-background-scale-mode: sliced` value — the valid values are `stretch-to-fill | scale-and-crop | scale-to-fit`. The Sprite's own 9-slice borders (set in the Sprite Editor) apply automatically when used as a background; override via `-unity-slice-left/top/right/bottom` in USS. **Pixels-per-unit gotcha:** UI Toolkit adjusts `-unity-slice-scale` by the sprite's PPU relative to the panel's `reference sprite pixels per unit` (default 100). A sprite with PPU=16 gets scaled by 0.16× — relevant if JX art uses low PPU. For tiling fills (e.g., a repeating carve-progress texture), use `background-repeat: repeat no-repeat` + `background-position-x` on the fill element. [Source: Unity Manual — USS common properties](https://docs.unity3d.com/6/Documentation/Manual/UIE-USS-SupportedProperties.html), [Unity Manual — Set background images](https://docs.unity3d.com/Manual/UIB-styling-ui-backgrounds.html), [Unity Discourse](https://discussions.unity.com/t/the-image-in-ui-toolkit-progress-bar-cant-fill-up-like-ugui-image/1701882).

6. **Texture2D (static art) backgrounds: `new StyleBackground(texture2D)` or `Resources.Load<Sprite>`.** For porting the carve-progress sprites (`hp.png`, blue/green fills, `img_carve_progressbg.png`), money icons (`tongqian/jinbi/yinliang.png`), and avatar frame (`btn_greencircular.png`) into UI Toolkit, assign them as `background-image` on VisualElements (via USS `url("resource:...")` or C# `style.backgroundImage`). For `scale-to-fit` (preserve aspect, like uGUI `Image.Type=Simple, PreserveAspect`), use `-unity-background-scale-mode: scale-to-fit`. [Source: Unity Manual — Set background images](https://docs.unity3d.com/6000.4/Documentation/Manual/UIB-styling-ui-backgrounds.html).

---

## Unity API notes

### Progress bar (TopBar HP/MP/Stamina)
- **Recommended:** manual background + fill VisualElement, `fill.style.width = Length(pct, Percent)`. This matches the existing mobile `SetBar()` pattern exactly. ✅
- To add the carved sprite fill visual (parity gap T5/P3): set `background-image: url("hp.png")` on the fill element + `-unity-background-scale-mode: scale-to-fit` (or `stretch-to-fill` if exact fill look is needed).
- Per-bar text label (`"6757/8969"`): use a `Label` child centered over the bar. Already present as separate labels (`HpText`, etc.) in mobile — ✓.

### PanelSettings (mobile scaling)
- API: `PanelSettings.scaleMode`, `PanelSettings.referenceResolution`, `PanelSettings.screenMatchMode`.
- Verify the existing mobile `PanelSettings` asset uses `ScaleMode.ScaleWithScreenSize` with an appropriate reference resolution. If it's on `ConstantPixelSize`, px values won't scale across devices.

### MiniMap
- Map texture: `style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt))`.
- Player dot position (parity gap M1): port the formula `xx=(left/16)+xRatio; yy=yRatio-(top/16); anchoredPosition(-xx,-yy)` → in UI Toolkit: `_playerDot.style.left = -xx; _playerDot.style.top = -yy;` with `position: absolute`.
- Background scale: `scale-and-crop` (Unity's own minimap sample choice).
- Coordinate text order (gap M3): swap to `$"{top}:{left}"`.

### Money / Avatar / DeviceStatus (new widgets)
- Money: 3 rows in a flex row (`flex-direction: row`), each = icon (VisualElement w/ background-image) + Label amount + Button (add). Use `flex-direction: row` + `flex-wrap: nowrap` (maps to uGUI `HorizontalLayoutGroup`). `ContentSizeFitter` ≈ flexbox auto-sizing (no explicit width; let content drive).
- Avatar: nested VisualElements — frame (bg `btn_greencircular.png`) > portrait bg (`btn_welfare_bg.png`, inset via padding/margin) > portrait (`008.png`, `-unity-background-scale-mode: scale-to-fit`) + Label level bottom-right (`position: absolute; right:0; bottom:0`).
- DeviceStatus: flex row with Wifi/Battery (VisualElement bg-image), Time/RTT Labels (green color, `hysz.ttf`). Live updates: `System.DateTime.Now.ToString("HH:mm:ss")`, `Application.internetReachability` / ping for RTT, `SystemInfo.batteryLevel` for battery.

### 9-slice / sprites
- Valid `-unity-background-scale-mode`: `stretch-to-fill | scale-and-crop | scale-to-fit` (NO `sliced`).
- Sprite slice borders apply automatically; override via `-unity-slice-*` USS properties.
- PPU adjustment: `effective_scale = uss_scale * (sprite_PPU / 100)`.

---

## Gotchas / warnings

- **uGUI → UI Toolkit is NOT a prefab copy.** Prefabs/RectTransform cannot be directly reused; the entire visual tree must be rebuilt as UXML/USS + C# VisualElement manipulation. The recon confirms vltk-mobile already does this (adapters query VisualElements by name from UXML).
- **VisualElement origin is top-left**, not pivot-centered like RectTransform. Player dots / markers must be offset by `-width/2, -height/2` to center on a coordinate. (Relevant to MiniMap dot M1.)
- **Built-in `ProgressBar` control is a trap for HUD bars** — use manual fill-width elements. The mobile adapter already does this correctly.
- **RenderTexture transparency gotcha:** black pixels render transparent if the VisualElement background alpha is 0. Set a solid background color (alpha 255) behind the render texture.
- **Editor repaint:** RenderTextures in Editor windows only update on demand (`MarkDirtyRepaint()`); runtime panels update every frame. EditMode tests should not assert on RenderTexture frame content.
- **PPU scaling:** JX sprites with low PPU (e.g., 16) will appear tiny when used as 9-slice backgrounds unless `-unity-slice-scale` is adjusted or PPU set to 100.
- **`-unity-background-scale-mode` has no `sliced` value** despite many expecting one. 9-slice works through the Sprite's own border + `stretch-to-fill`. Confirmed in Unity 6 docs and Discourse.
- **9-slice tiling** (`-unity-slice-type: tiled`) only works for Texture images, NOT Sprites (as of Unity 6000.0.50+). If tiled carved fills are needed, import as Texture not Sprite.
- **Fonts:** `UTM Cafeta #19.ttf` / `hysz.ttf` must be added as Font Assets and referenced in USS (`-unity-font`) or via `Label`'s font style. TTF import for UI Toolkit requires the font asset to be set on the PanelSettings Text Settings or referenced in USS.

---

## Confidence

**high** — All findings sourced from official Unity documentation (PanelSettings, USS properties, background images, migration guide) and corroborated by community consensus (SO, Unity Discourse, Reddit) with multiple independent sources agreeing on the same patterns. The mobile adapter's existing `SetBar()` pattern is independently confirmed as the correct UI Toolkit approach.

---

## Supervisor coordination

None needed. Research is self-contained; no decisions blocked. Implementation lane should proceed with the patterns above.
