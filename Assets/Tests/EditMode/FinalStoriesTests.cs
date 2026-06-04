// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.4 / 05.1-05.3 / 06.1-06.2 Tests
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // ── ST-04.4 PK / Tống Kim / Boss Hoàng Kim / Bang Chiến ────────────────

    public class PkCombatTests
    {
        [Test]
        public void PkMode_Peace_CannotAttack()
        {
            var pk = new PkCombatService(factionId: 5);
            pk.SetPkMode(PkMode.Peace);

            var attacker = new CombatActorState { actorId = 1, currentLife = 100, faction = (CombatFaction)5 };
            var target = new CombatActorState { actorId = 2, currentLife = 100, faction = (CombatFaction)1 };

            var result = pk.CanAttack(attacker, target);
            Assert.IsFalse(result.canAttack);
        }

        [Test]
        public void PkMode_Free_CanAttackAnyone()
        {
            var pk = new PkCombatService(factionId: 5);
            pk.SetPkMode(PkMode.Free);

            var attacker = new CombatActorState { actorId = 1, currentLife = 100, faction = (CombatFaction)5 };
            var target = new CombatActorState { actorId = 2, currentLife = 100, faction = (CombatFaction)1 };

            var result = pk.CanAttack(attacker, target);
            Assert.IsTrue(result.canAttack);
            Assert.AreEqual(PkPenaltyType.KarmaIncrease, result.penalty);
        }

        [Test]
        public void PkMode_Faction_SameFactionCannotAttack()
        {
            var pk = new PkCombatService(factionId: 5);
            pk.SetPkMode(PkMode.Faction);

            var attacker = new CombatActorState { actorId = 1, currentLife = 100, faction = (CombatFaction)5 };
            var target = new CombatActorState { actorId = 2, currentLife = 100, faction = (CombatFaction)5 };

            var result = pk.CanAttack(attacker, target);
            Assert.IsFalse(result.canAttack);
        }

        [Test]
        public void PkMode_Faction_DifferentFactionCanAttack()
        {
            var pk = new PkCombatService(factionId: 5);
            pk.SetPkMode(PkMode.Faction);

            var attacker = new CombatActorState { actorId = 1, currentLife = 100, faction = (CombatFaction)5 };
            var target = new CombatActorState { actorId = 2, currentLife = 100, faction = (CombatFaction)1 };

            var result = pk.CanAttack(attacker, target);
            Assert.IsTrue(result.canAttack);
        }

        [Test]
        public void TongJin_RecordKillAndEnd()
        {
            var tj = new TongJinBattleService();
            tj.StartMatch(1, 10f);

            tj.RecordKill(true);  // Tống kill
            tj.RecordKill(true);  // Tống kill
            tj.RecordKill(false); // Kim kill

            Assert.AreEqual(2, tj.State.songScore);
            Assert.AreEqual(1, tj.State.jinScore);
        }

        [Test]
        public void BossHoangKim_RespawnTimer()
        {
            var boss = new BossHoangKimService();
            Assert.Greater(boss.RegisteredBosses.Count, 0);

            // Kill boss
            boss.OnBossDeath(600, killerActorId: 1);
            Assert.IsFalse(boss.IsBossAlive(600));

            // Tick past respawn
            boss.Tick(3601f); // 60 minutes + 1 second
            Assert.IsTrue(boss.IsBossAlive(600));
        }

        [Test]
        public void BangChien_RecordAndEnd()
        {
            var bc = new BangChienService();
            bc.StartBangChien(1, 2);

            bc.RecordKill(true);  // Challenger kill
            bc.RecordKill(true);
            bc.RecordKill(false); // Defender kill

            int winner = bc.EndBangChien();
            Assert.AreEqual(1, winner); // Challenger wins 2-1
            Assert.IsFalse(bc.IsActive);
        }
    }

    // ── ST-05.1 Equipment Slot Mapping ─────────────────────────────────────

    public class EquipmentSlotTests
    {
        [Test]
        public void SlotMapping_WeaponIsEquippable()
        {
            Assert.IsTrue(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Weapon));
            Assert.IsTrue(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Armor));
            Assert.IsFalse(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Medicament));
        }

        [Test]
        public void SlotMapping_StackSizes()
        {
            Assert.AreEqual(1, EquipmentSlotMappingService.GetMaxStack(PcItemCategory.Weapon));
            Assert.AreEqual(99, EquipmentSlotMappingService.GetMaxStack(PcItemCategory.Medicament));
            Assert.AreEqual(999, EquipmentSlotMappingService.GetMaxStack(PcItemCategory.Material));
        }

        [Test]
        public void ItemTypeToCategory_MapsCorrectly()
        {
            Assert.AreEqual(PcItemCategory.Weapon, EquipmentSlotMappingService.ItemTypeToCategory(1));
            Assert.AreEqual(PcItemCategory.Armor, EquipmentSlotMappingService.ItemTypeToCategory(3));
            Assert.AreEqual(PcItemCategory.Currency, EquipmentSlotMappingService.ItemTypeToCategory(12));
        }
    }

    // ── ST-05.2 Enhance / Refine ──────────────────────────────────────────

    public class EnhanceRefineTests
    {
        [Test]
        public void QuestReward_GeneratesCorrectly()
        {
            var reward = EnhanceRefineService.GenerateQuestReward(questDifficulty: 3, playerLevel: 20);

            Assert.Greater(reward.exp, 0);
            Assert.Greater(reward.silver, 0);
            Assert.AreEqual(1, reward.skillPoints);
            Assert.Greater(reward.itemIds.Count, 0);
        }

        [Test]
        public void QuestReward_LowDifficulty_NoSkillPoint()
        {
            var reward = EnhanceRefineService.GenerateQuestReward(questDifficulty: 1, playerLevel: 10);
            Assert.AreEqual(0, reward.skillPoints);
        }
    }

    // ── ST-05.3 Economy ───────────────────────────────────────────────────

    public class EconomyTests
    {
        [Test]
        public void Economy_SpendAndEarnSilver()
        {
            var eco = new EconomyService(maxStashSlots: 50, initialSilver: 1000);

            Assert.IsTrue(eco.SpendSilver(300));
            Assert.AreEqual(700, eco.Wallet.silver);

            Assert.IsFalse(eco.SpendSilver(800)); // Không đủ
            Assert.AreEqual(700, eco.Wallet.silver);

            eco.EarnSilver(500);
            Assert.AreEqual(1200, eco.Wallet.silver);
        }

        [Test]
        public void Economy_StashDepositAndWithdraw()
        {
            var eco = new EconomyService(maxStashSlots: 5);

            Assert.IsTrue(eco.DepositToStash(1001, 10));
            Assert.AreEqual(1, eco.StashUsed);

            Assert.IsTrue(eco.DepositToStash(1001, 5)); // Stack
            Assert.AreEqual(1, eco.StashUsed);           // Vẫn 1 slot

            Assert.IsTrue(eco.WithdrawFromStash(1001, 8));
            Assert.AreEqual(7, eco.Stash[0].count);      // 15 - 8 = 7
        }

        [Test]
        public void Economy_TradeSession()
        {
            var eco = new EconomyService();
            var trade = eco.CreateTradeSession(1, 2);

            trade.AddItem(1, 1001, 1);
            trade.SetSilver(2, 500);
            trade.Lock(1);
            trade.Lock(2);

            Assert.IsTrue(trade.IsReady);
            Assert.AreEqual(1, trade.initiatorItems.Count);
            Assert.AreEqual(500, trade.targetSilver);
        }

        [Test]
        public void Economy_ShopBuySell()
        {
            var eco = new EconomyService(maxStashSlots: 50, initialSilver: 1000);

            Assert.IsTrue(eco.BuyFromShop(1001, 5, 50)); // Mua 5 cái giá 50 mỗi cái
            Assert.AreEqual(750, eco.Wallet.silver);      // 1000 - 250

            int sellPrice = eco.SellToShop(1001, 3, 50);  // Bán 3 cái, giá gốc 50
            Assert.AreEqual(75, sellPrice);                // 50*3/2 = 75
            Assert.AreEqual(825, eco.Wallet.silver);
        }
    }

    // ── ST-06.1 Mobile Input Spec ─────────────────────────────────────────

    public class MobileInputTests
    {
        [Test]
        public void InputSpec_JoystickZoneDetected()
        {
            // Vị trí joystick (15%, 25%) phải nằm trong zone
            Assert.IsTrue(MobileInputSpec.IsInJoystickZone(new Vector2(0.15f, 0.25f)));
            // Góc phải trên không phải joystick
            Assert.IsFalse(MobileInputSpec.IsInJoystickZone(new Vector2(0.9f, 0.9f)));
        }

        [Test]
        public void InputSpec_TouchZonesExist()
        {
            Assert.Greater(MobileInputSpec.DefaultTouchZones.Count, 5);
            Assert.Greater(MobileInputSpec.DefaultHudLayout.Count, 10);
        }

        [Test]
        public void InputSpec_FindTouchZone()
        {
            var zone = MobileInputSpec.FindTouchZone(new Vector2(0.15f, 0.25f));
            Assert.AreEqual("joystick", zone.name);
        }
    }

    // ── ST-06.2 Mobile Build ──────────────────────────────────────────────

    public class MobileBuildTests
    {
        [Test]
        public void Build_DefaultConfigValid()
        {
            var svc = new MobileBuildService();
            Assert.AreEqual("android", svc.BuildConfig.platform);
            Assert.AreEqual(150, svc.PerfBudget.maxDrawCalls);
            Assert.IsTrue(svc.PipelineConfig.bundleAssets);
        }

        [Test]
        public void Build_ValidateBudgetWithinLimits()
        {
            var svc = new MobileBuildService();
            Assert.IsTrue(svc.ValidateBudget(100, 50000, 256));
        }

        [Test]
        public void Build_ValidateBudgetExceedsDrawCalls()
        {
            var svc = new MobileBuildService();
            Assert.IsFalse(svc.ValidateBudget(200, 50000, 256));
        }

        [Test]
        public void Build_EstimateBundleSize()
        {
            var svc = new MobileBuildService();
            long size = svc.EstimateBundleSize(spriteCount: 1000, audioCount: 50, mapCount: 10);
            Assert.Greater(size, 0);
        }

        [Test]
        public void Build_PipelineRecommendation()
        {
            var svc = new MobileBuildService();
            string low = svc.GetPipelineRecommendation(1024);
            Assert.IsTrue(low.Contains("thấp cấp"));

            string high = svc.GetPipelineRecommendation(8192);
            Assert.IsTrue(high.Contains("cao cấp"));
        }
    }
}
