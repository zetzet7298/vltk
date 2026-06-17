using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWeaponThiefSkillSourceTests
    {
        private const string PcUpdate27SkillDir =
            "/var/www/jx-source/pak_unpacked/dmjx03/settings";

        private static string ClientWeaponSkillPath => Path.Combine(PcUpdate27SkillDir, "clientweaponskill.txt");
        private static string ThiefSkillPath => Path.Combine(PcUpdate27SkillDir, "thiefskill.txt");
        private static string SkillsPath => Path.Combine(PcUpdate27SkillDir, "skills.txt");

        [Test]
        public void ClientWeaponSkill_ParsesUpdate27SourceCountAndRepresentativeRows()
        {
            Assert.IsTrue(ClientWeaponSkillPath.Contains("/jx-source/"), "Path should be under jx-source canonical tree.");
            var rows = PcClientWeaponSkillParser.ParseFile(ClientWeaponSkillPath);
            var header = PcItemCommon.ReadServerLines(ClientWeaponSkillPath)[0].Split('\t');

            Assert.AreEqual(new[] { "Id", "WeaponType", "SkillId" }, header);
            Assert.AreEqual(32, rows.Count, "PC clientweaponskill.txt has exactly 32 data rows.");
            Assert.AreEqual(1, rows[0].id);
            Assert.AreEqual(53, rows[0].skillId);
            Assert.AreEqual(32, rows[31].id);
            Assert.AreEqual(53, rows[31].skillId);
            Assert.AreEqual(1, rows[7].skillId);
            Assert.AreEqual(2, rows[19].skillId);
            Assert.IsFalse(string.IsNullOrEmpty(rows[0].weaponTypeName));
        }

        [Test]
        public void ClientWeaponSkill_RegistryLinksWeaponRowsToPcSkillsScriptAndSafeLookups()
        {
            var registry = PcClientWeaponSkillParser.BuildRegistry(PcUpdate27SkillDir);

            Assert.AreEqual(32, registry.Count);
            Assert.AreEqual(21, registry.GetBySkillId(53).Count);
            Assert.AreEqual(10, registry.GetBySkillId(1).Count);
            Assert.AreEqual(1, registry.GetBySkillId(2).Count);
            Assert.AreEqual("\\script\\skill\\special\\近程物理攻击.lua", registry.Get(1).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\长兵物理攻击.lua", registry.Get(8).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\远程物理攻击.lua", registry.Get(20).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\近程物理攻击.lua", registry.Get(32).lvlSetScript);
            Assert.IsNull(registry.Get(0));
            Assert.AreEqual(0, registry.GetBySkillId(-1).Count);
            Assert.AreEqual(0, PcClientWeaponSkillParser.ParseFile("/tmp/missing-clientweaponskill.txt").Count);
        }

        [Test]
        public void ThiefSkill_ParsesUpdate27SourceCountAndRepresentativeRows()
        {
            Assert.IsTrue(ThiefSkillPath.Contains("/jx-source/"), "Path should be under jx-source canonical tree.");
            var rows = PcThiefSkillParser.ParseFile(ThiefSkillPath);
            var header = PcItemCommon.ReadServerLines(ThiefSkillPath)[0].Split('\t');

            Assert.AreEqual(17, header.Length);
            Assert.AreEqual(4, rows.Count, "PC thiefskill.txt has exactly 4 data rows.");
            Assert.AreEqual(new[] { 400, 401, 402, 403 }, rows.Select(r => r.skillId).ToArray());

            var stealMoney = rows[0];
            Assert.AreEqual(0, stealMoney.thiefStyle);
            Assert.AreEqual(100, stealMoney.attackRadius);
            Assert.AreEqual(1, stealMoney.maxLevel);
            Assert.AreEqual(100, stealMoney.timePerCast);
            Assert.AreEqual(50, stealMoney.cost);
            // PC source has quotes around the targetMovieInfo value: "10,1,1"
            Assert.AreEqual("\"10,1,1\"", stealMoney.targetMovieInfo);
            Assert.IsTrue(stealMoney.movie.EndsWith("劫富济贫1.spr"));
            Assert.IsTrue(stealMoney.skillSound.EndsWith("劫富济贫.wav"));

            Assert.AreEqual(50, rows[1].thiefPercent);
            Assert.IsTrue(string.IsNullOrEmpty(rows[1].movie));
        }

        [Test]
        public void ThiefSkill_RegistryLinksSkillIdsToPcSkillsScriptsAndSafeLookups()
        {
            var registry = PcThiefSkillParser.BuildRegistry(PcUpdate27SkillDir);

            Assert.AreEqual(4, registry.Count);
            Assert.AreEqual(400, registry.GetByThiefStyle(0).skillId);
            Assert.AreEqual(403, registry.GetByThiefStyle(3).skillId);
            Assert.AreEqual("\\script\\skill\\kunlun.lua", registry.Get(400).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\401.lua", registry.Get(401).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\402.lua", registry.Get(402).lvlSetScript);
            Assert.AreEqual("\\script\\skill\\special\\403.lua", registry.Get(403).lvlSetScript);
            Assert.IsNull(registry.Get(999999));
            Assert.IsNull(registry.GetByThiefStyle(-1));
            Assert.AreEqual(0, PcThiefSkillParser.ParseFile("/tmp/missing-thiefskill.txt").Count);
        }

        [Test]
        public void SkillSourceLinkParser_UsesSkillsTxtSkillIdColumnNotRowNumber()
        {
            var scripts = PcSkillSourceLinkParser.ParseSkillScripts(SkillsPath);

            Assert.AreEqual("\\script\\skill\\special\\长兵物理攻击.lua", scripts[1]);
            Assert.AreEqual("\\script\\skill\\special\\远程物理攻击.lua", scripts[2]);
            Assert.AreEqual("\\script\\skill\\special\\近程物理攻击.lua", scripts[53]);
            Assert.AreEqual("\\script\\skill\\kunlun.lua", scripts[400]);
            Assert.AreEqual("\\script\\skill\\special\\401.lua", scripts[401]);
            Assert.IsFalse(scripts.ContainsKey(0));
            Assert.AreEqual(0, PcSkillSourceLinkParser.ParseSkillScripts("/tmp/missing-skills.txt").Count);
        }
    }
}
