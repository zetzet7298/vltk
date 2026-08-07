# 44 — Fix waiting-window auto-close dead-wired (re-review 43 finding)

**What to build:** `SurvivorGameDirector.Update`: `if (Pause.IsPaused) return;` đặt TRƯỚC
`_skillChoice?.Tick(Time.time)` → Tick chỉ chạy play mode khi modal KHÔNG mở, nhưng modal mở
⇔ CardChoice scope acquire ⇔ IsPaused=true → auto-close (ticket 29/O6, 30s bỏ quên → close)
**không bao giờ chạy khi modal mở**. Player bỏ mặc modal → pause vô hạn (thoát được vì modal
vẫn click được, nhưng feature O6 không tồn tại runtime).

**Blocked by:** None — baseline `df8b0788a`.

**Status:** done — council review PASS

## Fix (2 phần, KHÔNG tách rời — fix nông sẽ leak)

1. **Dời Tick lên trước early-return**: `_skillChoice?.Tick(Time.time)` chạy trước
   `if (Pause.IsPaused) return;` (Tick dùng Time.time unscaled — an toàn khi paused).
2. **Auto-close → onClosed hook**: `Tick → Close(kv.Key)` (SkillChoiceService:296) là
   service-level close — chỉ Release(CardChoiceScope); LevelUp scope chỉ release qua onClosed
   hook của Overlay. Nếu dời Tick mà không thêm đường này: auto-close leak LevelUp scope +
   canvas modal không hide → timescale kẹt 0 vĩnh viễn.
   Correction: director/Overlay poll `Current(roleId)==null` → hide modal + Release(LevelUpScope);
   hoặc service nhận callback close-notify. Chọn cách nhỏ nhất đúng lifecycle.

## Acceptance

- [x] PlayMode: mở levelup modal, bỏ mặc 30s (hoặc rút timeout test) → modal tự đóng, timescale
      về 1, không leak scope
- [x] EditMode: test auto-close path — Tick khi paused + scope release đầy đủ (CardChoice +
      LevelUp), timescale 1 sau close
- [x] EditMode survivor suite xanh (265 + test mới)
- [x] KHÔNG regression: modal vẫn mở được bình thường, pick card vẫn hoạt động

## Comments

### 2026-06-13 implementer — REOPEN_REQUEST: premise fix phần 1 SAI (Time.time bị scale, không phải unscaled)

Ticket nói: *"Dời Tick lên trước early-return (Tick dùng Time.time unscaled — an toàn khi paused)"*.
Premise **sai**: `Time.time` bị `timeScale` ảnh hưởng (frozen = 0 khi pause). Dời Tick trước
early-return là cần nhưng KHÔNG đủ — `Tick(Time.time)` không bao giờ vượt `BeginWaitingLearnTime`
khi timescale 0 → O6 auto-close vẫn chết runtime → acceptance PlayMode không đạt với cơ chế như spec.

**Evidence (PlayMode thật, test mới `SurvivorWaitingWindowPlayModeTests`, window rút 0.2s, chờ 3.4s realtime):**

```
[44diag] tOpen=0.43→time=0.43 (delta 0) uOpen=2.115→unscaled=5.542 (delta 3.427) cur=True paused=True scale=0
```

- `Time.time` delta = **0** sau 3.427s realtime ở timescale 0 → scaled, frozen.
- `Time.unscaledTime` delta 3.427 → real time vẫn chạy.
- Event còn mở, pause còn giữ → auto-close không bao giờ fire.
- Docs: https://docs.unity3d.com/ScriptReference/Time-time.html (time = seconds since start, affected
  by timeScale); `Time.unscaledTime` = "timeScale-independent time for this frame".

**Fix đề xuất (1 dòng):** director Update dùng `_skillChoice?.Tick(Time.unscaledTime)` thay
`Time.time`. Phần còn lại đã implement + xanh:
- Dời Tick lên trước `if (Pause.IsPaused) return;` (SurvivorGameDirector.Update)
- OverlayPanel: `PollSkillChoiceAutoClose()` (Update poll `Current(1u)==null` → hide canvas + fire
  onClosed — release LevelUpScope đúng lifecycle), `_skillModalVisible`/`_skillOnClosed`, `IsVisible`
- SkillChoiceService: `WaitingLearnWindow` const → instance field (default 30, test rút timeout)
- EditMode tests mới: `LevelUp_AutoClose_ReleasesBothScopes_TimescaleRestored` +
  `LevelUp_PickCard_Closes_NoLeak_PollNoFalseClose` (SurvivorRuntimeWiringTests) — **PASS**
- EditMode survivor suite: **267/267 PASS** (265 baseline + 2 mới)
- PlayMode test mới (`LevelUp_WaitingWindow_AutoCloses_Unpauses_NoLeak`) — FAIL đúng như premise-fail
  dự đoán; sẽ PASS khi đổi time source sang `Time.unscaledTime` (chính là regression guard)

Files changed (chưa commit — chờ sanction đổi time source):
`Assets/Scripts/Survivor/SurvivorGameDirector.cs`, `Assets/Scripts/Survivor/UI/OverlayPanel.cs`,
`Assets/Scripts/Survivor/UI/SkillChoiceService.cs`, `Assets/Tests/EditMode/Survivor/SurvivorRuntimeWiringTests.cs`,
`Assets/Tests/PlayMode/SurvivorWaitingWindowPlayModeTests.cs` (mới), `Assets/Tests/PlayMode/VLTK.Tests.PlayMode.asmdef`.

## Verified

**Commit:** `7a1f7abc2` — fix(survivor): ticket 44 — waiting-window auto-close hoạt động runtime

**Premise correction (đã sanction):** Tick phải dùng `Time.unscaledTime`, KHÔNG phải `Time.time`
— `Time.time` bị timeScale ảnh hưởng (frozen = 0 khi paused) → auto-close chết runtime dù đã dời
Tick. Bằng chứng PlayMode diag: `tOpen=0.43→time=0.43 (delta 0)` vs `unscaled delta 3.427`.

**Files changed:**
- `Assets/Scripts/Survivor/SurvivorGameDirector.cs` — Tick trước early-return pause + `Time.unscaledTime`
- `Assets/Scripts/Survivor/UI/OverlayPanel.cs` — `PollSkillChoiceAutoClose` (Update poll
  `Current(1u)==null` → hide canvas + fire onClosed = release LevelUpScope), `_skillModalVisible`/`_skillOnClosed`, `IsVisible`
- `Assets/Scripts/Survivor/UI/SkillChoiceService.cs` — `WaitingLearnWindow` const → instance field (default 30)
- `Assets/Tests/EditMode/Survivor/SurvivorRuntimeWiringTests.cs` — +2 test (auto-close scope release + timescale, pick no-leak/no-false-close)
- `Assets/Tests/PlayMode/SurvivorWaitingWindowPlayModeTests.cs` (mới) — auto-close real Update, window rút 0.2s
- `Assets/Tests/PlayMode/VLTK.Tests.PlayMode.asmdef` — +ref `VLTK.Survivor.Runtime`

**Test output thật (Unity 6000.5.6f1):**
- EditMode survivor suite (19 fixtures): **267/267 PASS, 0 fail, 0 skip** (5.04s) — gồm
  `LevelUp_AutoClose_ReleasesBothScopes_TimescaleRestored` ✓ + `LevelUp_PickCard_Closes_NoLeak_PollNoFalseClose` ✓
- PlayMode `LevelUp_WaitingWindow_AutoCloses_Unpauses_NoLeak`: **PASS** (0.53s) — modal tự đóng,
  pause 2→0, CardChoice+LevelUp scope 0, timescale về 1, không leak (MalePlayer JX visual boot thật)

**Acceptance:** PlayMode auto-close ✓ (test trên) · EditMode auto-close path + scope release ✓ ·
suite 267 xanh ✓ · regression: modal mở + pick vẫn hoạt động ✓ (EditMode test 2 + PlayMode mở modal assert)
