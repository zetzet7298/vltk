# 42 — Mobile ship (portrait/touch/60fps/IL2CPP build)

**What to build:** Build Android + iOS (IL2CPP) chơi được: portrait lock, touch (joystick + tap) mượt, 60fps budget (monster cap config, draw call/batch, profiling plan ghi kết quả), `/SpritesRuntime` đóng gói đúng (copy StreamingAssets khi APK cần), verify camera ortho.

**Blocked by:** 27 (Skill cast runtime), 30 (Wave breadth), 34 (VFX pipeline), 35 (Monster visual JX), 37 (UI screens breadth)

**Status:** ready-for-agent

- [ ] Portrait lock + touch (joystick + tap) mượt trên device thật
- [ ] Monster cap config giữ 60fps; profiling (frame timing) ghi kết quả vào ticket
- [ ] Build Android + iOS IL2CPP chạy được, SpritesRuntime đóng gói đúng (SPR load được trên device)
- [ ] Báo cáo: fps thực tế, draw call, bundle size, device test list
