# US-M32-001 M3.2 NPC Spawn in Sandbox

## Status

implemented

## Lane

normal

## Intake

Intake #14 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

NPCs spawn in the sandbox so map population can be checked: a GM toggle spawns
placeholders from the region spawn manifest, each uses its decoded sprite where
available, an inspector shows the source template/spawn/script ids, and a GM
despawn removes NPCs without reloading the map.

## Relevant Product Docs

- `docs/spec.md` — "M3.2 — NPC Spawn in Sandbox"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Map has NPC spawns; GM toggles NPCs; NPC placeholders appear.
- AC2: NPC sprite available; spawn renders; NPC uses decoded sprite/animation.
- AC3: NPC clicked; inspector opens; source template/spawn/script ids are shown.
- AC4: GM despawn clicked; command runs; NPCs are removed without reloading map.

## Design Notes

- `NpcInstance` (live placeholder) + `NpcSpawnService` (pure C#): `ToggleNpcs`
  spawn/despawn, `SpawnFrom(manifest)` resolves template + sprite clip, `GetInstance`
  by id, `InspectorSummary` (AC#3), `DespawnAll` without map reload (AC#4).
- Built on M1.7 `RegionSpawnManifest` + M3.1 `NpcTemplateRegistry`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: toggle spawn/despawn, positions, sprite resolve/unresolved, inspector ids, instance ids |
| Integration | Spawn manifest + template registry (unit-covered) |
| E2E | GM toggle/click/despawn in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Sandbox NPC population layer reusing M1.7 + M3.1.

## Evidence

EditMode 287/287 pass (docs/evidence/editmode-results-2026-05-31-m3-npc-lua.json).
`NpcInstance` + `NpcSpawnService`. Suite `VLTK.Tests.Sandbox.NpcSpawnServiceTests`
(12 tests) covers AC1–AC4.
