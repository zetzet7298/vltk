# Agent Instructions

## Project Overview
### User-facing phải là tiếng việt. nếu jx pc là tiếng trung thì phải việt hoá
VLTK Mobile — Port game Võ Lâm Truyền Kỳ (JX Online 3) từ PC sang Unity Mobile.

### Cấu trúc Repo

| Path | Mục đích |
|------|---------|
| `/var/www/vltk-mobile` | Unity mobile client (C# / Unity 2022+) |

Reference files gốc từ PC được lưu trong `Assets/StreamingAssets/Reference/` (Skills.txt, gaibang.lua, Missles.txt, NpcS.txt, KNpc.cpp, SceneDataDef.h).

### PC VLTK source — không có bản "chuẩn"

Theo Cùi Bắp Dev (VLTK community): "chuẩn thì ko có chuẩn, chỉ có tự dev theo ý mình".
Mọi source PC VLTK là private server tự chỉnh theo era:

| Era | Skill cap | Items |
|-----|-----------|-------|
| HKMP (Hoa Kinh Ma Phong) 2008-2010 | ≤120 | xanh max |
| JX 6.0 2012-2015 | ≤135 | tím + thiên tứ |
| Công thành chiến 2018+ | ≤150 | bạch kim + huyền thiết |

Để có "gần CTC nhưng cap 120" → tắt tím + bạch kim trong EquipS.txt.

**Reference hiện tại** (từ jxwin-kinnox, 7.4GB source đã xóa) là PC jxwin era tiêu chuẩn cho VLTK 1.x → 2.x → 3.x, đủ dùng. MOD Vietnam skills (357, 359, 1073, 1074) port theo gaibang.lua + ModSkills.txt có sẵn trong Reference.

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
- `scripts/harness query matrix`

Use the Rust Harness CLI as the main operational tool. Run it through the
stable repo-local entrypoint `scripts/harness`, which uses the prebuilt Rust
binary at `scripts/bin/harness-cli` in installed projects.
<!-- HARNESS:END -->
