# US-M62-001 M6.2 Mobile Asset Loading

## Status

implemented

## Lane

normal

## Intake

Intake #24 (spec_slice, normal). Flags: Cross-platform, Performance.

## Product Contract

Mobile-friendly asset loading keeps large converted maps from freezing or
exceeding the memory budget: assets needed by the test scene are loadable, large
maps load asynchronously with visible progress, the runtime reports a budget
warning when memory is exceeded, and Asset Registry load modes stay stable when the
packaging decision changes.

## Relevant Product Docs

- `docs/spec.md` — "M6.2 — Mobile Asset Loading"
- Reuses M0.6 Asset Registry `LoadMode`

## Acceptance Criteria

- AC1: Asset packaging strategy selected; build runs; assets are included or loadable.
- AC2: Large map selected; load starts; loading is asynchronous or progress-visible.
- AC3: Memory budget exceeded; runtime detects risk; GM/Logs report budget warning.
- AC4: AssetBundle/Addressables decision changes; docs update; Asset Registry load
  modes remain stable.

## Design Notes

- `AssetLoadBudgetService` (pure C#): `BeginLoad`/`ReportProgress`/`CompleteLoad`
  async job tracking (AC#2), running `LoadedBytes` vs `BudgetBytes` with
  `CheckBudget` warning (AC#3), `Unload` frees bytes, `RuntimeLoadMode` keeps the
  runtime load mode stable (Editor/Resources/TestFixture → StreamingAssets; bundle/
  addressable preserved) regardless of packaging choice (AC#1/AC#4).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: begin/progress/complete/idempotent, under/over budget warning, unload, fail, load-mode mapping |
| Integration | Budget tracking across multiple loads (unit-covered) |
| E2E | Device async load + budget (documented; not automated in EditMode) |
| Platform | Memory budget enforced (asserted) |
| Release | N/A |

## Harness Delta

Mobile memory-budget primitive on top of M0.6 load modes.

## Evidence

EditMode 386/386 pass (docs/evidence/editmode-results-2026-05-31-m6-mobile.json).
`AssetLoadBudgetService` + `AssetLoadJob`/`MemoryBudgetStatus`. Suite
`VLTK.Tests.Sandbox.AssetLoadBudgetServiceTests` (11 tests) covers AC1–AC4.
