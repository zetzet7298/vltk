# 0009 Unity Sandbox Decomposition And Iteration Loop

Date: 2026-07-18

## Status

Accepted

## Context

`VLTK.Sandbox` contains roughly 99 thousand lines across hundreds of files. A
small per-feature asmdef can demonstrate a move but would not solve the durable
architecture problem and could create dozens of assembly edges. The same program
must improve the daily edit loop without confusing hot-reload success with full
Unity verification.

## Decision

Decompose Sandbox incrementally into approximately 5-10 cohesive assemblies. Use
the following direction as the initial architecture to validate against the real
dependency graph:

```text
VLTK.Model
    <- VLTK.Gameplay.Domain
        <- VLTK.PortData / VLTK.Combat / VLTK.World
            <- VLTK.Sandbox.Runtime
                <- VLTK.UI
```

Existing Core, Resources, Sprites, and Backend assemblies remain first-class
boundaries and must not be duplicated. Each extraction must seed or grow a durable
domain boundary; do not create an asmdef for a single class, test fixture, parser,
or narrow gameplay feature.

The accepted development loop is:

1. Use Debug Code Optimization for ordinary edit/compile work.
2. Enter Play once and use Fast Script Reload for supported method-body changes.
3. Test the change in the live session.
4. Run the smallest related batch of EditMode tests.
5. At every feature or refactor boundary, perform normal full compilation and the
   relevant PlayMode smoke tests.
6. Unity MCP script edits rely on their automatic import/compile request; agents
   must not issue a redundant refresh.

Fast Script Reload receives a bounded compatibility pilot before becoming a normal
tool. Structural changes always use normal Unity compilation.

## Alternatives Considered

1. A separate assembly for each feature such as CityWar. Rejected because it
   violates the 5-10-boundary target and creates micro-assembly churn.
2. One renamed monolithic runtime assembly. Rejected because it preserves compile
   blast radius and coupling.
3. Hot reload as final verification. Rejected because fields, serialized state,
   generics, inheritance, APIs, and assembly moves can require full compilation.
4. Release Code Optimization during daily iteration. Rejected as the default;
   Release remains appropriate for profiling and final performance verification.

## Consequences

Positive:

- The refactor converges toward a bounded domain architecture rather than a pile
  of feature assemblies.
- Daily method-body iteration can avoid repeated Play/Stop cycles.
- Full compile and PlayMode proof remain explicit completion gates.

Tradeoffs:

- The first extraction requires broader dependency mapping than a four-file
  feature-only assembly.
- Debug and Release timings are not directly comparable and must be labeled.
- Hot reload introduces a compatibility surface that requires a reversible pilot.

## Follow-Up

- Implemented durable seeds: `VLTK.PortData`, `VLTK.World`, `VLTK.Combat`, and
  `VLTK.Gameplay.Domain`.
  Each is cohesive and one-way; none is a per-feature micro-assembly.
- Current proven direction is `PortData -> Core/Model`,
  `World -> Core/Model/PortData`, `Combat -> Model/PortData`,
  `Gameplay.Domain -> Core/Model`, and
    `Sandbox.Runtime -> Gameplay.Domain/PortData/World/Combat`. UI consumes Domain but
    remains outside all lower layers. The pure in-memory `AssetRegistry` belongs to
    Domain; filesystem and StreamingAssets providers remain in `VLTK.Resources`.
- The existing composition assembly is now named `VLTK.Sandbox.Runtime` while its
  source namespace and asset GUIDs remain unchanged. This identity alignment follows
  the Domain/PortData/World/Combat extractions and is not counted as compile-blast
  reduction by itself. Continue remaining ownership moves only after dependency
  evidence proves a bounded slice; do not move dirty combat parity files merely to
  make the diagram look complete.
- The separate Fast Script Reload pilot passed 5/5 qualifying live method-body
  edits on one instance. It is accepted only for that narrow inner loop; normal
  Debug compilation remains mandatory for unsupported or structural changes.
