# Catalog corpus PC đã phát hiện

Catalog được sinh deterministic bằng `harness/scripts/generate-jx-spec-catalog.py`.
Encoding bảng text được chọn qua `vltktool.decode_item_texts_vi.decode_best`;
script không tự tính JX UID, không đọc index trong PAK và không tự chọn winner.
Mỗi record có `phase`, `acceptance_id`, `owner_domain`, `disposition` và
`evidence_status`; `BLOCKED_*` là blocker thật, không phải placeholder winner.

| Catalog | Số record hiện tại | Ý nghĩa |
| --- | ---: | --- |
| `skills.jsonl` | 1.554 | Mọi row và đủ 114/114 cột trong active client `Skills.txt` |
| `missiles.jsonl` | 441 | Mọi row trong active client `Missles.txt` |
| `npcs.jsonl` | 2.152 | Mọi row trong active client `Npcs.txt` |
| `goods.jsonl` | 707 | Mọi row trong active client `Goods.txt` |
| `maps.jsonl` | 987 | Mọi map ID được khai báo trong active client `MapList.ini` |
| `setting-files.jsonl` | 293 | Client và hai server settings snapshot, gồm TXT/INI |
| `ui-files.jsonl` | 200 | UI TXT/INI trong client snapshot |
| `ui-spr-assets.jsonl` | 1.345 | Mọi SPR loose trong `client/Ui` và `client/Spr`; có phân lớp scope nhưng winner/UID còn `BLOCKED` |
| `avatar-asset-candidates.jsonl` | 91 | 59 SPR và 32 bảng/config ứng viên avatar; là view dẫn xuất, có chồng lấp catalog nguồn |
| `audio-assets.jsonl` | 72 | 36 clip active loose và 36 bản `bak`; bản `bak` là `defer_reference_only` |
| `ui-lua-scripts.jsonl` | 9 | Lua dưới `client/Ui` hoặc component `script/ui` |
| `quest-candidates.jsonl` | 1.232 | Candidate theo rule path hẹp, chưa chứng minh behavior/load/call graph |
| `deferred-scripts.jsonl` | 9 | Script GM/backoffice hoặc subsystem PC ngoài phạm vi, vẫn census nhưng không blanket port |
| `lua-scripts.jsonl` | 8.451 | Lua gameplay/event còn lại sau khi partition |
| `packages.jsonl` | 31 | Từng package theo order `package.ini`: 27 present đã đọc UID index, 4 missing, SHA-256 nếu present |

Tổng Lua unique là 9.701 path và partition không chồng lấp: 8.451 general,
1.232 quest-candidate, 9 UI và 9 deferred. `avatar-asset-candidates.jsonl` là
candidate view dẫn xuất nên không được cộng vào tổng file unique.

`source-snapshot.yaml` pin package manifest, từng package configured/present/path/
size/SHA-256/blocker, source/tool revision, dirty state, generator hash và SHA-256
từng output. Chạy lại trên cùng snapshot phải cho cùng hash; timestamp không nằm
trong payload. `index.yaml.coverage` ghi actual aggregate theo `entity_type` và
không che `unresolved`.

Với 27 PAK hiện diện, generator dùng `vltktool.resolve_uid.pak_uids` để census
164.599 unique UID theo từng package và pin hash của danh sách UID canonical.
Điều này chỉ chứng minh index có thể đọc; không chốt winner vì package ưu tiên
cao nhất `vltkcache.pak` cùng `settings.pak`, `ui.pak`, `script.pak` đang thiếu,
và candidate path harvester không cam kết coverage đầy đủ.

## Giới hạn claim

- Record `DISCOVERED / UNVERIFIED` chỉ chứng minh tồn tại trong snapshot, chưa
  chứng minh behavior runtime hoặc đã port.
- Path/SPR/audio/Lua reference còn cần vltktool resolver để ghi logical path bytes,
  UID, package first-match winner, raw/decoded hash và locale. Loose-file hash
  không chứng minh runtime winner. Không được tự giải mã chuỗi path mixed encoding.
- Quest classification chỉ dùng component/token đường dẫn deterministic. Nó không
  phân tích Lua AST, recursive call, host API hay tên tiếng Trung, vì vậy mọi
  candidate vẫn `BLOCKED_BEHAVIOR_CLASSIFICATION`.
- Avatar classification chỉ tạo tập ứng viên từ token path và bảng `*Res`; không
  chứng minh part/action/direction layer. Gate `TEST-AVATAR-001` vẫn cần resolver
  và runtime matrix.
- Catalog row đầy đủ không thay thế deep behavior spec. P0/P1 phải trace tới
  domain requirement, contract, implementation, test và golden.
- Map `MAP-53` có `canonical_runtime_id=53`, `alias_allowed=false`; mọi remap sang
  79 là release failure.
