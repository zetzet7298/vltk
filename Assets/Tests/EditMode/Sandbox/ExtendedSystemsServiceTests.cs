// -----------------------------------------------------------------------------
// VLTK Mobile — Extended Systems Service Tests
// Coverage: MissileEffect, ShopConfig, TaskFlagRegistry, HudArtCatalog,
//           FactionMapRuntime, BattleScriptRuntime (PC-parity batch 8).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MissileEffectServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MissileEffectService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcMissileEffectRegistry();
            reg.Register(new PcMissileEffectEntry { missleId = 1, effectType = 0, name = "Chém" });
            reg.Register(new PcMissileEffectEntry { missleId = 2, effectType = 1, name = "AOE" });
            reg.Register(new PcMissileEffectEntry { missleId = 3, effectType = 0, name = "Chém 2" });
            var hits = reg.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void StopAll_DoesNotThrow()
        {
            var svc = new MissileEffectService();
            Assert.DoesNotThrow(() => svc.StopAll());
            Assert.AreEqual(0, svc.LiveCount);
        }
    }

    public class ShopConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ShopConfigService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetItemsForShop_ReturnsList()
        {
            var reg = new ShopConfigRegistry();
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 100, price = 50 });
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 101, price = 100 });
            reg.Register(new ShopConfigEntry { shopId = 2, itemId = 200, price = 200 });
            var svc = new ShopConfigService(reg);
            var items = svc.GetItemsForShop(1);
            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void TryBuy_RejectsInvalid()
        {
            var reg = new ShopConfigRegistry();
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 100, price = 50, requiredLevel = 10, stock = 5 });
            var svc = new ShopConfigService(reg);
            Assert.IsFalse(svc.TryBuy(1, 100, 1, playerLevel: 5));
            Assert.IsFalse(svc.TryBuy(1, 100, 10, playerLevel: 20));
            Assert.IsFalse(svc.TryBuy(0, 0, 1, playerLevel: 50));
            Assert.IsTrue(svc.TryBuy(1, 100, 1, playerLevel: 20));
        }

        [Test]
        public void GetRestockTime_NonNegative()
        {
            var reg = new ShopConfigRegistry();
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 100, restockSec = 300 });
            reg.Register(new ShopConfigEntry { shopId = 1, itemId = 101, restockSec = 0 });
            var svc = new ShopConfigService(reg);
            int t = svc.GetRestockTime(1);
            Assert.GreaterOrEqual(t, 0);
            Assert.AreEqual(300, t);
        }
    }

    public class TaskFlagRegistryServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskFlagRegistryService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new TaskFlagConfigRegistry();
            reg.Register(new TaskFlagConfigEntry { flagId = 1, taskType = 0, flagName = "Chính Tuyến 1" });
            reg.Register(new TaskFlagConfigEntry { flagId = 2, taskType = 2, flagName = "Hằng Ngày 1" });
            reg.Register(new TaskFlagConfigEntry { flagId = 3, taskType = 0, flagName = "Chính Tuyến 2" });
            var svc = new TaskFlagRegistryService(reg);
            var hits = svc.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void GetFlagTypeName_NotEmptyForValid()
        {
            var svc = new TaskFlagRegistryService();
            Assert.AreEqual("Chính Tuyến", svc.GetFlagTypeName(0));
            Assert.AreEqual("Hằng Ngày", svc.GetFlagTypeName(2));
            Assert.AreEqual("Môn Phái", svc.GetFlagTypeName(4));
            Assert.IsNotEmpty(svc.GetFlagTypeName(99));
        }
    }

    public class HudArtCatalogServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => HudArtCatalogService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new HudArtRegistry();
            reg.Register(new HudArtEntry { artId = 1, type = 0, name = "Btn" });
            reg.Register(new HudArtEntry { artId = 2, type = 1, name = "Ico" });
            reg.Register(new HudArtEntry { artId = 3, type = 0, name = "Btn 2" });
            var hits = reg.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void GetArtPath_ReturnsString()
        {
            var reg = new HudArtRegistry();
            reg.Register(new HudArtEntry { artId = 1, type = 1, name = "Ico", path = "ui/btn_ok" });
            var svc = new HudArtCatalogService(reg);
            string p = svc.GetArtPath(1);
            Assert.IsNotNull(p);
            Assert.IsTrue(p.Contains("btn_ok") || p.Contains("ui/btn_ok"));
        }
    }

    public class FactionMapRuntimeServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionMapRuntimeService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetMapsForFaction_FiltersCorrectly()
        {
            var reg = new PcFactionMapRegistry();
            reg.Register(new PcFactionMapEntry { mapId = 100, factionId = 1, mapNameRaw = "Map A" });
            reg.Register(new PcFactionMapEntry { mapId = 200, factionId = 2, mapNameRaw = "Map B" });
            reg.Register(new PcFactionMapEntry { mapId = 300, factionId = 1, mapNameRaw = "Map C" });
            var inner = new FactionMapService(reg);
            var svc = new FactionMapRuntimeService(inner);
            var hits = svc.GetMapsForFaction(1);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void GetContestedMaps_NonNull()
        {
            var reg = new PcFactionMapRegistry();
            reg.Register(new PcFactionMapEntry { mapId = 100, factionId = 1 });
            reg.Register(new PcFactionMapEntry { mapId = 200, factionId = 0 });
            reg.Register(new PcFactionMapEntry { mapId = 300, factionId = 2 });
            var inner = new FactionMapService(reg);
            var svc = new FactionMapRuntimeService(inner);
            var contested = svc.GetContestedMaps();
            Assert.IsNotNull(contested);
            Assert.GreaterOrEqual(contested.Count, 1);
        }
    }

    public class BattleScriptRuntimeServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleScriptRuntimeService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByTrigger_FiltersCorrectly()
        {
            var reg = new PcBattleScriptRegistry();
            reg.Register(new PcBattleScriptEntry { scriptId = 1, triggerType = 0, scriptName = "Start" });
            reg.Register(new PcBattleScriptEntry { scriptId = 2, triggerType = 1, scriptName = "End" });
            reg.Register(new PcBattleScriptEntry { scriptId = 3, triggerType = 0, scriptName = "Start 2" });
            var inner = new BattleScriptService(reg);
            var svc = new BattleScriptRuntimeService(inner);
            var hits = svc.GetByTrigger(0);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void EvaluateCondition_RejectsInvalid()
        {
            var reg = new PcBattleScriptRegistry();
            reg.Register(new PcBattleScriptEntry { scriptId = 1, triggerType = 0, mapId = 100, scriptName = "Map 100 Start" });
            var inner = new BattleScriptService(reg);
            var svc = new BattleScriptRuntimeService(inner);
            Assert.IsFalse(svc.EvaluateCondition(999, new BattleContext { currentMapId = 100 }));
            Assert.IsFalse(svc.EvaluateCondition(1, null));
            // Map khác
            Assert.IsFalse(svc.EvaluateCondition(1, new BattleContext { currentMapId = 200 }));
        }
    }
}
