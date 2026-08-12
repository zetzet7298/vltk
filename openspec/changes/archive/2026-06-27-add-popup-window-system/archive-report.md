# Archive Report — add-popup-window-system

## Status: ARCHIVED

## Change

`add-popup-window-system` (HUD-003) — build the reusable popup window infrastructure
(`PopupWindow` / `PopupManager` / `IPopupContent`) and deliver 6 feature windows through it:
Character Info, Inventory, Treasure, Team, Faction (Skill delivered via separate archived change).

## Lifecycle

- proposal → explore → spec → design → design-inventory → tasks (+tasks-inventory/team/faction/
  treasure) → apply (6 windows across 6 commits) → verify (PASS) → sync (SYNCED, new `popups`
  domain) → archive

## Commits

- Implementation: 5e12a46bc (shell + Đóng art), 381f0864f (CharacterInfo), a669ad7ce (inventory),
  e27304ca9 (treasure), 436335b52 (team), f3ed1cdfe (faction), bc907f863 (team cleanup),
  20da6896e (tasks shipped)
- Verify: `214c77b12`
- Sync: `25f243f34`

## Canonical Spec

Established new domain `openspec/specs/popups/spec.md` with 10 requirements covering the reusable
shell + single-focus manager + Đóng close art + 5 feature windows + non-destructive action
buttons + EditMode coverage.

## Domain Decision (sync-resolved)

Consolidated base + 5 feature windows into one `popups` domain (cohesive; all delivered in this
change as thin IPopupContent consumers). Skill window keeps its own `skill-panel` domain.

## Verification Evidence

- 6 windows delivered, all feature buttons wired via PopupManager.Show.
- Vietnamese-art-only (no Chinese SPRs shipped); Đóng close button from `关闭_vn.spr`.
- Khóa/Đính/Tháo confirmed present-and-clickable but non-destructive (log-only) per spec.
- 83/3 tasks; 3 unchecked all explicitly out-of-scope follow-ups (proposal §Out verbatim).
- Test categories green (Popup, GameHudController). Pre-existing baseline failures unchanged.

## Out-of-Scope Follow-up (separate changes)

- Real equip/unequip/socket (Khóa/Đính/Tháo) gameplay logic (item parser + accessory
  equip-binding data layers already delivered in archived changes).
- Mask/Amulet/Charm/Trinket data binding (display-only framework slots here).
- Inventory vision-diff E2 (paperdoll mismatch by scope — standalone bag vs Character Info tab).
- Drag-to-move/resize/persistence; remaining inline-panel migration; Đánh giá appraisal.

## Archive Location

`openspec/changes/archive/2026-06-27-add-popup-window-system/`
