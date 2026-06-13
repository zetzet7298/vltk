# PlayMode Boot Verification — Evidence 2026-06-13

## Environment

- Unity 6000.4.7f1, URP active
- Branch: dev, HEAD: `968c9cbd8`
- Profile: vltkmobile, Editor running as oracle
- Key change: `useFastEditorBoot = false` (was `true`)

## Boot sequence (Full profile, map 53 Ba Lăng huyện)

Total boot: 31192ms. Key milestones:

```
[Sandbox] Game/Camera/UI/World/Debug/Services root ready     ← subsystems
[Gameplay] Player 'Cái Bang Đệ Tử' Lv1 đã đăng ký            ← combat player
[SandboxBoot] BootstrapCombatRuntime: 122ms
[MapCatalog] Loaded 158 maps (57 outdoor, 101 indoor, 95524 regions)
[MapManager] Generated visual map catalogs merged: +847 maps, geometries=332, serverRegions=330
[PcMapBatch] Total 4484 entries: maps=1005 caves=369 tongs=33 waypoints=225 scrolls=2600 wharves=11 revive=241
[MapManager] Catalog ready: 1005 maps
[Sandbox] Regions: 64031 loaded
[MalePlayer] Loaded spr\npcres\man\MA_YY_999_ST01.spr: 120 frames, 8 dirs  ← player visual (8 parts)
[HorseVisual] Loaded spr\item\equip\horse\horse005.spr: 46x73
[Mount] Mounting horse type 5
[ItemImport] Gate PASS: 11627 items, 0 stubs                   ← item catalog
[SandboxBoot] PcItemBatchLoader.ImportInto: 1209ms
[... 54 streaming services auto-loaded ...]
[MapManager] Loaded map: Ba Lăng huyện (id=53)
[MapRenderer] Found 618 region files in g_1bbe240c72569d69
[MapRenderer] Rendered 618 regions; focus center=(50432,-50688) size=(16896,10240)
[MapEnemy] Map 53: spawned 812 enemies from 845 PC Region_S entries
[MapObject] Map 53: rendered=11, skipped=0, missingVisual=0
[MapTrap] Map 53: active=140, disabled=0, missingScript=0
[TrainingNpcSpawner] Spawned 5 training NPCs in pentagon at center=(53246,-52041), radius=300
[Sandbox] Camera configured for player-follow map view
[SandboxBoot] profile=Full, total=31192ms
[Perf] Runtime memory 2174MB vượt budget 200MB
```

## FastEditor boot (the BAD path) — for comparison

When `useFastEditorBoot = true`, the same scene boots with:

```
[SandboxBoot] FastEditor: skipped item/drop/skill loading.
[SandboxBoot] FastEditor boot: skipped optional StreamingAssets service batches.
[MapRenderer] Bounds applied without visuals: map=53, bounds=(16896,10240)
[SandboxBoot] FastEditor: skipped map visual rendering.
```

Result: flat 2-color void, no terrain, no NPCs, no items. Player floats in empty space.

## NPC visual evidence (SPR decode working in PlayMode)

```
[PcNpcVisual] Loaded spr\npcres\man\MA_YY_999_ST01.spr: 120 frames, 8 dirs      ← human NPC stand
[PcNpcVisual] Loaded spr\npcres\man\MA_YY_999_RN01.spr: 88 frames, 8 dirs       ← human NPC run
[PcNpcVisual] Loaded spr\npcres\enemy\enemy180\enemy180_st.spr: 1 frames, 1 dirs ← static enemy
[PcNpcVisual] Loaded spr\npcres\animal\ani061\ani061_st.spr: 112 frames, 8 dirs  ← animal stand
[PcNpcVisual] Loaded spr\npcres\animal\ani061\ani061_wlk.spr: 96 frames, 8 dirs  ← animal walk
[PcNpcVisual] Loaded spr\npcres\animal\ani063\ani063_st.spr: 120 frames, 8 dirs
[PcNpcVisual] Loaded spr\npcres\animal\ani063\ani063_wlk.spr: 64 frames, 8 dirs
[PcNpcVisual] Loaded spr\npcres\animal\ani049\ani049_st.spr: 64 frames, 8 dirs
[PcNpcVisual] Loaded spr\npcres\animal\ani049\ani049_wlk.spr: 88 frames, 8 dirs
```

## Screenshot verification

After full boot, `manage_camera(action="screenshot")` shows:
- Brown/green terrain tiles with seamless transitions
- Trees and shrubbery rendered correctly
- Stone walls/ruins visible
- Soft shadows under objects and player horse
- Player centered on white/gold horse, silver armor, white hair
- Training dummies (Cọc gỗ, Mộc nhân, Bao cát) rendered with Vietnamese labels
- HUD: HP/MP/Stamina bars, minimap "Ba Lăng huyện", joystick, chat, skill buttons

## Known issues

1. **812 enemies spawned but not visible near player** — they spawn from Region_S coordinates across the full map; viewport shows only a small area around the player spawn point. Zoom-out or camera pan needed to see them.
2. **Memory 2174MB** — 10x the 200MB mobile budget. Major optimization needed for device deployment.
3. **Boot 31s** — acceptable for Editor testing, too slow for mobile.
4. **1 SPR not staged**: `MA_LW_000_ST05.spr` — cosmetic only, doesn't block gameplay.
5. **Combat execution untested** — `GameplayLoopService` and `LootDropService` exist but haven't been exercised in PlayMode yet.
