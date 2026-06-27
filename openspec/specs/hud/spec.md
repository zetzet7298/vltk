# HUD Specification

> Domain: **hud** (canonical spec, established by change `add-bottom-bar-pc-frame` / HUD-002).
> Source of truth: PC `\spr\UI3\主界面\快捷栏(800).spr` (hash `ebb69f9b`) recovered via
> `jx-pc-resource-resolver`, decoded to `bottom_frame_pc.png` (863×91, aspect 9.48, 92%
> transparent overlay). PC INI `dc11ac12.ini` bottom-bar coordinates. `default_locale: vi`.

## Purpose

Establish the bottom HUD bar as a PC-parity ornate filigree frame with functional button
containers overlaid on top, so the mobile bottom strip visually matches the PC antique-silver
toolbar housing while preserving all button click wiring.

## Requirements

### Requirement: Ornate Filigree Frame Visible

The `.hud-bottom-strip` MUST display the PC ornate antique-silver filigree housing (left
low-profile band, raised center crown over T/P slots, right double-banded menu, circular right
end-cap for Bảo Vật), sourced from the recovered PC SPR `快捷栏.spr`.

#### Scenario: Frame renders

- GIVEN the Sandbox scene is playing
- WHEN the HUD loads
- THEN the bottom strip shows the ornate filigree frame (crown + end-cap + bands)
- AND no flat dark-green rectangle remains as the primary frame

### Requirement: Aspect Ratio Preserved

Frame art MUST preserve its original aspect ratio. The 4:3-origin frame MUST NOT be
raw-stretched across the 16:9 mobile strip. The implementation MUST use anchor-based layout +
`scale-to-fit` (or 9-slice) so scrollwork stays crisp; the strip may extend with a matching
dark fill on the far side if narrower than the art.

#### Scenario: No distortion

- GIVEN the mobile strip is 16:9 and the frame art is 4:3-origin
- WHEN the frame is laid out
- THEN the art preserves aspect ratio with no horizontal stretch distortion

### Requirement: Buttons Functional and On Top

All existing button click wiring (`GameHudController.RegisterClick`) MUST keep working. The
frame background layer MUST use `pickingMode: Ignore`; buttons MUST stay at z-top above the
frame so input is not swallowed.

#### Scenario: Buttons clickable over frame

- GIVEN the filigree frame is rendered
- WHEN the user taps BtnStatus / BtnItems / BtnSkills / etc.
- THEN the corresponding click handler fires (no frame layer swallows the input)

### Requirement: PC-Proportional Positioning

Hotkey slots 1–9, T/P skill slots, toggle row (6), menu row (8), and Bảo Vát MUST be
positioned to match PC coordinates from `dc11ac12.ini`, scaled to the mobile design
resolution, anchor-based (not raw multiplied).

#### Scenario: Hotkeys not overlapping

- GIVEN 9 hotkey slots
- THEN they render as a clean spread row (no stacking)

### Requirement: No Regressions

- The chat panel, tabs, and red-warning MUST sit cleanly above the strip (no overlap).
- Hotkey slots MUST NOT overlap each other.
- The HUD EditMode test category MUST stay green.

#### Scenario: Chat not overlapping strip

- GIVEN the strip is now taller (frame art ~120px-equivalent)
- THEN the chat panel + red warning sit above the strip top edge

## Follow-up (tracked separately, not part of this domain spec's baseline)

- Fine pixel-align each button to its frame slot well.
- Lock circular toggle button aspect ratio to 1:1.
- Decide whether to keep PC-art `左/右` labels in the crown or overlay `T/P`.
- Reconcile mobile chat panel style with PC chat input bar.
