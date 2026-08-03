# Survivor Mode — Profiling Plan (ticket 42, mobile ship)

Trạng thái: `ready-for-agent` → kết quả đo ghi vào ticket 42 khi chạy device thật.
Mục tiêu: **60fps (16.7ms/frame) portrait mobile trung bình, Android + iOS (IL2CPP)**.

## 1. Frame budget (own-design target)

| Hạng mục | Budget | Ghi chú |
|---|---|---|
| Render (world + UI) | ≤ 10ms | SPR SpriteRenderer không atlas — xem §2 |
| Scripts (Survivor + Sandbox bridge) | ≤ 4ms | director, monster Update, skill cast, collect |
| Physics / collider queries | ≤ 1ms | hiện dùng khoảng cách manual, ít physics |
| GC / alloc | ≤ 1ms | tránh alloc trong Update hot path |
| Headroom | ~0.7ms | vsync jitter, device variance |
| **Tổng** | **≤ 16.7ms** | = 60fps |

## 2. Draw call targets

World: mỗi `SpriteRenderer` ≈ 1 draw call (SPR ppu 40, không atlas, dynamic
batching hiếm khi bắt — sprite lớn). **Monster cap là đòn bẩy chính.**

| Nguồn | Target |
|---|---|
| Monster (cap) | = cap config (§3) |
| Gems / projectiles / VFX | ≤ 40 |
| Arena + player + boss | ≤ 5 |
| **World tổng** | **≤ cap + 45** |
| UI (HUD canvas 1, modal 1, joystick 1) | ≤ 15 |
| **Tổng scene** | **≤ 140 DC (low-end) / 200 (mid)** |

Cap table (own — đo lại trên device, ghi vào ticket):

| Tier | Cap | Thiết bị ví dụ |
|---|---|---|
| Low-end | 50 | RAM 2-3GB, Mali-G52 / Adreno 610 |
| **Mid (default)** | **80** | RAM 4-6GB, Snapdragon 7xx, A13 |
| High-end | 120 | Snapdragon 8xx, A15+ |

## 3. Monster cap (runtime)

- Component `SurvivorMonsterCap` (Platform/) thêm lên SurvivorDirector GO.
- Enforce: `LateUpdate` đếm `SurvivorGameDirector.Monsters` (public API, không
  sửa director 31), vượt cap → trim front-first (kẻ sống lâu nhất), **boss
  exempt**, despawn KHÔNG gọi `OnMonsterKilled` (không XP/gem fake).
- Fail-closed: director null → không đụng; cap ≤ 0 → default 80; > 200 → cắt trần.
- Spawn-gate: `MonsterCapPolicy.CanSpawn` (pure) — hook thêm vào
  `SpawnMonsterAt` khi ticket 31 cho phép sửa director; tới đó trim vẫn là
  backstop (2 lớp).
- EditMode self-check: `SurvivorPlatformTests` (cap logic pure).

## 4. Frame time measurement

- `PerfBudgetMonitor` (Platform/) trên SurvivorDirector GO: accumulator
  `Time.unscaledDeltaTime` (chạy cả khi pause timescale 0), log mỗi 5s:
  `[PerfBudget] avg=..ms min=..ms max=..ms (..fps) monsters=N`.
- Device: Profiler connect (Window > Analysis > Profiler, Autoconnect
  Android/iOS) — đo CPU main thread, GC alloc, 1% low frame.
- FrameTimingManager: optional (đã có `FrameTimingManager` API trong Unity —
  dùng khi cần breakdown GPU/CPU chính xác, không bắt buộc).
- **Target: avg ≤ 16.7ms, 1% low ≥ 25ms (40fps) trên mid device, swarm wave 2 phút.**

## 5. Batch check (Frame Debugger)

1. Window > Analysis > Frame Debugger → Enable.
2. Chọn frame trong swarm wave (cap đang chạy) — đếm `Draw Dynamic`/`Draw Static`.
3. Đối chiếu §2: world DC ≤ cap + 45; tổng ≤ 200.
4. UI: mở HUD + levelup modal + gameover — đếm batch canvas; text quá nhiều
   font atlas → gộp.
5. Ghi số vào ticket 42 (kèm tên device).

## 6. Platform settings (scene wiring)

Trên SurvivorDirector GO, thêm:

| Component | Tác dụng |
|---|---|
| `SurvivorPlatformSettings` | Portrait lock (Android runtime; iOS = PlayerSettings), `targetFrameRate=60`, `sleepTimeout=NeverSleep`, safe-area padding (static `CurrentSafePadding` cho UI 37), joystick radius (SurvivorJoystick.Radius — additive) |
| `SurvivorMonsterCap` | §3 |
| `PerfBudgetMonitor` | §4 |

Verify trên device: portrait đúng, notch không che HUD (safe-area), joystick
bám ngón tay (radius), auto-attack chạy khi touch.

## 7. SpritesRuntime packaging (bắt buộc khi build)

- Root thật = project `/SpritesRuntime` (67.499 file) —
  `SprRuntimeService.DefaultSpritesRoot`. APK/IPA **KHÔNG** tự có thư mục này.
- Build phải copy `/SpritesRuntime` → `Assets/StreamingAssets/SpritesRuntime`
  (hoặc Custom Player Folder) trước khi đóng gói — orchestrator lo.
- Verify trên device: vào game, console `SprRuntime` miss = 0 sau 2 phút chơi
  (skill cast + monster visual) — miss > 0 = packaging sai, FAIL build.
- Camera: ortho size 6, portrait — verify arena hiện đủ trong safe area.

## 8. Device test list (bảng báo cáo — ghi vào ticket 42)

| Device | OS | Tier | Cap | Avg fps | 1% low | DC | Bundle size | Notes |
|---|---|---|---|---|---|---|---|---|
| Samsung Galaxy A54 | Android 13 | mid | 80 | | | | | |
| Xiaomi Redmi Note 12 | Android 13 | low | 50 | | | | | |
| Pixel 7 | Android 14 | mid | 80 | | | | | |
| iPhone SE (2022) | iOS 17 | low | 50 | | | | | |
| iPhone 13 | iOS 17 | mid | 80 | | | | | |
| iPhone 15 | iOS 17 | high | 120 | | | | | |

Mỗi device chạy checklist: portrait lock, safe-area notch, joystick, swarm wave
2 phút (fps log), boss wave, levelup card pause, gameover → restart (không leak
fps), memory (Profiler) < 1.5GB.

## 9. Kết quả → ticket 42

Khi chạy xong, orchestrator ghi vào `issues/42-mobile-ship.md`:
fps thực tế avg/1% low, draw call, bundle size, cap đã chốt, device list, SPR
miss count, camera ortho verify. Cap table §3 chỉnh theo số đo được.
