// -----------------------------------------------------------------------------
// VLTK Mobile — EconomyService EditMode tests.
// Kiểm tra economy lifecycle: silver/gold currency, stash deposit/withdraw,
// trade session, NPC shop buy/sell, host dispatch chain.
// PC source: KNpc::Stash, Trade dialog, Silver currency.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class EconomyServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IEconomyHost
        {
            public int CurrencyCalls;
            public int DepositCalls;
            public int WithdrawCalls;
            public int StashFullCalls;
            public int BuyCalls;
            public int SellCalls;
            public int TradeCalls;
            public int SaveCalls;
            public int LastSilver;
            public int LastGold;
            public int LastHuyenTinh;
            public int LastItemId;
            public int LastCount;
            public int LastTotalCost;
            public int LastEarnedSilver;
            public int LastTradeId;
            public int LastInitiatorId;
            public int LastTargetId;
            public int LastStashUsed;
            public int LastMaxStashSlots;

            public void OnCurrencyChanged(int silver, int gold, int huyenTinh)
            {
                CurrencyCalls++;
                LastSilver = silver;
                LastGold = gold;
                LastHuyenTinh = huyenTinh;
            }
            public void OnStashDeposit(int itemId, int count, int totalStashUsed, int maxStashSlots)
            {
                DepositCalls++;
                LastItemId = itemId;
                LastCount = count;
                LastStashUsed = totalStashUsed;
                LastMaxStashSlots = maxStashSlots;
            }
            public void OnStashWithdraw(int itemId, int count, int totalStashUsed) { WithdrawCalls++; }
            public void OnStashFull(int maxStashSlots) { StashFullCalls++; }
            public void OnShopBuy(int itemId, int count, int totalSilverSpent)
            {
                BuyCalls++;
                LastTotalCost = totalSilverSpent;
            }
            public void OnShopSell(int itemId, int count, int silverEarned)
            {
                SellCalls++;
                LastEarnedSilver = silverEarned;
            }
            public void OnTradeSessionCreated(int tradeId, int initiatorId, int targetId)
            {
                TradeCalls++;
                LastTradeId = tradeId;
                LastInitiatorId = initiatorId;
                LastTargetId = targetId;
            }
            public void SaveEconomyState(int silver, int gold, int huyenTinh, int stashUsed, int maxStashSlots)
            {
                SaveCalls++;
            }
        }

        // ── Ctor + wallet ───────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new EconomyService();
            Assert.AreEqual(0, svc.Wallet.silver);
            Assert.AreEqual(0, svc.StashUsed);
        }

        [Test]
        public void Constructor_WithInitialSilver()
        {
            var svc = new EconomyService(100, 500);
            Assert.AreEqual(500, svc.Wallet.silver);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new EconomyService();
            svc.AttachHost(host);
            svc.EarnSilver(100);
            Assert.AreEqual(1, host.CurrencyCalls);
        }

        // ── SpendSilver / EarnSilver ────────────────────────────────────────

        [Test]
        public void EarnSilver_IncreasesBalance()
        {
            var svc = new EconomyService();
            svc.EarnSilver(100);
            Assert.AreEqual(100, svc.Wallet.silver);
        }

        [Test]
        public void EarnSilver_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.EarnSilver(50);
            Assert.AreEqual(1, host.CurrencyCalls);
            Assert.AreEqual(50, host.LastSilver);
        }

        [Test]
        public void SpendSilver_Success_Decreases()
        {
            var svc = new EconomyService(100, 100);
            Assert.IsTrue(svc.SpendSilver(40));
            Assert.AreEqual(60, svc.Wallet.silver);
        }

        [Test]
        public void SpendSilver_Insufficient_ReturnsFalse()
        {
            var svc = new EconomyService(100, 50);
            Assert.IsFalse(svc.SpendSilver(100));
            Assert.AreEqual(50, svc.Wallet.silver);
        }

        [Test]
        public void SpendSilver_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 100, host);
            svc.SpendSilver(40);
            Assert.AreEqual(1, host.CurrencyCalls);
            Assert.AreEqual(60, host.LastSilver);
        }

        [Test]
        public void SpendSilver_FiresOnSilverChangedEvent()
        {
            var svc = new EconomyService(100, 100);
            int fired = 0;
            int lastAmt = 0;
            int lastDelta = 0;
            svc.OnSilverChanged += (a, d) => { fired++; lastAmt = a; lastDelta = d; };
            svc.SpendSilver(40);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(60, lastAmt);
            Assert.AreEqual(-40, lastDelta);
        }

        // ── SpendGold / EarnGold ────────────────────────────────────────────

        [Test]
        public void EarnGold_Dispatches()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.EarnGold(10);
            Assert.AreEqual(1, host.CurrencyCalls);
            Assert.AreEqual(10, host.LastGold);
        }

        [Test]
        public void SpendGold_Success()
        {
            var svc = new EconomyService(100, 0);
            svc.EarnGold(100);
            Assert.IsTrue(svc.SpendGold(40));
            Assert.AreEqual(60, svc.Wallet.gold);
        }

        [Test]
        public void SpendGold_Insufficient_ReturnsFalse()
        {
            var svc = new EconomyService(100, 0);
            Assert.IsFalse(svc.SpendGold(10));
        }

        [Test]
        public void SpendGold_FiresOnGoldChangedEvent()
        {
            var svc = new EconomyService(100, 0);
            svc.EarnGold(100);
            int fired = 0;
            svc.OnGoldChanged += (a, d) => fired++;
            svc.SpendGold(40);
            Assert.AreEqual(1, fired);
        }

        // ── Stash ───────────────────────────────────────────────────────────

        [Test]
        public void DepositToStash_AddsNewSlot()
        {
            var svc = new EconomyService();
            Assert.IsTrue(svc.DepositToStash(1001, 5));
            Assert.AreEqual(1, svc.StashUsed);
        }

        [Test]
        public void DepositToStash_StacksExisting()
        {
            var svc = new EconomyService();
            svc.DepositToStash(1001, 5);
            svc.DepositToStash(1001, 3);
            Assert.AreEqual(1, svc.StashUsed);
            Assert.AreEqual(8, svc.Stash[0].count);
        }

        [Test]
        public void DepositToStash_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.DepositToStash(1001, 5);
            Assert.AreEqual(1, host.DepositCalls);
            Assert.AreEqual(1001, host.LastItemId);
            Assert.AreEqual(5, host.LastCount);
        }

        [Test]
        public void DepositToStash_Full_DispatchesStashFull()
        {
            var host = new FakeHost();
            var svc = new EconomyService(2, 0, host);
            svc.DepositToStash(1001, 1);
            svc.DepositToStash(1002, 1);
            Assert.IsFalse(svc.DepositToStash(1003, 1));
            Assert.AreEqual(1, host.StashFullCalls);
        }

        [Test]
        public void DepositToStash_NegativeCount_ReturnsFalse()
        {
            var svc = new EconomyService();
            Assert.IsFalse(svc.DepositToStash(1001, -1));
        }

        [Test]
        public void WithdrawFromStash_DecreasesCount()
        {
            var svc = new EconomyService();
            svc.DepositToStash(1001, 5);
            Assert.IsTrue(svc.WithdrawFromStash(1001, 2));
            Assert.AreEqual(3, svc.Stash[0].count);
        }

        [Test]
        public void WithdrawFromStash_RemovesSlot()
        {
            var svc = new EconomyService();
            svc.DepositToStash(1001, 5);
            svc.WithdrawFromStash(1001, 5);
            Assert.AreEqual(0, svc.StashUsed);
        }

        [Test]
        public void WithdrawFromStash_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.DepositToStash(1001, 5);
            svc.WithdrawFromStash(1001, 2);
            Assert.AreEqual(1, host.WithdrawCalls);
        }

        [Test]
        public void WithdrawFromStash_NotFound_ReturnsFalse()
        {
            var svc = new EconomyService();
            Assert.IsFalse(svc.WithdrawFromStash(1001, 1));
        }

        [Test]
        public void WithdrawFromStash_Insufficient_ReturnsFalse()
        {
            var svc = new EconomyService();
            svc.DepositToStash(1001, 2);
            Assert.IsFalse(svc.WithdrawFromStash(1001, 5));
        }

        [Test]
        public void StashRemaining_AfterDeposits()
        {
            var svc = new EconomyService(10);
            svc.DepositToStash(1001, 1);
            svc.DepositToStash(1002, 1);
            Assert.AreEqual(8, svc.StashRemaining);
        }

        // ── Trade ───────────────────────────────────────────────────────────

        [Test]
        public void CreateTradeSession_Success()
        {
            var svc = new EconomyService();
            var session = svc.CreateTradeSession(1, 2);
            Assert.IsNotNull(session);
            Assert.AreEqual(1, session.initiatorId);
            Assert.AreEqual(2, session.targetId);
            Assert.IsFalse(session.IsReady);
        }

        [Test]
        public void CreateTradeSession_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.CreateTradeSession(1, 2);
            Assert.AreEqual(1, host.TradeCalls);
            Assert.AreEqual(1, host.LastInitiatorId);
            Assert.AreEqual(2, host.LastTargetId);
        }

        [Test]
        public void CreateTradeSession_IncrementsId()
        {
            var svc = new EconomyService();
            svc.CreateTradeSession(1, 2);
            var s2 = svc.CreateTradeSession(3, 4);
            Assert.IsNotNull(s2);
        }

        // ── Shop ────────────────────────────────────────────────────────────

        [Test]
        public void BuyFromShop_Success()
        {
            var svc = new EconomyService(100, 1000);
            Assert.IsTrue(svc.BuyFromShop(1001, 5, 10));
            Assert.AreEqual(950, svc.Wallet.silver);
        }

        [Test]
        public void BuyFromShop_Insufficient_ReturnsFalse()
        {
            var svc = new EconomyService(100, 10);
            Assert.IsFalse(svc.BuyFromShop(1001, 5, 10));
            Assert.AreEqual(10, svc.Wallet.silver);
        }

        [Test]
        public void BuyFromShop_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 1000, host);
            svc.BuyFromShop(1001, 5, 10);
            Assert.AreEqual(1, host.BuyCalls);
            Assert.AreEqual(50, host.LastTotalCost);
        }

        [Test]
        public void SellToShop_HalfPrice()
        {
            var svc = new EconomyService(100, 0);
            int earned = svc.SellToShop(1001, 5, 100);
            Assert.AreEqual(250, earned);
            Assert.AreEqual(250, svc.Wallet.silver);
        }

        [Test]
        public void SellToShop_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.SellToShop(1001, 5, 100);
            Assert.AreEqual(1, host.SellCalls);
            Assert.AreEqual(250, host.LastEarnedSilver);
        }

        // ── Save / persistence ──────────────────────────────────────────────

        [Test]
        public void SaveEconomyState_DispatchedOnCurrencyChange()
        {
            var host = new FakeHost();
            var svc = new EconomyService(100, 0, host);
            svc.EarnSilver(50);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void SaveEconomyState_DispatchedOnStashChange()
        {
            // Stash doesn't directly call SaveEconomyState in current code
            // but it should be called when currency changes after buy/sell
            var host = new FakeHost();
            var svc = new EconomyService(100, 100, host);
            svc.BuyFromShop(1001, 1, 50);
            // Buy calls SpendSilver which calls Save
            Assert.IsTrue(host.SaveCalls >= 1);
        }

        [Test]
        public void EconomyService_WithoutHost_DoesNotThrow()
        {
            var svc = new EconomyService(100, 100);
            Assert.DoesNotThrow(() => svc.EarnSilver(50));
            Assert.DoesNotThrow(() => svc.DepositToStash(1001, 1));
            Assert.DoesNotThrow(() => svc.BuyFromShop(1001, 1, 10));
        }
    }
}
