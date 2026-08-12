# Domain: NPC và nhiệm vụ

## Định danh và phạm vi

- Domain ID: `DOM-NQ`; DRI: Gameplay Content; reviewer: Backend/Security/QA; P1 tương tác cơ bản, P2 catalog sâu.
- Sở hữu NPC identity/template/dialog/menu, quest graph/state/objective/reward và Lua host API.

## Bằng chứng as-is

- `EVID-0033`: `KNpcTemplate.h:12-73` có name/kind/camp/series/animation/AI/combat fields và init template.
- `EVID-0034`: `KPlayerDBFuns.cpp:688-737,975+` load/save task values, station và waypoint.
- `EVID-0035`: `KMission.cpp:100` gọi world script; corpus `bin/client/script/missions/**` và `bin/Server/script/**` cho thấy script rộng nhưng chưa reconciled package order/locale/hash.

## Invariant và state

- NPC interaction server validate range/LOS/active state; client không tự chọn menu/reward ngoài option được phát.
- Quest transition wire dùng `QuestState` gồm `UNAVAILABLE -> AVAILABLE -> ACCEPTED -> ACTIVE -> COMPLETABLE -> COMPLETED` hoặc `FAILED/ABANDONED`; reward grant đúng một lần.
- Legacy quest/event chạy Lua 5.1 sandbox, không filesystem/network/process; host API whitelist và CPU/memory/instruction budget BLOCKED `[CẦN XÁC NHẬN]`; owner Backend/Content; gỡ block khi corpus Lua + load/security test được reviewer duyệt.
- Script không ghi DB trực tiếp; chỉ phát domain intent được validate/transaction hóa.
- Commands: `InteractNpc`, `SelectNpcOption`, `AcceptQuest`, `AbandonQuest`, `ClaimQuestReward`; events: dialogue/menu/quest/reward deltas.

## Coverage và nghiệm thu

- P1: NPC huấn luyện/vertical slice và persistence. P2: 100% discovered NPC/quest/script có owner/source hash/locale/package order/phase/status.
- P3: party/guild/economy objectives; P4: event/PvP/endgame scripts.
- `TEST-NQ-001`: invalid transition, duplicate reward, reconnect giữa complete/claim.
- `TEST-NQ-002`: sandbox chặn API ngoài whitelist, timeout runaway, deterministic host responses.
- Runtime PC golden và canonical package order hiện `BLOCKED`; không gắn quest parity done.
