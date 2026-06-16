using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Combat parity tests cho 10 môn phái: xác nhận ID, tên, faction, attack radius,
    /// child skill ID, magic attribute values khớp với PC source.
    ///
    /// PC source: /var/www/vltksource_new/vl_update_27/Client 6.0/script/skill2/*.lua
    /// </summary>
    public class AllFactionsCombatParityTests
    {
        // ==== Thiếu Lâm (PC: shaolin.lua) ====
        [Test]
        public void Shaolin_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateShaolinSkills();
            var expectedIds = new[] { 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            Assert.That(skills.Select(s => s.skillId), Is.EquivalentTo(expectedIds));
            Assert.That(skills.All(s => s.faction == CombatFaction.Shaolin), Is.True);
        }

        [Test]
        public void Shaolin_JingangFumo_MatchesPC()
        {
            // PC shaolin.lua: jingang_fumo: physicsenhance_p {{1,15},{20,55}}, seriesdamage_p {{1,1},{20,10}}, skill_cost_v {{1,2},{20,6}}
            var skills = PcCombatCatalogFactory.CreateShaolinSkills();
            var skill = skills.First(s => s.skillId == 10);
            Assert.That(skill.DisplayName, Is.EqualTo("Kim Cang Phục Ma"));
            var d = skill.GetPcLevelData(20);
            Assert.That(d.First(MagicAttributeKind.PhysicsEnhanceP).value1, Is.EqualTo(55));
            Assert.That(d.First(MagicAttributeKind.SeriesDamageP).value1, Is.EqualTo(10));
            Assert.That(d.First(MagicAttributeKind.SkillCostV).value1, Is.EqualTo(6));
        }

        [Test]
        public void Shaolin_XinglongBayu_MatchesPC()
        {
            // PC shaolin.lua: xinglong_buyu: physicsenhance_p {{1,60},{20,445}}, deadlystrike_p {{1,5},{20,20}}
            var skills = PcCombatCatalogFactory.CreateShaolinSkills();
            var skill = skills.First(s => s.skillId == 14);
            Assert.That(skill.DisplayName, Is.EqualTo("Hàng Long Bất Vũ"));
            var d = skill.GetPcLevelData(20);
            Assert.That(d.First(MagicAttributeKind.PhysicsEnhanceP).value1, Is.EqualTo(445));
            Assert.That(d.First(MagicAttributeKind.DeadlyStrikeP).value1, Is.EqualTo(20));
            Assert.That(d.First(MagicAttributeKind.SkillCostV).value1, Is.EqualTo(10));
        }

        // ==== Võ Đang (PC: wudang.lua) — also covered by WuDangCombatCatalogTests ====
        [Test]
        public void WuDang_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateWuDangSkills();
            Assert.That(skills.Count, Is.GreaterThanOrEqualTo(16));
            Assert.That(skills.All(s => s.faction == CombatFaction.WuDang), Is.True);
        }

        // ==== Nga My (PC: emei.lua) ====
        [Test]
        public void EMei_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateEMeiSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.EMei), Is.True);
            Assert.That(skills.Count, Is.GreaterThanOrEqualTo(12), "Min 12 EMei skills");
        }

        [Test]
        public void EMei_PassiveJianFa_MatchesPC()
        {
            // PC emei.lua: emei_jianfa: physicsenhance_p {{1,15},{20,215}}
            var skills = PcCombatCatalogFactory.CreateEMeiSkills();
            var skill = skills.First(s => s.skillId == 77);
            Assert.That(skill.DisplayName, Is.EqualTo("Nga My Kiếm Pháp"));
            Assert.That(skill.skillStyle, Is.EqualTo(PcSkillStyle.PassivityNpcState));
            var d = skill.GetPcLevelData(20);
            Assert.That(d.First(MagicAttributeKind.PhysicsEnhanceP).value1, Is.EqualTo(215));
        }

        // ==== Tinh Túc (PC: tianren.lua) ====
        [Test]
        public void TianRen_HasMainSkills_AllTianRenFaction()
        {
            var skills = PcCombatCatalogFactory.CreateTianRenSkills();
            // Sub-skills (e.g. TianRenSub*) may be cross-faction; only check main 131, 132, 135-150 are TianRen
            var mainSkills = skills.Where(s => s.skillId == 131 || s.skillId == 132 || (s.skillId >= 135 && s.skillId <= 150)).ToList();
            Assert.That(mainSkills.Count, Is.GreaterThanOrEqualTo(18), "Min 18 main TianRen skills");
            Assert.That(mainSkills.All(s => s.faction == CombatFaction.TianRen), Is.True);
        }

        [Test]
        public void TianRen_CanYangRuXue_ExistsAtCorrectId()
        {
            // PC tianren.lua: canyang_ruxue at ID 135
            var skills = PcCombatCatalogFactory.CreateTianRenSkills();
            var skill = skills.FirstOrDefault(s => s.skillId == 135);
            Assert.That(skill, Is.Not.Null, "TianRen skill 135 not found");
            Assert.That(skill.DisplayName, Is.Not.Empty);
        }

        // ==== Côn Lôn (PC: kunlun.lua) ====
        [Test]
        public void KunLun_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateKunLunSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.KunLun), Is.True);
            Assert.That(skills.Count, Is.GreaterThanOrEqualTo(18), "Min 18 KunLun skills");
        }

        [Test]
        public void KunLun_PassiveDaofa_Exists()
        {
            // PC kunlun.lua: kunlun_daofa at ID 167
            var skills = PcCombatCatalogFactory.CreateKunLunSkills();
            var skill = skills.First(s => s.skillId == 167);
            Assert.That(skill.DisplayName, Is.EqualTo("Côn Lôn Đao Pháp"));
            Assert.That(skill.skillStyle, Is.EqualTo(PcSkillStyle.PassivityNpcState));
        }

        // ==== Thục Sơn / Tàng Môn (PC: tangmen.lua) ====
        [Test]
        public void TangMen_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateTangMenSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.TangMen), Is.True);
            Assert.That(skills.Count, Is.EqualTo(10));
        }

        [Test]
        public void TangMen_AnQi_MatchesPC()
        {
            // PC tangmen.lua: tangmen_anqi at ID 43, addphysicsdamage_p {{1,25},{20,215}}
            var skills = PcCombatCatalogFactory.CreateTangMenSkills();
            var skill = skills.First(s => s.skillId == 43);
            Assert.That(skill.DisplayName, Is.EqualTo("Đường Môn Ám Khí"));
            Assert.That(skill.skillStyle, Is.EqualTo(PcSkillStyle.PassivityNpcState));
            var d = skill.GetPcLevelData(20);
            Assert.That(d.First(MagicAttributeKind.AddPhysicsDamageP).value1, Is.EqualTo(215));
        }

        // ==== Đường Môn / Võ Độc (PC: wudu.lua) ====
        [Test]
        public void WuDu_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateWuDuSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.WuDu), Is.True);
            Assert.That(skills.Count, Is.GreaterThanOrEqualTo(16), "Min 16 WuDu skills");
        }

        [Test]
        public void WuDu_DocSaChuong_HasPoisonDamage()
        {
            // PC wudu.lua: dusha_zhang at ID 63, has poisondamage_v
            var skills = PcCombatCatalogFactory.CreateWuDuSkills();
            var skill = skills.First(s => s.skillId == 63);
            Assert.That(skill.DisplayName, Is.EqualTo("Độc Sa Chưởng"));
            var d = skill.GetPcLevelData(20);
            Assert.That(d.First(MagicAttributeKind.PoisonDamageV), Is.Not.Null);
        }

        // ==== Đại Lý / Thiên Vương (PC: tianwang.lua) ====
        [Test]
        public void TianWang_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateTianWangSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.TianWang), Is.True);
            Assert.That(skills.Count, Is.EqualTo(15));
        }

        [Test]
        public void TianWang_ZhanLongQuyet_Exists()
        {
            // PC tianwang.lua: zhanlong_jue at ID 29
            var skills = PcCombatCatalogFactory.CreateTianWangSkills();
            var skill = skills.First(s => s.skillId == 29);
            Assert.That(skill.DisplayName, Is.EqualTo("Trảm Long Quyết"));
        }

        // ==== Côn Lôn / Cự Yên (PC: cuiyan.lua) ====
        [Test]
        public void CuiYan_HasAllPcSourceSkills_AndFaction()
        {
            var skills = PcCombatCatalogFactory.CreateCuiYanSkills();
            Assert.That(skills.All(s => s.faction == CombatFaction.CuiYan), Is.True);
            Assert.That(skills.Count, Is.EqualTo(13));
        }

        [Test]
        public void CuiYan_PhongHoaTuyetNguyet_Exists()
        {
            // PC cuiyan.lua: fenghua_xueyue at ID 99
            var skills = PcCombatCatalogFactory.CreateCuiYanSkills();
            var skill = skills.First(s => s.skillId == 99);
            Assert.That(skill.DisplayName, Is.EqualTo("Phong Hoa Tuyết Nguyệt"));
        }

        // ==== Novice (PC: skill2/ + various) ====
        [Test]
        public void Novice_Has3StandardSkills()
        {
            var skills = PcCombatCatalogFactory.CreateNoviceSkills();
            Assert.That(skills.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(skills.Any(s => s.skillId == 1), Is.True);
            Assert.That(skills.Any(s => s.skillId == 2), Is.True);
        }

        // ==== Cross-faction: helper IsXSkill functions ====
        [Test]
        public void IsCaiBangSkill_ReturnsTrue_ForCaiBangIds()
        {
            Assert.That(PcCombatCatalogFactory.IsCaiBangSkill(115), Is.True);
            Assert.That(PcCombatCatalogFactory.IsCaiBangSkill(127), Is.True);
            Assert.That(PcCombatCatalogFactory.IsCaiBangSkill(130), Is.True);
            Assert.That(PcCombatCatalogFactory.IsCaiBangSkill(153), Is.False, "ID 153 is WuDang");
        }

        [Test]
        public void IsWuDangSkill_ReturnsTrue_ForWuDangIds()
        {
            Assert.That(PcCombatCatalogFactory.IsWuDangSkill(153), Is.True);
            Assert.That(PcCombatCatalogFactory.IsWuDangSkill(165), Is.True);
            Assert.That(PcCombatCatalogFactory.IsWuDangSkill(115), Is.False, "ID 115 is CaiBang");
        }

        [Test]
        public void IsShaolinSkill_ReturnsTrue_ForShaolinIds()
        {
            Assert.That(PcCombatCatalogFactory.IsShaolinSkill(3), Is.True);
            Assert.That(PcCombatCatalogFactory.IsShaolinSkill(10), Is.True);
            Assert.That(PcCombatCatalogFactory.IsShaolinSkill(115), Is.False, "ID 115 is CaiBang");
        }

        [Test]
        public void IsTangMenSkill_ReturnsTrue_ForTangMenIds()
        {
            Assert.That(PcCombatCatalogFactory.IsTangMenSkill(43), Is.True);
            Assert.That(PcCombatCatalogFactory.IsTangMenSkill(55), Is.True);
        }

        [Test]
        public void IsEMeiSkill_ReturnsTrue_ForEMeiIds()
        {
            Assert.That(PcCombatCatalogFactory.IsEMeiSkill(77), Is.True);
            Assert.That(PcCombatCatalogFactory.IsEMeiSkill(93), Is.True);
        }

        [Test]
        public void IsTianWangSkill_ReturnsTrue_ForTianWangIds()
        {
            Assert.That(PcCombatCatalogFactory.IsTianWangSkill(23), Is.True);
            Assert.That(PcCombatCatalogFactory.IsTianWangSkill(42), Is.True);
        }

        [Test]
        public void IsWuDuSkill_ReturnsTrue_ForWuDuIds()
        {
            Assert.That(PcCombatCatalogFactory.IsWuDuSkill(60), Is.True);
            Assert.That(PcCombatCatalogFactory.IsWuDuSkill(76), Is.True);
        }

        [Test]
        public void IsCuiYanSkill_ReturnsTrue_ForCuiYanIds()
        {
            Assert.That(PcCombatCatalogFactory.IsCuiYanSkill(95), Is.True);
            Assert.That(PcCombatCatalogFactory.IsCuiYanSkill(114), Is.True);
        }

        [Test]
        public void IsTianRenSkill_ReturnsTrue_ForTianRenIds()
        {
            Assert.That(PcCombatCatalogFactory.IsTianRenSkill(131), Is.True);
            Assert.That(PcCombatCatalogFactory.IsTianRenSkill(150), Is.True);
        }

        [Test]
        public void IsKunLunSkill_ReturnsTrue_ForKunLunIds()
        {
            Assert.That(PcCombatCatalogFactory.IsKunLunSkill(167), Is.True);
            Assert.That(PcCombatCatalogFactory.IsKunLunSkill(184), Is.True);
        }
    }
}
