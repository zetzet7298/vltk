# US-M111-001 M1.11 Visual Golden Snapshot Baseline

## Status

implemented

## Lane

normal

## Intake

Intake #7 (spec_slice, normal). Flags: Weak proof.

## Product Contract

Converted maps can produce reproducible snapshots (image + metadata) for a golden
fixture map so converter changes can be compared. Re-running the snapshot against
a saved golden produces a deterministic difference report; differences beyond a
tolerance are flagged as a visual regression; intentional golden updates record
an update reason.

## Relevant Product Docs

- `docs/spec.md` — "M1.11 — Visual Golden Snapshot Baseline"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: A golden fixture map exists; the snapshot command runs; image and metadata
  are saved.
- AC2: Converter changes; the snapshot command runs again; a difference report is
  produced.
- AC3: Difference exceeds tolerance; validation completes; the report marks a
  visual regression.
- AC4: Asset intentionally changes; the golden is updated; the update reason is
  documented.

## Design Notes

- `GoldenSnapshot` model (pure C#): map id, dimensions, deterministic content
  signature (perceptual buckets / pixel digest), metadata (generatedAt, tool
  version, golden update reason).
- `GoldenSnapshotComparer` (pure C#): compares two snapshots, computes a
  difference ratio, flags regression vs tolerance, supports an explicit
  golden-update path that records the reason. Fully EditMode-testable without a
  live render (operates on captured pixel/metadata payloads).
- Capture from a live RenderTexture is a thin MonoBehaviour wrapper documented
  for Play Mode; the diff/compare logic is the tested core.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: save snapshot, identical-diff=0, over-tolerance regression flag, golden update records reason |
| Integration | Snapshot round-trip serialize/deserialize stable |
| E2E | Live capture in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Establishes the visual-regression proof primitive future converter changes reuse.

## Evidence

EditMode 199/199 pass (docs/evidence/editmode-results-2026-05-31-m1-streaming-minimap-golden.json).
`GoldenSnapshot` model (pure C#) + `GoldenSnapshotComparer` (deterministic perceptual signature,
diff ratio, regression flag, golden-update-with-reason). Suite `VLTK.Tests.Sandbox.GoldenSnapshotTests`
(11 tests) covers AC1 (build/save image+metadata), AC2 (identical diff=0 / diff report), AC3
(over-tolerance + dimension mismatch regression flag), AC4 (golden update records reason),
plus JSON round-trip stability. Live RenderTexture capture remains a documented Play Mode wrapper.
