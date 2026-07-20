# Exec Plan

## Goal

Make the production TangMen static catalog match the independent 23-skill PC
oracle so `SKL-TM-PROOF-001` can complete its catalog/relationship gate.

## Scope

In scope:

- Replace the ten-definition TangMen learned factory set with the exact 23-ID
  oracle membership.
- Map populated static numeric/string fields and direct relationships.
- Ensure all 32 direct relationship targets resolve in the full catalog.
- Keep `51,55,57` unresolved/excluded and `uiOrder` unspecified.

Out of scope:

- Runtime combat behavior, projectile timing, UI/deck/tree order, assets/audio,
  device smoke, and PC runtime golden comparison.

## Risk Classification

Risk flags: public contract, existing behavior, weak proof, cross-platform.

Hard gates: frozen PC oracle is expected authority; do not derive values from
Unity, do not weaken validation, and do not infer UI order.

## Work Phases

1. [completed] Reproduce the 10-vs-23 membership failure.
2. [completed] Map 23 learned definitions and direct relationships.
3. [completed] Add missing support/event target definitions to the full catalog only.
4. [completed] Run oracle, compile, membership, field, relationship and regression checks.
5. [completed] Independent review and detailed Harness evidence.

## Stop Conditions

Pause if a populated oracle field has no `SkillDefinition` representation, a
target cannot be mapped without runtime semantics, canonical sources conflict,
or validation would need to be weakened.
