---
name: jx-hud-port
description: >-
  Port, fix, or visually match JX Online 1 / Võ Lâm Truyền Kỳ PC HUD/UI in
  VLTK-mobile using real PC PAK SPR art and INI coordinates, with pc_hud.png as
  the pixel arbiter. Use when HUD/UI should look like PC; elements are
  placeholder, overlapping, flipped, misaligned, missing, blocking the joystick;
  or mentions HP/MP/EXP/stamina bars, minimap, chat, hotbar, action buttons,
  inventory/skill/team/faction buttons, top/bottom bars, jade frame, Bảo Vật,
  Ui3, 顶部控制条, 工具控制条, 主界面玩家信息窗口, jx1024.spr, or PC sprites. Enforce
  verified corrections: UI art is in Client 6.0/data/*.pak, not Utility/Run/Ui/ui3;
  no C++ source exists in this dump; extract/key/composite SPRs instead of
  screenshot crops; keep Vietnamese text; verify against PC source, pc_hud.png,
  and live Unity runtime hit-testing because this skill can go stale.
---

# JX / VLTK PC HUD Porting

Use this skill to make the Unity mobile HUD visually match the PC client as closely as
possible. Do not redesign the HUD from memory. Treat PC INI + PC SPR as source of truth.

This skill is a checklist, **not** source of truth. Before editing, re-open the current
Unity files and the PC data. If a statement here conflicts with live source/runtime,
trust PC source + current Unity code + runtime probes, then update this skill.

## First principle


## Resource/hash guard learned from combat visual port

Before concluding that any PC SPR/icon/effect/NPC/HUD asset is missing, apply `jx-pc-port-rule` → **PC resource resolution doctrine**:

- Read PC TXT/INI tables with the correct encoding. Paths with Chinese resource folders are usually GB2312/GBK; mojibake paths hash to fake UIDs.
- PAK entries named `unknown/<uid>.spr` are valid extracted PC assets, not garbage.
- For PAK lookup use PC signed-byte FileNameHash, not an unsigned-byte/private runtime hash.
- Copy exact PC assets into `Assets/StreamingAssets/...`; never load directly from `/var/www/vltksource_new` at runtime.
- Verify with real file existence/decode/render evidence before claiming parity or missing source.

PC HUD is **data-driven**, but the source layout the original version of this
skill assumed (`Utility/Run/Ui/ui3/`) **does not exist** in `vltksource_new`. Do
not trust that path. Verified reality (this is the hard-won correction):

- INI + SPR art live in the canonical unpacked PAK source tree first: `/var/www/vltksource_new/vl_update_27/pak_unpacked/vl_update_27/Client 6.0/data/<pak>/...`
  (notably `1024/` for 1024-res UI, `updatejx08/` for patched art, `800/` for 800-res).
- Use `/var/www/vltktool/unpak_tool.py` only for exceptional repair/re-unpack cases; normal HUD porting should read the already-unpacked files.
- Loose INI fragments exist under `Client 6.0/ui` and Lua under `Client 6.0/script/ui`,
  but they are partial. The top-bar INI `顶部控制条.ini` is loose; the bottom-bar INIs
  are inside the PAKs.
- **There is no C++ source** (`UiShell.cpp` etc. do not exist in this dump). Do not
  cite C++ runtime logic as if you read it — infer behavior from INI + the live PC
  render `pc-evidence/pc_hud.png` instead.

When a user says “giống PC 100%”, your source of truth is, in order: (1) the actual
pixels of `pc_hud.png`, (2) INI coordinates extracted from PAKs, (3) the real SPR art.
Never redesign from memory and never trust a vision model over the raw pixels (see
“Pixel-truth verification” below — this is the single biggest time-sink if ignored).

## Key PC sources (verified locations)

| Source | Where it really is | Purpose |
|---|---|---|
| `顶部控制条.ini` | loose under `Client 6.0/ui` | HP/MP/Stamina/EXP/Level top status bar |
| `工具控制条` (uid `dc11ac12`) | inside `1024.pak` | run/sit/horse/trade/status/items/skills/team/faction/PK button slots |
| `主界面玩家信息窗口` (uid `e3b06434`) | inside `1024.pak` | main HUD / bottom info window layout |
| `pc-evidence/pc_hud.png` | in the Unity repo root | **the live PC HUD render — the final arbiter of layout/color** |
| `Assets/Scripts/UI/HudBottomBarPcSpec.cs` | Unity repo | INI-derived bottom-bar slot coordinates, already ported |

To find an INI/SPR by name inside a PAK, hash the GBK-encoded path to a uid and match
it against the PAK index (see “PAK / SPR extraction workflow”). Filenames in the dump
are often GBK-mangled (`锥斤拷` = U+FFFD lost on extract), so prefer uid matching or
byte-safe header scanning over relying on readable filenames.

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
- `Assets/Scripts/UI/HudDataService.cs` — data bridge between runtime state and HUD
- `Assets/Scripts/UI/HudUserFacingArtCatalog.cs` — PC art path → Unity texture lookup
- `Assets/Scripts/UI/HudBottomBarPcSpec.cs` — PC `工具控制条` bottom-button rects
- `Assets/Scripts/UI/InventoryWindowPcSpec.cs` — PC inventory window + mobile 4×7 override
- `Assets/UI/HUD/Textures/` — alternative bar textures (`tex_hp_bar.png` etc)
- `Assets/UI/HUD/HudTheme.tss` — UI Toolkit theme

The HUD uses UI Toolkit for sprites/panels and an IMGUI overlay for Vietnamese text.
That overlay exists because UI Toolkit text was unreliable without full runtime text
settings in this project. Keep art in UI Toolkit; only use IMGUI/uGUI for text if needed.

## PAK / SPR extraction workflow

The art you need is usually already available in the canonical unpacked PAK tree. First read
`/var/www/vltksource_new/vl_update_27/pak_unpacked/_SOURCE_OF_TRUTH.txt` and `/var/www/vltktool/README.md`. Do not write ad-hoc
SPR/PAK scanners unless the tool itself needs a surgical enhancement; broad scans can
crash the machine and usually produce false confidence.

Pipeline:

1. **Find the SPR/INI uid.** Build the GBK bytes of the in-game path (e.g.
   `\spr\UI3\主界面\背包按钮.spr`) and hash with `unpak_tool.file_id_from_bytes`. Scan
   each PAK's index for that uid. If the hash misses, use `resolve_uid.py` or a narrow
   `find_spr_by_image.py --pak <one pak>` query; never scan the whole source tree.
2. **Prefer already-unpacked files** under `/var/www/vltksource_new/vl_update_27/pak_unpacked`; only decompress with `/var/www/vltktool/unpak_tool.py` if the canonical tree/manifest proves a repair case is needed.
3. **Decode SPR frames to PNG.** Each SPR holds N frames; use
   `/var/www/vltktool/extract_item_spr.py` to write `*_frame_000.png` etc.
4. **Copy the PNG into the Unity project** under `Assets/UI/HUD/Art/` (and the mirror
   `Assets/StreamingAssets/UI/HUD/Art/` if the catalog reads from there).
5. **Reimport as a Texture2D.** A bare `cp` does NOT register the asset — the USS
   `url()` will fall back to Unity's pink/warn placeholder. Always call
   `unityMCP_manage_asset import` on the new PNG so it gets a `.meta` + TextureImporter.
6. Bind the PNG in `GameHudController.LoadArt()` or USS.

### Authentic icon UID catalog (extracted from `1024.pak`, verified by vision)

These are the real PC menu-button SPRs the current bottom bar composites. Reuse these
uids instead of re-deriving — the project's pre-existing `btn_*.png` files were
**20×20 placeholders from an old port commit, NOT PC art**:

| uid (frame_000) | depicts | menu function |
|---|---|---|
| `b5b2ef55` | character bust | Status / 人物属性 |
| `3d42308f` | chest / bag | Items / 背包 |
| `56d016e0` | clenched fist | Skills / 技能 |
| `04e4c9e4` | quest scroll | Task / 任务 |
| `823193a1` | two people | Team / 队伍 |
| `46c617c6` | open mouth | ChatRoom / 聊天室 |
| `00a58516` | system scroll | Options / 系统 |
| `1023e503` | banner | Faction / sub-bag |
| `63ba885e` | sword on flame | PK |
| `abd810d5` | blue map | minimap world-map toggle |
| `8f845112` (updatejx08.pak, 74×61) | orange “Bảo Vật” treasure button | far-right treasure button |

White SPR backgrounds (e.g. the Bảo Vật button) need keying: set pixels with
r,g,b > ~235 to alpha 0 before compositing.

Important gotcha: UI SPR frames were sometimes observed vertically flipped when decoded
with the initial script. For button/icon PNGs, flip top-bottom if inspection shows them
inverted. Do **not** flip bar fill PNGs unless visual inspection proves they are inverted.

## Top status bars: exact PC behavior

Value/percent behavior (inferred from INI bar definitions — there is no C++ to read):

- Life / Mana / Stamina: show `cur/full` text centered under each bar.
- EXP: percent clamped `0..100`, text `"%d%%"`.

PC visual references (`顶部控制条.ini`):

- Background panel: `新血条面板.spr`
- Life `生命条.spr`, Mana `内力条.spr`, Stamina `体力条.spr`, Exp `经验条.spr`

`KWndImagePart.SetPart()` clips the image by percent — it does **not** scale the image
into a shorter width. In Unity UI Toolkit, mimic this by:

- Track has fixed width/height and `overflow: hidden`
- Fill element width is percentage
- Fill background image has fixed background size equal to the original bar image

### Verified PC-pixel bar positions (this is the correction)

The original skill's coords (EXP x=58 / HP x=170 / MP x=282 / Stamina x=394 in a
552px panel) were **~200px off** from the real render. The fix that matched
`pc_hud.png`: measure the four bars directly in `pc_hud.png` (802px wide), then scale
×1.6 to the mobile 1280 reference. Result:

| bar | x (1280 ref) |
|---|---|
| Stamina | 289 |
| HP | 465 |
| MP | 641 |
| EXP | 816 |

Also verified against the actual pixels: the PC top bar frame is **flat with thin
double-line borders** — NOT an ornate metallic frame. An earlier `top_frame_bg.png`
with an upside-down label and four spurious EXP cells was wrong and was removed. Use a
flat dark PC-style frame, dark-green level/rank boxes, bar height ~16px (so the value
text sits below like PC), and a far-left connection-status text (e.g. “Hoạt động tốt 97”).

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

## Runtime UI Toolkit traps

Do not assume a registered callback proves a button is clickable. Reproduce pointer
issues in Play Mode:

1. Locate the live `UIDocument`, then query the element (`BtnItems`, `ToolbarRight`,
   `InventoryWindow`, etc.).
2. Inspect `pickingMode`, `resolvedStyle.display`, and `worldBound` for the element,
   its parents, and likely overlay siblings.
3. Call `panel.Pick(centerOfVisiblePcIcon)` and verify the picked element is the one
   with the intended handler.
4. If the visible icon is baked into art, use an invisible proxy at the PC INI rect
   instead of trusting a flex child that can be elsewhere.

UI Toolkit/UIDocument can recreate the visual tree across domain reload/play-mode
transitions. Controllers that cache `VisualElement` references must detect stale trees
and rebind (`ReferenceEquals(currentRoot, cachedRoot)`, check required elements/proxies).
Manual calls like `OpenInventory()` can pass while real clicks fail if the hitbox or
cached tree is stale.

## Minimap fixes

Pixel-truth finding: in `pc_hud.png` the minimap frame is a **thin ~1px dark border**
(scan shows edges at x=672 and x=800 with map content between) — NOT an ornate
wooden/gold bezel. A vision model will repeatedly claim it needs an ornate frame;
that is a hallucination against an imagined classic HUD, not the real render. Match
the real thing: a square map with a thin dark border, zone name above, coords below,
and a small row of utility buttons.

The two utility buttons below the map use authentic SPRs (blue world-map `abd810d5`
+ a worldmap icon). Bind them in USS by id, e.g.:

```css
#ToggleMapBtn { background-image: url('project://database/Assets/UI/HUD/Art/btn_minimap_toggle.png'); }
#WorldMapBtn  { background-image: url('project://database/Assets/UI/HUD/Art/btn_worldmap.png'); }
```

Reimport gotcha (cost real time): if the button shows a yellow warning-triangle, the
PNG was copied but never imported — run `unityMCP_manage_asset import` on it so it
becomes a Texture2D the `url()` can resolve.

## Action buttons

Do not remove action buttons just to avoid overlap. Users expect:

- run/walk
- sit
- mount/dismount horse
- trade/exchange

If they collide with joystick, keep them in the PC bottom-right lane (`ToolbarRight`),
not over the joystick. Current UXML uses one row:
`run/sit/horse/exchange | status/items/skills/team/faction/PK | Bảo Vật`.

Known source gap: the action-row SPRs (run/sit/horse/exchange) were not found in the
scanned PAK manifests. When exact SPR proof is absent, the only accepted fallback is an
explicitly documented PC screenshot crop from `pc_hud.png` for that action icon. Do not
invent replacement art.

## Bottom bar: SPR truth + hitboxes

The bottom bar may be shipped in Unity as a precomposited `bottom_bar_bg.png`, but that
file is only an implementation artifact. The source of truth is still PC SPR + INI; do
not resize/reposition by eyeballing the Unity PNG.

Verified composition:

- Base art = the real PC bar SPR `jx1024.spr` (uid `917565dd`, `1024.pak`), band
  `y[680..768]` = 1024×89. This is the authentic jade frame + hotbar + T/P slots.
- Menu icons = the individual button SPRs in the UID catalog above, composited onto
  the band at the slot x-positions defined by INI `工具控制条` (uid `dc11ac12`).
- Far-right treasure button = orange Bảo Vật SPR `8f845112` (updatejx08.pak), white
  background keyed to transparent, composited on top.
- The INI-derived slot coordinates are already ported in
  `Assets/Scripts/UI/HudBottomBarPcSpec.cs`, guarded by
  `Assets/Tests/EditMode/Sandbox/HudBottomBarAuthenticityTests.cs`. Update the spec +
  tests together when slots change.

Runtime lesson: visible baked pixels and UI Toolkit click elements can drift apart.
Before blaming business logic, compare `panel.Pick()` at the **visible PC icon center**
against the expected element. If the baked icon is in `bottom_bar_bg.png`, place an
invisible `PickingMode.Position` proxy at the matching `HudBottomBarPcSpec` rect and
route it to the PC handler. Example: inventory uses `[Items]` left=611 top=728 width=28
height=28 in 1024×768 PC coords; Unity scales that over the 1280×82 bottom strip and
routes to `OnItemsClick()` / `Open([[items]])`.

## Inventory / Túi đồ specifics

PC click path:

- `1024.pak` uid `dc11ac12`, section `[Items]`, `ClassType=Player_Items`, icon
  `\spr\UI3\主界面\背包按钮.spr`.
- `Client 6.0/data/1024/Ui/autoexec.lua`: `F4 -> Open([[items]])`.
- Real window uid `05ea8560` (`道具界面`), not the storage-box window:
  `[Main]` 214×474, background `\spr\Ui3\道具\daojumianban.spr`, `[ItemBox]`
  left=24 top=72 width=168 height=280, `HUnits=6`, `VUnits=10`, `UnitBorder=1`.

Mobile has an intentional user override: render/cap inventory to **4 columns × 7 rows**
(28 slots) while preserving the PC 6×10 provenance in `InventoryWindowPcSpec`. When a
user reports “bấm Túi đồ không hiện”, first verify the hitbox/proxy at the baked PC icon;
then verify `OpenInventory()` renders `InventoryWindow` with 28 slots.

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

## Pixel-truth verification (read this before trusting any score)

The biggest time-sink in HUD work is trusting a vision model over the raw pixels. In
this project a vision grader scored the *same unchanged* top bar 95, then 50, then 45,
and kept demanding “ornate metallic/wooden frames” that **do not exist** in
`pc_hud.png`. Two root causes:

1. Stacked side-by-side comparisons **downscale** the 1920-wide mobile capture ~2.4×,
   destroying icon detail and making the model hallucinate missing/blurry elements.
2. The model compares against an imagined “classic MMO HUD” rather than the actual
   reference image.

Defend against this:

- **Settle layout/frame questions with a pixel scan of `pc_hud.png`, not a vision
  opinion.** Read a row/column of pixels and look for real edges (large brightness
  deltas). That is how the minimap “thin 1px border” and the bar x-positions were
  established.
- **Verify at native resolution.** Crop the mobile capture region (top bar, minimap,
  bottom bar) at full res and inspect that — do not judge from the downscaled stack.
- **Ignore non-chrome differences** when scoring: game-state values (level/rank/hp
  numbers, enemy nameplates, player position), world terrain, and the mobile joystick
  (a mobile necessity). They are not HUD-chrome defects.

## Verification checklist

After any HUD edit:

1. Refresh Unity and compile.
2. Enter play mode.
3. Capture a Game View screenshot with `unityMCP_manage_camera screenshot`.
4. Check:
   - no compile errors
   - `panel.Pick()` at each visible PC icon center hits the expected element/proxy
   - manual controller call (e.g. `OpenInventory()`) and real pointer dispatch both work
   - HP/MP/Stamina/EXP use PC sprite fills and correct clipping
   - minimap button is inside the minimap frame
   - icons are not upside-down
   - joystick is not visually covered and UI Toolkit does not steal pointer events
   - action buttons exist (sit/horse/trade; run if sprite found)
   - Vietnamese text is visible and not clipped
5. Save `Assets/Scenes/Sandbox.unity` only if this task intentionally changed the scene.

## Don't do these

- Do not create new art for missing PC sprites unless the user explicitly accepts a placeholder.
- Do not “modernize” the HUD if the task says PC/VLTK fidelity.
- Do not stretch bar fills as a replacement for PC clipping.
- Do not put UI Toolkit panels over the joystick hit area.
- Do not rely only on file edits; always verify with Unity screenshot.
