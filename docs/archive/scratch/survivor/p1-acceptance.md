# P1 Acceptance — Manual play-checklist

Ticket: [25-p1-completion-bar](issues/25-p1-completion-bar.md) gate 2.
Scene: `Assets/Scenes/Survivor.unity`. Play 1 run ≥60s. Tick mỗi dòng.

## Run log

- Date: ____
- Duration: ____s
- Player: ____

## Checklist

- [ ] Player joystick move (touch left-half + WASD) + clamp trong arena
- [ ] Auto-attack fire projectile về nearest monster mỗi `AttackInterval`
- [ ] Wave spawn liên tục từ perimeter, interval ramp down + count ramp up
- [ ] Monster chase player, contact → playerTakeDamage + i-frame
- [ ] Projectile hit monster → monster Hp giảm → die → XP gem drop
- [ ] Pick gem (magnet radius) → XP tăng → levelup trigger
- [ ] Levelup: timescale=0 + 3 card panel hiện
- [ ] Pick card → stat apply đúng kind → timescale=1 resume
- [ ] Hp=0 → gameover panel → restart reload scene `Survivor`

## Gate 3: Console

- [ ] 0 error, 0 warning trong run (chỉ `[Survivor] OnInit/OnStart/OnGameStart/GameEnd`)

## Result

- [ ] PASS → ticket 25 closed, advance ticket 16 (P1.5 visual bridge)
- [ ] FAIL → ghi symptom, tạo follow-up
