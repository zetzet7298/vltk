// -----------------------------------------------------------------------------
// VLTK Mobile — Parser tests cho Faction + Guild + Battle registries (batch 10).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcFactionSkillTreeRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcFactionSkillTreeRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var reg = new PcFactionSkillTreeRegistry();
            reg.Register(new PcFactionSkillTreeEntry
            {
                factionId = 0, skillId = 100, tier = 1,
            });
            reg.Register(new PcFactionSkillTreeEntry
            {
                factionId = 1, skillId = 101, tier = 1,
            });
            var list0 = reg.GetByFaction(0);
            Assert.That(list0.Count, Is.EqualTo(1));
        }
    }

    public class PcFactionBonusRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcFactionBonusRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var reg = new PcFactionBonusRegistry();
            reg.Register(new PcFactionBonusEntry
            {
                factionId = 0, level = 10, hpBonus = 100,
            });
            reg.Register(new PcFactionBonusEntry
            {
                factionId = 1, level = 10, hpBonus = 200,
            });
            var list0 = reg.GetByFaction(0);
            Assert.That(list0.Count, Is.EqualTo(1));
            Assert.That(list0[0].hpBonus, Is.EqualTo(100));
        }
    }

    public class PcFactionRelationRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcFactionRelationRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Get_ReturnsNullForInvalid()
        {
            var reg = new PcFactionRelationRegistry();
            Assert.That(reg.Get(9999), Is.Null);
        }
    }

    public class PcGuildScriptRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcGuildScriptRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcGuildScriptRegistry();
            reg.Register(new PcGuildScriptEntry
            {
                scriptId = 1, type = GuildScriptType.Create,
            });
            reg.Register(new PcGuildScriptEntry
            {
                scriptId = 2, type = GuildScriptType.Donate,
            });
            var list = reg.GetByType(GuildScriptType.Create);
            Assert.That(list.Count, Is.EqualTo(1));
        }
    }

    public class PcBattleMapConfigRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcBattleMapConfigRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByBattleType_FiltersCorrectly()
        {
            var reg = new PcBattleMapConfigRegistry();
            reg.Register(new PcBattleMapConfigEntry
            {
                battleMapId = 1, battleType = (int)BattleType.TongKim,
            });
            reg.Register(new PcBattleMapConfigEntry
            {
                battleMapId = 2, battleType = (int)BattleType.QuocChien,
            });
            var list = reg.GetByBattleType((int)BattleType.TongKim);
            Assert.That(list.Count, Is.EqualTo(1));
        }
    }

    public class PcBattleRewardConfigRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcBattleRewardConfigRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetForRank_FiltersCorrectly()
        {
            var reg = new PcBattleRewardConfigRegistry();
            reg.Register(new PcBattleRewardConfigEntry
            {
                rewardId = 1, requiredRank = 1, winGold = 100,
            });
            reg.Register(new PcBattleRewardConfigEntry
            {
                rewardId = 2, requiredRank = 2, winGold = 200,
            });
            var rank1 = reg.GetForRank(1);
            Assert.That(rank1.Count, Is.EqualTo(1));
            Assert.That(rank1[0].winGold, Is.EqualTo(100));
        }
    }

    public class PcBattleHonorRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcBattleHonorRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByBattleType_FiltersCorrectly()
        {
            var reg = new PcBattleHonorRegistry();
            reg.Register(new PcBattleHonorEntry
            {
                honorId = 1, battleType = (int)BattleType.TongKim, name = "Tướng Quân",
            });
            reg.Register(new PcBattleHonorEntry
            {
                honorId = 2, battleType = (int)BattleType.Boss, name = "Thành Chủ",
            });
            var list = reg.GetByBattleType((int)BattleType.TongKim);
            Assert.That(list.Count, Is.EqualTo(1));
        }
    }

    public class PcSjBattleRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcSjBattleRegistry();
            Assert.That(reg.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetByTier_FiltersCorrectly()
        {
            var reg = new PcSjBattleRegistry();
            reg.Register(new PcSjBattleEntry
            {
                tierId = 1, tier = SongJinTier.So, name = "Sơ Cấp 1",
            });
            reg.Register(new PcSjBattleEntry
            {
                tierId = 2, tier = SongJinTier.Cao, name = "Cao Cấp 1",
            });
            var list = reg.GetByTier(SongJinTier.So);
            Assert.That(list.Count, Is.EqualTo(1));
        }
    }
}
