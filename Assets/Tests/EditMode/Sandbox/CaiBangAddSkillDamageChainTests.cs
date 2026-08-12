using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-AddSkillDamage 2026-06-29] PC gaibang.lua::addskilldamageN.
    // ENGINE TRUTH (KSkillList::GetAddSkillDamage + KNpc::AppendSkillEffect, MAX_PERCENT=100):
    //   addskilldamage is a PASSIVE flat %-damage amplifier, NOT a proc that casts a sub-skill.
    //   Learning skill G adds G.addskilldamageN[3]% to the damage of the skill G.addskilldamageN[1]
    //   points at, WHEN that target skill is cast. No RNG, no extra missiles/visual.
    // These tests assert the PC percent VALUES per skill/slot (slot[3] = the damage % bonus):
    //   119 (yanmen_tuobo)      → 359, +40% (asd2 → 125, +35% / asd3 → 1074, +32%)
    //   122 (jianren_shenshou)  → 357, +50% (asd3 → 1073, +40% / asd4 → 1101, +40%)
    //   125 (bangda_egou)       → 359, +60% AND 1074, +50%
    //   128 (kanglong_youhui)   → 357, +55% (asd2 → 1073, +45% / asd3 → 1101, +45%)
    //   357 (feilong_zaitian)   → 1073, +25% AND 1101, +25%  [fix 2026-07-17: thiếu 357 trong grants]
    //   359 (tianxia_wugou)     → 1074, +25%
    [TestFixture, Category("CaiBang")]
    public class CaiBangAddSkillDamageChainTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        [Test]
        public void YanmenTuobo_L20Chance40_Target359()
        {
            if (!PcCaiBangLuaLevelService.Applies(119))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(119, 20, "addskilldamage1", 3);
            Assert.AreEqual(40, chance, "PC yanmen_tuobo addskilldamage1[3] L20=40");
        }

        [Test]
        public void JianrenShenshou_L20Chance50_Target357()
        {
            if (!PcCaiBangLuaLevelService.Applies(122))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(122, 20, "addskilldamage1", 3);
            Assert.AreEqual(50, chance, "PC jianren_shenshou addskilldamage1[3] L20=50");
        }

        [Test]
        public void BangDaEgou_L20Chances60And50_Target359And1074()
        {
            if (!PcCaiBangLuaLevelService.Applies(125))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance1 = PcCaiBangLuaLevelService.GetSingleValue(125, 20, "addskilldamage1", 3);
            int chance2 = PcCaiBangLuaLevelService.GetSingleValue(125, 20, "addskilldamage2", 3);
            Assert.AreEqual(60, chance1, "PC newest bangda_egou addskilldamage1[3] L20=60 → target 359");
            Assert.AreEqual(50, chance2, "PC newest bangda_egou addskilldamage2[3] L20=50 → target 1074");
        }

        [Test]
        public void TianxiaWugou_L20Chance25_Target1074()
        {
            if (!PcCaiBangLuaLevelService.Applies(359))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(359, 20, "addskilldamage1", 3);
            Assert.AreEqual(25, chance, "PC tianxia_wugou addskilldamage1[3] L20=25");
        }

        [Test]
        public void KanglongYouhui_L20Chance55_Target357()
        {
            if (!PcCaiBangLuaLevelService.Applies(128))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int chance = PcCaiBangLuaLevelService.GetSingleValue(128, 20, "addskilldamage1", 3);
            Assert.AreEqual(55, chance, "PC kanglong_youhui addskilldamage1[3] L20=55");
        }

        [Test]
        public void FeilongZaitian_L20Chance25_Target1073()
        {
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int target = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "addskilldamage1", 1);
            int chance = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "addskilldamage1", 3);
            Assert.AreEqual(1073, target, "PC feilong_zaitian addskilldamage1[1] L20 target=1073");
            Assert.AreEqual(25, chance, "PC feilong_zaitian addskilldamage1[3] L20=25");
        }

        [Test]
        public void FeilongZaitian_L20Chance25_Target1101()
        {
            if (!PcCaiBangLuaLevelService.Applies(357))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int target = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "addskilldamage2", 1);
            int chance = PcCaiBangLuaLevelService.GetSingleValue(357, 20, "addskilldamage2", 3);
            Assert.AreEqual(1101, target, "PC feilong_zaitian addskilldamage2[1] L20 target=1101");
            Assert.AreEqual(25, chance, "PC feilong_zaitian addskilldamage2[3] L20=25");
        }

        [Test]
        public void Runtime_Learned357_Grants25PercentTo1073And1101()
        {
            // [CaiBang-AddSkillDamage 2026-07-17] Trước fix: AddSkillDamageGrants thiếu 357 →
            // 1073/1101 cast ra addSkillDamagePercent=0 cho caster học 357 (sai PC +25% mỗi skill).
            // Sau fix: 1073 = 122.asd3(+40) + 128.asd2(+45) + 357.asd1(+25) = 110;
            //         1101 = 122.asd4(+40) + 128.asd3(+45) + 357.asd2(+25) = 110.
            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });
            var caster = new CombatActorState
            {
                actorId = 7,
                faction = CombatFaction.CaiBang,
                level = 80,
                fightMode = true,
                currentMana = 5000,
                position = Vector2.zero,
                knownSkills = { 122, 128, 357, 1073, 1101 },
                skillLevels = { [122] = 20, [128] = 20, [357] = 20, [1073] = 20, [1101] = 20 },
            };
            foreach (var castId in new[] { 1073, 1101 })
            {
                var enemy = new CombatActorState
                {
                    actorId = 9,
                    faction = CombatFaction.None,
                    level = 1,
                    currentLife = 100000,
                    minDamage = 10,
                    maxDamage = 20,
                    position = new Vector2(100, 0),
                };
                var r = svc.Cast(caster, enemy, castId, enemy.position, CombatRelation.Enemy);
                Assert.IsTrue(r.success, r.detail);
                Assert.AreEqual(110, r.addSkillDamagePercent,
                    $"PC: casting {castId} with learned 122/128/357 must sum 40+45+25 = 110%");
            }
        }

        [Test]
        public void ChainTargets_AllRegisteredInCatalog()
        {
            // Chain targets 357/359/1074 phải có trong catalog.
            var cat = Catalog();
            Assert.IsNotNull(cat.Resolve(357), "chain target 357 (Phi Long) missing");
            Assert.IsNotNull(cat.Resolve(359), "chain target 359 (Bổng Đả Ác Cẩu) missing");
            Assert.IsNotNull(cat.Resolve(1074), "chain target 1074 (Phi Long Tại Thiên tier 2) missing");
            Assert.IsNotNull(cat.Resolve(1101), "chain target 1101 (Thừa Lục Long đa mục tiêu) missing");
        }
    }
}
