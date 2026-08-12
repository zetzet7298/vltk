# 01 — Parity definition: "DHCD parity" nghĩa gì khi data blocked?

Type: `grilling`
Status: `resolved`
Blocked by: —

## Question

Completeness bar yêu cầu "DHCD FULL parity", nhưng DHCD numeric balance (card weights
r-dhcd-001, drop/XP curve r-dhcd-006) **BLOCKED/encrypted FastXXTEA**, server không reverse,
data không port. "Parity" ở đây phải được chốt nghĩa gì để mọi ticket sau dùng chung?

## Answer

**Parity = STRUCTURAL / LIFECYCLE / LOOP-SHAPE parity (declaration + evidence docs + observable
loop), KHÔNG numeric parity.** Balance numbers (skill weights, drop rates, XP curve, wave timing,
boss HP/atk, difficulty ramp) = **OWN design/tuning**.

Cơ sở: r-dhcd-001/006 explicitly `unresolved/blocked` (no reward constant recovered);
`server-reverse-decision.md` cấm reverse; AGENTS.md cấm port dhcd data.

Quy tắc áp dụng cho mọi ticket nói "parity dhcd X":
- **Structure-parity** (bắt buộc cite): system lifecycle hook, field/schema, command shape,
  queue structure, state machine — trích từ `reconstructed-types/BattleCore/*.cs` + evidence docs.
- **Own-design** (bắt buộc ghi rationale): mọi con số + balance curve + feel.

Acceptance gate cho map: mỗi decision ticket phải tách rõ 2 phần này. Map không sinh ticket
"clone số dhcd".
