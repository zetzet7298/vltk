# Archive Report — add-character-info-popup (SUPERSEDED)

## Status: ARCHIVED — SUPERSEDED (no delivery under this change)

## Change

`add-character-info-popup` (HUD-003) — the **seed proposal** for the reusable popup system +
the Character Info window.

## Supersession

This proposal was **fully superseded** by `add-popup-window-system` (same HUD-003 marker). The
superseding change delivered the reusable popup infrastructure (`PopupWindow` /
`PopupManager` / `IPopupContent`) plus 6 feature windows — including the Character Info window
this proposal described — and has itself been verified, synced, and archived:

- Archived change: `openspec/changes/archive/2026-06-27-add-popup-window-system/`
- Canonical domain: `openspec/specs/popups/spec.md`

## Coverage Proof — every proposal item is delivered

| Proposal item | Delivered by | Evidence |
|---|---|---|
| Reusable PopupWindow base + PopupManager | `5e12a46bc` | canonical `popups` spec REQ: Popup shell / PopupManager single-focus |
| Character Info window (3 tabs Thuộc tính/Trang bị/Đánh giá) | `381f0864f` | `CharacterInfoContent.cs` — TitleVi "Thông Tin Nhân Vật", 3 tabs |
| Equipment paperdoll + Khóa/Đính/Tháo/Đóng | `381f0864f` | paperdoll binds PlayerEquipmentService; Khóa/Đính/Tháo non-destructive |
| Wire BtnStatus → PopupManager | `381f0864f` | `GameHudController.OnStatusClick` |
| Vietnamese SPR art (`玲珑盒内框`, `关闭_vn`) | `5e12a46bc` | Đóng close art extracted |

## Lifecycle

Truncated at proposal — no spec/design/tasks/apply/verify/sync was produced under THIS change
because the work migrated to the fuller `add-popup-window-system` change before those phases.
This archive records that supersession for audit-trail completeness.

## Relationship to Canonical Specs

No new canonical domain is created or modified by archiving this superseded proposal. The
Character Info requirements already live in the canonical `popups` domain.

## Archive Location

`openspec/changes/archive/2026-06-27-add-character-info-popup/`
