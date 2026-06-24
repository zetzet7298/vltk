# PC Source Limitations & Remediation Roadmap

**Date**: 2026-06-16
**Context**: Identified while auditing Cái Bang skills 127/130 against
PC source at `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/`

## What we have

| Resource | Location | Coverage |
|---|---|---|
| Lua skill formulas | `Client 6.0/script/skill/*.lua` | 100% — formulas, attrs, values |
| Settings data | `Client 6.0/settings/*.txt` | 100% — skill table, missile table, state mapping |
| SPR assets | `pak_unpacked/skills/**/*.spr` | 100% — byte-identical to Unity `StreamingAssets/Sprites/` |
| Server-side Lua | `Server 6.0/server/home_jxser/server1/script/` | 100% — identical to client scripts |
| Server binary | `Server 6.0/server/home_jxser/server1/jx_linux_y` | binary, ELF 32-bit, 30.7MB |
| Server libraries | `libheaven.so` (178KB), `librainbow.so` (103KB) | binary, ELF 32-bit |
| Client binary | `Client 6.0/` | compiled executables, not source |

## What we do NOT have

| Missing | Where it normally lives | Impact |
|---|---|---|
| C++ source (KSkill, KPlayer, KNpc) | Should be in `SwordOnline/Sources/` | Engine-level logic, formulas, opcodes |
| C headers (SceneDataDef.h, KNode.h, etc.) | Same | Struct layout, constants |
| Protocol documentation | Usually in `protocol*.txt` or header comments | Network opcode tables |
| Build system (Makefile, .vcxproj) | Same | Compile-time defines, target info |
| Debug symbols | Binary stripped | Function names only via demangled exports |

## Identified limitations (engine-level values)

These values are NOT in the Lua scripts and NOT in the settings files.
They live in the compiled C++ engine and can only be recovered by either
reverse-engineering the binary or capturing runtime from a live PC client.

### Limitation 1: Cast time formula

**Status**: NOT IMPLEMENTED in Unity (hardcoded `timePerCast=2` for most skills)

**PC engine behavior** (reverse-engineered from binary, partial):
- `KSkill::GetDelayPerCast(int level)` returns `m_nDelayPerCast[level]` if
  level ≠ 0, else `m_nBaseDelayPerCast`
- These arrays are loaded from `skills.txt` column `TimePerCast` (column 64)
- Formula on top of base: `actualDelay = baseDelay × (1 - castspeed_v × 0.01)`
  (standard PC formula; not yet verified by binary disassembly of
  `KNpcAttribModify::CastSpeedV`)
- `skill_mintimepercast_v` sets a floor (used by Thiên Nhẫn, Đường Môn, etc.,
  not by Cái Bang)

**What this means for skills 127/130**:
- Unity uses default 0.4s PreCast instead of PC value
- Cast time does not scale with player's `castspeed_v` attribute
- Cooldown does not honor `cooldown_v` attribute

**Remediation (effort: 2-3 hours)**:
1. Read `TimePerCast` column from `skills.txt` in `PcSkillSourceLinkParser`
2. Read `CooldownTime` column from `skills.txt` (default 0)
3. Read `castspeed_v` / `cooldown_v` from player state at cast time
4. Apply formula in `SkillEffectVisualService` PreCast calc (line 149 / 255)
5. Test: verify cast time scales correctly with `castspeed_v` buffs

**Risk**: Low. The formula is well-documented in PC modding community;
binary disassembly only needed if exact multiplicative constant differs
from 0.01 (i.e., is it 0.01 or 0.005 or other?).

---

### Limitation 2: Cooldown / WaitTime

**Status**: NOT IMPLEMENTED in Unity

**PC engine behavior** (inferred from Lua/binary):
- `skills.txt` column `WaitTime` (col 65) = minimum interval between casts
- Stored as `m_nWaitTime` in `KSkill`
- `KSkillList::CanCast(skillId, currentTime, ...)` checks against last cast time
- Some skills have `m_nMinTimePerCast` for floor enforcement

**What this means for skills 127/130**:
- Skills 127 and 130 can be cast back-to-back without delay
- Should be at least 1-2s cooldown in PC (typical for self-buffs)

**Remediation (effort: 1 hour)**:
1. Read `WaitTime` from `skills.txt` per skill
2. Add `lastCastTime` tracking in `CombatRuntimeService`
3. Block cast if `now - lastCastTime < WaitTime * 0.055f` (ticks→seconds)

**Risk**: Low. Standard pattern.

---

### Limitation 3: State machine rules (dispel / stack / refresh)

**Status**: PARTIALLY IMPLEMENTED — basic state application works, advanced
rules (dispel on hit, stack limit, refresh-on-reapply) NOT verified.

**PC engine behavior**:
- `KNpc::SetStateSkillEffect` accepts a `KMagicAttrib` and applies state
- Dispel logic: not yet traced (depends on `KMagicAttrib.nValue[2]` mode field)
- Stack behavior: most buff states do NOT stack; reapplying refreshes duration
- Some states are flagged as `Negative` (debuff) and can be dispelled by
  `StateTranspV`/`negative_state_resist_p` attributes

**What this means for skills 127/130**:
- Skill 127 (`fastwalkrun_p`): re-casting should refresh duration, NOT stack
- Skill 130 (`allres_p` etc.): same — refresh, not stack
- Unity `ApplyStates` uses `receiver.states[attr.kind] = attr;` which
  effectively replaces — may or may not match PC behavior

**Remediation (effort: 1-2 hours research + 1 hour code)**:
1. Read `gaibang.lua` more thoroughly — check for `cast_hooks` /
   `on_apply` / `on_remove` patterns
2. If absent, check binary `KNpc::SetStateSkillEffect` for dispel logic
3. Verify Unity's replace-on-set behavior matches PC's refresh behavior
4. Test: cast skill 127 twice, verify only one buff active with refreshed duration

**Risk**: Medium. PC dispel logic is non-trivial; misimplementation may
allow unintended stacking.

---

### Limitation 4: Dash duration for self-buff cast

**Status**: SKIPPED — log shows `dashDurationSeconds=0 — PC source does not provide duration, dash SKIPPED`

**PC engine behavior**:
- When casting a self-buff, character runs/dashes briefly
- Dash duration is NOT in `skills.txt` — it comes from the cast animation
- Animation duration = `MisslesForm` × cast animation table (engine-internal)
- `MisslesForm=6` (skill 127) = stationary cast (no dash)
- `MisslesForm=7` (skill 130) = special (no dash)

**What this means for skills 127/130**:
- Both skills SHOULD have no dash (MisslesForm 6 and 7)
- Current Unity code skips dash, which is CORRECT for these two skills
- Other Cái Bang skills (e.g., 117 with MisslesForm=2) may incorrectly skip dash

**Remediation (effort: 3-5 days for full RE, or 2-3 hours for skills 127/130 only)**:
1. For skills 127/130: confirm no dash needed (already correct in code)
2. For full coverage: RE `KNpc` cast animation table in binary
3. Map `MisslesForm` value to dash duration for each form

**Risk**: Low for skills 127/130 (already correct). Medium for other skills.

---

### Limitation 5: Network protocol opcodes

**Status**: NOT IMPLEMENTED — Unity uses mock protocol, not real PC binary protocol

**PC engine behavior**:
- Server binary `jx_linux_y` has network send/recv functions
- Opcodes are 1-byte or 2-byte constants in the binary
- Each skill cast, state change, damage event, etc. has a dedicated opcode
- Packet structs include player ID, skill ID, level, target, position, etc.

**What this means for skills 127/130**:
- Unity combat runtime works locally without server
- Network integration requires real PC opcode mapping
- Currently uses custom `CombatCastReport` data structure

**Remediation (effort: 1-2 weeks for full RE, or 3-5 days for skill cast only)**:
1. Install Ghidra (or use free IDA Free)
2. Load `jx_linux_y` ELF, auto-analyze
3. Find function with signature `send(sockfd, buf, len, flags)` near
   skill cast code
4. Trace back to opcode constants (e.g., 0x73, 0x74, etc.)
5. Map opcode → packet struct field
6. Implement in Unity `NetworkProtocol` layer

**Risk**: High. RE binary is error-prone; wrong opcode → silent corruption.

---

## Remediation priority

| # | Limitation | Effort | Impact | Recommended for |
|---|---|---|---|---|
| 1 | Cast time formula | 2-3h | High (all skills) | This week |
| 2 | Cooldown / WaitTime | 1h | High (all skills) | This week |
| 3 | State refresh behavior | 2-3h | Medium (all buffs) | This week |
| 4 | Dash duration (127/130) | 0h | None (already correct) | Skip |
| 5 | Network opcodes | 1-2 weeks | Critical (online play) | After binary RE infra |

## Required infrastructure for binary RE

- Ghidra or IDA Pro (free version OK for this binary)
- 1-2 days setup: import ELF, configure for 32-bit x86, set up function naming
- Reference: `nm` output has all demangled C++ symbols — start there
- Focus functions: `KSkill::Cast*`, `KNpc::Set*SkillEffect`, `KSkillList::Set*CastTime`

## How to capture runtime from PC (alternative to RE)

If running the PC client is feasible:

1. Launch PC client with `LD_PRELOAD` to log network calls
2. Cast skills 127/130, log opcodes
3. Use `gdb` to set breakpoints on `KSkill::CastInitiativeSkill`
4. Inspect `m_nDelayPerCast`, `m_nWaitTime` at runtime
5. Use frame-stepping to count animation frames for dash duration

**Output**: exact values without RE.

## References

- `docs/PC_SOURCE_AUDIT_CAIBANG_127_130.md` — current audit
- `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/jx_linux_y` — server binary
- `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/skill/gaibang.lua` — Cái Bang Lua formulas
