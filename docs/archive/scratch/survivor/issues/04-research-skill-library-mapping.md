# 04 — Research: full skill library mapping JX → survivor pool

Type: `research`
Status: `resolved`
Blocked by: 02

## Question

Map FULL `PcSkills.txt` (JX) → survivor skill pool (RandomSkillConfig-equivalent). Cần:

1. Inventory + phân loại: tổng số skill, breakdown theo faction (`LvlSetScript`: tianwang,
   kunlun, ...), dạng cast (melee `MisslesForm=12`/`IsMelee=1`, missile child, fan-spread
   `SKILL_MF_Spread`, supply heal/bomb/magnet...).
2. Portability từng skill: visual SPR staged-hash available? Build fail-closed list (skill nào
   chưa staged → chưa gán sprite, parity AGENTS.md).
3. RandomSkillConfig schema mapping: từ cột `PcSkills.txt`/`missles.txt` → trường own
   `SkillDef` (id, faction, weight, level-scaling, cast form, Param1/Param2 fan-spread, PreCastSpr
   hash, missile hash). Lưu ý GBK bytes path → `SprRuntimeService.ComputePathUidHex`.
4. Supply-skill subset (heal/bomb/magnet/full-clear): nguồn + classification.

## Output

Ghi `C:/Projects/vltk-mobile/.scratch/survivor/research/skill-library.md`: bảng skill (id|faction|
form|visual-staged?|own-SkillDef-field-map), fail-closed list, schema doc, supply subset. Đọc:
`PcSkills.txt` (GBK), `PcAllFactionLearnedDisplaySkills.txt` (TCVN3), `missles.txt`, Sandbox
`PcSkill*Parser`, `bin/client/package.ini` (winner priority), `C:/Projects/vltktool`
(`resolve_uid.py`, `extract_item_spr.py`), `C:/Projects/jx-pc` read-only.

Fail-closed: KHÔNG bịa path/sprite. Chỉ kết luận portability dựa staged-hash thực.

## Answer

**1.216 skill** PcSkills.txt (TCVN3, 113 col) + **441 missile** missles.txt (57 col).
- **10 phai ~452 skill**: tangmen 54, cuiyan 53, emei 51, tianwang 50, kunlun 50, shaolin 45, wudu 40, wudang 39, tianren 37, gaibang 33. Remainder = special(417)/npc(124)/partner(78)/battles(47) = monster va boss pool.
- **Cast form**: MisslesForm 7(652 dominant), 12-melee(22); IsMelee=1 (104); ByMissle=1 (224); child<>0 (675); PreCastSpr (357/1216).
- **Schema mapping chot** (PcSkills col -> SkillDef): 2->Id, 70->Faction, 19->Form, 26->IsMelee, 20->ChildMissileId, 6->PreCastSprUid(GBK->ComputePathUidHex), 58/60->Fan Param1/2, 52/53->Req/MaxLevel, 71-110->LevelScaling(own numeric).
- **BUG parser**: PcSkillFullParser.LvlSetScriptCol=71 SAI - thuc col 70 (71=LvlSetting1). Phai sua khi to-spec.
- **missles.txt**: AnimFile1-4 (col 29-50) = missile SPR visual; ResponseSkill(18)=on-hit.
- **Fail-closed rule** (KHONG enumerate 1216): runtime SprRuntimeService.FindSprDataInRoot(uid) -> null => proxy mau; melee=child+anim; child khong AnimFile (20/408/274/1083-88) => KHONG gan.
- **Supply**: heal=lifereplenish_v/lifemax_v; bomb=special/bomb.lua+dmg; magnet=own (collect mgr); buff=IsAura(41).
- Card pool composition law gio specifiable -> graduate fog.
Full: research/skill-library.md

