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

### Project Overview

### User-facing phải là tiếng việt. nếu jx pc là tiếng trung thì phải việt hoá

VLTK Mobile — Port game Võ Lâm Truyền Kỳ (JX Online 3) từ PC sang Unity Mobile.

### Cấu trúc Repo

| Path                     | Mục đích                            |
| ------------------------ | -------------------------------------- |
| `/var/www/vltk-mobile` | Unity mobile client (C# / Unity 2022+) |

Reference files gốc từ PC được lưu trong `Assets/StreamingAssets/Reference/` (Skills.txt, gaibang.lua, Missles.txt, NpcS.txt, KNpc.cpp, SceneDataDef.h).

### Tool hỗ trợ

- `/var/www/vltktool/` — Bộ công cụ Python: SPR decoder, PAK unpacker, item runtime, CMS web
