# Domain: Tiến triển nhân vật

## Định danh và phạm vi

- Domain ID: `DOM-PRG`; DRI: Gameplay Progression; reviewer: Backend/QA; sâu P1 cấp 1-200.
- Sở hữu EXP, level, điểm thuộc tính/kỹ năng, base stat, gia nhập/rời phái và checkpoint progression.

## Bằng chứng as-is

- `EVID-0036`: `KPlayerDBFuns.cpp:758-785` lưu điểm thuộc tính/kỹ năng, bốn thuộc tính, luck, EXP/level/translife, leadership và faction.
- `EVID-0037`: `KPlayerFaction.cpp:40-158` có series, add/leave faction và đọc tên/ID; rule gia nhập/rời chưa được chứng minh.
- `EVID-0038`: `KNpcDeathCalcExp.cpp` tồn tại seam tính EXP chết; exact formula/party split cần extraction/golden.

## Invariant và contract

- Level mục tiêu P1 từ 1 đến 200. Static authority đã xác định: `GameDataDef.h:69` đặt `MAX_LEVEL=200`, `KPlayerSet.cpp:453-502` đọc `level_exp.txt`/`level_add.txt`, và `KPlayer.cpp:2616-2629` áp dụng `next_exp = base[level-1] + add[translife][level-1]`, cộng `+5` attribute point và `+1` skill point mỗi level. Việc chọn đúng variant source vẫn `BLOCKED [CẦN XÁC NHẬN]`; owner Gameplay/Content; gỡ block bằng Reconciler pin snapshot/hash và reviewer.
- EXP grant có `grant_id` idempotent; level-up, points và derived stats commit cùng transaction.
- Không cho client đặt level/EXP. Allocate point validate available balance và revision.
- `GrantExperience` chỉ là command nội bộ từ combat/quest; client wire dùng `ProgressionCommand` với `ALLOCATE_ATTRIBUTE`, `LEARN_SKILL`, `UPGRADE_SKILL`, `JOIN_FACTION`, `LEAVE_FACTION`, luôn có `expected_revision`. Server trả `ProgressionEvent`; không cho client đặt EXP/level.
- P2-P4 catalog reincarnation/rank/reputation/leadership nếu extractor phát hiện; chưa thuộc vertical slice và không được bỏ owner.

## Bảng static progression đã trích

 - Candidate server/client canonical `bin/Server/settings/npc/player/level_exp.txt` và nested `bin/Server/Server/settings/...` byte-identical, SHA-256 `276e3dc3f18121aa3147b61527ee22d33ed916ce27b8c086a614fe9a99487fa`; có header và 200 row level. Candidate Utility/Run SHA `6bc0...` và `player.1` SHA `bada...` khác, không được trộn vào release.
 - Các mốc base EXP đọc được từ candidate server: level 1 `100`, 2 `500`, 3 `1100`, 4 `1900`, 5 `2900`; level 10 `10900`; level 20 `50900`; level 30 `139900`; level 40 `311400`; level 50 `660400`; level 60 `1476400`; level 70 `3561400`; level 80 `8891400`; level 90 `21878400`; level 99 `47564400`. Các bucket level 100-200 và cột chuyển sinh phải đọc nguyên row theo snapshot, không tổng hợp bằng heuristic. Full 200-row bytes remain source artifact, không copy/rewrite thành dữ liệu mới trong client.
 - `level_add.txt` SHA phải được pin cùng release; năm series lần lượt có `LifePerLevel` Metal/Wood/Water/Fire/Earth = `4/3/3/3/1`, `ManaPerLevel = 1/2/2/1/3`, `LifePerVitality = 8/5/6/7/3`, `ManaPerEnergy = 1/3/3/2/4`, resistance và stamina base theo từng row source. Không suy diễn tên phái từ series.
 - `NewPlayerBaseAttribute.ini` đặt Strength/Dexterity/Vitality/Energy `0`, Lucky `1` cho cả năm series; `NewPlayerIni00..09.ini` là mười template sex/series phải giữ source hash riêng.

## Faction và skill-point rule tĩnh

 - `KFaction.cpp:24-91` đọc faction INI SHA-256 `ac8814cf151e90e203c1f35d9e2eb9f934bd592582242544ba31d2daf2d218f`; `KPlayerFaction.cpp:63-119` chỉ cho AddFaction cùng series (ngoại lệ Huashan water), LeaveFaction đặt current `-1`; Lua wrapper `ScriptFuns.cpp:4308-4326` leave trước rồi add nếu có tên.
 - `KPlayer.cpp:3944-4005` yêu cầu skill đã biết, đủ `skill_point`, không phải EXP skill, level đích không vượt max và giới hạn nhân vật; rollback attribute/skill hoàn lại `+5/+1` theo level tại `KPlayer.cpp:3866-3867`. Exact skill catalog/membership vẫn chờ resolver winner và golden, không được điền default.

## Nghiệm thu

- `TEST-PRG-001`: từ fixture level 1 grant đến 200, retry/crash không double EXP/point.
- `TEST-PRG-002`: save/logout/resume bảo toàn level, EXP, attributes, skill points và faction.
- `TEST-PRG-003`: boundary max level/overflow/negative; exact parity formula `BLOCKED` đến golden.
