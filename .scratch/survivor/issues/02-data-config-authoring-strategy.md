# 02 — Data config authoring strategy (vì dhcd configs blocked)

Type: `grilling`
Status: `resolved`
Blocked by: 01

## Question

DHCD configs (RandomSkillConfig, LevelBase, CollectItem, PlayerExp, Booty) đều encrypted/blocked.
Author config thế nào (format, nguồn dữ liệu, tooling, fail-closed) để ship được mà không vi phạm
"không port / không bịa"?

## Answer

**Tự author config Unity-native; dhcd declaration chỉ làm SCHEMA reference (field name +
relationship), KHÔNG làm value source.**

- Static design data = **ScriptableObject** (`SkillDef`, `MonsterDef`, `WaveDef`, `DropTable`,
  `LevelCurve`, `ImpactDef`, `FactionPool`). Tạo dưới `Assets/Survivor/Data/`.
- Skill library nguồn = **JX `PcSkills.txt` (GBK) + `PcAllFactionLearnedDisplaySkills.txt`
  (TCVN3)**, parse bằng Sandbox parser có sẵn (read-only, KHÔNG sửa Sandbox); output → our
  `SkillDef` ScriptableObject. Faction = `LvlSetScript` (không phải cột CharClass).
- SPR visual = fail-closed: resolve logical path → UID qua `vltktool resolve_uid.py`, extract
  winner frame qua `extract_item_spr.py`, chỉ gán khi staged hash có sẵn
  (`Assets/StreamingAssets/Sprites/{hash}.spr`); chưa staged → KHÔNG gán (per AGENTS.md).
- Mỗi config + parser để lại 1 EditMode self-check (parity SURVIVOR_PLAN quy tắc port).
- dhcd declarations chỉ copy **schema ý tưởng** (ví dụ `CollectItemPoolConfig` có
  PoolID/ItemID/OutputType/Param1/Param2/BronID) — tự điền giá trị.

Tách rõ: schema-parity (cite declaration path) vs own-values (ghi rationale).
