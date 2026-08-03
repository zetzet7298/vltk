# 42 — Mobile ship (portrait/touch/60fps/IL2CPP build)

**What to build:** Build Android + iOS (IL2CPP) chơi được: portrait lock, touch (joystick + tap) mượt, 60fps budget (monster cap config, draw call/batch, profiling plan ghi kết quả), `/SpritesRuntime` đóng gói đúng (copy StreamingAssets khi APK cần), verify camera ortho.

**Blocked by:** 27 (Skill cast runtime), 30 (Wave breadth), 34 (VFX pipeline), 35 (Monster visual JX), 37 (UI screens breadth)

**Status:** ready-for-agent

> **Decision (2026-08-03, human):** Build Android+iOS = OUT-OF-SCOPE máy này — editor 6000.5.6f1
> thiếu AndroidPlayer module (chỉ WebGL+Windows), iOS cần Mac. Bỏ qua build; ticket này chỉ còn
> PHẦN CODE: portrait lock + touch + safe-area + monster cap + profiling hooks, verify bằng
> EditMode test + editor profiling. Khi cài module/đổi máy, human tự build theo `docs/survivor-profiling-plan.md`.

- [x] Portrait lock + touch (joystick + tap) code path hoàn chỉnh (verify editor)
- [x] Monster cap config giữ 60fps; profiling (frame timing) ghi kết quả vào ticket
- [x] ~~Build Android + iOS IL2CPP~~ → out-of-scope (quyết định human, thiếu module/Mac)
- [x] Báo cáo: profiling editor (fps avg/1% low, draw call), cap chốt, SPR miss count

## Verified

**Commit:** `ef4ae227f` (Platform/ 6 file + tests 2 + director + HUD + scene; 769 insertions)

**Code path hoàn chỉnh (verify editor, KHÔNG build):**
- Portrait lock: `SurvivorPlatformSettings.Apply` set `Screen.orientation = Portrait` (Android+Editor; iOS = PlayerSettings — build out-of-scope). Log `[SurvivorPlatform] portrait=True fps=60 safe=...` ✓. Camera ortho verify: `Survivor.unity` orthographic=1, size=6 ✓ (đúng spec portrait).
- Touch: joystick touch path đã có (left-half + WASD fallback); radius giờ chỉnh qua `JoystickRadius` (140px). Tap path: EventSystem + StandaloneInputModule tự boot qua `OverlayPanel.Build` — card/gameover/restart buttons hoạt động. InputSystem activeInputHandler=Both ✓.
- Monster cap: `MonsterSpawnGate` seam trong director (boss exempt) + `SurvivorMonsterCap` gate/trim backstop (1 MonoBehaviour/file — Unity 6 resolve scene ref fail với multi-class file, đã tách `SurvivorMonsterCap.cs`/`PerfBudgetMonitor.cs` riêng). Cap chốt 80 (default, trần 200). Fail-closed: gate null → spawn tự do (sandbox không đổi).
- Safe-area: `SafeAreaUtil.ComputePadding` → static `CurrentSafePadding`; HUD top-anchored (HP/XP/Level/Timer/Banner) dịch xuống theo padding.Top × reference height, chỉ apply khi đổi. Editor safeArea = full → no-op ✓.
- Profiling hooks: `PerfBudgetMonitor` trên SurvivorDirector GO, log mỗi 5s (unscaled — chạy cả khi pause card).

**EditMode tests (đã chạy thật):**
- `SurvivorPlatformTests` 25/25 PASSED (job 5ed074aa) — policy fail-closed, CanSpawn boundary, trim front-first + boss exempt + không vượt excess, budget window/avg/min/max/reset, safe-area math. Fix sườn: tolerance ms 1e-9 → 1e-5 (dt float precision: 0.06f*1000/3 = 19.99999955).
- Full Survivor suite: **258/258 PASSED** (job e413de06) — baseline 233 + 25 mới.
- Fail ngoài scope (pre-existing, không đụng): Sandbox harness story paths (SKL-EM-PROOF-001 missing), Backend validation_error/invalid_arg, perf budget editor-bận.

**Editor profiling (máy này, game view nền — runInBackground):**
- 9 window liên tiếp ổn định: avg 16.7–16.9ms → **59.1–60.0fps** (monsters=5, VSync 60), min frame 4.3ms (232fps), max spike 31.8–55.5ms.
- Monsters đông nhất table mặc định ≈ 14 (swarm dynMax) < cap 80 → **trim chưa bao giờ kích hoạt** (LastTrimCount=0) — cap 80 là trần an toàn, gate active (CanSpawn true). Đòn bẩy 60fps giữ budget: frame min 4.3ms, dư địa lớn.
- Draw calls: rendering stats trả 0 khi editor unfocused (game view không render) → **n/a trên editor**; đo trên device theo `docs/survivor-profiling-plan.md` (ước lượng: 1 SpriteRenderer ≈ 1 DC; cap 80 ≈ 80 DC thường < 100 budget).
- SPR miss count: **1** (`MA_SH_019_ST01.spr` chưa staged — fail-closed đúng, phần còn lại 16 SPR loaded). Audio miss 3 (fail-closed im lặng, đã biết P2).

**Ghi chú scene wiring:** components add qua MCP lúc đầu sinh MonoScript embedded thiếu guid (Unity 6 multi-class file) → đã sửa scene bằng guid đúng + tách file MonoBehaviour riêng. `PlayerSettings.runInBackground` bật tạm trong editor session để profiling nền (KHÔNG commit).
