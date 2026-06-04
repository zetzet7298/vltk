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
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, pct, dur, 0));
                int cost = Link(lv, (1, 24, ""), (20, 50, ""));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, cost, 0, 0));
                return d;
            }, skillStyle: PcSkillStyle.Missiles),

            // 128 Kháng Long Hữu Hối: damage (kanglong_youhui)
            DamageSkillNew(128, "Kháng Long Hữu Hối", "Kháng Long Hữu Hối", 50, 20, 512, 48, SkillMissileForm.Fan, 15, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 10, ""), (20, 536, "")), 0, Link(lv, (1, 10, ""), (20, 536, ""))),
                cost: (lv) => (Link(lv, (1, 10, ""), (20, 50, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 10, ""), (20, 50, "")), -1, 0),
                horseLimit: 1),

            // 129 Hóa Hiểm Vi Di: buff
            UtilitySkill(129, "Hóa Hiểm Vi Di", "Hóa Hiểm Vi Di", 20, 400, SkillMissileForm.Surround, targetEnemy:false, targetSelf:true, levelData:(lv)=>{
                var d = new SkillLevelData { level = lv };
                int ret = Link(lv, (1, 4, ""), (20, 46, ""));
                int def = Link(lv, (1, 48, ""), (20, 800, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, ret, -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, def, -1, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 0, 0, 0));
                return d;
            }, skillStyle: PcSkillStyle.PassivityNpcState),

            // 130 Túy Điệp Cuồng Vũ: buff (zuidie_kuangwu)
            UtilitySkill(130, "Túy Điệp Cuồng Vũ ", "Túy Điệp Cuồng Vũ", 60, 400, SkillMissileForm.None, targetEnemy:false, targetSelf:true, stateSpecialId:43, levelData:(lv)=>{
                var d = new SkillLevelData{level=lv};
                int dur = 18 * Link(lv, (1, 120, ""), (30, 180, ""));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 1, ""), (30, 30, "")), dur, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, Link(lv, (1, 10, ""), (30, 215, "")), dur, 0));
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
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, pct, dur, 0));
                int cost = Link(lv, (1, 24, ""), (20, 50, ""));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, cost, 0, 0));
                return d;
            }),

            DamageSkillNew(357, "Phi Long Tại Thiên ", "Phi Long Tại Thiên", 80, 20, 512, 166, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => 0,
                fire: (lv) => (Link(lv, (1, 10, ""), (15, 300, ""), (20, 750, "")), 0, Link(lv, (1, 10, ""), (15, 300, ""), (20, 750, ""))),
                cost: (lv) => (Link(lv, (1, 10, ""), (20, 65, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 20, ""), (20, 60, "")), -1, 0),
                horseLimit: 1),

            DamageSkillNew(359, "Thiên Hạ Vô Cẩu ", "Thiên Hạ Vô Cẩu (player)", 80, 20, 512, 168, SkillMissileForm.Single, 1, false, false, 11,
                phys: (lv) => Link(lv, (1, 12, ""), (15, 100, ""), (20, 206, "")),
                fire: (lv) => (Link(lv, (1, 70, ""), (15, 150, ""), (20, 285, "")), 0, Link(lv, (1, 70, ""), (15, 200, ""), (20, 432, ""))),
                cost: (lv) => (Link(lv, (1, 20, ""), (20, 50, "")), 0, 0),
                extra: (lv) => State(MagicAttributeKind.ConfuseP, Link(lv, (1, 20, ""), (20, 60, "")), -1, 0),
                horseLimit: 1),

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
            // Source: /var/www/vltksource_new/vl_update_27/Client 6.0/script/skill2/wudang.lua
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
            WuDangLightningDamage(165, "无我无剑", "Vô Ngã Vô Kiếm", 50, 400, 29, 16, 11,
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
            var s = BaseSkill(162, "玄一无象", "Huyền Nhất Vô Tượng", 50, 20, 520, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 27; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr"); s.missileSpriteId = Sprite("\\spr\\skill\\武当\\wd_04_玄一无象.spr");
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.LightingDamageV, 4 + lv * 7, 0, 296 + lv * 59)); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20 + lv * 3, 0, 0)); return d; }); return s;
        }

        private static SkillDefinition WuDangRenJianHeYi()
        {
            var s = BaseSkill(163, "人剑合一", "Nhân Kiếm Hợp Nhất", 50, 20, 90, SkillMissileForm.Surround); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 215; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true; s.effectSourceId = Sprite("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr");
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
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addPhys(lv), -1, elementParam)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, 12+3*lv, -1, 0)); d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, 5+lv, -1, 0)); return d; }); return s;
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
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addFire(lv), -1, elementParam));
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveMasteryLong(int id, string raw, string vi, int req, Func<int,int> lifemax, Func<int,int> manamax, Func<int,int> addfire, int elementParam, string icon)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, addfire(lv), -1, elementParam));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, lifemax(lv), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, manamax(lv), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition PassiveMasteryDao(int id, string raw, string vi, int req, Func<int,int> attackSpeed, Func<int,int> castSpeed, int elementParam, string icon, int charAnim = 14)
        {
            var s = BaseSkill(id, raw, vi, req, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = charAnim; s.targetOnly = false; s.iconSourceId = Sprite(icon);
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, attackSpeed(lv), -1, elementParam));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, castSpeed(lv), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition DamageSkillNew(int id, string raw, string vi, int req, int max, int radius, int child, SkillMissileForm form, int childNum, bool isPhysical, bool targetOnly, int charAnim, Func<int,int> phys, Func<int,(int,int,int)> fire, Func<int,(int,int,int)> cost, Func<int,SkillLevelData> extra=null, int horseLimit=0, int missilesGenerateData=0)
        {
            var s = BaseSkill(id, raw, vi, req, max, radius, form); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.isPhysical = isPhysical; s.targetOnly = targetOnly; s.targetEnemy = true; s.horseLimit = horseLimit; s.missilesGenerateData = missilesGenerateData;
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

        private static SkillDefinition DamageSkill(int id, string raw, string vi, int req, int max, int radius, int child, SkillMissileForm form, int childNum, bool isPhysical, bool targetOnly, int charAnim, Func<int,(int,int,int)> phys, Func<int,(int,int,int)> fire, Func<int,(int,int,int)> cost, Func<int,SkillMagicAttribute> extra=null, int horseLimit=0, int missilesGenerateData=0)
        {
            var s = BaseSkill(id, raw, vi, req, max, radius, form); s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = child; s.childSkillNum = childNum; s.baseSkill = true; s.charAnimId = charAnim; s.waitTime = 5; s.timePerCast = 2; s.isPhysical = isPhysical; s.targetOnly = targetOnly; s.targetEnemy = true; s.horseLimit = horseLimit; s.missilesGenerateData = missilesGenerateData;
            s.effectSourceId = id >= 118 ? Sprite("\\spr\\skill\\天忍\\mag_tr_16_施魔法.spr") : null;
            AddLevels(s, lv => { var d = new SkillLevelData { level = lv }; var p=phys(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV,p.Item1,p.Item2,p.Item3)); var f=fire(lv); d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV,f.Item1,f.Item2,f.Item3)); if (extra!=null) d.state.Add(extra(lv)); var c=cost(lv); d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV,c.Item1,c.Item2,c.Item3)); return d; }); return s;
        }

        private static SkillDefinition AuraSkill(int id, string raw, string vi, int req, int radius, int stateId, int child, Func<int,SkillLevelData> levelData)
        { var s = BaseSkill(id, raw, vi, req, 20, radius, SkillMissileForm.None); s.skillStyle = PcSkillStyle.InitiativeNpcState; s.isAura = true; s.stateSpecialId = stateId; s.childSkillId = child; s.childSkillLevel = 1; s.childSkillNum = 1; s.targetSelf = true; s.charAnimId = 14; AddLevels(s, levelData); return s; }

        private static SkillDefinition UtilitySkill(int id, string raw, string vi, int req, int radius, SkillMissileForm form, bool targetEnemy, bool targetSelf, int stateSpecialId=0, Func<int,SkillLevelData> levelData=null, PcSkillStyle skillStyle = PcSkillStyle.InitiativeNpcState, int maxLevel = 20)
        { var s = BaseSkill(id, raw, vi, req, maxLevel, radius, form); s.skillStyle = skillStyle; s.targetEnemy = targetEnemy; s.targetSelf = targetSelf; s.stateSpecialId = stateSpecialId; s.charAnimId = 11; AddLevels(s, levelData ?? (lv => new SkillLevelData{level=lv})); return s; }

        private static SkillDefinition BaseSkill(int id, string raw, string vi, int req, int max, int radius, SkillMissileForm form) => new SkillDefinition { skillId=id, nameRaw=raw, nameNormalized=vi, reqLevel=req, maxLevel=max, attackRadius=radius, missileForm=form, faction = IsCaiBangSkill(id) ? CombatFaction.CaiBang : IsWuDangSkill(id) ? CombatFaction.WuDang : IsShaolinSkill(id) ? CombatFaction.Shaolin : IsTangMenSkill(id) ? CombatFaction.TangMen : IsEMeiSkill(id) ? CombatFaction.EMei : IsTianWangSkill(id) ? CombatFaction.TianWang : IsWuDuSkill(id) ? CombatFaction.WuDu : IsCuiYanSkill(id) ? CombatFaction.CuiYan : IsTianRenSkill(id) ? CombatFaction.TianRen : IsKunLunSkill(id) ? CombatFaction.KunLun : CombatFaction.None, iconSourceId = Sprite(IconPathForSkill(id)), equipLimit=-2 };

        // Cái Bang skill set: PC gốc 115-130 + MOD 274, 277, 357, 359, 360, 714, 720, 1073, 1074, 1539 (NPC variant).
        // 1539 is an NPC/boss version of Thiên Hạ Vô Cẩu and stays in the catalog for boss AI;
        // the player skill panel filters it out via isNpcVariant.
        public static bool IsCaiBangSkill(int id) => id==209 || (id>=115 && id<=130) || id==274 || id==277 || id==357 || id==359 || id==360 || id==714 || id==720 || id==1073 || id==1074 || id==1539 || id==389;
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

        private static SkillDefinition ShaolinJingangFumo()
        {
            var s = BaseSkill(10, "金刚伏魔", "Kim Cang Phục Ma", 30, 20, 400, SkillMissileForm.Single);
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

        private static SkillDefinition TangMenDuoHunBiao()
        {
            var s = BaseSkill(47, "夺魂镖", "Đoạt Hồn Tiêu", 10, 20, 450, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 116; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
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

        private static SkillDefinition TangMenManThienHoaVu()
        {
            var s = BaseSkill(54, "漫天花雨", "Mạn Thiên Hoa Vũ", 50, 20, 400, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 38; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
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

        private static SkillDefinition TangMenThienLaDiaVong()
        {
            var s = BaseSkill(58, "天罗地网", "Thiên La Địa Võng", 50, 20, 520, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 67; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
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
            EMeiMeTungAoAnh(),
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

        private static SkillDefinition EMeiThuPhongDiep()
        {
            var s = BaseSkill(81, "秋风扫叶", "Thu Phong Diệp", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 204; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
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

        private static SkillDefinition EMeiLuuThuy()
        {
            var s = BaseSkill(86, "流水", "Lưu Thủy", 40, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 206; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
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

        private static SkillDefinition EMeiMongDiep()
        {
            var s = BaseSkill(89, "梦蝶", "Mộng Điệp", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 207; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 1, ""), (20, 10, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 20, ""), (20, 150, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiMeTungAoAnh()
        {
            var s = BaseSkill(90, "迷踪幻影", "Mê Tung Ảo Ảnh", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 20; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
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

        private static SkillDefinition EMeiPhatTamTuHuu()
        {
            var s = BaseSkill(92, "佛心慈佑", "Phật Tâm Từ Hữu", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 208; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 5, ""), (20, 30, "")), 1440, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition EMeiTuHangPhuDo()
        {
            var s = BaseSkill(93, "慈航普渡", "Từ Hàng Phổ Độ", 20, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetSelf = true; s.targetAlly = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.immediate.Add(new SkillMagicAttribute(MagicAttributeKind.ManaReplenishV, Link(lv, (1, 275, ""), (20, 750, "")), 0, 0));
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            var s = BaseSkill(33, "静心诀", "Tĩnh Tâm Quyết", 10, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AttackRatingEnhanceP, Link(lv, (1, 10, ""), (20, 100, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 20, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangKinhLoiTram()
        {
            var s = BaseSkill(34, "惊雷斩", "Kinh Lôi Trảm", 10, 20, 72, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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

        private static SkillDefinition TianWangThienVuongChienY()
        {
            var s = BaseSkill(36, "天王战意", "Thiên Vương Chiến Ý", 60, 30, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ManaMaxP, Link(lv, (1, 5, ""), (30, 60, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 2, ""), (30, 20, "")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition TianWangBatPhongTram()
        {
            var s = BaseSkill(37, "八风斩", "Bát Phong Trảm", 30, 20, 90, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Melee; s.charAnimId = 2; s.targetEnemy = true;
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
            var s = BaseSkill(42, "金钟罩", "Kim Chung Tráo", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsResP, Link(lv, (1, 10, ""), (20, 40, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.ColdResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.FireResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
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

        private static SkillDefinition WuDuVoHinhDoc()
        {
            var s = BaseSkill(69, "无形蛊", "Vô Hình Độc", 30, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 5; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonDamageV, Link(lv, (1, 25, ""), (20, 220, "")), 0, Link(lv, (1, 25, ""), (20, 220, ""))));
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

        private static SkillDefinition WuDuVanDocThucTam()
        {
            var s = BaseSkill(73, "万毒蚀心", "Vạn Độc Thực Tâm", 20, 20, 440, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.PoisonResP, Link(lv, (1, -10, ""), (20, -40, "")), 600 + 600 * lv, 0));
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

        private static SkillDefinition CuiYanPassiveShuangDao()
        {
            var s = BaseSkill(97, "翠烟双刀", "Thúy Yên Song Đao", 10, 20, 0, SkillMissileForm.None); s.skillStyle = PcSkillStyle.PassivityNpcState; s.charAnimId = 14;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddPhysicsDamageP, Link(lv, (1, 15, ""), (20, 215, "")), -1, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.DeadlyStrikeEnhanceP, Link(lv, (1, 6, ""), (20, 25, "Conic")), -1, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanPhongHoaTuyetNguyet()
        {
            var s = BaseSkill(99, "风花雪月", "Phong Hoa Tuyết Nguyệt", 10, 20, 360, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 70; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
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

        private static SkillDefinition CuiYanHoTheHanBang()
        {
            var s = BaseSkill(100, "护体寒冰", "Hộ Thể Hàn Băng", 40, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
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
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 71; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
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

        private static SkillDefinition CuiYanVuDaLeHoa()
        {
            var s = BaseSkill(105, "雨打梨花", "Vũ Đả Lê Hoa", 30, 20, 300, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 72; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 73; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.ColdDamageV, Link(lv, (1, 50, ""), (20, 385, "")), 0, Link(lv, (1, 50, ""), (20, 385, ""))));
                d.damage.Add(new SkillMagicAttribute(MagicAttributeKind.SeriesDamageP, Link(lv, (1, 10, ""), (20, 50, "")), 0, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, Link(lv, (1, 20, ""), (20, 40, "")), 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanTuyetAnh()
        {
            var s = BaseSkill(109, "雪影", "Tuyết Ảnh", 50, 20, 400, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.InitiativeNpcState; s.charAnimId = 2; s.targetSelf = true;
            AddLevels(s, lv => {
                var d = new SkillLevelData { level = lv };
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AllResP, Link(lv, (1, 5, ""), (20, 25, "")), 1200 + 1200 * lv, 0));
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.AddDefenseV, Link(lv, (1, 50, ""), (20, 350, "")), 1200 + 1200 * lv, 0));
                d.skill.Add(new SkillMagicAttribute(MagicAttributeKind.SkillCostV, 30, 0, 0));
                return d;
            });
            return s;
        }

        private static SkillDefinition CuiYanBichHaiTrieuSinh()
        {
            var s = BaseSkill(111, "碧海潮生", "Bích Hải Triều Sinh", 60, 20, 72, SkillMissileForm.Surround);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 74; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
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
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 75; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 2; s.targetEnemy = true;
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
            TianRenThienMaGiaiThe()
        };

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

        private static SkillDefinition TianRenLietHoaTinhThien()
        {
            var s = BaseSkill(141, "烈火情天", "Liệt Hỏa Tình Thiên", 30, 20, 384, SkillMissileForm.Single);
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

        private static SkillDefinition TianRenMaDiemThatSat()
        {
            var s = BaseSkill(148, "魔炎七杀", "Ma Diệm Thất Sát", 60, 20, 320, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 58; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
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
                d.state.Add(new SkillMagicAttribute(MagicAttributeKind.LifeMaxP, Link(lv, (1, 21, ""), (30, 20, "")), Link(lv, (1, 18 * 45, ""), (30, 18 * 180, "")), 0));
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
            KunLunKimThienThoatXac()
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

        private static SkillDefinition KunLunThienTeTanLoi()
        {
            var s = BaseSkill(172, "天际迅雷", "Thiên Tế Tấn Lôi", 30, 20, 384, SkillMissileForm.Single);
            s.skillStyle = PcSkillStyle.Missiles; s.childSkillId = 15; s.childSkillNum = 1; s.baseSkill = true; s.charAnimId = 11; s.targetEnemy = true;
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
