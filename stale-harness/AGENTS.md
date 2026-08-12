# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `/var/www/jx-pc/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `/var/www/jx-pc`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `/var/www/jx-pc/pak_unpacked/`.
- Index/audit hiện hành: `/var/www/jx-pc/docs/SOURCE_INDEX.md` và `/var/www/jx-pc/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/`.
- Với port JX x DHCD: JX là authority cho identity, map, NPC, item, skill base và SPR/VFX/WAV; behavior DHCD chỉ dùng khi có evidence/reverse tương ứng.

## Canonical Skill Root

- Project-local skills chỉ tồn tại tại `/var/www/vltk-mobile/harness/.agents/skills`.

## Skill Matrix

### Bắt buộc trước và trong mọi port task

| Skill | Khi nào dùng | Rule |
| --- | --- | --- |
| `/home/zet/.agents/skills/srcwalk/SKILL.md` | Navigation, tìm file/symbol/flow | Chạy `srcwalk guide` trước; dùng `overview`/`discover`/`show` trước raw `rg` hoặc đọc rộng. |
| `/home/zet/.codex/skills/reverse-engineering` | Mọi reverse DHCD, Unity/IL2CPP, APK/smali, ARM64 `.so`, DODAB1/AssetBundle, native callee hoặc parity claim | Đọc project-local skill trước; route qua toolkit `/var/www/reverse-skill`; hash-first, map control flow, phân biệt `proven` / `high-confidence reconstruction` / `product decision`, không gọi suy luận là parity. |
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
- Không sửa bất kỳ file nào dưới `/var/www/jx-pc`.
- Nếu source thiếu hoặc mâu thuẫn: fail closed, đánh dấu provisional/blocked và chạy reverse/ghi ADR phù hợp; không tự bịa behavior, sprite, effect, formula, frame, tên hoặc tọa độ.

### Tool hỗ trợ

- `~/Projects/vltktool/` — resolver/hash toolchain, SPR decoder, PAK unpacker và item runtime.
- `/var/www/reverse-skill/` — canonical reverse workflow/tool registry; project entry skill là `.agents/skills/reverse-engineering/SKILL.md`.
- Reverse executables đã đăng ký: `/home/zet/tools/jadx/bin/jadx`, `/usr/bin/apktool`, `/usr/bin/adb`, `/home/zet/.local/bin/frida`, `/usr/bin/r2`, `/usr/bin/rabin2`, `/usr/bin/python3`. Không bootstrap lại tool được đánh dấu available trong `/var/www/reverse-skill/skills/tool-index.md`.
