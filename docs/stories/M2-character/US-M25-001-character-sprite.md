# US-M25-001 M2.5 Character Sprite Placeholder to Real Sprite

## Status

implemented

## Lane

normal

## Intake

Intake #12 (spec_slice, normal). Flags: Weak proof.

## Product Contract

The player placeholder can swap to decoded sprite clips: when a registered clip is
selected the player renders using decoded frames, animation direction changes as
the player moves, frame pivot/offset stays stable, and diagnostics surface an
incomplete clip.

## Relevant Product Docs

- `docs/spec.md` — "M2.5 — Character Sprite Placeholder to Real Sprite"
- `docs/ARCHITECTURE.md`

## Acceptance Criteria

- AC1: Sprite clip is registered; GM selects it; player renders using decoded
  frames (frame advances at the clip frame rate).
- AC2: Direction changes; player moves; animation direction changes if available.
- AC3: Frame offset exists; sprite animates; pivot/offset remains stable.
- AC4: Clip missing frames; clip selected; diagnostics show incomplete clip.

## Design Notes

- `ClipPlaybackService` (pure C#): advances time by deltaTime at `frameRate`,
  `CurrentFrameInDirection`/`CurrentAtlasFrameIndex` (direction * framesPerDir +
  frame), `SetDirection` wrap/clamp (single-direction ignored), `CurrentPivotOffset`
  (pivot + per-frame offset, falls back to pivot), `Diagnose` flags incomplete
  clips (expected vs available frames).
- Built on `SpriteClipDefinition` (M0.8) + atlas frame index (M0.8 SprAtlasPacker).
- MonoBehaviour driver maps atlas frame index to a Sprite (documented).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: frame advance, loop, atlas index, direction switch/wrap/single, pivot+offset, diagnostics complete/partial/missing |
| Integration | Uses SpriteClipDefinition + atlas index (unit-covered) |
| E2E | Live atlas sprite swap in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Animation playback primitive reused by NPC sprite work in M3.

## Evidence

EditMode 248/248 pass (docs/evidence/editmode-results-2026-05-31-m2-character.json).
`ClipPlaybackService` + `ClipPlaybackDiagnostics`. Suite
`VLTK.Tests.Sandbox.ClipPlaybackTests` (12 tests) covers AC1–AC4 plus safe-defaults.
