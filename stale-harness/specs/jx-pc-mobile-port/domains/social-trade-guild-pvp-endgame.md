# Domain: Social, trade, bang hội, PvP và endgame

## Định danh và phạm vi

- Domain ID: `DOM-STGPE`; DRI: Backend Social/Economy; reviewer: Gameplay/QA; team P2, social/economy P3, PvP/endgame P4.
- Sở hữu chat/channel, team, direct trade coordination, guild/RBAC, PK modes, event/ladder/boss reward.

## Bằng chứng as-is

- `EVID-0042`: `KPlayerTeam.cpp:63-196` create/invite/reply/auto-refuse/info; `:304-622` server create/invite/follow-captain flags.
- `EVID-0043`: `KPlayerTong.cpp`, `KTongData.cpp` chứa bang hội; `KPlayerTrade.cpp` và `KItemList.cpp:3308-3354` có trade backup/recover/start.
- `EVID-0044`: `KPlayerPK.cpp:59-220` kiểm PK theo lock/value, mode enable, team captain-follow, guild-war và cooldown.
- `EVID-0045`: `KLadder.cpp` có ladder seam; rules/reward/event schedules chưa được chứng minh.

## Invariant theo aggregate

- Team: membership duy nhất, captain rõ, invite idempotent/expiring; party affinity qua transfer.
- Trade: mọi offer change xóa confirm; hai confirm cùng revision; chuyển item+money atomic, ACK sau commit; lock order ổn định.
- Guild: membership/role/RBAC persistent; không cho client tự cấp quyền; leave cooldown/capacity BLOCKED `[CẦN XÁC NHẬN]`; owner Social; gỡ block khi source rule + product policy được reviewer duyệt.
- PK: server validate map/event/mode/cooldown/PK value/team/guild; client chỉ request. Exact thresholds từ content/source, không hard-code suy đoán.
- Endgame: event lifecycle và reward grant có event/participant/rank key idempotent; leaderboard có season/version.

## Contract và phase coverage

- P2 dùng `SocialCommand` với `TEAM_CREATE/INVITE/REPLY/KICK` và `PARTY_LEAVE`, roster trả `SocialEvent`.
- P3 dùng `CHAT_SEND`, `TRADE_OFFER/LOCK/CONFIRM/CANCEL`, `GUILD_CREATE/APPLY/ROLE/LEAVE`; moderation policy BLOCKED `[CẦN XÁC NHẬN]`; owner PO/Social; gỡ block khi moderation policy và test manifest được duyệt.
- P4 dùng `PK_MODE_CHANGE`, `EVENT_ENROLL/LEAVE/CLAIM_REWARD`; `SocialEvent` kind `PK_STATE/EVENT_STATE/LADDER_STATE`; mọi discovered event có owner/catalog.
- `TEST-STGPE-001`: concurrent invite/leave/captain transfer và reconnect/channel affinity.
- `TEST-STGPE-002`: economy crash/idempotency cho trade; không item/tiền âm, mất hoặc nhân đôi.
- `TEST-STGPE-003`: PK transition/cooldown/team/guild/map rule từ content; thresholds runtime parity `BLOCKED`.
- `TEST-STGPE-004`: season/event checkpoint, duplicate reward, leaderboard rebuild và rollback.
