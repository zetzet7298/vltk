# Domain: Kỹ năng dùng chung, nhập môn và 10 phái

## Định danh và authority

- Domain ID: `DOM-SKL`; DRI: Gameplay Skill; reviewer: QA Parity/Content; P0 catalog + lab, P1 persistence, P2-P4 mở wave.
- Stable key mục tiêu: `(content_version, skill_id, level)`; tên hiển thị không phải khóa.
- `EVID-0039`: `KSkillList.cpp:34-132` có find/add/remove/count; `KPlayerDBFuns.cpp:623-680,933-972` load/save fight và state skill.
- `EVID-0040`: `KSkills.cpp`, `KSkillManager.cpp`, `SkillDef.h`, `KMagicAttrib.h` chứa seam logic/attribute; cần extraction/hash trước khi xác nhận từng skill.
- `EVID-0041`: `bin/client/script/tagnewplayer/skillid.lua:3-101` chứa literal faction skill IDs; `bin/Server/script/skill/{emei,cuiyan,gaibang,kunlun}.lua` và các script khác là evidence content rời rạc, không phải catalog đã reconciled.
- Catalog census hiện có 1.554 row tại `registry/catalogs/skills.jsonl` (SHA-256 `a968a6a8414cbe0698e542a6983e1951c2f70aafb1a2f003cca3685352b8903e`). Nó chỉ chứng minh row tồn tại trong client snapshot; không tự chứng minh row thuộc shared/novice/phái nào, công thức server hay thứ tự runtime.

 ### Static selection đã chứng minh, deep parity vẫn khóa

 - Client `bin/client/settings/Skills.txt` là authority visual/client với 1.554 row, 114 cột, SHA-256 `62142f7e6587e5e39105249b07e35d16ca72efa9f7351d2d274ff57dbf91e097`; server copy khác SHA và có contradiction row `SkillId=836` (`rankex.lua` ở client, `skillstate.lua` ở server), vì vậy client thắng cho catalog/visual nhưng server phải resolve authority logic bằng evidence riêng.
 - `NewPlayerIni00..09.ini` giống nhau ở bootstrap: `[FSKILLS]` canonical `SkillId 53, 1, 2` level 1; `tagnewplayer/skillid.lua:1-104` và các skillbook task map IDs theo 10 faction. Artifact [`skill-static-selection.yaml`](../delivery/case-matrices/skill-static-selection.yaml) pin các join này, không phân loại bằng tên/icon.
 - Top-level faction scripts có static counts: Thiếu Lâm 45, Thiên Vương 51, Đường Môn 55, Ngũ Độc 39, Nga Mi 53, Thúy Yên 55, Cái Bang 34, Thiên Nhẫn 38, Võ Đang 40, Côn Lôn 50; `skill.lua` common có 22/59/497. Tổng 460 faction + 3 novice + 3 shared được static join; 1.088 row còn lại có script special/NPC/partner/event hoặc thiếu script và giữ `DEFERRED`/`BLOCKED`, không promote thành skill người chơi.
 - `KSkills.cpp:2091-2305` là static loader cho SkillId/ReqLevel/target/cost/timing/icon/Spr/sound và `LvlSetting/LvlData`; top-level Lua cùng hash phải trích formula/effect trước khi chuyển `SPECIFIED`. `script/skill/head.lua` định nghĩa `Line`, `Conic`, `Extrac`, `Link` và `GetSkillLevelData`; extractor phải pin preload/hash, giữ `floor(Link(...))` và function-valued entry theo Lua 5.1, không nội suy endpoint tùy ý. Runtime PC golden vẫn bắt buộc cho proc/RNG/frame/audio/SSIM.

## Catalog bắt buộc

| Nhóm | Stable catalog ID | Coverage | Trạng thái |
| --- | --- | --- | --- |
| Dùng chung | `SKL-SHARED-*` | đánh thường, di chuyển/khinh công, hồi thành và utility được extractor phát hiện | `BLOCKED` danh sách canonical |
| Nhập môn | `SKL-NOVICE-*` | toàn bộ skill trước khi gia nhập phái | `BLOCKED` danh sách canonical |
| Thiếu Lâm | `SKL-SHAOLIN-*` | mọi nhánh/level/effect/visual/audio | `BLOCKED` selection; raw rows `DISCOVERED` |
| Thiên Vương | `SKL-TIANWANG-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Đường Môn | `SKL-TANGMEN-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Ngũ Độc | `SKL-WUDU-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Nga Mi | `SKL-EMEI-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Thúy Yên | `SKL-CUIYAN-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Cái Bang | `SKL-GAIBANG-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Thiên Nhẫn | `SKL-TIANREN-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Võ Đang | `SKL-WUDANG-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |
| Côn Lôn | `SKL-KUNLUN-*` | như trên | `BLOCKED` selection; raw rows `DISCOVERED` |

Mỗi record phải có ID/level, phái/nhánh, loại active/passive/state/missile, cost/cooldown/range/target relation, integer formula, RNG stream, effect/state, prerequisite, frame/sprite/audio, source path+hash+locale+package order, DRI, phase, status, test/golden. Không để entity vô chủ.

### Schema deep-spec cho từng skill/level

| Nhóm trường | Trường bắt buộc | Quy tắc khi chưa có evidence |
| --- | --- | --- |
| Định danh | `content_version`, `catalog_ref`, `skill_id`, `skill_level`, `group`, `branch`, `owner` | `group/branch` phải `BLOCKED`, không phân loại bằng tên/icon |
| Học/nâng | `max_level`, `required_character_level`, `prerequisite[]`, `skill_point_cost`, `currency_cost`, `replace/unlock relation` | Không suy diễn từ một row `Skills.txt`; cần server source + save/load evidence |
| Cast | active/passive/aura/state/missile, relation, target mode, range, LOS, resource cost, cooldown, cast/recovery tick | Giá trị thiếu để `null + BLOCKED + blocker`, cấm default production |
| Logic | biểu thức integer/fixed-point, đơn vị, clamp, rounding từng bước, proc order, RNG stream/seed/draw count, stacking/refresh/replace | Prose mô tả skill không phải formula; bắt buộc trace source và runtime oracle |
| Effect | damage/heal/state/missile/child skill, số hit, interval tick, immunity/resist, death/dispel/interrupt interaction | Mỗi effect có thứ tự, source ref và expected event; không gộp nhiều effect thành tổng damage |
| Visual/audio | actor animation, missile, impact, state overlay, SPR UID/path/frame/fps/offset/blend, SFX theo giới tính | Winner Việt/package/hash chưa resolve thì `BLOCKED`; không dùng asset gần giống |
| Nghiệm thu | case IDs, fixture, input/tick/seed, event oracle, snapshot oracle, frame/audio oracle, DRI/reviewer/lifecycle | Thiếu runtime golden chỉ tối đa `FUNCTIONAL`; không gắn `PARITY_DONE` |

Schema máy đọc cho một case nằm tại `delivery/case-matrices/skill-parity-p0.schema.json`; ma trận nhóm/chiều kiểm nằm tại `delivery/case-matrices/skill-parity-p0.json`. Mỗi trường có `status`, `value`, `evidence_refs`, `blocker`; trường `BLOCKED` phải nêu blocker cụ thể.

### Quy tắc phân loại shared, novice và 10 phái

1. Join row client bằng `SkillId`, quan hệ child/start/fly/collide và source row; giữ nguyên raw bytes/hash.
2. Join setting/server script và literal faction IDs bằng evidence path + hash; mâu thuẫn ghi contradiction, không chọn theo tên Việt.
3. Xác định membership/branch/level range từ authority đã reconcile; một row có thể là child/missile nội bộ nhưng chỉ có đúng một owner/disposition.
4. Sinh case theo từng skill canonical và từng level hợp lệ. Không dùng một skill đại diện cho cả phái và không coi 1.554 row đều là skill người chơi có thể học.
5. Shared/novice/phái chỉ chuyển `SPECIFIED` khi selection query tái lập được. Novice `SKILL-1/2/53` đã `SOURCE_PROVEN`; shared và 10 phái vẫn `BLOCKED` trong ma trận P0 cho đến khi candidate joins chứng minh branch/learnability và owner disposition.

### Ma trận case P0 bắt buộc

Mỗi skill/level được cross-product có kiểm soát với: level min/mid/max; cost đủ/thiếu một đơn vị; cooldown ready/còn một tick; range trong/đúng biên/ngoài một đơn vị; target hợp lệ/chết/sai relation; LOS clear/blocked; proc fail/success theo seed; duplicate/out-of-order; disconnect trước result; target chết giữa cast/missile; state stack/refresh/expire/dispel; replay cùng seed. Case không áp dụng phải ghi `NOT_APPLICABLE` kèm rule chứng minh, không được âm thầm bỏ.

Oracle logic so sánh event theo tick và thứ tự (`CastAccepted`, cost, cooldown, missile/state, damage/heal, death); oracle state so snapshot sau từng tick; oracle visual so đúng frame sequence/offset/blend và SSIM từng case `>=0,99`; audio so clip/tick/channel. Tổng damage hoặc screenshot cuối không đủ chứng minh parity.

## Invariant và contract

- Server resolve `Learn/Upgrade/Cast`; client chỉ hiển thị catalog pinned và gửi intent.
- Level/point/prerequisite/cost/cooldown đều validate lại; persistence atomic với point/skill revision.
- Formula, proc order, RNG, stacking và frame sequence chưa có golden phải gắn `BLOCKED` kèm field/blocker/evidence thiếu, không điền theo tên.
- Case chạy trên `FIXTURE-CBT-TRAINING-001`; resistance/hitbox/rounding/PC visual oracle của fixture đang `BLOCKED`, vì vậy fixture hiện chỉ khóa baseline DevHarness chứ chưa phải PC golden.
- P0: extractor tạo coverage 100% discovered + lab mọi skill. P1: save/load/reconnect. P2: world PvE skill interactions. P3: party/guild/pet modifiers. P4: PvP/event normalization.
- `TEST-SKL-001`: catalog uniqueness/owner/source hash; `TEST-SKL-002`: formula boundary + deterministic RNG; `TEST-SKL-003`: logic 100% và SSIM >=0,99/case, hiện runtime `BLOCKED`.
