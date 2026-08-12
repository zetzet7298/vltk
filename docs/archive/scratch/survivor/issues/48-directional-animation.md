# 48 — Player/enemy di chuyển không đúng animation 8 hướng

**What to build:** Fix visual direction (8-way facing) cho player + enemy trong Survivor mode. Baseline: HEAD.

**Blocked by:** None. **Status:** done — verified + close (2026-08-04, commit 63ddd4ac0)

## Bug

- Player (SurvivorPlayer): chỉ gọi `_visual?.PlayMove(dir.sqrMagnitude > 0.01f)` (SurvivorPlayer.cs:99) — **KHÔNG gọi `SetDirection`** → nhân vật luôn đứng 1 hướng khi chạy (joystick 8 hướng nhưng sprite không xoay theo).
- Monster (SurvivorMonster): đã gọi `SetDirection(MalePlayerSpriteCatalog.DirectionFromMove(d.normalized))` (line 62) — cần verify đúng 8 hướng + direction mapping khớp convention của visual (JX SPR 8-dir: dir index convention của MalePlayerSpriteCatalog/FemalePlayerSpriteCatalog).

## Context đã biết

- `IActorVisual.SetDirection(int dirIndex8)` — dirIndex8 = 8-way index.
- `JxPlayerVisual` wrapper → `MalePlayerVisual.SetDirection(d)` (Sandbox, đã có `DirectionFromMove`? grep: SandboxPlayerController.cs:305 `visual.SetDirection(facing8Way)` — có sẵn logic facing 8-way từ input, tham chiếu).
- `JxNpcVisual` wrapper → NpcVisual impl SetDirection (line 94).
- `MalePlayerSpriteCatalog.DirectionFromMove(Vector2)` tồn tại (monster dùng).
- ProxyActorVisual.SetDirection = no-op (P1 placeholder — không phải bug, chỉ note).

## Nghi vấn cần verify (để làm đúng, không bịa)

1. Convention dir index 8 của MalePlayerVisual vs `DirectionFromMove` output — khớp hay lệch? (Monster dùng DirectionFromMove đang hoạt động? PlayMode test trước đây có xác nhận monster xoay hướng?)
2. Sprite flip: 8 hướng JX = 4 sprite + flip ngang hay 8 frame riêng? MalePlayerVisual xử lý thế nào (xem code ApplyFrame/direction) — nếu hướng 5-8 chỉ flip thì SetDirection vẫn đúng.
3. Idle facing: khi player đứng yên sau khi chạy, giữ hướng cuối (PC behavior) — cần cache facing, chỉ gọi SetDirection khi đổi hướng (tránh spam).
4. Diagonal speed/visual: không liên quan animation frame, chỉ facing — không đụng.

## Acceptance

- [x] Player chạy joystick/WASD → sprite xoay đúng 8 hướng di chuyển, đứng yên giữ hướng cuối
- [x] Monster đuổi player → sprite hướng theo hướng di chuyển (đúng 8 hướng, không chỉ left/right flip)
- [x] Không regression: y-sort (46/47), skill cast direction, PlayMode test suite xanh
- [x] EditMode test mới + PlayMode verify screenshot 8 hướng

## Verify

- PlayMode: joystick di chuyển chéo/phải/trái/lên/xuống, screenshot từng hướng
- Test suite hiện hành (EditMode ~277 tests)

## Result (2026-08-04, commit `63ddd4ac0`)

**Fix:**
- `SurvivorPlayer.Update`: gọi `SetDirection` qua `UpdateFacing(ref _facing, dir)` (static, testable) —
  JX dir order (0=S..7=SE, `MalePlayerSpriteCatalog.DirectionFromMove`); idle giữ hướng cuối
  (move≈0 → không reset); chỉ SetDirection khi hướng ĐỔI (không spam mỗi frame).
- Bonus: null-guard `SurvivorGameDirector.Instance` (giết 4 NRE cũ lúc scene teardown).
- `SurvivorMonster`: cache facing — chỉ SetDirection khi hướng đổi (tránh garbage Vector2 trong NpcBridge mỗi frame).
- KHÔNG sửa Sandbox (bridge qua adapter — rule workspace).

**Tests mới:**
- `Assets/Tests/EditMode/Survivor/SurvivorDirectionTests.cs` — NpcBridge angle formula round-trip (8 dir),
  cardinal anchors, zero→-1, `UpdateFacing` cache semantics (đổi/giữ/idle).
- `Assets/Tests/PlayMode/SurvivorDirectionPlayModeTests.cs` — player 8 hướng qua bridge (`MalePlayerVisual.direction`)
  + idle-hold + monster chase direction khớp `DirectionFromMove` + screenshots evidence.

**Verify (lead re-run độc lập):**
- EditMode Survivor group: **283/283 passed** (277 baseline + 6 mới, gồm SurvivorYSortTests 46/47)
- PlayMode `SurvivorDirectionPlayModeTests`: **2/2 passed**
- Full-suite failures (~24, Backend/Sandbox) = pre-existing — proven bằng git stash + run lại trên baseline
- Console sạch; screenshots `Assets/Screenshots/ticket48-*.png` (E/NE/N/idle/monster-chase) khác hướng rõ
