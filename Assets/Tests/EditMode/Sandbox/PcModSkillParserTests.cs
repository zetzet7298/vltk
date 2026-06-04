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

        [TestCase(1216, "Đăng Điệp Đại Chiêu Âm Tào Địa Trảo")]
        [TestCase(1262, "Càn Khôn Vô Lượng")]
        [TestCase(1322, "Hư Nhược Vô Lực")]
        [TestCase(1539, "Thiên Hạ Vô Cẩu")]
        public void ToSkillDefinition_NormalizesRepresentativeModSkillNamesForVietnameseUi(int skillId, string expectedName)
        {
            var rows = PcModSkillParser.ParseFile(ModSkillsPath);
            var row = rows.Find(r => r.skillId == skillId);

            Assert.IsNotNull(row, $"Missing PC ModSkills row {skillId}.");

            var skill = PcModSkillParser.ToSkillDefinition(row);

            Assert.AreEqual(expectedName, skill.nameNormalized);
            Assert.IsFalse(skill.nameNormalized.Contains("�"), $"Skill {skillId} must not expose Unicode replacement characters in UI.");
            Assert.IsFalse(ContainsCjk(skill.nameNormalized), $"Skill {skillId} must be Vietnamese, not CJK, in UI.");
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
            var rows = PcModSkillParser.ParseFile(ModSkillsPath);
            var caiBangRow = rows.Find(r => r.charClass == 4);
            var tianWangRow = rows.Find(r => r.charClass == 2);
            var tangMenRow = rows.Find(r => r.charClass == 3);
            var shaolinRow = rows.Find(r => r.charClass == 1);
            var wuDuRow = rows.Find(r => r.charClass == 5);

            Assert.IsNotNull(caiBangRow, "PC ModSkills.txt includes Cái Bang CharClass=4 rows.");
            Assert.IsNotNull(tianWangRow, "PC ModSkills.txt includes Thiên Vương CharClass=2 rows.");
            Assert.IsNotNull(tangMenRow, "PC ModSkills.txt includes Đường Môn CharClass=3 rows.");
            Assert.IsNotNull(shaolinRow, "PC ModSkills.txt includes Thiếu Lâm CharClass=1 rows.");
            Assert.IsNotNull(wuDuRow, "PC ModSkills.txt includes Ngũ Độc CharClass=5 rows.");

            Assert.AreEqual(CombatFaction.CaiBang, PcModSkillParser.ToSkillDefinition(caiBangRow).faction);
            Assert.AreEqual(CombatFaction.TianWang, PcModSkillParser.ToSkillDefinition(tianWangRow).faction);
            Assert.AreEqual(CombatFaction.TangMen, PcModSkillParser.ToSkillDefinition(tangMenRow).faction);
            Assert.AreEqual(CombatFaction.Shaolin, PcModSkillParser.ToSkillDefinition(shaolinRow).faction);
            Assert.AreEqual(CombatFaction.WuDu, PcModSkillParser.ToSkillDefinition(wuDuRow).faction);
        }

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

        private static bool ContainsCjk(string text)
        {
            foreach (var ch in text)
            {
                if ((ch >= '\u3400' && ch <= '\u4dbf')
                    || (ch >= '\u4e00' && ch <= '\u9fff')
                    || (ch >= '\uf900' && ch <= '\ufaff'))
                    return true;
            }

            return false;
        }
    }
}
