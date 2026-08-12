# Phi Long Tại Thiên + NPC Death — Port Status (2026-06-15)

## Nguyên tắc

**Mọi behavior implement PHẢI có PC source.** Không có source → TODO rõ ràng, KHÔNG đoán.

PC source: `00.src-tinh-kiem/Server 6.0/server/home_jxser_bachkim_6.0/server1/jx_linux_y`
(ELF32 binary, có debug info, symbol KNpc::* đầy đủ).

---

## Phi Long Tại Thiên (skill 357) — Dash / Lunge

### PC source (binary evidence)

| Function | Offset | Vai trò |
|---|---|---|
| `KNpc::CastMeleeSkill(KSkill*)` | 0x0809bfa0 | Switch dispatch theo skill type |
| `KNpc::DoRunAttack()` | 0x0809b9c0 | **Close-range lunge** — sets `m_214 = 0x12` (LUNGE_STATE) |
| `KNpc::NewJump(int, int)` | 0x08099fd0 | **Long-range jump** — `TestMovePos` check, distance ở `m_1834` |
| `KNpc::RunTo(int, int)` | 0x0809c08d | Fallback run (no jump possible) |
| `KNpc::DoBlurAttack` | 0x0809bed0 | Multi-hit attack (no lunge) |
| `KNpc::DoManyAttack` | 0x0809bb10 | Many-target attack |

### Mobile port (chỉ những gì có PC source)

✅ **Dash state machine** (`SandboxPlayerController`):
- `BeginDash(target, duration)` API — lerp position qua nhiều frame
- `IsDashing`, `DashProgress`, `DashStartPos`, `DashTargetPos` public properties
- `SimulateMove` skip normal movement khi `IsDashing=true`
- `CancelDash()` cho stun/knockback edge cases
- PC semantic: lerp, KHÔNG teleport (PC client engine handles smooth animation)

✅ **`SkillDefinition.dashDurationSeconds` field** — caller cung cấp duration.

✅ **CombatSkillSlotController wire-up**:
- Sau `Cast()` success: `player.BeginDash(caster.position, skill.dashDurationSeconds)`
- Nếu `dashDurationSeconds == 0`: skip dash + log warning (TODO PC runtime observation)

### ❌ Không implement (không có PC source)

❌ **`dashDurationSeconds` value** — PC server chỉ set state + distance. Duration thuộc client engine animation.
- **TODO(PC-runtime)**: cần PC runtime video để verify chính xác duration bao nhiêu giây cho mỗi skill.
- Hiện tại: tất cả skill `dashDurationSeconds = 0` → dash bị skip. **Mày sẽ thấy player KHÔNG dash** cho tới khi có PC runtime evidence.

### Verify trong Play mode (Unity MCP)

```
BEFORE BeginDash: pos=(53246, -52041)
BeginDash(target=(53296, -52041), duration=0.2f) → IsDashing=True, Progress=0.0
After 1s: pos=(53296, -52041), IsDashing=False, Progress=1.0 ✓
```

Lerp smooth, không teleport. Đúng PC state machine semantic.

---

## NPC Death

### PC source (binary evidence)

| Function | Offset | Vai trò |
|---|---|---|
| `KNpc::OnDeath()` | 0x0809dea0 | `WaitForFrame` → jump `OnDeathProcess` |
| `KNpc::OnDeathProcess()` | 0x0809dad0 | Switch theo `m_24` (1=player, 2=partner, 0=monster) |
| `KNpc::DoDeath(int, int)` | 0x0809def0 | Set state `m_214 = 0xa` (DEATH_STATE), `ClearProcessAI()` |
| `KNpc::ClearProcessAI()` | 0x08090cf0 | Stop AI loop |
| `KNpc::Revive()` | 0x080a0e10 | `RestoreNpcBaseInfo` + `Mps2Map` (respawn flow) |
| Field `m_CorpseSettingIdx` | — | Index vào corpse SPR list |
| Field `CorpseIdx` (class name in binary) | — | Lookup corpse sprite theo NPC template |
| Field `m_DeathFrame` | — | Death animation frame counter |

### Mobile port (chỉ những gì có PC source)

✅ **Death state machine** (`BaLangEnemyRuntime`):
- Public `IsDead` flag (PC semantic: `m_214 = 0xa`)
- `SetLife(currentLife, showDamage)`: khi `CurrentLife <= 0 && previousLife > 0` → set `_isDead = true`
- `Tick()`: skip toàn bộ AI logic khi `_isDead` (PC semantic: `ClearProcessAI` stops AI loop)

✅ **EditMode test** `BaLangEnemyAi_IsDead_Flag_IsPublic`:
- Verify `IsDead = false` khi fresh NPC
- Verify `IsDead = true` sau `SetLife(0)`

### ❌ Không implement (không có PC source)

❌ **Corpse sprite swap** — `CorpseIdx` + `m_CorpseSettingIdx` fields tồn tại trong binary NHƯNG
SPR mapping data không có trong source tree (`00.src-tinh-kiem/pak_unpacked/`).
- **TODO(PC-runtime)**: cần client runtime video để verify corpse sprite nào được dùng cho mỗi NPC.
- Hiện tại: body **vẫn hiển thị** sau khi chết (không hide, không swap sang corpse). Mày sẽ thấy NPC "đứng bất động" thay vì animation corpse.
- **User-visible bug**: "heo trắng chết mà vẫn hiện diện" — chưa fix được vì cần PC source.

❌ **Respawn delay value** — `KNpc::Revive()` function tồn tại nhưng delay constant KHÔNG có trong binary.
- **TODO(PC-runtime)**: cần PC runtime video để verify respawn bao nhiêu giây.
- Hiện tại: **KHÔNG respawn**. NPC chết → body đứng yên vĩnh viễn. Mày phải dùng GM tool hoặc restart map.
- **Quyết định**: KHÔNG fake "5s respawn" như commit cũ (đã revert).

❌ **Death animation frame count** — `m_DeathFrame` field có nhưng count logic không accessible.
- **TODO(PC-runtime)**: cần video để đếm frame.

### Verify trong Play mode (Unity MCP)

```
Heo Enemy_593 SetLife(0):
  IsDead=False → True ✓
  parent.activeSelf=True (giữ active cho AI stop logic)
  NpcSprite/NpcShadow/Nameplate=True (KHÔNG hide — TODO corpse)
  (PC source: AI stops, but body should swap to corpse sprite)
```

---

## Gap summary cho User (mày)

| Bug mày báo | PC source có? | Status |
|---|---|---|
| Phi Long visual "tu luot" (player tự lướt tới mục tiêu) | Có (DoRunAttack state 0x12) | ✅ **Fix xong**: state machine + lerp. **NHƯNG duration=0 cho tất cả skill** → dash bị skip cho tới khi có PC runtime evidence. |
| Phi Long visual "vẫn như cũ" (không thấy gì) | Có | ✅ **API ready**, cần duration thực tế từ PC runtime |
| Heo trắng chết vẫn hiện diện | Có (DoDeath + ClearProcessAI) | 🟡 **Partial**: AI stops (`IsDead=true`, Tick skip). **Body vẫn hiện** vì cần corpse SPR data (no PC source). |
| Heo respawn sau 5s | Có (Revive function) | ❌ **Không implement**: delay value không có trong PC source. Cần PC runtime evidence. |

## Cần từ mày

1. **PC runtime video** Phi Long Tại Thiên cast ở close-range (5-10s clip):
   - Đo chính xác dash duration
   - Verify dash có smooth hay instant
2. **PC runtime video** NPC chết + respawn (10-15s clip):
   - Đo corpse animation frames + duration
   - Đo respawn delay
3. Hoặc: **accept MVP incomplete** + document gaps, không fake

Mày cung cấp evidence → tao wire up đúng PC behavior. Không có evidence → behavior đó dừng ở TODO, không đoán.
