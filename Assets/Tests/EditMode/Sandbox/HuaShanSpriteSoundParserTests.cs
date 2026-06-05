// -----------------------------------------------------------------------------
// VLTK Mobile — Parser-only tests for batch 11.
// Direct registry tests (no service layer).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class HuaShanSpriteSoundParserTests
    {
        // ── PcHuaShanLuanJianRegistry ──────────────────────────────────
        [Test]
        public void PcHuaShan_Count_NonNegative()
        {
            var reg = new PcHuaShanLuanJianRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcHuaShan_GetByMap_FiltersCorrectly()
        {
            var reg = new PcHuaShanLuanJianRegistry();
            reg.Register(new PcHuaShanLuanJianEntry { id = 1, roundIdx = 1, mapId = 100, requiredLevel = 10 });
            reg.Register(new PcHuaShanLuanJianEntry { id = 2, roundIdx = 2, mapId = 100, requiredLevel = 20 });
            reg.Register(new PcHuaShanLuanJianEntry { id = 3, roundIdx = 3, mapId = 200, requiredLevel = 30, isFinalRound = true });
            var map100 = reg.GetByMap(100);
            Assert.AreEqual(2, map100.Count);
            var final = reg.GetFinalRound();
            Assert.IsNotNull(final);
            Assert.AreEqual(3, final.roundIdx);
            Assert.AreEqual(3, reg.GetTotalRounds());
        }

        // ── PcSpriteAssetRegistry ──────────────────────────────────────
        [Test]
        public void PcSpriteAsset_Count_NonNegative()
        {
            var reg = new PcSpriteAssetRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSpriteAsset_GetByCategory_FiltersCorrectly()
        {
            var reg = new PcSpriteAssetRegistry();
            reg.Register(new PcSpriteAssetEntry { spriteId = 1, category = 0, name = "player" });
            reg.Register(new PcSpriteAssetEntry { spriteId = 2, category = 1, name = "npc" });
            reg.Register(new PcSpriteAssetEntry { spriteId = 3, category = 0, name = "player2" });
            var playerList = reg.GetByCategory(0);
            Assert.AreEqual(2, playerList.Count);
        }

        // ── PcSoundEffectRegistry ──────────────────────────────────────
        [Test]
        public void PcSoundEffect_Count_NonNegative()
        {
            var reg = new PcSoundEffectRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSoundEffect_GetByCategory_FiltersCorrectly()
        {
            var reg = new PcSoundEffectRegistry();
            reg.Register(new PcSoundEffectEntry { soundId = 1, category = 0, name = "click" });
            reg.Register(new PcSoundEffectEntry { soundId = 2, category = 3, name = "hit" });
            reg.Register(new PcSoundEffectEntry { soundId = 3, category = 0, name = "click2" });
            var clickList = reg.GetByCategory(0);
            Assert.AreEqual(2, clickList.Count);
        }

        // ── PcMapConnectionRegistry ────────────────────────────────────
        [Test]
        public void PcMapConn_Count_NonNegative()
        {
            var reg = new PcMapConnectionRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapConn_GetByFromMap_FiltersCorrectly()
        {
            var reg = new PcMapConnectionRegistry();
            reg.Register(new PcMapConnectionEntry { connectionId = 1, fromMapId = 100, toMapId = 200, fromX = 0, fromY = 0, toX = 100, toY = 100 });
            reg.Register(new PcMapConnectionEntry { connectionId = 2, fromMapId = 100, toMapId = 300, fromX = 0, fromY = 0, toX = 200, toY = 200 });
            reg.Register(new PcMapConnectionEntry { connectionId = 3, fromMapId = 200, toMapId = 400, fromX = 0, fromY = 0, toX = 300, toY = 300 });
            var from100 = reg.GetByFromMap(100);
            Assert.AreEqual(2, from100.Count);
        }

        // ── PcNpcShopItemRegistry ──────────────────────────────────────
        [Test]
        public void PcNpcShop_Count_NonNegative()
        {
            var reg = new PcNpcShopItemRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcNpcShop_GetByShop_FiltersCorrectly()
        {
            var reg = new PcNpcShopItemRegistry();
            reg.Register(new PcNpcShopItemEntry { id = 1, shopNpcId = 100, slotIdx = 0, itemId = 500, price = 1000 });
            reg.Register(new PcNpcShopItemEntry { id = 2, shopNpcId = 100, slotIdx = 1, itemId = 501, price = 2000 });
            reg.Register(new PcNpcShopItemEntry { id = 3, shopNpcId = 200, slotIdx = 0, itemId = 502, price = 3000 });
            var shop100 = reg.GetByShop(100);
            Assert.AreEqual(2, shop100.Count);
        }

        // ── PcReputationRegistry ───────────────────────────────────────
        [Test]
        public void PcReputation_Count_NonNegative()
        {
            var reg = new PcReputationRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcReputation_GetByFaction_FiltersCorrectly()
        {
            var reg = new PcReputationRegistry();
            reg.Register(new PcReputationEntry { reputationId = 1, name = "Thiếu Lâm Sơ Cấp", factionId = 0, requiredLevel = 10 });
            reg.Register(new PcReputationEntry { reputationId = 2, name = "Thiếu Lâm Cao Cấp", factionId = 0, requiredLevel = 50 });
            reg.Register(new PcReputationEntry { reputationId = 3, name = "Cái Bang Sơ Cấp", factionId = 5, requiredLevel = 10 });
            var shaolin = reg.GetByFaction(0);
            Assert.AreEqual(2, shaolin.Count);
        }

        // ── PcTitleEffectRegistry ──────────────────────────────────────
        [Test]
        public void PcTitleEffect_Count_NonNegative()
        {
            var reg = new PcTitleEffectRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcTitleEffect_GetByTitle_FiltersCorrectly()
        {
            var reg = new PcTitleEffectRegistry();
            reg.Register(new PcTitleEffectEntry { effectId = 1, titleId = 100, effectType = 0, effectValue = 100 });
            reg.Register(new PcTitleEffectEntry { effectId = 2, titleId = 100, effectType = 2, effectValue = 50 });
            reg.Register(new PcTitleEffectEntry { effectId = 3, titleId = 200, effectType = 0, effectValue = 200 });
            var t100 = reg.GetByTitle(100);
            Assert.AreEqual(2, t100.Count);
            var hpEffects = reg.GetByType(0);
            Assert.AreEqual(2, hpEffects.Count);
        }

        // ── PcVipLevelRegistry ─────────────────────────────────────────
        [Test]
        public void PcVipLevel_Count_NonNegative()
        {
            var reg = new PcVipLevelRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcVipLevel_GetVipLevel_ReturnsNullForInvalid()
        {
            var reg = new PcVipLevelRegistry();
            reg.Register(new PcVipLevelEntry { vipLevel = 1, requiredRecharge = 100, shopDiscount = 0.05f });
            reg.Register(new PcVipLevelEntry { vipLevel = 2, requiredRecharge = 500, shopDiscount = 0.10f });
            Assert.IsNull(reg.GetVipLevel(99999));
            var v1 = reg.GetVipLevel(1);
            Assert.IsNotNull(v1);
            Assert.AreEqual(0.05f, v1.shopDiscount, 0.001f);
        }
    }
}
