# 42 — Mobile ship (portrait/touch/60fps/IL2CPP build)

**What to build:** Build Android + iOS (IL2CPP) chơi được: portrait lock, touch (joystick + tap) mượt, 60fps budget (monster cap config, draw call/batch, profiling plan ghi kết quả), `/SpritesRuntime` đóng gói đúng (copy StreamingAssets khi APK cần), verify camera ortho.

**Blocked by:** 27 (Skill cast runtime), 30 (Wave breadth), 34 (VFX pipeline), 35 (Monster visual JX), 37 (UI screens breadth)

**Status:** ready-for-agent

> **Decision (2026-08-03, human):** Build Android+iOS = OUT-OF-SCOPE máy này — editor 6000.5.6f1
> thiếu AndroidPlayer module (chỉ WebGL+Windows), iOS cần Mac. Bỏ qua build; ticket này chỉ còn
> PHẦN CODE: portrait lock + touch + safe-area + monster cap + profiling hooks, verify bằng
> EditMode test + editor profiling. Khi cài module/đổi máy, human tự build theo `docs/survivor-profiling-plan.md`.

- [ ] Portrait lock + touch (joystick + tap) code path hoàn chỉnh (verify editor)
- [ ] Monster cap config giữ 60fps; profiling (frame timing) ghi kết quả vào ticket
- [x] ~~Build Android + iOS IL2CPP~~ → out-of-scope (quyết định human, thiếu module/Mac)
- [ ] Báo cáo: profiling editor (fps avg/1% low, draw call), cap chốt, SPR miss count

## Verified

- (trống — chờ implementer)
