# Exec Plan

## Goal

Incrementally reduce `VLTK.Sandbox` compilation cost and dependency blast radius
without changing gameplay behavior, losing Unity asset GUIDs, weakening tests, or
colliding with active parity work.

## Scope

In scope:

- Evidence-backed dependency and compile baseline.
- A small, one-way target assembly architecture.
- Approximately 5-10 cohesive assemblies for the Sandbox decomposition, aligned
  to Domain, PortData, Combat, World, Runtime composition, and UI consumption.
- One bounded first extraction selected after independent review.
- Script and `.meta` moves, asmdef wiring, and tests required for that slice.
- Before/after Unity compilation and focused proof.

Out of scope:

- Gameplay changes or PC parity changes.
- Files already modified or untracked by concurrent work.
- Global scripting defines, Editor settings, Auto Refresh, Code Optimization, or
  unrelated package changes. Debug Code Optimization and a bounded Fast Script
  Reload pilot are explicitly in scope for this accepted initiative.
- A big-bang move of all Sandbox code.

## Risk Classification

Risk flags:

- Existing behavior: assembly moves can break compilation and Unity references.
- Weak proof: compilation performance needs comparable before/after evidence.
- Multi-domain: Sandbox currently contains many gameplay and porting domains.
- Cross-platform: runtime assemblies feed the Unity mobile project.

Hard gates:

- Pause if the selected extraction requires changing gameplay behavior,
  weakening validation, or altering the agreed dependency direction.

## Work Phases

1. Herdr scout maps cohesive low-coupling extraction candidates.
2. Herdr peer challenges assembly direction and migration order.
3. Herdr proof auditor establishes baseline and acceptance gates.
4. Coordinator rejects per-feature micro-assemblies, selects one clean slice
   within the 5-10-assembly target, and records the architecture decision.
5. A separately owned iteration-tooling pilot validates Debug Code Optimization
   and Fast Script Reload compatibility without serving as completion proof.
6. One writer owns the bounded implementation paths.
7. Coordinator integrates and runs full compilation, Console, focused test, and
   measurement proof.
8. Reviewer checks the integrated diff before close.

Current progress: PortData waves 1 through 4, World waves 1 through 4, the dedicated
PortData EditMode assembly, Combat waves 1 and 2, and the Gameplay.Domain seed are implemented with
normal compile plus focused/PlayMode proof. Gameplay.Domain owns six pure catalog/
formula/registry sources (791 LOC), and its dedicated EditMode assembly runs 13
tests in under one second. PortData waves 3 and 4 moved 32 clean character, social,
progression, reward, ranking, Guild, and Tong parser pairs (3,020 LOC combined)
while preserving every GUID. World waves 3 and 4 moved ten boss/siege/mission-
placement parser pairs (1,018 LOC) with unchanged bytes/GUIDs. A separate
evidence-backed cleanup removed three unreferenced plain-class source/meta pairs
(256 LOC). Sandbox is down to 81,219 C# LOC and World has grown to 5,405 LOC.
Debug Code Optimization
is active, and the narrow Fast Script Reload method-body pilot passed 5/5
same-instance edits. The remaining composition assembly is now named
`VLTK.Sandbox.Runtime` with its namespace and asset GUIDs preserved. Remaining
Combat/World/PortData ownership, further test topology, comparable compile timing,
and final integrated review remain in progress.

## Stop Conditions

Pause for human confirmation if:

- Product behavior or the ownership of a subsystem is ambiguous.
- A dependency cycle cannot be removed without a broader redesign.
- The only candidate overlaps the current dirty gameplay files.
- Validation requirements would need to be weakened.
- Architecture direction changes from the accepted decision.
