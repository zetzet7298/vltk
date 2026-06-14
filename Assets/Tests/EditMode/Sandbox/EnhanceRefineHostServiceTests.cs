// -----------------------------------------------------------------------------
// VLTK Mobile — EnhanceRefineService EditMode tests.
// Kiểm tra cường hóa/tinh luyện/quest reward: cost, max level, null guards,
// host dispatch chain, deterministic reward gen.
// PC source: KNpc::EnhanceItem, RefineItem, quest reward tables.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class EnhanceRefineHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IEnhanceRefineHost
        {
            public int SuccessCalls;
            public int FailedCalls;
            public int InsufficientCalls;
            public int RefineSuccessCalls;
            public int RefineFailedCalls;
            public int RewardGenCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastItemId;
            public int LastOldLevel;
            public int LastNewLevel;
            public int LastSilverCost;
            public int LastRequiredSilver;
            public int LastCurrentSilver;
            public bool LastItemDestroyed;
            public int LastOldRefineLevel;
            public int LastNewRefineLevel;
            public int LastBonusAttrCode;
            public int LastBonusValue;
            public int LastRefineLevelArg;
            public int LastTargetAttrCode;
            public int LastQuestDifficulty;
            public int LastPlayerLevel;
            public int LastRewardItemCount;
            public string LastMessage;
            public string LastSfxAction;

            public void OnEnhanceSuccess(int itemId, int oldLevel, int newLevel, int silverCost)
            {
                SuccessCalls++;
                LastItemId = itemId;
                LastOldLevel = oldLevel;
                LastNewLevel = newLevel;
                LastSilverCost = silverCost;
            }
            public void OnEnhanceFailed(int itemId, int currentLevel, int newLevel, bool itemDestroyed)
            {
                FailedCalls++;
                LastItemDestroyed = itemDestroyed;
            }
            public void OnEnhanceInsufficientSilver(int itemId, int requiredSilver, int currentSilver)
            {
                InsufficientCalls++;
                LastRequiredSilver = requiredSilver;
                LastCurrentSilver = currentSilver;
            }
            public void OnRefineSuccess(int itemId, int oldRefineLevel, int newRefineLevel, int bonusAttrCode, int bonusValue)
            {
                RefineSuccessCalls++;
                LastOldRefineLevel = oldRefineLevel;
                LastNewRefineLevel = newRefineLevel;
                LastBonusAttrCode = bonusAttrCode;
                LastBonusValue = bonusValue;
            }
            public void OnRefineFailed(int itemId, int currentRefineLevel, int targetAttrCode)
            {
                RefineFailedCalls++;
                LastRefineLevelArg = currentRefineLevel;
                LastTargetAttrCode = targetAttrCode;
            }
            public void OnQuestRewardGenerated(int questDifficulty, int playerLevel, int itemCount)
            {
                RewardGenCalls++;
                LastQuestDifficulty = questDifficulty;
                LastPlayerLevel = playerLevel;
                LastRewardItemCount = itemCount;
            }
            public void ShowEnhanceRefineUI(int itemId, int currentLevel, int currentRefineLevel) { ShowCalls++; }
            public void LogEnhanceRefineEvent(int itemId, int level, int refineLevel, string message) { LogCalls++; LastMessage = message; }
            public void PlayEnhanceSFX(int itemId, string action) { SfxCalls++; LastSfxAction = action; }
            public void SaveItemEnhanceState(int itemId, int level, int refineLevel) { SaveCalls++; }
        }

        private static ItemDefinition MakeItem(int id = 1000)
        {
            return new ItemDefinition { itemId = id, nameRaw = "TestItem" };
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new EnhanceRefineService();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService();
            svc.AttachHost(host);
            svc.Enhance(MakeItem(1), 0, 1000);
            // either Success or Failed or Insufficient — at least one
            Assert.IsTrue(host.SuccessCalls + host.FailedCalls + host.InsufficientCalls > 0);
        }

        // ── Enhance: guards ─────────────────────────────────────────────────

        [Test]
        public void Enhance_NullItem_EmptyResult()
        {
            var svc = new EnhanceRefineService();
            var result = svc.Enhance(null, 0, 1000);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void Enhance_MaxLevel_EmptyResult()
        {
            var svc = new EnhanceRefineService();
            var result = svc.Enhance(MakeItem(), 16, 10000);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void Enhance_InsufficientSilver_NoSuccess()
        {
            var svc = new EnhanceRefineService();
            // level 0 → cost = 100 * 1 * 1 = 100
            var result = svc.Enhance(MakeItem(1), 0, 50);
            Assert.IsFalse(result.success);
            Assert.AreEqual(0, result.silverCost);
        }

        [Test]
        public void Enhance_InsufficientSilver_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.Enhance(MakeItem(1), 0, 50);
            Assert.AreEqual(1, host.InsufficientCalls);
            Assert.AreEqual(100, host.LastRequiredSilver);
            Assert.AreEqual(50, host.LastCurrentSilver);
        }

        [Test]
        public void Enhance_HasSilver_DispatchesResult()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.Enhance(MakeItem(1), 0, 10000);
            // Result is random, but one of Success/Failed must be called
            Assert.AreEqual(1, host.SuccessCalls + host.FailedCalls);
        }

        [Test]
        public void Enhance_HasSilver_DispatchesUI()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.Enhance(MakeItem(1), 0, 10000);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void Enhance_MultipleRolls_AllDispatch()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            for (int i = 0; i < 5; i++) svc.Enhance(MakeItem(1), i, 100000);
            // 5 rolls total
            int total = host.SuccessCalls + host.FailedCalls;
            Assert.AreEqual(5, total);
        }

        // ── Refine: guards ──────────────────────────────────────────────────

        [Test]
        public void Refine_NullItem_EmptyResult()
        {
            var svc = new EnhanceRefineService();
            var result = svc.Refine(null, 0, 1);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void Refine_MaxLevel_EmptyResult()
        {
            var svc = new EnhanceRefineService();
            var result = svc.Refine(MakeItem(), 10, 1);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void Refine_DispatchesResult()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.Refine(MakeItem(1), 0, 5);
            Assert.AreEqual(1, host.RefineSuccessCalls + host.RefineFailedCalls);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.SfxCalls);
        }

        [Test]
        public void Refine_DispatchesTargetAttr()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.Refine(MakeItem(1), 0, 7);
            Assert.AreEqual(7, host.LastTargetAttrCode);
        }

        // ── GenerateQuestReward: deterministic ─────────────────────────────

        [Test]
        public void GenerateQuestReward_Difficulty1_1Item()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(1, 10);
            Assert.AreEqual(1, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_Difficulty2_2Items()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(2, 10);
            Assert.AreEqual(2, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_Difficulty3_3Items()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(3, 10);
            Assert.AreEqual(3, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_Difficulty5_4Items()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(5, 10);
            Assert.AreEqual(4, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_Difficulty0_0Items()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(0, 10);
            Assert.AreEqual(0, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_ExpFormula()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(3, 20);
            Assert.AreEqual(3L * 20 * 50L, reward.exp);
        }

        [Test]
        public void GenerateQuestReward_SilverFormula()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(3, 20);
            Assert.AreEqual(3 * 20 * 5, reward.silver);
        }

        [Test]
        public void GenerateQuestReward_SkillPoints_HighDifficulty()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(3, 20);
            Assert.AreEqual(1, reward.skillPoints);
        }

        [Test]
        public void GenerateQuestReward_SkillPoints_LowDifficulty()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(2, 20);
            Assert.AreEqual(0, reward.skillPoints);
        }

        [Test]
        public void GenerateQuestReward_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EnhanceRefineService(host);
            svc.GenerateQuestReward(3, 20);
            Assert.AreEqual(1, host.RewardGenCalls);
            Assert.AreEqual(3, host.LastQuestDifficulty);
            Assert.AreEqual(20, host.LastPlayerLevel);
            Assert.AreEqual(3, host.LastRewardItemCount);
        }

        [Test]
        public void GenerateQuestReward_Static()
        {
            var reward = EnhanceRefineService.GenerateQuestRewardStatic(2, 10);
            Assert.AreEqual(2, reward.itemIds.Count);
        }

        [Test]
        public void GenerateQuestReward_Description_Set()
        {
            var svc = new EnhanceRefineService();
            var reward = svc.GenerateQuestReward(3, 20);
            Assert.IsNotNull(reward.descriptionVi);
            Assert.IsTrue(reward.descriptionVi.Length > 0);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void EnhanceRefineService_WithoutHost_DoesNotThrow()
        {
            var svc = new EnhanceRefineService();
            Assert.DoesNotThrow(() => svc.Enhance(MakeItem(1), 0, 100));
            Assert.DoesNotThrow(() => svc.Refine(MakeItem(1), 0, 1));
            Assert.DoesNotThrow(() => svc.GenerateQuestReward(2, 10));
        }

        [Test]
        public void EnhanceRefineService_WithoutHost_NullItem()
        {
            var svc = new EnhanceRefineService();
            Assert.DoesNotThrow(() => svc.Enhance(null, 0, 100));
            Assert.DoesNotThrow(() => svc.Refine(null, 0, 1));
        }
    }
}
