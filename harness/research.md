Everything is in order. My deliverable `harness/research/s5-research.md` is written (87 lines), `progress.md` is updated. No Unity code was edited (research-only constraint honored). The `harness/research/` directory is untracked (new), which is expected.

Let me produce the final research brief output and acceptance report.

The research is complete. Here is the summary of findings:

# Research: Unity APIs/patterns for HUD chrome port (uGUI → UI Toolkit)

## Key finding: This is a CROSS-FRAMEWORK port
The source (vltkunity) uses **uGUI** (Canvas, Image, Text, Slider, RectTransform); the target (vltk-mobile) uses **UI Toolkit** (VisualElement, Label, USS). This is NOT a self-contained same-framework port — external research was genuinely warranted.

## Research questions
1. How to implement a Slider-style fill bar in UI Toolkit?
2. What's the UI Toolkit equivalent of uGUI CanvasScaler for mobile?
3. How to map uGUI RectTransform anchors + LayoutGroups to UI Toolkit?
4. How to display a runtime minimap RenderTexture + position a player dot?
5. How to apply carved-progress sprite fills + 9-slice backgrounds?

## Key findings
1. **Fill-bar = child VisualElement with `style.width` in Percent** (NOT the built-in `ProgressBar`). Community + Unity's own minimap sample converge on manual background+fill elements. The mobile adapter's existing `SetBar()` pattern is confirmed correct.
2. **`PanelSettings` ScaleMode = `ScaleWithScreenSize`** is the direct equivalent of uGUI CanvasScaler — Reference Resolution + Screen Match Mode (Match Width/Height, Expand, Shrink).
3. **uGUI anchoring/pivots have NO direct equivalent** — UI Toolkit uses flexbox (Yoga). For absolute positioning (player dot), use `position: absolute` + `left`/`top`. **VisualElements use top-left origin by default** (offset markers by half-size to center).
4. **Minimap = `Background.FromRenderTexture(rt)` on `style.backgroundImage`**. Use `-unity-background-scale-mode: scale-and-crop`. Gotcha: transparent VisualElement bg makes black pixels transparent.
5. **9-slice via `-unity-slice-*` USS properties**, but NO `sliced` scale-mode value (only stretch-to-fill/scale-and-crop/scale-to-fit). Sprite borders apply automatically. **PPU gotcha:** PPU=16 → 0.16× scale adjustment.

The full 87-line brief with Unity API notes, gotchas, and source citations is written to `harness/research/s5-research.md`.