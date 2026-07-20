# Design

## Domain Model

The refactor treats assemblies as ownership boundaries. A candidate boundary must
have a cohesive responsibility, stable consumers, and a one-way dependency path.
The target Sandbox decomposition is approximately 5-10 cohesive assemblies. The
default direction to validate is:

```text
VLTK.Model
    <- VLTK.Gameplay.Domain
        <- VLTK.PortData / VLTK.Combat / VLTK.World
            <- VLTK.Sandbox.Runtime
                <- VLTK.UI
```

Existing Core, Resources, Sprites, and Backend assemblies must be reused rather
than duplicated. The first extraction must seed one of these durable boundaries;
a CityWar-only, parser-only, or class-sized asmdef is not acceptable.

### First Slice: PortData Item-Ingestion Seam

The first implementation seeds `VLTK.PortData` with 20 clean files: shared text
decoding, shared item row parsing, sixteen item-type parsers, and drop-rate
parsing/registry. It keeps current namespaces for compatibility and uses
`InternalsVisibleTo` rather than broadening internal helper APIs.

`VLTK.PortData` may reference only `VLTK.Core` and `VLTK.Model` in this slice.
`VLTK.Sandbox.Runtime` depends on PortData; PortData must never depend back on Runtime.
Batch loaders, importers, databases, loot/gameplay policy, MonoBehaviours, boot
wiring, UI, Combat, World, and CityWar remain outside this slice.

### Runtime Composition Identity

After the lower assemblies exist, the remaining scene/composition assembly is named
`VLTK.Sandbox.Runtime`. Its source folder and `VLTK.Sandbox` namespace remain stable,
so the rename changes assembly identity without rewriting script assets or GUIDs.
UI and the broad integration test assemblies reference Runtime by the new name;
PortData grants its internal helper access to that same identity. The rename is an
architecture-alignment step, not evidence of compile-speed improvement.

## Application Flow

Existing runtime flows remain unchanged. Only compilation and ownership boundaries
may change in the first slice.

## Interface Contract

No public gameplay contract change is intended. New public types or APIs require
explicit coordinator review because they enlarge the assembly surface.

## Data Model

No persistent data or schema changes.

## UI / Platform Impact

UI must continue to reference runtime contracts one-way. Inner assemblies cannot
reference UI, Editor, tests, or scene-orchestration layers. The active Unity target
remains unchanged.

## Observability

Capture assembly source count, compile duration from comparable Unity/Bee evidence,
affected dependent assemblies, clean Console state, and exact tests run.

Daily iteration uses Debug Code Optimization and, after a bounded compatibility
pilot, Fast Script Reload for supported method-body edits. Every feature/refactor
boundary still requires a normal full compile and relevant PlayMode smoke proof.

## Alternatives Considered

1. Big-bang Sandbox split. Rejected due to dirty-work collisions and unbounded
   compilation/runtime risk.
2. Assembly per feature or folder. Rejected because arbitrary micro-assemblies add
   dependency overhead without durable ownership.
3. Hot reload only. Rejected because it cannot validate structural assembly moves.
4. One assembly per gameplay feature. Rejected because it creates micro-assembly
   churn and increases cycle risk instead of producing stable domain ownership.
