# Sync Report — add-bottom-bar-pc-frame

## Status: SYNCED

## Executive Summary

Sync established a new canonical domain spec for HUD, since no canonical spec existed under
`openspec/specs/` before this change:

`openspec/specs/hud/spec.md`

The new canonical spec contains all 5 requirements introduced by this change:

- Requirement: Ornate Filigree Frame Visible
- Requirement: Aspect Ratio Preserved
- Requirement: Buttons Functional and On Top
- Requirement: PC-Proportional Positioning
- Requirement: No Regressions

Follow-up items (pixel-align, aspect-ratio 1:1, label T/P, chat panel) are tracked in the
canonical spec under an explicit "Follow-up (tracked separately)" section, not as baseline
requirements.

## Files Changed This Step

- `openspec/specs/hud/spec.md` (new canonical domain spec)
- `openspec/changes/add-bottom-bar-pc-frame/sync-report.md` (this report)

No edits to implementation code.

## Delta-to-Canonical Evidence

| Change requirement | Canonical location | Status |
|---|---|---|
| Ornate filigree frame visible | Requirement: Ornate Filigree Frame Visible | written |
| Aspect ratio preserved | Requirement: Aspect Ratio Preserved | written |
| Buttons functional + pickingMode Ignore | Requirement: Buttons Functional and On Top | written |
| PC-proportional positioning | Requirement: PC-Proportional Positioning | written |
| No regressions | Requirement: No Regressions | written |

## Verification Link

- verify-report.md status: PASS
- Implementation commits: `c0385c10c`, `8681f1c2b`

## Next Recommended Phase

archive — the change is verified and synced; archive can move it under `openspec/changes/archive/`.

## Risks

- Canonical spec wording mirrors the change-level spec wording. If the recovered PC SPR frame
  is later re-derived or PC labels change, both files should be updated together.
