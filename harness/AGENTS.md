# Agent Instructions


## Tools RULES:

- Always use `srcwalk` skill: for best codebase/files/dirs exploration, discover, searching.
- Always prefer `srcwalk` CLI over read/glob/grep tool.
- Prefer `fd` over `find`.
- Prefer `rg` over `grep`.

## Skill Matrix

### 🔴 Bắt buộc — Dùng trước & trong mọi port task

| Skill | Khi nào dùng | Tóm tắt |
|-------|-------------|--------|
| `jx-pc-port-rule` | **TRƯỚC MỌI PORT TASK** — không ngoại lệ | Ép inspect PC source trước, port 100% từ PC. Không đoán, không tự chế. |
| `srcwalk` | Code navigation, tìm symbol/file/flow | Repo map, symbol search, callers/callees, deps. Ưu tiên hơn grep/read. |
| `unity-mcp-orchestrator` | Tạo/sửa GameObject, scene, script, test trong Unity Editor | Điều khiển Unity Editor qua MCP — CRUD scene, script, component, test. |

### 🟡 Theo task — Port cụ thể

| Skill | Khi nào dùng | Tóm tắt |
|-------|-------------|--------|
| `jx-map-port` | Port map, Region_C.dat, terrain, minimap, click-to-move | Port map PC→Unity: geometry, SPR terrain, minimap, toạ độ PC. |
| `jx-enemy-port` | Port enemy/mob/NPC spawn, NpcS.txt, Region_S, võ sư/cọc gỗ | Spawn enemy từ PC data, SPR visuals, 8-way anim, HP/nameplate. |
| `jx-hud-port` | Port HUD/UI khớp PC — bars, minimap, hotbar, icons | HP/MP/EXP/stamina bars, minimap, chat, hotbar, Ui3 SPR art. |
| `jx-player-visual` | Player avatar — layered SPR, giáp/vũ khí/cưỡi ngựa | Body/head/hair/hand/weapon layers, 8-way direction, sprite catalog. |

### 🟢 Hỗ trợ — Khi cần

| Skill | Khi nào dùng | Tóm tắt |
|-------|-------------|--------|
| `diagnose` | Bug khó, regression, crash | Reproduce → minimise → hypothesise → instrument → fix → regression-test. |
| `tdd` | Viết feature/fix có test | Red-green-refactor loop. Test trước, code sau. |
| `review` | Review code branch/PR | 2 trục: Standards (coding std) + Spec (đúng yêu cầu issue/PRD). |
| `understand-explain` | Cần giải thích deep file/function/module | Deep-dive explanation cụ thể. |
| `understand-chat` | Hỏi đáp kiến trúc/flow qua knowledge graph | Q&A về codebase. |
| `handoff` | Compact context cho agent tiếp theo | Tóm tắt conversation → handoff document. |
| `pi-subagents` | Delegate task cho sub-agent, chain, parallel | Fan-out công việc, review song song, pipeline. |
| `prototype` | Test nhanh ý tưởng UI/logic trước khi commit | Prototype throwaway — UI variations hoặc terminal state app. |

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

<!-- HARNESS:BEGIN -->

## Harness

This repo uses Harness. Before work, read:

- `README.md`
- `docs/HARNESS.md`
- `docs/FEATURE_INTAKE.md`
- `docs/ARCHITECTURE.md`
- `docs/CONTEXT_RULES.md`
- `docs/PORT_STATUS.md` — **BẮT BUỘC** — Checklist trạng thái port PC→Mobile, luôn đọc trước khi bắt story mới để biết gì đã làm/g chưa làm.
- `scripts/harness query matrix`

### Port Source-of-Truth Rule

`docs/PORT_STATUS.md` là bảng chân trị (source of truth) cho tiến độ port.

- **Trước mỗi story**: đọc `docs/PORT_STATUS.md`, xác nhận section liên quan, check ✅/🔄/☐.
- **Sau khi implement**: update status trong PORT_STATUS.md (☐ → 🔄 → ✅).
- **Không đánh dấu ✅** nếu thiếu tests hoặc chưa verify.
- PC reference docs: `/var/www/vltksource_new/docs/port_docs/` (00–17).
- Mỗi story trong harness DB có field `notes` với `port-docs:` reference đến các file port_docs liên quan.

Use the Rust Harness CLI as the main operational tool. Run it through the
stable repo-local entrypoint `scripts/harness`, which uses the prebuilt Rust
binary at `scripts/bin/harness-cli` in installed projects.

Durable Harness DB rule:

- Chỉ dùng một durable Harness DB: `/var/www/vltk-mobile/harness/harness.db`.
- Khi làm việc từ harness repo, chạy `scripts/harness ...` trong `/var/www/vltk-mobile/harness`.
- Không tạo hoặc dùng `/var/www/vltk-mobile/harness.db` ở project root.
- Nếu bắt buộc chạy từ `/var/www/vltk-mobile`, set `HARNESS_DB=/var/www/vltk-mobile/harness/harness.db` trước lệnh Harness.

<!-- HARNESS:END -->

### Không tự ý tạo các tài liệu markdown trừ khi human cho phép
