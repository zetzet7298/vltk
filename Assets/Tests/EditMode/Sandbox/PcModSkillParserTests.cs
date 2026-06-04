using System.IO;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcModSkillParserTests
    {
        private static string ModSkillsPath => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/ModSkills.txt");

        [Test]
        public void ParseFile_LoadsExpansionSkillIdsFromPcModSkills()
        {
            var rows = PcModSkillParser.ParseFile(ModSkillsPath);

            Assert.GreaterOrEqual(rows.Count, 338);
            Assert.IsTrue(rows.Exists(r => r.skillId == 1216));
            Assert.IsTrue(rows.Exists(r => r.skillId == 1539));
            Assert.IsTrue(rows.Exists(r => r.skillId == 1554));
        }

        [Test]
        public void ToSkillDefinition_PreservesCorePcColumns()
        {
            var rows = PcModSkillParser.ParseFile(ModSkillsPath);
            var row = rows.Find(r => r.skillId == 1262); // Càn Khôn Vô Lượng
            var skill = PcModSkillParser.ToSkillDefinition(row);

            Assert.AreEqual(1262, skill.skillId);
            Assert.AreEqual(row.attackRadius, skill.attackRadius);
            Assert.AreEqual(row.childSkillId, skill.childSkillId);
            Assert.AreEqual(row.missilesGenerate, skill.missilesGenerate);
            Assert.AreEqual(row.missilesGenerateData, skill.missilesGenerateData);
            Assert.AreEqual(row.equipLimit, skill.equipLimit);
            Assert.AreEqual(row.horseLimit, skill.horseLimit);
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, skill.skillStyle);
            Assert.AreEqual(SkillMissileForm.None, skill.missileForm);
            Assert.IsNotNull(skill.pcLevelData);
            Assert.AreEqual(1, skill.pcLevelData.Count);
        }

        [Test]
        public void CreateCatalogFromFile_RegistersAllExpansionModSkills()
        {
            var catalog = PcModSkillParser.CreateCatalogFromFile(ModSkillsPath);

            Assert.GreaterOrEqual(catalog.Count, 338);
            Assert.IsNotNull(catalog.Resolve(1220));
            Assert.IsNotNull(catalog.Resolve(1336));
            Assert.IsNotNull(catalog.Resolve(1539));
            Assert.IsNotNull(catalog.Resolve(1554));
        }

        [Test]
        public void ModSkillCatalog_PreservesBuffAuraAndTargetFlags()
        {
            var catalog = PcModSkillParser.CreateCatalogFromFile(ModSkillsPath);
            var aura = catalog.Resolve(1322); // Hạ Nhược Vô Lực, style 14 in PC ModSkills

            Assert.IsNotNull(aura);
            Assert.IsTrue(aura.isAura);
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, aura.skillStyle);
            Assert.AreEqual(SkillMissileForm.None, aura.missileForm);
        }


        [Test]
        public void ToSkillDefinition_ResolvesModMissileSpriteFromChildSkillId()
        {
            PcMissileRegistry.ClearAndInitialize(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets"));
            var rows = PcModSkillParser.ParseFile(ModSkillsPath);
            var row = rows.Find(r => r.skillId == 1216); // PC ModSkills child missile 400

            Assert.IsNotNull(row);
            Assert.AreEqual(400, row.childSkillId);

            var skill = PcModSkillParser.ToSkillDefinition(row);

            Assert.AreEqual(SkillMissileForm.Surround, skill.missileForm);
            Assert.IsNotNull(skill.missileSpriteId);
            Assert.AreEqual("\\spr\\skill\\150\\wu\\wd_xingxiaoguli_c.spr", skill.missileSpriteId.sourcePath);
            Assert.AreEqual(ResourceKind.Sprite, skill.missileSpriteId.resourceKind);
        }

        [Test]
        public void ToSkillDefinition_MapsPcCharClassToFaction()
        {
            Assert.AreEqual(CombatFaction.Shaolin, PcModSkillParser.ToSkillDefinition(RowWithFaction(1, string.Empty)).faction);
            Assert.AreEqual(CombatFaction.EMei, PcModSkillParser.ToSkillDefinition(RowWithFaction(2, string.Empty)).faction);
            Assert.AreEqual(CombatFaction.TangMen, PcModSkillParser.ToSkillDefinition(RowWithFaction(3, string.Empty)).faction);
            Assert.AreEqual(CombatFaction.CaiBang, PcModSkillParser.ToSkillDefinition(RowWithFaction(4, string.Empty)).faction);
            Assert.AreEqual(CombatFaction.WuDang, PcModSkillParser.ToSkillDefinition(RowWithFaction(5, string.Empty)).faction);
        }

        [Test]
        public void ToSkillDefinition_PrefersLevelSetScriptFactionOverPcCharClassGroup()
        {
            Assert.AreEqual(CombatFaction.WuDu, PcModSkillParser.ToSkillDefinition(RowWithFaction(5, @"\script\skill\wudu.lua")).faction);
            Assert.AreEqual(CombatFaction.TianRen, PcModSkillParser.ToSkillDefinition(RowWithFaction(1, @"\script\skill\tianren.lua")).faction);
            Assert.AreEqual(CombatFaction.KunLun, PcModSkillParser.ToSkillDefinition(RowWithFaction(3, @"\script\skill\kunlun.lua")).faction);
            Assert.AreEqual(CombatFaction.CuiYan, PcModSkillParser.ToSkillDefinition(RowWithFaction(4, @"\script\skill\cuiyan.lua")).faction);
        }

        private static PcModSkillRow RowWithFaction(int charClass, string levelSetScript) => new PcModSkillRow
        {
            skillName = "Test Skill",
            skillId = 2000,
            charClass = charClass,
            levelSetScript = levelSetScript,
            maxLevel = 1,
        };

        [Test]
        public void PcCombatCatalogFactory_CanMergeBaseAndModSkillCatalogs()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceCoreSectAndModCatalog(ModSkillsPath);

            Assert.IsNotNull(catalog.Resolve(115)); // base Cái Bang
            Assert.IsNotNull(catalog.Resolve(151)); // base Võ Đang
            Assert.IsNotNull(catalog.Resolve(1216)); // expansion ModSkills
            Assert.IsNotNull(catalog.Resolve(1539)); // expansion Thiên Hạ Vô Cẩu
            Assert.GreaterOrEqual(catalog.Count, 510);
        }
    }
}
