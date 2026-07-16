# Waves Và Enemies

| Trường | Giá trị |
|---|---|
| Mục đích | Ghép wave orchestration DHCD với NPC/AI/stat/visual JX |
| Trạng thái | `provisional` |
| Owner / reviewer | Gameplay owner / JX enemy reviewer |
| Cập nhật | 2026-07-15 |

## Ownership

| Concern | Owner |
|---|---|
| Wave schedule, spawn interval, pool, clear | Wave service/config |
| NPC identity, base stat, skill, AI task, collider flags | JX NPC template sau audit |
| Animation/render/shadow/nameplate | Unity JX visual layer |
| Damage resolution | Shared deterministic combat rules; Go canonical |
| Drop/XP | DHCD-style reward orchestrator + versioned data |

`CityDefenceService` và `PcCityDefenceParser` chỉ là reusable seam; defender IDs/counts từ `newcitydefence/*.txt` không chứng minh JX Region_S roster/AI.

## Wave schema

```yaml
Wave:
  id: stable-wave-id
  schedule: absolute-or-tick-events
  spawns:
    - npc_template_id: audited-jx-id
      count: data
      interval_ticks: data
      spawn_anchor: audited-map-anchor
  clear_condition: all_dead|timer|objective
  reward_table_version: version
```

## NPC gate

Mỗi template cần NpcS/Npcs row, map Region_S placement nếu có, exact SPR actions/directions/frames, stats/skills/AI và visual golden. Mapping trong `Assets/Scripts/Sandbox/MapEnemyDatabase.cs` là provisional tới khi đủ rows.

## Acceptance

- Spawn sequence deterministic và không vượt pool/limit.
- NPC không render dưới map hoặc dùng placeholder khi candidate thiếu.
- Collider, target, death, drop event có replay sequence.
- Map unload dọn timer/pool; malformed reverse method không được tự bịa cleanup.
