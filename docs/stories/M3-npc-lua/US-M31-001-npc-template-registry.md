# US-M31-001 M3.1 NPC Template Registry

## Status

implemented

## Lane

normal

## Intake

Intake #13 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

NPC templates derived from PC data populate a registry keyed by id with
name/stats and resource/script references where known. Spawn references resolve a
template; missing sprite/script resources are reported by validation.

## Relevant Product Docs

- `docs/spec.md` — "M3.1 — NPC Template Registry"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: NPC template config exists; converter runs; template registry includes
  id/name/stats/resource/script refs where known.
- AC2: Spawn references template; map loads; spawn marker resolves template.
- AC3: Template missing resource; validation runs; missing resource is reported.

## Design Notes

- `NpcTemplate` model (pure data) + `NpcResourceIssue`.
- `NpcTemplateRegistry` (pure C#): `Register`/`Resolve`/`Contains`,
  `ValidateResources` resolves sprite + script through `IAssetRegistry`, stamps
  resolution flags, returns missing-resource report.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: register fields, resolve known/unknown, missing sprite/script reported, available no-issue |
| Integration | Resource resolution via AssetRegistry (unit-covered) |
| E2E | N/A |
| Platform | N/A |
| Release | N/A |

## Harness Delta

NPC template primitive consumed by M3.2 spawn + later combat phases.

## Evidence

EditMode 287/287 pass (docs/evidence/editmode-results-2026-05-31-m3-npc-lua.json).
`NpcTemplate`/`NpcResourceIssue` + `NpcTemplateRegistry`. Suite
`VLTK.Tests.Sandbox.NpcTemplateRegistryTests` (9 tests) covers AC1–AC3.
