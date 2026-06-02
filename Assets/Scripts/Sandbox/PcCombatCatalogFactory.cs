using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC-parity seed catalog for novice attack skills and Cái Bang (Beggar Sect).
    /// Values copied from StreamingAssets/Reference/PcSkills.txt
    /// and Lua level scripts under gaibang.lua / gaibang/*.lua.
    /// </summary>
    public static class PcCombatCatalogFactory
    {
        public const int NoviceShortWeaponAttack = 53;
        public const int NoviceLongWeaponAttack = 1;
        public const int NoviceRangedAttack = 2;
        public const int NoviceThrowStone = 196;
        public const int NovicePoisonAttack = 199;
        public const int CaiBangMinSkillId = 115;
        public const int CaiBangMaxSkillId = 130;
        // MOD Vietnam adds 7 extra Cái Bang player skills beyond the original 115-130 PC range. Skill 1539 is
        // a boss/NPC variant of Thiên Hạ Vô Cẩu and is included for documentation but
        // marked isNpcVariant=true so the UI can hide it if needed.
        public const int CaiBangModMinSkillId = 274;
        public const int CaiBangModMaxSkillId = 1539;
        public const int CaiBangDogBeatingAuraChild = 209;

        public static SkillCatalog CreateNoviceAndCaiBangCatalog(IAssetRegistry assets = null)
        {
            var catalog = new SkillCatalog(assets);
            foreach (var s in CreateNoviceSkills()) catalog.Register(s);
            foreach (var s in CreateCaiBangSkills()) catalog.Register(s);
            catalog.Register(CaiBangDogBeatingAuraChildSkill());
            return catalog;
        }

        public static List<SkillDefinition> CreateNoviceSkills() => new()
        {
            PhysicalAttack(1, "长兵物理攻击", "Đòn dài", 100, child:64),
            PhysicalAttack(2, "远程物理攻击", "Tấn công tầm xa", 320, child:65),
            PhysicalAttack(53, "短兵物理攻击", "Tấn công cận chiến", 75, child:63),
            PhysicalAttack(196, "扔石头", "Ném đá", 180, child:87, charAnim:11, timePerCast:2, isMelee:false),
            PoisonAttack(199, "吐口水", "Nhổ độc", 180, child:90),
        };

        public static List<SkillDefinition> CreateCaiBangSkills() => new()
        {
            PassiveMastery(115, "丐帮棒法", "Cái Bang Bổng Pháp", 10, addPhys:(lv)=>15+10*lv, elementParam:2, icon:"\\spr\\Ui\\技能图标\\棍法.spr"),
            PassiveMastery(116, "丐帮拳法", "Cái Bang Chưởng Pháp", 10, addPhys:(lv)=>30+15*lv, elementParam:9, icon:"\\spr\\Ui\\技能图标\\暗器使用.spr"),
            DamageSkill(117, "投石问路", "Ném Đá Hỏi Đường", 10, 20, 280, 44, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(8+4*lv,0,28+11*lv), (lv)=>Same(15+10*lv), (lv)=>Triple(8,0,0)),
            ResistBuff(118, "孤木遁雷", "Cô Mộc Độn Lôi", 10, MagicAttributeKind.LightingResP),
            DamageSkill(119, "沿门托钵", "Duyên Môn Thác Bát", 20, 20, 240, 45, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(16+5*lv,0,44+13*lv), (lv)=>Same(20+10*lv), (lv)=>Triple(15,0,0)),
            ResistBuff(120, "奔流到海", "Bôn Lưu Đáo Hải", 20, MagicAttributeKind.FireResP),
            UtilitySkill(121, "妙手空空", "Diệu Thủ Không Không", 20, 180, SkillMissileForm.Surround, targetEnemy:false, targetSelf:false, levelData:(lv)=>SkillOnly(MagicAttributeKind.SkillCostV, 10,0,0)),
            DamageSkill(122, "见人伸手", "Kiến Nhân Thân Thủ", 30, 20, 300, 46, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(23+6*lv,0,65+15*lv), (lv)=>Same(40+10*lv), (lv)=>Triple(25,0,0), horseLimit:1),
            ResistBuff(123, "奎木星照", "Khuê Mộc Tinh Chiếu", 30, MagicAttributeKind.PoisonResP),
            AuraSkill(124, "打狗阵", "Đả Cẩu Trận", 30, 180, 44, 209, (lv)=>Immediate(MagicAttributeKind.AddDefenseV, 30+10*lv,25,0)),
            DamageSkill(125, "天下无狗", "Thiên Hạ Vô Cẩu", 40, 20, 400, 47, SkillMissileForm.Surround, 16, false, false, 11, (lv)=>Triple(30+8*lv,0,87+17*lv), (lv)=>Same(60+10*lv), (lv)=>Triple(50,0,0), horseLimit:1, missilesGenerateData:5),
            ResistBuff(126, "金乌映雪", "Kim Ô Ánh Tuyết", 40, MagicAttributeKind.ColdResP, costBugReturnsResultTwice:true),
            PassiveResist(127, "滑不留手", "Hoạt Bất Lưu Thủ", 40, MagicAttributeKind.PhysicsResP),
            // JXWin VM runtime data: \script\skill\gaibang.lua / kanglong_youhui.
            // L20: Spread(form=2), 15 dragons, Param1=2, Speed=32, AttackRadius=512.
            DamageSkill(128, "亢龙有悔", "Kháng Long Hữu Hối", 60, 30, 512, 48, SkillMissileForm.Fan, 15, false, false, 11, (lv)=>Triple(112+24*lv,0,112+24*lv), (lv)=>Same(150+25*lv), (lv)=>Triple(40+lv,0,0), horseLimit:1),
            ResistBuff(129, "化险为夷", "Hóa Hiểm Vi Di", 50, MagicAttributeKind.PhysicsResP, shortDuration:true, costBugUndefined:true),
            UtilitySkill(130, "醉蝶狂舞", "Túy Điệp Cuồng Vũ", 50, 400, SkillMissileForm.None, targetEnemy:false, targetSelf:true, stateSpecialId:43, levelData:(lv)=>{ var d=new SkillLevelData{level=lv}; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Floor(Log10(lv+1)/2f*60), 600+120*lv,0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,50,0,0)); return d; }),

            // ===== MOD Vietnam Cái Bang additions =====
            // 274 Giương Long Chưởng: passive combat-mastery support (Attrib 704, "hỗ trợ chiêu thức đấu-bị động").
            // MOD Skills.txt: ReqLevel 30, MaxLevel 20, missileForm=0, AttackRadius=0, child=0, charAnim=14.
            // Lua: gaibang.lua::xianglong_zhang (lifemax_p, manamax_p, addfiremagic_v/p, firedamage_v).
            PassiveMastery(274, "降龙掌", "Giương Long Chưởng", 30, addPhys:(lv)=>20+8*lv, elementParam:2, icon:"\\spr\\Ui\\技能图标\\icon_sk_gb_32.spr"),
            // 277 Hoành Bách Lộ Thiên: chưởng hỗ trợ chiêu thức (Attrib 700, "Chưởng hỗ trợ chiêu thức").
            // MOD Skills.txt: ReqLevel 40, MaxLevel 20, missileForm=2, AttackRadius=400, childSkillId=114, childSkillNum=1, charAnim=11, missilesGenerateData=57, stateSpecialId=3.
            // Lua: gaibang.lua::huabu_liushou (fastwalkrun_p, addphysicsdamagep_v, manamax_p).
            // Note: MagicAttributeKind doesn't expose fastwalkrun_p; use AddPhysicsDamageP as the
            // primary buff (closest available analog). Lua formula is in gaibang.lua::huabu_liushou
            // and can be ported later when MagicAttributeKind is extended.
            UtilitySkill(277, "横拨留手", "Hoành Bách Lộ Thiên", 40, 400, SkillMissileForm.Surround, targetEnemy:false, targetSelf:true, stateSpecialId:3, levelData:(lv)=>{ var d=new SkillLevelData{level=lv}; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, 25+8*lv, -1, 0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20+3*lv,0,0)); return d; }),
            // 357 Phi Long Tại Thiên: công kích nội công (Attrib 702). Highest-tier Giáng Long skill in MOD.
            // MOD Skills.txt: ReqLevel 80, MaxLevel 20, missileForm=0, AttackRadius=400, child=166, charAnim=11.
            // Lua: gaibang.lua::feilong_zaitian (seriesdamage_p 20→60%, firedamage_v L1=10→L20=750, misslenum 1→4 at L20, missle_speed 20→24, attackradius 448→512, cost 10→65, addskilldamage1=1073 Thần Thủ Lệnh Long, addskilldamage2=1101, addskillexp1 stacks self, param1 0→32=range 2-shot, skill_eventskilllevel 1→20, skill_showevent 0→4, skill_collideevent=389 Long Chiến Ở Dư).
            // Icon: \spr\Ui\skill\龙战在野.spr (NOT in MOD or PC gốc PAK - alias cai_bang_skill_128.png with note in PC_SOURCE.txt).
            // PC: feilong_zaitian L1-10=Fan(1), L11+=Line(1→4, param1=32 spread)
            DamageSkill(357, "飞龙在天", "Phi Long Tại Thiên", 80, 20, 400, 166, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(60+18*lv, 0, 180+50*lv), (lv)=>{ int n = lv < 11 ? 1 : (lv < 16 ? 2 : (lv < 20 ? 3 : 4)); return Same(10+45*lv/n); }, (lv)=>Triple(10+2*lv, 0, 0), horseLimit:1),
            // 359 Thiên Hạ Vô Cẩu (player): công kích ngoại công (Attrib 701). Player version - distinct from 125 (PC gốc) and 1539 (NPC variant).
            // MOD Skills.txt: ReqLevel 80, MaxLevel 20, missileForm=0, AttackRadius=400, child=168, charAnim=11.
            // Icon: \spr\Ui\skill\天下无狗.spr - SAME filename as PC gốc 125 but different UID 0x31d018f1.
            // Extracted from PC gốc PAK UID 0x31d018f1 (NOT aliased to 125 PNG; this is a distinct icon).
            // PC: tianxia_wugou player skill_misslenum_v L1=1, L20=3 target-seeking (NOT 16 circle)
            DamageSkill(359, "天下无狗", "Thiên Hạ Vô Cẩu (player)", 80, 20, 400, 168, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(70+20*lv, 0, 200+55*lv), (lv)=>Same(120+15*lv), (lv)=>Triple(80+3*lv, 0, 0), horseLimit:1),
            // 360 Tiêu Dao Công: passive combat mastery (Attrib 700, "Chưởng hỗ trợ công kích").
            // MOD Skills.txt: ReqLevel 60, MaxLevel 20, missileForm=0, AttackRadius=0, child=0, charAnim=14.
            // Lua: gaibang.lua::xiaoyao_gong (attackspeed_v, castspeed_v, allskillanti, etc).
            // Icon: \spr\Ui\skill\逍遥功.spr (NOT in MOD or PC gốc PAK - alias cai_bang_skill_130.png).
            PassiveMastery(360, "逍遥功", "Tiêu Dao Công", 60, addPhys:(lv)=>40+12*lv, elementParam:2, icon:"\\spr\\Ui\\skill\\逍遥功.spr"),
            // 1073 Thần Thủ Lệnh Long: công kích nội công (Attrib 702). 150-tier Cái Bang top skill.
            // MOD Skills.txt: ReqLevel 150, MaxLevel 20, missileForm=0, AttackRadius=400, child=335, charAnim=11.
            // Lua: gaibang.lua::gb_150_shichengjiulong_a (referenced in 357 addskilldamage1=1101; child 1103 z-Thần Thương Lệnh Long Hoàn, requires skill 1073).
            // Icon: \spr\Ui\技能图标\150\icon_sk_150_gb_01.spr - extracted from MOD updatejx08.pak UID 0x9878076a.
            // PC: MisslesForm=1 (single guided), ChildSkillId=335, 3-phase event chain
            DamageSkill(1073, "神失令龙", "Thần Thủ Lệnh Long", 150, 20, 400, 335, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(180+40*lv, 0, 500+100*lv), (lv)=>Same(300+40*lv), (lv)=>Triple(150+5*lv, 0, 0), horseLimit:1),
            // 1074 Bổng Hoành Lược Mã: công kích ngoại công (Attrib 701). 150-tier Cái Bang top skill.
            // MOD Skills.txt: ReqLevel 150, MaxLevel 20, missileForm=0, AttackRadius=400, child=336, charAnim=11.
            // Icon: \spr\Ui\技能图标\150\icon_sk_150_gb_02.spr - extracted from MOD updatejx08.pak UID 0x95215f74.
            // PC: gungaibang150 skill_misslenum_v L1=1, L20=5 target-seeking (NOT surround)
            DamageSkill(1074, "棒横掠马", "Bổng Hoành Lược Mã", 150, 20, 400, 336, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(200+45*lv, 0, 550+110*lv), (lv)=>Same(280+35*lv), (lv)=>Triple(160+5*lv, 0, 0), horseLimit:1),
            // 1539 Thiên Hạ Vô Cẩu (NPC variant): boss/NPC version of 125/359.
            // MOD Skills.txt: ReqLevel 1, MaxLevel 60, missileForm=0, AttackRadius=400, child=?, charAnim=11.
            // Marked isNpcVariant=true - hidden from player skill panel but available for boss AI.
            DamageSkill(1539, "天下无狗NPC", "Thiên Hạ Vô Cẩu (NPC)", 1, 60, 400, 47, SkillMissileForm.Surround, 16, false, false, 11, (lv)=>Triple(50+15*lv, 0, 150+40*lv), (lv)=>Same(100+10*lv), (lv)=>Triple(80, 0, 0), horseLimit:1, missilesGenerateData:5),
        };

        public static SkillDefinition CaiBangDogBeatingAuraChildSkill()
        {
            var s = BaseSkill(209, "打狗阵子弹", "Đả Cẩu Trận Tử Đạn", 50, 20, 180, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 92; s.childSkillNum = 1; s.baseSkill = true; s.byMissile = true;
            s.targetAlly = true; s.targetSelf = true; s.stateSpecialId = 44; s.charAnimId = 14;
            AddLevels(s, lv => Immediate(MagicAttributeKind.AddDefenseV, 30 + 10 * lv, 25, 0));
            return s;
        }

        private static SkillDefinition PhysicalAttack(int id, string raw, string vi, int radius, int child, int charAnim = 9, int timePerCast = 0, bool isMelee = true)
        {
            var s = BaseSkill(id, raw, vi, 0, 20, radius, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillLevel = id is 1 or 53 ? 1 : 0; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = charAnim; s.isMelee = isMelee; s.waitTime = 5; s.timePerCast = timePerCast; s.isPhysical = id != 196; s.targetOnly = id is 1 or 2 or 53; s.targetEnemy = true; s.isUseAttackRating = true; s.doHurt = id != 196; s.weaponSkill = id is 1 or 2 or 53;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, 0,0,0)); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP,0,0,0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,0,0,0)); return d; });
            return s;
        }

        private static SkillDefinition PoisonAttack(int id, string raw, string vi, int radius, int child)
        {
            var s = BaseSkill(id, raw, vi, 10, 20, radius, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.waitTime = 5; s.timePerCast = 2; s.targetEnemy = true;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, 1+lv,200,10)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,0,0,0)); return d; });
            return s;
        }

        private static SkillDefinition PassiveMastery(int id, string raw, string vi, int req, Func<int,int> addPhys, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addPhys(lv), -1, elementParam)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, 12+3*lv, -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, 5+lv, -1, 0)); return d; }); return s;
        }

        private static SkillDefinition PassiveResist(int id, string raw, string vi, int req, MagicAttributeKind kind)
        { var s = BaseSkill(id, raw, vi, req, 20, 400, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 11; AddLevels(s, lv => State(kind, Floor(Log10(lv+5)/2f*50), -1, 0)); return s; }

        private static SkillDefinition ResistBuff(int id, string raw, string vi, int req, MagicAttributeKind kind, bool shortDuration=false, bool costBugUndefined=false, bool costBugReturnsResultTwice=false)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 400, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = id switch {118=>49,120=>50,123=>51,126=>52,129=>53,_=>0}; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.waitTime = 5; s.timePerCast = 2; s.targetAlly = true; s.targetSelf = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; int pct = shortDuration ? Floor(Log10(lv+1)/2f*60) : Floor(Log10(lv+1)/2f*80); int dur = shortDuration ? 600+600*lv : 1200+1200*lv; d.state.Add(new SkillMagicAttribute(kind,pct,dur,0)); if (costBugUndefined) d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,0,0,0)); else if (costBugReturnsResultTwice) d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,20,20,0)); else d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,20,0,0)); return d; }); return s;
        }

        private static SkillDefinition DamageSkill(int id, string raw, string vi, int req, int max, int radius, int child, SkillMissileForm form, int childNum, bool isPhysical, bool targetOnly, int charAnim, Func<int,(int,int,int)> phys, Func<int,(int,int,int)> fire, Func<int,(int,int,int)> cost, Func<int,SkillMagicAttribute> extra=null, int horseLimit=0, int missilesGenerateData=0)
        {
            var s = BaseSkill(id, raw, vi, req, max, radius, form); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.isPhysical = isPhysical; s.targetOnly = targetOnly; s.targetEnemy = true; s.horseLimit = horseLimit; s.missilesGenerateData = missilesGenerateData;
            s.effectSourceId = id >= 118 ? Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr") : null;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; var p=phys(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV,p.Item1,p.Item2,p.Item3)); var f=fire(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV,f.Item1,f.Item2,f.Item3)); if (extra!=null) d.state.Add(extra(lv)); var c=cost(lv); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,c.Item1,c.Item2,c.Item3)); return d; }); return s;
        }

        private static SkillDefinition AuraSkill(int id, string raw, string vi, int req, int radius, int stateId, int child, Func<int,SkillLevelData> levelData)
        { var s = BaseSkill(id, raw, vi, req, 20, radius, SkillMissileForm.None); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.isAura = true; s.stateSpecialId = stateId; s.childSkillId = child; s.childSkillLevel = 1; s.childSkillNum = 1; s.targetSelf = true; s.charAnimId = 14; AddLevels(s, levelData); return s; }

        private static SkillDefinition UtilitySkill(int id, string raw, string vi, int req, int radius, SkillMissileForm form, bool targetEnemy, bool targetSelf, int stateSpecialId=0, Func<int,SkillLevelData> levelData=null)
        { var s = BaseSkill(id, raw, vi, req, 20, radius, form); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.targetEnemy = targetEnemy; s.targetSelf = targetSelf; s.stateSpecialId = stateSpecialId; s.charAnimId = 11; AddLevels(s, levelData ?? (lv => new SkillLevelData{level=lv})); return s; }

        private static SkillDefinition BaseSkill(int id, string raw, string vi, int req, int max, int radius, SkillMissileForm form) => new SkillDefinition { skillId=id, nameRaw=raw, nameNormalized=vi, reqLevel=req, maxLevel=max, attackRadius=radius, missileForm=form, faction = IsCaiBangSkill(id) ? CombatFaction.CaiBang : CombatFaction.None, iconSourceId = Sprite(IconPathForSkill(id)), equipLimit=-2 };

        // Cái Bang skill set: PC gốc 115-130 + MOD 274, 277, 357, 359, 360, 1073, 1074, 1539 (NPC variant).
        // 1539 is an NPC/boss version of Thiên Hạ Vô Cẩu and stays in the catalog for boss AI;
        // the player skill panel filters it out via isNpcVariant.
        public static bool IsCaiBangSkill(int id) => id==209 || (id>=115 && id<=130) || id==274 || id==277 || id==357 || id==359 || id==360 || id==1073 || id==1074 || id==1539;
        private static string IconPathForSkill(int id) => id switch
        {
            1 => "\\spr\\Ui\\技能图标\\icon_sk_ty_ap.spr",
            2 => "\\spr\\Ui\\技能图标\\icon_sk_ty_at.spr",
            53 => "\\spr\\Ui\\技能图标\\icon_sk_ty_as.spr",
            115 => "\\spr\\Ui\\技能图标\\棍法.spr",
            116 => "\\spr\\Ui\\技能图标\\暗器使用.spr",
            117 or 196 or 197 or 198 or 199 or 200 or 201 => "\\spr\\Ui\\技能图标\\icon_sk_gb_01.spr",
            118 => "\\spr\\Ui\\技能图标\\icon_sk_gb_02.spr",
            119 => "\\spr\\Ui\\技能图标\\icon_sk_gb_11.spr",
            120 => "\\spr\\Ui\\技能图标\\icon_sk_gb_12.spr",
            121 => "\\spr\\Ui\\技能图标\\icon_sk_gb_13.spr",
            122 => "\\spr\\Ui\\技能图标\\icon_sk_gb_21.spr",
            123 => "\\spr\\Ui\\技能图标\\icon_sk_gb_22.spr",
            124 or 209 => "\\spr\\Ui\\技能图标\\icon_sk_gb_23.spr",
            125 or 359 or 1539 => "\\spr\\Ui\\技能图标\\icon_sk_gb_31.spr", // 359/1539 share the same path family (天下无狗) but use distinct extracted UIDs; see PC_SOURCE.txt.
            126 or 274 => "\\spr\\Ui\\技能图标\\icon_sk_gb_32.spr", // 274 Giương Long Chưởng shares the GB_32 icon visually.
            127 or 277 => "\\spr\\Ui\\技能图标\\icon_sk_gb_33.spr", // 277 Hoành Bách Lộ Thiên shares the GB_33 icon visually.
            128 or 357 => "\\spr\\Ui\\技能图标\\icon_sk_gb_41.spr", // 357 Phi Long Tại Thiên: same Long-family icon, real icon not in any PAK.
            129 => "\\spr\\Ui\\技能图标\\icon_sk_gb_42.spr",
            130 or 360 => "\\spr\\Ui\\技能图标\\icon_sk_gb_43.spr", // 360 Tiêu Dao Công: alias to Túy Điệp Cuồng Vũ; real icon not in any PAK.
            1073 => "\\spr\\Ui\\技能图标\\150\\icon_sk_150_gb_01.spr", // MOD 150-tier GB icon, extracted from updatejx08.pak.
            1074 => "\\spr\\Ui\\技能图标\\150\\icon_sk_150_gb_02.spr", // MOD 150-tier GB icon, extracted from updatejx08.pak.
            _ => "\\spr\\Ui\\技能图标\\icon_sk_gb_01.spr",
        };
        private static void AddLevels(SkillDefinition s, Func<int,SkillLevelData> f) { int max=Mathf.Max(1, s.maxLevel == 0 ? 1 : s.maxLevel); for (int lv=1; lv<=max; lv++) { var data = f(lv) ?? new SkillLevelData(); data.level = lv; s.pcLevelData.Add(data); } var first=s.GetPcLevelData(1); if (first?.First(MagicAttributeKind.PhysicsDamageV) is SkillMagicAttribute a) s.damageLevels.Add(new SkillDamageLevel{level=1,baseDamage=a.value3,attackRatio=1f,isPhysical=s.isPhysical}); }
        private static SourceAssetId Sprite(string path) => new SourceAssetId { sourcePath = path, resourceKind = ResourceKind.Sprite, uid = path.GetHashCode() };
        private static int Floor(float v) => Mathf.FloorToInt(v);
        private static float Log10(float v) => Mathf.Log10(v);
        private static (int,int,int) Triple(int a,int b,int c)=>(a,b,c);
        private static (int,int,int) Same(int a)=>(a,0,a);
        private static SkillMagicAttribute Damage(MagicAttributeKind k,int a,int b,int c)=>new(k,a,b,c);
        private static SkillLevelData State(MagicAttributeKind k,int a,int b,int c){var d=new SkillLevelData(); d.state.Add(new SkillMagicAttribute(k,a,b,c)); return d;}
        private static SkillLevelData Immediate(MagicAttributeKind k,int a,int b,int c){var d=new SkillLevelData(); d.immediate.Add(new SkillMagicAttribute(k,a,b,c)); return d;}
        private static SkillLevelData SkillOnly(MagicAttributeKind k,int a,int b,int c){var d=new SkillLevelData(); d.skill.Add(new SkillMagicAttribute(k,a,b,c)); return d;}
    }
}
