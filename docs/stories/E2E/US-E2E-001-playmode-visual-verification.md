# US-E2E-001 E2E PlayMode Tests + Visual Verification (MCP Orchestrator)

## Status

implemented

## Lane

normal

## Intake

Intake #27 (maintenance_request, normal). Flags: Weak proof, Existing behavior.

## Product Contract

The sandbox runtime is exercised end-to-end in Play Mode (real MonoBehaviour
lifecycle, not just pure logic) and visually verified, so that what was built
through the harness process is proven to actually boot, render, and behave in a
live Unity session. Automation is driven through the unity-mcp-orchestrator skill
(MCP for Unity).

## Relevant Product Docs

- `.pi/skills/unity-mcp-orchestrator/SKILL.md`
- `docs/spec.md` — Validation Strategy (E2E / Play Mode)
- All M0–M6 stories (this is their integrated proof)

## Acceptance Criteria

- AC1: A PlayMode test assembly exists and runs the sandbox through its real
  MonoBehaviour boot lifecycle.
- AC2: E2E tests cover the live map load/switch/unload flow and shared
  AssetRegistry/MapManager/MapRenderer wiring.
- AC3: E2E tests drive the live RegionStreamController through GameObject/Transform
  input and the frame loop (active+ring load, boundary crossing, failure handling,
  budget cap).
- AC4: The sandbox is visually verified in Play Mode via MCP screenshots (game view
  + scene view) with zero runtime errors.

## Design Notes

- `Assets/Tests/PlayMode/VLTK.Tests.PlayMode.asmdef` — PlayMode test assembly
  (UNITY_INCLUDE_TESTS), references all VLTK runtime assemblies.
- `SandboxBootE2ETests` (9 `[UnityTest]`): boot/init, boot report, service wiring,
  subsystem roots, StreamingAssets catalog, map load/switch/unload, registry
  registration.
- `RegionStreamingE2ETests` (6 `[UnityTest]`): first-tick ring load, no-churn,
  boundary cross load/unload, failed region marked + runtime continues, budget cap,
  Update-loop streaming from a Transform.
- Visual verification: `manage_editor(play)` → drive `MapManager.LoadMap` via
  `execute_code` → `manage_camera(screenshot)` game + scene view → `read_console`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | (covered by EditMode 386/386) |
| Integration | Live AssetRegistry/MapManager/MapRenderer wiring asserted in PlayMode |
| E2E | PlayMode 15/15 pass (SandboxBoot + RegionStreaming suites) |
| Visual | Play Mode screenshots (game + scene view), 0 console errors |
| Platform | N/A |

## Harness Delta

Adds the E2E/Play Mode proof layer and visual-verification workflow (MCP
orchestrator) on top of the EditMode unit suite. Sets the `e2e` proof flag on
US-PORT-001, US-M06-001, US-M10-001, US-M19-001.

## Evidence

PlayMode 15/15 pass (job 2c3f917a9fc94846aa563e8f1fee1219, 2.73s) +
visual screenshots (Assets/Screenshots/e2e_playmode_map0.png,
e2e_sceneview_map0.png). Live runtime loaded map 0, catalog=159, registry=1, 0
console errors. Full evidence: docs/evidence/e2e-visual-results-2026-05-31.json.
Combined automated suite: EditMode 386 + PlayMode 15 = 401 tests green.
