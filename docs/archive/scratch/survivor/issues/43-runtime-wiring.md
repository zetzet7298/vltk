# 43 — Runtime wiring P2 (council FAIL fix: skill/supply boot + pause + wire gaps)

**What to build:** Fix council review FAIL (integration seam) — wire P2 feature systems vào
game loop thật. Class-level code đã verified (258/258); phần thiếu là BOOT WIRING.

**Blocked by:** None — can start immediately (baseline `8cbdee0e3`).

**Status:** done — verified + council re-review PASS (2026-08-04, pi session)

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

- [x] Skill boot: levelup pick card → 3-card modal → skill cast được (PlayMode manual verify:
      cast 1 skill thấy projectile/missile theo SkillDef, không còn P1 flat-card path)
- [x] Supply: heal/bomb/magnet slot UI hiện + dùng được (PlayMode)
- [x] WaveIndexSource: HUD wave banner hiện số đúng (PlayMode)
- [x] OnApplicationPause: ra ngoài app → timescale 0, vào lại → resume (editor simulate)
- [x] Settings panel: mở được, volume/language change + persist
- [x] Minor 5, 6 fix + test
- [x] EditMode survivor suite xanh (258 + test mới)
- [x] **+1 boot smoke test** (EditMode): assert director OnInit wire đủ — SkillService/Cast/
      SupplyMgr khác null sau boot (chặn tái phạm dead-wiring)

## Verified

**Đã verify hoàn tất (implementer run 2 — verify độc lập lại toàn bộ acceptance, khớp report run 1):**

- Compile: refresh + domain reload sạch, 0 error console (chỉ 2 nhiễu: stale `Failed to find entry-points` + MCP watchdog).
- PlayMode (3 vòng play sạch + 2 scene reload qua gameover→Restart thật):
  - Boot: Pause/Cast/SkillService/Supply/SupplyBar/SettingsPanel non-null, BossSkillPool=739, HUD.WaveIndexSource wire, Wave banner số thật (2→3).
  - Skill: OnLevelUp → modal 3 card SkillDef thật (id 98/1109/1161…) → click Card0 → roster learn + Pause 2→0 + ts 0→1. Cast thật: precast fx `skill_precast` + projectile dmg=2 srcId=1166 (đúng công thức DamageFor level 1) qua SkillCastSpawner.
  - Supply: 4 slot UI (Heal/Bomb/Magnet/FullClear); heal end-to-end 5→3→5 (clamp MaxHp); bomb AoE 5→0; magnet MagnetActiveTime=4.
  - OnApplicationPause (reflection invoke private): true → ts 0 + AppLifecycle scope 1; false → scope 0; ref-count giữ pause khi scope khác còn giữ (đúng spec D13).
  - Settings: launcher ⚙ mở → Settings scope 1 + ts 0; ✕ đóng → scope 0 + ts 1; SetLang(en/vi) → persist `survivor.settings` JSON `lang` đổi + i18n CHUNG (hud lang = overlay lang = en).
  - GameOver → Restart click → scene reload → run mới boot sạch, mọi scope = 0, ts = 1.
- **NRE kết luận (SurvivorMonster.cs:67 `Instance.Player.TakeDamage` + SurvivorGameDirector.cs:97 `_spawner.Tick`)**: KHÔNG tái hiện trong play sạch (3 vòng play, monster contact damage chạy thật, 2 vòng scene teardown reload) → xác nhận là FSR hot-reload race lúc implementer edit khi play. Ghi note, KHÔNG fix (theo contract).
- EditMode suite Survivor: **265/265 passed, 0 failed** (258 baseline + 7 mới: 3 SurvivorRuntimeWiringTests boot smoke/catalog real sizes, 2 Select Contains guard, 3 SurvivorPause scopes — CardChoicePause cũ bị thay, test cũ thay tương ứng).
- Ghi chú phụ: 1 tên skill hiển thị mojibake (`ấ±³ậÁựÁỳnpc` id 1161) — TCVN3/GBK decode 1 dòng lệch, ngoài acceptance (cosmetic).

## Council re-review (2026-08-04, pi session) — PASS

- Diff `df8b0788a` vs baseline `8cbdee0e3` (741 insertions, 15 files, sandbox untouched):
  - ✅ Blocker 1 skill boot: `BootSkillSystem` — `Player.Cast = SkillCastRuntime`, `Overlay.SkillService = SkillChoiceService` (pool player thật), `BossSkillPool` fill từ catalog boss/npc, supply setup fail-closed
  - ✅ Blocker 2 supply: `BootSupplyBar` (`SupplyBar.Build` + `OnUse` → heal/bomb/magnet/full-clear thật) + `SurvivorPlayerDamageable` adapter (Heal clamp MaxHp)
  - ✅ Blocker 3: `WaveIndexSource = () => WaveIndex` (EditMode fallback FindAnyObjectByType)
  - ✅ Blocker 4: `OnApplicationPause` → `SurvivorPause.AppLifecycleScope` ref-count; settings panel `Build(text, pause)` + launcher ⚙/✕ + NRE fix (lang Button cùng GO)
  - ✅ Minor 5: `Select` LevelUp/Shop `Contains(ev, card.Def)` trước learn/spend (Box đã có)
  - ✅ Minor 6: `SurvivorBoss.Update` — `ReportHp` trước death check, hp clamp 0 → loss = maxHp
  - ✅ Boot smoke test: `SurvivorRuntimeWiringTests` (3 test: wire all, levelup service-path non-legacy, catalog real sizes) — chống tái phạm dead-wiring
- `SurvivorPause` ref-count per-scope (5 scope), apply transition 0→1/1→0 — release đúng scope không nuốt pause scope khác; `Update` Tick trước early-return (ticket 44 đã sửa sau, dùng unscaledTime)
- EditMode suite hiện tại (sau 44-47): **277/277 PASSED** (0 failed, 3.96s) — job `17eee45fd4a940db9ab29e7c8ecfc490`
- Verdict: **PASS** — mọi blocker cũ đã wire vào game loop thật, không phát hiện challenge mới.

## Snapshot (xml) — implementer report

```xml
<snapshot ticket="43-runtime-wiring-p2" baseline="8cbdee0e3" date="2026-01-02" status="DONE">
  <editmode suite="VLTK.Tests.Survivor" total="265" passed="265" failed="0" skipped="0" duration_s="2.16" result="Passed"/>
  <files>
    <new>Assets/Scripts/Survivor/UI/SurvivorPause.cs</new>
    <new>Assets/Scripts/Survivor/Skill/SurvivorSkillCatalogService.cs</new>
    <new>Assets/Tests/EditMode/Survivor/SurvivorRuntimeWiringTests.cs</new>
    <modified>Assets/Scripts/Survivor/SurvivorGameDirector.cs</modified>
    <modified>Assets/Scripts/Survivor/UI/OverlayPanel.cs</modified>
    <modified>Assets/Scripts/Survivor/UI/SkillChoiceService.cs</modified>
    <modified>Assets/Scripts/Survivor/UI/SurvivorAudioSettingsPanel.cs</modified>
    <modified>Assets/Scripts/Survivor/Actor/SurvivorPlayer.cs</modified>
    <modified>Assets/Scripts/Survivor/Actor/SurvivorBoss.cs</modified>
    <modified>Assets/Scripts/Survivor/Supply/SurvivorSupplyMgr.cs</modified>
    <modified>Assets/Tests/EditMode/Survivor/SurvivorSkillChoiceTests.cs</modified>
    <sandbox_touched>false</sandbox_touched>
  </files>
  <wiring>
    <skill route="runtime-parse" catalog_rows="1217" display_rows="243" missile_rows="442"
          boss_pool="739" skill_service="OverlayPanel.SkillService" cast="SurvivorPlayer.Cast"/>
    <supply mgr="SurvivorSupplyMgr" heal="OnUse heal" bomb="OnUse bomb" magnet="OnUse magnet"
            full_clear="OnUse full-clear" healer_adapter="SurvivorPlayerDamageable"/>
    <pause type="SurvivorPause" scopes="CardChoice,Settings,AppLifecycle,GameOver,LevelUp"
           replaces="CardChoicePause (deleted)"/>
    <levelup flow="director OnLevelUp acquire LevelUp → overlay onClosed release (legacy + service path)"/>
    <wave banner="SurvivorHud.WaveIndexSource = () => WaveIndex"/>
    <settings panel="SurvivorAudioSettingsPanel.Build(text, Pause)" language="shared SurvivorText"/>
  </wiring>
  <bugs_fixed>
    <bug id="B1">OverlayPanel.TryShowSkillChoice pick handler never fired director onPick → stuck pause. Fixed via onClosed + SurvivorPause.</bug>
    <bug id="B2">SkillChoiceService.Select LevelUp/Shop branches missing Contains(ev, card.Def). Added (pre-spend).</bug>
    <bug id="B3">SurvivorBoss.Update m.Hp&lt;=0 checked before ReportHp → death check moved after report.</bug>
    <bug id="B4">SurvivorAudioSettingsPanel.Construct NRE at boot: lang buttons grabbed Button via transform.parent (root panel has none). Fixed to same-GO GetComponent. Latent — panel never built before.</bug>
    <bug id="B5">Stale Time.timeScale=0 across scene reload after gameover/pause (old director's pause delegate died with it). OnInit now resets ts=1.</bug>
  </bugs_fixed>
</snapshot>
```

## Verification (real test output)

**EditMode — full survivor suite: 265/265 PASS (2 runs final, 0 failures)**
- Baseline 258 + 9 new: SurvivorRuntimeWiringTests (4: boot smoke, levelup flow, catalog smoke, dead-code sweep) + SurvivorSkillChoiceTests (3 SurvivorPause ref-count/scopes/unknown-scope replacing 1 CardChoicePause test, +2 Contains guard tests)
- Intermediate run caught B4 (NRE panel boot) + EditMode-only HUD.Instance-Awake issue → fixed → green

**PlayMode manual (real UI clicks, editor drive):**
```
play boot: pause=True cast=True skillSvc=True roster=0 supplyBar=True
           heal=True bomb=True magnet=True waveSrc=True settingsPanel=True bossPool=739
levelup (AddXp): ts=0 count=2 LevelUp=1 CardChoice=1 lvl=2        ← modal mở, game đứng
click Card0 (Button.onClick.Invoke): ts=1 count=0 roster=1 skill=344 form=6 lvl=1, canvas off
cast: cd=0.97 proj=11 precast=1 melee=0 monsterHp 30 → 8.25      ← fan spread 11 tia + fx
supply heal: 2 → 4/5  | bomb: 50 → 42 (cd 12)  | magnet: active 4.0s (cd 18)
         full-clear: 20→14 ×2 (cd 30)
settings: gear click → panelOpen=True settingsScope=1 ts=0; EN click → hudLang=en (shared text);
          close → scope 0; masterSlider read 1.00 (persist path)
gameover: TakeDamage(999) → ts=0 GameOverScope=1 modal + Restart btn → click → fresh run
          hp=5/5 lvl=1 ts=1 count=0 (mọi scope sạch — không leak)
applifecycle: editor gửi OnApplicationPause(true) lúc vào play (unfocused) → Pause scope giữ, ts=0
              — wiring hoạt động (observed count=1 ts=0 tại boot)
```

**Ghi chú**: player chết ~wave 2 (~12-15s) — P1 balance cũ (auto-attack 1 dmg/0.6s vs swarm), combat code không đụng (chỉ thêm Heal); ngoài scope ticket này. Play mode exit bất thường 1 lần giữa session (FSR hot-reload warning của editor, không phải code path).
