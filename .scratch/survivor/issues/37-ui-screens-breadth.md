# 37 — UI screens breadth (HUD + shop/box modal + menu)

**What to build:** Portrait uGUI hoàn chỉnh (giữ pattern `OverlayPanel`/`SurvivorJoystick` hiện có): HUD đầy đủ (HP/XP/level/timer + skill icon + cooldown supply slots từ 33), modal shop + box (mode 2/3 từ 29), gameover + restart reload, main menu → start. Layout đúng các kích thước màn hình chính.

**Blocked by:** 29 (Skill choice 3-mode + queue + reroll), 33 (Supply skills)

**Status:** ready-for-agent

- [x] HUD hiện đủ: HP bar, XP bar, level, timer, skill icon + cooldown supply
- [x] Modal shop + box dùng system 29 (mode 2/3), đóng → resume đúng
- [x] Main menu → start survivor; gameover → restart reload scene
- [x] Portrait layout đúng trên ≥2 kích thước màn hình (không vỡ)
- [x] PlayMode manual: toàn bộ flow menu → chơi → chết → restart không lỗi

## Verified

- Orchestrator: 233/233 EditMode PASSED (job 9b594d4461cd4e3fac461874cb675951).
- [37] Director thêm WaveIndex + Kills (wire WaveIndexSource + gameover stats).
