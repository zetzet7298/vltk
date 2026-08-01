# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `C:/Projects/jx-source/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `C:/Projects/jx-source`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `C:/Projects/jx-source/pak_unpacked/`.
- Index/audit hiện hành: `C:/Projects/jx-source/docs/SOURCE_INDEX.md` và `C:/Projects/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `C:/Projects/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`.

## Canonical PC Rules

- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `C:/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Với SPR thì nên dùng hash/disk path không nên dùng logical path. logical path Chỉ để hiểu logic,behavior code của Pc
- SPR có text/UI: luôn kiểm tra `bin/client/package.ini` để chọn **winner theo package priority** (ví dụ Vietnamese override `update01.pak` có thể ghi đè `spr.pak`); không dùng fallback tiếng Trung chỉ vì logical path trùng.
- Resolve logical path → UID bằng `vltktool resolve_uid.py`, extract đúng frame winner bằng `vltktool extract_item_spr.py`, rồi `cmp` với PNG Unity và lưu UID/package/frame + SHA-256 vào provenance trước khi dùng.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `C:/Projects/jx-source`.


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
