using System.Collections.Generic;
using System.IO;
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
            Assert.That(catalog.Resolve(165).childSkillNum, Is.EqualTo(8), "PC wudang.lua: skill_misslenum_v {{1,1},{20,8}}");
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
            Assert.That(xuanyi.GetPcLevelData(20).First(MagicAttributeKind.LightingDamageV).ToString(), Is.EqualTo("LightingDamageV=10,0,100"), "PC wudang.lua xuanyi_wuxiang: lightingdamage_v[1]={{1,1},{20,10}}, [3]={{1,10},{20,100}}");
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
            Assert.That(fx153.pcPreCastSpriteKey, Is.EqualTo("3dfcabc2"), "jx-pc SPR hash for WuDang precast");
            Assert.That(fx153.pcMissileSpriteKey, Is.EqualTo("c9ba5bf1"), "jx-pc SPR hash for WuDang missile");
            Assert.That(fx153.pcMissileSpeedPerTick, Is.EqualTo(20));
            Assert.That(fx153.pcMissileLifeTicks, Is.EqualTo(16));

            var fx165 = visual.PlaySkillCast(catalog.Resolve(165), UnityEngine.Vector2.zero, new UnityEngine.Vector2(300, 0), 20);
            Assert.That(fx165.pcMissileSpriteKey, Is.EqualTo("01744d1a"), "jx-pc SPR hash for WuDang 165 missile");
            Assert.That(fx165.pcMissileSpeedPerTick, Is.EqualTo(20));
            Assert.That(fx165.pcMissileLifeTicks, Is.EqualTo(16));
            Assert.That(fx165.missileCount, Is.EqualTo(8), "PC wudang.lua L20: 8 missiles");
        }


        [Test]
        public void WuDangExtractedSprFiles_ArePresentForRuntimeKeys()
        {
            // SPRs now live in project root /SpritesRuntime (outside Assets/ to keep Unity import fast).
            var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                UnityEngine.Application.dataPath, "..", "SpritesRuntime"));
            foreach (var key in new[] { "3dfcabc2", "c9ba5bf1", "55542141", "01744d1a", "8de48699" })
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


        // CTS-04: catalog drives from PC source via PcSkillFullParser (ReadLinesTcvn3,
        // TCVN3, not GBK auto-detect). Verify all 16 PC WuDang skill IDs (151-166)
        // are present in the TCVN3-decoded PC skills.txt, with non-mojibake
        // Vietnamese names (no U+FFFD replacement char).
        [Test]
        public void WuDangSkillIds_ArePresentInPcSourceTcvn3Catalog()
        {
            var dir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/StreamingAssets/Reference/PcSkill");
            var reg = PcSkillRegistry.LoadFromDirectory(dir);
            Assert.GreaterOrEqual(reg.Count, 1, "PcSkillRegistry must load at least 1 row from skills.txt");

            var wuDangIds = new[] { 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166 };
            foreach (var id in wuDangIds)
            {
                var s = reg.Resolve(id);
                Assert.IsNotNull(s, $"PC source must contain WuDang skill id={id} (loaded via ReadLinesTcvn3)");

                string name = s.nameNormalized ?? string.Empty;
                Assert.IsFalse(name.Contains('\uFFFD'),
                    $"nameRaw must not contain U+FFFD (mojibake); got {name} for id={id} — " +
                    "do NOT switch ReadLinesTcvn3 back to GBK auto-detect for skills.txt");
                Assert.IsTrue(name.Length > 0,
                    $"name must be non-empty for id={id} (skills.txt is TCVN3 Vietnamese)");
            }
        }


        // CTS-04: skill ids match between PcCombatCatalogFactory and the canonical
        // PC source (PcSkillFullParser). Both sides must agree on 151-166, proving
        // the runtime catalog is in lockstep with the PC reference rather than a
        // hand-maintained copy that could drift.
        [Test]
        public void WuDangFactorySkillIds_MatchPcSourceRegistry()
        {
            var dir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/StreamingAssets/Reference/PcSkill");
            var reg = PcSkillRegistry.LoadFromDirectory(dir);

            var factoryIds = PcCombatCatalogFactory.CreateWuDangSkills()
                .Select(s => s.skillId)
                .OrderBy(id => id)
                .ToArray();

            // Walk the WuDang PC id range (151-166) and collect everything the
            // PC source registry can resolve. PcSkillRegistry has no public key
            // collection, so we go through Resolve() instead of touching private state.
            var pcSourceIds = new List<int>();
            for (int id = PcCombatCatalogFactory.WuDangMinSkillId;
                     id <= PcCombatCatalogFactory.WuDangMaxSkillId;
                     id++)
            {
                if (reg.Resolve(id) != null) pcSourceIds.Add(id);
            }

            Assert.That(factoryIds, Is.EquivalentTo(pcSourceIds),
                "PcCombatCatalogFactory.CreateWuDangSkills() ids must match the PC source " +
                "registry's WuDang range (151-166). Drift = someone added/removed a skill " +
                "in one place but not the other.");
        }
    }
}
