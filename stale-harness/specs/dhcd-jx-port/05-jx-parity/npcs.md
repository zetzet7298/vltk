# JX NPC/Enemy Parity

| Trường | Giá trị |
|---|---|
| Mục đích | Dùng NPC template, stat, AI và visual JX thay cho enemy giả |
| Trạng thái | `provisional` |
| Owner / reviewer | JX enemy owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Template contract

Mỗi template gồm JX NPC ID, `NpcS.txt`/`npcs.txt` row, name, level/stat, skill IDs, AI task, movement/collision flags, drop link, SPR action/direction/frame, shadow/nameplate và map placement evidence.

## Current Unity caveat

`Assets/Scripts/Sandbox/MapEnemyDatabase.cs` có mappings/coordinates dùng để tái sử dụng nhưng broad/default values chưa được chứng minh bằng từng Region_S/NpcS row. Mark `provisional` cho tới khi [npc-template-roster](npc-template-roster.md) pass.

`PcCityDefenceParser.cs` và `CityDefenceService.cs` có thể làm wave scheduling seam; chúng không chứng minh JX AI/stat hay roster.

## Acceptance

- Spawn template -> visual -> AI -> damage/drop golden traceable.
- Không fallback NPC placeholder trong pilot.
- Missing/ambiguous template chặn wave content và ghi blocker.
