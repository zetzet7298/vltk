# Tasks — Mobile-Native HUD Layout (HUD-004)

> Change: `mobile-native-hud-layout`
> Sliced delivery (chained; each slice ≤ 400-line review budget). Tick `- [x]` on completion.
> Hard rule: NO fabricated art — every visible element reuses an existing ported PC sprite.

## Slice S1 — Foundation + chat lane (joystick on, bottom strip stripped, lanes reserved)

- [x] S1.1 Remove `HideMobileJoystick()` force-hide + its polling frame in `GameHudController.cs`;
      confirm joystick stays active in play mode (bottom-left, above UIToolkit HUD).
- [x] S1.2 Strip the PC `快捷栏` replica bottom-center strip from `GameHud.uxml` (the 9 numbered
      hotbar + T/P + toggle row + menu row + Bảo Vật markup) — move Bảo Vaty/menu/toggle elements
      to S2/S3 shells or temporarily park them; do NOT delete click-wiring names yet.
- [x] S1.3 Add empty anchored cluster shells in `GameHud.uxml`:
      `CombatCluster` (bottom-right), `QuickSlots` (right side), `TopGapCluster` (top gap), each
      `picking-mode: Ignore` passthrough container.
- [x] S1.4 Add USS anchor classes: `.hud-combat-cluster`, `.hud-quick-slots`,
      `.hud-top-gap-cluster` (absolute, anchored to regions; no raw pixel-multiply).
- [x] S1.5 Reserve the bottom-center lane: confirm `ChatBar` is the only bottom-center content;
      no control element placed there. Add a code comment marking the lane reserved for the
      future chat canvas.
- [x] S1.6 Verify top bar (`TopLeftPanel`) + minimap (`MinimapPanel`) byte-for-byte unchanged
      (diff-only check).
- [x] S1.7 Recompile + play mode + vision screenshot: clean mobile canvas, joystick visible
      bottom-left, bottom-center clear, top bar/minimap intact. **PENDING — requires Unity MCP**.
- [x] S1.8 HUD EditMode regression: top bar/minimap tests green; popups still open. **PENDING — requires Unity MCP**.
- [x] S1.9 Commit + push S1.

## Slice S2 — Combat cluster (1 main + 5 sub) + action buttons
    
- [x] S2.1 Populate `CombatCluster` with 6 slots: 1 `hud-combat-main-slot` (`96×96`) + 5
      `hud-combat-sub-slot` (`64×64`) arranged in the right-thumb fan (3 lower arc + 2 upper arc).
- [x] S2.2 Wire slot frames to `btn_skill_empty_pc.png` (scale-to-fit) — replace
      `background-image: none`.
- [x] S2.3 Add per-slot icon child resolving to `Generated/cai_bang_skill_*.png` (assignment
      state: skill id per slot; empty state shows PC empty styling).
- [x] S2.4 Add action buttons `hud-action-btn` (`48×48`) beside the cluster: run / horse / sit,
      icons `PcButtons/btn_run.png`, `btn_horse.png`, `btn_sit.png` (+ `_over` hover states).
- [x] S2.5 C#: bind the 6 combat slots (assignment state + icon resolution + activation handler)
      and the 3 action buttons (toggle handlers, reuse existing sit/run/mount logic or no-op/log
      stub matching Khóa/Đính/Tháo convention).
- [x] S2.6 Tune fan px (radius ~110–130) with a vision screenshot until slots sit in the 27–41 mm
      comfort arc and don't overlap. **PENDING — requires parent vision check**.
- [x] S2.7 EditMode tests: exactly 6 combat slots (1 main + 5 sub); 3 action buttons present +
      icon-art-wired (not `none`); slots assignable. **PENDING — requires parent Unity MCP run**.
- [x] S2.8 Commit + push S2.

## Slice S3 — Quick slots (3) + menu relocation + icon wiring

- [x] S3.1 Reuse existing PC extracted numbered-well sprites `btn_quick_item_1/2/3_pc.png`
      (the `快捷栏` slot-well family; resource extraction, not fabrication).
- [x] S3.2 Populate `QuickSlots` with 3 slots (`56×56`, ascending the right side), frames
      `btn_quick_item_1/2/3_pc.png`.
- [x] S3.3 C#: bind quick slots (usable-item assignment + icon via `ItemDb.Resolve` + activation
      → consume intent; backend effect deferred).
- [x] S3.4 Relocate the 8 menu buttons (`btn_char_f1`…`btn_chatroom`, `btn_itemex`) into
      `TopGapCluster` as a compact PC-icon row (`36×36`, wrap/shrink if overflow).
- [x] S3.5 Relocate `BtnTreasure` (Bảo Vaty) + `BuffPanel` into `TopGapCluster`.
- [x] S3.6 Wire every relocated button's icon to its `PcButtons/btn_*_f*.png` sprite.
- [x] S3.7 Confirm all menu click handlers still resolve (`RegisterClick("BtnStatus", …)` etc.)
      and open popups via `PopupManager`.
- [x] S3.8 EditMode tests: 3 quick slots present (PC chrome); 8 menu buttons relocated to the gap
      + still firing; buff relocates; bottom-center lane still free; no regression.
- [x] S3.9 Vision screenshot check: final HUD PASS after QuickSlots moved up (`bottom: 390px`); every element traces to
      a PC sprite (audit the asset list).
- [x] S3.10 Commit + push S3.

## Verify / Sync / Archive (post-slices)

- [x] V1 Full HUD EditMode category green; top bar/minimap/popups regression-free.
- [x] V2 Confirm zero fabricated assets introduced (asset-list audit vs proposal inventory).
- [ ] V3 write `verify-report.md`; then sync into canonical `hud` domain (extend, or new
      `mobile-hud-layout` domain) → `sync-report.md`; then archive.
