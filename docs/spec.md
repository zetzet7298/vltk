# VLTK Mobile — Full Specification

**Version**: 1.1  
**Created**: 2026-05-30  
**Updated**: 2026-05-30  
**Project**: Port Võ Lâm Truyền Kỳ / JX Online 3 từ PC sang Unity Mobile  
**Primary strategy**: Sandbox-first · Data-model-first · Map-first  
**Unity Version**: Unity 6 LTS line, currently documented as 6000.4.7f1 in ADR-0006  
**PC Source Reference**: `/var/www/vltk-mobile/jxwin-kinnox`  
**Tool Suite**: `/var/www/vltktool`  
**Primary research tools**: Semble for semantic/source search; GitNexus for code graph when repo is indexed.  

---

## 0. Conventions

- **AC** = Acceptance Criteria.
- **GM** = Game Master / developer debug tooling.
- **Sandbox** = the permanent Unity test scene used for porting validation.
- **PC source** = original C++/DirectX/Lua source under `jxwin-kinnox`.
- **Tool suite** = Python conversion/audit helpers under `/var/www/vltktool`.
- **Canonical model** = Unity-side data shape that preserves PC identity without exposing raw PC binary structs to gameplay code.
- **Source identity** = stable identifier derived from original resource path, UID, package, map id, or config row.
- **Converted artifact** = Unity-ready output such as texture, atlas, ScriptableObject-like metadata, binary blob, JSON manifest, or AssetBundle entry.
- **Given/When/Then** acceptance criteria describe observable behavior, not implementation internals.

---

## 1. Product Scope

### 1.1 Goal

Create a Unity Mobile client that can faithfully port the PC game data and behavior. The first validated target is a sandbox scene capable of loading and inspecting all maps converted from the PC source/data. Gameplay systems are layered on top of the map/data foundation.

### 1.2 First non-negotiable milestone

The first milestone is complete only when a developer can:

1. Open Unity and land in the Sandbox Scene.
2. Press Play and see sandbox initialization logs.
3. Open/close GM Panel from a UI button and shortcut `q`.
4. Select a map from a converted map catalog.
5. Load the map into the sandbox world root.
6. See region bounds, terrain/object layers, obstacle overlay, lighting/weather metadata, and conversion diagnostics.
7. Switch to another map without editing scenes.
8. Inspect missing or invalid source assets through a report rather than a crash.

### 1.3 Source-of-truth hierarchy

1. User instruction and accepted PRD/spec.
2. PC source and data evidence from `jxwin-kinnox`.
3. Tool output and conversion reports from `/var/www/vltktool`.
4. Unity canonical models and generated manifests.
5. Runtime sandbox behavior and validation evidence.

If these conflict, prefer PC source/data evidence for original behavior, then update PRD/spec with the resolved decision.

---

## 2. Reference Research Protocol

### 2.1 Semble usage

Use Semble for semantic and targeted source searches on `jxwin-kinnox`, for example:

```text
semble search "KScenePlaceRegionC map loading region terrain obstacle" /var/www/vltk-mobile/jxwin-kinnox
semble search "SPR sprite parser KSpr palette frame directions bitmap KCanvas" /var/www/vltk-mobile/jxwin-kinnox
semble search "PAK package archive open read file pack index KPakFile" /var/www/vltk-mobile/jxwin-kinnox
semble search --include-text-files "map list world maps ini region file" /var/www/vltk-mobile/jxwin-kinnox
```

Use Semble for `/var/www/vltktool` when locating existing helpers:

```text
semble search --include-text-files "SPR decoder PAK unpacker map BLH terrain item quality gate Unity scaffold" /var/www/vltktool
```

### 2.2 GitNexus usage

Use GitNexus when the target repo is indexed and visible:

```text
gitnexus query --repo jxwin-kinnox "map loading terrain region obstacle"
gitnexus context --repo jxwin-kinnox --name KScenePlaceRegionC
gitnexus query --repo vltk-mobile "Sandbox GM Panel MapManager AssetRegistry"
```

Current discovery note: GitNexus listed `vltk-mobile`, but did not list `jxwin-kinnox` during the documentation update. Until `jxwin-kinnox` appears in GitNexus, treat Semble evidence as the active source-research path and record the GitNexus indexing gap in task notes.

### 2.3 Evidence anchors already identified

| Domain | PC source/tool evidence |
| --- | --- |
| Scene/region client load | `KScenePlaceRegionC`, `KScenePlaceC`, region load/preload/prerender flow |
| Region combined files | `SceneDataDef` concepts: obstacle, trap, NPC, object, ground, built-in object sections |
| Map/world switching | `KSubWorld::LoadMap`, protocol world sync flow, world/map list INI examples |
| Map metadata | map settings INI with `rect`, `brightness`, `color`, `MapLTRegionIndex`, weather/light sections |
| Obstacle semantics | obstacle flags for walk/fly/jump; little-map obstacle data; region obstacle queries |
| PAK/archive | `KPakData`, `KPakFile`, `KZipList`, package utility code |
| SPR/rendering | `KSpriteMaker`, `KDrawSprite`, `KDrawSpriteAlpha`, represent shell sprite rendering |
| vltktool map helpers | `extract_blh_terrain.py`, `find_blh_terrain_uids.py`, unpacked `maps_pak/*.ini` examples |
| vltktool item helpers | item contract bundle generation, quality gate, golden replay, Unity scaffold generation |

---

## 3. Architecture Overview

### 3.1 Layering

```text
Source Intelligence
  -> Offline Conversion Pipeline
    -> Canonical Data Model + Asset Registry
      -> Sandbox Runtime Systems
        -> GM Panel + Debug Visualization
          -> Mobile Runtime / Production UI
```

### 3.2 Deep modules

| Module | Responsibility | Stable interface idea | Must not do |
| --- | --- | --- | --- |
| Source Intelligence | Record where source behavior came from | source evidence records, research notes, symbol/path anchors | Modify PC source |
| Asset Conversion Pipeline | Convert raw PC resources into Unity-ready artifacts | convert job -> manifest + artifacts + report | Depend on Unity Play Mode |
| Canonical Data Model | Define stable map/sprite/item/NPC/skill shapes | pure serializable data types | Hide source identity |
| Asset Registry | Resolve source ids to Unity artifacts | resolve id/path/type -> artifact handle/status | Decode raw PAK in gameplay loop |
| Map Port Pipeline | Discover, convert, validate maps/regions | map id -> MapDefinition + RegionDefinitions + report | Couple to GM UI |
| Sprite Pipeline | Decode SPR frames/palettes/metadata | source sprite -> atlas/frames/clip metadata | Bake gameplay rules |
| Sandbox Runtime | Boot scene, load systems, coordinate runtime state | initialize, load map, reset, diagnostics | Become production-only logic |
| GM Panel | Developer-facing control surface | command/query tabs, hotkey toggle | Own conversion internals |
| Lua Bridge | Load/run original Lua with safe bindings | script invocation + binding registry + logs | Block M0/M1 map work |
| Validation Harness | Unit/integration/sandbox/golden checks | command/report outputs | Test private implementation details |

### 3.3 Unity/runtime decisions

- Use Unity 6 LTS and C# per ADR-0006.
- Use uGUI for GM Panel and sandbox UI unless a later ADR replaces it.
- Use SpriteRenderer or equivalent 2D rendering for original sprite art.
- Use IL2CPP for Android/iOS production builds.
- Evaluate AssetBundles vs Addressables after conversion manifests stabilize.
- Prefer asynchronous loading for large scenes/assets.
- Use Unity scene list/build settings so Sandbox Scene can be the first dev scene and a build-test scene.

---

## 4. Canonical Data Model

These are conceptual models. Exact C# names can change, but the information must exist.

### 4.1 SourceAssetId

Purpose: preserve the PC identity of every source resource.

Required fields:

- `sourcePath`: original normalized PC path if known.
- `packageName`: source PAK/archive or extracted root if known.
- `uid`: numeric/hash identifier when available.
- `resourceKind`: map, region, terrain, sprite, object, npc, trap, config, item, skill, lua, audio, unknown.
- `encoding`: source text/binary encoding where relevant.
- `discoveryTool`: Semble, GitNexus, vltktool, manual, runtime.
- `evidenceNote`: short explanation of how this id was resolved.

### 4.2 ConversionManifest

Purpose: make offline conversion deterministic and auditable.

Required fields:

- `manifestVersion`.
- `sourceRoot`.
- `conversionTimestamp`.
- `toolVersion` or git commit when available.
- `inputs`: source assets and checksums.
- `outputs`: converted artifacts and checksums.
- `warnings`.
- `errors`.
- `coverage`: counts by resource kind.

### 4.3 AssetRegistryEntry

Purpose: allow runtime lookup by source identity.

Required fields:

- `sourceId`.
- `artifactType`: Texture2D, Sprite, SpriteAtlas, MapDefinition, RegionDefinition, BinaryBlob, AudioClip, TextAsset, Script, Prefab, other.
- `unityAssetPath` or bundle key.
- `bundleName` when packaged.
- `loadMode`: editor direct, Resources, AssetBundle, Addressables, streaming assets, test fixture.
- `status`: available, missing, invalid, pending, deprecated.
- `validationHash`.

### 4.4 MapCatalogEntry

Required fields:

- `mapId`.
- `displayNameRaw` and `displayNameNormalized`.
- `sourceMapPath`.
- `settingSourceId`.
- `worldSetMembership` when known.
- `rect`: source region rectangle.
- `mapLeftTopRegionIndex`.
- `isIndoor`.
- `defaultBrightness`.
- `defaultColor`.
- `weatherProfileId`.
- `lightProfileId`.
- `conversionStatus`.

### 4.5 MapDefinition

Required fields:

- `catalogEntry`.
- `regionGrid`.
- `regionWidthPixels`.
- `regionHeightPixels`.
- `cellWidth` and `cellHeight` if inferred.
- `terrainLayers`.
- `objectLayers`.
- `obstacleGridRefs`.
- `trapRefs`.
- `npcSpawnRefs`.
- `minimapRef`.
- `environmentProfile`.
- `conversionReportRef`.

### 4.6 RegionDefinition

Required fields:

- `mapId`.
- `regionX`, `regionY`.
- `sourceRegionPath` or source section reference.
- `boundsPixels`.
- `connectedRegions`: left, right, up, down, diagonals when known.
- `terrainChunks`.
- `builtInObjects`.
- `dynamicObjects`.
- `obstacleGrid`.
- `traps`.
- `clientNpcs`.
- `loadPriority`.

### 4.7 ObstacleGrid

Required fields:

- `mapId`, `regionX`, `regionY`.
- `width`, `height`.
- `cellToWorldScale`.
- `cells`: compact bytes or bitfields.
- Encoded states: walk blocked, fly blocked, jump blocked, normal/unknown.
- Query contract: `CanWalk`, `CanFly`, `CanJump`, `GetRawFlags`.

### 4.8 SpriteClipDefinition

Required fields:

- `sourceSpriteId`.
- `frameCount`.
- `frameRate` or per-frame duration.
- `directionCount` if applicable.
- `actionName` if applicable.
- `pivot` / reference spot.
- `frameOffsets`.
- `atlasRef`.
- `paletteInfo`.
- `alphaMode`.
- `renderStyle`.
- `validationStatus`.

### 4.9 RuntimeSandboxState

Required fields:

- `activeMapId`.
- `activeRegion`.
- `playerPosition`.
- `cameraState`.
- `timeScale`.
- `weatherOverride`.
- `visibleDebugLayers`.
- `selectedEntity`.
- `lastError`.
- `logFilters`.

---

## 5. Sandbox Scene Contract

## M0.1 — Sandbox Scene Boot

**As a** developer,  
**I want** a single Sandbox Scene that loads by default,  
**So that** every test starts from a stable environment.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Unity project opens | Editor is ready | Sandbox Scene is available and is the first dev/build-test scene. |
| 2 | Sandbox Scene is open | Hierarchy is inspected | Roots exist for Game, Camera, UI, World, Debug, and Services. |
| 3 | Play Mode starts | Sandbox initializes | Console shows a sandbox initialized log with version and timestamp. |
| 4 | A subsystem fails | Error is thrown | Error is logged with subsystem prefix and sandbox stays inspectable where possible. |
| 5 | Play Mode exits | State is saved | GM panel preferences and selected map may persist for next run. |

### Technical Notes

- The scene should remain useful throughout the project, not be deleted after production UI exists.
- Production systems may be tested inside sandbox behind toggles.
- Sandbox may contain placeholder objects for unfinished systems.

---

## M0.2 — GM Panel Toggle

**As a** developer,  
**I want** a GM popup opened by UI button and shortcut `q`,  
**So that** I can inspect and change runtime state quickly.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Sandbox is running | Developer clicks GM button | GM Panel opens. |
| 2 | GM Panel is open | Developer clicks close or GM button again | GM Panel closes. |
| 3 | Sandbox is running | Developer presses `q` | GM Panel toggles open/closed. |
| 4 | Developer is typing in an input field | Developer presses `q` | The typed text is not corrupted; shortcut is ignored or handled safely. |
| 5 | GM Panel opens | UI is shown | Tabs are visible for Overview, Map, Player, World, Assets, Logs, Tools. |

### Required GM tabs for M0

- Overview: runtime status and active systems.
- Map: map list, search, load/unload/switch, coordinate jump.
- Player: placeholder stats, position, speed.
- World: time/weather/lighting overrides.
- Assets: registry lookup and missing assets.
- Logs: filtered console stream.
- Tools: conversion/audit report links.

---

## M0.3 — Source Evidence Ledger

**As a** reverse engineer,  
**I want** each porting claim to carry source evidence,  
**So that** future agents can verify decisions.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | A converter/model is implemented | It maps PC behavior | The implementation note includes PC source/tool evidence. |
| 2 | Semble is used | Search finds relevant source | The task records query intent and source anchors. |
| 3 | GitNexus is unavailable for `jxwin-kinnox` | Agent needs code graph | The task records the index gap instead of pretending graph evidence exists. |
| 4 | GitNexus is available | Agent changes source-derived behavior | The task uses GitNexus context/query for symbol/process evidence. |

---

## M0.4 — vltktool Integration Plan

**As a** tool user,  
**I want** existing Python tools to feed Unity conversion,  
**So that** previous reverse-engineering work is reused.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Tool suite is inspected | A conversion task is planned | Existing scripts are mapped to PAK, SPR, map, item, or scaffold responsibilities. |
| 2 | A tool output is consumed | Unity converter imports it | Manifest records tool name/version and source inputs. |
| 3 | A tool reports invalid assets | Sandbox loads related map | Missing/invalid assets appear in diagnostics, not as silent failures. |
| 4 | A tool is not sufficient | New converter work is needed | Spec notes whether to extend vltktool or implement Unity-side importer. |

---

## M0.5 — Canonical Data Model Foundation

**As a** data engineer,  
**I want** pure data models for source assets, manifests, maps, regions, obstacles, sprites, and registry entries,  
**So that** runtime systems do not depend on raw PC binary layout.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | A converted artifact is registered | Runtime resolves it | Asset Registry returns artifact path/status by source identity. |
| 2 | A missing artifact is requested | Runtime resolves it | Registry returns missing status with diagnostic context. |
| 3 | A map manifest is loaded | Data is parsed | Map catalog entries are available without instantiating Unity GameObjects. |
| 4 | Unit tests run | Data models are tested | Tests validate serialization/deserialization and identity stability. |

---

## M0.6 — Asset Registry

**As a** runtime system,  
**I want** to resolve PC resource IDs to Unity artifacts,  
**So that** map, sprite, NPC, item, and skill systems share one lookup path.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Registry is loaded | Query by source path/uid | Correct artifact entry is returned. |
| 2 | Registry contains duplicate source identity | Validation runs | Duplicate is reported as error or deterministic override. |
| 3 | Registry entry references missing Unity asset | Validation runs | Missing asset is reported with source identity. |
| 4 | Map loads | Map uses resources | MapManager accesses resources through registry rather than hard-coded paths. |

---

## M0.7 — PAK/Resource Lookup Abstraction

**As a** converter author,  
**I want** a PAK/resource lookup abstraction,  
**So that** map and sprite converters can locate source bytes consistently.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Source package exists | Converter requests resource path/uid | Abstraction returns bytes or missing status. |
| 2 | Source uses legacy encoding | Converter normalizes path | Lookup handles configured encoding strategy. |
| 3 | Resource is compressed | Converter reads it | Decompressed bytes are returned or clear error is emitted. |
| 4 | Runtime gameplay runs | It needs asset data | It does not parse large PAK archives in the gameplay loop. |

---

## M0.8 — SPR Parser/Decoder Foundation

**As a** sprite engineer,  
**I want** SPR data decoded into frames and metadata,  
**So that** terrain, object, NPC, and item art can be ported.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Valid SPR input | Decoder runs | Frames, palette/alpha data, dimensions, and metadata are emitted. |
| 2 | Invalid SPR input | Decoder runs | Error report identifies invalid signature/format without crashing batch job. |
| 3 | Frame offsets exist | Clip metadata is generated | Pivot/reference spot and offsets are preserved. |
| 4 | Atlas packing runs | Output is imported | Sprite frames can be loaded through Asset Registry. |

---

## M0.9 — Map Catalog Discovery

**As a** map engineer,  
**I want** to discover all candidate maps from PC settings/world lists,  
**So that** sandbox scope is complete.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | PC data/settings are available | Discovery runs | Map catalog includes every discoverable map id/path. |
| 2 | Map name uses legacy encoding | Discovery normalizes it | Raw and normalized names are preserved. |
| 3 | Map points to missing setting/root | Discovery runs | Catalog entry is marked missing/incomplete. |
| 4 | Discovery finishes | Report is generated | Report includes counts by available/missing/invalid maps. |

---

## M0.10 — Sandbox Placeholder Map Flow

**As a** developer,  
**I want** sandbox map switching to work before full conversion is complete,  
**So that** UI/runtime flow can be tested early.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Placeholder map catalog exists | GM Panel opens Map tab | Placeholder maps are listed. |
| 2 | Developer selects map | Load button clicked | World root is cleared and selected map placeholder is loaded. |
| 3 | Developer switches maps repeatedly | Load completes | No duplicate root objects remain. |
| 4 | Map fails to load | Failure occurs | Error is shown in GM Panel and Logs tab. |

---

## 6. Phase M1 — Map Reverse Engineering and Full Sandbox Port

> **Mục tiêu**: port toàn bộ map vào sandbox trước. M1 chưa cần full combat/item/server. M1 thành công khi map catalog toàn diện, conversion reports rõ ràng, và sandbox có thể load/inspect mọi map ở mức terrain/object/obstacle/environment diagnostics.

## M1.1 — Map Metadata Conversion

**As a** map engineer,  
**I want** PC map settings converted into MapDefinition metadata,  
**So that** Unity preserves map bounds and environment rules.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Map settings include rect | Conversion runs | MapDefinition stores source rect and derived bounds. |
| 2 | Map settings include `MapLTRegionIndex` | Conversion runs | MapDefinition stores top-left region anchor. |
| 3 | Map settings include brightness/color | Conversion runs | Environment profile stores defaults. |
| 4 | Map settings include LIGHT section | Conversion runs | Time-of-day light profile is generated. |
| 5 | Map settings include Weather section | Conversion runs | Weather profile is generated. |
| 6 | Setting is missing/incomplete | Conversion runs | Report records warning/error per missing field. |

---

## M1.2 — Region File Conversion

**As a** map engineer,  
**I want** region-level data converted into canonical regions,  
**So that** Unity can stream and inspect map chunks.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Region combined data exists | Converter runs | RegionDefinition contains sections for ground, built-in objects, obstacle, trap, NPC/object where available. |
| 2 | Separate region files exist | Converter runs | Same canonical RegionDefinition is produced. |
| 3 | Region section is absent | Converter runs | Region reports missing optional/required section correctly. |
| 4 | Region connects to neighbors | Converter runs | Connected region references are preserved where known. |
| 5 | Batch conversion runs | It finishes | Conversion report includes per-region status. |

---

## M1.3 — Terrain Layer Conversion

**As a** map engineer,  
**I want** ground/terrain layers converted into Unity-renderable artifacts,  
**So that** maps visually resemble the PC original.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Ground layer references SPR/bitmap resources | Conversion runs | Terrain sprites/textures are resolved through Asset Registry. |
| 2 | Multiple ground layers exist | Sandbox renders map | Layers appear in correct order. |
| 3 | Terrain asset is missing | Sandbox loads map | Missing tile placeholder/overlay appears and report links source id. |
| 4 | Large map loads | Terrain is rendered | Loading is chunked/streamed without freezing for unacceptable duration. |

---

## M1.4 — Built-in Object and Foreground Layer Conversion

**As a** map engineer,  
**I want** built-in object placement converted,  
**So that** decorations, buildings, and foreground elements match PC maps.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Built-in objects exist | Converter runs | ObjectPlacement entries include source sprite, position, layer, z-order, flags. |
| 2 | Object has foreground behavior | Sandbox renders it | It can draw above the player when appropriate. |
| 3 | Object source sprite missing | Sandbox renders map | Placeholder and diagnostic are shown. |
| 4 | Object count is high | Map loads | Batching/atlas diagnostics report draw-call risk. |

---

## M1.5 — Obstacle Grid Conversion

**As a** gameplay engineer,  
**I want** obstacle data converted into queryable grids,  
**So that** movement/pathfinding can respect PC walkability.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Region obstacle data exists | Conversion runs | ObstacleGrid stores compact cell flags. |
| 2 | Sandbox debug overlay enabled | Map is loaded | Walk-blocked/fly-blocked/jump-blocked cells are visually distinct. |
| 3 | Player placeholder clicks blocked cell | Movement requested | Movement is rejected or pathing avoids blocked cell. |
| 4 | Query uses world coordinate | Runtime calls obstacle query | Correct region/cell is resolved. |
| 5 | Obstacle data missing | Map loads | Grid defaults are explicit and report marks risk. |

---

## M1.6 — Trap and Trigger Region Conversion

**As a** script engineer,  
**I want** trap data converted even before Lua is complete,  
**So that** map script surfaces are visible.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Trap section exists | Converter runs | TrapDefinition entries include bounds, script id/name, trigger type where known. |
| 2 | Debug overlay enabled | Map loads | Trap areas are visible with labels. |
| 3 | Player placeholder enters trap | Sandbox detects overlap | Log event is emitted even if script action is stubbed. |
| 4 | Trap script missing | Validation runs | Missing script is reported. |

---

## M1.7 — NPC/Object Spawn Table Conversion

**As a** gameplay engineer,  
**I want** NPC/object spawn records converted from map data,  
**So that** sandbox can show content placement before full AI exists.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | NPC spawn data exists | Converter runs | NpcSpawn entries include template id, position, region, direction, script reference where known. |
| 2 | GM toggles NPC display | Map is loaded | Placeholder NPC markers appear at spawn points. |
| 3 | NPC template missing | Validation runs | Missing template is reported separately from missing spawn. |
| 4 | Spawn count is high | Sandbox loads map | GM Panel shows count and performance warning if needed. |

---

## M1.8 — Minimap and World Map Data

**As a** map engineer,  
**I want** minimap/world map assets converted,  
**So that** navigation UI can be validated early.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Map has overview image | Conversion runs | Minimap/world map artifact is registered. |
| 2 | GM Panel Map tab shows loaded map | Minimap toggle enabled | Minimap preview appears. |
| 3 | Player placeholder moves | Minimap displayed | Marker position updates in correct scale. |
| 4 | Minimap asset missing | Toggle enabled | Missing state is visible with source id. |

---

## M1.9 — Region Streaming

**As a** runtime engineer,  
**I want** active map regions to stream around the camera/player,  
**So that** large maps are usable on mobile.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Player starts in a region | Map loads | Active region plus configured neighbor ring loads. |
| 2 | Player crosses region boundary | Runtime updates | Neighbor regions load/unload deterministically. |
| 3 | GM overlay enabled | Player moves | Loaded, loading, and unloaded regions are color-coded. |
| 4 | Region load fails | Runtime continues | Failed region is marked and error is logged. |
| 5 | Mobile memory budget is set | Many regions load | Streaming respects max loaded region budget. |

---

## M1.10 — Map Switcher Completeness

**As a** developer,  
**I want** the GM map switcher to handle all converted maps,  
**So that** full map audit can happen inside one scene.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Converted map catalog has many entries | GM Panel opens | Map list supports search, filters, and status badges. |
| 2 | Developer selects converted map | Load clicked | Previous map is unloaded and selected map loads. |
| 3 | Developer selects incomplete map | Load clicked | Sandbox loads available layers and shows missing sections. |
| 4 | Developer chooses random next map | Button clicked | A map is selected from the filtered list. |
| 5 | Batch audit mode starts | It cycles maps | Report records load success/failure and screenshots if available. |

---

## M1.11 — Visual Golden Snapshot Baseline

**As a** QA engineer,  
**I want** reproducible screenshots of converted maps,  
**So that** converter changes can be compared.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | A golden fixture map exists | Snapshot command runs | Image and metadata are saved. |
| 2 | Converter changes | Snapshot command runs again | Difference report is produced. |
| 3 | Difference exceeds tolerance | Validation completes | Report marks visual regression. |
| 4 | Asset intentionally changes | Golden is updated | Update reason is documented. |

---

## M1.12 — Map Conversion Coverage Report

**As a** project owner,  
**I want** a full conversion coverage report,  
**So that** map porting progress is measurable.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Discovery found maps | Conversion report generated | Report lists total maps and statuses. |
| 2 | Each map has regions | Report generated | Counts include regions converted/missing/failed. |
| 3 | Each map references assets | Report generated | Counts include sprites/textures resolved/missing/invalid. |
| 4 | Sandbox loads maps | Report generated | Runtime load statuses are merged or linked. |
| 5 | Report has errors | Developer opens GM Tools tab | Errors can be filtered by map, resource kind, severity. |

---

## 7. Phase M2 — Character, Camera, Movement

## M2.1 — Player Placeholder

**As a** developer, I want a controllable player placeholder, so that map scale, camera, and obstacles can be tested.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Map is loaded | Player spawns | Player appears at configured safe/default position. |
| 2 | Developer clicks walkable map cell | Input is processed | Player moves toward target. |
| 3 | Developer clicks blocked cell | Input is processed | Movement rejects or routes around obstacle. |
| 4 | GM changes speed | Player moves | Movement speed updates immediately. |

---

## M2.2 — Coordinate System Parity

**As a** gameplay engineer, I want PC coordinates mapped to Unity coordinates, so that map, NPC, skill, and obstacle data align.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Source region/cell/pixel coordinate | Conversion runs | Unity world coordinate is deterministic. |
| 2 | Unity coordinate | Debug inspector opens | Equivalent map/region/cell coordinate is shown. |
| 3 | Region boundary crossed | Player moves | Coordinate conversion remains continuous. |

---

## M2.3 — Camera Controller

**As a** developer, I want camera follow/zoom/pan tools, so that maps can be inspected quickly.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Player exists | Camera follow enabled | Camera follows player. |
| 2 | GM unlocks camera | Developer drags/pans | Camera can inspect any map area. |
| 3 | Pinch or mouse wheel used | Zoom changes | Zoom remains within configured min/max. |
| 4 | GM reset camera clicked | Command runs | Camera returns to player/default target. |

---

## M2.4 — Pathfinding Prototype

**As a** gameplay engineer, I want a pathfinding prototype over converted obstacles, so that map movement can be validated.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Obstacle grid exists | Path requested | Path avoids walk-blocked cells. |
| 2 | No valid path exists | Path requested | Failure is logged and shown in GM diagnostics. |
| 3 | Path debug overlay enabled | Path requested | Nodes/segments are drawn. |
| 4 | Region boundary path requested | Path crosses region | Neighbor region obstacles are considered. |

---

## M2.5 — Character Sprite Placeholder to Real Sprite

**As a** sprite engineer, I want the player placeholder to swap to decoded sprite clips, so that animation pipeline can be validated.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Sprite clip is registered | GM selects it | Player renders using decoded frames. |
| 2 | Direction changes | Player moves | Animation direction changes if available. |
| 3 | Frame offset exists | Sprite animates | Pivot/offset remains stable. |
| 4 | Clip missing frames | Clip selected | Diagnostics show incomplete clip. |

---

## 8. Phase M3 — NPC, Lua, Map Scripts

## M3.1 — NPC Template Registry

**As a** gameplay engineer, I want NPC templates derived from PC data, so that spawn points can instantiate meaningful placeholders.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | NPC template config exists | Converter runs | Template registry includes id/name/stats/resource/script refs where known. |
| 2 | Spawn references template | Map loads | Spawn marker resolves template. |
| 3 | Template missing resource | Validation runs | Missing resource is reported. |

---

## M3.2 — NPC Spawn in Sandbox

**As a** developer, I want NPCs to spawn in the sandbox, so that map population can be checked.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Map has NPC spawns | GM toggles NPCs | NPC placeholders appear. |
| 2 | NPC sprite available | Spawn renders | NPC uses decoded sprite/animation. |
| 3 | NPC clicked | Inspector opens | Source template/spawn/script ids are shown. |
| 4 | GM despawn clicked | Command runs | NPCs are removed without reloading map. |

---

## M3.3 — Lua Script Loader

**As a** script engineer, I want Lua scripts to load through a controlled bridge, so that original map/NPC scripts can be tested.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Lua script path is registered | Loader runs | Script loads or reports syntax/encoding error. |
| 2 | GM runs script function | Function exists | Function executes and logs result. |
| 3 | Function calls unbound API | Script runs | Bridge logs missing binding instead of crashing. |
| 4 | Script reload clicked | Script changes | New script version is used in sandbox. |

---

## M3.4 — Trap Script Hook

**As a** map script engineer, I want trap triggers to call stubbed/real Lua hooks, so that map interactions can be validated incrementally.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Player enters trap | Lua bridge disabled | Stub log records trigger. |
| 2 | Player enters trap | Lua bridge enabled | Configured Lua function is attempted. |
| 3 | Lua function fails | Trigger fires | Error appears in Logs tab with trap id. |

---

## 9. Phase M4 — Skills, Missiles, Combat

## M4.1 — Skill Catalog

**As a** gameplay engineer, I want a skill catalog mapped from PC config/source, so that skills can be selected in sandbox.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Skill config exists | Converter runs | SkillDefinition entries are generated. |
| 2 | Skill references icon/effect | Registry resolves | Asset links are validated. |
| 3 | GM selects skill | UI updates | Selected skill details are shown. |

---

## M4.2 — Missile/Projectile Prototype

**As a** combat engineer, I want missile/projectile visuals to spawn from skill data, so that PC projectile behavior can be mapped.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Skill has missile/effect ref | Cast in sandbox | Projectile/effect placeholder spawns. |
| 2 | Effect sprite available | Cast in sandbox | Decoded sprite effect plays. |
| 3 | Target blocked/out of range | Cast requested | Cast is rejected with diagnostic reason. |

---

## M4.3 — Damage Formula Port

**As a** combat engineer, I want damage formulas mapped from PC logic, so that future combat parity is testable.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Formula source evidence exists | Port implemented | Unit tests cover representative fixtures. |
| 2 | GM edits stats | Skill damage preview updates | Preview matches formula output. |
| 3 | Formula source is unclear | Work starts | Source evidence gap is recorded before implementation. |

---

## 10. Phase M5 — Items and Equipment

## M5.1 — Item Contract Import

**As an item engineer,** I want item contract outputs from `/var/www/vltktool` imported, so that Unity item data starts from existing validated artifacts.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Contract bundle generated | Unity import runs | Item definitions are created/updated. |
| 2 | Quality gate report exists | Import completes | Gate status is visible in GM Tools tab. |
| 3 | Contract has stubbed rules | Strict mode enabled | Import fails or marks warning according to config. |

---

## M5.2 — Inventory and Equipment Sandbox

**As a** gameplay engineer, I want sandbox inventory/equipment tools, so that item data can be tested before production UI.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Item database exists | GM opens Items tab | Items are searchable. |
| 2 | Developer adds item | Command runs | Item appears in test inventory. |
| 3 | Developer equips item | Command runs | Character stats preview updates. |
| 4 | Item icon missing | Item displayed | Missing icon diagnostic is shown. |

---

## M5.3 — Set Bonus and Refine Rules

**As an item engineer,** I want set bonus/refine rules validated against golden cases, so that item parity is measurable.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Golden replay cases exist | Tests run | Expected stat outcomes match. |
| 2 | Rule is stubbed | Quality gate runs | Stub status is reported. |
| 3 | GM changes equipment | Preview updates | Set/refine effects recalculate. |

---

## 11. Phase M6 — Mobile Polish and Production Runtime

## M6.1 — Touch Controls

**As a** mobile player, I want touch controls, so that the game is playable on phones/tablets.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Mobile build runs | Player taps walkable map | Player moves to target. |
| 2 | Virtual joystick enabled | Player drags joystick | Player moves continuously. |
| 3 | Skill buttons visible | Player taps skill | Skill cast flow starts. |
| 4 | Pinch gesture used | Camera zooms | Zoom respects limits. |
| 5 | UI button displayed | Screen size changes | Touch target remains readable and usable. |

---

## M6.2 — Mobile Asset Loading

**As a** runtime engineer, I want mobile-friendly asset loading, so that large converted maps do not freeze or exceed memory budget.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Asset packaging strategy selected | Build runs | Assets needed by test scene are included or loadable. |
| 2 | Large map selected | Load starts | Loading is asynchronous or progress-visible. |
| 3 | Memory budget exceeded | Runtime detects risk | GM/Logs report budget warning. |
| 4 | AssetBundle/Addressables decision changes | Docs update | Asset Registry load modes remain stable. |

---

## M6.3 — Android/iOS Build Smoke

**As a** release owner, I want Android/iOS smoke builds, so that the sandbox can prove mobile viability.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | Android build target selected | Build runs | Build completes with IL2CPP config. |
| 2 | iOS build target selected | Build/export runs | Export completes or reports missing platform setup. |
| 3 | Sandbox runs on device/emulator | GM button tapped | GM Panel opens. |
| 4 | Map loaded on device | Runtime runs | FPS/memory counters are captured. |

---

## M6.4 — Production HUD Bridge

**As a** UX engineer, I want production HUD to consume sandbox-proven systems, so that debug and production surfaces do not diverge.

### Acceptance Criteria

| # | Given | When | Then |
| --- | --- | --- | --- |
| 1 | HUD is implemented | It needs map/player data | It reads from runtime systems, not conversion internals. |
| 2 | GM Panel exists | Production HUD enabled | GM can still be opened in dev builds. |
| 3 | Release build configured | GM disabled or protected | Debug controls are not exposed unintentionally. |

---

## 12. Validation Strategy

### 12.1 Unit tests

Unit tests should cover pure logic:

- Source identity normalization and hashing.
- Manifest parsing and duplicate detection.
- PAK/resource lookup abstraction with fixtures.
- SPR parser metadata and invalid format reporting.
- Map metadata parser.
- Region/obstacle conversion.
- Coordinate conversion.
- Asset Registry resolution.
- Damage/item formula pure functions when implemented.

### 12.2 Integration tests

Integration tests should cover module boundaries:

- vltktool output -> Unity import manifest.
- Map catalog -> MapDefinition -> Asset Registry references.
- Region conversion -> obstacle query.
- Sprite conversion -> atlas/clip registry.
- Item contract bundle -> Unity definitions.

### 12.3 Sandbox smoke tests

Sandbox smoke tests should prove:

- Scene boots.
- GM button opens/closes panel.
- `q` toggles panel in Play Mode.
- Map list populates.
- Map load/unload works.
- Error reporting is visible.
- Overlay toggles work.

### 12.4 Golden tests

Golden tests should use fixed fixtures:

- One small representative map.
- One large outdoor map.
- One indoor map.
- One map with weather/light metadata.
- One map with missing assets to validate diagnostics.
- Representative SPR files including valid, alpha, multi-frame, directional, and invalid examples.
- Item golden cases reused from vltktool.

### 12.5 Performance budgets

Initial budgets are provisional and must be refined after first device profiling:

| Area | Initial target |
| --- | --- |
| Sandbox boot | Fast enough for iterative editor work; exact budget after implementation baseline |
| GM panel toggle | Immediate, no visible hitch |
| Map switch in editor | Progress-visible; no unhandled exception |
| Runtime map streaming | Active + neighbor regions within memory budget |
| Mobile frame rate | Target 60 FPS where feasible; document fallback for heavy maps |
| Missing asset handling | No crash; diagnostic shown |

---

## 13. Reference Paths and Tool Responsibilities

### 13.1 PC source key areas

| System | Reference area |
| --- | --- |
| Scene/map | `SwordOnline/Sources/Core/Src/Scene/` |
| Region/world | `KRegion`, `KSubWorld`, scene data definitions |
| Player | `KPlayer`, `KPlayerDef` |
| NPC | `KNpc`, `KNpcAI`, NPC templates/config |
| Skill/combat | Skill, fight, missile/projectile source areas |
| Item/equipment | Item, inventory, magic attrib, refine/set systems |
| Sprite/rendering | Engine sprite/bitmap/canvas/draw source and Represent shell |
| Archive/package | PAK/zip/package data source code |
| Lua binding | Script function bridge/source and Lua script folders |
| Config | settings/map/item/npc/skill/missile text and INI files |

### 13.2 vltktool responsibilities

| Task | Existing helper direction |
| --- | --- |
| PAK extraction | Reuse unpak/resource lookup concepts and decompression helpers. |
| SPR decode | Reuse sprite extraction experiments and invalid SPR reporting. |
| BLH/map terrain | Reuse terrain UID/path experiments as fixtures for map conversion. |
| Map metadata | Reuse unpacked `maps_pak` samples for parser fixtures. |
| Item contracts | Reuse contract bundle, quality gate, and golden replay. |
| Unity scaffolding | Reuse scaffold generator patterns for generated C# data if appropriate. |
| Resource docs | Reuse inventory/audit style for conversion coverage reports. |

---

## 14. Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| `jxwin-kinnox` not indexed in GitNexus | Harder call-graph research | Use Semble, record gap, re-index before deep symbol-sensitive refactors. |
| Legacy encodings corrupt names/paths | Wrong asset matching | Preserve raw and normalized text; add encoding tests. |
| SPR format variants/invalid signatures | Missing visuals | Keep invalid reports; classify by source package and decoder failure. |
| Map data huge | Editor/mobile slowdowns | Offline conversion, streaming, atlases, diagnostics, budgets. |
| Obstacle parity wrong | Movement/combat broken | Visual overlay, coordinate tests, PC source evidence. |
| Raw PAK runtime parsing too slow | Mobile hitching | Runtime consumes Unity-ready artifacts; raw parsing only in tools/sandbox experiments. |
| GM Panel becomes dumping ground | Hard to maintain | Keep commands/query surface; internals live in modules. |
| Full gameplay distracts from map parity | Port loses foundation | M1 requires all maps before deep M4/M5 work. |
| Tool outputs become untraceable | Impossible to debug assets | Require manifests with source inputs/checksums/tool versions. |

---

## 15. Definition of Done by Phase

| Phase | Done means |
| --- | --- |
| M0 | Sandbox boots, GM opens by button/`q`, canonical model/registry skeleton exists, source/tool workflow documented. |
| M1 | All discoverable maps are in catalog; converted maps load or report actionable errors in sandbox; map coverage report exists. |
| M2 | Player/camera/movement can validate converted coordinates and obstacles. |
| M3 | NPC/trap/Lua surfaces can be inspected and partially executed/stubbed in sandbox. |
| M4 | Skill/missile/combat prototypes run against source-derived data and formulas. |
| M5 | Item/equipment systems import existing contracts and validate golden cases. |
| M6 | Mobile controls, packaging, performance, and Android/iOS smoke builds are proven. |

---

## 16. First Work Packet Recommendation

Implement the first vertical slice as:

1. Create/open Sandbox Scene as default dev scene.
2. Add root GameObjects and sandbox bootstrap logs.
3. Add GM button and `q` toggle.
4. Add Map tab with placeholder catalog.
5. Add Asset Registry and ConversionManifest pure models.
6. Add MapCatalogEntry/MapDefinition pure models.
7. Add a fixture-based map loader that can later consume real converter output.
8. Add Logs tab and subsystem log prefixes.
9. Add source evidence note template in docs or report output.
10. Add smoke tests or manual validation checklist for scene boot, GM toggle, and map switching.

This first slice should avoid deep gameplay and focus on proving the sandbox/control/data architecture.
