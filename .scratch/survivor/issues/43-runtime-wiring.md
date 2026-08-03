# 43 — Runtime wiring P2 (council FAIL fix: skill/supply boot + pause + wire gaps)

**What to build:** Fix council review FAIL (integration seam) — wire P2 feature systems vào
game loop thật. Class-level code đã verified (258/258); phần thiếu là BOOT WIRING.

**Blocked by:** None — can start immediately (baseline `8cbdee0e3`).

**Status:** ready-for-agent

## Blocker (council challenges, đã verify bằng grep + read)

1. **Skill boot wiring** — `OverlayPanel.SkillService` không ai assign; `SurvivorPlayer.Cast`
   (SkillCastRuntime) không ai gán; `Assets/Scripts/Survivor/Skill/Generated/` chưa tồn tại
   (editor menu `VLTK/Survivor/Generate Skill Catalog` chưa chạy lần nào). Hệ quả: skill
   library 26 + cast 27 + choice 29 là dead code, levelup chạy `ShowLegacyLevelUp` P1 flat-card.
   Fix: chạy generator (hoặc runtime parse từ `StreamingAssets/Reference`) → tạo
   `SkillCastRuntime` gán `Player.Cast` → tạo `SkillChoiceService` (roster, pool, rng, gold,
   pause) gán `overlay.SkillService` → fill `BossSkillPool` từ catalog boss/npc pool.
2. **Supply wiring (ticket 33)** — `SupplyBar.Build` không caller, `SurvivorSupplyMgr` không
   instantiate runtime. Hệ quả: heal/bomb/magnet/full-clear + slot UI vắng mặt; impact runtime
   (28) chỉ reachable qua supply heal → dead.
3. **WaveIndexSource chưa wire** — HUD wave banner luôn 0. Fix 1 dòng:
   `SurvivorHud.Instance.WaveIndexSource = () => WaveIndex` (director đã expose).
4. **OnApplicationPause/Settings** — không có `OnApplicationPause` nào; `SurvivorAudioSettingsPanel`
   (321 dòng) không ai Build; `SetLanguage` không ai gọi; `SaveSettings` chỉ trong panel unwired.
   Fix: `OnApplicationPause(bool)` trên director → pause ref-count scope AppLifecycle; wire
   settings panel boot (tối thiểu persist path + language apply).

## Minor (council challenges)

5. `SkillChoiceService.Select` LevelUp/Shop branch không check card ∈ current draw (Box có
   `Contains`, 2 branch kia chỉ check gold). Fix: thêm `Contains(ev, card.Def)`.
6. `SurvivorBoss.Update` check `m.Hp <= 0` TRƯỚC `ReportHp` → đòn chí mạng vượt window spawn
   booty phase cũ. Fix: reorder `ReportHp(maxHp, Mathf.Max(0, hp))` trước death check.

## NOT a challenge (lead verify đã bác)

- Monster visual JX: ĐÃ wire — `SurvivorMonster.Init` destroy proxy + AddComponent JxNpcVisual.
  KHÔNG sửa.

## Acceptance

- [ ] Skill boot: levelup pick card → 3-card modal → skill cast được (PlayMode manual verify:
      cast 1 skill thấy projectile/missile theo SkillDef, không còn P1 flat-card path)
- [ ] Supply: heal/bomb/magnet slot UI hiện + dùng được (PlayMode)
- [ ] WaveIndexSource: HUD wave banner hiện số đúng (PlayMode)
- [ ] OnApplicationPause: ra ngoài app → timescale 0, vào lại → resume (editor simulate)
- [ ] Settings panel: mở được, volume/language change + persist
- [ ] Minor 5, 6 fix + test
- [ ] EditMode survivor suite xanh (258 + test mới)
- [ ] **+1 boot smoke test** (EditMode): assert director OnInit wire đủ — SkillService/Cast/
      SupplyMgr khác null sau boot (chặn tái phạm dead-wiring)

## Verified

- (trống — chờ implementer)
