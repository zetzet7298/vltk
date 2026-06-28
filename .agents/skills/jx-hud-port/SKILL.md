---
name: jx-hud-port
description: "Trigger: port/fix HUD to match VLTK PC — bars (HP/MP/EXP/stamina), minimap, chat, toolbar, action buttons (run/sit/horse/trade), flipped/overlapping/missing icons, joystick blocked, Ui3 INI, 顶部控制条/工具控制条/小地图_小/聊天条.ini, 8da7027d/dc11ac12/ec10b91e/c9c8a750. Ports from PC INI + SPR, 100% parity, no invented art."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "2.0"
---

# JX / VLTK PC HUD Porting

Make the Unity mobile HUD match the PC client. PC INI + PC SPR are the source of
truth. Never redesign from memory; never invent art.

## Activation Contract

Activate when the task touches the HUD: bars, minimap, chat bar, toolbar, action
buttons, popup windows, Vietnamese labels, icon flipping/overlap/missing, or a
joystick blocked by UI. Do **not** activate for skill-combat logic, map/terrain
geometry, NPC spawn, or player-sprite layering (other skills own those).

## Hard Rules

1. **PC is the source of truth.** Read the matching PC INI (GBK-decoded) and the
   referenced SPR before editing Unity. Canonical unpacked tree:
   `/var/www/vltksource_new/vl_update_27/pak_unpacked/<pak>/unknown/<hash>.{ini,spr}`.
2. **No phantom paths.** `jxwin-kinnox/SourceNew/swrod3/...`, `UiShell.cpp`,
   `WndButton.cpp`, `WndImagePart.cpp` do **not** exist in `vltksource_new`. Do not
   cite them. Resolve SPR names → hashes via the `jx-pc-resource-resolver` skill.
3. **Decode with the right tool, no blanket flipping.** Use
   `~/Projects/vltktool/extract_item_spr.py` (default top-down = correct game
   orientation; verified `btn_run.png` md5 == PC frame0). Do not run blanket
   top-bottom flips — that corrupts correct output. Flip one PNG only if a screenshot
   proves it inverted.
4. **Stage art to BOTH paths.** Runtime loads via
   `HudArtPathResolver.ResolveArtRoot` → `Assets/StreamingAssets/UI/HUD/Art/`
   (nested static class in `GameHudController.cs`). Copying only into
   `Assets/UI/HUD/Art/` is a **silent load failure** (no log, missing texture).
   Always stage to both; the `Assets/UI/HUD/Art/` copy is editor/SVN visibility only.
5. **PC SPRs only.** No homemade/placeholder art unless the user explicitly accepts one.
6. **Joystick stays touchable.** uGUI joystick must keep receiving pointer events.

### Per-area rules

- **Bars (8da7027d.ini):** PC clips the fill by percent (`KWndImagePart`-style), it
  does not scale it shorter. UI Toolkit: track fixed W×H + `overflow:hidden`; fill
  width = %; fill background-size = original bar image. Text = `cur/max` for
  HP/MP/Stamina, `pct%` for EXP. Offsets + fill sizes: see `references/`.
- **Action buttons (dc11ac12.ini = 工具控制条):** run/sit/horse are `CheckBox=1`
  toggles with two frames each. Decode both, stage `btn_<n>.png` (off) +
  `btn_<n>_on.png` (on), and swap at runtime. **Horse is inverted:**

  | Button | off (Up) | on (Down) | on = state |
  | --- | --- | --- | --- |
  | Run (đi/chạy) | f0 | f1 | walking (`!IsRunning`) |
  | Sit (thiền) | f0 | f1 | `IsMeditating` |
  | Horse (ngựa) | **f1** | **f0** | `Mount.IsMounted` |

  Swap via `GameHudController.RefreshActionToggles()` (reads the three bools each
  frame, reloads only on change). Both `BtnXxx` and `ActionBtnXxx` clusters sync.
- **Quick slots:** assigned slots cast on tap; no long-press picker, no
  `_longPress*` flags. Picker opens on tap of an empty slot only.
- **Joystick pick modes:** root + children `PickingMode.Ignore`; re-enable
  `PickingMode.Position` only on real buttons in `RegisterClick`. Keep bottom-left
  content at `x >= 155` to free the joystick lane.
- **Vietnamese text:** localize text only; art stays PC-derived. Use
  `PcHudVietnameseTextOverlay.cs` for labels (UI Toolkit text was unreliable).
  Scale a 1280×720 reference by `Screen.w/1280`, `Screen.h/720`.

## Decision Gates

| Situation | Action |
| --- | --- |
| PC SPR exists in pak_unpacked | Decode → stage PNG to both art roots → bind |
| Art filename unclear | Resolve via `jx-pc-resource-resolver` (GBK hash); never guess |
| CheckBox=1 toggle button | Decode Up AND Down frames; wire state swap (table above) |
| Sprite looks inverted | Flip that one PNG only after a screenshot proves it |
| Only Chinese variant exists | Stop; ask user — do not ship CN text |
| Need a SPR hash | Read `references/pc-hud-resource-uids.md`, do not inline the catalog |

## Execution Steps

1. Read the relevant PC INI (`8da7027d`/`dc11ac12`/`ec10b91e`/`c9c8a750`, GBK-decoded).
2. Resolve each referenced SPR to its hash (`references/pc-hud-resource-uids.md` or
   `jx-pc-resource-resolver`).
3. Decode with `~/Projects/vltktool/extract_item_spr.py --file <spr> --out-root <dir>`.
4. Stage PNG(s) to **both** `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/`.
5. Bind in `GameHudController.LoadArt()` / USS / `RefreshActionToggles()` as needed.
6. Refresh Unity, compile, enter play mode, capture a Game View screenshot.
7. Verify: no errors; bars clip (not stretch); minimap button inside frame; icons
   upright; joystick unblocked; VI text visible; toggle frames swap on state change.
8. Save `Assets/Scenes/Sandbox.unity`.

## Output Contract

- PC files read (INI hash + section) and SPR hashes used, with evidence they match.
- Unity files changed and art staged to **both** paths.
- Screenshot evidence of the verified state.
- Any case where only the Chinese variant exists (flagged for user decision).
- Residual risks (e.g. unverified pixel offsets).

## References

- `references/pc-hud-resource-uids.md` — full SPR hash → name catalog (bars, toolbar,
  minimap, chat channels) + bar panel offsets/fill sizes.
- `scripts/extract_ui_spr.py` — **deprecated** legacy decoder (hardcodes a phantom
  `jxwin-kinnox` path). Use `~/Projects/vltktool/extract_item_spr.py`.
- `jx-pc-resource-resolver` skill — GBK path → JX Pack Hash → filename resolution.
- `jx-pc-port-rule` skill — mandatory PC-source-first guardrail for all port tasks.
- Unity files: `Assets/UI/HUD/GameHud.uxml` + `GameHud.uss`,
  `Assets/Scripts/UI/GameHudController.cs` (+ nested `HudArtPathResolver`),
  `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`,
  `Assets/Scripts/UI/CombatSkillSlotController.cs`,
  `Assets/Scripts/Sandbox/SandboxRuntimeState.cs`, `Assets/UI/HUD/HudPanelSettings.asset`.
