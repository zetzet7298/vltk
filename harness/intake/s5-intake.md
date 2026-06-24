# Intake S5 — HUD Chrome Port (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar)

**Date:** 2026-06-23
**Task:** Port HUD chrome (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar) from vltkunity → vltk-mobile, 100% UI/UX parity. Source of truth = `/var/www/vltk-mobile/vltkunity` (Unity project), NOT jx-source PC. Target already has MiniMapVltkUnityAdapter + TopBarVltkUnityAdapter.
**Classification-only task — NO Unity code edits.**

---

## Required reading (done)

- `docs/FEATURE_INTAKE.md` ✅
- `AGENTS.md` (root + harness) ✅
- `docs/HARNESS.md` ✅
- `docs/stories/story-hud-port.md`, `docs/stories/story-hud-slices.md` ✅ (existing intake #24 + slice plan)
- Matrix: `scripts/bin/harness-cli query matrix` ✅

---

## Input type

**`spec-slice`** — bounded parity slice of the larger HUD full-port initiative (intake #24, high-risk). The task is explicitly scoped to 6 HUD chrome widgets, not the full HUD. Per FEATURE_INTAKE, a spec slice lands as a story packet.

---

## Risk checklist

| Flag | Hit? | Reason |
| Auth | no | HUD chrome has no login/session |
| Authorization | no | no roles/permissions/tenant scope |
| Data model | no | read-only display of HudDataBridge state; no schema/migration |
| Audit/security | no | no sensitive data, no audit logs |
| External systems | no | no email/payments/cloud/SDKs/queues/webhooks |
| Public contracts | **yes** | HUD widget shape + adapter wiring is client-visible behavior |
| Cross-platform | **yes** | mobile HUD layout/scale vs desktop Unity reference (vltkunity) |
| Existing behavior | **yes** | MiniMap/TopBar adapters already implemented + test-covered; changes can break wiring |
| Weak proof | partial | Money/Avatar/DeviceStatus/ProgressBar untested |
| Multi-domain | no | single HUD chrome area only |

**Flags: 3** (public-contracts, cross-platform, existing-behavior).

No hard gates triggered: no Auth, no Authorization, no data loss/migration, no Audit/security, no external provider, no removal/weakening of validation.

---

## Lane decision

```
Flags: 3 → "normal with stronger validation" (FEATURE_INTAKE: 2-3 flags = normal with stronger validation)
Hard gates: none
=> Lane: normal
```

This matches the task's hypothesis ("likely normal — bounded HUD chrome"). The full HUD port was high-risk (#24); this S5 slice is the bounded, single-area child → normal.

---

## Source/target recon (verified, not guessed)

### vltkunity source (source of truth) — confirmed present
`client/Assets/Resources/WorldGameUI/Prefabs/`:
- MiniMap.prefab + TopBar.prefab + Money.prefab + Avatar.prefab + DeviceStatus.prefab + ProgressBar.prefab (all 6 ✅)
`client/Assets/Scripts/UI/`:
- MiniMap.cs, TopBar.cs, DeviceStatus.cs (+ map.client/minimap.cs)

### vltk-mobile target — confirmed
- `Assets/Scripts/UI/TopBarVltkUnityAdapter.cs` ✅ (ctor: `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, IDisposable)
- `Assets/Scripts/UI/MiniMapVltkUnityAdapter.cs` ✅ (same ctor pattern)
- Wired in `Assets/Scripts/UI/GameHudController.cs:337 (_vltkunityTopBar)`, `:340 (_vltkunityMiniMap)`
- Tests: `Assets/Tests/EditMode/UI/TopBarVltkUnityAdapterTests.cs`, `Assets/Tests/EditMode/UI/MiniMapVltkUnityAdapterTests.cs`
- Gap (not present): Money, Avatar, DeviceStatus, ProgressBar adapters

### Conclusion
2/6 = parity verify; 4/6 = gap-fill. Bounded blast radius, existing adapter pattern to follow → normal lane.

---

## Harness commands run

```bash
# 1. Matrix snapshot
scripts/bin/harness-cli query matrix

# 2. Intake recording
scripts/bin/harness-cli intake \
  --type "spec-slice" \
  --summary "S5 HUD chrome port (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar) tu vltkunity sang vltk-mobile, 100% UI/UX parity. Source of truth = /var/www/vltk-mobile/vltkunity (Unity project), KHONG phai jx-source PC. Target da co MiniMapVltkUnityAdapter + TopBarVltkUnityAdapter (parity/gap-fill cho Money/Avatar/DeviceStatus/ProgressBar)." \
  --lane normal \
  --flags "existing-behavior,cross-platform,public-contracts" \
  --docs "docs/stories/story-hud-chrome-s5.md" \
  --story "story-hud-chrome-s5" \
  --notes "..."
# => Intake #25 recorded.

# 3. Story registration
scripts/bin/harness-cli story add \
  --id "story-hud-chrome-s5" \
  --title "S5 HUD chrome port (MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar) vltkunity->vltk-mobile parity" \
  --lane normal
# => Story story-hud-chrome-s5 added.
```

---

## Deliverables

- **Intake:** #25 (normal, spec-slice)
- **Story:** `docs/stories/story-hud-chrome-s5.md` (created) + durable row `story-hud-chrome-s5` added
- **This doc:** `harness/intake/s5-intake.md`

---

## Classification summary

| Field | Value |
| **Lane** | `normal` |
| **Input type** | `spec-slice` |
| **Intake ID** | #25 |
| **Story** | `story-hud-chrome-s5` (`docs/stories/story-hud-chrome-s5.md`) |
| **Parent initiative** | intake #24 (HUD full port, high-risk) |
| **Risk flags (3)** | public-contracts, cross-platform, existing-behavior |
| **Hard gates** | none |
| **Work shape** | 2/6 parity verify (MiniMap, TopBar), 4/6 gap-fill (Money, Avatar, DeviceStatus, ProgressBar) |
| **Source of truth** | `/var/www/vltk-mobile/vltkunity` (Unity project) — NOT jx-source PC |
| **Adapter pattern** | `(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)`, IDisposable; wire in `GameHudController.cs`; test under `Assets/Tests/EditMode/UI/` |

---

## Residual notes for implementation lane (out of scope here)

- Add category `HUD` to new EditMode tests (parallel to `CaiBang`/`Slow`).
- Localize any zh strings → vi.
- Decide mobile layout/scale adaptation policy vs pixel-exact vltkunity layout.
- No Unity code was edited in this classification task (constraint honored).
