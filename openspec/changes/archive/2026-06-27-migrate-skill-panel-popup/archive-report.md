# Archive Report — migrate-skill-panel-popup

## Status: ARCHIVED

## Change

`migrate-skill-panel-popup` — migrate the inline `CaiBangSkillPanel` HUD element +
`GameHudController` skill methods onto the shared `PopupManager` / `PopupWindow` base as a new
`SkillContent` popup body, reusing `PcSkillPanelService` untouched. Retires the IMGUI
`DrawSkillPanelText()` overlay and de-inlines `GameHudController`.

## Lifecycle

- proposal → spec → design → tasks → apply (PR-1 + PR-2) → verify (PASS 11/11) → sync (SYNCED,
  new `skill-panel` domain) → archive

## Commits

- Implementation: `ef4d556bf` (PR-1 SkillContent body), `fe5a77cd7` (PR-2 wire BtnSkills +
  de-inline), `99be0e88c` (docs follow-up closure)
- Verify: `ffb0f925a`
- Sync: `1497295e6`

## Canonical Spec

Established new domain `openspec/specs/skill-panel/spec.md` with 11 requirements
(gameplay-touching: progression grant, fight-skill-point spend, 30-cell grid, data-reuse
invariant, de-inlining, IMGUI retirement).

## Domain Decision (sync-resolved)

Chose dedicated `skill-panel` domain over merging into `hud` (HUD chrome) or `popups`
(popup-window mechanics). Reversible internal spec-organization choice.

## Verification Evidence

- 11/11 REQ PASS; 47/47 tasks checked; zero stray references.
- PcSkillPanelService reused unchanged (`git diff` empty on that file).
- Gameplay-critical grant-before-BuildPage ordering + idempotency confirmed in code AND tests.
- Test categories green: Skill 12/12, CaiBangSkillPanelTests 12/12, GameHudControllerTests
  10/10, Popup 46/46.
- Baseline disclaimer: 25 full-suite failures pre-existing and out-of-scope (Backend, BaLang,
  CaiBang combat order-dependent, CombatSkillSlot, InventoryService, Mount/MalePlayerVisual
  Slow, PcWeaponThief) — NOT regressions.

## Residual Risks (non-blocking)

- 10 faction fixtures vs spec's originally-named 4 = pre-existing organic growth; only Cái Bang
  retargeted through SkillContent, rest are pure data-service fixtures.
- No automated visual baseline (structurally verified).

## Archive Location

`openspec/changes/archive/2026-06-27-migrate-skill-panel-popup/`
