# Validation

## Proof Strategy

Mỗi sửa đổi phải nối được: PC source/config line hoặc exact extracted bytes → Unity mapping → automated test. Test phải fail trên behavior sai hiện tại và pass sau sửa. Static evidence tối đa `SOURCE_PROVEN`/`SPECIFIED`; Unity automated test có thể chứng minh `FUNCTIONAL`, không tự đủ cho `PARITY_DONE`.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Level curves, parsed attributes, child/collide/state relations cho skill bị sửa |
| Integration | `PcCombatCatalogFactory` + `CombatRuntimeService` cast/effect path |
| E2E | Không bắt buộc nếu Unity runtime unavailable; ghi blocker |
| Platform | EditMode fixture `VLTK.Tests.Sandbox.CaiBangCombatParityTests` |
| Performance | Không regress catalog build/cast path rõ rệt; không benchmark riêng nếu diff nhỏ |
| Logs/Audit | Harness story evidence và trace |

## Fixtures

- `CombatActorState` Cái Bang trong `CaiBangCombatParityTests`.
- Canonical Cái Bang PC source dưới `/var/www/jx-pc`.
- Repo-local `Assets/StreamingAssets/Reference/gaibang.lua` chỉ dùng khi exact provenance được kiểm tra.

## Commands

```text
cd /var/www/vltk-mobile
python3 scripts/compile_scripts.py
bash scripts/run_caibang_parity_tests.sh
```

## Acceptance Evidence

- Canonical static rows extracted from `/var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt` into exact repo slices:
  - `Assets/StreamingAssets/Reference/PcCaiBangSkills.txt` for audit/readability.
  - `Assets/Resources/Reference/PcCaiBangSkills.bytes` for synchronous packaged runtime loading on Android.
- `PcCombatCatalogFactory` overlays canonical skill style, state, missile, child/event, targeting, animation, wait/cooldown and ByMissle fields for player Cái Bang rows; Unity render fallback is retained only where PC form `0` still owns child missiles.
- `CombatRuntimeService` now:
  - defers `ByMissle` damage until each projectile collision;
  - tracks impact owner/level per projectile instance, including nested collide-event missiles;
  - resolves `1073 → 1072 → missile 334` and `357 → 389 → missile 195` through their own lifecycles;
  - clamps learned grant level before evaluating `addskilldamage`.
- `CombatSkillSlotController` wires nested projectile visuals back into runtime collision resolution, preserves partially customized decks, migrates only empty/known generated decks, and defaults Cái Bang to five canonical player damage skills `117/119/122/125/128`.
- `SkillSectCatalog` aligns canonical animation/style-facing metadata and corrects skill 125 display name to `Bổng Đả Ác Cẩu`.
- Unity forced refresh/compile completed without C# errors. Existing unrelated compiler warnings remain.
- Fresh verifier result: `136 total, 136 passed, 0 failed, 0 skipped` across all 13 `CaiBang*` EditMode fixtures plus `CombatSkillSlotTests`.
- Independent Herdr reviewer found four correctness regressions (Android `StreamingAssets` filesystem access, nested ByMissle timing, partial-deck overwrite, skill 125 name), then one HUD nested-collision integration gap. All were fixed and final reviewer reported no remaining high-confidence finding.

## Residual Gaps

- No Android APK/AAB device smoke was run. `Resources` packaging is covered by Unity resource-load test and standard Unity packaging semantics, but device proof remains below `PARITY_DONE`.
- No PC runtime golden capture exists for every animation/frame/visual/audio outcome. Story reaches automated functional parity for covered Cái Bang behavior, not universal `PARITY_DONE`.
