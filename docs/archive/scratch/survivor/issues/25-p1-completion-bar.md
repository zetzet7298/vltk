# 25 — Decision: P1 completion bar (skeleton hiện có ≠ P1 xong)

Type: `grilling`
Status: `closed`
Blocked by: 01
Closed: 2026-08-02 — 3 gates pass (8/8 EditMode test green + human play-test OK + console sạch Survivor)

## Question

Skeleton P1 đã build+verified (14 script + scene + portrait, compile sạch, play OK) nhưng CHỈ
prove loop. Chốt "P1 done" nghĩa gì trước khi vào P1.5/P2.

## Decisions

### P1 bar (đọc lại SURVIVOR_PLAN line 74-80)

6 mục minimum, auto-attack + card-pick = skill progression (KHÔNG explicit active skill):

1. Arena + player (joystick + 1 auto-attack). Card-pick = skill progression parity dhcd
   `RandomSkillParam.Type=1` (mode levelup). Active skill (super/supply) = P2 ticket 13.
2. Wave spawn liên tục (WaveSpawner: interval ramp + count ramp, perimeter spawn).
3. Monster AI tối giản (move-to-player + contact dmg).
4. Hit processing + die (projectile dmg, monster die, player die).
5. XP drop + levelup → 3-card panel (mode levelup).
6. Die → restart (SceneManager reload).

Pause card (timescale=0 parity r-dhcd-003) đã có trong `SurvivorGameDirector.OnLevelUp` —
không phải gap.

### Gap list (skeleton hiện tại → P1-bar)

**0 gap functional.** Skeleton đã prove đủ 6 mục. Chỉ thiếu **acceptance test**.

### Acceptance form

Hybrid (B): EditMode self-check pure logic + manual play-checklist.

- EditMode: `VLTK.Survivor.Runtime` ref thêm vào `VLTK.Tests.EditMode.asmdef` + 1 test file
  `Assets/Tests/EditMode/Survivor/SurvivorP1LogicTests.cs` cover XpToNext curve, ApplyCard
  mỗi kind, TakeDamage invuln guard.
- Manual: `.scratch/survivor/p1-acceptance.md` checklist 1 run ≥60s, tick pass/fail.

### Close-condition (3 gates, ALL pass)

1. **EditMode self-check pass** — `SurvivorP1LogicTests` green.
2. **Manual play-checklist tick** — `p1-acceptance.md` 1 run ≥60s, mọi dòng pass.
3. **Console sạch** — 0 error/warning trong run (chỉ `[Survivor]` Debug.Log lifecycle).

Gate pass → close ticket + advance ticket 16 (visual bridge P1.5) `ready-for-agent`.
