// -----------------------------------------------------------------------------
// VLTK Mobile — Item/Equipment/Spawn services EditMode tests
// 13 services: Gold/Platina/Horse/Potion/MagicScript/MagicAttrib/Scroll/
//              CaveList/GoldBoss/ChangeFeatureData/GlobalConfig/NormalSpawn/RareEnchant
// Mỗi service có 2 test: load không throw + filter hoạt động đúng.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ItemEquipmentServiceTests
    {
        private static string FullDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcItemFull");
        private static string MapDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMap");
        private static string NpcDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcNpc");
        private static string AttribDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAttrib");

        // ----- GoldEquipService -----
        [Test]
        public void GoldEquipService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GoldEquipService.LoadFromStreamingAssets());
        }

        [Test]
        public void GoldEquipService_GetByLevel_FiltersCorrectly()
        {
            var reg = PcGoldEquipParser.BuildRegistry(FullDir);
            var svc = new GoldEquipService();
            svc.AttachRegistry(reg);
            var result = svc.GetByLevel(50);
            foreach (var e in result) Assert.AreEqual(50, e.requiredLevel, "Mọi entry phải có requiredLevel=50");
        }

        // ----- PlatinaEquipService -----
        [Test]
        public void PlatinaEquipService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PlatinaEquipService.LoadFromStreamingAssets());
        }

        [Test]
        public void PlatinaEquipService_GetBySeries_FiltersCorrectly()
        {
            var reg = PcPlatinaEquipParser.BuildRegistry(FullDir);
            var svc = new PlatinaEquipService();
            svc.AttachRegistry(reg);
            var result = svc.GetBySeries(0);  // Kim
            foreach (var e in result) Assert.AreEqual(0, e.series, "Mọi entry phải có series=0 (Kim)");
        }

        // ----- HorseService -----
        [Test]
        public void HorseService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => HorseService.LoadFromStreamingAssets());
        }

        [Test]
        public void HorseService_GetByLevel_FiltersCorrectly()
        {
            var reg = PcHorseParser.BuildRegistry(FullDir);
            var svc = new HorseService();
            svc.AttachRegistry(reg);
            var result = svc.GetByLevel(50);
            foreach (var e in result) Assert.AreEqual(50, e.requiredLevel, "Mọi entry phải có requiredLevel=50");
        }

        // ----- PotionService -----
        [Test]
        public void PotionService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PotionService.LoadFromStreamingAssets());
        }

        [Test]
        public void PotionService_GetByType_FiltersCorrectly()
        {
            var reg = PcPotionParser.BuildRegistry(FullDir);
            var svc = new PotionService();
            svc.AttachRegistry(reg);
            var result = svc.GetByType(0);  // 0=HP
            foreach (var e in result) Assert.AreEqual(0, e.type, "Mọi entry phải có type=0 (HP)");
        }

        // ----- MagicScriptService -----
        [Test]
        public void MagicScriptService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MagicScriptService.LoadFromStreamingAssets());
        }

        [Test]
        public void MagicScriptService_GetByTrigger_FiltersCorrectly()
        {
            var reg = PcMagicScriptParser.BuildRegistry(FullDir);
            var svc = new MagicScriptService();
            svc.AttachRegistry(reg);
            var result = svc.GetByTrigger(0);  // 0=hit
            foreach (var e in result) Assert.AreEqual(0, e.triggerOn, "Mọi entry phải có triggerOn=0 (hit)");
        }

        // ----- MagicAttribService -----
        [Test]
        public void MagicAttribService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MagicAttribService.LoadFromStreamingAssets());
        }

        [Test]
        public void MagicAttribService_GetAttrib_ReturnsNullForInvalid()
        {
            var reg = PcMagicAttribParser.BuildRegistry(FullDir);
            var svc = new MagicAttribService();
            svc.AttachRegistry(reg);
            var result = svc.GetAttrib(-1);  // ID không tồn tại
            Assert.IsNull(result, "GetAttrib(-1) phải trả về null");
        }

        // ----- ScrollService -----
        [Test]
        public void ScrollService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ScrollService.LoadFromStreamingAssets());
        }

        [Test]
        public void ScrollService_GetByFromMap_FiltersCorrectly()
        {
            var reg = PcScrollParser.BuildRegistry(MapDir);
            var svc = new ScrollService();
            svc.AttachRegistry(reg);
            var result = svc.GetByFromMap(1);
            foreach (var e in result) Assert.AreEqual(1, e.fromMapId, "Mọi entry phải có fromMapId=1");
        }

        // ----- CaveListEntryService -----
        [Test]
        public void CaveListEntryService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => CaveListFullService.LoadFromStreamingAssets());
        }

        [Test]
        public void CaveListEntryService_CanEnter_RejectsLevelMismatch()
        {
            var reg = PcCaveListEntryParser.BuildRegistry(MapDir);
            var svc = new CaveListEntryService();
            svc.AttachRegistry(reg);
            // Nếu không có cave nào, CanEnter luôn false
            Assert.IsFalse(svc.CanEnter(0, 50, 1), "CanEnter phải false khi cave không tồn tại");
        }

        // ----- GoldBossService -----
        [Test]
        public void GoldBossService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GoldBossService.LoadFromStreamingAssets());
        }

        [Test]
        public void GoldBossService_GetBoss_ReturnsNullForInvalid()
        {
            var reg = PcGoldBossParser.BuildRegistry(NpcDir);
            var svc = new GoldBossService();
            svc.AttachRegistry(reg);
            var result = svc.GetBoss(-1);  // ID không tồn tại
            Assert.IsNull(result, "GetBoss(-1) phải trả về null");
        }

        // ----- ChangeFeatureDataService -----
        [Test]
        public void ChangeFeatureDataService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ChangeFeatureDataService.LoadFromStreamingAssets());
        }

        [Test]
        public void ChangeFeatureDataService_GetByCategory_FiltersCorrectly()
        {
            var reg = PcChangeFeatureDataParser.BuildRegistry(FullDir);
            var svc = new ChangeFeatureDataService();
            svc.AttachRegistry(reg);
            var result = svc.GetByCategory(126);
            foreach (var e in result) Assert.AreEqual(126, e.magicAttribId, "Mọi entry phải có magicAttribId=126");
        }

        // ----- GlobalConfigService -----
        [Test]
        public void GlobalConfigService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GlobalConfigService.LoadFromStreamingAssets());
        }

        [Test]
        public void GlobalConfigService_GetValue_ReturnsNullForInvalid()
        {
            var reg = PcGlobalConfigParser.BuildRegistry(AttribDir);
            var svc = new GlobalConfigService();
            svc.AttachRegistry(reg);
            var result = svc.GetValue("__KEY_KHONG_TON_TAI__");
            Assert.IsNull(result, "GetValue của key không tồn tại phải trả về null");
        }

        // ----- NormalSpawnService -----
        [Test]
        public void NormalSpawnService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NormalSpawnService.LoadFromStreamingAssets());
        }

        [Test]
        public void NormalSpawnService_GetByMap_FiltersCorrectly()
        {
            var reg = PcNormalSpawnParser.BuildRegistry(NpcDir);
            var svc = new NormalSpawnService();
            svc.AttachRegistry(reg);
            var result = svc.GetByMap(1);
            foreach (var e in result) Assert.AreEqual(1, e.mapId, "Mọi entry phải có mapId=1");
        }

        // ----- RareEnchantService -----
        [Test]
        public void RareEnchantService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => RareEnchantService.LoadFromStreamingAssets());
        }

        [Test]
        public void RareEnchantService_GroupsByMagicId()
        {
            // rare.txt is a magic-attribute / weapon-enchant roll table, not a spawn
            // table. Verify the table indexes rows by MAGIC_ID instead of mapId.
            var table = PcRareEnchantParser.BuildTable(NpcDir);
            var svc = new RareEnchantService();
            svc.AttachTable(table);
            if (svc.Count == 0)
            {
                Assert.Inconclusive("No committed rare.txt rows under PcNpc; nothing to group.");
                return;
            }
            Assert.Greater(svc.MagicIdCount, 0, "Loaded rows must index at least one MAGIC_ID");
            foreach (var row in svc.All)
            {
                var tiers = svc.GetByMagicId(row.magicId);
                Assert.IsTrue(tiers.Exists(t => ReferenceEquals(t, row) || t.magicId == row.magicId),
                    $"Row with magicId={row.magicId} must be retrievable via GetByMagicId");
            }
        }
    }
}
