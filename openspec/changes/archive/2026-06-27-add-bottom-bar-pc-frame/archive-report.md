# Archive Report — add-bottom-bar-pc-frame

## Status: ARCHIVED

## Change

`add-bottom-bar-pc-frame` (HUD-002) — replace the flat placeholder bottom bar with the real
PC filigree frame (`bottom_frame_pc.png`, recovered from PC SPR `快捷栏.spr` hash `ebb69f9b`)
as a transparent `pickingMode: Ignore` background layer under all functional button containers.

## Lifecycle

- proposal → spec → design → tasks → apply → verify (PASS) → sync (SYNCED, new `hud` domain) → archive

## Commits

- Implementation: `c0385c10c` (PC filigree frame), `8681f1c2b` (scale toolbar khung + icon)
- Verify: `b0bdb18e2`
- Sync: `dd82e6b2d`

## Canonical Spec

Established new domain `openspec/specs/hud/spec.md` with 5 requirements:
- Ornate Filigree Frame Visible
- Aspect Ratio Preserved
- Buttons Functional and On Top
- PC-Proportional Positioning
- No Regressions

## Verification Evidence

- HUD EditMode category: 13/13 passed (per tasks C4).
- Vision checks: frame continuous, all 8 menu + 6 toggle + Bảo Vật + T/P visible, no overlap.
- Phase A–D all DONE; 4 follow-up items explicitly out-of-milestone.

## Follow-up (separate change)

- Fine pixel-align buttons to frame slot wells.
- Lock circular toggle button aspect ratio to 1:1.
- Decide PC-art `左/右` labels vs overlay `T/P`.
- Reconcile chat panel style with PC chat input bar (separate change).

## Archive Location

`openspec/changes/archive/2026-06-27-add-bottom-bar-pc-frame/`
