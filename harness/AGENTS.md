# Agent Instructions

## Research trước khi làm

## Tools RULES:

- Always use `srcwalk` skill: for best codebase/files/dirs exploration, discover, searching.
- Always prefer `srcwalk` CLI over read/glob/grep tool.
- Prefer `fd` over `find`.
- Prefer `rg` over `grep`.

Trước khi bắt tay làm bất cứ việc gì (fix bug, port feature, dùng API/library lạ), **PHẢI dùng exa (`exa_web_search_exa`/`exa_web_fetch_exa`) và deepwiki (`mcp_deepwiki_deepwiki_fetch`) để research** cách làm chuẩn — không đoán, không vá mò. Research xong mới implement. Xong việc thì **commit all change + push**.

## Skill Matrix

### 🔴 Bắt buộc — Dùng trước & trong mọi port task

| Skill                      | Khi nào dùng                                               | Tóm tắt                                                                        |
| -------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `jx-pc-port-rule`        | **TRƯỚC MỌI PORT TASK** — không ngoại lệ        | Ép inspect PC source trước, port 100% từ PC. Không đoán, không tự chế. |
| `srcwalk`                | Code navigation, tìm symbol/file/flow                       | Repo map, symbol search, callers/callees, deps. Ưu tiên hơn grep/read.        |
| `unity-mcp-orchestrator` | Tạo/sửa GameObject, scene, script, test trong Unity Editor | Điều khiển Unity Editor qua MCP — CRUD scene, script, component, test.       |

### 🟡 Theo task — Port cụ thể

| Skill                | Khi nào dùng                                                 | Tóm tắt                                                           |
| -------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------- |
| `jx-map-port`      | Port map, Region_C.dat, terrain, minimap, click-to-move        | Port map PC→Unity: geometry, SPR terrain, minimap, toạ độ PC.   |
| `jx-enemy-port`    | Port enemy/mob/NPC spawn, NpcS.txt, Region_S, võ sư/cọc gỗ | Spawn enemy từ PC data, SPR visuals, 8-way anim, HP/nameplate.     |
| `jx-hud-port`      | Port HUD/UI khớp PC — bars, minimap, hotbar, icons           | HP/MP/EXP/stamina bars, minimap, chat, hotbar, Ui3 SPR art.         |
| `jx-player-visual` | Player avatar — layered SPR, giáp/vũ khí/cưỡi ngựa      | Body/head/hair/hand/weapon layers, 8-way direction, sprite catalog. |

### 🟢 Hỗ trợ — Khi cần

| Skill                  | Khi nào dùng                                    | Tóm tắt                                                                     |
| ---------------------- | ------------------------------------------------- | ----------------------------------------------------------------------------- |
| `diagnose`           | Bug khó, regression, crash                       | Reproduce → minimise → hypothesise → instrument → fix → regression-test. |
| `tdd`                | Viết feature/fix có test                        | Red-green-refactor loop. Test trước, code sau.                              |
| `review`             | Review code branch/PR                             | 2 trục: Standards (coding std) + Spec (đúng yêu cầu issue/PRD).          |
| `understand-explain` | Cần giải thích deep file/function/module       | Deep-dive explanation cụ thể.                                               |
| `understand-chat`    | Hỏi đáp kiến trúc/flow qua knowledge graph   | Q&A về codebase.                                                             |
| `handoff`            | Compact context cho agent tiếp theo              | Tóm tắt conversation → handoff document.                                   |
| `pi-subagents`       | Delegate task cho sub-agent, chain, parallel      | Fan-out công việc, review song song, pipeline.                              |
| `prototype`          | Test nhanh ý tưởng UI/logic trước khi commit | Prototype throwaway — UI variations hoặc terminal state app.                |

## Project Overview

### User-facing phải là tiếng việt. nếu jx pc là tiếng trung thì phải việt hoá

VLTK Mobile — Port game Võ Lâm Truyền Kỳ (JX Online 3) từ PC sang Unity Mobile.

### Cấu trúc Repo

| Path                     | Mục đích                            |
| ------------------------ | -------------------------------------- |
| `/var/www/vltk-mobile` | Unity mobile client (C# / Unity 2022+) |

Reference files gốc từ PC được lưu trong `Assets/StreamingAssets/Reference/` (Skills.txt, gaibang.lua, Missles.txt, NpcS.txt, KNpc.cpp, SceneDataDef.h).

### Tool hỗ trợ

- `/var/www/vltktool/` — Bộ công cụ Python: SPR decoder, PAK unpacker, item runtime, CMS web

### 🔴 BẮT BUỘC khi tìm UI / icon / img / SPR / PAK / tài nguyên game

Khi cần tìm hoặc tra cứu bất kỳ **UI, icon, ảnh, SPR, PAK, hay tài nguyên game** nào trong nguồn PC (`/var/www/jx-source`):

1. **PHẢI đọc trước** `/var/www/vltktool/README.md` — đặc biệt phần **Ma Trận Tra Cứu Nhanh Cho Agent** — để chọn đúng tool.
2. **PHẢI đọc/cite cây canonical đã unpack trước**: `/var/www/jx-source/pak_unpacked` (manifest `_unpack_summary.json`). Không unpack lại khi file đã có sẵn.
3. **PHẢI dùng tool có sẵn** trong `/var/www/vltktool/` khi cần tra cứu/preview/repair (vd `find_spr_by_image.py`, `resolve_uid.py`, `extract_item_spr.py`; `unpak_tool.py` chỉ cho repair/re-unpack đặc biệt).
4. **KHÔNG được tự viết script riêng** để decode/scan SPR/PAK, hash uid, hay match ảnh. Tool đã chuẩn hoá, đã test, có guard chống crash.
5. **KHÔNG quét toàn bộ source.** Dùng logic (đọc ini/lua trong source loose + pak_unpacked, suy ra feature) để **thu hẹp vùng** (1 PAK / 1 folder) trước, rồi mới trỏ tool vào — quét rộng sẽ crash máy.
6. Nếu tool thiếu tính năng cần thiết → **bổ sung vào tool trong `/var/www/vltktool/`** (surgical edit + test), không tạo script rời.
7. **Tra port docs trước khi quét tool** — `/var/www/jx-source/docs/port_docs/` là tài liệu đã audit (verified vs source):
   - `16_client_resources.md` — cây tài nguyên client (PAK, SPR, ui3, settings) + feature nằm ở PAK/folder nào → dùng để **thu hẹp vùng** (point 5).
   - `18_spr_asset_index.md` — SPR asset index/taxonomy. Cây canonical hiện có 75,928 SPR; label map/API có thể cần rebuild trên root mới trước khi dùng tên Việt/taxonomy tự động (`http://localhost:8081/`, chạy bằng `make dev` trong vltktool).
   - `19_pak_spr_taxonomy.md` — taxonomy PAK→SPR, cách map nhóm → folder gốc khi port.
8. **Quy tắc provenance (≥99% không bịa)** — nếu label map đã được rebuild và có field `confidence` thì dùng nó như evidence phụ, không thay thế path thật trong `pak_unpacked`:
   - `high`: có path engine THẬT (proven từ npcres-table / part-enum / code-ref / named). Port trực tiếp được.
   - `unidentified`: hash-only `unknown/<hash>.spr`, KHÔNG resolve được path. **KHÔNG gán công dụng** (Icon/Visual/Object...) vì sẽ sai — chỉ có metadata đo được (pak nguồn, kích thước, frame). Muốn biết là gì → mở preview tool xem, KHÔNG đoán tên/công dụng.
   - `pak_origin`: dùng để truy vết, nhưng vẫn cite đường dẫn file thật trong `pak_unpacked` khi port.

## Unity Package Matrix

> Unity 6 (6000.4.7f1) — URP đã active. Dùng bảng này để biết API nào available trước khi code.

### 🔴 Rendering & Pipeline

| Package                                  | Version            | Namespace / API chính              | Dùng khi nào                                                            |
| ---------------------------------------- | ------------------ | ----------------------------------- | ------------------------------------------------------------------------- |
| `com.unity.render-pipelines.universal` | 17.4.0 BuiltIn     | `UnityEngine.Rendering.Universal` | URP pipeline, URP materials, SRP Batcher, 2D Renderer, Volume system      |
| `com.unity.render-pipelines.core`      | (bundled với URP) | `UnityEngine.Rendering`           | `Volume`, `VolumeProfile`, `RenderPipelineAsset`                    |
| `com.unity.2d.sprite`                  | 1.0.0 BuiltIn      | `UnityEngine.U2D`                 | Sprite Atlas, SpriteRenderer                                              |
| `com.unity.2d.tilemap`                 | 1.0.0 BuiltIn      | `UnityEngine.Tilemaps`            | Tilemap rendering                                                         |
| `com.unity.2d.pixel-perfect`           | 6.0.0              | `UnityEngine.U2D`                 | `PixelPerfectCamera` — đảm bảo sprite không blur trên mọi device |
| `com.unity.2d.psdimporter`             | 15.0.0             | Unity Editor only                   | Import PSD multi-layer art                                                |

### 🟡 Asset Streaming

| Package                    | Version | Namespace / API chính            | Dùng khi nào                                                                                                                           |
| -------------------------- | ------- | --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `com.unity.addressables` | 3.1.0   | `UnityEngine.AddressableAssets` | Load SPR textures, map assets, NPC prefabs async. Thay thế `Resources.Load` và raw AssetBundle. `Addressables.LoadAssetAsync<T>()` |

### 🟡 Input & Controls

| Package                   | Version | Namespace / API chính      | Dùng khi nào                                                                            |
| ------------------------- | ------- | --------------------------- | ----------------------------------------------------------------------------------------- |
| `com.unity.inputsystem` | 1.19.0  | `UnityEngine.InputSystem` | Mobile joystick, touch input, action maps.**KHÔNG dùng `Input.GetAxis` legacy** |

### 🟡 UI

| Package                    | Version        | Namespace / API chính       | Dùng khi nào                                                                     |
| -------------------------- | -------------- | ---------------------------- | ---------------------------------------------------------------------------------- |
| `com.unity.ugui`         | 2.0.0 BuiltIn  | `UnityEngine.UI`           | HUD: HP/MP bars, minimap, hotbar, chat. Dùng cho in-game overlay                  |
| `com.unity.localization` | 1.5.9 embedded | `UnityEngine.Localization` | `LocalizedString`, `StringTable` — Việt hoá toàn bộ text từ tiếng Trung |

### 🟡 Networking

| Package                 | Version | Namespace / API chính         | Dùng khi nào                                                                                            |
| ----------------------- | ------- | ------------------------------ | --------------------------------------------------------------------------------------------------------- |
| `com.unity.transport` | 2.7.3   | `Unity.Networking.Transport` | UDP transport layer cho server connection. VLTK dùng custom binary protocol —**KHÔNG dùng NGO** |

### 🟢 Performance & Profiling

| Package                                           | Version       | Namespace / API chính              | Dùng khi nào                                                                                     |
| ------------------------------------------------- | ------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------- |
| `com.unity.burst`                               | 1.8.29        | `Unity.Burst`                     | `[BurstCompile]` — compile C# jobs thành native SIMD. Dùng cho SPR decode loops, Z-sort       |
| `com.unity.collections`                         | 6.4.0 BuiltIn | `Unity.Collections`               | `NativeArray<T>`, `NativeList<T>`, `NativeHashMap<K,V>` — GC-free containers cho job system |
| `com.unity.mathematics`                         | 1.3.3         | `Unity.Mathematics`               | `float2`, `float3`, `int2` — Burst-compatible math. Thay `Vector2/3` khi dùng với Burst |
| `com.unity.adaptiveperformance`                 | 6.0.0 BuiltIn | `UnityEngine.AdaptivePerformance` | Detect thermal throttle, scale quality động                                                      |
| `com.unity.adaptiveperformance.google.android`  | 6.0.0 BuiltIn | (provider)                          | Android ADPF provider — cần enable trong AdaptivePerformance settings                            |
| `com.unity.adaptiveperformance.samsung.android` | 5.1.0         | (provider)                          | Samsung GameSDK provider                                                                           |
| `com.unity.memoryprofiler`                      | 1.1.12        | Editor only                         | Snapshot memory, tìm SPR texture leak giữa map transitions                                       |
| `com.unity.performance.profile-analyzer`        | 1.4.0         | Editor only                         | So sánh 2 profile captures, verify optimization                                                   |
| `com.unity.profiling.core`                      | 1.0.3         | `Unity.Profiling`                 | `ProfilerMarker` — custom profiling markers trong code                                          |

### 🟢 Serialization & Data

| Package                             | Version | Namespace / API chính | Dùng khi nào                                                                                              |
| ----------------------------------- | ------- | ---------------------- | ----------------------------------------------------------------------------------------------------------- |
| `com.unity.nuget.newtonsoft-json` | 3.2.2   | `Newtonsoft.Json`    | Parse NpcS.txt, Skills.txt, complex JSON với Dictionary/inheritance.`JsonConvert.DeserializeObject<T>()` |

### 🟢 Camera & Scene

| Package                     | Version | Namespace / API chính   | Dùng khi nào                                           |
| --------------------------- | ------- | ------------------------ | -------------------------------------------------------- |
| `com.unity.cinemachine`   | 2.10.7  | `Cinemachine`          | `CinemachineVirtualCamera`, smooth follow, dolly track |
| `com.unity.ai.navigation` | 2.0.12  | `UnityEngine.AI`       | `NavMeshAgent` cho NPC pathfinding                     |
| `com.unity.timeline`      | 1.8.12  | `UnityEngine.Timeline` | Cutscene, story sequence                                 |

### 🟢 Testing

| Package                      | Version       | Namespace / API chính | Dùng khi nào          |
| ---------------------------- | ------------- | ---------------------- | ----------------------- |
| `com.unity.test-framework` | 1.6.0 BuiltIn | `NUnit.Framework`    | EditMode/PlayMode tests |

### ❌ KHÔNG CÓ / Không dùng

| Package                              | Lý do                                                                             |
| ------------------------------------ | ---------------------------------------------------------------------------------- |
| `com.unity.netcode.gameobjects`    | VLTK dùng custom binary TCP protocol — NGO không compatible                     |
| `com.unity.2d.animation` (16.0.0)  | Gây compile error với Unity 6 — đã remove. Unity 6 có builtin version riêng |
| `Resources.Load<T>()`              | Đã migrate sang Addressables                                                     |
| `Input.GetAxis` / `Input.GetKey` | Đã migrate sang InputSystem                                                      |

### ⚠️ Lưu ý quan trọng cho agent

1. **URP active** — mọi material mới phải dùng URP shaders (`Universal Render Pipeline/...`), không dùng `Standard` shader
2. **Localization embedded** — package ở `Packages/com.unity.localization/` (editable), không phải PackageCache
3. **Addressables GUID conflicts** — 2 exceptions về GUID conflict từ Addressables Tests là bug đã biết, bỏ qua
4. **Burst cần IL2CPP** — `[BurstCompile]` chỉ compile đầy đủ trên IL2CPP build (mobile), trên Editor dùng managed fallback
5. **Collections namespace** — `Unity.Collections` không phải `System.Collections.Generic`

<!-- HARNESS:BEGIN -->

## Harness

This repo uses Harness. Before work, read:

- `README.md`
- `docs/HARNESS.md`
- `docs/FEATURE_INTAKE.md`
- `docs/ARCHITECTURE.md`
- `docs/CONTEXT_RULES.md`
- `scripts/bin/harness-cli query matrix` on macOS/Linux, or `.\scripts\bin\harness-cli.exe query matrix` on Windows

Use the Rust Harness CLI at `scripts/bin/harness-cli` on macOS/Linux or
`scripts/bin/harness-cli.exe` on Windows as the main operational tool.

## VLTK Mobile Harness Extension

Project-specific source-of-truth additions for this repo:

- `docs/PORT_STATUS.md` is the port-status truth matrix. Read it before starting any PC→Mobile port task and update it after implementation.
- Do not mark a `PORT_STATUS.md` row `✅` unless exact code/catalog/source evidence and tests/verifier prove the stated narrow scope. Use `🔄` for parser/service/data-only or partial runtime.
- PC reference docs live at `/var/www/jx-source/docs/port_docs/`.
- PC source-of-truth includes the canonical unpacked PAK tree `/var/www/jx-source/pak_unpacked` with manifest `/var/www/jx-source/pak_unpacked/_unpack_summary.json`. PAK contents are not image-only; they include SPR assets, Lua, TXT/INI config, map/runtime data, and other logic. Port/audit agents must inspect both loose PC source and this unpacked tree before declaring behavior/resource missing. Current audit: all 46 real source `.pak` files accounted for, 401,281/401,640 unique entries on disk (99.91%); known undecoded gap is 357 entries with unsupported compression method `0x11000000`. Use `/var/www/vltktool/unpak_tool.py` only for exceptional repair/re-unpack cases.
- Harness durable DB is `/var/www/vltk-mobile/harness/harness.db`.
- When working from `/var/www/vltk-mobile/harness`, run Harness commands in this directory. Do not create or use `/var/www/vltk-mobile/harness.db` at project root.
- If running Harness from `/var/www/vltk-mobile`, set `HARNESS_DB=/var/www/vltk-mobile/harness/harness.db` first.

<!-- HARNESS:END -->

### Không tự ý tạo các tài liệu markdown trừ khi human cho phép
