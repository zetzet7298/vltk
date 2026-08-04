# 41 — Endless (unlock + linear ramp)

**What to build:** Sau wave cố định → endless mở (parity skeleton `IsReposeWave` + `WaveRefresh` dynamic caps + `GetEndlessWaveCount()`), ramp **linear v1** theo wave index: quái hp/atk/count/speed + boss frequency tăng, hệ số từ config own (exponential/stair = upgrade path sau playtest).

**Blocked by:** 30 (Wave breadth), 31 (Boss multi-phase)

**Status:** done — implement P2 core (0a649b663) + verified (DifficultyCurve, tests xanh)

- [ ] Clear wave cố định → endless loop không hồi kết
- [ ] Ramp linear: quái mạnh hơn/nhiều hơn, boss xuất hiện định kỳ tăng dần (hệ số config)
- [ ] EditMode self-check xanh: scaling formula theo waveIndex
- [ ] PlayMode manual: 15 phút endless thấy khó tăng rõ rệt, không giật
