# PRD: VLTK Mobile — Port Võ Lâm Truyền Kỳ PC sang Unity Mobile

**Status**: `ready-for-agent`  
**Priority**: High  
**Created**: 2026-05-30  
**Updated**: 2026-05-30  
**Owner intent**: sandbox-first reverse engineering, port toàn bộ map trước trong Unity, sau đó mở rộng gameplay.  
**Target runtime**: Unity 6 LTS, C#, Android/iOS, IL2CPP.  
**PC source reference**: `/var/www/vltk-mobile/jxwin-kinnox` — read-only reference.  
**Porting tool suite**: `/var/www/vltktool` — helper tools for extraction, conversion, audit, and scaffolding.  

---

## Problem Statement

Võ Lâm Truyền Kỳ / JX Online 3 đã có bản PC hoàn chỉnh dựa trên C++/DirectX/Lua, proprietary data formats, nhiều map, sprite, NPC, item, skill, script, và config cũ. Dự án mobile cần một Unity client có thể chạy trên Android/iOS nhưng vẫn giữ được cảm giác, dữ liệu, và hành vi cốt lõi của bản PC.

Vấn đề lớn nhất không phải là dựng UI mobile trước, mà là **reverse engineer đúng dữ liệu gốc**. Nếu port từng màn hình hoặc từng gameplay system rời rạc ngay từ đầu, dự án sẽ dễ mất parity với PC source, không biết asset nào đúng, map nào thiếu, obstacle nào sai, và khó debug trên thiết bị mobile. Vì vậy cần một **Sandbox Scene duy nhất** trong Unity để nhồi tất cả hệ thống porting vào trước: map rendering, asset registry, data model, GM tools, switch map, chỉnh thông số, bật/tắt debug, kiểm tra sprite/region/obstacle/NPC/trap, và chạy các conversion output từ `/var/www/vltktool`.

Người phát triển cần có một môi trường mà chỉ cần mở Unity hoặc bấm Play là có thể:

- Load sandbox mặc định.
- Mở GM popup bằng nút UI hoặc shortcut `q`.
- Switch map nhanh.
- Inspect và chỉnh thông số runtime.
- Render thử tất cả map đã reverse engineer.
- So sánh dữ liệu đã port với PC source gốc.
- Ghi nhận thiếu hụt theo từng map/system để agent hoặc developer tiếp tục xử lý.

---

## Solution

Xây dựng VLTK Mobile theo hướng **Sandbox-first, Data-model-first, Map-first**:

1. **Sandbox Scene là mặt trận đầu tiên**  
   Một scene Unity duy nhất dùng để test mọi hệ thống trước khi tách thành production scene. Sandbox chứa GameManager, Camera, World root, UI root, Debug root, GM Panel, map loader, resource registry, input/debug shortcuts, và runtime diagnostics.

2. **GM Panel là command center của developer**  
   GM Panel mở bằng nút trên UI và bằng shortcut `q`. Panel có tab map switching, runtime variables, character stats, camera, weather/time, obstacle overlay, NPC/trap/object spawns, asset diagnostics, console log, profiler counters, và conversion/audit status.

3. **Reverse engineer map toàn diện trước gameplay sâu**  
   Ưu tiên port toàn bộ map vào sandbox đầu tiên. Dữ liệu map được convert offline thành Unity-native data: metadata, terrain tiles/atlases, region data, obstacle grid, object placement, minimap/overview, lighting/weather, NPC/trap spawn tables. Runtime sandbox chỉ load assets đã convert, không parse raw PAK/MAP nặng trong gameplay loop.

4. **`jxwin-kinnox` là reference truth, `vltktool` là conversion support**  
   Khi cần hiểu logic PC, dùng Semble để search semantic/text trên `/var/www/vltk-mobile/jxwin-kinnox`. Dùng GitNexus repo `jxwin-kinnox` cho knowledge graph của phần core game/engine/render đã index; các vùng bị loại khỏi graph để tránh treo analyzer như Utility, server/payment/update tooling, config text, và Lua script bulk vẫn tra cứu bằng Semble khi cần. Dùng `/var/www/vltktool` để tái sử dụng tool extraction/conversion đã có: PAK unpacking, SPR decoding, BLH terrain experiments, item contract bundles, Unity scaffold generation, and quality gates.

5. **Deep modules thay vì script lẻ**  
   Tách thành các module sâu, testable, ít thay đổi interface: Source Intelligence, Asset Conversion Pipeline, Canonical Data Model, Asset Registry, Map Port Pipeline, Sprite/Animation Pipeline, Sandbox Runtime, GM Panel, Lua Bridge, Gameplay Systems, Validation Harness.

6. **Mobile production chỉ đến sau khi sandbox chứng minh parity**  
   Touch controls, HUD production, mobile performance, asset bundle strategy, and Android/iOS builds được triển khai sau khi sandbox đã render được map và validate data model ổn định.

---

## User Stories

1. As a developer, I want Unity to open a Sandbox Scene by default, so that every porting task starts from a known test environment.
2. As a developer, I want the Sandbox Scene to contain stable root objects for game, camera, UI, world, and debug layers, so that systems can be mounted consistently.
3. As a developer, I want a visible GM button in the sandbox UI, so that I can open debugging tools without memorizing commands.
4. As a developer, I want shortcut `q` to toggle the GM popup in Play Mode, so that keyboard-driven testing is fast.
5. As a developer, I want the GM popup to preserve its last tab and selected map, so that repeated test sessions are efficient.
6. As a developer, I want the GM Panel to show project/runtime status, so that I can immediately know whether sandbox initialization succeeded.
7. As a developer, I want the GM Panel to expose map switching, so that I can test any converted map without changing scene files.
8. As a developer, I want the GM Panel to expose search/filter for map list, so that hundreds of maps can be navigated quickly.
9. As a developer, I want the GM Panel to expose coordinate teleport, so that I can jump to any region/cell/pixel position.
10. As a developer, I want the GM Panel to expose player stats and movement speed, so that character/map interactions can be tested before full gameplay exists.
11. As a developer, I want the GM Panel to expose weather and time-of-day controls, so that converted environment data can be validated.
12. As a developer, I want the GM Panel to expose obstacle overlay toggles, so that blocked/walkable/fly/jump data can be visually checked.
13. As a developer, I want the GM Panel to expose region bounds and streaming diagnostics, so that map loading errors are visible.
14. As a developer, I want the GM Panel to expose sprite diagnostics, so that missing frames, pivots, palettes, and alpha issues are visible.
15. As a developer, I want the GM Panel to expose NPC/trap/object spawners, so that map content can be tested before production systems are complete.
16. As a developer, I want the GM Panel to expose Lua script reload/run controls, so that original script behavior can be tested incrementally.
17. As a developer, I want the GM Panel to expose console logs filtered by system, so that map, sprite, item, skill, and Lua issues can be isolated.
18. As a developer, I want the GM Panel to expose performance counters, so that large maps and sprite counts can be profiled early.
19. As a developer, I want the sandbox to catch subsystem exceptions and continue running where possible, so that one bad asset does not stop the whole audit.
20. As a reverse engineer, I want a documented source-search protocol for `jxwin-kinnox`, so that every claim about PC behavior can be traced.
21. As a reverse engineer, I want Semble search examples for map, sprite, PAK, item, skill, and Lua systems, so that future agents use the right tool first.
22. As a reverse engineer, I want GitNexus usage documented when the source repo is indexed, so that call graph and symbol context can complement Semble evidence.
23. As a reverse engineer, I want missing GitNexus index status to be explicit, so that agents do not pretend knowledge graph evidence exists.
24. As a reverse engineer, I want PC source references to identify the relevant class/module names, so that port work has precise anchors.
25. As a tool user, I want `/var/www/vltktool` documented as the helper suite, so that existing extraction/conversion work is reused.
26. As a tool user, I want vltktool command categories documented, so that PAK, SPR, map, item, and scaffold tasks start from known utilities.
27. As a tool user, I want vltktool outputs to feed Unity asset conversion, so that Python experiments become reproducible Unity inputs.
28. As a data engineer, I want a canonical data model for source assets, converted assets, map metadata, regions, objects, obstacles, traps, NPC spawns, sprites, and configs, so that gameplay code does not depend on raw PC binary layout.
29. As a data engineer, I want asset IDs to preserve PC resource identity, so that runtime lookups can map back to original files.
30. As a data engineer, I want conversion manifests, so that each converted Unity asset can be traced back to its PAK/resource source.
31. As a data engineer, I want conversion reports with success/warning/error states, so that missing assets are visible before runtime.
32. As a data engineer, I want deterministic conversion output, so that repeated runs do not produce unstable GUIDs or duplicated assets.
33. As a map engineer, I want to discover every map from PC map lists/settings, so that the sandbox target scope is complete.
34. As a map engineer, I want to convert all map metadata, so that names, bounds, indoor/outdoor flags, brightness, color, lighting, weather, and map-left-top region data are preserved.
35. As a map engineer, I want to convert each map into region records, so that Unity can stream the active region plus neighboring regions.
36. As a map engineer, I want to preserve original region dimensions and coordinate math, so that NPC, movement, and obstacle systems align with PC behavior.
37. As a map engineer, I want terrain layers converted into atlases and metadata, so that Unity can render ground efficiently.
38. As a map engineer, I want built-in objects converted into object placement records, so that trees, buildings, decorations, and foreground layers render correctly.
39. As a map engineer, I want obstacle data converted into compact grids, so that movement/pathfinding can query walkability quickly.
40. As a map engineer, I want trap regions converted into sandbox-visible overlays, so that script triggers can be validated later.
41. As a map engineer, I want minimap/world map data converted, so that map UI parity can be checked.
42. As a map engineer, I want sandbox map switching to display load progress and errors, so that incomplete maps are easy to triage.
43. As a map engineer, I want a batch conversion mode for all maps, so that full-map parity is measurable.
44. As a map engineer, I want golden map snapshots, so that visual regressions can be detected after converter changes.
45. As a sprite engineer, I want SPR files decoded into textures/sprites with palettes and alpha preserved, so that original art style remains intact.
46. As a sprite engineer, I want frame metadata, pivots, offsets, directions, and render style captured, so that animation parity is possible.
47. As a sprite engineer, I want invalid SPR reports, so that broken/mismatched resources can be separated from decoder bugs.
48. As a rendering engineer, I want region/object sorting rules to preserve PC z-order, so that foreground/background layering looks correct.
49. As a rendering engineer, I want batching/atlas strategy documented, so that mobile performance is considered from the start.
50. As a gameplay engineer, I want a player placeholder that can move on converted maps, so that map scale, coordinates, and obstacles can be tested.
51. As a gameplay engineer, I want character data models for stats, skills, equipment, position, and state, so that later gameplay systems share a stable base.
52. As a gameplay engineer, I want NPC templates and spawn tables derived from PC data, so that sandbox maps can show representative NPC content.
53. As a gameplay engineer, I want Lua integration staged behind a bridge, so that original scripts can be reused without coupling Unity directly to raw Lua files.
54. As a gameplay engineer, I want skill, missile, and combat formulas mapped from source reference, so that combat parity can be built after map parity.
55. As an item engineer, I want item contracts generated from existing tooling, so that equipment and inventory can align with original data.
56. As a QA engineer, I want tests focused on external behavior and parity, so that implementation details can change safely.
57. As a QA engineer, I want unit tests for pure parsers and data models, so that raw format decisions are locked down.
58. As a QA engineer, I want integration tests for conversion manifests and asset registry resolution, so that Unity runtime can trust converted data.
59. As a QA engineer, I want sandbox smoke tests, so that scene boot, GM panel, shortcut, and map switching do not regress.
60. As a QA engineer, I want performance budgets for large maps, so that mobile feasibility is tested before production polish.
61. As a mobile player, I want touch controls and readable UI after core map/gameplay systems work, so that the final game is usable on phones.
62. As a mobile player, I want assets to stream without long freezes, so that switching maps and moving across regions feels smooth.
63. As a release owner, I want Android/iOS build constraints documented, so that IL2CPP, asset bundles, and memory budgets are not afterthoughts.
64. As a project owner, I want a milestone plan from sandbox to production, so that agent work can be sliced safely.
65. As a project owner, I want out-of-scope work recorded, so that multiplayer/server/payment/release features do not distract from the porting foundation.
66. As a future agent, I want docs to state which source paths and tools are evidence anchors, so that I can continue without rediscovering everything.
67. As a future agent, I want each work item to state acceptance criteria, so that implementation can be validated without asking the user again.
68. As a future agent, I want known risks and index gaps documented, so that hallucinated source claims are avoided.
69. As a future agent, I want the docs to prefer Semble and GitNexus over ad-hoc grep/glob reading for PC source research, so that searches stay aligned with repo instructions.
70. As a future agent, I want the sandbox to be allowed to contain unfinished systems, so that porting can proceed incrementally while remaining inspectable.

---

## Implementation Decisions

### 1. Sandbox-first is the implementation spine

The first playable surface is not a production login flow or mobile HUD. The first surface is one Sandbox Scene that can host every porting subsystem and show failures directly. Production scenes may come later, but sandbox remains the permanent developer QA surface.

### 2. GM Panel is mandatory in the first milestone

The GM Panel is not a nice-to-have debug overlay. It is the primary control surface for porting. It must be accessible through both a visible UI button and the `q` shortcut. Its first version must support map switching, runtime variable editing, logs, overlays, and diagnostic tabs. Later versions add NPC, Lua, skill, item, and profiling tools.

### 3. Reverse engineer and port all maps into sandbox before deep gameplay

The project should prioritize full map inventory/conversion/rendering before deep character, combat, inventory, or production mobile UI. Maps are the broadest data dependency and expose sprite, PAK, region, obstacle, object placement, lighting, weather, and script trigger issues early.

### 4. PC source remains read-only reference truth

The PC source under `/var/www/vltk-mobile/jxwin-kinnox` should not be modified during the Unity port. It is used to understand formats and behavior. Each non-trivial porting decision should be traceable to source evidence, tool output, or observed converted data.

### 5. Reference research protocol uses Semble and GitNexus appropriately

- Use Semble for semantic/text search across `jxwin-kinnox`, especially when locating source modules, class names, data structures, or config examples.
- Use GitNexus for symbol context, call graph, process flows, and impact from the indexed `jxwin-kinnox` core graph.
- The `jxwin-kinnox` GitNexus graph is intentionally scoped by `.gitnexusignore` to stable runtime game/engine/render code so `gitnexus analyze` completes; use Semble for excluded legacy tooling, server/payment/update code, config text, and bulk Lua script research.
- If GitNexus ever does not list `jxwin-kinnox`, do not fabricate GitNexus evidence. Record the gap and use Semble until the index is refreshed.
- Use GitNexus for `vltk-mobile` when checking current Unity-side symbols/docs.

### 6. `/var/www/vltktool` is a helper suite, not a runtime dependency

The Python tools can extract, decode, convert, generate manifests, generate Unity scaffolds, and run quality gates. Unity runtime should consume deterministic converted artifacts and manifests, not shell out to Python tools in normal gameplay.

### 7. Offline conversion is the default for heavy proprietary data

Raw PAK, SPR, MAP/region, config, item, and script data should be converted offline where practical. Runtime parsing is allowed only for small metadata, diagnostics, or temporary sandbox experiments. Mobile runtime should prefer Unity-native assets, ScriptableObject-style metadata, binary blobs designed for fast load, and asset bundles/addressable packaging.

### 8. Canonical data model decouples Unity from PC binary layout

Unity systems consume canonical models such as MapCatalog, MapDefinition, RegionDefinition, ObstacleGrid, ObjectPlacement, SpriteClip, NpcTemplate, ItemDefinition, SkillDefinition, and ScriptBinding. These preserve source identity but avoid leaking C++ structs into gameplay code.

### 9. Asset Registry is a deep module

Asset Registry maps original PC resource identity to Unity asset paths, converted GUIDs, bundle names, manifests, and validation status. It is the stable interface used by map, sprite, item, NPC, and skill systems.

### 10. Map Port Pipeline is a deep module

Map Port Pipeline owns discovery, extraction, conversion, validation, and reporting for map data. Its interface should accept a map id/path and produce canonical map artifacts plus a report. The runtime MapManager should not know how PAK or raw region files are decoded.

### 11. Sprite/Animation Pipeline is a deep module

SPR parsing, palette conversion, frame extraction, pivot/offset preservation, atlas packing, and clip metadata are isolated from gameplay systems. Rendering systems consume Unity sprites/clips and metadata, not raw SPR bytes.

### 12. Lua Bridge is staged after map and data model foundation

Lua compatibility matters, but the first milestone should not block on full Lua behavior. The bridge starts with script loading, sandbox invocation, bindings audit, and deterministic logs, then expands to NPC/dialog/trap behaviors.

### 13. Mobile performance is considered from M0 but optimized after parity

The docs use Unity mobile-aware decisions: scene list/build inclusion, asynchronous scene/asset loading, AssetBundle/Addressables evaluation, IL2CPP builds, batching/atlas constraints, and memory budgets. Heavy optimization should follow after map parity is measurable.

### 14. Tests target behavior and parity, not implementation details

Tests validate external outputs: decoded metadata, conversion reports, asset registry resolution, sandbox boot, GM shortcut behavior, map load/render, obstacle queries, and golden snapshots. Internal class names or helper method shapes should remain flexible.

### 15. Source evidence already identified

Semble review found source anchors for map and data behavior: scene/region loading, map settings/minimap handling, obstacle classification, PAK archive reading, SPR frame/palette processing, and world/map lists. The spec records these as research anchors, not final implementation APIs.

---

## Testing Decisions

A good test for this project proves **observable porting behavior**:

- Given source data and conversion config, the generated manifest and artifacts are deterministic.
- Given a converted asset id, the Asset Registry resolves the correct Unity asset.
- Given a map id, Sandbox loads the map, shows region/object/obstacle diagnostics, and reports missing assets without crashing.
- Given shortcut `q`, the GM Panel toggles in Play Mode.
- Given map coordinates, obstacle queries match converted PC obstacle classes.
- Given a golden map fixture, the rendered output stays within accepted visual tolerance.

Modules that need tests early:

1. Source identity and UID hashing.
2. PAK index/resource lookup abstraction.
3. SPR parser/decoder and frame metadata exporter.
4. Map catalog discovery.
5. Region/obstacle/object/trap data converters.
6. Asset Registry resolution.
7. Sandbox boot and GM Panel toggle.
8. MapManager load/switch/unload flow.
9. Conversion report validation.
10. Item contract/scaffold quality gates reused from `vltktool`.

Prior art and evidence:

- `/var/www/vltktool` already has quality gate and golden replay concepts for item contracts.
- PC source has clear map/region/obstacle/sprite/archive boundaries that can become parser fixtures.
- Unity docs fetched through Context7 confirm scene list/build inclusion and asynchronous scene/asset loading considerations for mobile.

---

## Out of Scope

The following are intentionally out of scope for the first PRD slice unless explicitly requested later:

- Production login, account, authentication, payment, chat, guild, marketplace, or server networking.
- Full MMO backend implementation.
- App Store / Play Store submission pipeline.
- Anti-cheat, security hardening, and live operations tooling.
- Full combat/item balancing parity before maps are loaded in sandbox.
- Full Lua compatibility before sandbox can load and inspect converted maps.
- Replacing Unity with another engine.
- Editing the PC source under `jxwin-kinnox`.
- Treating extracted cache outputs as source truth without conversion manifests.
- Shipping raw PAK/SPR/MAP blobs as the primary mobile runtime format without a deliberate performance decision.

---

## Further Notes

### Evidence and reference anchors from discovery

Semble review of `jxwin-kinnox` found the following high-value anchors:

| Area | Evidence anchor |
| --- | --- |
| Scene/region client rendering | `KScenePlaceRegionC`, `KScenePlaceC`, scene region load/preload/prerender behavior |
| Map switching / world sync | `KSubWorld::LoadMap`, protocol world sync flow |
| Region file composition | `SceneDataDef` defines combined region file sections such as obstacle, trap, NPC, object, ground, built-in object |
| Obstacle semantics | `ObstacleDef`, `KScenePlaceC::GetObstacleInfo`, `KRegion::LoadLittleMapData`, obstacle flags for walk/fly/jump |
| Minimap/world map | `ScenePlaceMapC`, `KLittleMap`, UI world map references to map list/settings |
| PAK archive | `KPakData`, `KPakFile`, `KZipList`, package utility code |
| SPR/sprite rendering | `KSpriteMaker`, `KDrawSprite`, `KDrawSpriteAlpha`, represent shell sprite draw/copy flows |
| Map inventory examples | `WorldSet*.ini`, map list/settings files in PC data |

Semble review of `/var/www/vltktool` found helper anchors:

| Area | Tool/evidence |
| --- | --- |
| PAK extraction | `unpak_tool.py` and helpers such as UID/path normalization/decompress entry |
| BLH terrain experiments | `extract_blh_terrain.py`, `find_blh_terrain_uids.py` |
| Map settings examples | unpacked `maps_pak/*.ini` with lighting/weather/rect/MapLTRegionIndex |
| SPR decode experiments | extracted SPR/PNG reports including invalid-signature diagnostics |
| Item contracts | `generate_item_contract_bundle.py`, `run_item_quality_gate.py`, `run_item_golden_replay.py` |
| Unity scaffolding | `generate_unity_item_scaffold.py` |

### GitNexus note

Current discovery now confirms GitNexus has `jxwin-kinnox` indexed. The index is intentionally scoped to core runtime source paths that complete reliably: Core, Engine, Represent, and shared headers. Use GitNexus for symbol/call-graph/process context in that scope, and use Semble for semantic repository search across excluded legacy tooling, server/payment/update code, Lua bulk scripts, and config files.

### Milestone suggestion

| Phase | Focus | Deliverable |
| --- | --- | --- |
| M0 | Foundation | Sandbox Scene, GM Panel, data model, source/tool protocol, PAK/SPR/map conversion skeleton |
| M1 | Maps | All discoverable maps converted and loadable in sandbox with terrain/object/obstacle/lighting diagnostics |
| M2 | Character & movement | Player placeholder, coordinate model, camera follow, movement/pathfinding over converted obstacles |
| M3 | NPC, Lua, map scripts | NPC templates/spawns, traps, Lua bridge, script diagnostics |
| M4 | Skills & combat | Skill catalog, missile/projectile rendering, damage formulas, status effects |
| M5 | Items & equipment | Item database, inventory/equipment, set bonus/refine, icon/resources |
| M6 | Mobile polish | Touch controls, HUD, performance, asset packaging, Android/iOS builds |

### Recommended first implementation slice

Build M0.1–M0.6 as one vertical slice:

1. Sandbox Scene opens by default.
2. GM Panel toggles by button and `q`.
3. Source identity and Asset Registry models exist.
4. Map catalog can ingest a small fixture or generated manifest.
5. Sandbox can switch between placeholder maps and show diagnostics.
6. The workflow records exactly which Semble/GitNexus/vltktool evidence was used.
