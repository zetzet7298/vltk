# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `/var/www/jx-source/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `/var/www/jx-source`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `/var/www/jx-source/pak_unpacked/`.
- Index/audit hiện hành: `/var/www/jx-source/docs/SOURCE_INDEX.md` và `/var/www/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`.

## Canonical PC Rules

- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `~/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Với SPR thì nên dùng hash/disk path không nên dùng logical path. logical path Chỉ để hiểu logic,behavior code của Pc
- SPR có text/UI: luôn kiểm tra `bin/client/package.ini` để chọn **winner theo package priority** (ví dụ Vietnamese override `update01.pak` có thể ghi đè `spr.pak`); không dùng fallback tiếng Trung chỉ vì logical path trùng.
- Resolve logical path → UID bằng `vltktool resolve_uid.py`, extract đúng frame winner bằng `vltktool extract_item_spr.py`, rồi `cmp` với PNG Unity và lưu UID/package/frame + SHA-256 vào provenance trước khi dùng.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `/var/www/jx-source`.

## Unity MCP Skill Matrix

Use `/var/www/vltk-mobile/harness/.agents/skills/unity-mcp-orchestrator/SKILL.md` Orchestrate Unity Editor via MCP (Model Context Protocol) tools and resources. Use when working with Unity projects through MCP for Unity - creating/modifying GameObjects, editing scripts, managing scenes, running tests, or any Unity Editor automation. Provides best practices, tool schemas, and workflow patterns for effective Unity-MCP integration.

<!-- HARNESS:BEGIN -->
## Harness

Choose the request class before any Harness operation.

- When the requested outcome is only an answer, explanation, review, diagnosis,
  plan, or status report: inspect only the material needed to respond. Keep the
  task read-only. Do not bootstrap, initialize or migrate a database, record
  intake, or record a trace.
- When the user explicitly asks to change, build, fix, or write repository
  artifacts: first run `scripts/bootstrap-harness.sh`
  on macOS/Linux or `.\scripts\bootstrap-harness.ps1` on Windows. Then use
  `docs/FEATURE_INTAKE.md` to classify and record the request, query
  `scripts/bin/harness-cli query matrix --active --summary` on macOS/Linux or
  `.\scripts\bin\harness-cli.exe query matrix --active --summary` on Windows,
  and retrieve only the lane- and task-specific context described in
  `docs/CONTEXT_RULES.md`.
<!-- HARNESS:END -->
