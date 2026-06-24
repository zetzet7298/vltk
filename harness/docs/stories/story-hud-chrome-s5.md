# S5 HUD Chrome Port (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar)

## Status

planned

## Lane

normal

## Intake

- **Intake ID:** #25
- **Input type:** spec-slice (bounded parity slice of HUD full port intake #24)
- **Lane:** normal
- **Risk flags:** existing-behavior, cross-platform, public-contracts (3 flags → normal with stronger validation)

## Product Contract

Port the six HUD "chrome" widgets from the vltkunity Unity project into vltk-mobile with 100% UI/UX parity. Source of truth is `/var/www/vltk-mobile/vltkunity` (Unity project), **not** jx-source PC. Target already wires two of six adapters; the remaining four (Money, Avatar, DeviceStatus, ProgressBar) are gap-fill.

| Widget | vltkunity source (prefab + script) | vltk-mobile target | Work type |
| MiniMap | `WorldGameUI/Prefabs/MiniMap.prefab` + `Scripts/UI/MiniMap.cs` | `MiniMapVltkUnityAdapter` + `MiniMapVltkUnityAdapterTests` | parity verify |
| TopBar | `WorldGameUI/Prefabs/TopBar.prefab` + `Scripts/UI/TopBar.cs` | `TopBarVltkUnityAdapter` + `TopBarVltkUnityAdapterTests` | parity verify |
| Money | `WorldGameUI/Prefabs/Money.prefab` | (none) | gap-fill |
| Avatar | `WorldGameUI/Prefabs/Avatar.prefab` | (none) | gap-fill |
| DeviceStatus | `WorldGameUI/Prefabs/DeviceStatus.prefab` + `Scripts/UI/DeviceStatus.cs` | (none) | gap-fill |
| ProgressBar | `WorldGameUI/Prefabs/ProgressBar.prefab` | (none) | gap-fill |

## Plan

**Authoritative implementation plan:** `harness/planning/s5-plan.md`

- **Framing:** cross-framework port (uGUI source → UI Toolkit target); prefabs cannot be copied, visual tree rebuilt as UXML/USS + C#.
- **Critical path (Step §1):** widen `HudSnapshot` + `IRuntimeStateProvider` with `currentStamina`/`maxStamina`/`staminaFraction`, `PlayerMaxMana` (replaces hardcoded 100), `PlayerMaxExp`/`expFraction`. Compile break across all implementors — must sweep every `IRuntimeStateProvider` implementor.
- **TopBar (§3):** bind stamina (was HP 🔴), real EXP fraction (was fudge), apply carve-progress sprites + `UTM Cafeta` font (T5/P3). Do NOT replicate vltkunity's `UdpateUIMP` HPMax bug.
- **MiniMap (§2):** port position formula `(left/16)+xRatio`, `yRatio−(top/16)` (M1 🔴); coord text `top:left` (M3).
- **Gap-fill (§4–§7):** Money (3 currency rows + recharge intent), Avatar (frame/portrait/level), DeviceStatus (Wifi/Battery/Time/RTT, implement live updates — source stub is empty), ProgressBar (reusable `HudProgressBar` helper).
- **Tests:** category `HUD` (existing — `TopBarVltkUnityAdapterTests` already uses it); dev loop `category_names=["HUD"]`, full suite only pre-push.
- **Residual decisions (§13):** M1 sign/projection, currency binding, recharge target, level-text de-dup, portrait + Wifi/Battery art sourcing, PanelSettings verify.

## Relevant Product Docs

- `docs/stories/story-hud-port.md` (parent initiative)
- `docs/stories/story-hud-slices.md` (slice plan, S5 row)
- `docs/FEATURE_INTAKE.md`

## Acceptance Criteria

- MiniMap and TopBar adapters match vltkunity MiniMap/TopBar prefab structure 1:1 (verified by existing + extended EditMode tests).
- Money, Avatar, DeviceStatus, ProgressBar adapters are ported and registered in `GameHudController`, mirroring vltkunity layout/data binding.
- All six widgets render with the same UXML structure / data sources as vltkunity (visual + data parity).
- EditMode tests cover each adapter under `Assets/Tests/EditMode/UI/` (category `HUD` recommended).
- No zh user-facing text remains (vi localization where applicable).
- No regression to existing HUD wiring / HudDataBridge / IHudCommandBus.

## Risk Checklist

| Flag | Hit? | Reason |
| Auth | no | HUD chrome has no login/session |
| Authorization | no | no roles/permissions |
| Data model | no | read-only display of HudDataBridge state |
| Audit/security | no | no sensitive data |
| External systems | no | no provider SDKs |
| Public contracts | yes | HUD widget shape is client-visible behavior; adapter wiring is a contract |
| Cross-platform | yes | mobile HUD layout/scale vs desktop Unity reference |
| Existing behavior | yes | MiniMap/TopBar adapters already implemented + test-covered; changes can break wiring |
| Weak proof | partial | Money/Avatar/DeviceStatus/ProgressBar untested; MiniMap/TopBar tested |
| Multi-domain | no | single HUD chrome area only |

**3 flags → normal lane with stronger validation.** No hard gates triggered (no auth, no authz, no data loss, no audit, no external provider, no validation removal).

## Design Notes

- Source of truth: vltkunity prefabs under `client/Assets/Resources/WorldGameUI/Prefabs/` and scripts under `client/Assets/Scripts/UI/`.
- Pattern to follow: existing `MiniMapVltkUnityAdapter` / `TopBarVltkUnityAdapter` (ctor `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, `IDisposable`).
- Wiring point: `Assets/Scripts/UI/GameHudController.cs` (lazy-init adapters at lines ~337/340).
- Test location: `Assets/Tests/EditMode/UI/` (follow `TopBarVltkUnityAdapterTests` / `MiniMapVltkUnityAdapterTests` shape).
- Data binding via `HudDataBridge`; commands via `IHudCommandBus`.
- Localize any zh strings → vi (per AGENTS.md user-facing-Vietnamese rule).

## Validation

When updating durable proof status, use numeric booleans:
`scripts/bin/harness-cli story update --id story-hud-chrome-s5 --unit 1 --integration 1 --e2e 0 --platform 0`.

| Layer | Expected proof |
| Unit | EditMode adapter tests (per-widget ctor/null-arg/data-bind) under `Assets/Tests/EditMode/UI/`, category `HUD` |
| Integration | HUD controller wires all six adapters; HudDataBridge feeds values |
| E2E | manual: HUD renders all six widgets in-game |
| Platform | mobile build HUD draw-call / layout smoke |
| Release | full EditMode suite green before push |

## Harness Delta

- Intake #25 recorded (spec-slice, normal).
- This story packet created under `docs/stories/`.
- Recommend category `HUD` be added for new tests (parallel to existing `CaiBang`/`Slow`).

## Evidence

- vltkunity source confirmed: 6 prefabs + 4 UI scripts (MiniMap.cs, TopBar.cs, DeviceStatus.cs, map.client/minimap.cs).
- vltk-mobile target confirmed: `TopBarVltkUnityAdapter.cs`, `MiniMapVltkUnityAdapter.cs`, both with EditMode tests; wired in `GameHudController.cs:337,340`.
