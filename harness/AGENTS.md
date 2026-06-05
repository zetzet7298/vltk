# Agent Instructions


## Tools RULES:

- Always use `srcwalk` skill: for best codebase/files/dirs exploration, discover, searching.
- Always prefer `srcwalk` CLI over read/glob/grep tool.
- Prefer `fd` over `find`.
- Prefer `rg` over `grep`.

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
