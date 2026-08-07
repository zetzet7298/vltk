# 03 — Backend / multiplayer P3 scope (có trong offline bar không?)

Type: `task`
Status: `resolved`
Blocked by: —

## Question

Backend/multiplayer có nằm trong offline bar của map này không? Có reverse dhcd server không?

## Answer

**KHÔNG trong offline bar của map này.** Offline single-player = sản phẩm hoàn chỉnh ship được
(thuộc destination). Multiplayer/server = **effort P3 RIÊNG**, mở map mới khi offline complete.

P3 sẽ gồm (effort mới, không phải ticket trong map này): auth, cloud save, authoritative
progression/economy, matchmaking. **KHÔNG reverse dhcd server** (per `server-reverse-decision.md`):
không reuse dhcd server binary/DB/wire-protocol; define new backend contract.

→ Close ticket. Đây là scope boundary (không phải step trên route offline) — được GHI vào
`Decisions so far` chứ không phải `Out of scope`, vì P3 là work thật (separate effort), chỉ không
nằm trên route tới destination offline của map này.
