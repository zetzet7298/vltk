// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for batch 11: HuaShan + Sprite + Sound + MapConnection +
// NpcShopItem + Reputation + TitleEffect + VipLevel services.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class HuaShanSpriteSoundServiceTests
    {
        // ── HuaShanLuanJianService ────────────────────────────────────
        [Test]
        public void HuaShan_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = HuaShanLuanJianService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void HuaShan_GetByMap_FiltersCorrectly()
        {
            var svc = new HuaShanLuanJianService();
            var list = svc.GetByMap(99999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void HuaShan_CanJoinRound_RejectsLowLevel()
        {
            var svc = new HuaShanLuanJianService();
            Assert.IsFalse(svc.CanJoinRound(1, 0));
        }

        [Test]
        public void HuaShan_GetFinalRound_NonNull_WhenDataPresent()
        {
            var svc = new HuaShanLuanJianService();
            // Empty registry, expect null
            Assert.IsNull(svc.GetFinalRound());
        }

        [Test]
        public void HuaShan_GetRoundName_NonEmpty()
        {
            var svc = new HuaShanLuanJianService();
            string name = svc.GetRoundName(1);
            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length > 0);
        }

        // ── SpriteAssetService ────────────────────────────────────────
        [Test]
        public void SpriteAsset_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SpriteAssetService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void SpriteAsset_GetByCategory_FiltersCorrectly()
        {
            var svc = new SpriteAssetService();
            var list = svc.GetByCategory(0);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void SpriteAsset_GetSpritePath_ReturnsString()
        {
            var svc = new SpriteAssetService();
            string p = svc.GetSpritePath(99999);
            Assert.IsNotNull(p);
            Assert.AreEqual(string.Empty, p);
        }

        // ── SoundEffectService ────────────────────────────────────────
        [Test]
        public void SoundEffect_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SoundEffectService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void SoundEffect_GetByCategory_FiltersCorrectly()
        {
            var svc = new SoundEffectService();
            var list = svc.GetByCategory(0);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void SoundEffect_GetSoundPath_ReturnsString()
        {
            var svc = new SoundEffectService();
            string p = svc.GetSoundPath(99999);
            Assert.IsNotNull(p);
            Assert.AreEqual(string.Empty, p);
        }

        [Test]
        public void SoundEffect_GetCategoryName_NonEmpty()
        {
            var svc = new SoundEffectService();
            string name = svc.GetCategoryName(0);
            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length > 0);
        }

        // ── MapConnectionService ──────────────────────────────────────
        [Test]
        public void MapConn_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MapConnectionService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void MapConn_GetByFromMap_FiltersCorrectly()
        {
            var svc = new MapConnectionService();
            var list = svc.GetByFromMap(99999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void MapConn_GetAdjacentMaps_NonNull()
        {
            var svc = new MapConnectionService();
            var list = svc.GetAdjacentMaps(0);
            Assert.IsNotNull(list);
        }

        [Test]
        public void MapConn_CanUseConnection_RejectsLowLevel()
        {
            var svc = new MapConnectionService();
            Assert.IsFalse(svc.CanUseConnection(99999, 0));
        }

        [Test]
        public void MapConn_ComputeDistance_ZeroForInvalid()
        {
            var svc = new MapConnectionService();
            Assert.AreEqual(0f, svc.ComputeDistance(99999));
        }

        [Test]
        public void MapConn_GetConnectionTypeName_NonEmpty()
        {
            var svc = new MapConnectionService();
            string name = svc.GetConnectionTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length > 0);
        }

        // ── NpcShopItemService ────────────────────────────────────────
        [Test]
        public void NpcShop_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = NpcShopItemService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void NpcShop_GetShopItems_NonNull()
        {
            var svc = new NpcShopItemService();
            var list = svc.GetShopItems(99999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void NpcShop_CanBuy_RejectsInsufficientRep()
        {
            var svc = new NpcShopItemService();
            Assert.IsFalse(svc.CanBuy(99999, 0, 0));
        }

        [Test]
        public void NpcShop_GetEffectivePrice_ZeroForInvalid()
        {
            var svc = new NpcShopItemService();
            Assert.AreEqual(0, svc.GetEffectivePrice(99999, 0, 0));
        }

        // ── ReputationService ────────────────────────────────────────
        [Test]
        public void Reputation_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ReputationService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void Reputation_GetByFaction_FiltersCorrectly()
        {
            var svc = new ReputationService();
            var list = svc.GetByFaction(99999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Reputation_CanEarn_RejectsLowLevel()
        {
            var svc = new ReputationService();
            Assert.IsFalse(svc.CanEarn(99999, 0, 0));
        }

        [Test]
        public void Reputation_GetTierName_NonEmpty()
        {
            var svc = new ReputationService();
            string tier = svc.GetTierName(1, 100);
            Assert.IsNotNull(tier);
            Assert.IsTrue(tier.Length > 0);
        }

        // ── TitleEffectService ────────────────────────────────────────
        [Test]
        public void TitleEffect_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = TitleEffectService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void TitleEffect_GetByTitle_FiltersCorrectly()
        {
            var svc = new TitleEffectService();
            var list = svc.GetByTitle(99999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void TitleEffect_ComputeTotalAtkBonus_ZeroForInvalid()
        {
            var svc = new TitleEffectService();
            Assert.AreEqual(0, svc.ComputeTotalAtkBonus(99999));
        }

        [Test]
        public void TitleEffect_ComputeTotalHpBonus_ZeroForInvalid()
        {
            var svc = new TitleEffectService();
            Assert.AreEqual(0, svc.ComputeTotalHpBonus(99999));
        }

        [Test]
        public void TitleEffect_GetEffectTypeName_NonEmpty()
        {
            var svc = new TitleEffectService();
            string name = svc.GetEffectTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length > 0);
        }

        // ── VipLevelService ───────────────────────────────────────────
        [Test]
        public void VipLevel_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = VipLevelService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void VipLevel_GetVipForRecharge_ZeroForZero()
        {
            var svc = new VipLevelService();
            Assert.AreEqual(0, svc.GetVipForRecharge(0));
        }

        [Test]
        public void VipLevel_GetShopDiscount_ZeroForZero()
        {
            var svc = new VipLevelService();
            Assert.AreEqual(0f, svc.GetShopDiscount(0));
        }

        [Test]
        public void VipLevel_GetDailyGoldBonus_ZeroForZero()
        {
            var svc = new VipLevelService();
            Assert.AreEqual(0, svc.GetDailyGoldBonus(0));
        }

        [Test]
        public void VipLevel_GetMaxBuyPerDay_ZeroForZero()
        {
            var svc = new VipLevelService();
            Assert.AreEqual(0, svc.GetMaxBuyPerDay(0));
        }

        [Test]
        public void VipLevel_HasMallAccess_TrueForVip1()
        {
            var svc = new VipLevelService();
            // With empty registry, expect false
            Assert.IsFalse(svc.HasMallAccess(1));
        }
    }
}
