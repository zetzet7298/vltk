# Design — Bottom Bar PC-Parity Frame (HUD-002)

> Change: `add-bottom-bar-pc-frame`

## Key Decision (ADR-001): Frame art source = real PC SPR 快捷栏.spr (RECOVERED)

**Context:** The PC in-game toolbar filigree SPR was initially assumed unrecoverable. An
initial attempt to derive the frame from the user screenshot `bottom_bar.png` failed —
that image was contaminated (game-world bleed, T/P text, portrait, tooltips "Bạn Hữu"/
"Bảo Vật", buff icons) and could not be cleanly masked.

**Recovery (via skill `jx-pc-resource-resolver`):** The PC INI `dc11ac12.ini` `[Main]`
references `\spr\UI3\主界面\快捷栏(800).spr` (commented). Applying the JX Pack Hash UID
algorithm (GBK-encode normalized lowercase path → hash) to the base name `快捷栏.spr`
yielded hash `ebb69f9b`, located at:
`pak_unpacked/updatejx08/unknown/ebb69f9b.spr`.

**Verification:** Decoded via `extract_item_spr.py` → 965×768 single-frame overlay. Content
bbox = toolbar at bottom (~y628-715 of 716-tall content canvas). Cropped to the toolbar
region → `bottom_frame_pc.png` (863×91, aspect 9.48, 92% transparent = true overlay).
Vision confirmed 10/10 clean: both circular jade/silver end-caps, raised center crown
over 2 empty T/P slots with 左/右 labels, 9 numbered hotkey slots + unnumbered slots,
full top+bottom scrollwork bands. **No contamination.**

**Decision:** Use this recovered `bottom_frame_pc.png` as the authoritative toolbar frame
art. Overlay the existing real-SPR mobile buttons (from commit `2bc2f3128`) on top of the
frame's slot wells. No screenshot-based masking, no USS synthetic filigree.

**Note on slot count:** PC frame shows 9 numbered + ~10-12 unnumbered slots. Mobile will
overlay its own button containers (6 toggle + 8 menu) over the unnumbered region; exact
slot-perfect alignment is a polish task, not a blocker.

## Architecture

### Layering (z-order, bottom → top)
1. **Frame background** — new `hud-bottom-frame` VisualElement, `pickingMode: Ignore`,
   `background-image: url('bottom_frame_pc.png')`, `scale-to-fit` (aspect-locked), full
   strip width.
2. **Slot/button containers** — existing `HotbarCenter`, `hud-skill-panel`,
   `hud-right-cluster` (toggle row + menu row), `BtnTreasure` — positioned via the frame's
   coordinate system (see below), z-above frame.
3. **Chat panel** — absolute, above strip top edge.

### Coordinate mapping (PC 1024-space → mobile design space)
PC bottom bar occupies `Left=0, Top=400..490` in 1024×768 (height 90, plus crown rising
above). Map PC X (0–1024) → mobile strip-local X via the same anchor family already used
for the topbar. Frame art `bottom_frame_pc.png` is placed at the strip's hotkey-origin so
its internal slot wells align with the overlaid buttons.

### Frame asset production
1. Load `bottom_bar.png` (RGBA).
2. For each known button cell (hotkey 1–9 grid, T/P, 8 menu, 6 toggle, Bảo Vạt circle),
   erase/alpha-zero the interior, keeping only the filigree frame + slot borders.
3. Save `Assets/UI/HUD/Art/bottom_frame_pc.png` (+ StreamingAssets copy).
   - Fallback if erase is imperfect: keep buttons in the frame PNG; overlaying identical
     real-SPR mobile buttons on top hides any double-image. Acceptable per REQ-3.

### USS changes
- `.hud-bottom-strip`: replace `bottom_bar_bg.png` + flat `background-color` with layered
  `hud-bottom-frame` child; keep height tuned to frame aspect (≈120 design px → mobile).
- Add `.hud-bottom-frame` rule: absolute inset 0, `pickingMode: ignore`,
  `-unity-background-scale-mode: scale-to-fit` (or `stretch` if 9-slice preferred).
- `.hud-right-cluster` / rows: nudge positions to align with frame wells.

### UXML changes
- Insert `<ui:VisualElement name="BottomFrame" class="hud-bottom-frame"/>` as first child
  of `BottomPanel`, before `HotbarCenter`.

### C# changes
- None required for frame (pure USS/UXML). `LoadArt()` already loads button icons; no
  dict changes. (Confirm `RegisterClick` still resolves — names unchanged.)

## Risks & mitigations
- **R-A** Frame art erase leaves seams → use fallback (keep buttons, overlay). Low risk.
- **R-B** Aspect mismatch on ultra-wide → `scale-to-fit` + dark fill extension. Low.
- **R-C** Review budget: this is ~1 new asset + USS edits + minor UXML; forecast < 200
  changed lines → single PR, within 400 budget.

## Test plan
- `vision ui_diff_check` mobile vs `bottom_bar.png` (acceptance ≥ 80% frame match).
- HUD EditMode category (MCP run_tests).
- Manual: tap each menu/toggle button in play mode → handler fires.
