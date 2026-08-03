# 44 — Fix waiting-window auto-close dead-wired (re-review 43 finding)

**What to build:** `SurvivorGameDirector.Update`: `if (Pause.IsPaused) return;` đặt TRƯỚC
`_skillChoice?.Tick(Time.time)` → Tick chỉ chạy play mode khi modal KHÔNG mở, nhưng modal mở
⇔ CardChoice scope acquire ⇔ IsPaused=true → auto-close (ticket 29/O6, 30s bỏ quên → close)
**không bao giờ chạy khi modal mở**. Player bỏ mặc modal → pause vô hạn (thoát được vì modal
vẫn click được, nhưng feature O6 không tồn tại runtime).

**Blocked by:** None — baseline `df8b0788a`.

**Status:** ready-for-agent

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

- [ ] PlayMode: mở levelup modal, bỏ mặc 30s (hoặc rút timeout test) → modal tự đóng, timescale
      về 1, không leak scope
- [ ] EditMode: test auto-close path — Tick khi paused + scope release đầy đủ (CardChoice +
      LevelUp), timescale 1 sau close
- [ ] EditMode survivor suite xanh (265 + test mới)
- [ ] KHÔNG regression: modal vẫn mở được bình thường, pick card vẫn hoạt động

## Verified

- (trống — chờ implementer)
