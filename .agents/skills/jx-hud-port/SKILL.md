---
name: jx-hud-port
description: >-
  Port, fix, or visually match the JX Online 1 / Võ Lâm Truyền Kỳ PC HUD and UI
  in the VLTK-mobile Unity client using the real PC Ui3 INI layout and SPR art.
  Use this skill whenever the user asks for HUD/UI to look like PC, complains that
  HUD elements are placeholders, overlapping, flipped, misaligned, missing icons,
  blocking the mobile joystick, or mentions HP/MP/EXP/stamina bars, minimap buttons,
  chat bar, hotbar, action buttons (run/sit/horse/trade), inventory/skill/team/faction
  buttons, Ui3, 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini, 小地图_小.ini,
  or PC sprites, 8da7027d.ini, dc11ac12.ini, ec10b91e.ini, c9c8a750.ini. This skill preserves the hard-won fixes: use real PC SPR assets,
  decode/flip UI icons correctly, mimic KWndImagePart clipping for bars, keep joystick
  touches unblocked, and add Vietnamese text without inventing art.
---

# JX / VLTK PC HUD Porting

Use this skill to make the Unity mobile HUD visually match the PC client as closely as
possible. Do not redesign the HUD from memory. Treat PC INI + PC SPR as source of truth.

## First principle

PC HUD is **data-driven**:

- Layout comes from `jxwin-kinnox/SourceNew/swrod3/Utility/Run/Ui/ui3/*.ini`
- Art comes from `jxwin-kinnox/SourceNew/swrod3/Utility/Run/spr/Ui3/**/*.spr`
- Runtime values come from UI classes in `SourceNew/swrod3/SwordOnline/Sources/S3Client/Ui/UiShell.cpp`

When a user says “giống PC 100%”, inspect those three sources before changing Unity.

## Key PC files

Read these before editing HUD layout:

| PC file / Hashed name in `vltksource_new` | Purpose |
|---|---|
| `Utility/Run/Ui/ui3/顶部控制条.ini` (`8da7027d.ini`) | HP/MP/Stamina/EXP/Level top status bar |
| `Utility/Run/Ui/ui3/玩家信息主界面.ini` (`dc11ac12.ini` / `mainui.ini`) | Main HUD, chat input, bottom quick slots |
| `Utility/Run/Ui/ui3/工具控制条.ini` (`dc11ac12.ini`) | Run/sit/horse/trade/status/items/skills/team/faction/PK buttons |
| `Utility/Run/Ui/ui3/小地图_小.ini` (`ec10b91e.ini`) | Minimap frame, scene name/pos, minimap buttons |
| `Utility/Run/Ui/ui3/聊天条.ini` (`c9c8a750.ini`) | Chat Window UI with channel buttons |
| `S3Client/Ui/UiShell.cpp` | Player_Life/Mana/Stamina/Exp update logic |
| `S3Client/Ui/Elem/WndButton.cpp` | `KWndImageTextButton`, `Set2IntValue`, text toggles |
| `S3Client/Ui/Elem/WndImagePart.cpp` | `KWndImagePart.SetPart`: percent clipping, not scaling |

## PC Client Resource Matching (`vltksource_new` Pack)

When porting or aligning HUD assets using the canonical source in `/var/www/vltksource_new/vl_update_27/pak_unpacked/`, resources are stored as Hash UIDs inside `unknown/` folders (due to the unpacker not having the full dictionary).

The matching files for the HUD are:

### Top Status Bar (Thanh Trạng Thái Phía Trên)
*   **新血条面板.spr (Khung viền máu mới)**: `973816f3.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **生命条.spr (Thanh sinh lực - HP)**: `74b299b9.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **内力条.spr (Thanh nội lực - MP)**: `b72be14b.spr` (in `update01/unknown/`, `spr/unknown/`)
*   **体力条.spr (Thanh thể lực - Stamina)**: `83e13762.spr` (in `update01/unknown/`, `spr/unknown/`)
*   **经验条.spr (Thanh kinh nghiệm - EXP)**: `f5d017dd.spr` (in `update01/unknown/`, `spr/unknown/`)

### Bottom Shortcut Bar (Thanh Phím Tắt Phía Dưới - Toolbar)
*   **聊天室按钮.spr**: `de6475b9.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **人物属性按钮_0.spr (Nhân vật - F1)**: `cf92ecbe.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **背包按钮.spr (Hành trang - F2)**: `175edefc.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **子母袋按钮.spr (Túi phụ / Túi mở rộng)**: `c732baf9.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **技能按钮.spr (Võ công - F3)**: `2317ae46.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **任务按钮.spr (Nhiệm vụ - F4)**: `a3717b5e.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **队伍按钮.spr (Tổ đội - F6)**: `b3455277.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **帮会按钮.spr (Bang hội)**: `234770bb.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **跑步按钮.spr (Chạy/Đi bộ)**: `41d364a1.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **打坐按钮.spr (Đả tọa / Ngồi thiền)**: `82a5aa21.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **骑马按钮.spr (Lên/xuống ngựa)**: `fc8a4f16.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **交易按钮.spr (Giao dịch)**: `cc903517.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **PK按钮.spr (Đóng mở PK)**: `42e22aac.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)
*   **摄像机按钮.spr (Quay phim / Chụp ảnh)**: `9aca89f7.spr` (in `dmjx01/unknown/`, `updatejx08/unknown/`)

### Minimap (Bản đồ nhỏ)
*   **小地图－切换按钮0.spr (Nút thu phóng)**: `14f1acc9.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **小地图－洞窟.spr (Bản đồ sơn động)**: `2e66ad6f.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **小地图－世界大地图按钮.spr (Bản đồ thế giới)**: `c33f656f.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **小地图－旗帜按钮.spr (Nút cắm cờ)**: `c9371d0d.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **地图小旗帜.spr (Cờ nhỏ cắm trên radar)**: `206e74a3.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)

### Chat Channels (Biểu tượng và Kênh Chat)
*   **频道开与关a/b.spr**: `3b255f40.spr` / `34fc44d5.spr` (in `update01/unknown/`, `spr/unknown/`)
*   **聊天条底部/顶部/中部改.spr**: Đáy: `bdf9af98.spr`, Đỉnh: `8fa68495.spr`, Giữa: `3483ec02.spr` (in `update01/unknown/`, `spr/unknown/`)
*   **聊天条阴影按钮.spr**: `bcca4952.spr` (in `update01/unknown/`, `spr/unknown/`)
*   **通用拖动条.spr (Nút cuộn chat)**: `23fe2a10.spr` (in `update01/unknown/`, `spr/unknown/`, `dmjx01/unknown/`)
*   **Kênh chat (Nút chọn & Icon)**:
    *   *Nói thầm*: Chọn: `3be3a09f.spr` / Icon: `69fbc7e6.spr`
    *   *Bạn bè*: Chọn: `7addeacc.spr` / Icon: `2c66b90e.spr`
    *   *Thế giới*: Chọn: `59b0db0b.spr` / Icon: `50d91112.spr`
    *   *Tổ đội*: Chọn: `8ff6d47a.spr` / Icon: `a9d1f2f2.spr`
    *   *Môn phái*: Chọn: `4074febd.spr` / Icon: `69f46c8c.spr`
    *   *Lân cận*: Chọn: `314af2aa.spr` / Icon: `f434779f.spr`
    *   *Thành thị*: Chọn: `a8671666.spr` / Icon: `b6d58e29.spr`
    *   *Hệ thống/GM*: Chọn: `b2a6f8a3.spr` / Icon: `e277c438.spr`
    *   *Bang hội*: Chọn: `401cf1d6.spr` / Icon: `8340787f.spr` (in `update03/unknown/`, `dmjx01/unknown/`)
    *   *Liên minh*: Chọn: `9d6df5e0.spr` / Icon: `64f8476e.spr`
    *   *Chiến trường Tống*: Chọn: `58166d73.spr` / Icon: `8f8c13b9.spr`
    *   *Chiến trường Kim*: Chọn: `bcc87eec.spr` / Icon: `efb03ac7.spr`
    *   *Tự nói (Nói thường)*: Icon: `50304af7.spr`

Read extracted art in Unity:

- `Assets/UI/HUD/Art/bar_panel_bg.png` (`新血条面板.spr`)
- `Assets/UI/HUD/Art/bar_hp_fill.png` (`生命条.spr`)
- `Assets/UI/HUD/Art/bar_mp_fill.png` (`内力条.spr`)
- `Assets/UI/HUD/Art/bar_stamina_fill.png` (`体力条.spr`)
- `Assets/UI/HUD/Art/bar_exp_fill.png` (`经验条.spr`)
- `Assets/UI/HUD/Art/btn_*.png` for PC button icons

## Current Unity implementation

Main files:

- `Assets/UI/HUD/GameHud.uxml`
- `Assets/UI/HUD/GameHud.uss`
- `Assets/Scripts/UI/GameHudController.cs`
- `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`
- `Assets/Scripts/Sandbox/SandboxRuntimeState.cs`
- `Assets/UI/HUD/HudPanelSettings.asset`

The HUD uses UI Toolkit for sprites/panels and an IMGUI overlay for Vietnamese text.
That overlay exists because UI Toolkit text was unreliable without full runtime text
settings in this project. Keep art in UI Toolkit; only use IMGUI/uGUI for text if needed.

## SPR extraction workflow

If required art is missing:

1. Look for the SPR path in the PC INI using GBK decoding.
2. Search under `jxwin-kinnox/SourceNew/swrod3/Utility/Run/spr/Ui3`.
3. Decode SPR frames to PNG under `Assets/UI/HUD/Art`.
4. Refresh Unity assets.
5. Bind the PNG in `GameHudController.LoadArt()` or USS.

Existing extractor reference:

- `/tmp/extract_ui_spr.py` from the HUD port session

Important gotcha: UI SPR frames were observed vertically flipped when decoded with the
initial script. For button/icon PNGs, flip top-bottom after extraction. Do **not** flip
bar fill PNGs unless visual inspection proves they are inverted.

Suggested icon flip command:

```bash
python3 - <<'PY'
from PIL import Image
from pathlib import Path
root=Path('/var/www/vltk-mobile/Assets/UI/HUD/Art')
patterns=['btn_*.png','*按钮*.png','坐标点_*.png','小地图*.png','聊天频道图示*.png','表情符号*.png']
files=[]
for pat in patterns:
    files += list(root.glob(pat))
seen=set()
for p in files:
    if p in seen: continue
    seen.add(p)
    im=Image.open(p)
    im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(p)
print('flipped', len(seen), 'icon pngs')
PY
```

## Top status bars: exact PC behavior

PC logic from `UiShell.cpp`:

- Life: `Set2IntValue(nLife, nLifeFull)` then `Set2IntText(nLife, nLifeFull, '/')`
- Mana: `Set2IntValue(nMana, nManaFull)` then `Set2IntText(nMana, nManaFull, '/')`
- Stamina: `Set2IntValue(nStamina, nStaminaFull)` then `Set2IntText(nStamina, nStaminaFull, '/')`
- EXP: compute percent, clamp `0..100`, `Set2IntValue(np, 100)`, text `"%d%%"`

PC visual from `顶部控制条.ini`:

- Background: `\Spr\Ui3\主界面\新血条面板.spr`
- Life image: `生命条.spr`
- Mana image: `内力条.spr`
- Stamina image: `体力条.spr`
- Exp image: `经验条.spr`

`KWndImagePart.SetPart()` clips the image according to percent. It does **not** scale the
image into a shorter width. In Unity UI Toolkit, mimic this by:

- Track has fixed width/height and `overflow: hidden`
- Fill element width is percentage
- Fill background image has fixed background size equal to the original bar image

In current Unity:

- `bar_panel_bg.png`: 552x17
- bar fill tracks: 104x9
- positions within panel:
  - EXP x=58
  - HP x=170
  - MP x=282
  - Stamina x=394

## Joystick safety rule

The mobile joystick is uGUI and must remain touchable.

Common failure: a full-screen UIDocument or bottom strip steals pointer events, so the
joystick stops moving even if it looks uncovered.

Fix in `GameHudController.BindElements()`:

```csharp
doc.rootVisualElement.pickingMode = PickingMode.Ignore;
root.pickingMode = PickingMode.Ignore;
foreach (var child in root.Children())
    child.pickingMode = PickingMode.Ignore;
```

Then only re-enable pick on actual HUD buttons:

```csharp
el.pickingMode = PickingMode.Position;
el.RegisterCallback<PointerDownEvent>(_ => cb());
```

Also keep bottom HUD panels physically away from the joystick. In the current layout,
left chat/hotbar content starts at `x >= 155` to leave the joystick lane free.

## Minimap fixes

PC minimap buttons live inside the lower-right of the minimap frame, not below it.

Current USS target:

```css
.hud-minimap-btns {
    position: absolute;
    left: 100px;
    top: 116px;
    width: 32px;
    height: 14px;
}
```

Use PC sprites only:

- `btn_worldmap.png`
- `小地图－世界大地图按钮_01.png`
- `minimap_dot.png`

## Action buttons

Do not remove action buttons just to avoid overlap. Users expect:

- run/walk
- sit
- mount/dismount horse
- trade/exchange

If they collide with joystick, move them to the right side above the main menu cluster.
Current layout places `ToolbarLeft` at bottom-right above menu:

```css
.hud-left-tools {
    position: absolute;
    right: 8px;
    bottom: 58px;
}
```

Known source gap: `工具控制条.ini` references `跑步.spr`, but this file was not present in
the checked-in `Ui3/主界面/按钮条按钮` folder. Do not invent a run icon. Leave it blank or
search paks/source archives for the missing SPR.

## Vietnamese text

Vietnamese labels should be localized, but art must remain PC-derived. Use text overlay
for labels/values, not homemade image art.

Current overlay:

- `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`

It draws:

- `Cấp`
- `Kinh nghiệm`
- `Sinh lực`
- `Nội lực`
- `Thể lực`
- chat warning: `!! Hãy sử dụng hồi phục`
- menu labels: `Nhân`, `Túi`, `Võ`, `Đội`, `Bang`, `PK`

If changing positions, keep a 1280x720 reference coordinate system and scale by
`Screen.width / 1280f`, `Screen.height / 720f`.

## Combat Slots and Click Responsiveness

When porting or fixing the combat HUD quick slots:

- **Touch Responsiveness (độ nhạy phím bấm)**: To achieve instant, snappy skill casting without any lag or click ignoring, the long-press (chạm giữ) mechanism for opening the skill picker on assigned slots should be **completely removed**.
- **Alternative Picker Flow**: Opening the skill picker is done by clicking/tapping on an empty slot directly. Assigned slots are strictly dedicated to instant casting on tap, or aiming drag on drag.
- **Implementation**: Do not use any long-press timer coroutine (`OpenPickerAfterLongPress`), `_longPressCoroutine`, or `_longPressOpened` flags. Touch down immediately captures the pointer, touch move processes aiming drag, and touch up immediately triggers the skill or opens the picker (only if the slot is empty).

## Verification checklist

After any HUD edit:

1. Refresh Unity and compile.
2. Enter play mode.
3. Capture a Game View screenshot with `unityMCP_manage_camera screenshot`.
4. Check:
   - no compile errors
   - HP/MP/Stamina/EXP use PC sprite fills and correct clipping
   - minimap button is inside the minimap frame
   - icons are not upside-down
   - joystick is not visually covered and UI Toolkit does not steal pointer events
   - action buttons exist (sit/horse/trade; run if sprite found)
   - Vietnamese text is visible and not clipped
5. Save `Assets/Scenes/Sandbox.unity` when verified.

## Don't do these

- Do not create new art for missing PC sprites unless the user explicitly accepts a placeholder.
- Do not “modernize” the HUD if the task says PC/VLTK fidelity.
- Do not stretch bar fills as a replacement for PC clipping.
- Do not put UI Toolkit panels over the joystick hit area.
- Do not rely only on file edits; always verify with Unity screenshot.
