# Spec — Bottom Bar PC-Parity Frame (HUD-002)

> Change: `add-bottom-bar-pc-frame`
> Spec format: requirement/scenario deltas vs current baseline

## Requirements

### REQ-1 — Ornate filigree frame visible
The `.hud-bottom-strip` MUST display the PC ornate antique-silver filigree housing
(left low-profile band + raised center crown over T/P + right double-banded menu +
circular right end-cap for Bảo Vật), matching `pc-evidence/hud/bottom_bar.png`.

### REQ-2 — No aspect-ratio distortion
Frame art MUST preserve its original aspect ratio. The 4:3-origin frame (933×120)
MUST NOT be raw-stretched across the 16:9 mobile strip. Use anchor + `scale-to-fit`
or 9-slice so scrollwork stays crisp; the strip may extend with a matching dark fill
on the far side if narrower than art.

### REQ-3 — Buttons remain functional & on top
All existing button click wiring (`GameHudController.RegisterClick`) MUST keep working.
The frame background MUST use `pickingMode: Ignore`; buttons stay at z-top.

### REQ-4 — PC-proportional positioning
Hotkey slots 1–9, T/P skill slots, toggle row (6), menu row (8), and Bảo Vật MUST be
repositioned to match PC coordinates from `dc11ac12.ini` (README §6.2), scaled to the
mobile design resolution, anchor-based.

### REQ-5 — No regressions
- Chat panel/tabs/red-warning MUST sit cleanly ABOVE the strip (no overlap).
- Hotkey slots MUST NOT overlap each other (regression guard vs `2bc2f3128`).
- HUD EditMode test category MUST stay green.

## Scenarios

### S1 — Frame renders
GIVEN the Sandbox scene is playing
WHEN the HUD loads
THEN the bottom strip shows the ornate filigree frame (crown + end-cap + bands)
AND no flat dark-green rectangle remains as the primary frame.

### S2 — Buttons clickable over frame
GIVEN the filigree frame is rendered
WHEN the user taps BtnStatus / BtnItems / BtnSkills / etc.
THEN the corresponding click handler fires (no frame layer swallows the input).

### S3 — Hotkeys not overlapping
GIVEN 9 hotkey slots
THEN they render as a clean spread row (vision confirms no stacking).

### S4 — Chat not overlapping strip
GIVEN the strip is now taller (frame art ~120px-equivalent)
THEN the chat panel + red warning sit above the strip top edge.

## Acceptance

- `vision ui_diff_check` (mobile vs `bottom_bar.png`) frame-section match ≥ 80%.
- HUD EditMode tests: 0 failures.
- Visual review: crown + end-cap + bands recognisable; no distortion.
