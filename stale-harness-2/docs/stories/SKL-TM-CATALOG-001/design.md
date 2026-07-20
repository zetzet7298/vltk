# Design

## Domain Model

The learned TangMen set is the 23-ID `pcLearnedSkillIds` collection in the
hash-pinned PC oracle. `51,55,57` remain observed Unity-only unresolved IDs and
must not appear in that production learned set. The 32 relationship targets
are support/event definitions, not learned membership.

## Application Flow

The factory maps each frozen oracle row to one `SkillDefinition`. The EditMode
consumer test loads the independent oracle, compares exact learned membership,
then checks only fields listed in each row's `present` array. The full catalog
must also resolve every direct relationship target, including `58 -> 227`.

## Interface Contract

`PcCombatCatalogFactory.CreateTangMenSkills()` returns exactly the oracle's 23
learned IDs as an unordered membership set. This contract does not define UI
order. No expected value may be derived from the Unity implementation.

## Data Model

No persistence or migration changes. Canonical rows and target closure remain
in the vltktool-generated slices and deterministic oracle owned by
`SKL-TM-PROOF-001`.

## UI / Platform Impact

The production combat catalog changes; UI order and panel membership are not
changed by this story. Any UI decision requires separate evidence.

## Observability

The bounded TangMen parity runner records oracle hash, exact membership diff,
field/relationship mismatches, and target-resolution failures.

## Alternatives Considered

1. Keep the ten Unity display definitions and weaken the oracle — rejected as
   circular and contrary to canonical PC learning evidence.
2. Treat progression order as UI order — rejected because PC evidence does not
   prove that contract.

