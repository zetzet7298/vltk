// -----------------------------------------------------------------------------
// VLTK Mobile — Extended System Parser Tests
// Coverage: PcMissileEffect, ShopConfig, TaskFlagConfig, HudArt, BattleScript.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissileEffectRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcMissileEffectRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void Get_ReturnsNullForInvalid()
        {
            var reg = new PcMissileEffectRegistry();
            Assert.IsNull(reg.Get(99999));
        }
    }

    public class ShopConfigRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new ShopConfigRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void GetByShop_FiltersCorrectly()
        {
            var reg = new ShopConfigRegistry();
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 100 });
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 101 });
            reg.Register(new ShopConfigEntry { shopId = 2, itemId = 200 });
            var hits = reg.GetByShop(1);
            Assert.AreEqual(2, hits.Count);
        }
    }

    public class TaskFlagConfigRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new TaskFlagConfigRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new TaskFlagConfigRegistry();
            reg.Register(new TaskFlagConfigEntry { flagId = 1, taskType = 0 });
            reg.Register(new TaskFlagConfigEntry { flagId = 2, taskType = 2 });
            reg.Register(new TaskFlagConfigEntry { flagId = 3, taskType = 0 });
            var hits = reg.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }
    }

    public class HudArtRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new HudArtRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new HudArtRegistry();
            reg.Register(new HudArtEntry { artId = 1, type = 0 });
            reg.Register(new HudArtEntry { artId = 2, type = 1 });
            reg.Register(new HudArtEntry { artId = 3, type = 0 });
            var hits = reg.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }
    }

    public class BattleScriptRegistryTests
    {
        [Test]
        public void Count_NonNegative()
        {
            var reg = new PcBattleScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void GetByTrigger_FiltersCorrectly()
        {
            var reg = new PcBattleScriptRegistry();
            reg.Register(new PcBattleScriptEntry { scriptId = 1, triggerType = 0 });
            reg.Register(new PcBattleScriptEntry { scriptId = 2, triggerType = 2 });
            reg.Register(new PcBattleScriptEntry { scriptId = 3, triggerType = 0 });
            var hits = reg.GetByTriggerType(0);
            int n = 0;
            foreach (var _ in hits) n++;
            Assert.AreEqual(2, n);
        }
    }
}
