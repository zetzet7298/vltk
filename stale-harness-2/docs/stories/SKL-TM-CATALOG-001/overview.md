# Map canonical learned Đường Môn skills into production catalog

## Current Behavior

`PcCombatCatalogFactory.CreateTangMenSkills()` now returns exactly the 23
PC-learned IDs. The legacy panel still resolves ten display rows, while
`51,55,57` are registered separately as display-only residuals and are excluded
from player learned/upgrade state. All 32 direct relationship targets resolve in
the full catalog without entering learned membership.

## Target Behavior

The production TangMen catalog exposes exactly the 23 `pcLearnedSkillIds` from
`PcTangMenOracle.json`, with every populated static field and direct
relationship matching the frozen oracle. Relationship targets resolve in the
full production catalog but are not promoted into learned membership.

This bounded static target is implemented. Runtime magic/projectile semantics
for the sixteen static-only learned definitions and support targets remain out
of scope.

## Affected Users

- Players whose Đường Môn combat definitions are loaded on mobile.
- Reviewers consuming the TangMen static parity proof.

## Affected Product Docs

- `docs/stories/SKL-TM-PROOF-001/`
- `docs/stories/SKL-ALL-PARITY-001/`

## Non-Goals

- UI order, skill-tree layout, deck assignment, and progression presentation.
- Runtime damage/projectile semantics, assets/audio validation, Android smoke,
  or a `PARITY_DONE` claim.
