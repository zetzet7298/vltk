# Exec Plan

## Goal

Establish an independent canonical static proof gate for TangMen before any
runtime parity work.

## Scope

In scope:

- Reconcile Unity roots `43,45,47,48,50,51,54,55,57,58` with active PC
  progression and level 90/120/150 skillbook grants. Their union contains 23
  learned-membership IDs, with 16 PC-only and three Unity-only unresolved IDs.
- Pin `membership-classification.json`; keep `51,55,57` unresolved/excluded from
  learned membership and preserve child/support/event relationships separately.
- Byte-preserving source slice, Lua supplement, deterministic JSON/hash oracle.
- EditMode catalog/coverage verifier and existing fixture regression.

Out of scope:

- CombatRuntimeService semantics, projectile lifecycle, UI/deck, assets/audio,
  Android/device smoke, PC golden, or generic oracle infrastructure.

## Risk Classification

Risk flags: public contract, existing behavior, weak proof, cross-platform.

Hard gates: use PC learning flow as membership authority without claiming it
proves UI order, reconcile stale epic state against the completed Cái Bang proof
packet, preserve vltktool-only decoding, and never weaken validation or derive
expected values from Unity implementation.

## Work Phases

1. [completed] Reconcile progression + skillbook membership with Unity display.
2. [completed] Record reviewed membership/unresolved classification artifact.
3. [completed] Produce and `--check` the vltktool exact-byte skills slice/provenance.
4. [deferred] UI order has no PC evidence in this wave; `uiOrder` remains null and
   the product/UI contract stays outside static membership proof.
5. [completed] Slice/oracle design, target-closure proof and implementation.
6. [completed] Hash-pinned EditMode verification and independent review.
7. [completed] Harness evidence update; runtime work remains a separate story.

## Stop Conditions

Pause if the PC-only/Unity-only classification is unresolved, UI order requires
an unevidenced product choice, canonical sources conflict, encoded evidence is
unresolved, or the epic/child proof state is not reconciled.
