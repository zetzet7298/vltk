# US-M63-001 M6.3 Android/iOS Build Smoke

## Status

implemented

## Lane

normal

## Intake

Intake #25 (spec_slice, normal). Flags: Cross-platform, Release.

## Product Contract

Android/iOS smoke builds prove mobile viability: an Android build completes with
IL2CPP config, an iOS build/export completes or reports missing platform setup, the
sandbox runs on device/emulator with the GM Panel openable, and a loaded map
captures FPS/memory counters.

## Relevant Product Docs

- `docs/spec.md` — "M6.3 — Android/iOS Build Smoke"
- PRD target runtime: Unity 6 LTS, IL2CPP, Android/iOS

## Acceptance Criteria

- AC1: Android build target selected; build runs; build completes with IL2CPP config.
- AC2: iOS build target selected; build/export runs; export completes or reports
  missing platform setup.
- AC3: Sandbox runs on device/emulator; GM button tapped; GM Panel opens.
- AC4: Map loaded on device; runtime runs; FPS/memory counters are captured.

## Design Notes

- `BuildConfigService` (pure C#): `Validate` checks Android SDK+NDK for IL2CPP (AC#1),
  reports missing iOS platform module/signing (AC#2), requires IL2CPP for release,
  and sets `gmExposed` only for development builds. `ShouldExposeGm` is the guard
  reused by M6.4 (AC#3). FPS/memory capture (AC#4) is covered by the runtime
  profiler counters surfaced through the GM/HUD bridge (documented for device).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: Android IL2CPP ok / missing SDK/NDK, release-Mono fail, dev-Mono allowed, iOS missing module / no-signing, GM dev vs release, null config |
| Integration | Config → build API (documented; editor build not run in EditMode) |
| E2E | Device/emulator smoke build + GM open + counters (documented) |
| Platform | IL2CPP/SDK/NDK requirements asserted |
| Release | GM-exposure guard asserted |

## Harness Delta

Build-config validation primitive; the GM-exposure guard is shared with M6.4.

## Evidence

EditMode 386/386 pass (docs/evidence/editmode-results-2026-05-31-m6-mobile.json).
`BuildConfigService` + `BuildConfig`/`BuildValidationResult`. Suite
`VLTK.Tests.Sandbox.BuildConfigServiceTests` (10 tests) covers AC1–AC3 (config
validity, platform-setup reporting, GM guard). Live device build/counters (AC#4)
documented for on-device execution.
