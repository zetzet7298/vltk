# Sync Report — mobile-native-hud-layout (HUD-004)

## Status: SYNCED

## Executive Summary

Sync updated the canonical HUD domain spec:

`openspec/specs/hud/spec.md`

The previous canonical HUD spec represented the HUD-002 PC-parity full bottom `快捷栏` strip as the
runtime baseline. HUD-004 supersedes that ARRANGEMENT for mobile play. The new canonical baseline
is mobile-native layout with PC sprite identity preserved 1:1:

- joystick bottom-left;
- combat cluster 1+5 bottom-right;
- run/horse/sit action buttons beside combat;
- 3 usable-item quick slots on the right side;
- overflow PC menu buttons + Bảo Vật + buff panel in the minimap↔topbar gap;
- bottom-center clear/reserved for future chat;
- top status bar and minimap unchanged.

No implementation code was changed during sync.

## Canonical Reconciliation

| Prior canonical item | HUD-004 sync result |
|---|---|
| Full ornate `快捷栏` bottom strip visible | Historical HUD-002 baseline only; superseded as runtime mobile layout |
| PC art source `快捷栏.spr` hash `ebb69f9b` | Preserved as resource source; quick slots use `btn_quick_item_1/2/3_pc` slot-well art |
| PC bottom row menu/toggle arrangement | Replaced by mobile thumb-zone arrangement |
| Buttons functional/on top | Preserved under relocated top-gap menu + combat/action clusters |
| Top bar/minimap not covered | Strengthened as explicit unchanged regression guard |

## Requirements Synced

The canonical `hud` spec now contains requirements for:

- top bar and minimap unchanged;
- joystick enabled bottom-left;
- combat cluster 1+5 in the right-thumb zone;
- right-hand action buttons;
- 3 usable-item quick slots;
- overflow PC menu buttons in the minimap↔topbar gap;
- bottom-center reserved for chat;
- sprite-reuse invariant (no fabricated art);
- anchor-based layout;
- HUD regression tests.

## Verification Link

- verify-report.md status: PASS (10/10 spec requirements satisfied).
- S1/S2/S3 all implemented + runtime verified.
- HUD EditMode: S1/S2 `13/13`, S3 final `16/16`, all green.
- Vision screenshots:
  - `Assets/Screenshots/mobile-hud-s1-overlay.png` — S1 PASS.
  - `Assets/Screenshots/mobile-hud-s2-combat.png` — S2 PASS/Excellent.
  - `Assets/Screenshots/mobile-hud-s3-complete-fixed.png` — S3 PASS after quick-slot overlap fix.

## Next Recommended Phase

archive — implementation, verify, and sync are complete.

## Risks / Follow-up

- Future chat canvas owns the reserved bottom-center lane.
- Real combat tap→fire refinements and consumable backend effects remain gameplay follow-ups.
- Quest/ChatRoom/ItemEx popups are deferred; current handlers are safe no-op/log until their features land.
