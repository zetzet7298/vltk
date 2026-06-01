# US-BALANG-ENEMY-SPAWN-PC-PARITY Ba Lăng enemy spawn PC parity

## Status

validated

## Lane

normal

## Product Contract

Map Ba Lăng huyện must show PC-derived outside-town enemies from original JX `Region_S.dat` server NPC sections. Spawns must use PC MPS coordinates so each enemy appears at the same map position as PC data. Spawned enemies must use Vietnamese display names, show elemental prefix before the name, show current/max HP text under the name, and show a health bar under that text. Runtime AI should follow PC fields as closely as practical in sandbox: static training objects stay still; animals wander using `NpcS.txt` AIMode/AIParam/WalkSpeed and remain near spawn/origin active radius.

## Relevant Product Docs

- `docs/ARCHITECTURE.md`
- `docs/stories/US-M17-001` matrix row: NPC/Object Spawn Table
- `docs/stories/US-M31-001` matrix row: NPC Template Registry
- `docs/stories/US-M32-001` matrix row: NPC Spawn in Sandbox
- PC source refs:
  - `jxwin-kinnox/SourceNew/swrod3/bin/Server/Settings/NpcS.txt`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/KNpcAI.cpp`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/Scene/SceneDataDef.h`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/KRegion.cpp`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/Core/Src/KNpcSet.cpp`
  - `Assets/StreamingAssets/TestData/Regions/Map_79/*_Region_S.dat`

## Acceptance Criteria

- Ba Lăng map loads real PC server NPC section entries from `Map_79/*_Region_S.dat`.
- Runtime spawns kind=0 enemies/training objects only and skips town NPC kind=3.
- PC enemy roster for Ba Lăng is present:
  - `31` 金猫 → `Mèo vàng` (`102` spawns)
  - `42` 梅花鹿 → `Hươu sao` (`193` spawns)
  - `43` 白猪 → `Heo trắng` (`189` spawns)
  - `413/414/415` training objects (`30` spawns total)
- Total live enemy/object spawn count: `514` from `539` Region_S NPC entries.
- Each spawn uses PC MPS `(x,y)` and converts to Unity world using Region_S/MapRenderer parity:
  - `regionCol = mpsX / 512`
  - `regionRow = mpsY / 1024`
  - `worldX = mpsX`
  - `worldY = -(mpsY - regionRow * 512)`
- Enemy names are Vietnamese and prefixed by ngũ hành text, e.g. `Mộc hệ Heo trắng`.
- Each live enemy has 3 visible UI layers:
  1. elemental prefix + Vietnamese name,
  2. HP text `current/max` such as `100/100`,
  3. health bar.
- HP supports current/max values; changing current HP updates text and bar.
- AI data comes from PC `NpcS.txt` fields: Kind, Series, NpcResType, WalkSpeed, RunSpeed, VisionRadius, ActiveRadius, AIMode, AIParam1..9.
- Sandbox AI mirrors PC behavior enough for this slice: static AIMode=0 stays still; AIMode 1/4/6 periodically chooses wander targets from AIParam distance/angle and clamps to active radius around origin.
- Unity compile has zero errors; targeted EditMode tests pass; PlayMode probe confirms enemy count, Vietnamese labels, HP UI, AI movement, and screenshot evidence.

## Design Notes

- `Region_S.dat` is authoritative for server-side NPC spawns. Old `Region_C.dat` critter-only path was wrong for outside-town combat enemies.
- Parser follows PC structs:
  - combined file header: `DWORD sectionCount` + `KCombinFileSection[]`
  - NPC section index `2`
  - `KNpcFileHead`
  - variable-length `KSPNpc` records with 60-byte fixed prefix plus script string.
- `KSPNpc.nPositionX/Y` are PC MPS coordinates. PC code path: `KRegion::LoadServerNpc()` → `NpcSet.Add(nSubWorld, &sNpcCell)` → `KSubWorld::Mps2Map()`.
- Current runtime still uses simple placeholder body sprites because real `ani048/ani060/ani061/enemy177/enemy178/enemy179` SPRs are not staged yet. Spawn data/name/AI/HP UI are PC-derived; replace bodies when real NPC SPR extraction lands.
- Screen-space `BaLangEnemyNameplateOverlay` is kept so labels remain readable over dense map art.

## Validation

| Layer | Proof |
| --- | --- |
| Compile | Unity compile/import, console errors `0`. |
| Unit | EditMode `VLTK.Tests.Sandbox.BaLangEnemyTests`: job `f22961c7f73e474eb455f42d87281e7b`, `8/8` passed. |
| Parser | Region_S scanner parses `539` NPC entries and filters `514` kind=0 enemy/object spawns. |
| E2E | PlayMode probe: `activeMap=79 enemies=514 live=514 vi=514 three=514 hp=514 moving=484 overlay=True cat=102 deer=193 pig=189 sample=Mộc hệ Heo trắng 100/100 @(57747, -53000, 0)`. |
| Platform | Screenshot: `Assets/Screenshots/balang-region-s-enemy-spawns-pc-coords.png`. |

## Harness Delta

Story moved from client `Region_C` critter extraction to server `Region_S` authoritative PC spawn extraction. If this pattern repeats for more maps, create reusable `jx-enemy-port` skill or expand `jx-map-port` with Region_S NPC parsing + MPS coordinate conversion.

## Evidence

- Parser implementation: `Assets/Scripts/Sandbox/BaLangEnemyRegionScanner.cs`
- PC enemy database + coordinate transform: `Assets/Scripts/Sandbox/BaLangEnemyDatabase.cs`
- Runtime spawn/AI/nameplate: `Assets/Scripts/Sandbox/BaLangEnemyRuntime.cs`
- Screen-space labels: `Assets/Scripts/Sandbox/BaLangEnemyNameplateOverlay.cs`
- Sandbox integration: `Assets/Scripts/Sandbox/SandboxManager.cs`
- Tests: `Assets/Tests/EditMode/Sandbox/BaLangEnemyTests.cs`
- Exported audit data: `Assets/StreamingAssets/TestData/balang_npc_spawns_pc.json`
- Test job: `f22961c7f73e474eb455f42d87281e7b` (`8/8` passed)
- PlayMode probe: `activeMap=79 enemies=514 live=514 vi=514 three=514 hp=514 moving=484 overlay=True cat=102 deer=193 pig=189`
- Screenshot: `Assets/Screenshots/balang-region-s-enemy-spawns-pc-coords.png`
