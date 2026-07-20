# Correct Cái Bang and Đường Môn runtime parity

## Current Behavior

Static source slices and catalog oracles pass for Cái Bang and Đường Môn, but fresh
Play verification reports both factions still differ from PC. Current proof is
asymmetric: Cái Bang has focused runtime tests for selected skills, while Đường
Môn proof covers learned membership, static fields, and relationship resolution
without proving projectile, trap, poison, event, visual, or audio behavior.

`CombatRuntimeService` currently implements direct damage, missile collision, and
selected Cái Bang semantics. Start events are partial; fly and vanish lifecycle
events remain unimplemented. A green static oracle therefore cannot establish
runtime parity.

## Target Behavior

- Every corrected behavior is derived from canonical PC C++/Lua/config evidence.
- Cái Bang and Đường Môn casts preserve PC target, cost, cooldown, damage/state,
  child/event, missile/trap, timing, and faction constraints for the bounded cases.
- Focused tests compare observable event/state sequences with independent PC
  expected values and fail on the pre-fix implementation.
- Unavailable PC runtime, SPR, audio, or device evidence remains explicitly blocked;
  the story does not claim `PARITY_DONE` without E4 proof.

## Affected Users

- Players using Cái Bang or Đường Môn skills in the Unity Sandbox.
- QA and maintainers reviewing PC-to-mobile skill parity.

## Affected Product Docs

- `harness/docs/product/sandbox-runtime.md`
- `harness/specs/jx-pc-mobile-port/domains/skills.md`
- `harness/specs/jx-pc-mobile-port/governance/source-authority.md`
- `harness/docs/stories/SKL-ALL-PARITY-001/`

## Non-Goals

- Claiming all ten factions are complete from two-faction proof.
- Modifying any file under `/var/www/jx-source`.
- Guessing encoded tables, PAK winners, SPR assets, formulas, or UI order.
- Treating aggregate EditMode pass counts as PC runtime golden evidence.
