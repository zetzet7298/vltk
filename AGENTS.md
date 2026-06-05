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

## Harness DB Rule

- Chỉ dùng một durable Harness DB: `/var/www/vltk-mobile/harness/harness.db`.
- Chạy Harness CLI từ `/var/www/vltk-mobile/harness` bằng `scripts/harness ...`.
- Không `init`, `migrate`, `intake`, `story`, `trace` hoặc `query` vào `/var/www/vltk-mobile/harness.db` ở repo root.
- Nếu đang đứng ở `/var/www/vltk-mobile`, dùng `HARNESS_DB=/var/www/vltk-mobile/harness/harness.db scripts/harness ...` hoặc chuyển `workdir` sang `/var/www/vltk-mobile/harness`.


## Port Status Rule

- **Trước mỗi port task**: đọc `/var/www/vltk-mobile/harness/docs/PORT_STATUS.md` — checklist trạng thái port PC→Mobile.
- PC reference docs: `/var/www/vltksource_new/docs/port_docs/` (00–17).
- Sau khi implement port, update PORT_STATUS.md: ☐ → 🔄 → ✅ (chỉ ✅ khi có tests pass).
- Mỗi story trong harness DB có `port-docs:` reference đến PC docs liên quan.

### Không tự ý tạo các tài liệu markdown trừ khi human cho phép
