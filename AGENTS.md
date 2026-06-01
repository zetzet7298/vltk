# Agent Instructions

## Project Overview

VLTK Mobile — Port game Võ Lâm Truyền Kỳ (JX Online 3) từ PC sang Unity Mobile.

### Cấu trúc Repo

Dự án gồm **2 git repo riêng biệt**:

| Repo | Path | Mục đích |
|------|------|---------|
| `vltk-mobile` | `/var/www/vltk-mobile` | Unity mobile client (C# / Unity 2022+) |
| `jxwin-kinnox` | `/var/www/vltk-mobile/jxwin-kinnox` | PC source gốc (C++ / Lua / JX Online 3) — **read-only reference** |

> `jxwin-kinnox/` được exclude khỏi git của `vltk-mobile` (xem `.gitignore`).

### GitNexus Index

Cả 2 repo đã được index bởi GitNexus:
- `vltk-mobile` — Unity scripts, ProjectSettings, docs
- `jxwin-kinnox` — C++ source (~2000 files), Lua scripts (~1671 files)

Dùng `gitnexus query --repo jxwin-kinnox "..."` để tra cứu logic game gốc.

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
