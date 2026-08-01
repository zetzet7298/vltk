# Lua Level-Data Audit — 8 phái còn lại (2026-07-18)

## Phạm vi
Audit nguồn level data PC (`bin/Server/script/skill/*.lua`) vs runtime mobile
(`SkillLevelCurveService` / `PcSkillTuningRegistry`) cho 8 phái chưa port:
Shaolin, TianWang, EMei, CuiYan, WuDu, TianRen, WuDang, KunLun.
(CaiBang + TangMen đã port: `PcCaiBangLuaLevelService`, `PcTangMenLuaLevelService`.)

## PC nguồn (jx-source, read-only)
`01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/skill/`
- shaolin.lua 645 dòng, tianwang.lua 778, emei.lua 640, cuiyan.lua 640,
  wudu.lua 616, tianren.lua 619, wudang.lua 665, kunlun.lua 621 (tổng 5,224)
- lvlup_*.lua riêng cho ultimate cấp 20→30: lvlup_duanjin_fugu,
  lvlup_luanhuan_ji, lvlup_pililuanhuan_ji, lvlup_pudu_zhongsheng,
  lvlup_shehun_luanxin, lvlup_zuixian_cuogu

## Số skill keys / attribute coverage (parser đếm SKILLS{} block)
| phái | skill keys | attrs chính (count) |
|---|---|---|
| shaolin | 161 | physicsenhance_p 11, addphysicsdamage_p 5 |
| tianwang | 231 | physicsenhance_p 15, addphysicsdamage_p 3 |
| emei | 174 | seriesdamage_p 6, physicsenhance_p 6, skill_appendskill 5 |
| cuiyan | 159 | seriesdamage_p 9, physicsenhance_p 4 |
| wudu | 159 | seriesdamage_p 11, poisondamage_v 3 |
| tianren | 164 | seriesdamage_p 9, adddefense_v 3 |
| wudang | 173 | seriesdamage_p 10, lightingdamage_v 2 |
| kunlun | 183 | seriesdamage_p 10, physicsenhance_p 3 |

Lưu ý: keys bao gồm passive/passive-enhance (cuiyan_daofa/shuangdao,
shaolin_gunfa/daofa/quanfa = võ công bị động), không phải toàn bộ là
active skill player. Attr đầy đủ cho skill active: skill_attackradius,
skill_cost_v, missle_speed_v, seriesdamage_p/colddamage_v/..., addskilldamageN.

## Ví dụ xác minh id→key (cột LvlData1, file learned display)
- 336 Băng Tung Vô ảnh (CuiYan ultimate) → `bingzong_wuying` trong cuiyan.lua:
  attackradius {{1,448},{20,512},{21,512}}, cost {{1,40},{20,60}},
  colddamage_v [1]{{1,10},{15,140},{20,173}}, [3]{{1,50},{15,200},{20,276}},
  missle_speed_v {{1,20},{20,24}}, skill_eventskilllevel, skill_collideevent
- 271 Long Trảo Hổ Trảo → `longzhao_huzhua` (2 rows trong file = 271 + biến thể)
- LvlData1 map đầy đủ trong cột 73 file
  `Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt` (242 rows)

## Mobile hiện trạng (gap)
`PcSkillTuningRegistry.RadiusCurves`: chỉ attackRadius FLAT (1→20, 2 điểm),
~5 skill/phái, nguồn PcSkills AttackRadius — KHÔNG có damage/cost/speed curves.

`SkillLevelCurveService.GetStats` fallback khi không có tuning:
- skillCost = EstimateCost: heuristic theo tier (Active 10+2(lv-1), Ultimate 50+...)
- baseDamage = EstimateDamage: heuristic theo tier
- attackRadius = EstimateRadius: heuristic theo tier

→ 8 phái dùng heuristic, KHÔNG khớp PC Lua (ví dụ 336: PC cost 40→60/20lv,
mobile Estimate Ultimate = 50+(lv-1)*2 = 50→88 — lệch cả chiều và biên độ).
Đây là dimension balance per-level, không ảnh hưởng visual/cast/đủ-skill.

## Quyết định
- KHÔNG port 8 Lua service trong task này (5,224 dòng parse + verify id→key
  từng skill + lvlup_* riêng = cần task riêng; CaiBang 790 dòng service là
  tiền lệ cho mức công sức). Ghi gap TODO.
- Không có bug "damage=0" từ audit: stub skills vẫn nhận Estimate >0 qua
  SkillLevelCurveService; không skill nào bị cost=0 sai (passive trả 0 đúng).

## TODO sau
- [ ] Port generic `PcFactionLuaLevelService`: parse engine dùng lại
      PcCaiBangLuaLevelService (đã generic cho SKILLS table), map id→key tự
      sinh từ cột LvlData1 file learned display (đã xác minh 336/271),
      per-faction: shaolin/tianwang/emei/cuiyan/wudu/tianren/wudang/kunlun
      + lvlup_*.lua cho ultimate 21-30.
- [ ] Verify từng phái: cost/damage/radius/speed tại lv 1/10/20/30 đối chiếu
      Lua source trực tiếp (giống CaiBang parity test).
