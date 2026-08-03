# 15 — Decision: ImpactMgr / buff-debuff system design

Type: `grilling`
Status: `ready-for-human`
Blocked by: 07

## Question

Thiết kế `SurvivorActorAttr` + impact system: attribute model, impact type enum, apply/tick/
remove lifecycle, DOT (poison/burn), control (freeze/stun + stun state), stacking rules. Dựa
research 07. Structure-parity cite declaration; numeric (dmg tick, duration) = own.
