# Pin canonical static oracle cho Đường Môn

## Current Behavior

The production TangMen learned catalog now contains the 23 IDs established by
active PC progression plus level 90/120/150 skillbook grants. The legacy panel
still displays ten rows, but `51,55,57` are explicitly display-only residuals:
they are excluded from the learned oracle, `knownSkills`, `skillLevels`, upgrade
and `MaxAll` state. The other sixteen PC-learned IDs are present in the static
catalog but deliberately do not establish runtime combat parity.

## Target Behavior

Use `membership-classification.json` to keep progression, skillbook, Unity-only
and unresolved evidence separate. Then freeze a reviewed canonical byte slice
and deterministic oracle for the evidenced player scope, including relationship
edges without deriving UI order from progression tiers.

This target is implemented for the bounded static proof. `uiOrder` remains null
and runtime/projectile/UI parity remains separate work.

## Affected Users

- Players whose TangMen skill panel/catalog is exposed on mobile.
- Reviewers consuming parity evidence.

## Affected Product Docs

- `docs/stories/SKL-ALL-PARITY-001/coverage-matrix.json`
- `docs/stories/SKL-CB-PROOF-002/`

## Non-Goals

- This story does not claim runtime parity or `PARITY_DONE`.
- Runtime combat, projectile timing, UI/deck, assets/audio, Android/device
  smoke, and PC runtime golden are separate work.
