# Archive Report — mobile-native-hud-layout (HUD-004)

## Status: ARCHIVED

## Change

`mobile-native-hud-layout` — redesign the runtime HUD arrangement from a PC-replica bottom
`快捷栏` toolbar into a mobile-native thumb-zone HUD while preserving PC sprite identity 1:1.

## Lifecycle

- research/explore (exa + current HUD audit) → proposal → spec → design → tasks → apply (S1/S2/S3)
  → verify (PASS) → sync (canonical `hud` updated) → archive

## Implementation Slices

- **S1** `9cdc76675` — mobile-native foundation: joystick on, strip bottom `快捷栏` replica, reserve
  chat lane.
- **S1 verify** `e63fd7a85` — HUD tests 13/13, vision PASS.
- **S2** `d23593071` — combat cluster 1+5 + run/horse/sit action buttons.
- **S2 verify** `2d65ff786` — HUD tests 13/13, combat vision PASS.
- **S3** `c17a1ea17` — quick slots + relocate PC menu to top gap.
- **S3 meta** test meta commit — adds test asset metadata.
- **Verify** — HUD tests 16/16, final vision PASS.
- **Sync** — canonical `openspec/specs/hud/spec.md` updated to HUD-004 baseline.

## Canonical Spec

Updated `openspec/specs/hud/spec.md` from the historical HUD-002 PC bottom-strip baseline to the
HUD-004 runtime mobile-native baseline:

- joystick bottom-left;
- 1 main + 5 sub combat cluster bottom-right;
- run/horse/sit beside combat;
- 3 usable-item quick slots on the right side;
- PC menu + Bảo Vật + buff in minimap↔topbar gap;
- bottom-center reserved for future chat;
- top status bar and minimap unchanged;
- no fabricated art; anchor-based layout.

## Verification Evidence

- S1 screenshot `Assets/Screenshots/mobile-hud-s1-overlay.png`: joystick visible bottom-left,
  bottom-center clear, top bar/minimap intact.
- S2 screenshot `Assets/Screenshots/mobile-hud-s2-combat.png`: 6 combat slots (1 main + 5 sub),
  run/horse/sit, no overlap.
- S3 initial screenshot found a real overlap (QuickSlot1 clipped a combat sub-slot); fixed by
  moving `.hud-quick-slots` up (`bottom: 320px → 390px`).
- S3 final screenshot `Assets/Screenshots/mobile-hud-s3-complete-fixed.png`: PASS — quick slots,
  top-gap menu, combat cluster, joystick, bottom-center all clean.
- HUD EditMode category: final `16/16 passed`, 0 failures.
- Verify report: PASS (10/10 requirements satisfied).

## No-Fabricated-Art Audit

All visible new/repositioned controls use existing PC art:

- `btn_skill_empty_pc.png` / PC skill-slot art for combat frames;
- `Generated/cai_bang_skill_*.png` for skill icons;
- `btn_run`, `btn_horse`, `btn_sit` for action buttons;
- `btn_quick_item_1/2/3_pc.png` for quick slots;
- `btn_status`, `btn_items`, `btn_itemex`, `btn_skills`, `btn_quest`, `btn_team`, `btn_faction`,
  `btn_chatroom`, `btn_treasure` for relocated top-gap menu.

## Follow-up

- Future mobile chat canvas owns the reserved bottom-center lane.
- Real combat tap→fire refinements / deck assignment UX.
- Consumable backend effect for quick slots (e.g. Ngũ Hoa Ngọc Lộ).
- Quest / ItemEx / ChatRoom popups (current handlers safe no-op/log).

## Archive Location

`openspec/changes/archive/2026-06-28-mobile-native-hud-layout/`
