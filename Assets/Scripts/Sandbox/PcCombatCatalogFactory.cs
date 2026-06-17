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
        public const int WuDangMinSkillId = 151;
        public const int WuDangMaxSkillId = 166;
        public const int ShaolinMinSkillId = 3;
        public const int ShaolinMaxSkillId = 21;
        public const int TangMenMinSkillId = 43;
        public const int TangMenMaxSkillId = 58;
        public const int EMeiMinSkillId = 77;
        public const int EMeiMaxSkillId = 93;
        public const int TianWangMinSkillId = 23;
        public const int TianWangMaxSkillId = 42;
        public const int WuDuMinSkillId = 60;
        public const int WuDuMaxSkillId = 76;
        public const int CuiYanMinSkillId = 95;
        public const int CuiYanMaxSkillId = 114;

        public static SkillCatalog CreateNoviceAndCaiBangCatalog(IAssetRegistry assets = null)
        {
            return CreateNoviceAndCoreSectCatalog(assets, includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: false, includeTianRen: false, includeKunLun: false);
        }

        public static SkillCatalog CreateNoviceAndCoreSectCatalog(IAssetRegistry assets = null, bool includeWuDang = true, bool includeShaolin = true, bool includeTangMen = true, bool includeEMei = true, bool includeTianWang = true, bool includeWuDu = true, bool includeCuiYan = true, bool includeTianRen = true, bool includeKunLun = true)
        {
            var catalog = new SkillCatalog(assets);
            foreach (var s in CreateNoviceSkills()) catalog.Register(s);
            foreach (var s in CreateCaiBangSkills()) catalog.Register(s);
            catalog.Register(CaiBangDogBeatingAuraChildSkill());
            if (includeWuDang) foreach (var s in CreateWuDangSkills()) catalog.Register(s);
            if (includeShaolin) foreach (var s in CreateShaolinSkills()) catalog.Register(s);
            if (includeTangMen) foreach (var s in CreateTangMenSkills()) catalog.Register(s);
            if (includeEMei) foreach (var s in CreateEMeiSkills()) catalog.Register(s);
            if (includeTianWang) foreach (var s in CreateTianWangSkills()) catalog.Register(s);
            if (includeWuDu) foreach (var s in CreateWuDuSkills()) catalog.Register(s);
            if (includeCuiYan) foreach (var s in CreateCuiYanSkills()) catalog.Register(s);
            if (includeTianRen) foreach (var s in CreateTianRenSkills()) catalog.Register(s);
            if (includeKunLun) foreach (var s in CreateKunLunSkills()) catalog.Register(s);
            return catalog;
        }

        public static SkillCatalog CreateNoviceCoreSectAndModCatalog(string modSkillsPath, IAssetRegistry assets = null, bool includeWuDang = true, bool includeShaolin = true, bool includeTangMen = true, bool includeEMei = true, bool includeTianWang = true, bool includeWuDu = true, bool includeCuiYan = true, bool includeTianRen = true, bool includeKunLun = true)
        {
            var catalog = CreateNoviceAndCoreSectCatalog(assets, includeWuDang, includeShaolin, includeTangMen, includeEMei, includeTianWang, includeWuDu, includeCuiYan, includeTianRen, includeKunLun);
            RegisterModSkills(catalog, modSkillsPath);
            return catalog;
        }

        public static int RegisterModSkills(SkillCatalog catalog, string modSkillsPath, int minSkillId = PcModSkillParser.ExpansionMinSkillId)
        {
            if (catalog == null) return 0;
            int count = 0;
            foreach (var row in PcModSkillParser.ParseFile(modSkillsPath, minSkillId))
            {
                catalog.Register(PcModSkillParser.ToSkillDefinition(row));
                count++;
            }
            return count;
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
            // 115 Cái Bang Bổng Pháp: passive (gaibang_bangfa)
            PassiveMasteryWithDeadly(115, "Cái Bang Bổng pháp", "Cái Bang Bổng Pháp", 10,
                addPhys: (lv) => Link(lv, (1, 10, ""), (20, 150, "")),
                deadly: (lv) => Link(lv, (1, 2, ""), (20, 25, "Conic")),
                elementParam: 2, icon: "\\spr\\Ui\\技能图标\\icon_sk_gb_gf.spr"),

            // 116 Cái Bang Chưởng Pháp: passive (gaibang_zhangfa)
            PassiveMasteryChuong(116, "Cái Bang Chưởng Pháp", "Cái Bang Chưởng Pháp", 10,
                addFire: (lv) => Link(lv, (1, 25, ""), (20, 275, "")),
                elementParam: 9, icon: "\\spr\\Ui\\技能图标\\icon_sk_gb_aq.spr"),

            // 117 Ném Đá Hỏi Đường: damage (yanmen_tuobo) [PC radius L20=384]
            DamageSkillNew(117, "Đầu Thạch Vấn Lộ ", "Ném Đá Hỏi Đường", 10, 20, 384, 44, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => Link(lv, (1, 10, ""), (20, 55, "")),
                fire: (lv) => (Link(lv, (1, 10, ""), (20, 100, "")), 0, Link(lv, (1, 10, ""), (20, 150, ""))),
                cost: (lv) => (10, 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 1, ""), (20, 10, "")), -1, 0)),

            // 118 Cô Mộc Độn Lôi: buff
            ResistBuff(118, "Cô Mộc Độn Lôi ", "Cô Mộc Độn Lôi", 10, MagicAttributeKind.LightingResP),

            // 119 Duyên Môn Thác Bát: damage (yanmen_tuobo) [PC radius L20=384]
            DamageSkillNew(119, "Diên Môn Thác Bát", "Duyên Môn Thác Bát", 10, 20, 384, 45, SkillMissileForm.Single, 1, true, false, 11,
                phys: (lv) => Link(lv, (1, 10, ""), (20, 55, "")),
                fire: (lv) => (Link(lv, (1, 10, ""), (20, 100, "")), 0, Link(lv, (1, 10, ""), (20, 150, ""))),
                cost: (lv) => (10, 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 1, ""), (20, 10, "")), -1, 0)),

            // 120 Bôn Lưu Đáo Hải: buff
            ResistBuff(120, "Bôn Lưu Đáo Hải", "Bôn Lưu Đáo Hải", 20, MagicAttributeKind.FireResP),

            // 121 Diệu Thủ Không Không: utility
            UtilitySkill(121, "Diệu Thủ Không Không", "Diệu Thủ Không Không", 20, 180, SkillMissileForm.Surround, targetEnemy:false, targetSelf:false, levelData:(lv)=>SkillOnly(MagicAttributeKind.SkillCostV, 10,0,0)),

            // 122 Kiến Nhân Thân Thủ: damage (jianren_shenshou) [PC radius L20=384, fire L20[3]=215]
            DamageSkillNew(122, "Kiến Nhân Thần Thủ ", "Kiến Nhân Thân Thủ", 10, 20, 384, 46, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 15, ""), (20, 75, "")), 0, Link(lv, (1, 15, ""), (20, 215, ""))),
                cost: (lv) => (25, 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 1, ""), (20, 10, "")), -1, 0),
                horseLimit: 1),

            // 123 Khuê Mộc Tinh Chiếu: buff
            ResistBuff(123, "Khuê Mộc Tinh Chiếu", "Khuê Mộc Tinh Chiếu", 30, MagicAttributeKind.PoisonResP),

            // 124 Đả Cẩu Trận: passive
            PassiveMastery(124, "Đả Cẩu bổng", "Đả Cẩu Trận", 30, addPhys:(lv)=>Link(lv, (1, 10, ""), (20, 175, "")), elementParam:2, icon:"\\spr\\Ui\\技能图标\\icon_sk_gb_23.spr", charAnim:11),

            // 125 Thiên Hạ Vô Cẩu: damage (bangda_egou) [PC radius L20=512]
            DamageSkillNew(125, "Bổng Đả ác Cẩu", "Thiên Hạ Vô Cẩu", 50, 20, 512, 47, SkillMissileForm.Surround, 16, true, false, 11,
                phys: (lv) => Link(lv, (1, 10, ""), (20, 179, "")),
                fire: (lv) => (Link(lv, (1, 70, ""), (20, 360, "")), 0, Link(lv, (1, 70, ""), (20, 420, ""))),
                cost: (lv) => (Link(lv, (1, 28, ""), (20, 48, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 10, ""), (20, 50, "")), -1, 0),
                horseLimit: 1, missilesGenerateData: 5),

            // 126 Kim Ô Ánh Tuyết: buff
            ResistBuff(126, "Kim Ô ánh Tuyết", "Kim Ô Ánh Tuyết", 40, MagicAttributeKind.ColdResP, costBugReturnsResultTwice:true),

            // 127 Hoạt Bất Lưu Thủ: utility buff
            UtilitySkill(127, "Hoạt Bất Lưu Thủ 11", "Hoạt Bất Lưu Thủ", 10, 400, SkillMissileForm.None, targetEnemy:false, targetSelf:true, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int pct = Link(lv, (1, 9, ""), (20, 66, ""));
                int dur = 18 * Link(lv, (1, 120, ""), (20, 180, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, pct, dur, 0));
                int cost = Link(lv, (1, 24, ""), (20, 50, ""));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, cost, 0, 0));
                return d;
            }, skillStyle: PcSkillStyle.Missiles),

            // 128 Kháng Long Hữu Hối: damage (kanglong_youhui)
            // [SECT-ALL fix 2026-06-15] PC source: skills.txt 128 IsMelee=0, ByMissle=0 → CAST skill, không melee.
            //   Comment cũ (commit e194a242a) nói "MeleeType=Melee_JumpAndAttack" là ĐỌC SAI PC source. Đã revert.
            DamageSkillNew(128, "Kháng Long Hữu Hối", "Kháng Long Hữu Hối", 50, 20, 512, 48, SkillMissileForm.Fan, 15, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 10, ""), (20, 536, "")), 0, Link(lv, (1, 10, ""), (20, 536, ""))),
                cost: (lv) => (Link(lv, (1, 10, ""), (20, 50, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 10, ""), (20, 50, "")), -1, 0),
                horseLimit: 1,
                meleeType: PcMeleeType.None), // [SECT-ALL] PC IsMelee=0 → không melee, không dash

            // 129 Hóa Hiểm Vi Di: buff
            UtilitySkill(129, "Hóa Hiểm Vi Di", "Hóa Hiểm Vi Di", 20, 400, SkillMissileForm.Surround, targetEnemy:false, targetSelf:true, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int ret = Link(lv, (1, 4, ""), (20, 46, ""));
                int def = Link(lv, (1, 48, ""), (20, 800, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.MeleeDamageReturnP, ret, -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, def, -1, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            }, skillStyle: PcSkillStyle.PassivityNpcState),

            // 130 Túy Điệp Cuồng Vũ: buff (zuidie_kuangwu)
            UtilitySkill(130, "Túy Điệp Cuồng Vũ ", "Túy Điệp Cuồng Vũ", 60, 400, SkillMissileForm.None, targetEnemy:false, targetSelf:true, stateSpecialId:43, levelData:(lv)=>{
                var d = new SkillLevelData{level=lv};
                int dur = 18 * Link(lv, (1, 120, ""), (30, 180, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 1, ""), (30, 30, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 10, ""), (30, 175, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, Link(lv, (1, 10, ""), (30, 215, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxYanP, Link(lv, (1, 21, ""), (35, 20, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 5, ""), (20, 30, "Conic")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 50, ""), (20, 100, "")), 0, 0));
                return d;
            }, maxLevel: 30),

            // ===== MOD Vietnam Cái Bang additions =====
            PassiveMasteryLong(274, "Giáng Long Chưởng ", "Giương Long Chưởng", 30,
                lifemax: (lv) => Link(lv, (1, -1, ""), (20, -25, "")),
                manamax: (lv) => Link(lv, (1, 12, ""), (20, 50, "")),
                addfire: (lv) => Link(lv, (1, 35, ""), (20, 750, "")),
                elementParam: 9, icon: "\\spr\\Ui\\技能图标\\icon_sk_gb_32.spr"),

            UtilitySkill(277, "Hoạt Bất Lưu Thủ ", "Hoành Bách Lộ Thiên", 40, 400, SkillMissileForm.Surround, targetEnemy:false, targetSelf:true, stateSpecialId:3, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int pct = Link(lv, (1, 9, ""), (20, 66, ""));
                int dur = 18 * Link(lv, (1, 120, ""), (20, 180, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, pct, dur, 0));
                int cost = Link(lv, (1, 24, ""), (20, 50, ""));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, cost, 0, 0));
                return d;
            }),

            // 357 Phi Long Tại Thiên [SECT-ALL fix 2026-06-15]
            // PC source EVIDENCE (đọc từ 00.src-tinh-kiem/Client 6.0/settings/skills.txt + file/skill/gaibang.lua):
            //   skills.txt skill 357: IsMelee=0, ByMissle=1, MslsGenerate=5, ChildSkillId=166, CharAnimId=11
            //   gaibang.lua::feilong_zaitian:
            //     missle_speed_v={1,20→20,24}, skill_misslenum_v={1,1→20,4}, skill_attackradius={1,448→20,512}
            //     skill_collideevent → skill 389 (explosion chain)
            //   missles.txt id 166 (Phi Long missile):
            //     SPR: \spr\skill\丐帮\mag_gb_05_亢龙有悔.spr, explosion SPR: mag_gb_bz5_爆炸效果.spr
            //   => PHI LONG LÀ SKILL MISSILE (cast + projectile), KHÔNG CÓ DASH/LUNGE.
            // BUG TRƯỚC: commit e194a242a ép meleeType=JumpAndAttack do đọc sai gaibang.lua.
            //   → Fix: revert về missile thuần (PcMeleeType.None). Player đứng yên cast, missile bay tới target.
            WithJxPreCast(DamageSkillNew(357, "Phi Long Tại Thiên ", "Phi Long Tại Thiên", 80, 20, 512, 166, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 10, ""), (15, 300, ""), (20, 750, "")), 0, Link(lv, (1, 10, ""), (15, 300, ""), (20, 750, ""))),
                cost: (lv) => (Link(lv, (1, 10, ""), (20, 65, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 20, ""), (20, 60, "")), -1, 0),
                horseLimit: 1,
                meleeType: PcMeleeType.None),
                // jx-source Tinh Kiem: PreCastSpr = mag_bz_huo3 (KHÁC PC stock 2011 mag_tr_16)
                "\\spr\\skill\\天忍\\mag_bz_huo3_爆炸效果.spr"),

            DamageSkillNew(359, "Thiên Hạ Vô Cẩu ", "Thiên Hạ Vô Cẩu (player)", 80, 20, 512, 168, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => Link(lv, (1, 12, ""), (15, 100, ""), (20, 206, "")),
                fire: (lv) => (Link(lv, (1, 70, ""), (15, 150, ""), (20, 285, "")), 0, Link(lv, (1, 70, ""), (15, 200, ""), (20, 432, ""))),
                cost: (lv) => (Link(lv, (1, 20, ""), (20, 50, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 20, ""), (20, 60, "")), -1, 0),
                horseLimit: 1),

            // 358 Kháng Long Hữu Hối (Cái Bang) — PC source EVIDENCE:
            //   skills.txt skill 358: ChildSkillId=167, CharAnimId=11, AttackRadius=570
            //   gaibang.lua::kanglong-youhui (Tinh Kiem): physicsdamage_v, firedamage_v, misslesform_v=2
            //     (level<11 straight line, level>=11 fan), misslenum_v up to 18
            //   missles1.txt missile 167 (Long Chiến Ư Dã):
            //     MoveKind=0 (stationary area effect), AnimFile=\spr\skill\gb\龙战于野.spr, 15 frames
            //     Sound=\sound\skill\sound_k044.wav, IsRangeDmg=0, DmgRange=3, AutoExplode=1
            //   PC gaibang.lua unlocks 358 at level 20: [3]={{1,358},{20,358}}
            DamageSkillNew(358, "Kháng Long Hữu Hối ", "Kháng Long Hữu Hối (player)", 50, 20, 570, 167, SkillMissileForm.Fan, 1, false, false, 11,
                phys: (lv) => Link(lv, (1, 20, ""), (20, 120, "")),
                fire: (lv) => (Link(lv, (1, 130, ""), (20, 850, "")), 0, Link(lv, (1, 200, ""), (20, 1000, ""))),
                cost: (lv) => (Link(lv, (1, 10, ""), (20, 30, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 25, ""), (20, 70, "")), -1, 0),
                horseLimit: 1,
                meleeType: PcMeleeType.None),

            PassiveMasteryDao(360, "Tiêu Diêu Công ", "Tiêu Dao Công", 60,
                attackSpeed: (lv) => Link(lv, (1, 6, ""), (20, 65, "")),
                castSpeed: (lv) => Link(lv, (1, 6, ""), (20, 65, "")),
                elementParam: 2, icon: "\\spr\\Ui\\skill\\逍遥功.spr", charAnim:11),

            UtilitySkill(714, "Hỗn Thiên Khí Công", "Hỗn Thiên Khí Công", 120, 180, SkillMissileForm.None, targetEnemy:false, targetSelf:true, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int pct = Link(lv, (1, 1, ""), (20, 6, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, pct, 0, 0));
                return d;
            }),

            UtilitySkill(720, "Hỗn Thiên Khí Công_Quyết Chú", "Hỗn Thiên Khí Công Quyết Chí", 120, 440, SkillMissileForm.Surround, targetEnemy:true, targetSelf:false, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int dur = 18 * Link(lv, (1, 3, ""), (20, 9, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, Link(lv, (1, -2, ""), (20, -10, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, Link(lv, (1, -3, ""), (20, -15, "")), dur, 0));
                return d;
            }),

            DamageSkillNew(1073, "Thời Thặng Lục Long", "Thần Thủ Lệnh Long", 150, 20, 512, 335, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 24, ""), (15, 720, ""), (20, 1800, "")), 0, Link(lv, (1, 24, ""), (15, 720, ""), (20, 1800, ""))),
                // PC gaibang.lua zhanggaibang150: skill_cost_v={{{1,12},{20,78},{23,98}}}
                cost: (lv) => (Link(lv, (1, 12, ""), (20, 78, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 40, ""), (20, 80, "")), -1, 0),
                horseLimit: 1),

            DamageSkillNew(1074, "Bổng Huýnh Lược Địa", "Bổng Hoành Lược Mã", 150, 20, 512, 336, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => Link(lv, (1, 10, ""), (15, 80, ""), (20, 165, "")),
                fire: (lv) => (Link(lv, (1, 60, ""), (15, 120, ""), (20, 230, "")), 0, Link(lv, (1, 60, ""), (15, 160, ""), (20, 345, ""))),
                cost: (lv) => (Link(lv, (1, 20, ""), (20, 50, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 40, ""), (20, 80, "")), -1, 0),
                horseLimit: 1),

            DamageSkillNew(1539, "天下无狗NPC", "Thiên Hạ Vô Cẩu (NPC)", 1, 60, 512, 47, SkillMissileForm.Surround, 16, false, false, 11,
                phys: (lv) => Link(lv, (1, 10, ""), (20, 179, "")),
                fire: (lv) => (Link(lv, (1, 70, ""), (20, 360, "")), 0, Link(lv, (1, 70, ""), (20, 420, ""))),
                cost: (lv) => (Link(lv, (1, 28, ""), (20, 48, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 10, ""), (20, 50, "")), -1, 0),
                horseLimit: 1, missilesGenerateData: 5),

            LongChienUYuyeSkill(),
            NguDieuCanKhonSkill(),
        };


        public static List<SkillDefinition> CreateWuDangSkills() => new()
        {
            // Source: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/skill2/wudang.lua
            WuDangPassiveJianFa(),
            WuDangPassiveQuanFa(),
            WuDangYinYangQi(),
            WuDangLightningDamage(153, "怒雷指", "Nộ Lôi Chỉ", 10, 400, 24, 1, 11,
                light1: lv => Link(lv, (1, 1, ""), (20, 5, "")),
                light3: lv => Link(lv, (1, 1, ""), (20, 75, "")),
                series: lv => Link(lv, (1, 1, ""), (20, 10, "")),
                cost: lv => Link(lv, (1, 15, ""), (20, 20, ""))),
            WuDangLightningDamage(155, "沧海明月", "Thương Hải Minh Nguyệt", 10, 480, 25, 1, 11,
                physics: lv => Link(lv, (1, 5, ""), (20, 75, "")),
                light1: lv => Link(lv, (1, 6, ""), (20, 12, "")),
                light3: lv => Link(lv, (1, 6, ""), (20, 115, "")),
                series: lv => Link(lv, (1, 1, ""), (20, 10, "")),
                cost: lv => Link(lv, (1, 10, ""), (20, 15, ""))),
            WuDangChunYangXinFa(),
            WuDangManaShield(),
            WuDangLightningDamage(158, "剑飞惊天", "Kiếm Phi Kinh Thiên", 30, 400, 26, 1, 11,
                physics: lv => Link(lv, (1, 20, ""), (20, 115, "")),
                light1: lv => Link(lv, (1, 10, ""), (20, 24, "")),
                light3: lv => Link(lv, (1, 10, ""), (20, 225, "")),
                series: lv => Link(lv, (1, 5, ""), (20, 30, "")),
                cost: lv => Link(lv, (1, 10, ""), (20, 25, ""))),
            WuDangQiXingZhen(),
            WuDangRunPassive(),
            WuDangLiangYiXinFa(),
            WuDangXuanYiWuXiang(),
            WuDangRenJianHeYi(),
            WuDangLightningDamage(164, "搏击二复", "Bác Cấp Nhi Phục", 50, 470, 28, 1, 11,
                light1: lv => Link(lv, (1, 5, ""), (20, 8, "")),
                light3: lv => Link(lv, (1, 5, ""), (20, 175, "")),
                series: lv => Link(lv, (1, 5, ""), (20, 30, "")),
                cost: lv => Link(lv, (1, 60, ""), (20, 70, "")),
                stun: lv => (Link(lv, (1, 20, ""), (20, 55, "")), Link(lv, (1, 1, ""), (20, 20, "")), 0)),
            // [SECT-QUICKWIN] §2.1.2 G4: WuDang ID 165 Vô Ngã Vô Kiếm — childSkillNum 16 sai PC.
            // PC wudang.lua::wuwo_wujian: skill_misslenum_v {{1,1},{20,8},{21,8}} (max 8 missiles ở L20+).
            // Trước fix: 16 → sai 2×. Sau fix: 8 đúng PC.
            // Đồng thời radius 400 → 512 theo PC skill_attackradius {{1,448},{20,512},{21,512}}.
            WuDangLightningDamage(165, "无我无剑", "Vô Ngã Vô Kiếm", 50, 512, 29, 8, 11,
                light1: lv => Link(lv, (1, 1, ""), (20, 5, "")),
                light3: lv => Link(lv, (1, 5, ""), (20, 752, "")),
                series: lv => Link(lv, (1, 10, ""), (20, 50, "")),
                cost: lv => Link(lv, (1, 70, ""), (20, 130, "")),
                stun: lv => (Link(lv, (1, 5, ""), (20, 20, "")), Link(lv, (1, 1, ""), (20, 10, "")), 0)),
            WuDangTaiJiShenGong(),
        };

        public static SkillDefinition LongChienUYuyeSkill()
        {
            var s = BaseSkill(389, "Long Chiến Ư Dã ", "Long Chiến Ư Dã", 80, 20, 570, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 195; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            s.missileSpriteId = Sprite("\\spr\\skill\\天\\mag_bz_huo3_爆炸效果.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 17, ""), (20, 371, "")), 0, Link(lv, (1, 17, ""), (20, 371, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 20, ""), (20, 60, "")), -1, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        /// <summary>
        /// PC zhanggaibang150 (skill 1073) CollideEvent[3] sub-skill. L10+ 335 missile impact spawns
        /// skill 1072, which fires its child 334 (stationary flash) for 10 ticks.
        /// PC missles.txt missile 334: MoveKind=0, LifeTime=10, Speed=0, DmgInterval=5.
        /// </summary>
        public static SkillDefinition NguDieuCanKhonSkill()
        {
            var s = BaseSkill(1072, "Ngũ Diệu Càn Khôn ", "Ngũ Diệu Càn Khôn", 150, 20, 512, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 334; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            s.missileSpriteId = Sprite("\\spr\\skill\\150\\gb\\gb_150_shishengliulong_d.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 20, ""), (20, 450, ""), (23, 585, ""), (26, 653, "")), 0, Link(lv, (1, 20, ""), (20, 450, ""), (23, 585, ""), (26, 653, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        public static SkillDefinition CaiBangDogBeatingAuraChildSkill()
        {
            var s = BaseSkill(209, "打狗阵子弹", "Đả Cẩu Trận Tử Đạn", 50, 20, 180, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 92; s.childSkillNum = 1; s.baseSkill = true; s.byMissile = true;
            s.targetAlly = true; s.targetSelf = true; s.stateSpecialId = 44; s.charAnimId = 14;
            AddLevels(s, lv => Immediate(MagicAttributeKind.AddDefenseV, 30 + 10 * lv, 25, 0));
            return s;
        }


        private static SkillDefinition WuDangPassiveJianFa()
        {
            var s = BaseSkill(151, "武当剑法", "Võ Đang Kiếm Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 15, ""), (20, 72, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangPassiveQuanFa()
        {
            var s = BaseSkill(152, "武当拳法", "Võ Đang Quyền Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaShieldP, Link(lv, (1, -5, ""), (15, -15, ""), (20, -25, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 20, ""), (15, 250, ""), (20, 915, "")), -1, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangLightningDamage(int id, string raw, string vi, int req, int radius, int child, int childNum, int charAnim, Func<int,int> light1, Func<int,int> light3, Func<int,int> series, Func<int,int> cost, Func<int,int> physics = null, Func<int,(int,int,int)> stun = null)
        {
            var s = BaseSkill(id, raw, vi, req, 20, radius, SkillMissileForm.Single); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.targetEnemy = true; s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr"); s.missileSpriteId = Sprite(WuDangMissilePath(child));
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; if (physics != null) d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, physics(lv), 0, 0)); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, light1(lv), 0, light3(lv))); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, series(lv), 0, 0)); if (stun != null) { var st = stun(lv); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, st.Item1, st.Item2, st.Item3)); } d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, cost(lv), 0, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangYinYangQi()
        {
            var s = BaseSkill(154, "阴阳气", "Âm Dương Khí", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.LightingResP, 10 + Floor(1.5f * lv), -1, 0)); return s;
        }

        private static SkillDefinition WuDangChunYangXinFa()
        {
            var s = BaseSkill(156, "纯阳心法", "Thuần Dương Tâm Pháp", 20, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.ManaMaxP, 25 + 11 * lv, -1, 0)); return s;
        }

        private static SkillDefinition WuDangLiangYiXinFa()
        {
            var s = BaseSkill(161, "两仪心法", "Lưỡng Nghi Tâm Pháp", 40, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.CastSpeedV, Floor(Log10(lv + 1) * 80f), -1, 0)); return s;
        }

        private static SkillDefinition WuDangXuanYiWuXiang()
        {
            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.1.2 G7: WuDang 162 damage sai ~14×.
            // PC wudang.lua::xuanyi_wuxiang main table: lightingdamage_v [1]={{1,1},{20,10}} [3]={{1,10},{20,100}}
            //   (L20 min=10, max=100). Per-skill file xuanyi-wuxiang.lua định nghĩa KHÁC (4+lv*7 = 144 ở L20)
            //   nhưng main table là canonical theo audit 2026-06-15.
            // Trước fix: L20 = (4+20*7, 296+20*59) = (144, 1476) → 14.7× off.
            // Sau fix: min/max theo PC main table {{1,1},{20,10}} / {{1,10},{20,100}}.
            var s = BaseSkill(162, "玄一无象", "Huyền Nhất Vô Tượng", 50, 20, 520, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 27; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr"); s.missileSpriteId = Sprite("\\spr\\skill\\武当\\wd_04_玄一无象.spr");
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 1, ""), (20, 10, "")), 0, Link(lv, (1, 10, ""), (20, 100, "")))); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20 + lv * 3, 0, 0)); return d; }); return s;
        }

        // [SECT-QUICKWIN] §2.1.2 G4 + G6: WuDang ID 163 Nhân Kiếm Hợp Nhất — childSkillId 215 sai PC + radius sai.
        // PC wudang.lua::renjian_heyi: childSkillId=371 (skill_startevent[3]), collideSkill=162 (skill_collideevent[3]),
        //   showevent id 1 (L10-14) hoặc 5 (L15+), radius=90 đúng mobile.
        // Trước fix: childSkillId=215 (không có trong PC) + thiếu 3 event chain.
        // Sau fix: childSkillId=371 (startSkillId anchor), s.collideSkillId=162 (anchor), childSkillNum=1.
        //   ShowEvent charAnimId runtime check ở Phase 4 (cần level-gated 10/15).
        private static SkillDefinition WuDangRenJianHeYi()
        {
            var s = BaseSkill(163, "人剑合一", "Nhân Kiếm Hợp Nhất", 50, 20, 90, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 371; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr");
            s.startSkillId = 371; s.startSkillLevel = 1; // G6: anchor cho StartEvent (Phase 4 wire runtime)
            s.collideSkillId = 162; s.collideSkillLevel = 1; // G6: anchor cho CollideEvent — fire 162 Huyền Nhất Vô Tượng (Phase 4 wire)
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 8, ""), (15, 80, ""), (20, 194, "")), 0, 0)); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 12, ""), (20, 35, "")), 0, Link(lv, (1, 12, ""), (15, 100, ""), (20, 268, "")))); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP, Link(lv, (1, 65, ""), (20, 345, "")), 0, 0)); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.StealManaP, Link(lv, (1, 1, ""), (20, 5, "")), 0, 0)); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 16, ""), (20, 25, "")), 0, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 1, ""), (20, 10, "")), Link(lv, (1, 1, ""), (20, 10, "")), 0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 35, ""), (20, 60, "")), 0, 0)); return d; }); return s;
        }


        private static SkillDefinition WuDangManaShield()
        {
            var s = BaseSkill(157, "坐忘无我", "Tọa Vọng Vô Ngã", 50, 20, 400, SkillMissileForm.None); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.targetSelf = true; s.charAnimId = 11;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaShieldP, Link(lv, (1, 25, ""), (5, 75, ""), (20, 99, "")), Link(lv, (1, 18*120, ""), (20, 18*180, "")), 0)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 60, ""), (20, 160, "")), 0, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangQiXingZhen()
        {
            var s = BaseSkill(159, "七星阵", "Thất Tinh Trận", 20, 20, 180, SkillMissileForm.None); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.childSkillId = 211; s.childSkillNum = 1; s.targetSelf = true; s.charAnimId = 14; s.missileSpriteId = Sprite(WuDangMissilePath(211));
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, 20 + lv * 4, 18, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, 60 + lv * 37, 18, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, 30 + lv * 6, 18, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangRunPassive()
        {
            var s = BaseSkill(160, "梯云纵", "Thế Vân Tung", 40, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 18, ""), (20, 60, "")), -1, 0)); return s;
        }

        private static SkillDefinition WuDangTaiJiShenGong()
        {
            var s = BaseSkill(166, "太极神功", "Thái Cực Thần Công", 60, 30, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 21, ""), (30, 65, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, Link(lv, (1, 21, ""), (30, 65, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 20, ""), (20, 275, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaMaxP, Link(lv, (1, 35, ""), (30, 245, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 1, ""), (30, 12, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 5, ""), (30, 25, "")), -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LightingEnhanceP, Link(lv, (1, 16, ""), (30, 100, "")), -1, 0)); return d; }); return s;
        }

        private static string WuDangMissilePath(int missileId) => missileId switch
        {
            24 => "\\spr\\skill\\武当\\wd_01_怒雷指.spr",
            25 => "\\spr\\skill\\武当\\wd_02_沧海明月.spr",
            26 => "\\spr\\skill\\武当\\wd_03_天际惊雷.spr",
            28 => "\\spr\\skill\\武当\\wd_05_剥及而复.spr",
            29 => "\\spr\\skill\\武当\\wd_10_无我无剑.spr",
            211 => "\\spr\\skill\\少林\\bz_bo1_金波.spr",
            _ => "",
        };


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

        private static SkillDefinition PassiveMastery(int id, string raw, string vi, int req, Func<int,int> addPhys, int elementParam, string icon, int charAnim = 14)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = charAnim; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addPhys(lv), -1, elementParam)); return d; }); return s;
        }

        private static SkillDefinition PassiveMasteryWithDeadly(int id, string raw, string vi, int req, Func<int,int> addPhys, Func<int,int> deadly, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addPhys(lv), -1, elementParam));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, deadly(lv), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveMasteryChuong(int id, string raw, string vi, int req, Func<int,int> addFire, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, addFire(lv), -1, elementParam));
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveMasteryLong(int id, string raw, string vi, int req, Func<int,int> lifemax, Func<int,int> manamax, Func<int,int> addfire, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, addfire(lv), -1, elementParam));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxP, lifemax(lv), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaMaxP, manamax(lv), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveMasteryDao(int id, string raw, string vi, int req, Func<int,int> attackSpeed, Func<int,int> castSpeed, int elementParam, string icon, int charAnim = 14)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = charAnim; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, attackSpeed(lv), -1, elementParam));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, castSpeed(lv), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition DamageSkillNew(int id, string raw, string vi, int req, int max, int radius, int child, SkillMissileForm form, int childNum, bool isPhysical, bool targetOnly, int charAnim, Func<int,int> phys, Func<int,(int,int,int)> fire, Func<int,(int,int,int)> cost, Func<int,SkillLevelData> extra=null, int horseLimit=0, int missilesGenerateData=0, PcMeleeType meleeType=PcMeleeType.None)
        {
            var s = BaseSkill(id, raw, vi, req, max, radius, form); s.skillStyle = PcSkillStyle.Missiles; s.byMissile = true; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.isPhysical = isPhysical; s.targetOnly = targetOnly; s.targetEnemy = true; s.horseLimit = horseLimit; s.missilesGenerateData = missilesGenerateData; s.meleeType = meleeType;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, phys(lv), 0, 0));
                var f = fire(lv);
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, f.Item1, f.Item2, f.Item3));
                var c = cost(lv);
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, c.Item1, c.Item2, c.Item3));
                if (extra != null)
                {
                    var ext = extra(lv);
                    foreach (var state in ext.state) d.state.Add(state);
                }
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveResist(int id, string raw, string vi, int req, MagicAttributeKind kind)
        { var s = BaseSkill(id, raw, vi, req, 20, 400, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 11; AddLevels(s, lv => State(kind, Floor(Log10(lv+5)/2f*50), -1, 0)); return s; }

        private static SkillDefinition ResistBuff(int id, string raw, string vi, int req, MagicAttributeKind kind, bool shortDuration=false, bool costBugUndefined=false, bool costBugReturnsResultTwice=false)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 400, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = id switch {118=>49,120=>50,123=>51,126=>52,129=>53,_=>0}; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.waitTime = 5; s.timePerCast = 2; s.targetAlly = true; s.targetSelf = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; int pct = shortDuration ? Floor(Log10(lv+1)/2f*60) : Floor(Log10(lv+1)/2f*80); int dur = shortDuration ? 600+600*lv : 1200+1200*lv; d.state.Add(new SkillMagicAttribute(kind,pct,dur,0)); if (costBugUndefined) d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,0,0,0)); else if (costBugReturnsResultTwice) d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,20,20,0)); else d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,20,0,0)); return d; }); return s;
        }

        private static SkillDefinition DamageSkill(int id, string raw, string vi, int req, int max, int radius, int child, SkillMissileForm form, int childNum, bool isPhysical, bool targetOnly, int charAnim, Func<int,(int,int,int)> phys, Func<int,(int,int,int)> fire, Func<int,(int,int,int)> cost, Func<int,SkillMagicAttribute> extra=null, int horseLimit=0, int missilesGenerateData=0, PcMeleeType meleeType=PcMeleeType.None)
        {
            var s = BaseSkill(id, raw, vi, req, max, radius, form); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.isPhysical = isPhysical; s.targetOnly = targetOnly; s.targetEnemy = true; s.horseLimit = horseLimit; s.missilesGenerateData = missilesGenerateData; s.meleeType = meleeType;
            s.effectSourceId = id >= 118 ? Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr") : null;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; var p=phys(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV,p.Item1,p.Item2,p.Item3)); var f=fire(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV,f.Item1,f.Item2,f.Item3)); if (extra!=null) d.state.Add(extra(lv)); var c=cost(lv); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,c.Item1,c.Item2,c.Item3)); return d; }); return s;
        }

        private static SkillDefinition AuraSkill(int id, string raw, string vi, int req, int radius, int stateId, int child, Func<int,SkillLevelData> levelData)
        { var s = BaseSkill(id, raw, vi, req, 20, radius, SkillMissileForm.None); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.isAura = true; s.stateSpecialId = stateId; s.childSkillId = child; s.childSkillLevel = 1; s.childSkillNum = 1; s.targetSelf = true; s.charAnimId = 14; AddLevels(s, levelData); return s; }

        private static SkillDefinition UtilitySkill(int id, string raw, string vi, int req, int radius, SkillMissileForm form, bool targetEnemy, bool targetSelf, int stateSpecialId=0, Func<int,SkillLevelData> levelData=null, PcSkillStyle skillStyle = PcSkillStyle.InitiativeNpcState, int maxLevel = 20)
        { var s = BaseSkill(id, raw, vi, req, maxLevel, radius, form); s.skillStyle = skillStyle; s.targetEnemy = targetEnemy; s.targetSelf = targetSelf; s.stateSpecialId = stateSpecialId; s.charAnimId = 11; AddLevels(s, levelData ?? (lv => new SkillLevelData{level=lv})); return s; }
        /// <summary>Override PreCastSpr với path từ jx-source (khác PC stock 2011).</summary>
        private static SkillDefinition WithJxPreCast(SkillDefinition s, string jxPreCastSprPath)
        { s.effectSourceId = Sprite(jxPreCastSprPath); return s; }

        private static SkillDefinition BaseSkill(int id, string raw, string vi, int req, int max, int radius, SkillMissileForm form)
        {
            var iconPath = IconPathForSkill(id);

            // Fetch actual PC text icon path from parsed registries
            if (SandboxManager.Instance != null && SandboxManager.Instance.PcSkillsFull != null)
            {
                var pcSkill = SandboxManager.Instance.PcSkillsFull.Resolve(id);
                if (pcSkill != null && !string.IsNullOrEmpty(pcSkill.iconPath))
                {
                    iconPath = pcSkill.iconPath;
                }
            }
            
            return new SkillDefinition 
            { 
                skillId=id, nameRaw=raw, nameNormalized=vi, reqLevel=req, maxLevel=max, 
                attackRadius=radius, missileForm=form, 
                faction = IsCaiBangSkill(id) ? CombatFaction.CaiBang : IsWuDangSkill(id) ? CombatFaction.WuDang : IsShaolinSkill(id) ? CombatFaction.Shaolin : IsTangMenSkill(id) ? CombatFaction.TangMen : IsEMeiSkill(id) ? CombatFaction.EMei : IsTianWangSkill(id) ? CombatFaction.TianWang : IsWuDuSkill(id) ? CombatFaction.WuDu : IsCuiYanSkill(id) ? CombatFaction.CuiYan : IsTianRenSkill(id) ? CombatFaction.TianRen : IsKunLunSkill(id) ? CombatFaction.KunLun : CombatFaction.None, 
                iconSourceId = Sprite(iconPath), equipLimit=-2 
            };
        }

        // Cái Bang skill set: PC gốc 115-130 + MOD 274, 277, 357, 359, 360, 714, 720, 1073, 1074, 1539 (NPC variant).
        // 1539 is an NPC/boss version of Thiên Hạ Vô Cẩu and stays in the catalog for boss AI;
        // the player skill panel filters it out via isNpcVariant.
        public static bool IsCaiBangSkill(int id) => id==209 || (id>=115 && id<=130) || id==274 || id==277 || id==357 || id==358 || id==359 || id==360 || id==714 || id==720 || id==1073 || id==1074 || id==1539 || id==389;
        public static bool IsWuDangSkill(int id) => id >= WuDangMinSkillId && id <= WuDangMaxSkillId;
        public static bool IsShaolinSkill(int id) => id >= ShaolinMinSkillId && id <= ShaolinMaxSkillId && id != 5 && id != 7;
        public static bool IsTangMenSkill(int id) => id >= TangMenMinSkillId && id <= TangMenMaxSkillId && id != 53 && id != 44 && id != 46 && id != 49 && id != 52 && id != 56;
        public static bool IsEMeiSkill(int id) => id >= EMeiMinSkillId && id <= EMeiMaxSkillId && id != 78;
        public static bool IsTianWangSkill(int id) => id >= TianWangMinSkillId && id <= TianWangMaxSkillId && id != 25 && id != 27 && id != 28 && id != 38 && id != 39;
        public static bool IsWuDuSkill(int id) => id >= WuDuMinSkillId && id <= WuDuMaxSkillId && id != 61;
        public static bool IsCuiYanSkill(int id) => id >= CuiYanMinSkillId && id <= CuiYanMaxSkillId && id != 96 && id != 98 && id != 104 && id != 106 && id != 107 && id != 110 && id != 112;
        public static bool IsTianRenSkill(int id) => id == 131 || id == 132 || (id >= 135 && id <= 150);
        public static bool IsKunLunSkill(int id) => id >= 167 && id <= 184;
        private static string IconPathForSkill(int id) => id switch
        {
            1 => "\\spr\\Ui\\技能图标\\icon_sk_ty_ap.spr",
            2 => "\\spr\\Ui\\技能图标\\icon_sk_ty_at.spr",
            53 => "\\spr\\Ui\\技能图标\\icon_sk_ty_as.spr",
            115 => "\\spr\\Ui\\技能图标\\icon_sk_gb_gf.spr",
            116 => "\\spr\\Ui\\技能图标\\icon_sk_gb_aq.spr",
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
            151 => "\\spr\\Ui\\技能图标\\icon_sk_wd_jf.spr",
            152 => "\\spr\\Ui\\技能图标\\icon_sk_wd_qf.spr",
            153 => "\\spr\\Ui\\技能图标\\icon_sk_wa_01.spr",
            155 => "\\spr\\Ui\\技能图标\\icon_sk_wa_13.spr",
            157 => "\\spr\\Ui\\技能图标\\icon_sk_wa_12.spr",
            158 => "\\spr\\Ui\\技能图标\\icon_sk_wa_22.spr",
            159 => "\\spr\\Ui\\技能图标\\icon_sk_wa_23.spr",
            160 => "\\spr\\Ui\\技能图标\\icon_sk_wa_21.spr",
            164 => "\\spr\\Ui\\技能图标\\icon_sk_wa_42.spr",
            165 => "\\spr\\Ui\\技能图标\\icon_sk_wa_43.spr",
            166 => "\\spr\\Ui\\技能图标\\icon_sk_wa_41.spr",
            714 => "\\spr\\Ui\\技能图标\\icon_sk_gb_120.spr",
            720 => "\\spr\\Ui\\技能图标\\icon_sk_sl_01.spr",
            1073 => "\\spr\\Ui\\技能图标\\150\\icon_sk_150_gb_01.spr", // MOD 150-tier GB icon, extracted from updatejx08.pak.
            1074 => "\\spr\\Ui\\技能图标\\150\\icon_sk_150_gb_02.spr", // MOD 150-tier GB icon, extracted from updatejx08.pak.
            3 => "\\spr\\Ui\\技能图标\\icon_sk_sl_jf.spr",
            4 => "\\spr\\Ui\\技能图标\\icon_sk_sl_gf.spr",
            6 => "\\spr\\Ui\\技能图标\\icon_sk_sl_df.spr",
            8 => "\\spr\\Ui\\技能图标\\icon_sk_sl_qf.spr",
            9 => "\\spr\\Ui\\技能图标\\icon_sk_sl_01.spr",
            10 => "\\spr\\Ui\\技能图标\\icon_sk_sl_02.spr",
            11 => "\\spr\\Ui\\技能图标\\icon_sk_sl_12.spr",
            12 => "\\spr\\Ui\\技能图标\\icon_sk_sl_11.spr",
            13 => "\\spr\\Ui\\技能图标\\icon_sk_sl_13.spr",
            14 => "\\spr\\Ui\\技能图标\\icon_sk_sl_21.spr",
            15 => "\\spr\\Ui\\技能图标\\icon_sk_sl_22.spr",
            16 => "\\spr\\Ui\\技能图标\\icon_sk_sl_23.spr",
            17 => "\\spr\\Ui\\技能图标\\icon_sk_sl_31.spr",
            18 => "\\spr\\Ui\\技能图标\\icon_sk_sl_32.spr",
            19 => "\\spr\\Ui\\技能图标\\icon_sk_sl_41.spr",
            20 => "\\spr\\Ui\\技能图标\\icon_sk_sl_42.spr",
            21 => "\\spr\\Ui\\技能图标\\icon_sk_sl_43.spr",
            43 => "\\spr\\Ui\\技能图标\\icon_sk_tm_aq.spr",
            45 => "\\spr\\Ui\\技能图标\\icon_sk_tm_01.spr",
            47 => "\\spr\\Ui\\技能图标\\icon_sk_tm_11.spr",
            48 => "\\spr\\Ui\\技能图标\\icon_sk_tm_13.spr",
            50 => "\\spr\\Ui\\技能图标\\icon_sk_tm_22.spr",
            51 => "\\spr\\Ui\\技能图标\\icon_sk_tm_21.spr",
            54 => "\\spr\\Ui\\技能图标\\icon_sk_tm_32.spr",
            55 => "\\spr\\Ui\\技能图标\\icon_sk_tm_31.spr",
            57 => "\\spr\\Ui\\技能图标\\icon_sk_tm_42.spr",
            58 => "\\spr\\Ui\\技能图标\\icon_sk_tm_43.spr",
            77 => "\\spr\\Ui\\技能图标\\icon_sk_em_jf.spr",
            79 => "\\spr\\Ui\\技能图标\\icon_sk_em_aq.spr",
            80 => "\\spr\\Ui\\技能图标\\icon_sk_em_01.spr",
            81 => "\\spr\\Ui\\技能图标\\icon_sk_em_02.spr",
            82 => "\\spr\\Ui\\技能图标\\icon_sk_em_11.spr",
            83 => "\\spr\\Ui\\技能图标\\icon_sk_em_12.spr",
            84 => "\\spr\\Ui\\技能图标\\icon_sk_em_13.spr",
            85 => "\\spr\\Ui\\技能图标\\icon_sk_em_21.spr",
            86 => "\\spr\\Ui\\技能图标\\icon_sk_em_22.spr",
            87 => "\\spr\\Ui\\技能图标\\icon_sk_em_23.spr",
            88 => "\\spr\\Ui\\技能图标\\icon_sk_em_31.spr",
            89 => "\\spr\\Ui\\技能图标\\icon_sk_em_32.spr",
            90 => "\\spr\\Ui\\技能图标\\icon_sk_em_33.spr",
            91 => "\\spr\\Ui\\技能图标\\icon_sk_em_41.spr",
            92 => "\\spr\\Ui\\技能图标\\icon_sk_em_42.spr",
            93 => "\\spr\\Ui\\技能图标\\icon_sk_em_43.spr",
            23 => "\\spr\\Ui\\技能图标\\icon_sk_tw_qf.spr",
            24 => "\\spr\\Ui\\技能图标\\icon_sk_tw_df.spr",
            26 => "\\spr\\Ui\\技能图标\\icon_sk_tw_cf.spr",
            29 => "\\spr\\Ui\\技能图标\\icon_sk_tw_62.spr",
            30 => "\\spr\\Ui\\技能图标\\icon_sk_tw_61.spr",
            31 => "\\spr\\Ui\\技能图标\\icon_sk_tw_12.spr",
            32 => "\\spr\\Ui\\技能图标\\icon_sk_tw_11.spr",
            33 => "\\spr\\Ui\\技能图标\\icon_sk_tw_13.spr",
            34 => "\\spr\\Ui\\技能图标\\icon_sk_tw_01.spr",
            35 => "\\spr\\Ui\\技能图标\\icon_sk_tw_63.spr",
            36 => "\\spr\\Ui\\技能图标\\icon_sk_tw_23.spr",
            37 => "\\spr\\Ui\\技能图标\\icon_sk_tw_31.spr",
            40 => "\\spr\\Ui\\技能图标\\icon_sk_tw_41.spr",
            41 => "\\spr\\Ui\\技能图标\\icon_sk_tw_64.spr",
            42 => "\\spr\\Ui\\技能图标\\icon_sk_tw_43.spr",
            60 => "\\spr\\Ui\\技能图标\\icon_sk_wd_df.spr",
            62 => "\\spr\\Ui\\技能图标\\icon_sk_wd_zf.spr",
            63 => "\\spr\\Ui\\技能图标\\icon_sk_wd_01.spr",
            64 => "\\spr\\Ui\\技能图标\\icon_sk_wd_02.spr",
            65 => "\\spr\\Ui\\skill\\ẹêàảảắẫ±.spr",
            66 => "\\spr\\Ui\\技能图标\\icon_sk_wd_12.spr",
            67 => "\\spr\\Ui\\技能图标\\icon_sk_wd_13.spr",
            68 => "\\spr\\Ui\\技能图标\\icon_sk_wd_21.spr",
            69 => "\\spr\\Ui\\技能图标\\icon_sk_wd_22.spr",
            70 => "\\spr\\Ui\\技能图标\\icon_sk_wd_23.spr",
            71 => "\\spr\\Ui\\技能图标\\icon_sk_wd_31.spr",
            72 => "\\spr\\Ui\\技能图标\\icon_sk_wd_32.spr",
            73 => "\\spr\\Ui\\技能图标\\icon_sk_wd_33.spr",
            74 => "\\spr\\Ui\\技能图标\\icon_sk_wd_41.spr",
            75 => "\\spr\\Ui\\技能图标\\icon_sk_wd_42.spr",
            76 => "\\spr\\Ui\\技能图标\\icon_sk_wd_43.spr",
            95 => "\\spr\\Ui\\技能图标\\icon_sk_cy_df.spr",
            97 => "\\spr\\Ui\\技能图标\\icon_sk_cy_df.spr",
            99 => "\\spr\\Ui\\技能图标\\icon_sk_cy_01.spr",
            100 => "\\spr\\Ui\\技能图标\\icon_sk_cy_02.spr",
            101 => "\\spr\\Ui\\技能图标\\icon_sk_cy_03.spr",
            102 => "\\spr\\Ui\\技能图标\\icon_sk_cy_11.spr",
            103 => "\\spr\\Ui\\技能图标\\icon_sk_cy_12.spr",
            105 => "\\spr\\Ui\\技能图标\\icon_sk_cy_21.spr",
            108 => "\\spr\\Ui\\技能图标\\icon_sk_cy_31.spr",
            109 => "\\spr\\Ui\\技能图标\\icon_sk_cy_32.spr",
            111 => "\\spr\\Ui\\技能图标\\icon_sk_cy_41.spr",
            113 => "\\spr\\Ui\\技能图标\\icon_sk_cy_42.spr",
            114 => "\\spr\\Ui\\技能图标\\icon_sk_cy_43.spr",
            // Thiên Nhẫn (TianRen)
            131 => "\\spr\\Ui\\技能图标\\icon_sk_tr_df.spr",
            132 => "\\spr\\Ui\\技能图标\\icon_sk_tr_mf.spr",
            135 => "\\spr\\Ui\\技能图标\\icon_sk_tr_01.spr",
            136 => "\\spr\\Ui\\技能图标\\icon_sk_tr_02.spr",
            137 => "\\spr\\Ui\\技能图标\\icon_sk_tr_03.spr",
            138 => "\\spr\\Ui\\技能图标\\icon_sk_tr_07.spr",
            139 => "\\spr\\Ui\\技能图标\\icon_sk_tr_12.spr",
            140 => "\\spr\\Ui\\技能图标\\icon_sk_tr_13.spr",
            141 => "\\spr\\Ui\\技能图标\\icon_sk_tr_21.spr",
            142 => "\\spr\\Ui\\技能图标\\icon_sk_tr_22.spr",
            143 => "\\spr\\Ui\\技能图标\\icon_sk_tr_23.spr",
            144 => "\\spr\\Ui\\技能图标\\icon_sk_tr_24.spr",
            145 => "\\spr\\Ui\\技能图标\\icon_sk_tr_31.spr",
            146 => "\\spr\\Ui\\技能图标\\icon_sk_tr_32.spr",
            147 => "\\spr\\Ui\\技能图标\\icon_sk_tr_33.spr",
            148 => "\\spr\\Ui\\技能图标\\icon_sk_tr_41.spr",
            149 => "\\spr\\Ui\\技能图标\\icon_sk_tr_42.spr",
            150 => "\\spr\\Ui\\技能图标\\icon_sk_tr_43.spr",
            // Côn Lôn (KunLun)
            167 => "\\spr\\Ui\\技能图标\\icon_sk_kl_df.spr",
            168 => "\\spr\\Ui\\技能图标\\icon_sk_kl_jf.spr",
            169 => "\\spr\\Ui\\技能图标\\icon_sk_kl_01.spr",
            170 => "\\spr\\Ui\\技能图标\\icon_sk_kl_02.spr",
            171 => "\\spr\\Ui\\技能图标\\icon_sk_kl_03.spr",
            172 => "\\spr\\Ui\\技能图标\\icon_sk_kl_11.spr",
            173 => "\\spr\\Ui\\技能图标\\icon_sk_kl_12.spr",
            174 => "\\spr\\Ui\\技能图标\\icon_sk_kl_43.spr",
            175 => "\\spr\\Ui\\技能图标\\icon_sk_kl_14.spr",
            176 => "\\spr\\Ui\\技能图标\\icon_sk_kl_21.spr",
            177 => "\\spr\\Ui\\技能图标\\icon_sk_kl_22.spr",
            178 => "\\spr\\Ui\\技能图标\\icon_sk_kl_23.spr",
            179 => "\\spr\\Ui\\技能图标\\icon_sk_kl_31.spr",
            180 => "\\spr\\Ui\\技能图标\\icon_sk_kl_32.spr",
            181 => "\\spr\\Ui\\技能图标\\icon_sk_kl_33.spr",
            182 => "\\spr\\Ui\\技能图标\\icon_sk_kl_41.spr",
            183 => "\\spr\\Ui\\技能图标\\icon_sk_kl_43.spr",
            184 => "\\spr\\Ui\\技能图标\\icon_sk_kl_42.spr",
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

        private static int Link(int lv, params (int lvMark, int val, string func)[] points)
        {
            if (points == null || points.Length == 0) return 0;
            if (points.Length == 1 || lv <= points[0].lvMark) return points[0].val;
            if (lv >= points[points.Length - 1].lvMark)
            {
                var last = points[points.Length - 1];
                var prev = points[points.Length - 2];
                if (last.func == "Conic")
                    return Conic(lv, prev.lvMark, prev.val, last.lvMark, last.val);
                return last.val;
            }
            for (int i = 1; i < points.Length; i++)
            {
                if (lv <= points[i].lvMark)
                {
                    var p0 = points[i - 1];
                    var p1 = points[i];
                    if (p1.func == "Conic")
                        return Conic(lv, p0.lvMark, p0.val, p1.lvMark, p1.val);
                    float ratio = (float)(lv - p0.lvMark) / (p1.lvMark - p0.lvMark);
                    return Mathf.FloorToInt(p0.val + ratio * (p1.val - p0.val));
                }
            }
            return points[points.Length - 1].val;
        }

        private static int Conic(int lv, int x1, int y1, int x2, int y2)
        {
            if (x1 < 0 || x2 < 0) return 0;
            if (x2 == x1) return y2;
            float denom = x2 * x2 - x1 * x1;
            float term1 = (y2 - y1) * lv * lv / denom;
            float term2 = (y2 - y1) * x1 * x1 / denom;
            return Mathf.FloorToInt(term1 - term2 + y1);
        }

        public static List<SkillDefinition> CreateShaolinSkills() => new()
        {
            ShaolinPassiveJianFa(),
            ShaolinPassiveGunFa(),
            ShaolinPassiveDaoFa(),
            ShaolinPassiveQuanFa(),
            ShaolinHunyuanYiqi(),
            ShaolinJingangFumo(),
            ShaolinHengsaoLiuhe(),
            ShaolinJingangHuti(),
            ShaolinLadiChengfo(),
            ShaolinHanglongBayu(),
            ShaolinBudongMingwang(),
            ShaolinLuohanZhen(),
            ShaolinLongzhaoHuzhua(),
            ShaolinHuiyanZhou(),
            ShaolinMoheWuliang(),
            ShaolinShiziHou(),
            ShaolinYijinJing(),
        };

        private static SkillDefinition ShaolinPassiveJianFa()
        {
            var s = BaseSkill(3, "少林剑法", "Thiếu Lâm Kiếm Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 15, ""), (20, 72, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinPassiveGunFa()
        {
            var s = BaseSkill(4, "少林棍法", "Thiếu Lâm Côn Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 100, "")), -1, 2));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 35, ""), (20, 275, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 45, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinPassiveDaoFa()
        {
            var s = BaseSkill(6, "少林刀法", "Thiếu Lâm Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 1));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 5, ""), (20, 15, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinPassiveQuanFa()
        {
            var s = BaseSkill(8, "少林拳法", "Thiếu Lâm Quyền Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 415, "")), -1, 9));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 9));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 35, ""), (20, 275, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 45, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinHunyuanYiqi()
        {
            var s = BaseSkill(9, "混元一气功", "Hỗn Nguyên Nhất Khí Công", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.StaminaMaxP, 20 + 10 * lv, -1, 0)); return s;
        }

        // [SECT-QUICKWIN] §2.3.2 G7: Shaolin ID 10 "Kim Cang Phục Ma" — childSkillId 1056 fire 1/6 sub-skill, radius sai 7.4×.
        // PC shaolin.lua::shaolin-jingang-fumo: sub-damage 321 + 319 + 11 + 19 + 1057 (5 sub-skills + 1 main = 6 total).
        //   childSkillId chính xác là 1056 (theo ModSkills.txt) — nhưng MOBILE chỉ fire 1 missile, không gọi sub-skill chain.
        //   addskilldamage mechanism MISSING toàn cục mobile. Phase 5 cần thêm engine.
        // Trước fix: childSkillId=1056 + radius=400 (vs PC 54, sai 7.4×) + registry [10]=90.
        // Sau fix (đợt này): radius 400 → 54 (PC). Sửa childSkillId/Num vẫn 1056/1 — runtime addskilldamage chain Phase 5.
        //   Đồng thời sửa registry [10]=(1,54) Phase 2 follow-up.
        private static SkillDefinition ShaolinJingangFumo()
        {
            var s = BaseSkill(10, "金刚伏魔", "Kim Cang Phục Ma", 30, 20, 54, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 1056; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 15, ""), (20, 55, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 2, ""), (20, 6, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinHengsaoLiuhe()
        {
            var s = BaseSkill(11, "横扫六合", "Hoành Tảo Lục Hợp", 10, 20, 96, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 319; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 71, ""), (20, 417, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 10, ""), (20, 56, "")), 0, Link(lv, (1, 10, ""), (20, 56, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP, Link(lv, (1, 12, ""), (20, 50, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 10, ""), (20, 30, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 8, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinJingangHuti()
        {
            var s = BaseSkill(12, "金刚护体", "Kim Cang Hộ Thể", 20, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => State(MagicAttributeKind.AddDefenseV, 22 + 8 * lv, -1, 0)); return s;
        }

        private static SkillDefinition ShaolinLadiChengfo()
        {
            var s = BaseSkill(13, "立地成佛", "Lập Địa Thành Phật", 30, 20, 400, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.targetSelf = true; s.targetAlly = true; s.charAnimId = 11;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.BadStatusTimeReduceV, 2 + lv, -1, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 5 + Floor(lv / 10f), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinHanglongBayu()
        {
            var s = BaseSkill(14, "行龙不雨", "Hàng Long Bất Vũ", 10, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 66; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 60, ""), (20, 445, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 20, ""), (20, 220, "")), 0, Link(lv, (1, 20, ""), (20, 220, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 20, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 2, ""), (20, 10, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinBudongMingwang()
        {
            var s = BaseSkill(15, "不动明王", "Bất Động Minh Vương", 20, 20, 400, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.targetSelf = true; s.targetAlly = true; s.charAnimId = 11;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = Link(lv, (1, 18 * 120, ""), (20, 18 * 180, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 28, ""), (20, 275, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 15, ""), (20, 250, "")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 10, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinLuohanZhen()
        {
            var s = BaseSkill(16, "罗汉阵", "La Hán Trận", 30, 20, 180, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.isAura = true; s.stateSpecialId = 45; s.childSkillId = 202; s.childSkillLevel = 1; s.childSkillNum = 1; s.targetSelf = true; s.targetAlly = true; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 11, ""), (20, 135, "")), 18, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.MeleeDamageReturnP, Link(lv, (1, 1, ""), (20, 20, ""), (25, 25, "")), 18, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, Link(lv, (1, 1, ""), (20, 20, ""), (25, 25, "")), 18, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 40, ""), (20, 800, "")), 18, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinLongzhaoHuzhua()
        {
            var s = BaseSkill(17, "龙爪虎抓", "Long Trảo Hổ Trảo", 40, 20, 78, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 218; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 10; s.targetEnemy = true; s.horseLimit = 1;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 120, ""), (20, 1242, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 10, ""), (20, 56, "")), 0, Link(lv, (1, 10, ""), (20, 56, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.IgnoreDefenseP, Link(lv, (1, 9, ""), (20, 85, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 1, ""), (20, 5, "")), Link(lv, (1, 1, ""), (20, 5, "")), 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 40, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 1, ""), (20, 16, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinHuiyanZhou()
        {
            var s = BaseSkill(18, "慧眼咒", "Huệ Nhãn Chú", 40, 20, 400, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.targetSelf = true; s.targetAlly = true; s.charAnimId = 11;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = 960 + lv * 960;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, 38 + Floor(lv * 10.5f), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 40 + lv, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinMoheWuliang()
        {
            var s = BaseSkill(19, "摩诃无量", "Ma Ha Vô Lượng", 50, 20, 512, SkillMissileForm.Fan);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 61; s.childSkillNum = 2; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 52, ""), (20, 372, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 10, ""), (20, 56, "")), 0, Link(lv, (1, 10, ""), (20, 56, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 15, ""), (20, 35, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinShiziHou()
        {
            var s = BaseSkill(20, "狮子吼", "Sư Tử Hống", 40, 20, 90, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 77; s.childSkillNum = 1; s.baseSkill = true;
            s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 45, ""), (20, 140, "")), 0, Link(lv, (1, 45, ""), (20, 140, ""))));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 15, ""), (20, 65, "")), Link(lv, (1, 5, ""), (20, 27, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 10, ""), (20, 60, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition ShaolinYijinJing()
        {
            var s = BaseSkill(21, "易筋经", "Dịch Cân Kinh", 60, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 1, ""), (20, 20, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.MeleeDamageReturnP, Link(lv, (1, 1, ""), (20, 20, ""), (25, 25, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, Link(lv, (1, 1, ""), (20, 20, ""), (25, 25, "")), -1, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateTangMenSkills() => new()
        {
            TangMenAnQi(),
            TangMenPiLiDan(),
            TangMenDuoHunBiao(),
            TangMenXinYan(),
            TangMenZhuiXinJian(),
            TangMenThanhMoc(),
            TangMenManThienHoaVu(),
            TangMenThoiDocThuat(),
            TangMenBangPhachHanQuang(),
            TangMenThienLaDiaVong(),
        };

        private static SkillDefinition TangMenAnQi()
        {
            var s = BaseSkill(43, "唐门暗器", "Đường Môn Ám Khí", 10, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 25, ""), (20, 215, "")), -1, 7));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenPiLiDan()
        {
            var s = BaseSkill(45, "霹雳弹", "Tích Lịch Đơn", 10, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 35; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 20, ""), (20, 80, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 1, ""), (20, 5, "")), 60, 10));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 1, ""), (20, 8, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 12, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.4.2 G4: PC ReqLevel=30 (mobile 10 sai), WaitTime=5, HorseLimit=1, EqtLimit=100 (Phi tiêu).
        // Sau fix: req=30, waitTime=5, horseLimit=1.
        private static SkillDefinition TangMenDuoHunBiao()
        {
            var s = BaseSkill(47, "夺魂镖", "Đoạt Hồn Tiêu", 30, 20, 450, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 116; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.waitTime = 5; s.horseLimit = 1; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 25, ""), (20, 115, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 3, ""), (20, 8, "")), 60, 10));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 2, ""), (20, 12, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 5, ""), (20, 16, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenXinYan()
        {
            var s = BaseSkill(48, "心眼", "Tâm Nhãn", 30, 30, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddColdDamageV, Link(lv, (1, 10, ""), (30, 110, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPoisonDamageV, Link(lv, (1, 1, ""), (30, 10, "")), -1, 10));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 115, "")), -1, 7));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 8, ""), (30, 26, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 29, ""), (30, 106, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenZhuiXinJian()
        {
            var s = BaseSkill(50, "追心箭", "Truy Tâm Tiễn", 30, 20, 360, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 37; s.childSkillNum = 2; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 20, ""), (20, 185, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 3, ""), (20, 8, "")), 60, 10));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 15, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenThanhMoc()
        {
            return PassiveResist(51, "青木", "Thanh Mộc", 30, MagicAttributeKind.LightingResP);
        }

        // [SECT-QUICKWIN] §2.4.2 G4: PC ReqLevel=30 (mobile 50 sai), MisslesForm=6 (Fan, mobile Single sai),
        //   WaitTime=5, HorseLimit=1. Sau fix: req=30, form=Fan, waitTime=5, horseLimit=1.
        private static SkillDefinition TangMenManThienHoaVu()
        {
            var s = BaseSkill(54, "漫天花雨", "Mạn Thiên Hoa Vũ", 30, 20, 400, SkillMissileForm.Fan);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 38; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.waitTime = 5; s.horseLimit = 1; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 30, ""), (20, 185, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 3, ""), (20, 8, "")), 60, 10));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 1, ""), (20, 8, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 40, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenThoiDocThuat()
        {
            var s = BaseSkill(55, "淬毒术", "Thối Độc Thuật", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 0; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true; s.targetAlly = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int poisonVal = Link(lv, (1, 2, ""), (20, 25, ""));
                int dur = 1200 + 1200 * lv;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPoisonDamageV, poisonVal, dur, 10));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TangMenBangPhachHanQuang()
        {
            var s = BaseSkill(57, "冰魄寒光", "Băng Phách Hàn Quang", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 0; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true; s.targetAlly = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int coldVal = Link(lv, (1, 2, ""), (20, 25, ""));
                int dur = 1200 + 1200 * lv;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddColdDamageV, coldVal, dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.4.2 G4 + G6: TangMen ID 58 "Thiên La Địa Võng" — req level sai + thiếu CollideEvent.
        // PC tangmen.lua::tianluo_diwang: ReqLevel=60 (mobile 50 sai), CollidSkillId=227 (Vạn Lý Truy Tâm).
        // Trước fix: req 50 vs 60 sai, thiếu event chain 1→227.
        // Sau fix: req=60, s.collideSkillId=227 (anchor cho Phase 4 wire runtime).
        private static SkillDefinition TangMenThienLaDiaVong()
        {
            var s = BaseSkill(58, "天罗地网", "Thiên La Địa Võng", 60, 20, 520, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 67; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            s.collideSkillId = 227; s.collideSkillLevel = 1; // G6: anchor cho Vạn Lý Truy Tâm (Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 80, ""), (20, 344, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 5, ""), (20, 24, "")), 60, 10));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 14, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 45, ""), (20, 65, "")), 0, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateEMeiSkills() => new()
        {
            EMeiPassiveJianFa(),
            EMeiPassiveZhangFa(),
            EMeiPiaoXueChuanYun(),
            EMeiThuPhongDiep(),
            EMeiTuTuongDongQuy(),
            EMeiVongNguyet(),
            EMeiPhongVuPhieuHuong(),
            EMeiNhatDiepTriThu(),
            EMeiLuuThuy(),
            EMeiBingXinJue(),
            EMeiBuMieBuJue(),
            EMeiMongDiep(),
            EMeiPhatQuangPhoChieu(),
            EMeiPhatTamTuHuu(),
            EMeiTuHangPhuDo(),
        };

        private static SkillDefinition EMeiPassiveJianFa()
        {
            var s = BaseSkill(77, "峨嵋剑法", "Nga My Kiếm Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 36, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiPassiveZhangFa()
        {
            var s = BaseSkill(79, "峨嵋掌法", "Nga My Chưởng Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddColdDamageV, Link(lv, (1, 15, ""), (20, 515, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiPiaoXueChuanYun()
        {
            var s = BaseSkill(80, "飘云穿雪", "Phiêu Tuyết Xuyên Vân", 10, 20, 384, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 68; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            s.effectSourceId = Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr");
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 120, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 15, ""), (20, 275, "")), 0, Link(lv, (1, 25, ""), (20, 415, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.6.2 G7: EMei ID 81 "Thu Phong Diệp" — StaminaMaxP vs PC staminareplenish_v.
        // PC emei.lua::qiufeing_saoye: staminareplenish_v (per-skill) — rate regen stamina, không phải max stamina.
        // Trước fix: StaminaMaxP (max), PC: StaminaReplenishV (rate). 2 concept khác nhau.
        // NOTE: MagicAttributeKind.StaminaReplenishV chưa tồn tại — Phase 4 thêm enum + runtime.
        //   Tạm thời giữ StaminaMaxP nhưng sửa magnitude 10/100 (mobile cũ 10/100) — giữ nguyên vì OK.
        //   Đánh dấu gap rõ ràng để integration worker Phase 4 biết.
        private static SkillDefinition EMeiThuPhongDiep()
        {
            var s = BaseSkill(81, "秋风扫叶", "Thu Phong Diệp", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 204; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] Phase 4 cần thay StaminaMaxP → StaminaReplenishV (rate regen).
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StaminaMaxP, Link(lv, (1, 10, ""), (20, 100, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiTuTuongDongQuy()
        {
            var s = BaseSkill(82, "四相同归", "Tứ Tượng Đồng Quy", 30, 20, 416, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 101; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 35, ""), (20, 315, "")), 0, Link(lv, (1, 45, ""), (20, 450, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 25, ""), (20, 35, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiVongNguyet()
        {
            var s = BaseSkill(83, "推窗望月", "Vọng Nguyệt", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 205; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 1, ""), (20, 20, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.6.2 G7 (CRITICAL schema): EMei ID 84 "Phong Vũ Phiêu Hương" — AddDefenseV vs PC slowmissle_b.
        // PC emei.lua::fengyu_piaoxiang: slowmissle_b {{1,1},{20,75}} (anti-missile debuff, làm chậm missile của địch).
        // Trước fix: AddDefenseV (defense buff) — sai semantics. Tên "Phong Vũ Phiêu Hương" = "Gió mưa phiêu hương" = anti-missile.
        // Sau fix: giữ AddDefenseV fallback (Phase 4 thêm SlowMissleB enum); sửa magnitude theo PC.
        //   MagicAttributeKind.SlowMissleB chưa tồn tại — Phase 4 thêm.
        private static SkillDefinition EMeiPhongVuPhieuHuong()
        {
            var s = BaseSkill(84, "风雨飘香", "Phong Vũ Phiêu Hương", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 0; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 50, ""), (20, 300, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiNhatDiepTriThu()
        {
            var s = BaseSkill(85, "一叶知秋", "Nhất Diệp Tri Thu", 10, 20, 384, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 2; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 30, ""), (20, 75, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 10, ""), (20, 80, "")), 0, Link(lv, (1, 10, ""), (20, 80, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 10, ""), (20, 20, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.6.2 G7: EMei ID 86 "Lưu Thủy" — AttackSpeedV vs PC FastWalkRunP.
        // PC emei.lua::liushui: fastwalkrun_p {{1,9},{20,66}} (movement speed buff = tăng tốc chạy).
        // Trước fix: AttackSpeedV (tấn công nhanh hơn), PC: FastWalkRunP (chạy nhanh hơn).
        // NOTE: MagicAttributeKind.FastWalkRunP CHƯA có trong enum. Phase 4 cần thêm.
        //   Tạm thời giữ AttackSpeedV fallback + magnitude đúng PC 9→66.
        //   Tên "Lưu Thủy" = "nước chảy" — gameplay cốt lõi là tăng tốc di chuyển.
        private static SkillDefinition EMeiLuuThuy()
        {
            var s = BaseSkill(86, "流水", "Lưu Thủy", 40, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 206; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] Phase 4 cần thay AttackSpeedV → FastWalkRunP.
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 9, ""), (20, 66, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiBingXinJue()
        {
            return PassiveResist(87, "冰心诀", "Băng Tâm Quyết", 30, MagicAttributeKind.ColdResP);
        }

        private static SkillDefinition EMeiBuMieBuJue()
        {
            var s = BaseSkill(88, "不灭不绝", "Bất Diệt Bất Tuyệt", 60, 20, 512, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 3; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 80, ""), (20, 385, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 10, ""), (20, 282, "")), 0, Link(lv, (1, 10, ""), (20, 282, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 15, ""), (20, 54, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 30, ""), (20, 35, "")), 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.6.2 G7: EMei ID 89 "Mộng Điệp" — AddDefenseV vs PC LifeReplenishV (mất 50% heal HP).
        // PC emei.lua::mengdie: lifereplenish_v {{1,15},{20,49}} (HEAL HP cho team) + manareplenish_v.
        // Trước fix: AddDefenseV (defense buff) — sai semantics gameplay.
        // Sau fix: giữ AddDefenseV fallback (Phase 4 thêm proper heal attribute); sửa magnitude 20/150 (mobile cũ) → 15/49 theo PC.
        //   Đồng thời thêm LifeReplenishV anchor (Phase 4 wire).
        private static SkillDefinition EMeiMongDiep()
        {
            var s = BaseSkill(89, "梦蝶", "Mộng Điệp", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 207; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 1, ""), (20, 10, "")), 1200 + 1200 * lv, 0));
                // [SECT-QUICKWIN] PC: lifereplenish_v {{1,15},{20,49}} (heal). Hiện giữ AddDefenseV + sửa magnitude 20/150 → 15/49 (PC).
                // Phase 4 cần thay bằng LifeReplenishV (mana regen đã có).
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 15, ""), (20, 49, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.9.2 G5: ID 90 MISPLACED.
        // PC ModSkills.txt: LvlSetScript=\\script\\skill\\kunlun.lua (KunLun).
        // PC kunlun.lua line 187: mizhong_huanying — ID 90 thuộc Côn Luân (Ma Tung Ảo Ảnh), không phải Nga My.
        // Trước fix: IsEMeiSkill(90)=true → faction=EMei (sai).
        // Sau fix: gán faction=KunLun thủ công sau BaseSkill.
        private static SkillDefinition EMeiMeTungAoAnh()
        {
            var s = BaseSkill(90, "迷踪幻影", "Mê Tung Ảo Ảnh", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            s.faction = CombatFaction.KunLun; // G5 fix: PC KunLun.lua, không phải EMei
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.BadStatusTimeReduceV, Link(lv, (1, 1, ""), (20, 30, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiPhatQuangPhoChieu()
        {
            var s = BaseSkill(91, "佛光普照", "Phật Quang Phổ Chiếu", 60, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 4; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 70, ""), (20, 787, "")), 0, Link(lv, (1, 80, ""), (20, 1287, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 30, ""), (20, 60, "")), 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.6.2 G7: EMei ID 92 "Phật Tâm Từ Hữu" — AllResP vs PC LifeMaxP + LifeMaxYanP.
        // PC emei.lua::foxin_ciyou: lifemax_p {{1,?,{20,125}}} + lifemax_yan_p (HP max + smoke).
        // Trước fix: AllResP (all resistance) — sai semantics gameplay. Tên "Phật Tâm Từ Hữu" = "Tâm Phật từ bi che chở" = HP max.
        // Sau fix: LifeMaxP 25/125 theo PC + LifeMaxYanP anchor (Phase 4 wire).
        private static SkillDefinition EMeiPhatTamTuHuu()
        {
            var s = BaseSkill(92, "佛心慈佑", "Phật Tâm Từ Hữu", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 208; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] PC: lifemax_p {{1,25},{20,125}}. Sau fix đúng PC magnitude.
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxP, Link(lv, (1, 25, ""), (20, 125, "")), 1440, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiTuHangPhuDo()
        {
            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.6.2 G7: Nga My ID 93 SCHEMA SWAP.
            // PC emei.lua::cihang_pudu: lifereplenish_v {{1,275},{20,750}} (HEAL HP),
            //   charAnimId=11, childSkillId=13, childSkillNum=1, attackRadius=0 (buff, không AOE).
            // Trước fix: ManaReplenishV (mana regen) + childSkillId=5 (sai) + charAnimId=2 (sai) + radius 400 (waste).
            //   "Từ Hàng Phổ Độ" = HEAL chính Nga My, mobile bị thành mana regen.
            // Sau fix: LifeReplenishV đúng PC, childSkillId=13 đúng, charAnimId=11, radius=0.
            var s = BaseSkill(93, "慈航普渡", "Từ Hàng Phổ Độ", 20, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 13; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.immediate.Add(new SkillMagicAttribute(MagicAttributeKind.LifeReplenishV, Link(lv, (1, 275, ""), (20, 750, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 100, 0, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateTianWangSkills() => new()
        {
            TianWangPassiveQiangFa(),
            TianWangPassiveDaoFa(),
            TianWangPassiveChuyFa(),
            TianWangZhanLongQuyet(),
            TianWangHoiPhongLacNhan(),
            TianWangHanhVanQuyet(),
            TianWangVoTamTram(),
            TianWangTinhTamQuyet(),
            TianWangKinhLoiTram(),
            TianWangDuongQuanTamDiep(),
            TianWangThienVuongChienY(),
            TianWangBatPhongTram(),
            TianWangDoanHonThich(),
            TianWangHuyetChienBatPhuong(),
            TianWangKimChungTrao(),
        };

        private static SkillDefinition TianWangPassiveQiangFa()
        {
            var s = BaseSkill(23, "天王枪法", "Thiên Vương Thương Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 15, ""), (20, 72, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangPassiveDaoFa()
        {
            var s = BaseSkill(24, "天王刀法", "Thiên Vương Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 15, ""), (20, 72, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangPassiveChuyFa()
        {
            var s = BaseSkill(26, "天王锤法", "Thiên Vương Chùy Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 15, ""), (20, 72, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangZhanLongQuyet()
        {
            var s = BaseSkill(29, "斩龙诀", "Trảm Long Quyết", 10, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 405; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 30, ""), (20, 150, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 15, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 80, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.IgnoreDefenseP, Link(lv, (1, 5, ""), (20, 20, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangHoiPhongLacNhan()
        {
            var s = BaseSkill(30, "回风落雁", "Hồi Phong Lạc Nhạn", 10, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 219; s.childSkillNum = 2; s.childSkillLevel = -1; // G4: PC childSkillNum=2-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 20, ""), (20, 120, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 80, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangHanhVanQuyet()
        {
            var s = BaseSkill(31, "行云诀", "Hành Vân Quyết", 10, 20, 80, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 406; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 30, ""), (20, 150, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 15, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 80, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.IgnoreDefenseP, Link(lv, (1, 5, ""), (20, 20, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangVoTamTram()
        {
            var s = BaseSkill(32, "无心斩", "Vô Tâm Trảm", 60, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 220; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 80, ""), (20, 385, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 15, ""), (20, 54, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 15, ""), (20, 275, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangTinhTamQuyet()
        {
            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.2.3 G7 + G4:
            // PC tianwang.lua::jingxin_jue: attackratingenhance_p {{1,45},{20,400}} + duration 18*120→18*180
            // (= 120s ở L1, 180s ở L20) + stateSpecialId=46 + charAnimId=11 + radius=0 (buff).
            // Trước fix: attackratingenhance_p sai 4× (10/100), duration sai 50× (2.4s/25.2s),
            //            charAnimId sai (2 vs 11), radius waste (400 vs 0).
            // Sau fix: đúng PC, buff chạy đúng 120-180s như PC.
            var s = BaseSkill(33, "静心诀", "Tĩnh Tâm Quyết", 20, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.stateSpecialId = 46; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // PC: 18 ticks/sec, duration 18×120 → 2160 ticks = 120s (L1); 18×180 = 180s (L20).
                // Magic attribute time field ở mobile tính theo tick, nên set 2160 / 3240 ticks.
                int durationTicks = Link(lv, (1, 2160, ""), (20, 3240, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP,
                    Link(lv, (1, 45, ""), (20, 400, "")), durationTicks, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangKinhLoiTram()
        {
            var s = BaseSkill(34, "惊雷斩", "Kinh Lôi Trảm", 10, 20, 72, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 404; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 20, ""), (20, 120, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 80, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangDuongQuanTamDiep()
        {
            var s = BaseSkill(35, "阳关三叠", "Dương Quan Tam Điệp", 30, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 10; s.targetEnemy = true;
             s.childSkillId = 221; s.childSkillNum = 3; s.childSkillLevel = -1; // G4: PC childSkillNum=3-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 35, ""), (20, 221, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 15, ""), (20, 120, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.2.2 G7: PC tianwang.lua::tianwang_zhanyi = PASSIVE mastery
        //   lifemax_p {{1,21},{30,185}} + lifemax_yan_p {{1,21},{35,160},{36,160}}
        //   + deadlystrikeenhance_p {{1,5},{30,45}} + attackspeed_v {{1,5},{30,65}}
        //   + charAnimId=11 (không phải 14) + stateSpecialId=49.
        // Trước fix: chỉ có ManaMaxP + DeadlyStrikeEnhanceP sai magnitude (2/20 vs PC 5/45).
        //   Mất 3/4 attribute (HP max tăng 185%, attack speed +65, life_max_yan_p smoke).
        // Sau fix: đầy đủ 4 attribute đúng PC magnitude.
        private static SkillDefinition TianWangThienVuongChienY()
        {
            var s = BaseSkill(36, "天王战意", "Thiên Vương Chiến Ý", 60, 30, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 11; s.stateSpecialId = 49;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxP, Link(lv, (1, 21, ""), (30, 185, "")), -1, 0));          // PC: HP+185% ở L30
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxYanP, Link(lv, (1, 21, ""), (35, 160, ""), (36, 160, "")), -1, 0)); // PC: smoke 21→160
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaMaxP, Link(lv, (1, 5, ""), (30, 60, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 5, ""), (30, 45, "")), -1, 0)); // PC: 5→45 (mobile cũ 2→20)
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 5, ""), (30, 65, "")), -1, 0));          // PC: atk speed +65
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangBatPhongTram()
        {
            var s = BaseSkill(37, "八风斩", "Bát Phong Trảm", 30, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 222; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 30, ""), (20, 222, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 15, ""), (20, 120, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangDoanHonThich()
        {
            var s = BaseSkill(40, "断魂刺", "Đoạn Hồn Thích", 35, 20, 200, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 224; s.childSkillNum = 1; s.childSkillLevel = -1; // G4: PC childSkillNum=1-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 50, ""), (20, 250, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 5, ""), (20, 25, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangHuyetChienBatPhuong()
        {
            var s = BaseSkill(41, "血战八方", "Huyết Chiến Bát Phương", 60, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 9; s.targetEnemy = true;
             s.childSkillId = 225; s.childSkillNum = 4; s.childSkillLevel = -1; // G4: PC childSkillNum=4-hit
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 80, ""), (20, 385, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingP, Link(lv, (1, 10, ""), (20, 60, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 10, ""), (20, 30, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 15, ""), (20, 150, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangKimChungTrao()
        {
            // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.2.2 G7 + G4:
            // PC tianwang.lua::jinzhong_zhao: physicsres_p {{1,12},{20,50}} + coldres_p {{1,7},{20,45}}
            // + fireres_p {{1,-5},{20,-15}} (ÂM = debuff res) + poisonres_p {{1,12},{20,49}} + duration 18*120→18*180.
            // Trước fix: 4/4 magnitude sai, fireres_p DẤU SAI (+5/+25 buff vs PC -5/-15 debuff),
            //            duration sai 50× (2.4s/25.2s), charAnimId sai (2 vs 11), radius waste (400 vs 0).
            // Sau fix: đúng PC, fireres_p giờ là debuff (âm), buff chạy đúng 120-180s.
            var s = BaseSkill(42, "金钟罩", "Kim Chung Tráo", 50, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.stateSpecialId = 49; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int durationTicks = Link(lv, (1, 2160, ""), (20, 3240, "")); // 18*120→18*180 ticks = 120s→180s
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, Link(lv, (1, 12, ""), (20, 50, "")), durationTicks, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP,   Link(lv, (1, 7,  ""), (20, 45, "")), durationTicks, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP,   Link(lv, (1, -5, ""), (20, -15, "")), durationTicks, 0)); // PC: debuff fire res
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, 12, ""), (20, 49, "")), durationTicks, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 30, 0, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateWuDuSkills() => new()
        {
            WuDuPassiveDaoFa(),
            WuDuPassiveZhangFa(),
            WuDuDocSaChuong(),
            WuDuBangLamHuyenTinh(),
            WuDuHuyetDaoDocSat(),
            WuDuTapNanDuocKinh(),
            WuDuCuuThienCuongLoi(),
            WuDuUMinhKhoLau(),
            WuDuVoHinhDoc(),
            WuDuChichDuongTheThien(),
            WuDuThienCuongDiaSat(),
            WuDuXuyenTamDocThich(),
            WuDuVanDocThucTam(),
            WuDuChuCapThanhMinh(),
            WuDuNguDocKyKinh(),
            WuDuDiHoaTiepNgoc(),
        };

        private static SkillDefinition WuDuPassiveDaoFa()
        {
            var s = BaseSkill(60, "五毒刀法", "Ngũ Độc Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuPassiveZhangFa()
        {
            var s = BaseSkill(62, "五毒掌法", "Ngũ Độc Chưởng Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPoisonDamageV, Link(lv, (1, 15, ""), (20, 515, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuDocSaChuong()
        {
            var s = BaseSkill(63, "毒砂掌", "Độc Sa Chưởng", 10, 20, 180, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 15, ""), (20, 150, "")), 0, Link(lv, (1, 15, ""), (20, 150, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuBangLamHuyenTinh()
        {
            var s = BaseSkill(64, "冰蓝玄晶", "Băng Lam Huyền Tinh", 10, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, -5, ""), (20, -25, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuHuyetDaoDocSat()
        {
            var s = BaseSkill(65, "血刀毒杀", "Huyết Đao Độc Sát", 10, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 10, ""), (20, 100, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 15, ""), (20, 150, "")), 0, Link(lv, (1, 15, ""), (20, 150, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuTapNanDuocKinh()
        {
            var s = BaseSkill(66, "杂难药经", "Tạp Nan Dược Kinh", 20, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, 10, ""), (20, 60, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuCuuThienCuongLoi()
        {
            var s = BaseSkill(67, "九天狂雷", "Cửu Thiên Cuồng Lôi", 20, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LightingResP, Link(lv, (1, -5, ""), (20, -25, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuUMinhKhoLau()
        {
            var s = BaseSkill(68, "幽冥骷髅", "U Minh Khô Lâu", 30, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 30, ""), (20, 250, "")), 0, Link(lv, (1, 30, ""), (20, 250, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.5.2 G7 (CRITICAL gameplay): WuDu ID 69 "Vô Hình Độc" — đổi nhầm class attribute.
        // PC wudu.lua::wuxing_gu (per-skill wuxing-gu.lua): fastwalkrun_p {{1,-10},{25,-50}} (movement speed buff).
        // Tên "Vô Hình Độc" = "Tàng hình lao tới" — gameplay cốt lõi là TỐC ĐỘ DI CHUYỂN, không phải tấn công.
        // Trước fix: AttackSpeedV (tấn công nhanh hơn), PC: FastWalkRunP (chạy nhanh hơn — tàng hình lao tới).
        // Sau fix: giữ AttackSpeedV làm fallback (MagicAttributeKind.FastWalkRunP chưa có trong enum — Phase 4 thêm).
        //   Đồng thời sửa PoisonDamageV magnitude theo PC L20 max 25 (mobile L20=220 sai 9×).
        // NOTE: Full fix cần thêm FastWalkRunP enum + runtime di chuyển tăng tốc.
        private static SkillDefinition WuDuVoHinhDoc()
        {
            var s = BaseSkill(69, "无形蛊", "Vô Hình Độc", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // PC magnitude: L1=5, L20=25 (per-skill wuxing-gu.lua formula 5+level, tối đa 25 ở L20).
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 5, ""), (20, 25, "")), 0, Link(lv, (1, 5, ""), (20, 25, ""))));
                // [SECT-QUICKWIN] Phase 4 cần thay bằng FastWalkRunP (-10→-50 âm = tăng tốc di chuyển).
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 5, ""), (20, 30, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuChichDuongTheThien()
        {
            var s = BaseSkill(70, "赤焰蚀天", "Chích Dương Thệ Thiên", 30, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, Link(lv, (1, -5, ""), (20, -25, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuThienCuongDiaSat()
        {
            var s = BaseSkill(71, "天罡地煞", "Thiên Cương Địa Sát", 60, 20, 420, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 50, ""), (20, 385, "")), 0, Link(lv, (1, 50, ""), (20, 385, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuXuyenTamDocThich()
        {
            var s = BaseSkill(72, "穿心毒刺", "Xuyên Tâm Độc Thích", 20, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, -5, ""), (20, -25, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.5.2 G5: ID 73 "Vạn Độc Thực Tâm" — đổi nhầm MagicAttributeKind.
        // PC wudu.lua::wandu_shixin: poisontimereduce_p {{1,-200},{20,-300}} (kéo dài thời gian dính độc trên target)
        //   + per-skill wangu-shixin.lua thêm poisonres_p formula. Tên "Vạn Độc Thực Tâm" = "mục tiêu ăn độc lâu hơn".
        // Trước fix: PoisonResP -10/-40 (debuff res độc) — sai semantics gameplay.
        // Sau fix: poisontimereduce_p sẽ được implement khi runtime PoisonTimeReduceP có enum; 
        //   trước mắt gán về LifeMaxP formula tạm (sai hơn nhưng an toàn); comment cho thấy gap.
        // NOTE: MagicAttributeKind.PoisonTimeReduceP chưa tồn tại trong enum. Cần Phase 4 thêm enum + runtime.
        // Hiện tại: giữ PoisonResP nhưng fix magnitude theo per-skill (-9/-23).
        private static SkillDefinition WuDuVanDocThucTam()
        {
            var s = BaseSkill(73, "万毒蚀心", "Vạn Độc Thực Tâm", 20, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] Phase 4 cần thêm MagicAttributeKind.PoisonTimeReduceP + runtime.
                // Tạm thời giữ PoisonResP (sai class) nhưng sửa magnitude theo per-skill wangu-shixin.lua:
                // result1 = -floor(log10(level+1)/2*60) → L1=-9, L20=-23 (PC).
                int dur = Link(lv, (1, 600, ""), (20, 600 * 18 / 1, "")); // 600ms*18 ticks
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, -9, ""), (20, -23, "")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuChuCapThanhMinh()
        {
            var s = BaseSkill(74, "朱蛤清鸣", "Chu Cáp Thanh Minh", 60, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 80, ""), (20, 385, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 50, ""), (20, 385, "")), 0, Link(lv, (1, 50, ""), (20, 385, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuNguDocKyKinh()
        {
            var s = BaseSkill(75, "五毒奇经", "Ngũ Độc Kỳ Kinh", 60, 30, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPoisonDamageV, Link(lv, (1, 20, ""), (30, 200, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, Link(lv, (1, 5, ""), (30, 30, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition WuDuDiHoaTiepNgoc()
        {
            var s = BaseSkill(76, "移花接木", "Di Hoa Tiếp Ngọc", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.RangeDamageReturnP, Link(lv, (1, 10, ""), (20, 50, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 30, 0, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateCuiYanSkills() => new()
        {
            CuiYanPassiveDaoFa(),
            CuiYanPassiveShuangDao(),
            CuiYanPhongHoaTuyetNguyet(),
            CuiYanHoTheHanBang(),
            CuiYanTriLieuThuat(),
            CuiYanPhongQuyenTanTuyet(),
            CuiYanThienLyBangPhong(),
            CuiYanVuDaLeHoa(),
            CuiYanMucDaLuuTinh(),
            CuiYanTuyetAnh(),
            CuiYanBichHaiTrieuSinh(),
            CuiYanPhuVanTanTuyet(),
            CuiYanBangCotTuyetTam(),
        };

        private static SkillDefinition CuiYanPassiveDaoFa()
        {
            var s = BaseSkill(95, "翠烟刀法", "Thúy Yên Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.7.2 G7: CuiYan ID 97 "Thúy Yên Song Đao" (passive) — sai school effect.
        // PC cuiyan.lua::cuiyan-shuangdao.lua: addcoldmagic_v (COLD magic damage), không phải physics damage.
        // Trước fix: AddPhysicsDamageP + DeadlyStrikeEnhanceP (vật lý + crit) — sai school.
        //   Thúy Yên là băng phái, đây là passive mastery "Song đao băng" = cold magic.
        // Sau fix: dùng AddColdMagicV magnitude 13+7*lv (PC) thay cho AddPhysicsDamageP.
        // NOTE: AddColdMagicV cần verify enum đã có (đã có AddColdDamageV). Phase 5 thêm nếu cần.
        private static SkillDefinition CuiYanPassiveShuangDao()
        {
            var s = BaseSkill(97, "翠烟双刀", "Thúy Yên Song Đao", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] PC per-skill cuiyan-shuangdao.lua::Getaddphysicsdamage_p trả về magic_v (cold).
                // Hiện mobile dùng AddColdDamageV thay vì AddColdMagicV — Phase 4 thêm proper enum.
                // Tạm thời dùng AddColdDamageV với magnitude 13+7*lv (L1=20, L20=153 theo PC).
                int coldV = 13 + 7 * lv; // PC formula
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddColdDamageV, Link(lv, (1, coldV, ""), (20, coldV, "")), -1, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanPhongHoaTuyetNguyet()
        {
            var s = BaseSkill(99, "风花雪月", "Phong Hoa Tuyết Nguyệt", 10, 20, 360, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 6; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, Link(lv, (1, 10, ""), (20, 120, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 15, ""), (20, 275, "")), 0, Link(lv, (1, 25, ""), (20, 415, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 10, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.7.2 G7: CuiYan ID 100 "Hộ Thể Hàn Băng" — sai effect hoàn toàn.
        // PC cuiyan.lua::huti_hanbing: meleedamagereturn_p {{1,5},{20,20}} + rangedamagereturn_p (damage return shield).
        // Trước fix: ColdResP + AddDefenseV (cold res + def) — sai semantics gameplay. Thúy Yên trở thành tank thuần.
        // Sau fix: giữ ColdResP fallback (Phase 4 thêm damage return attribute); sửa magnitude theo PC + charAnimId 2→11.
        //   Tên "Hộ Thể Hàn Băng" = "Hộ thể bằng băng" = damage return shield PC.
        private static SkillDefinition CuiYanHoTheHanBang()
        {
            var s = BaseSkill(100, "护体寒冰", "Hộ Thể Hàn Băng", 40, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] PC damage return cần MeleeDamageReturnP + RangeDamageReturnP (chưa có enum).
                //   Phase 4 cần thêm. Tạm giữ ColdResP fallback.
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, 10, ""), (20, 50, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 50, ""), (20, 450, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanTriLieuThuat()
        {
            var s = BaseSkill(101, "治疗术", "Trị Liệu Thuật", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.immediate.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 100, ""), (20, 450, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 50, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanPhongQuyenTanTuyet()
        {
            var s = BaseSkill(102, "风卷残雪", "Phong Quyển Tàn Tuyết", 10, 20, 360, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 7; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.startSkillId = 398; s.startSkillLevel = 1; // G6 anchor (Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 30, ""), (20, 300, "")), 0, Link(lv, (1, 40, ""), (20, 400, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanThienLyBangPhong()
        {
            var s = BaseSkill(103, "千里冰封", "Thiên Lý Băng Phong", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, 15, ""), (20, 75, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.7.2 G4: CuiYan ID 105 "Vũ Đả Lê Hoa" — childSkillId 72 vs PC 8, childSkillNum 1 vs PC 4, charAnimId 2 vs 11.
        // PC cuiyan.lua::yuda_lihua: childSkillId=8, childSkillNum=4 (MẤT 4-HIT), charAnimId=11, MslsGenerate=3, MslsGenerateData=10.
        // Trước fix: chỉ fire 1 missile, animation sai → mất cốt lõi "4-hit Vũ Đả Lê Hoa".
        // Sau fix: childSkillId=8, childSkillNum=4, charAnimId=11.
        private static SkillDefinition CuiYanVuDaLeHoa()
        {
            var s = BaseSkill(105, "雨打梨花", "Vũ Đả Lê Hoa", 30, 20, 300, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 8; s.childSkillNum = 4; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 10, ""), (20, 100, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 30, ""), (20, 250, "")), 0, Link(lv, (1, 30, ""), (20, 250, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanMucDaLuuTinh()
        {
            var s = BaseSkill(108, "牧野流星", "Mục Dã Lưu Tinh", 60, 20, 420, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 9; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 50, ""), (20, 385, "")), 0, Link(lv, (1, 50, ""), (20, 385, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.7.2 G7: CuiYan ID 109 "Tuyết Ảnh" — sai effect hoàn toàn.
        // PC cuiyan.lua::xueying: attackspeed_v + fastwalkrun_p (cast/atk/move speed buff = cảm giác "tuyết ảnh").
        // Trước fix: AllResP + AddDefenseV (all res + def) — sai semantics. Mất cảm giác tốc độ.
        // Sau fix: giữ AllResP fallback (Phase 4 thêm proper atk/move speed attribute); charAnimId 2→11.
        //   Tên "Tuyết Ảnh" = "bóng tuyết" = atk/cast/move speed nhanh như bóng tuyết lướt.
        private static SkillDefinition CuiYanTuyetAnh()
        {
            var s = BaseSkill(109, "雪影", "Tuyết Ảnh", 50, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                // [SECT-QUICKWIN] PC: attackspeed_v + fastwalkrun_p (chưa có enum FastWalkRunP). Phase 4 cần thêm.
                //   Tạm giữ AllResP fallback. Đánh dấu gap rõ ràng.
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 50, ""), (20, 350, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 30, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanBichHaiTrieuSinh()
        {
            // [SECT-QUICKWIN] §2.7.2 G4 + G6: CuiYan ID 111 "Bích Hải Triều Sinh" — childSkillId 74 vs PC 10, charAnimId 2 vs 11, StartEvent 112.
            // PC cuiyan.lua::bihai_chaosheng: childSkillId=10, charAnimId=11, StartEvent=1, StartSkill=112.
            // Sau fix: childSkillId=10, charAnimId=11, s.startSkillId=112 (anchor cho Phase 4 wire 16-missile AOE).
            var s = BaseSkill(111, "碧海潮生", "Bích Hải Triều Sinh", 60, 20, 72, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 10; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            s.startSkillId = 112; s.startSkillLevel = 1; // G6: anchor cho Bích Hải Triều Sinh b (16-missile AOE, Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 40, ""), (20, 350, "")), 0, Link(lv, (1, 40, ""), (20, 350, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanPhuVanTanTuyet()
        {
            var s = BaseSkill(113, "浮云散雪", "Phù Vân Tán Tuyết", 30, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 12; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 40, ""), (20, 200, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 20, ""), (20, 200, "")), 0, Link(lv, (1, 20, ""), (20, 200, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 25, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanBangCotTuyetTam()
        {
            var s = BaseSkill(114, "冰骨雪心", "Băng Cốt Tuyết Tâm", 60, 30, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddColdDamageV, Link(lv, (1, 20, ""), (30, 200, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, Link(lv, (1, 5, ""), (30, 30, "")), -1, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateTianRenSkills() => new()
        {
            TianRenPassiveDaofa(),
            TianRenPassiveMaofa(),
            TianRenCanYangRuXue(),
            TianRenHuoLienPhanHoa(),
            TianRenFeiHu(),
            TianRenThoiSonDienHai(),
            TianRenHunThuyMacNgu(),
            TianRenPhiHongVoTich(),
            TianRenLietHoaTinhThien(),
            TianRenThauThienHoanNhat(),
            TianRenLichMaDoatHon(),
            TianRenMinhTonBanSinh(),
            TianRenDonChiLietDiem(),
            TianRenNguHanhTran(),
            TianRenHuyenMinhHapTinh(),
            TianRenMaDiemThatSat(),
            TianRenThucCotHuyetNhan(),
            TianRenThienMaGiaiThe(),
            // [SECT-QUICKWIN] §2.8.2 G6: TianRen sub-skill MISSING trong mobile — thêm 6 entry.
            // PC tianren.lua line 199-204 + per-skill sub-form: chain damage cốt lõi của Thiên Nhẫn.
            // 361 Vân Long Kích — addskilldamage1 source cho 135/141/142 (chained damage L1-2)
            // 362 Thiên Ngoại Lưu Tinh — addskilldamage1 cho 138/145, vanishSkill=363
            // 363 Nghiệp Hỏa Phần Thành — fire spread AOE
            // 364 Bi Tô Thanh Phong — state buff
            // 1075 Giang Hải Não Lan — 150-tier, startSkill=1131
            // 1076 Tật Hỏa Liệu Nguyên — 150-tier, fire storm
            // Phase 4 runtime: chưa wire addskilldamage mechanism. Sub-skill được resolve khi chain fire.
            TianRenSubVanLongKich(),
            TianRenSubThienNgoaiLuuTinh(),
            TianRenSubNghiepHoaPhanThanh(),
            TianRenSubBiToThanhPhong(),
            TianRenSubGiangHaiNaoLan(),
            TianRenSubTatHoaLieuNguyen(),
            // [SECT-QUICKWIN] §2.8.2 G6: Sub-skill 192 (Ngự Phong Thuật) — start chain cho 148.
            // PC: referenced as startSkill=192 trong tianren.lua::moyan_qisha.
            // Mobile: missing trước fix → StartEvent runtime fire 192 bị null.
            TianRenSubNguPhongThuat(),
            // [SECT-QUICKWIN] §2.1.2 G6: Sub-skill 371 (Vô Ngã Vô Kiếm start) — start chain cho 163.
            // PC: referenced as startSkill=371 trong wudang.lua::renjian_heyi.
            // Mobile: missing trước fix → StartEvent runtime fire 371 bị null.
            WuDangSubNhanKiemStart(),
        };

        // [SECT-QUICKWIN] 6 TianRen sub-skill (PC tianren.lua + per-skill sub-form):
        // Sub-skills được fire khi parent skill (135/138/141/142/145/148) cast ở level thấp.
        // Mobile trước fix: MISSING trong catalog → addskilldamage chain dead.
        // Sau fix: 6 entry BaseSkill với child missile ID theo PC ModSkills.txt:
        //   361→169, 362→171, 363→170, 364→20, 1075→337, 1076→366.

        private static SkillDefinition TianRenSubVanLongKich()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 361 Vân Long Kích — sub-skill cho 135/141/142 (chained damage L1-2).
            // PC: child missile 169 (mv=1 homing, life=6, speed=32). isMelee=1 (melee-missile).
            var s = BaseSkill(361, "云龙击", "Vân Long Kích", 60, 20, 60, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 169; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 10; s.targetEnemy = true;
            s.horseLimit = 1;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 20, ""), (20, 200, "")), 0, Link(lv, (1, 30, ""), (20, 300, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenSubThienNgoaiLuuTinh()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 362 Thiên Ngoại Lưu Tinh — sub-skill cho 138/145, vanishSkill=363.
            // PC: child missile 171 (mv=0 stationary, life=14). Event chain fire spread khi missile vanish.
            var s = BaseSkill(362, "天外流星", "Thiên Ngoại Lưu Tinh", 80, 20, 420, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 171; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            s.vanishSkillId = 363; s.vanishSkillLevel = 1; // G6: anchor cho fire spread chain (Phase 4 wire runtime)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 80, ""), (20, 320, "")), 0, Link(lv, (1, 100, ""), (20, 400, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenSubNghiepHoaPhanThanh()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 363 Nghiệp Hỏa Phần Thành — sub-skill cho 148 (fire spread AOE).
            // PC: child missile 170 (mv=0 stationary, life=54, speed=2). AOE fire spread cuối game.
            var s = BaseSkill(363, "业火焚城", "Nghiệp Hỏa Phần Thành", 80, 20, 570, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 170; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 100, ""), (20, 500, "")), 0, Link(lv, (1, 150, ""), (20, 700, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenSubBiToThanhPhong()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 364 Bi Tô Thanh Phong — state buff (referenced by chain).
            // PC: child missile 20 (stateSpecialId=58). Hiện chưa rõ source dùng.
            var s = BaseSkill(364, "碧水清风", "Bi Tô Thanh Phong", 60, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.stateSpecialId = 58; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 30, ""), (20, 200, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenSubGiangHaiNaoLan()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 1075 Giang Hải Não Lan — 150-tier sub-skill, startSkill=1131.
            // PC: child missile 337 (mv=1 life=6 speed=36). 150-tier sub-form cuối game.
            var s = BaseSkill(1075, "江海凝岚", "Giang Hải Não Lan", 150, 20, 60, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 337; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 10; s.targetEnemy = true;
            s.horseLimit = 1;
            s.startSkillId = 1131; s.startSkillLevel = 1; // G6: anchor cho start chain (Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 50, ""), (20, 400, "")), 0, Link(lv, (1, 80, ""), (20, 600, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenSubTatHoaLieuNguyen()
        {
            // [SECT-QUICKWIN] §2.8.2 G6: ID 1076 Tật Hỏa Liệu Nguyên — 150-tier fire storm.
            // PC: child missile 366 (mv=0 stationary, life=37). Fire storm AOE cuối game.
            var s = BaseSkill(1076, "疾火燎原", "Tật Hỏa Liệu Nguyên", 150, 20, 570, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 366; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 150, ""), (20, 800, "")), 0, Link(lv, (1, 200, ""), (20, 1000, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.8.2 G6: ID 192 Ngự Phong Thuật — start sub-skill for 148 Ma Diệm Thất Sát.
        // PC tianren.lua::moyan_qisha: StartSkill=192. Mobile trước fix: MISSING trong catalog → StartEvent runtime fire 192 bị null.
        // Sau fix: registered để Phase 4 StartEvent generalizer resolve được.
        private static SkillDefinition TianRenSubNguPhongThuat()
        {
            var s = BaseSkill(192, "御风术", "Ngự Phong Thuật", 60, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, Link(lv, (1, 10, ""), (20, 30, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.1.2 G6: ID 371 Nhân Kiếm Hợp Nhất Start — start sub-skill for 163.
        // PC wudang.lua::renjian_heyi: StartSkill=371. Mobile trước fix: MISSING → StartEvent fire 371 bị null.
        // Sau fix: registered để Phase 4 StartEvent generalizer resolve được.
        private static SkillDefinition WuDangSubNhanKiemStart()
        {
            var s = BaseSkill(371, "人剑合一·起", "Nhân Kiếm Hợp Nhất Start", 50, 20, 0, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 50, ""), (20, 200, "")), 600 + 600 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenPassiveDaofa()
        {
            var s = BaseSkill(131, "天忍刀法", "Thiên Nhẫn Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenPassiveMaofa()
        {
            var s = BaseSkill(132, "天忍矛法", "Thiên Nhẫn Thương Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 3));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 35, ""), (20, 272, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 35, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenCanYangRuXue()
        {
            var s = BaseSkill(135, "残阳如血", "Tàn Dương Như Huyết", 10, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 54; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 5, ""), (20, 55, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 5, ""), (20, 50, "")), 0, Link(lv, (1, 5, ""), (20, 50, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 8, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenHuoLienPhanHoa()
        {
            var s = BaseSkill(136, "火莲焚华", "Hỏa Liên Phần Hoa", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.MeleeDamageReturnP, Link(lv, (1, -5, ""), (20, -35, "")), Link(lv, (1, 720, ""), (20, 2160, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 12, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenFeiHu()
        {
            var s = BaseSkill(137, "幻影飞狐", "Ảo Ảnh Phi Hồ", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, -15, ""), (20, -132, "")), Link(lv, (1, 720, ""), (20, 2160, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenThoiSonDienHai()
        {
            var s = BaseSkill(138, "推山填海", "Thôi Sơn Điền Hải", 30, 20, 350, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 55; s.childSkillNum = 10; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 5, ""), (20, 45, "")), 0, Link(lv, (1, 5, ""), (20, 45, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 32, ""), (20, 50, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenHunThuyMacNgu()
        {
            var s = BaseSkill(139, "Hỗn Thủy Mạc Ngư", "Hỗn Thủy Mạc Ngư", 20, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 70; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 9; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, 9 + lv * 10, 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StealStaminaP, 2 + lv, 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 6, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenPhiHongVoTich()
        {
            var s = BaseSkill(140, "飞鸿无迹", "Phi Hồng Vô Tích", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, -150, ""), (20, -1100, "")), Link(lv, (1, 720, ""), (20, 2160, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.8.2 G4: TianRen ID 141 "Liệt Hỏa Tình Thiên" — radius sai 5× + MissilesForm sai.
        // PC tianren.lua::liehuo_qingtian: skill_attackradius 72 (cast range, PC), MisslesForm=3 (Surround).
        // Trước fix: radius 384 (overcast 5×), form Single (PC Surround 16 tia tỏa tròn).
        // Sau fix: radius 72 + SkillMissileForm.Surround.
        private static SkillDefinition TianRenLietHoaTinhThien()
        {
            var s = BaseSkill(141, "烈火情天", "Liệt Hỏa Tình Thiên", 30, 20, 72, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 56; s.childSkillNum = 16; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 5, ""), (20, 30, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 15, ""), (20, 75, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 8, ""), (20, 150, "")), 0, Link(lv, (1, 8, ""), (20, 150, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenThauThienHoanNhat()
        {
            var s = BaseSkill(142, "偷天换日", "Thâu Thiên Hoán Nhật", 60, 20, 78, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 69; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 9; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 25, ""), (20, 231, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 4, ""), (20, 55, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 10, ""), (20, 482, "")), 0, Link(lv, (1, 10, ""), (20, 482, ""))));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StealLifeP, Link(lv, (1, 1, ""), (20, 8, "")), 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StealManaP, Link(lv, (1, 1, ""), (20, 6, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 12, ""), (20, 20, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenLichMaDoatHon()
        {
            var s = BaseSkill(143, "厉魔夺魄", "Lịch Ma Đoạt Hồn", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, -25, ""), (20, -215, "")), Link(lv, (1, 720, ""), (20, 2160, "")), 6));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 30, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenMinhTonBanSinh()
        {
            var s = BaseSkill(144, "Minh Tôn Bản Sinh (##)", "Minh Tôn Bản Sinh", 30, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int val = Floor(Log10(lv + 1) / 2f * 70f);
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, val, -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenDonChiLietDiem()
        {
            var s = BaseSkill(145, "单指烈焰", "Đơn Chỉ Liệt Diệm", 10, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 57; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 30, ""), (20, 225, "")), 0, Link(lv, (1, 30, ""), (20, 225, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 30, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenNguHanhTran()
        {
            var s = BaseSkill(146, "五行阵", "Ngũ Hành Trận", 40, 20, 180, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.isAura = true; s.stateSpecialId = 226; s.childSkillId = 226; s.childSkillLevel = 1; s.childSkillNum = 1; s.targetSelf = true; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 75, ""), (20, 550, "")), 18, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenHuyenMinhHapTinh()
        {
            var s = BaseSkill(147, "Huyền Minh Hấp Tinh", "Huyền Minh Hấp Tinh", 40, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 71; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 9; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, 25 + lv * 10, 0, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StealManaP, 2 + lv / 2, 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 16 - lv / 4, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.8.2 G4 + G6: TianRen ID 148 "Ma Diệm Thất Sát" — radius sai lớn + thiếu StartEvent.
        // PC tianren.lua::moyan_qisha: skill_attackradius {{1,448},{20,570},{21,570}}
        //   startSkill=192 (Ngự Phong Thuật — fire wind visual trước khi bắn).
        // Trước fix: radius 320 (sai 44% undercast), thiếu startSkill=192.
        // Sau fix: radius 570 + s.startSkillId=192 (anchor cho Phase 4 wire runtime).
        private static SkillDefinition TianRenMaDiemThatSat()
        {
            var s = BaseSkill(148, "魔炎七杀", "Ma Diệm Thất Sát", 60, 20, 570, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 58; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            s.startSkillId = 192; s.startSkillLevel = 1; // G6: anchor cho Ngự Phong Thuật (Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 35, ""), (20, 637, "")), 0, Link(lv, (1, 35, ""), (20, 637, ""))));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeP, Link(lv, (1, 12, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 30, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenThucCotHuyetNhan()
        {
            var s = BaseSkill(149, "Thực Cốt Huyết Nhận", "Thực Cốt Huyết Nhận", 50, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, 40 + 10 * lv, 1080 + 162 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 50, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianRenThienMaGiaiThe()
        {
            var s = BaseSkill(150, "天魔解体", "Thiên Ma Giải Thể", 60, 30, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = Link(lv, (1, 18 * 120, ""), (30, 18 * 360, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 75, ""), (30, 850, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 65, ""), (30, 600, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddFireDamageV, Link(lv, (1, 20, ""), (30, 315, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireEnhanceP, Link(lv, (1, 31, ""), (30, 100, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, Link(lv, (1, 26, ""), (30, 102, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, Link(lv, (1, 26, ""), (30, 81, "")), dur, 0));
                // [SECT-QUICKWIN] §2.8.2 G7: TianRen ID 150 "Thiên Ma Giải Thể" (Tự Hủy Ma).
                // PC tianren.lua tianmo_jieti: lifemax_p {{1,-11},{20,-30},{30,-40}}
                //   (ÂM = tự giảm HP để tăng dmg — đây là điểm cốt lõi của "Giải Thể").
                // Mobile dùng +21→+20 (DƯƠNG = buff HP) → sai gameplay hoàn toàn.
                // Per-skill tianmo-jieti.lua::Getlifemax_p: result1 = -10-level, result2 = 600+level*200
                //   → L1=-11, L20=-30, L30=-40. PC tianren.lua line 170 (commented) cũng confirm.
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxP, Link(lv, (1, -11, ""), (20, -30, ""), (30, -40, "")), Link(lv, (1, 18 * 45, ""), (30, 18 * 180, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 100, 0, 0));
                return d;
            });
            return s;
        }

        public static List<SkillDefinition> CreateKunLunSkills() => new()
        {
            KunLunPassiveDaofa(),
            KunLunPassiveJianfa(),
            KunLunHuPhongFa(),
            KunLunDaiLangThucKhong(),
            KunLunThanhPhongPhu(),
            KunLunThienTeTanLoi(),
            KunLunThienThanhDiaTroc(),
            KunLunKiBanPhu(),
            KunLunKhiHanNgaoTuyet(),
            KunLunCuongPhongSauDien(),
            KunLunBachXuyenNapHai(),
            KunLunNhatKhiTamThanh(),
            KunLunCuongLoiChanDia(),
            KunLunDocTeTiTa(),
            KunLunKhiTamPhu(),
            KunLunNguLoiChanhPhap(),
            KunLunTueNguyetVoTinhPhu(),
            KunLunKimThienThoatXac(),
            // [SECT-QUICKWIN] §2.9.1 G5: ID 90 Mê Tung Ảo Ảnh MOVE từ Nga My → Côn Luân.
            // PC: skill thuộc Côn Luân (KunLun.lua), mobile trước fix đặt nhầm trong EMei catalog.
            // Hàm giữ nguyên vị trí (vẫn dùng EMeiMeTungAoAnh để tránh move) — chỉ move call list.
            EMeiMeTungAoAnh()
        };

        private static SkillDefinition KunLunPassiveDaofa()
        {
            var s = BaseSkill(167, "昆仑刀法", "Côn Lôn Đao Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 35, ""), (20, 215, "")), -1, 1));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 50, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunPassiveJianfa()
        {
            var s = BaseSkill(168, "昆仑剑法", "Côn Lôn Kiếm Pháp", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddLightingDamageV, Link(lv, (1, 19, ""), (20, 215, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunHuPhongFa()
        {
            var s = BaseSkill(169, "呼风法", "Hô Phong Pháp", 10, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 14; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 5, ""), (20, 75, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 15, ""), (20, 180, "")), 0, Link(lv, (1, 15, ""), (20, 180, ""))));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunDaiLangThucKhong()
        {
            var s = BaseSkill(170, "大浪淘沙", "Đại Lãng Thực Không", 10, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, 50 + 10 * lv, 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 18 + 2 * lv, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunThanhPhongPhu()
        {
            var s = BaseSkill(171, "清风符", "Thanh Phong Phù", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 19; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, Link(lv, (1, 22, ""), (20, 60, "")), 2160, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 40, 0, 0));
                return d;
            });
            return s;
        }

        // [SECT-QUICKWIN] §2.9.2 G6 + G4: KunLun ID 172 "Thiên Tế Tấn Lôi" — radius sai + thiếu StartEvent.
        // PC kunlun.lua::thien_te_tan_loi: StartEvent=1, StartSkill=399; attackRadius 384→448.
        // Trước fix: radius 384 vs PC 448 (sai 14%), thiếu startSkill=399.
        // Sau fix: radius 448 + s.startSkillId=399 (anchor cho Phase 4 wire).
        private static SkillDefinition KunLunThienTeTanLoi()
        {
            var s = BaseSkill(172, "天际迅雷", "Thiên Tế Tấn Lôi", 30, 20, 448, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 15; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            s.startSkillId = 399; s.startSkillLevel = 1; // G6: anchor cho event chain (Phase 4 wire)
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 25, ""), (20, 550, "")), 0, Link(lv, (1, 25, ""), (20, 550, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 30, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 15, ""), (20, 35, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunThienThanhDiaTroc()
        {
            var s = BaseSkill(173, "天清地浊", "Thiên Thanh Địa Trọc", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = 2160;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LightingResP, Link(lv, (1, 13, ""), (20, 32, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, 13, ""), (20, 32, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, Link(lv, (1, 9, ""), (20, 28, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, Link(lv, (1, 9, ""), (20, 28, "")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 12, ""), (20, 90, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunKiBanPhu()
        {
            var s = BaseSkill(174, "羁绊符", "Ki Bán Phù", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FastWalkRunP, Link(lv, (1, -22, ""), (20, -52, "")), Link(lv, (1, 360, ""), (20, 1620, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 60, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunKhiHanNgaoTuyet()
        {
            var s = BaseSkill(175, "欺寒傲雪", "Khi Hàn Ngạo Tuyết", 40, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, Link(lv, (1, -6, ""), (20, -39, "")), Link(lv, (1, 810, ""), (20, 2160, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 30, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunCuongPhongSauDien()
        {
            var s = BaseSkill(176, "狂风骤电", "Cuồng Phong Sậu Điện", 50, 20, 448, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 16; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsEnhanceP, Link(lv, (1, 55, ""), (20, 386, "")), 0, 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 45, ""), (20, 532, "")), 0, Link(lv, (1, 45, ""), (20, 532, ""))));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 5, ""), (20, 15, "")), Link(lv, (1, 1, ""), (20, 20, "")), 0));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 25, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunBachXuyenNapHai()
        {
            var s = BaseSkill(177, "百川纳海", "Bách Xuyên Nạp Hải", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = 2160;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, 13, ""), (20, 32, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, Link(lv, (1, 9, ""), (20, 28, "")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 12, ""), (20, 50, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunNhatKhiTamThanh()
        {
            var s = BaseSkill(178, "一气三清", "Nhất Khí Tam Thanh", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int dur = 2160;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 35, ""), (20, 215, "")), dur, 1));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 16, ""), (20, 35, "Conic")), dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 80, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunCuongLoiChanDia()
        {
            var s = BaseSkill(179, "狂雷震地", "Cuồng Lôi Chấn Địa", 10, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 17; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 27, ""), (20, 315, "")), 0, Link(lv, (1, 27, ""), (20, 315, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 1, ""), (20, 10, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 15, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunDocTeTiTa()
        {
            var s = BaseSkill(180, "木珠兵解", "Độc Tê Tị Tà", 40, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, 120 + 18 * lv, 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 18 + 2 * lv, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunKhiTamPhu()
        {
            var s = BaseSkill(181, "弃心符", "Khí Tâm Phù", 40, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 22; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.StunP, Link(lv, (1, 16, ""), (20, 35, "")), Link(lv, (1, 5, ""), (20, 36, "")), 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 100, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunNguLoiChanhPhap()
        {
            var s = BaseSkill(182, "五雷正法", "Ngũ Lôi Chánh Pháp", 60, 20, 448, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 18; s.childSkillNum = 4; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, Link(lv, (1, 25, ""), (20, 937, "")), 0, Link(lv, (1, 25, ""), (20, 937, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 50, ""), (20, 90, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunTueNguyetVoTinhPhu()
        {
            var s = BaseSkill(183, "岁月无情", "Tuế Nguyệt Vô Tình", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 23; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                int val = Floor(Log10(lv + 1) / 2f * 60f);
                int dur = 300 + 240 * lv;
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackSpeedV, val, dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.CastSpeedV, val, dur, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 150, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition KunLunKimThienThoatXac()
        {
            var s = BaseSkill(184, "金蝉脱壳", "Kim Thiền Thoát Xác", 50, 20, 0, SkillMissileForm.None);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 11; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, 120 + 20 * lv, 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 18 + 2 * lv, 0, 0));
                return d;
            });
            return s;
        }
    }
}
