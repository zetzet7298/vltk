# Apply Progress — Mobile-Native HUD Layout (HUD-004)

> Change: `mobile-native-hud-layout`
> Slice: **S1** (foundation + chat lane). Status: code complete, structurally verified.
> Unity runtime verification (screenshot + EditMode tests) **PENDING** — worker lacks Unity MCP tools.

## Slice S1 — Foundation + chat lane

### S1.1 — Remove `HideMobileJoystick()` force-hide ✅ (code done)
- Removed `_joystickHidden` field, the `HideMobileJoystick()` method + its doc comment, and the
  `HideMobileJoystick()` call in `Update()`.
- Replaced with a comment explaining the joystick stays VISIBLE/ACTIVE for mobile-native play
  (spawned by SandboxManager, sortingOrder 500, above UIToolkit HUD).
- Verified: `grep HideMobileJoystick` → only the explanatory comment remains; no dangling refs.
- File: `Assets/Scripts/UI/GameHudController.cs`

### S1.2 — Strip PC `快捷栏` replica bottom-center strip ✅ (code done)
- Replaced the `BottomPanel` (bottom-center strip: 9 hotbar slots + T/P + toggle row + menu row +
  BtnTreasure) with a hidden `PendingRelocation` container (`display:none`).
- All element NAMES preserved (BtnRun, BtnSit, BtnHorse, BtnStatus…BtnChatRoom, BtnTreasure,
  HotbarCenter, ItemSlot0-8, etc.) so `root.Q(name)` in `RegisterClick` + `LoadArt` still resolves.
- `BottomFrame` art (bottom_frame_pc.png) removed from the center lane.
- Verified via XML parse: all parked names FOUND, `BottomPanel` removed.
- File: `Assets/UI/HUD/GameHud.uxml`

### S1.3 — Add empty anchored cluster shells ✅ (code done)
- `CombatCluster` (bottom-right) — empty, S2 populates.
- `QuickSlots` (right side, ascending) — empty, S3 populates.
- `TopGapCluster` (top gap) — empty, S3 populates.
- All `picking-mode: Ignore` passthrough containers.
- Verified via XML parse: all 3 shells FOUND.

### S1.4 — Add USS anchor classes ✅ (code done)
- `.hud-combat-cluster` (absolute, right:24px, bottom:24px, picking-mode Ignore).
- `.hud-quick-slots` (absolute, right:28px, bottom:320px, column-reverse).
- `.hud-top-gap-cluster` (absolute, left:620px, top:8px).
- `.hud-pending-relocation` (display:none — hidden parking).
- All anchor-based, no raw pixel-multiply of art.
- File: `Assets/UI/HUD/GameHud.uss`

### S1.5 — Reserve bottom-center lane ✅ (code done)
- Added UXML comments marking the bottom-center lane RESERVED for the future mobile chat canvas.
- `ChatBar` confirmed as the only bottom-center content; no control element placed there.
- Verified: `BottomPanel` removed; no combat/slot/button element remains in bottom-center.

### S1.6 — Verify top bar + minimap unchanged ✅ (diff-verified)
- `git diff` confirms `TopLeftPanel` (HP/MP/EXP/Stamina/Level/WorldSort + captions) and
  `MinimapPanel` (frame/content/dot/4 map buttons) have ZERO changed lines.
- XML parse confirms all intact elements FOUND.

### S1.7 — Recompile + play mode + screenshot ⚠️ PENDING (no Unity MCP)
- Unity IS running (PID 12867, Unity 6000.4.7f1, project /var/www/vltk-mobile).
- No CS compile errors found in the full editor log (`/tmp/vltk-unity.log`).
- C# braces balanced (190 open = 190 close); UXML valid XML.
- **BLOCKER**: worker does not have Unity MCP tools in its toolset (only read/edit/write/bash/mem_*).
  Cannot enter play mode or capture screenshot. Requires a Unity-MCP-enabled session to verify.

### S1.8 — HUD EditMode regression tests ⚠️ PENDING (no Unity MCP)
- Cannot run `run_tests(mode=EditMode, category_names=["HUD"])` — no Unity MCP tools.
- Requires a Unity-MCP-enabled session.

### S1.9 — Commit + push ✅ (done after report)

## Structural Verification (performed without Unity MCP)

| Check | Method | Result |
|---|---|---|
| C# braces balanced | raw char count | 190 = 190 ✅ |
| No CS errors in log | grep `/tmp/vltk-unity.log` | 0 errors ✅ |
| No dangling refs to removed method | grep `HideMobileJoystick`/`_joystickHidden` | only comment ✅ |
| No C# refs to removed BottomPanel/BottomFrame | grep `Assets/Scripts/` | none ✅ |
| UXML valid XML | ElementTree parse | VALID ✅ |
| New shells present | XML name lookup | CombatCluster/QuickSlots/TopGapCluster FOUND ✅ |
| Parked elements resolvable | XML name lookup | all 18 button names FOUND ✅ |
| Top bar unchanged | git diff | 0 changed lines ✅ |
| Minimap unchanged | git diff | 0 changed lines ✅ |
| BottomPanel removed | XML name lookup | removed = True ✅ |

## Files Changed

| File | Change |
|---|---|
| `Assets/Scripts/UI/GameHudController.cs` | Removed `HideMobileJoystick()` method + `_joystickHidden` field + call in `Update()` |
| `Assets/UI/HUD/GameHud.uxml` | Removed `BottomPanel` strip; added 3 empty shells + `PendingRelocation` hidden container; added chat-lane-reserved comments |
| `Assets/UI/HUD/GameHud.uss` | Added `.hud-combat-cluster`, `.hud-quick-slots`, `.hud-top-gap-cluster`, `.hud-pending-relocation` anchor classes |

## Deviations

- None from design/spec. All S1 tasks implemented as specified.

## Remaining (S2/S3)

- **S2**: populate CombatCluster (1 main + 5 sub fan) + run/horse/sit action buttons with PC sprites.
- **S3**: populate QuickSlots (3) + relocate 8 menu buttons + buff + Bảo Vật into TopGapCluster + icon wiring.
- **S1.7/S1.8**: Unity runtime verification (screenshot + EditMode tests) — must be run by a
  Unity-MCP-enabled session before marking S1 fully verified.

## S1 Runtime Verification (parent session, Unity MCP) ✅

- **Recompile**: clean (no CS errors). Editor `ready_for_tools: true`.
- **HUD EditMode tests**: `13/13 passed`, 0 failures, 0 regression (0.6s). Category `HUD`.
- **Play mode + vision screenshot** (`Assets/Screenshots/mobile-hud-s1-overlay.png`):
  - **Joystick**: PRESENT bottom-left (jade medallion, gold border). ✅ force-hide removed.
  - **Bottom-center**: CLEAR — PC `快捷栏` replica toolbar fully removed; only game world
    visible. Chat-lane reserved. ✅
  - **Top bar**: PRESENT & intact — Level `Cấp 200`, HP/MP/Stamina/EXP bars + Vietnamese
    captions. ✅ unchanged.
  - **Minimap**: PRESENT & intact top-right — frame, player dot, coords, map buttons. ✅
    unchanged.
  - **Combat/quick slots**: not yet present (correct — S2/S3 work).
- **Verdict**: S1 PASS. Clean mobile canvas: joystick bottom-left, empty bottom-center, top bar
  + minimap intact. Ready for S2.

## Slice status
- S1 ✅ DONE + verified (commit `9cdc76675`)
- S2 ⏳ pending (combat cluster 1+5 + action buttons)
- S3 ⏳ pending (quick slots + menu relocation)
