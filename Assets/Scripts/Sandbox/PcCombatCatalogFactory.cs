using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC-parity seed catalog for novice attack skills and Cái Bang (Beggar Sect).
    /// Values copied from jxwin-kinnox/SourceNew/swrod3/bin/Server/Settings/Skills.txt
    /// and Lua level scripts under Utility/Run/Script/skill/{special,gaibang}.
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
            PassiveMastery(115, "丐帮棒法", "Cái Bang Bổng Pháp", 10, elementParam:2, icon:"\\spr\\Ui\\技能图标\\棍法.spr"),
            PassiveMastery(116, "丐帮拳法", "Cái Bang Chưởng Pháp", 10, elementParam:9, icon:"\\spr\\Ui\\技能图标\\暗器使用.spr"),
            DamageSkill(117, "投石问路", "Ném Đá Hỏi Đường", 10, 20, 280, 44, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(2+Floor(lv*0.2f),0,8+Floor(lv*0.8f)), (lv)=>Same(5+Floor(lv*0.5f)), (lv)=>Triple(3+Floor(lv/5f),0,0)),
            ResistBuff(118, "孤木遁雷", "Cô Mộc Độn Lôi", 10, MagicAttributeKind.LightingResP),
            DamageSkill(119, "沿门托钵", "Duyên Môn Thác Bát", 20, 20, 240, 45, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(1+Floor(lv*0.2f),0,9+Floor(lv*1.1f)), (lv)=>Same(8+Floor(lv*0.8f)), (lv)=>Triple(5+Floor(lv/5f),0,0)),
            ResistBuff(120, "奔流到海", "Bôn Lưu Đáo Hải", 20, MagicAttributeKind.FireResP),
            UtilitySkill(121, "妙手空空", "Diệu Thủ Không Không", 20, 180, SkillMissileForm.Surround, targetEnemy:false, targetSelf:false, levelData:(lv)=>SkillOnly(MagicAttributeKind.SkillCostV, 10,0,0)),
            DamageSkill(122, "见人伸手", "Kiến Nhân Thân Thủ", 30, 20, 300, 46, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(3+Floor(lv*0.5f),0,14+Floor(lv*2.1f)), (lv)=>Same(10+lv), (lv)=>Triple(10+Floor(lv/2f),0,0), horseLimit:1),
            ResistBuff(123, "奎木星照", "Khuê Mộc Tinh Chiếu", 30, MagicAttributeKind.PoisonResP),
            AuraSkill(124, "打狗阵", "Đả Cẩu Trận", 30, 180, 44, 209, (lv)=>Immediate(MagicAttributeKind.AddDefenseV, 30+10*lv,25,0)),
            DamageSkill(125, "天下无狗", "Thiên Hạ Vô Cẩu", 40, 20, 400, 47, SkillMissileForm.Surround, 16, false, false, 11, (lv)=>Triple(10+Floor(lv*1.2f),0,27+lv*4), (lv)=>Same(10+Floor(lv*0.5f)), (lv)=>Triple(25+lv,0,0), extra:(lv)=>Damage(MagicAttributeKind.ConfuseP,0,0,0), horseLimit:1, missilesGenerateData:5),
            ResistBuff(126, "金乌映雪", "Kim Ô Ánh Tuyết", 40, MagicAttributeKind.ColdResP, costBugReturnsResultTwice:true),
            PassiveResist(127, "滑不留手", "Hoạt Bất Lưu Thủ", 40, MagicAttributeKind.PhysicsResP),
            DamageSkill(128, "亢龙有悔", "Kháng Long Hữu Hối", 60, 30, 360, 48, SkillMissileForm.Single, 1, false, false, 11, (lv)=>Triple(10+Floor(lv*2.2f),0,35+lv*6), (lv)=>Same(20+2*lv), (lv)=>Triple(40+2*lv,0,0), horseLimit:1),
            ResistBuff(129, "化险为夷", "Hóa Hiểm Vi Di", 50, MagicAttributeKind.PhysicsResP, shortDuration:true, costBugUndefined:true),
            UtilitySkill(130, "醉蝶狂舞", "Túy Điệp Cuồng Vũ", 50, 400, SkillMissileForm.None, targetEnemy:false, targetSelf:true, stateSpecialId:43, levelData:(lv)=>{ var d=new SkillLevelData{level=lv}; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Floor(Log10(lv+1)/2f*60), 600+120*lv,0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,50,0,0)); return d; }),
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

        private static SkillDefinition PassiveMastery(int id, string raw, string vi, int req, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, 8+2*lv, -1, elementParam)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, 12+3*lv, -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, 5+lv, -1, 0)); return d; }); return s;
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

        private static SkillDefinition BaseSkill(int id, string raw, string vi, int req, int max, int radius, SkillMissileForm form) => new SkillDefinition { skillId=id, nameRaw=raw, nameNormalized=vi, reqLevel=req, maxLevel=max, attackRadius=radius, missileForm=form, faction = (id>=115 && id<=130) || id==209 ? CombatFaction.CaiBang : CombatFaction.None, iconSourceId = Sprite(IconPathForSkill(id)), equipLimit=-2 };
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
            125 => "\\spr\\Ui\\技能图标\\icon_sk_gb_31.spr",
            126 => "\\spr\\Ui\\技能图标\\icon_sk_gb_32.spr",
            127 => "\\spr\\Ui\\技能图标\\icon_sk_gb_33.spr",
            128 => "\\spr\\Ui\\技能图标\\icon_sk_gb_41.spr",
            129 => "\\spr\\Ui\\技能图标\\icon_sk_gb_42.spr",
            130 => "\\spr\\Ui\\技能图标\\icon_sk_gb_43.spr",
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
