# Agent Instructions

### So sánh Tỉ lệ màn hình & Quy đổi Giao diện (PC vs. Mobile)

* **Màn hình PC gốc (`vltksource_new`)**: Tỉ lệ chuẩn **4:3**. Độ phân giải `800x600` hoặc `1024x768` (mặc định). Mọi tọa độ và layout trong file INI được định vị tuyệt đối theo khung 4:3 này.
* **Màn hình Mobile (`vltk-mobile`)**: Tỉ lệ chuẩn màn hình rộng **16:9**, sử dụng độ phân giải thiết kế làm mốc là **`1920x1080`**.
* **Công thức Quy đổi & Thiết kế**:
  * **Scale Trục Ngang (X)**: $\approx 1920 / 1024 = 1.875$
  * **Scale Trục Dọc (Y)**: $\approx 1080 / 768 = 1.406$
  * **Nguyên tắc bố cục**: Không được nhân tỷ lệ thô trực tiếp cho ảnh (sẽ gây méo hình ảnh gốc). Hãy giữ nguyên tỉ lệ ảnh (Aspect Ratio), áp dụng neo góc màn hình (Anchor) theo cụm HUD (topbar neo Top-Center, minimap neo Top-Right, chat neo Bottom-Left, button bar neo Bottom-Center), và co giãn theo tỉ lệ màn hình thực tế của thiết bị. **Lưu ý**: `curSalx`/design-resolution từng được chép nhầm từ bản port C++ `jx-cocos` (không phải PC gốc) — đã gỡ bỏ; nguồn quy đổi duy nhất là `/var/www/vltksource_new` (file INI trong `pak_unpacked/1024` + `config.ini`).

Trước khi bắt tay làm bất cứ việc gì (fix bug, port feature, dùng API/library lạ), **PHẢI dùng exa (`exa_web_search_exa`/`exa_web_fetch_exa`) và deepwiki (`mcp_deepwiki_deepwiki_fetch`) để research** cách làm chuẩn — không đoán, không vá mò. Research xong mới implement. Xong việc thì **commit all change + push**.

### 🔴 Tìm SPR / PAK trong `vltksource_new` (PC) — dùng skill `jx-pc-resource-resolver`

Khi cần lấy bất kỳ **sprite / pak / UI art** nào từ PC (`/var/www/vltksource_new`), **BẮT BUỘC** dùng skill `jx-pc-resource-resolver` (`harness/.pi/skills/jx-pc-resource-resolver/SKILL.md`) để:
- Đọc file INI (mã hoá **GBK**, đường dẫn tiếng Trung vd `đườn dẫn đến SPR`) → tính **JX Pack Hash** → ra tên file hex thật (vd `đườn dẫn đến hash.spr`) trên `pak_unpacked/*/unknown/`.
- **Chọn đúng bản TIẾNG VIỆT**, không nhầm bản tiếng Trung: VLTK PC có nhiều phiên bản SPR song song (CN + VI). Cross-check `name_vi` trong `pak_unpacked/_labels.json` + decode SPR (`~/Projects/vltktool`) để kiểm chứng chữ trên ảnh là tiếng Việt.

⚠️ **Không bao giờ đoán mò tên file SPR** — phải tính hash. Sanity check: `đườn dẫn đến bút túi.spr` → `175edefc.spr`, `đườn dẫn đến thanh công cụ.spr` → `ebb69f9b.spr`.

---

## Skill Matrix

### 🔴 Bắt buộc — Dùng trước & trong mọi port task

| Skill                      | Khi nào dùng                                               | Tóm tắt                                                                        |
| -------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `jx-pc-port-rule`        | **TRƯỚC MỌI PORT TASK** — không ngoại lệ        | Ép inspect PC source trước, port 100% từ PC. Không đoán, không tự chế. |
| `srcwalk`                | Code navigation, tìm symbol/file/flow                       | Repo map, symbol search, callers/callees, deps. Ưu tiên hơn grep/read.        |
| `unity-mcp-orchestrator` | Tạo/sửa GameObject, scene, script, test trong Unity Editor | Điều khiển Unity Editor qua MCP — CRUD scene, script, component, test.       |
| `jx-pc-resource-resolver` | **MỌI lúc tìm SPR/PAK/UI trong `vltksource_new` (PC)** — không ngoại lệ | Hash CN (GBK) → tên file hex thật trên disk; chọn đúng bản **tiếng VIỆT**, không nhầm bản tiếng Trung. Cross-check `_labels.json` (`name_vi`) + decode SPR kiểm chứng. |

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
# Mặc định khi dev 1 phái/skill cụ thể — chỉ chạy tests liên quan (~1-2s)
unityMCP___run_tests(mode="EditMode", category_names=["<PháiTên>"])

# Skip slow sprite tests khi không cần verify visual
unityMCP___run_tests(mode="EditMode", category_names=["!Slow"])

# Filter namespace khi không có category (regex groupNames)
unityMCP___run_tests(mode="EditMode",
    group_names=["^VLTK\\.Tests\\.Sandbox\\.PhaiTenTests\\."])
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

- `~/Projects/vltktool/` — Bộ công cụ Python: SPR decoder, PAK unpacker, item runtime

## Test run commands (EditMode)

Full suite (4049 tests, ~4 phút) — chỉ dùng trước khi push/CI.

Category filter (nhanh hơn ~140×, dùng khi dev):

```python
# Mặc định khi dev 1 phái/skill cụ thể — chỉ chạy tests liên quan (~1-2s)
unityMCP___run_tests(mode="EditMode", category_names=["<PháiTên>"])

# Skip slow sprite tests khi không cần verify visual
unityMCP___run_tests(mode="EditMode", category_names=["!Slow"])

# Filter namespace khi không có category (regex groupNames)
unityMCP___run_tests(mode="EditMode",
    group_names=["^VLTK\\.Tests\\.Sandbox\\.PhaiTenTests\\."])
```

Categories đã add (2026-06-19):

- `CaiBang` — 12 fixtures, 82 tests (Phi Long, Bổng Đả, Kháng Long, Thiên Hạ Vô Cẩu, ...)
- `Slow` — MountVisualTests, MalePlayerVisualTests (sprite decode chậm)

Shared catalog cache: `TestCatalogCache.NoviceAndCaiBang` (avoid rebuild ~50ms/call).


<!-- HARNESS:END --
