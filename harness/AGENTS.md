
# Agent Instructions

Add project-specific agent instructions here.

<!-- HARNESS:BEGIN -->

## Harness

This repo uses Harness. Before work, read:

- `README.md`
- `docs/HARNESS.md`
- `docs/FEATURE_INTAKE.md`
- `docs/ARCHITECTURE.md`
- `docs/CONTEXT_RULES.md`
- `docs/TOOL_REGISTRY.md`
- `scripts/bin/harness-cli query matrix` on macOS/Linux, or `.\scripts\bin\harness-cli.exe query matrix` on Windows

Use the Rust Harness CLI at `scripts/bin/harness-cli` on macOS/Linux or
`scripts/bin/harness-cli.exe` on Windows as the main operational tool. Before a
step that could use an external tool, run `scripts/bin/harness-cli query tools --capability <name> --status present` to see what is equipped; an absent
capability is a clean skip.

<!-- HARNESS:END -->


## Game hiện tại đang port từ game PC /var/www/vltksource_new vì thế nên trong quá trình port thì phải đọc hiểu code bên PC

Trước khi bắt tay làm bất cứ việc gì (fix bug, port feature, dùng API/library lạ), **PHẢI dùng exa (`exa_web_search_exa`/`exa_web_fetch_exa`) và deepwiki (`mcp_deepwiki_deepwiki_fetch`) để research** cách làm chuẩn — không đoán, không vá mò. Research xong mới implement. Xong việc thì **commit all change + push**.

---



## Skill Matrix

### 🔴 Bắt buộc — Dùng trước & trong mọi port task

| Skill                      | Khi nào dùng                                               | Tóm tắt                                                                        |
| -------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `jx-pc-port-rule`        | **TRƯỚC MỌI PORT TASK** — không ngoại lệ        | Ép inspect PC source trước, port 100% từ PC. Không đoán, không tự chế. |
| `srcwalk`                | Code navigation, tìm symbol/file/flow                       | Repo map, symbol search, callers/callees, deps. Ưu tiên hơn grep/read.        |
| `unity-mcp-orchestrator` | Tạo/sửa GameObject, scene, script, test trong Unity Editor | Điều khiển Unity Editor qua MCP — CRUD scene, script, component, test.       |

### 🔴 Bắt buộc — Test run rule (EditMode)

**KHÔNG BAO GIỜ** chạy full EditMode suite (4049 tests, ~4 phút) trong dev loop.
**LUÔN** filter theo category / namespace khi chạy test:

```python
# Mặc định khi dev 1 phái/skill cụ thể — chỉ chạy tests liên quan (~1-2s)
unityMCP___run_tests(mode="EditMode", category_names=["<PháiTên>"])

# Skip slow sprite tests khi không cần verify visual
unityMCP___run_tests(mode="EditMode", category_names=["!Slow"])

# Filter namespace khi không có category (regex groupNames)
unityMCP___run_tests(mode="EditMode",
    group_names=["^VLTK\\.Tests\\.Sandbox\\.PhaiTenTests\\."])
```

Full suite CHỈ chạy khi:

- Trước khi `git push` (final gate).
- Sau khi sửa code shared (`PcCombatCatalogFactory`, `CombatRuntimeService`, `SkillEffectVisualService`, asmdef).

Khi tạo test file mới, PHẢI add `[TestFixture, Category("<PháiTên>")]` ở class-level
(class-level áp dụng cho all methods trong class — verified qua NUnit docs).
Nếu test chạm visual/sprite decode (chậm), add `[TestFixture, Category("Slow")]` để
skip được khi cần.

Categories hiện có (cập nhật 2026-06-19):

- `CaiBang` — 12 fixtures, 82 tests (Phi Long, Bổng Đả, Kháng Long, Thiên Hạ Vô Cẩu, ...)
- `Slow` — MountVisualTests, MalePlayerVisualTests (sprite decode chậm nhất)
- Khi port phái mới (Thiếu Lâm, Võ Đang, ...) → add category riêng ngay từ đầu.

Single test (debug nhanh):

```python
unityMCP___run_tests(mode="EditMode",
    test_names=["VLTK.Tests.Sandbox.CaiBangCombatParityTests.CaiBang_122_FireDamageMaxesAtPc215_AtLevel20"])
```

Shared catalog cache: `TestCatalogCache.NoviceAndCaiBang` (avoid rebuild ~50ms/call).
Tests KHÔNG mutate catalog mới dùng cache; tests mutate (vd. search-and-remove) phải
gọi `PcCombatCatalogFactory.CreateXxxCatalog()` trực tiếp để lấy fresh copy.

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

- `~/Projects/vltktool/` — Bộ công cụ Python: SPR decoder, PAK unpacker, item runtime, CMS web

## Test run commands (EditMode)

Full suite (4049 tests, ~4 phút) — chỉ dùng trước khi push/CI.

Category filter (nhanh hơn ~140×, dùng khi dev):

```python
# Cái Bang only — 82 tests, ~1.7 giây
unityMCP___run_tests(mode="EditMode", category_names=["CaiBang"])

# Skip slow sprite tests
unityMCP___run_tests(mode="EditMode", category_names=["!Slow"])

# Filter theo namespace (regex groupNames)
unityMCP___run_tests(mode="EditMode",
    group_names=["^VLTK\\.Tests\\.Sandbox\\.CaiBangCombatParityTests\\."])

# Single test
unityMCP___run_tests(mode="EditMode",
    test_names=["VLTK.Tests.Sandbox.CaiBangCombatParityTests.CaiBang_122_FireDamageMaxesAtPc215_AtLevel20"])
```

Categories đã add (2026-06-19):

- `CaiBang` — 12 fixtures, 82 tests (Phi Long, Bổng Đả, Kháng Long, Thiên Hạ Vô Cẩu, ...)
- `Slow` — MountVisualTests, MalePlayerVisualTests (sprite decode chậm)

Shared catalog cache: `TestCatalogCache.NoviceAndCaiBang` (avoid rebuild ~50ms/call).
