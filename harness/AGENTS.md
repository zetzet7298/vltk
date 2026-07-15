# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `/var/www/jx-source/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `/var/www/jx-source`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `/var/www/jx-source/pak_unpacked/`.
- Index/audit hiện hành: `/var/www/jx-source/docs/SOURCE_INDEX.md` và `/var/www/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`.
- Với port JX x DHCD: JX là authority cho identity, map, NPC, item, skill base và SPR/VFX/WAV; behavior DHCD chỉ dùng khi có evidence/reverse tương ứng.

## Canonical Skill Root

- Project-local skills chỉ tồn tại tại `/var/www/vltk-mobile/harness/.agents/skills`.
- Đây phải là thư mục thật, không phải symlink; không tạo mirror dưới `.agents`, `.codex`, `.factory`, `.opencode`, `.pi` hoặc `.kiro` ở nơi khác trong project.
- `/var/www/vltk-mobile/harness/.agents` chỉ được chứa thư mục `skills`; không giữ workspace/eval/cache cạnh canonical root.
- Không giữ skill project dưới `.agent`, `bak/skills`, file archive `*.skill` ở project root, hoặc agent-run workspace dưới `/var/www/vltk-mobile/.agents`.
- Skill nằm trong dependency/vendor như `Library`, `.venv` hoặc `bmad` không phải project discovery root; chỉ sửa khi task trực tiếp yêu cầu dependency đó.

## Skill Matrix

### Bắt buộc trước và trong mọi port task

| Skill | Khi nào dùng | Rule |
| --- | --- | --- |
| `srcwalk` | Navigation, tìm file/symbol/flow | Chạy `srcwalk guide` trước; dùng `overview`/`discover`/`show` trước raw `rg` hoặc đọc rộng. |
| `jx-pc-port-rule` | Mọi port PC -> Unity | Đọc và áp dụng source-of-truth rule trước khi sửa code, data, visual hoặc config. |
| `jx-pc-resource-resolver` | Mọi lookup PC resource, đặc biệt PAK/SPR/DAT/UI/WAV/Hash_UID | Resolve path/encoding/Hash_UID bằng tool; cross-check `_labels.json` (`name_vi`) và decode SPR, không đoán tên file. |
| `unity-mcp-orchestrator` | Tạo/sửa GameObject, scene, script hoặc test trong Unity Editor | Dùng MCP cho thao tác Editor và xác nhận compile/PlayMode phù hợp. |

## Canonical PC Rules

- Trước khi port, inspect cả source loose và dữ liệu runtime trong `pak_unpacked`; Unity code hiện tại chỉ là implementation clue, không phải proof.
- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `~/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Evidence phải ghi absolute disk path và hash; logical path một mình không đủ.
- Enumerate toàn bộ candidate hợp lệ, xác định patch/version và active package/load-order winner. Mtime chỉ là tie-breaker sau khi version/load-order tương đương.
- Mỗi selected source/asset phải ghi original path, pack/version, load-order winner, UID nếu có, encoding/path bytes, byte count và SHA-256.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `/var/www/jx-source`.
- Nếu source thiếu hoặc mâu thuẫn: fail closed, đánh dấu provisional/blocked và chạy reverse/ghi ADR phù hợp; không tự bịa behavior, sprite, effect, formula, frame, tên hoặc tọa độ.

### Tool hỗ trợ

- `~/Projects/vltktool/` — resolver/hash toolchain, SPR decoder, PAK unpacker và item runtime.


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
