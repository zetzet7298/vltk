# Design

## Domain Model

The bounded unit is a canonical skill case: faction, root skill, level, target
relation, resource/cooldown state, projectile/event chain, ordered effects, and
source evidence. Relationship targets remain support definitions unless PC
learning evidence promotes them.

Runtime reports must preserve the ordered observations needed by tests: cast
accept/reject, resource/cooldown, spawned projectiles, lifecycle sub-skill events,
damage/state applications, and final actor state.

## Application Flow

1. Resolve canonical PC row and Lua/C++ semantics.
2. Resolve the learned root and support-only relationship targets from the pinned
   catalog.
3. Validate cast gates and resource/cooldown state.
4. Execute direct, missile, trap, or lifecycle behavior in PC order.
5. Emit a deterministic report that independent tests can compare.

Shared lifecycle support belongs in the runtime seam. Faction-specific formulas
stay in source-backed adapters; they must not be guessed inside generic runtime
branches.

## Interface Contract

No public network contract changes. Existing Sandbox cast/deck interfaces remain
stable. Any new lifecycle API must be explicit about event kind, source skill,
event skill level, projectile identity, and idempotency.

## Data Model

No persistence or schema change. New fixtures and provenance are repo-local only
when selected for actual runtime/test use.

## UI / Platform Impact

Unity Sandbox behavior and rendered skill effects may change. Visual verification
must use package-priority Vietnamese winners; Android/device proof remains a
separate gate unless run in this story.

## Observability

Focused test reports record case IDs and ordered runtime observations. Harness
records proven, disproven, and unresolved claims separately.

## Alternatives Considered

1. Accept static oracle tests as parity: rejected because they do not execute
   runtime formulas, projectile lifecycle, traps, states, visuals, or audio.
2. Add faction-specific hard-coded outcomes in tests: rejected because expected
   values must be derived independently from canonical PC evidence.
3. Rewrite all ten factions at once: rejected in favor of reviewable source-backed
   waves while preserving the full epic scope.
