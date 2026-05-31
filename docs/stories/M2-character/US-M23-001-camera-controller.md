# US-M23-001 M2.3 Camera Controller

## Status

implemented

## Lane

normal

## Intake

Intake #11 (spec_slice, normal). Flags: Cross-platform.

## Product Contract

Camera follow/zoom/pan tools let a developer inspect maps quickly: the camera
follows the player, a GM unlock enables free pan to any map area, zoom (pinch /
wheel) stays within configured min/max, and a reset returns the camera to the
player/default target.

## Relevant Product Docs

- `docs/spec.md` — "M2.3 — Camera Controller"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Player exists; camera follow enabled; camera follows player.
- AC2: GM unlocks camera; developer drags/pans; camera can inspect any map area.
- AC3: Pinch or mouse wheel used; zoom changes; zoom remains within configured
  min/max.
- AC4: GM reset camera clicked; command runs; camera returns to player/default
  target.

## Design Notes

- `CameraRigService` (pure C#): `CameraMode` Follow/Free, `SetFollowTarget`/
  `EnableFollow` (AC1), `Unlock`+`Pan` ignored in Follow (AC2), `ZoomBy`/`SetZoom`
  clamped to min/max (AC3), `Reset` → Follow + snap to target (AC4).
- MonoBehaviour driver applies `Focus`/`Zoom` to a Unity Camera (documented).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: follow tracking, pan in/ignored, zoom clamp (by/set), reset, initial-zoom clamp |
| Integration | Driver maps Focus/Zoom to Camera (documented) |
| E2E | GM camera controls in Play Mode (documented; not automated in EditMode) |
| Platform | Zoom clamp shared across platforms (asserted) |
| Release | N/A |

## Harness Delta

Camera inspection rig reused by every later visual milestone.

## Evidence

EditMode 248/248 pass (docs/evidence/editmode-results-2026-05-31-m2-character.json).
`CameraRigService` + `CameraMode`. Suite `VLTK.Tests.Sandbox.CameraRigTests` (10
tests) covers AC1–AC4 plus initial-zoom clamp.
