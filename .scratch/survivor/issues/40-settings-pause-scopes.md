# 40 — Settings + pause scopes (settings UI + apply-runtime)

**What to build:** Màn hình settings (volume master/bgm/sfx → mixer 36, quality, language → 38) persist qua 39 + apply-runtime. `SurvivorPause` ref-count đủ scope: CardChoice (29 đã có)/Settings/AppLifecycle/GameOver → timescale ∈ {0,1}; `OnApplicationPause` acquire/release đúng.

**Blocked by:** 36 (Audio pipeline), 38 (i18n VN/EN), 39 (Save/progression)

**Status:** ready-for-agent

- [ ] Settings UI chỉnh volume → mixer ngay + lưu; quality + lang áp runtime
- [ ] Pause: mở settings/gameover/ra ngoài app → timescale 0 đúng scope ref-count, resume đúng
- [ ] OnApplicationPause acquire/release không leak timescale (về lại game đúng)
- [ ] EditMode self-check xanh: ref-count FSM
- [ ] PlayMode manual: pause mọi scope → resume không kẹt timescale 0
