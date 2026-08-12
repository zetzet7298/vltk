# 17 — Decision: full UI screens architecture

Type: `grilling`
Status: `ready-for-human`
Blocked by: 01

## Question

Thiết kế UI portrait: HUD (hp/xp/level/timer/skill-icon), levelup modal 3-card, shop, box-open,
gameover+restart, settings, main menu. Quyết định: uGUI vs UI Toolkit, screen-stack/flow, overlay
panel parity r-dhcd-003 (OnVisible/OnHidden timescale). Skeleton hiện có `OverlayPanel`/`SurvivorJoystick`
— decide keep/replace.
