# Sync Report — migrate-skill-panel-popup

## Status: SYNCED

## Executive Summary

Sync established a new canonical domain spec for the skill panel, since no canonical spec
existed before this change:

`openspec/specs/skill-panel/spec.md`

The verify report flagged a domain-naming ambiguity: the change-level spec self-named the
domain as the change name (`migrate-skill-panel-popup`). Sync resolved this by choosing a
dedicated, stable domain name **`skill-panel`** (rather than merging into `hud` or `popups`),
because the requirements are gameplay-touching (mutate live `PlayerProgressionState`, faction
skill-panel progression grant, fight-skill-point spend) and materially more than HUD chrome.
This is an internal, reversible spec-organization choice.

The new canonical spec carries all 11 requirements:

- SkillContent popup-body contract
- Skill grid layout — 30 cells, single scrollable page
- Skill-point summary display
- Skill selection detail toggle (interactive parity)
- Upgrade mutates live progression (interactive parity)
- Data-reuse invariant — no skill-logic duplication
- Progression-grant preservation on open (gameplay-critical)
- Popup layout hint — PC-footprint parity
- BtnSkills wiring via PopupManager
- GameHudController de-inlining
- IMGUI skill-panel render retirement
- Test migration (RED-first)

## Domain Decision (sync-resolved)

| Candidate | Chosen? | Rationale |
|---|---|---|
| `skill-panel` | **YES** | 11 REQ, gameplay-touching progression mutation — warrants its own domain |
| merge into `hud` | no | `hud` domain is HUD chrome (bottom bar frame); skill panel is progression logic |
| merge into `popups` | no | popup-window mechanics belong to `add-popup-window-system`; skill-panel is its content/semantics |

## Files Changed This Step

- `openspec/specs/skill-panel/spec.md` (new canonical domain spec)
- `openspec/changes/migrate-skill-panel-popup/sync-report.md` (this report)

No edits to implementation code.

## Verification Link

- verify-report.md status: PASS (11/11 REQ; 47/47 tasks; zero stray refs; PcSkillPanelService untouched)
- Gameplay-critical grant-before-BuildPage ordering + idempotency confirmed in code AND tests.
- Implementation commits: `ef4d556bf` (PR-1), `fe5a77cd7` (PR-2), `99be0e88c` (docs)

## Next Recommended Phase

archive — the change is verified and synced; archive can move it under `openspec/changes/archive/`.

## Risks

- The 10 faction skill-panel fixtures exceed the spec's originally-named 4 (Cái Bang/CuiYan/
  KunLun/TianRen); the extra factions were ported organically in earlier work. Only Cái Bang
  was retargeted through `SkillContent` in this change; the others remain pure data-service
  fixtures. Canonical spec wording uses "faction skill-panel fixtures" generically to cover
  both the originally-named set and the grown set.
- No automated visual baseline (manual screenshot not captured; structurally verified).
