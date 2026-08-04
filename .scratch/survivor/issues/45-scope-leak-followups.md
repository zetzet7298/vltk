# 45 — Scope-leak followups từ dual review 44 (2 challenge material)

**What to build:** Council dual review ticket 44 (reviewer 44a `a97df140` + 44b `9e4aa179`,
cả 2 verdict PASS) phát hiện 2 đường leak LevelUpScope còn lại — cùng class bug ticket 44
(scope không release → pause kẹt vô hạn). Fix nhỏ, 2 phần độc lập.

**Blocked by:** None — baseline `7a1f7abc2` (fix 44) + `6e795114b` (docs).

**Status:** ready-for-agent

## Phần 1 — Player chết khi modal mở → LevelUpScope kẹt (44a)

**Evidence:** `SurvivorMonster.Update` damage check `if (dist < 0.7f) Player.TakeDamage(ContactDamage)`
chạy mỗi frame KHÔNG check `Pause` (movement frozen bởi dt=0, damage check thì không; invuln không
decay khi dt=0 → tối đa 1 hit) → player chết khi modal levelup đang mở → `OnPlayerDied` →
`ShowGameOver` set `_skillModalVisible=false` → poll tắt → LevelUpScope không bao giờ release
(count kẹt LevelUp+GameOver=2). Pre-existing (trước 44 cùng path), KHÔNG phải regression — nhưng
đúng class bug 44 (scope-count lệch, timescale kẹt 0 cho tới restart).

**Correction (reviewer đề xuất):** 1 dòng trong `OnPlayerDied` —
`Pause.Release(SurvivorPause.LevelUpScope);` TRƯỚC `ShowGameOver` (Release no-op khi scope vắng —
an toàn mọi path).

## Phần 2 — Auto-close race vs queue mới → modal giữ card cũ (44b)

**Evidence:** `SkillChoiceService.Close` → `Pump` → Trigger event MỚI cùng roleId trong frame
auto-close; `OverlayPanel.PollSkillChoiceAutoClose` guard chỉ `Current(1u) != null` — KHÔNG check
identity với event đã render. Khi queue khác rỗng lúc auto-close (mode khác enqueue khi levelup
modal mở — box/shop đã trong enum, P2 sẽ wire) → pump trigger event mới → poll thấy Current != null
→ modal canvas giữ + hiện card CŨ, `Select` card lạ → fail-closed từ chối → LevelUpScope không bao
giờ release → pause vô hạn (đúng bug class 44, latent hôm nay vì levelup-only + queue rỗng khi
paused — reachable khi box/shop wire).

**Correction (reviewer đề xuất):**
1. `TryShowSkillChoice` lưu `_renderedEvent = ev` (event đã render lên modal).
2. Poll đổi điều kiện: `var c = SkillService.Current(1u); if (c == null || !ReferenceEquals(c, _renderedEvent))`
   → hide + onClosed (fail-closed đóng thay vì kẹt).
3. Kèm roleId field (`_skillOnClosed`/`_renderedEvent` kèm roleId thay vì `Current(1u)` cố định —
   TryShowSkillChoice đã tham số hóa roleId, poll thì không; hôm nay 1 call site nhưng sửa luôn
   cho đối xứng).

## Acceptance

- [ ] EditMode: test player-die-while-modal-open → LevelUpScope release (count 0 sau OnPlayerDied),
      gameover hiển thị bình thường, timescale không kẹt (restart dọn — assert scope count)
- [ ] EditMode: test auto-close với queue có event mới (2 event cùng roleId) → poll đóng modal
      fail-closed (IsVisible=false + cả 2 scope release), KHÔNG giữ card cũ
- [ ] EditMode survivor suite xanh (267 + test mới)
- [ ] KHÔNG regression: pick card bình thường, auto-close không queue, gameover không modal

## Verified

- (trống — chờ implementer)
