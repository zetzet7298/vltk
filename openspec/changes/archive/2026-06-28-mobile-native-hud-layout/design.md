# Design — Mobile-Native HUD Layout (HUD-004)

> Change: `mobile-native-hud-layout`
> Design space: **1280×720** (16:9). All art = existing ported PC sprites (no fabrication).
> `default_locale: vi`.

## ADR Summary

- **ADR-1 — Mobile arrangement, PC art 1:1.** Abandon the PC bottom-toolbar *arrangement*;
  preserve every PC *sprite*. The visual identity is PC; only ergonomics become mobile-native.
- **ADR-2 — "Main" combat slot = player-chosen priority, no fixed role.** All 6 slots assignable.
- **ADR-3 — Bottom-center reserved for future chat.** No control element lands there.
- **ADR-4 — Overflow PC UI → minimap↔topbar gap.** 8 menu buttons + buff panel relocate up, not
  dropped, preserving every PC affordance.
- **ADR-5 — Anchor-based USS; no raw pixel-multiply.** Aspect ratios preserved.
- **ADR-6 — Slot frames wired to real PC sprites** (replace current `background-image: none`
  placeholders with `btn_skill_empty_pc.png` for combat and a `快捷栏` numbered-well crop for
  quick slots).

## Screen Regions (anchor map, 1280×720)

```
┌───────────────────────────────────────────────────────────┐
│  [TopLeftPanel status bar]          ┌──MinimapPanel──┐     │  TOP band (y: 0..~150)
│  unchanged                          │ unchanged       │     │
│                                     └─────────────────┘     │
│  ┌──TopGapCluster────────────────┐                          │  TOP-GAP (between bar & map)
│  │ [8 menu btns] row (btn_*_f*)  │                          │  relocates overflow PC UI here
│  │ [BuffPanel] (when buffs exist)│                          │
│  └───────────────────────────────┘                          │
│                                                            │
│                       (game world — kept clear)             │
│                                                            │
│                                  ┌─QuickSlots─┐             │  RIGHT (ascends toward minimap)
│                                  │ [qs1]      │             │  3 usable-item slots (Ngũ Hoa
│                                  │ [qs2]      │             │  Ngọc Lộ etc), PC numbered-well
│                                  │ [qs3]      │             │  chrome
│                                  └────────────┘             │
│ ┌──Joystick──┐        ┌─CombatCluster─────┐                 │  BOTTOM-RIGHT (right-thumb fan)
│ │ MobileJoy  │        │  [sub5][sub4]     │                 │  1 main (big) + 5 sub
│ │ stick      │        │     [ MAIN ]      │                 │
│ │ (left)     │        │  [sub1][sub2][sub3]│                │
│ └────────────┘        │  [run][horse][sit]│                 │  + action buttons beside cluster
│       LEFT            └───────────────────┘    RIGHT        │
│                                                            │
│            [ChatBar]  ← bottom-center lane RESERVED          │  BOTTOM-CENTER (chat)
└───────────────────────────────────────────────────────────┘
```

### Anchors (USS position strategy)
- **Joystick**: uGUI `MobileJoystick` already anchors bottom-left (sortingOrder 500). No USS
  change; only remove the `HideMobileJoystick` force-hide. Left-thumb zone.
- **CombatCluster**: absolute, anchored bottom-right. `right: ~24px; bottom: ~24px`.
- **QuickSlots**: absolute, anchored right side, ascending. `right: ~28px; bottom: ~280px` and
  upward.
- **TopGapCluster**: absolute, anchored top, between `TopLeftPanel` right edge and minimap left
  edge. `top: ~12px; left: <after status bar>`.
- **ChatBar**: stays bottom-center (current). Lane explicitly reserved; no control element added
  there.

## Combat Cluster Geometry (right-thumb fan)

Built on the ACM thumb-zone evidence (primary comfort radius ~27–41 mm; high-frequency buttons
along the screen-edge fan; the centered two slots are most noticed → main skills).

- **Main slot**: `96×96` design px (≈ visually larger; sits at the thumb's natural rest point,
  centered). Reuses `btn_skill_empty_pc.png` (scale-to-fit) as frame.
- **5 sub slots**: `64×64` design px each, arranged as a right-fan around the main slot:
  - 3 along the lower arc (sub1/sub2/sub3) under the main slot,
  - 2 along the upper arc (sub4/sub5) above-right.
- Fan radius from main center ≈ `110–130` design px (keeps sub slots inside the 27–41 mm comfort
  arc on a ~5.5" device at 1280 design width). Exact px tuned in apply with a screenshot check.
- Slot icon: `44×44`–`56×56` centered inside the frame; reuses `Generated/cai_bang_skill_*.png`.
- All 6 slots carry a small assignable hotkey hint label (optional, VI).

> The fan geometry is finalized at apply with a vision screenshot (REQ acceptance). Design
> fixes the topology (1 main + 5 sub in a right fan) and the sprite sources; px are tuned.

## Quick Slots (3) — usable items

- `56×56` design px each, stacked vertically ascending the right side (qs1 lowest → qs3 highest),
  between the combat cluster and the minimap.
- **Frame chrome**: crop a single numbered well from the already-decoded `快捷栏` SPR
  (`bottom_frame_pc.png` source / `ebb69f9b.spr`) → `qs_slot_frame_pc.png`. Cropping a region of
  the genuine decoded SPR is resource extraction, NOT fabrication (same method that produced
  `bottom_frame_pc.png`). Fallback if the crop is unclear: reuse `btn_skill_empty_pc.png`
  styling (still PC art).
- Each slot assignable to a usable item (icon from item DB). Activate = consume intent (backend
  effect is a separate gameplay change; this change = assignment + activation UI).

## Action Buttons (run / horse / sit)

- `48×48` design px each, in a small row beside/below the combat cluster (right-thumb reach).
- Icons: `PcButtons/btn_run.png`, `PcButtons/btn_horse.png`, `PcButtons/btn_sit.png` (with
  `_over` hover states wired for over/normal).
- These reuse the exact PC toggle-row sprites (no restyle).

## TopGapCluster — relocated overflow PC UI

- A compact container holding the **8 menu buttons** (`btn_char_f1` … `btn_chatroom`,
  `btn_itemex`) as a horizontal row of `36×36` PC-icon buttons, placed in the top gap (between
  the status bar's right edge and the minimap's left edge).
- **BuffPanel** renders below that row when buffs exist (same content, relocated).
- Click handlers unchanged: each `BtnXxx` still calls `OnXxxClick` → `PopupManager.Show(...)`.
- If the 8-button row overflows the gap width at 1280, wrap to 2 compact rows or shrink to
  `32×32` (still ≥44pt hit via invisible padding per ADR-5).

## Bottom Strip — removed from center

- The PC `快捷栏` replica `BottomPanel` bottom-center strip (the 9 numbered hotbar + T/P + toggle
  row + menu row) is **removed from the center lane**.
- Surviving elements are redistributed: menu row → TopGapCluster; toggle row → action buttons
  (run/horse/sit) beside combat; T/P skill slots → folded into the 6-slot combat cluster; the
  9 hotbar slots → superseded by the combat cluster (6) + quick slots (3).
- `BtnTreasure` (Bảo Vật) → relocated into TopGapCluster (or beside minimap) — PC sprite kept.

## UXML Changes

Restructure `GameHud.uxml`:
- Keep `TopFrameBg`, `TopLeftPanel`, `MinimapPanel`, `MapPreviewOverlay`, `ChatBar`, popups as-is.
- Replace `BottomPanel` (bottom-center strip) with two anchored clusters:
  - `<VisualElement name="CombatCluster" class="hud-combat-cluster">` → main + 5 sub slots +
    action buttons (run/horse/sit) + (optional) trade/camera/pk.
  - `<VisualElement name="QuickSlots" class="hud-quick-slots">` → qs1/qs2/qs3.
- Add `<VisualElement name="TopGapCluster" class="hud-top-gap-cluster">` holding the relocated 8
  menu buttons (BtnStatus…BtnChatRoom) + BtnTreasure + BuffPanel.
- Remove the inline `快捷栏` strip markup from the center (keep `bottom_frame_pc.png` art only if
  reused for quick-slot chrome extraction).

## USS Changes

- New classes: `.hud-combat-cluster`, `.hud-combat-main-slot`, `.hud-combat-sub-slot`,
  `.hud-quick-slots`, `.hud-quick-slot`, `.hud-top-gap-cluster`, `.hud-action-btn`.
- **Wire PC sprite frames** (replace `background-image: none`):
  - `.hud-combat-main-slot` / `.hud-combat-sub-slot` → `btn_skill_empty_pc.png` (scale-to-fit).
  - `.hud-quick-slot` → `qs_slot_frame_pc.png` (the `快捷栏` well crop).
  - `.hud-action-btn` icons → the `PcButtons/btn_*.png` sprites (normal + `_over`).
  - TopGap menu buttons → their `btn_*_f*.png` sprites.
- Anchors: all new clusters use absolute positioning anchored to their region; `picking-mode`
  passthrough on containers so only slots/buttons receive input.

## C# Changes (`GameHudController.cs`)

- **Remove** `HideMobileJoystick()` force-hide (and its polling frame) — joystick stays enabled.
- **Bind** the 6 combat slots + 3 quick slots: assignment state (skill id / item id per slot),
  icon resolution (`ItemDb.Resolve` / skill icon catalog), activation handlers (tap → fire skill
  intent / consume item intent).
- **Relocate** menu-button click wiring to the new TopGapCluster elements (names preserved so
  `RegisterClick("BtnStatus", OnStatusClick)` etc. still resolve).
- Action buttons: wire run/horse/sit toggle handlers (reuse existing sit/run/mount logic if
  present; if stubs, keep as no-op/log like Khóa/Đính/Tháo until a gameplay change).
- Reused unchanged: `MobileJoystick`, `TouchInputService`, `PcSkillPanelService`, item/equipment
  services, popup wiring.

## Sliced Delivery (chained — forecast > 400 lines)

Per auto-forecast + 400-line review budget, deliver as chained slices (each ≤ budget, each its
own commit/PR):

- **S1 — Foundation + chat lane**: remove `HideMobileJoystick` (joystick on); strip the bottom-
  center `快捷栏` replica; reserve bottom-center lane for chat; add empty anchored cluster shells
  (`CombatCluster`, `QuickSlots`, `TopGapCluster`) with no binding yet. Vision: clean mobile
  canvas, joystick visible, bottom clear.
- **S2 — Combat + action buttons**: populate `CombatCluster` (1 main + 5 sub fan) with PC slot
  frames + skill-icon assignment; add run/horse/sit action buttons with PC icons. Vision: full
  right-thumb combat zone.
- **S3 — Quick slots + menu relocation**: populate `QuickSlots` (3, PC well chrome) with
  usable-item assignment; relocate 8 menu buttons + buff + Bảo Vật into `TopGapCluster`, wire
  icons; verify all menu popups still fire. Vision: complete mobile HUD, top bar/minimap intact.

Exact line counts + slice boundaries finalized in `tasks`.

## Risks & Mitigations

- **R-A Fan geometry off on real devices** → tune px in apply with a vision screenshot (acceptance
  gate). Low–med.
- **R-B Quick-slot chrome crop unclear** → fallback to `btn_skill_empty_pc.png` styling (still PC
  art). Low.
- **R-C Menu row overflows the top gap at 1280** → wrap to 2 rows / shrink to 32px + invisible
  hit padding. Low.
- **R-D Regression to popups/top bar/minimap** → EditMode regression tests + vision diff. Low.
- **R-E Review workload** → chained slices keep each PR ≤ 400 lines. Mitigated by slicing.

## Test Plan

- EditMode category `HUD`/`MobileHud`: joystick enabled; 6 combat slots (1 main + 5 sub); 3 quick
  slots; run/horse/sit present + icon-wired; 8 menu buttons relocated + still firing; bottom-
  center lane free; top bar/minimap unchanged.
- Vision `ui_diff_check`: mobile screenshot vs the design mock; acceptance ≥ 80% layout match;
  every element traces to a PC sprite.
- Manual play mode: joystick moves character; tap combat slot → fires assigned skill; tap quick
  slot → consume intent; menu buttons open popups.
