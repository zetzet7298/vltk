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

### Port Map (đã giải quyết 99%)

Để port BẤT KỲ map nào từ PC sang Unity (terrain + buildings + decor + art thật),
dùng skill **`jx-map-port`** (`.kiro/skills/jx-map-port/`). Nó đóng gói toàn bộ pipeline
đã mất nhiều session để tìm ra — quan trọng nhất là **hash `g_FileName2Id` dùng SIGNED
byte** (không phải unsigned) để resolve path chữ Hán trong `maps.pak`/`spr.pak`.

```bash
python3 .kiro/skills/jx-map-port/scripts/list_maps.py <tên>        # tìm map + bounds
python3 .kiro/skills/jx-map-port/scripts/jx_map_port.py \
  --map-name '两湖区\巴陵县' --project-map-id 79 --unity-root /var/www/vltk-mobile
```

Đọc `references/pitfalls.md` trước khi thử cách khác — mọi ngõ cụt đã được ghi lại.

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

## Code Search

Use `semble search` to find code by describing what it does or naming a symbol/identifier, instead of grep:

```bash
semble search "authentication flow" ./my-project
semble search "save_pretrained" ./my-project
semble search "save model to disk" ./my-project --top-k 10
```

Use `--content docs` to search documentation and prose, `--content config` for config files (yaml, toml, etc.), or `--content all` to search code, docs, and config:

```bash
semble search "deployment guide" ./my-project --content docs
semble search "database host port" ./my-project --content config
semble search "authentication" ./my-project --content all
```

Use `semble find-related` to discover code similar to a known location (pass `file_path` and `line` from a prior search result):

```bash
semble find-related src/auth.py 42 ./my-project
```

`path` defaults to the current directory when omitted; git URLs are accepted. If `semble` is not on `$PATH`, use `uvx --from "semble[mcp]" semble` in its place.

### Workflow

1. Start with `semble search` to find relevant chunks.
2. Use `--content docs` for documentation, `--content config` for config files, or `--content all` for everything.
3. Inspect full files only when the returned chunk is not enough context.
4. Optionally use `semble find-related` with a promising result's `file_path` and `line` to discover related implementations.
5. Use grep only when you need exhaustive literal matches or quick confirmation of an exact string.

<!-- context7 -->
Use the `ctx7` CLI to fetch current documentation whenever the user asks about a library, framework, SDK, API, CLI tool, or cloud service -- even well-known ones like React, Next.js, Prisma, Express, Tailwind, Django, or Spring Boot. This includes API syntax, configuration, version migration, library-specific debugging, setup instructions, and CLI tool usage. Use even when you think you know the answer -- your training data may not reflect recent changes. Prefer this over web search for library docs.

Do not use for: refactoring, writing scripts from scratch, debugging business logic, code review, or general programming concepts.

## Steps

1. Resolve library: `npx ctx7@latest library <name> "<user's question>"` — use the official library name with proper punctuation (e.g., "Next.js" not "nextjs", "Customer.io" not "customerio", "Three.js" not "threejs")
2. Pick the best match (ID format: `/org/project`) by: exact name match, description relevance, code snippet count, source reputation (High/Medium preferred), and benchmark score (higher is better). If results don't look right, try alternate names or queries (e.g., "next.js" not "nextjs", or rephrase the question)
3. Fetch docs: `npx ctx7@latest docs <libraryId> "<user's question>"`
4. Answer using the fetched documentation

You MUST call `library` first to get a valid ID unless the user provides one directly in `/org/project` format. Use the user's full question as the query -- specific and detailed queries return better results than vague single words. Do not run more than 3 commands per question. Do not include sensitive information (API keys, passwords, credentials) in queries.

For version-specific docs, use `/org/project/version` from the `library` output (e.g., `/vercel/next.js/v14.3.0`).

If a command fails with a quota error, inform the user and suggest `npx ctx7@latest login` or setting `CONTEXT7_API_KEY` env var for higher limits. Do not silently fall back to training data.
<!-- context7 -->
