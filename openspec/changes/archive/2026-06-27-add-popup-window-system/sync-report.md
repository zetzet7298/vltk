# Sync Report — add-popup-window-system

## Status: SYNCED

## Executive Summary

Sync established a new canonical domain `popups` for the reusable popup window infrastructure +
the feature windows delivered alongside it, since no canonical spec existed before:

`openspec/specs/popups/spec.md`

The verify report proposed candidate domains `popups` (base) + per-feature sub-domains
(character-info/inventory/team/faction/treasure). Sync consolidated the base + the 5 delivered
feature windows into one `popups` domain for cohesion (the features are thin `IPopupContent`
consumers of the same base and were delivered in this same change), while the Skill window keeps
its own canonical domain `skill-panel` (delivered in a separate archived change). This is an
internal, reversible spec-organization choice.

The new canonical spec carries 10 requirements:

- Popup shell is reusable and content-agnostic
- PopupManager hosts windows with single-focus
- Close button is Vietnamese "Đóng" SPR
- Feature windows open through PopupManager
- Character Info window matches the reference layout
- Trang bị paperdoll binds real equipment data where it exists
- Thuộc tính tab binds player stats
- Đánh giá tab present, content deferred
- Action buttons present, non-destructive
- EditMode test coverage

## Domain Decision (sync-resolved)

| Candidate | Chosen? | Rationale |
|---|---|---|
| `popups` (base + 5 feature windows) | **YES** | Cohesive — all delivered in this one change; features are thin IPopupContent consumers of the shared base |
| separate `character-info` / `inventory` / `team` / `faction` / `treasure` sub-domains | no | Over-fragmentation for thin content classes; fold into `popups` |
| `skill-panel` | already exists (separate archived change) | Kept — it is gameplay-touching progression logic, warrants its own domain |

## Files Changed This Step

- `openspec/specs/popups/spec.md` (new canonical domain spec)
- `openspec/changes/add-popup-window-system/sync-report.md` (this report)

No edits to implementation code.

## Delta-to-Canonical Evidence

| Change requirement | Canonical location | Status |
|---|---|---|
| REQ-1 reusable shell | Requirement: Popup shell is reusable and content-agnostic | written |
| REQ-2 PopupManager single-focus | Requirement: PopupManager hosts windows with single-focus | written |
| REQ-3 Đóng close SPR | Requirement: Close button is Vietnamese "Đóng" SPR | written |
| REQ-4 Character Info layout | Requirement: Character Info window matches the reference layout | written |
| REQ-5 paperdoll real data | Requirement: Trang bị paperdoll binds real equipment data where it exists | written |
| REQ-6 Thuộc tính stats | Requirement: Thuộc tính tab binds player stats | written |
| REQ-7 Đánh giá placeholder | Requirement: Đánh giá tab present, content deferred | written |
| REQ-8 BtnStatus wiring | (merged into Requirement: Feature windows open through PopupManager) | written |
| REQ-9 action buttons non-destructive | Requirement: Action buttons present, non-destructive | written |
| REQ-10 EditMode tests | Requirement: EditMode test coverage | written |

Follow-up out-of-scope items (tracked in canonical spec §Out of Scope): real equip/unequip/socket
gameplay; Mask/Amulet/Charm/Trinket data binding; drag-to-move/resize/persistence; remaining
inline-panel migration; Đánh giá appraisal.

## Verification Link

- verify-report.md status: PASS (6 windows delivered, Vietnamese-art-only, Khóa/Đính/Tháo
  confirmed present-and-clickable but non-destructive log-only).
- 83/3 tasks; 3 unchecked all explicitly out-of-scope follow-ups.
- Implementation commits: 5e12a46bc (shell), 381f0864f (CharacterInfo), a669ad7ce (inventory),
  20da6896e (tasks shipped), e27304ca9 (treasure), 436335b52 (team), f3ed1cdfe (faction),
  bc907f863 (team cleanup).

## Next Recommended Phase

archive — the change is verified and synced; archive can move it under `openspec/changes/archive/`.

## Risks

- Scope exceeded the original proposal (slice-1 = base + Character Info only; actual delivery =
  base + 6 windows). Each window was its own slice with its own tasks file, each ≤ 400-line
  budget. Canonical spec reflects the full delivered scope.
- Per-feature content is consolidated under `popups`; if a future change adds deep behavior to a
  single feature (e.g. inventory drag-drop), it may warrant splitting that feature into its own
  domain then.
