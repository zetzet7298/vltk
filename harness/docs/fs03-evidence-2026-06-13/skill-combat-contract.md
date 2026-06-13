# FS-03A — Skill + Combat Contract (Pinned 2026-06-13)

**Lane:** backend (`/var/www/vltk-mobile/backend`)
**Backend:** FastAPI + PostgreSQL `vltk_game` (uvicorn :8020)
**Pytest verdict:** 38/38 pass (25 cũ + 13 mới trong `test_fs03a_skill_combat_flow.py`)
**Smoke test verdict:** 17/17 curl step pass trên live server (xem `smoke_test.sh` + `0[1-9]_*.json`)

---

## TL;DR — đã pin

| Trục | Giá trị pin |
| --- | --- |
| **8 endpoints chính** | `/v1/skill/{by-role,learn,level-up,cast/check,cast}` + `/v1/combat/{damage/calc,status/tick,pk/check}` |
| **Parity source** | `Assets/StreamingAssets/Reference/KNpc.cpp` (MÃ NGUỒN C++ THẬT — verify qua binary `jx_linux_y`) |
| **Server-authoritative cost** | `POST /v1/skill/cast` đọc tài nguyên + cooldown TỪ DB, TRỪ THẬT, ghi `last_cast_ms` (chống spoof H-SK2/H-SK3) |
| **Predict-reconcile** | Client chỉ hiển thị effect/animation; KHÔNG được tin damage/mana/HP client tự tính — server trả về trạng thái SAU mỗi call |
| **Combat mutate in place** | `damage/calc` và `status/tick` MUTATE `target` tại chỗ (parity KNpc.cpp struct) — response trả kèm state sau |
| **RNG determinism** | `seed` trong `damage/calc` → tất định (khớp `g_Random` của C++) — tiện test + replay |

---

## 1. Parity verification — chân lý từ đâu

Mọi pin đều đối chiếu MÃ NGUỒN C++ THẬT tại
`Assets/StreamingAssets/Reference/KNpc.cpp` (file 6027 dòng, KHÔNG phải
disasm-reverse). Xác minh bằng cách so symbol trong binary `jx_linux_y`
chứa đúng field `m_CurrentMeleeDmgRetPercent`, `m_ManaShield`, `m_PhysicsArmor`…
Đã pin tại `harness-be` decision 0016.

| Module BE | Hàm C++ tham chiếu | Dòng KNpc.cpp |
| --- | --- | --- |
| `combat_resolve.calc_damage` | `KNpc::CalcDamage(nAttacker, nMin, nMax, nType, bIsMelee, bDoHurt, bReturn)` | 2125-2362 |
| `status_effect.process_state` | `KNpc::ProcessState()` | 612-863 |
| `skill_cast.*` (CanCastSkill chain) | `KSkill::CanCastSkill` + getters vtable | 0x08101010 / 0x08107e70-0x08107e90 |

So sánh pipeline CalcDamage (đã verify line-by-line):

| Bước | C++ (KNpc.cpp) | BE (`combat_resolve.calc_damage`) |
| --- | --- | --- |
| Guard target chết / out-region / dmg<=0 | 2127-2131 | `if nmin+nmax<=0: return` ✓ |
| Roll: range<0 dùng `-range`; else `nMin+g_Random(nMax-nMin)` | 2134-2141 | `_roll` ✓ |
| Kháng cap `*ResistMax` rồi `MAX_RESIST=95` | 2143-2292 | `_resist_and_armor` ✓ |
| Giáp hấp thụ: armor-=dmg; armor<0 → dmg=-armor & nTime=0 | 2158-2168, 2163 | ✓ (reset `*_armor_time=0` khi vỡ) |
| COLD/FIRE/LIGHT/POISON LUÔN dùng `RangeDmgRetPercent` cho phản đòn | 2203, 2230, 2255, 2282 | `_reflect_percent` ✓ |
| MAGIC: `nRes=0`, KHÔNG reassign nMax | 2285-2291 | ✓ (return `nmax` gốc) |
| Mana shield: nManaDamage=dmg*%/100; mana<0 → dmg-=mana + shield vỡ + nTime=0 | 2298-2313 | ✓ |
| dmg = dmg*(100-nRes)/100 | 2314 | ✓ |
| Phản đòn (chỉ khi attacker>0 & !bReturn): flat theo melee/range + % theo hệ | 2318-2333 | ✓ |
| PK rate khi cả hai là player | 2336-2337 | ✓ |
| Life -= dmg; damage2mana; DoHurt | 2341-2352 | ✓ |
| Life<0 → chết | 2353-2361 | `target_died=True` ✓ |

So sánh pipeline ProcessState (đã verify):

| Bước | C++ (KNpc.cpp) | BE (`status_effect.process_state`) |
| --- | --- | --- |
| Mỗi `GAME_UPDATE_TIME=10` frame: do_sit → life+=max*3/1000 (min 1) | 623-641 | ✓ |
| Natural regen: life+=lifeReplenish; mana+=manaReplenish (cap max) | 643-649 | ✓ |
| Aura auto-cast khi `m_ActiveAuraID` + level>0 | 660-722 | ✓ (BE trả `aura_cast_skill_id/level`, app tầng trên thực hiện cast) |
| Armor timers: nTime>0 → --; hết hạn → value0=0 | 728-780 | `_decay_armor` ✓ |
| Mana shield: nTime>0 → --; hết hạn → percent=0 | 773-780 | ✓ |
| Poison: nTime--; value1==0 → =1; khi nTime%value1==0 → DoT `damage_poison` từ `poison_source` (bReturn=TRUE) | 783-794 | ✓ (gọi `calc_damage(...is_return=True)`) |
| Freeze: nTime--; nTime&1 → nRet=TRUE | 796-803 | ✓ |
| Burn: nTime--; mỗi value1 frame → DoT `damage_fire` lên chính mình (bReturn=FALSE) | 805-816 | ✓ (gọi `calc_damage(...attacker=target, is_return=False)`) |
| Confuse: nTime--; về 0 → `confuseEnded` | 818-825 | ✓ |
| Stun: nTime--; nRet=TRUE | 827-831 | ✓ |
| LifeState HoT: nTime%10==0 → life+=value0 (cap max) | 833-845 | ✓ |
| ManaState MoT: nTime%10==0 → mana+=value0 (cap max) | 846-858 | ✓ |
| Drunk: nTime-- | 859-863 | ✓ |

---

## 2. Pinned schemas (OpenAPI snapshot 2026-06-13)

Nguồn: `curl http://127.0.0.1:8020/openapi.json` (lưu tại `openapi.json`).
Tất cả schema `extra=forbid` → gửi field lạ → 422. CamelCase JSON; snake_case trong Python.

### 2.1 Endpoints skill (5)

| Method | Path | Mục đích |
| --- | --- | --- |
| `POST` | `/v1/skill/learn` | Học kỹ năng mới (parity LearnMagic) |
| `POST` | `/v1/skill/by-role/{role_id}/level-up/{skill_id}` | Nâng cấp kỹ năng (parity UpdateSkill) |
| `GET` | `/v1/skill/by-role/{role_id}` | Liệt kê kỹ năng đã học (parity GetSkillLevel) |
| `POST` | `/v1/skill/cast/check` | Pre-flight check (stateless, dev/test dùng) |
| `POST` | `/v1/skill/cast` | **Server-authoritative**: trừ resource + ghi cooldown |

### 2.2 Endpoints combat (3 trong 8 chính)

| Method | Path | Mục đích |
| --- | --- | --- |
| `POST` | `/v1/combat/damage/calc` | Tính sát thương parity KNpc::CalcDamage |
| `POST` | `/v1/combat/status/tick` | Tiến 1 frame parity KNpc::ProcessState |
| `POST` | `/v1/combat/pk/check` | Kiểm tra server-side PK hợp lệ (vùng an toàn / khác phe) |

### 2.3 Request/Response schemas

#### `SkillLearnRequest` (`POST /v1/skill/learn`)
```json
{
  "roleId": 1,        // required, int>=1
  "skillId": 22,      // required, int>=1 (phải có trong skills.txt)
  "charLevel": 50,    // required, 1..200
  "faction": 0        // 0..9; -1 = chưa nhập phái
}
```
**Errors:**
- `404` "Kỹ năng không có trong bảng định nghĩa" — skillId không tồn tại
- `409` "Nhân vật đã học kỹ năng này" — duplicate
- `422` "Chưa đủ cấp độ yêu cầu" — charLevel < template.req_level
- `422` Pydantic body invalid

#### `PlayerSkillResponse` (của learn / list / level-up)
```json
{
  "id": 1,
  "roleId": 1,
  "skillId": 22,
  "level": 1,
  "isActive": true,
  "skillName": "Kim Ba",   // đính kèm từ template (nếu có)
  "maxLevel": 20
}
```

#### `SkillCastCheckRequest` (`POST /v1/skill/cast/check`)
```json
{
  "roleId": 1,
  "skillId": 210,
  "currentMana": 300,      // 0..∞ (stateless check)
  "currentLife": 1000,
  "currentStamina": 100,
  "onHorse": false,
  "relation": 2,           // 0=self, 1=ally, 2=enemy, 3=other
  "distance": 0,           // 0..∞; 0 = bỏ qua tầm (skill không dùng IsUseAR)
  "weaponType": 0,         // 0=không yêu cầu; 1=kiếm; 2=đao; ...
  "equipState": -2,        // -2 = không giới hạn
  "nowMs": 1000,           // mốc hiện tại (ms)
  "lastCastMs": 0          // mốc thi triển gần nhất (0 = chưa cast)
}
```
**Response 200:**
```json
{
  "skillId": 210,
  "canCast": true,
  "reason": null,           // null khi OK; "Vũ khí đang trang bị không phù hợp" v.v.
  "costType": 1,            // 0=none, 1=mana, 2=life, 3=stamina
  "costValue": 50,
  "delayPerCast": 0,        // ms — từ TimePerCast hoặc TimePerCastOnHorse
  "nextCastTime": 0         // mốc sẵn sàng (input.lastCastMs nếu có)
}
```
**Errors:** 404 "Nhân vật chưa học kỹ năng này"; 422 "Kỹ năng không có trong bảng định nghĩa".

#### `SkillCastRequest` (`POST /v1/skill/cast`) — **SERVER-AUTHORITATIVE**
```json
{
  "roleId": 1,
  "skillId": 210,
  "onHorse": false,         // gate không-spoofable (vẫn từ client)
  "relation": 2,
  "distance": 0,
  "weaponType": 0,
  "equipState": -2,
  "nowMs": 1000             // server gửi xuống, >=1
}
```
**LƯU Ý:** KHÔNG nhận `currentMana`/`currentLife`/`currentStamina`/`lastCastMs` từ
client. Server đọc thẳng từ `player_states` + `player_skills.last_cast_ms`. Chống
H-SK2 (spoof cost) + H-SK3 (spoof cooldown).

**Response 200:**
```json
{
  "skillId": 210,
  "cast": true,
  "costType": 1,
  "costPaid": 50,
  "currentLife": 1000,
  "currentMana": 250,        // 300 - 50 (server-authoritative trừ thật)
  "currentStamina": 0,
  "nextCastTime": 1000,      // nowMs + max(delay, waitTime)
  "effects": []              // danh sách (attrib, effect_key, p1, p2, p3) sau nội suy
}
```
**Errors:**
- `404` "Nhân vật chưa học kỹ năng này"
- `404` "Không tìm thấy trạng thái nhân vật" — thiếu `player_states`
- `409` "Không thể thi triển kỹ năng lúc này" — gate fail (relation/range/weapon/eqt/horse)
- `409` "Kỹ năng đang trong thời gian hồi" — cooldown gate
- `409` "Không đủ tài nguyên để thi triển kỹ năng" — resource gate

#### `DamageCalcRequest` (`POST /v1/combat/damage/calc`)
```json
{
  "atkMin": 200,
  "atkMax": 200,
  "damageKind": 0,           // 0=physics, 1=cold, 2=fire, 3=light, 4=poison, 5=magic
  "isMelee": true,
  "isReturn": false,         // chính đòn phản — chặn đệ quy (KNpc.cpp:2318)
  "pkDamageRate": 100,       // % dmg khi cả hai là player (default 100)
  "target": {                // CombatantState — XEM schema bên dưới
    "life": 1000, "lifeMax": 1000,
    "physicsArmor": 50, "physicsResist": 20,
    "..."
  },
  "attacker": null,          // optional
  "seed": 1                  // optional — null = random
}
```
**Response 200 (với `armor+resist` example):**
```json
{
  "damage": 120,             // (200-50) * (100-20)/100 = 120
  "manaAbsorbed": 0,
  "armorAbsorbed": 50,
  "manaShieldBroke": false,
  "targetDied": false,
  "reflectToAttacker": 0,
  "reflectKind": 5,          // magic
  "target": { ...mutated... }  // life=880, physicsArmor=0 (đã trừ armor)
}
```

#### `CombatantStateSchema` — trạng thái thực thể (cho cả damage/calc + status/tick)
```json
{
  "life": 1000, "lifeMax": 1000,
  "mana": 0, "manaMax": 0,

  "physicsResist": 0, "coldResist": 0, "fireResist": 0,
  "lightResist": 0, "poisonResist": 0,
  "physicsResistMax": 95, "coldResistMax": 95, "fireResistMax": 95,
  "lightResistMax": 95, "poisonResistMax": 95,   // trần kháng cứng (MAX_RESIST=95)

  "physicsArmor": 0, "coldArmor": 0, "fireArmor": 0,
  "lightArmor": 0, "poisonArmor": 0,
  "physicsArmorTime": 0, "coldArmorTime": 0, "fireArmorTime": 0,
  "lightArmorTime": 0, "poisonArmorTime": 0,     // timer giáp (KNpc.cpp:728-780)

  "manaShieldPercent": 0, "manaShieldTime": 0,   // khiên nội lực
  "meleeDmgRet": 0, "rangeDmgRet": 0,
  "meleeDmgRetPercent": 0, "rangeDmgRetPercent": 0,
  "damage2ManaPercent": 0,                        // % dmg -> hồi nội
  "isPlayer": false
}
```

#### `StatusTickRequest` (`POST /v1/combat/status/tick`)
```json
{
  "target": { ...CombatantStateSchema... },
  "status": {              // StatusBundleSchema — default tất cả time=0
    "poisonState": {"value0": 15, "value1": 2, "time": 3},
    "freezeState": {"time": 0},
    "burnState":  {"time": 0},
    "confuseState": {"time": 0},
    "stunState":  {"time": 0},
    "lifeState":  {"value0": 20, "time": 10},     // HoT
    "manaState":  {"value0": 15, "time": 10},     // MoT
    "drunkState": {"time": 0}
  },
  "loopFrames": 1,         // m_LoopFrames hiện tại (regen mỗi %10==0)
  "isSitting": false,
  "lifeReplenish": 30,
  "manaReplenish": 20,
  "poisonSource": null,    // kẻ gây độc cuối (m_nLastPoisonDamageIdx)
  "activeAuraId": 0,       // m_ActiveAuraID
  "activeAuraLevel": 0
}
```
**Response 200 (poison DoT example):**
```json
{
  "controlled": false,       // bị freeze odd-tick hoặc stun
  "confuseEnded": false,
  "dotResults": [
    {"damage": 15, "manaAbsorbed": 0, "armorAbsorbed": 0,
     "manaShieldBroke": false, "targetDied": false,
     "reflectToAttacker": 0, "reflectKind": 5}
  ],
  "auraCastSkillId": 0,
  "auraCastLevel": 0,
  "target": { ...mutated life=985... },
  "status": { ...mutated poisonState.time=2... }
}
```

#### `PkCheckRequest` (`POST /v1/combat/pk/check`)
```json
{
  "attackerCamp": 1,        // 0=chưa phân phe
  "targetCamp": 2,
  "mapType": "City",        // City/Capital/Field/Battlefield/...
  "inBattle": true          // đang trong chiến trường
}
```
**Response 200:**
```json
{
  "canAttack": false,
  "mapPkAllowed": false,
  "isSafeZone": true,
  "reason": "Vùng an toàn — cấm PK"   // null khi OK
}
```

---

## 3. Cost types — giá trị tài nguyên tiêu hao

| `costType` | Hằng BE | Ý nghĩa | Field DB đọc/ghi | Khi `costValue=0` |
| --- | --- | --- | --- | --- |
| `0` | `COST_TYPE_NONE` | Không tiêu hao | — | OK — không đụng resource |
| `1` | `COST_TYPE_MANA` | Nội lực (mana) | `player_states.current_mana` | OK — không trừ |
| `2` | `COST_TYPE_LIFE` | Sinh lực (HP) | `player_states.current_life` | OK — không trừ |
| `3` | `COST_TYPE_STAMINA` | Thể lực (stamina) | `player_states.current_stamina` | OK — không trừ |

**Quy ước parity:** nếu `costType` lạ (không nằm trong `{0,1,2,3}`), `has_enough_resource`
trả `True` (engine PC bỏ qua loại không xác định — xem `skill_cast.py:74-75`).

**Skill test data (verified từ `settings/skills.txt` PC):**
- Skill 22 (Kim Ba): `costType=0`, `costValue=0`, `wait=5`, target_enemy+ally.
- Skill 210 (Khinh công): `costType=1`, `costValue=50`, `wait=5`, no-target.
- Skill 1 (Công kích vật lý): `costType=0`, `costValue=0`, `wait=5`, `weaponSkill=1`, `attackRadius=100`, `isUseAR=1`.
- Skill 100 (Hồi máu/Hồn Băng): `costType=0`, `costValue=0`, `targetSelf=1` (heal).
- Skill 333 (Phục Đế Trùng Sinh): `costType=0`, `costValue=0`, `targetSelf+Ally=1` (rez/heal).

---

## 4. Server-authoritative rules — chống spoof

### 4.1 `POST /v1/skill/cast` — KHÔNG tin client về resource & cooldown

| Field | Client gửi? | Server đọc từ đâu? | Lý do |
| --- | --- | --- | --- |
| `currentMana/Life/Stamina` | ❌ KHÔNG nhận | `player_states.current_*` (DB) | H-SK2: chống client tự +1000 mana rồi cast |
| `lastCastMs` | ❌ KHÔNG nhận | `player_skills.last_cast_ms` (DB) | H-SK3: chống client tự set last=0 để bypass cooldown |
| `nowMs` | ✅ CÓ | dùng để so cooldown, ghi `last_cast_ms` | Cần mốc "bây giờ" để tính `next = nowMs + max(delay, wait)` |
| `onHorse/relation/distance/weaponType/equipState` | ✅ CÓ | dùng gate | Đây là context runtime từ client (không lưu DB) — engine PC cũng đọc từ Npc state runtime |

### 4.2 Chuỗi gate `KSkill::CanCastSkill` (KSkills.cpp:300-421)

Thứ tự kiểm tra (gate fail sớm → trả reason đầu tiên, KHÔNG trừ resource):

1. `template is not None` (skill có trong skills.txt)
2. `learned is not None` (đã học — parity HaveMagic >= 0)
3. **Relation gate** — targetEnemy/Ally/Self/Other khớp `relation`
   - Skill no-target (cả 4 cờ = 0) → BỎ QUA gate (vd skill 210)
4. **Range gate** — `IsUseAR` + `distance <= AttackRadius`
5. **Weapon gate** — `WeaponSkill=0` hoặc `weaponType` khớp
6. **Eqt gate** — `EqtLimit=-2` (không giới hạn) hoặc `equipState` khớp
7. **Horse gate** — `HorseLimit=0` (cho cưỡi) hoặc `onHorse=false`
8. **Cooldown gate** — `nowMs >= lastCastMs + max(delay, waitTime)`
9. **Resource gate** — `costValue<=0` hoặc tài nguyên hiện tại `>=` cost

Gate 3-7 dùng `_` (gate fail → `err_skill_cannot_cast` 409, không phân biệt loại).
Gate 8 riêng (`err_skill_on_cooldown`). Gate 9 riêng (`err_not_enough_resource`).

### 4.3 Cooldown — formula parity

`delay = max(TimePerCast, TimePerCastOnHorse khi on_horse, WaitTime)`
`next_ready = last_cast_ms + delay` (server lưu `last_cast_ms = nowMs` ngay khi cast OK)
`sẵn_sàng = nowMs >= next_ready`
**Lần đầu (`last_cast_ms <= 0`) luôn sẵn sàng.**

Ví dụ skill 210 (`WaitTime=5`): cast nowMs=1000, sau đó:
- nowMs=1001: cooldown (1001 < 1005) → 409
- nowMs=1005: `1005 >= 1000+5=1005` → OK
- nowMs=1010: OK, trừ mana lần nữa

---

## 5. Predict-reconcile contract for client (Unity)

| Hành động client | Được phép predict (chỉ để hiển thị/animation) | PHẢI reconcile (chờ server) |
| --- | --- | --- |
| Hiển thị cooldown bar | Local timer với delay/wait đã biết | Tắt nếu server trả cooldown khác (vd cast 2 nhân vật) |
| Animation skill | Ngay khi gửi cast request | KHÔNG ảnh hưởng logic |
| HP/Mana hiển thị sau cast | Ngay: `currentMana -= cost` (chỉ local) | **PHẢI** dùng `currentMana` server trả trong response |
| Damage hiển thị | Animation dựa theo skill ID | **PHẢI** dùng `damage` server trả từ `/v1/combat/damage/calc` (KHÔNG tính local) |
| DoT dot tick | Animation dot trên UI | **PHẢI** dùng `dotResults` + `target.life` server trả từ `/v1/combat/status/tick` |
| Stun/freeze effect | Local flash UI | **PHẢI** dùng `controlled` server trả |

**Quy tắc vàng:** mọi giá trị số (HP/mana/damage/dotResults) đều KHÔNG tin
client tính — chỉ tin `target`/`currentLife`/`currentMana` server trả. Animation
được phép predict trước; số thì đợi server.

### 5.1 Vì sao `damage/calc` và `status/tick` MUTATE state

`combat_resolve.calc_damage` và `status_effect.process_state` thay đổi `target`
tại chỗ (parity KNpc.cpp vốn mutate struct Npc[] toàn cục). Để hỗ trợ chuỗi
nhiều đòn/ nhiều frame, client gửi state hiện tại lên, server mutate, trả về
state sau. Client KHÔNG cần merge chính xác — chỉ việc thay thế state local bằng
state server trả. Combat state cũng là "session resource" (không persist DB) nên
không có vấn đề concurrency.

### 5.2 Khi nào dùng `damage/calc` vs gọi thẳng skill.cast?

- **`POST /v1/skill/cast`** (server-authoritative) dùng cho gameplay chính: trừ
  resource, ghi cooldown, áp hiệu ứng. Đây là endpoint mà player trigger qua UI.
- **`POST /v1/combat/damage/calc`** dùng cho:
  - NPC AOE (skill quái cast nhiều đòn cùng lúc lên nhiều mục tiêu — server batch).
  - Damage environmental (lửa, bẫy, lời nguyền).
  - Damage khi đánh thường (skill hành động) — cần thêm step ở tầng combat nếu
    gắn với resource.
- **`POST /v1/combat/status/tick`** dùng mỗi frame combat (~10Hz) cho tất cả
  entity trong vùng. Server tự xử lý poison/freeze/stun/HoT/MoT aura.

---

## 6. Error matrix (đối chiếu HTTP code + detail)

| Endpoint | Code | Detail | Khi nào |
| --- | --- | --- | --- |
| `learn` | 404 | "Kỹ năng không có trong bảng định nghĩa" | skillId không có trong skills.txt |
| `learn` | 409 | "Nhân vật đã học kỹ năng này" | duplicate |
| `learn` | 422 | "Chưa đủ cấp độ yêu cầu" | charLevel < req_level |
| `level-up` | 404 | "Nhân vật chưa học kỹ năng này" | chưa học |
| `level-up` | 422 | "Kỹ năng đã đạt cấp tối đa" | level >= max_level |
| `cast/check` | 404 | "Nhân vật chưa học kỹ năng này" | chưa học |
| `cast/check` | 422 | "Kỹ năng không có trong bảng định nghĩa" | skillId lạ |
| `cast` | 404 | "Nhân vật chưa học kỹ năng này" | chưa học |
| `cast` | 404 | "Không tìm thấy trạng thái nhân vật" | thiếu player_states |
| `cast` | 409 | "Không thể thi triển kỹ năng lúc này" | gate fail (relation/range/weapon/eqt/horse) |
| `cast` | 409 | "Kỹ năng đang trong thời gian hồi" | cooldown gate |
| `cast` | 409 | "Không đủ tài nguyên để thi triển kỹ năng" | resource gate |
| `damage/calc` | 422 | Pydantic body invalid | field lạ / out-of-range |
| `status/tick` | 422 | Pydantic body invalid | field lạ / out-of-range |
| `pk/check` | 422 | Pydantic body invalid | field lạ / out-of-range |
| `pk/check` | 200 với `canAttack=false` | "Vùng an toàn — cấm PK" | mapType=City/Capital |
| `pk/check` | 200 với `canAttack=false` | "Cùng phe hoặc chưa phân phe — không thể tấn công" | inBattle + same camp |
| `record_kill` | 403 | "Không thể tấn công người cùng phe trong chiến trường" | same camp kill |
| `record_kill` | 404 | "Trận đấu không tồn tại" | battle_id lạ |

---

## 7. Test verdict (38/38 pass, 13 mới)

### 7.1 Test mới trong `test_fs03a_skill_combat_flow.py` (13)

| Test | Pin |
| --- | --- |
| `test_cast_zero_cost_does_not_change_resources` | costType=0: cast KHÔNG đụng resource (chỉ ghi last_cast_ms) |
| `test_cast_horse_limit_blocks_when_skill_prohibits_horse` | skill 210 horseLimit=1: on_horse=true → 409 + KHÔNG trừ mana |
| `test_cast_succeeds_within_max_level_then_level_up` | max=20: cast ở cấp 1 + 19 level-up, level-up 21 → 422 |
| `test_damage_calc_cold_armor_absorbs_full` | COLD armor: 50 armor hấp thụ 50 dmg, còn 50→life=950 |
| `test_damage_calc_reflect_uses_range_percent_for_cold` | COLD: phản đòn dùng range% bất kể melee; 3 case (cold+melee/cold+range/phy+melee) |
| `test_damage_calc_damage2mana_drains_to_target_pool` | damage2ManaPercent=50: 100 dmg → +50 mana (cap max) |
| `test_status_tick_armor_timers_decay_to_zero` | 3 frame decay physics+fire armor timer, hết hạn → value0=0 |
| `test_status_tick_freeze_odd_tick_controls` | freeze time=3: tick 1 chẵn (no control), tick 2 lẻ (controlled) |
| `test_status_tick_confuse_ended_flag_on_last_frame` | confuse time=1: tick cuối → confuseEnded=true |
| `test_status_tick_sitting_bonus_regen_on_game_update_time` | is_sitting + loopFrames=0: bonus max*3/1000 + natural regen |
| `test_status_tick_hot_applies_every_game_update_time` | lifeState HoT: time 10→9 chưa HoT, time 9→8 vẫn chưa (parity: time SAU decrement % 10 == 0) |
| `test_status_tick_burn_dot_self_damage` | burnState value1=1: time 5→4, 4%1==0 → DoT 20 lên chính mình (bReturn=FALSE) |
| `test_full_combat_loop_cast_then_damage_then_status` | End-to-end: cast 300→250 mana, damage 1000→880 life, DoT poison 880→865 life, cast cooldown vẫn enforce |

### 7.2 Test cũ (25 — từ GD5/GD6)

`test_skill.py` (5) + `test_skill_cast_authoritative.py` (6) + `test_combat.py` (14):
- Learn/list/level-up happy + reject (duplicate, unlearned, unknown).
- Cast: trừ mana, cooldown gate, not-enough-mana, unlearned, missing player_state.
- Damage: armor+resist, mana shield break, reflect melee/range.
- Status: poison DoT decay/applies, stun controls, regen every 10 frames.
- PK: safe zone block, battlefield allowed, kill score parity, same-camp reject.

---

## 8. Known limits / TODO future

- **Stamina gate cast:** schema `SkillCastCheckRequest`/`SkillCastRequest` có
  `currentStamina` nhưng service chưa gate độc lập — cast dùng
  `player_states.current_stamina` (BE chưa seed runtime stamina ngoài test).
- **is_player on DoT:** `status_effect.process_state` gọi `calc_damage(
  target, poison_source, ...)` nhưng chưa truyền `is_player` của poison_source
  — PK rate trong DoT chưa áp dụng. Sẽ port khi US-P3 (combat PK pipeline
  end-to-end).
- **`skill_cast.py:166`** trả 422 cho skillId lạ (template None) — không có
  detail đặc thù ngoài "Kỹ năng không có trong bảng định nghĩa".
- **`Aura auto-cast`**: `process_state` trả `aura_cast_skill_id/level` nhưng
  tầng application combat chưa loop cast aura cho mỗi entity — TODO port ở
  US-P3.
- **Skill 22 max_level=20 chỉ từ skills.txt** — verify lại sau khi thấy 30 chuỗi
  cast liên tiếp (chưa test stress trong FS-03A).

---

## 9. Files reference

- **Parity source:** `/var/www/vltk-mobile/Assets/StreamingAssets/Reference/KNpc.cpp`
  (CalcDamage 2125-2362; ProcessState 612-863).
- **BE combat:** `app/modules/combat/domain/{combat_resolve,status_effect,damage}.py`.
- **BE skill:** `app/modules/skill/domain/{skill_cast,skill_logic,skills,constants,exceptions}.py`.
- **BE service:** `app/modules/skill/application/service.py` + `app/modules/combat/application/service.py`.
- **Tests:** `tests/integration/modules/skill/{test_skill,test_skill_cast_authoritative,test_fs03a_skill_combat_flow}.py`
  + `tests/integration/modules/combat/test_combat.py`.
- **Smoke test:** `smoke_test.sh` (17 steps, all 2xx except step 11 expected 409).
- **Pytest log:** `pytest_fs03a_skill_combat.log`.
- **OpenAPI snapshot:** `openapi.json` (112954 bytes).
