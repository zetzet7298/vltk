# US-M61-001 M6.1 Touch Controls

## Status

implemented

## Lane

normal

## Intake

Intake #23 (spec_slice, normal). Flags: Cross-platform, New input surface.

## Product Contract

Touch controls make the game playable on phones/tablets: tapping a walkable map
cell moves the player, a virtual joystick drives continuous movement, skill buttons
start a cast flow, pinch zoom respects limits, and UI touch targets stay readable as
screen size changes.

## Relevant Product Docs

- `docs/spec.md` — "M6.1 — Touch Controls"

## Acceptance Criteria

- AC1: Mobile build runs; player taps walkable map; player moves to target.
- AC2: Virtual joystick enabled; player drags joystick; player moves continuously.
- AC3: Skill buttons visible; player taps skill; skill cast flow starts.
- AC4: Pinch gesture used; camera zooms; zoom respects limits.
- AC5: UI button displayed; screen size changes; touch target remains usable.

## Design Notes

- `TouchInputService` (pure C#): `Tap` (UI-hit vs move-to via ScreenToWorld),
  `JoystickToMove` (dead-zone + rescaled magnitude), `SkillButton` cast intent,
  `PinchZoom` (clamped delta), `TouchTargetPixels` (DPI-scaled, min 44pt).
- Reuses M2.1 movement + M2.3 zoom clamp + M4 skill cast downstream.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: tap move/UI, joystick deadzone/full/partial/direction, skill intent, pinch clamp/in, touch-target dpi/min |
| Integration | Feeds movement/camera/skill services (unit-covered) |
| E2E | Device touch in Play Mode (documented; not automated in EditMode) |
| Platform | Touch-target scaling asserted across DPI |
| Release | N/A |

## Harness Delta

Mobile input primitive feeding existing movement/camera/skill systems.

## Evidence

EditMode 386/386 pass (docs/evidence/editmode-results-2026-05-31-m6-mobile.json).
`TouchInputService` + `TapResult`/`SkillCastIntent`. Suite
`VLTK.Tests.Sandbox.TouchInputServiceTests` (11 tests) covers AC1–AC5.
