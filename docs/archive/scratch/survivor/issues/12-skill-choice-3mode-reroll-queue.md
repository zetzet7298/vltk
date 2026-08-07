# 12 — Decision: 3-mode skill choice + reroll + per-roleId queue

Type: `grilling`
Status: `ready-for-human`
Blocked by: 01, 04

## Question

Thiết kế `SurvivorRandomSkillCtrl`: 3 mode (levelup/box/shop) + reroll + per-roleId pending-event
queue (`m_playerEventWaitingList` Queue parity r-dhcd-002, role-keyed). Quyết định: command shape
(SelectRandomSkill/ReRandomSkill/SelectBoxSkill/SkillRun-equivalent), queue lifecycle
(enqueue/wait/dequeue), modal trigger. Pause timescale khi card mở = own-design scope (r-dhcd-003
chỉ prove 1 acquire/release path, KHÔNG prove global pause → quyết định pause toàn battle hay
chỉ freeze spawn/animation). Card pool composition law phụ thuộc 04 (graduate từ Not yet specified).
