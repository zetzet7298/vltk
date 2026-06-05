using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WuDangCombatCatalogTests
    {
        [Test]
        public void CreateWuDangSkills_UsesPcSourceRanges()
        {
            var skills = PcCombatCatalogFactory.CreateWuDangSkills();

            var ids = skills.Select(s => s.skillId).ToArray();
            Assert.That(ids, Is.EquivalentTo(new[] { 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166 }));
            Assert.That(skills.All(s => s.faction == CombatFaction.WuDang), Is.True);

            var nulei = skills.First(s => s.skillId == 153);
            Assert.That(nulei.DisplayName, Is.EqualTo("Nộ Lôi Chỉ"));
            Assert.That(nulei.attackRadius, Is.EqualTo(400));
            Assert.That(nulei.childSkillId, Is.EqualTo(24));
            Assert.That(nulei.effectSourceId.sourcePath, Is.EqualTo("\\spr\\skill\\昆仑\\kl_16_魔法施放.spr"));
            Assert.That(nulei.missileSpriteId.sourcePath, Is.EqualTo("\\spr\\skill\\武当\\wd_01_怒雷指.spr"));
            Assert.That(nulei.GetPcLevelData(20).First(MagicAttributeKind.LightingDamageV).value3, Is.EqualTo(75));
            Assert.That(nulei.GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).value1, Is.EqualTo(20));
        }

        [Test]
        public void CoreSectCatalog_IncludesCaiBangAndWuDangRuntimeSkills()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

            Assert.That(catalog.Resolve(128)?.faction, Is.EqualTo(CombatFaction.CaiBang));
            Assert.That(catalog.Resolve(165)?.faction, Is.EqualTo(CombatFaction.WuDang));
            Assert.That(catalog.Resolve(165).childSkillId, Is.EqualTo(29));
            Assert.That(catalog.Resolve(165).childSkillNum, Is.EqualTo(16));
            Assert.That(catalog.Resolve(165).missileSpriteId.sourcePath, Is.EqualTo("\\spr\\skill\\武当\\wd_10_无我无剑.spr"));
            Assert.That(catalog.Resolve(165).GetPcLevelData(20).First(MagicAttributeKind.LightingDamageV).value3, Is.EqualTo(752));
        }

        [Test]
        public void WuDangPassiveAndMissingActives_MatchPcLuaLevel20()
        {
            var skills = PcCombatCatalogFactory.CreateWuDangSkills();
            Assert.That(skills.First(s => s.skillId == 154).GetPcLevelData(20).First(MagicAttributeKind.LightingResP).ToString(), Is.EqualTo("LightingResP=40,-1,0"));
            Assert.That(skills.First(s => s.skillId == 156).GetPcLevelData(20).First(MagicAttributeKind.ManaMaxP).ToString(), Is.EqualTo("ManaMaxP=245,-1,0"));
            Assert.That(skills.First(s => s.skillId == 161).GetPcLevelData(20).First(MagicAttributeKind.CastSpeedV).value1, Is.EqualTo(105));

            var xuanyi = skills.First(s => s.skillId == 162);
            Assert.That(xuanyi.childSkillId, Is.EqualTo(27));
            Assert.That(xuanyi.attackRadius, Is.EqualTo(520));
            Assert.That(xuanyi.GetPcLevelData(20).First(MagicAttributeKind.LightingDamageV).ToString(), Is.EqualTo("LightingDamageV=144,0,1476"));
            Assert.That(xuanyi.GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString(), Is.EqualTo("SkillCostV=80,0,0"));

            var renjian = skills.First(s => s.skillId == 163);
            Assert.That(renjian.childSkillId, Is.EqualTo(215));
            Assert.That(renjian.GetPcLevelData(20).First(MagicAttributeKind.PhysicsEnhanceP).value1, Is.EqualTo(194));
            Assert.That(renjian.GetPcLevelData(20).First(MagicAttributeKind.LightingDamageV).value3, Is.EqualTo(268));
            Assert.That(renjian.GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).value1, Is.EqualTo(60));
        }


        [Test]
        public void WuDangVisualService_UsesPcMissileSpeedLifeAndSpriteKeys()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var visual = new SkillEffectVisualService(null, catalog);
            var fx153 = visual.PlaySkillCast(catalog.Resolve(153), UnityEngine.Vector2.zero, new UnityEngine.Vector2(300, 0), 20);
            Assert.That(fx153.pcPreCastSpriteKey, Is.EqualTo("42ed0184"));
            Assert.That(fx153.pcMissileSpriteKey, Is.EqualTo("5698379e"));
            Assert.That(fx153.pcMissileSpeedPerTick, Is.EqualTo(20));
            Assert.That(fx153.pcMissileLifeTicks, Is.EqualTo(16));

            var fx165 = visual.PlaySkillCast(catalog.Resolve(165), UnityEngine.Vector2.zero, new UnityEngine.Vector2(300, 0), 20);
            Assert.That(fx165.pcMissileSpriteKey, Is.EqualTo("7bcefae7"));
            Assert.That(fx165.pcMissileSpeedPerTick, Is.EqualTo(20));
            Assert.That(fx165.pcMissileLifeTicks, Is.EqualTo(16));
            Assert.That(fx165.missileCount, Is.EqualTo(16));
        }


        [Test]
        public void WuDangExtractedSprFiles_ArePresentForRuntimeKeys()
        {
            var root = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Sprites");
            foreach (var key in new[] { "42ed0184", "5698379e", "55542141", "7bcefae7", "8de48699" })
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(root, key + ".spr")), Is.True, key);
        }


        [Test]
        public void WuDangTaiJiShenGong_UsesPcPassiveScaling()
        {
            var taiji = PcCombatCatalogFactory.CreateWuDangSkills().First(s => s.skillId == 166);
            var level30 = taiji.GetPcLevelData(30);

            Assert.That(taiji.DisplayName, Is.EqualTo("Thái Cực Thần Công"));
            Assert.That(taiji.maxLevel, Is.EqualTo(30));
            Assert.That(level30.First(MagicAttributeKind.AttackSpeedV).value1, Is.EqualTo(65));
            Assert.That(level30.First(MagicAttributeKind.ManaMaxP).value1, Is.EqualTo(245));
            Assert.That(level30.First(MagicAttributeKind.LightingEnhanceP).value1, Is.EqualTo(100));
        }
    }
}
