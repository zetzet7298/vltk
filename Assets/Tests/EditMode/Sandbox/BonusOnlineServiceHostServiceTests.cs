// -----------------------------------------------------------------------------
// VLTK Mobile — BonusOnlineService host dispatch tests
// ST-10.15 Thưởng Online. PC source: settings/bonus_onlinetime/bonus_online.txt.
// Verifies IBonusOnlineServiceHost receives expected events for load / query /
// can-claim / claim / tick / reset.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BonusOnlineServiceHostServiceTests
    {
        private sealed class FakeHost : IBonusOnlineServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryBonusCount;

            public int ResolvedCalls;
            public int LastResolvedBonusId;
            public int LastResolvedRequiredMinutes;
            public int LastResolvedRewardId;
            public int LastResolvedRewardCount;
            public int LastResolvedVipRequired;

            public int ForMinutesQueriedCalls;
            public int LastForMinutes;
            public int LastForMinutesResultCount;

            public int ByVipQueriedCalls;
            public int LastByVip;
            public int LastByVipResultCount;

            public int AllQueriedCalls;
            public int LastAllResultCount;

            public int CanClaimEvaluatedCalls;
            public int LastCanClaimBonusId;
            public bool LastCanClaimResult;
            public int LastCanClaimCurrentMinutes;
            public int LastCanClaimVipLevel;

            public int ClaimDispatchedCalls;
            public int LastClaimBonusId;
            public bool LastClaimSuccess;
            public string LastClaimDetailVi;

            public int TickCalls;
            public int LastTickCurrentMinutes;
            public int LastTickVipLevel;

            public int UIShowCalls;
            public int LastUIBonusId;
            public int LastUIRequiredMinutes;
            public int LastUIRewardId;

            public int LogCalls;
            public int LastLogBonusId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXBonusId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveBonusId;
            public int LastSaveCurrentMinutes;
            public int LastSaveVipLevel;

            public void OnBonusRegistryAttached(int bonusCount)
            {
                RegistryAttachedCalls++;
                LastRegistryBonusCount = bonusCount;
            }
            public void OnBonusResolved(int bonusId, int requiredMinutes, int rewardId, int rewardCount, int vipRequired)
            {
                ResolvedCalls++;
                LastResolvedBonusId = bonusId;
                LastResolvedRequiredMinutes = requiredMinutes;
                LastResolvedRewardId = rewardId;
                LastResolvedRewardCount = rewardCount;
                LastResolvedVipRequired = vipRequired;
            }
            public void OnBonusForMinutesQueried(int minutes, int resultCount)
            {
                ForMinutesQueriedCalls++;
                LastForMinutes = minutes;
                LastForMinutesResultCount = resultCount;
            }
            public void OnBonusByVipQueried(int vipLevel, int resultCount)
            {
                ByVipQueriedCalls++;
                LastByVip = vipLevel;
                LastByVipResultCount = resultCount;
            }
            public void OnAllBonusQueried(int resultCount)
            {
                AllQueriedCalls++;
                LastAllResultCount = resultCount;
            }
            public void OnCanClaimEvaluated(int bonusId, bool canClaim, int currentMinutes, int vipLevel)
            {
                CanClaimEvaluatedCalls++;
                LastCanClaimBonusId = bonusId;
                LastCanClaimResult = canClaim;
                LastCanClaimCurrentMinutes = currentMinutes;
                LastCanClaimVipLevel = vipLevel;
            }
            public void OnBonusClaimDispatched(int bonusId, bool success, string detailVi)
            {
                ClaimDispatchedCalls++;
                LastClaimBonusId = bonusId;
                LastClaimSuccess = success;
                LastClaimDetailVi = detailVi;
            }
            public void OnOnlineTick(int currentMinutes, int vipLevel)
            {
                TickCalls++;
                LastTickCurrentMinutes = currentMinutes;
                LastTickVipLevel = vipLevel;
            }
            public void ShowBonusUI(int bonusId, int requiredMinutes, int rewardId)
            {
                UIShowCalls++;
                LastUIBonusId = bonusId;
                LastUIRequiredMinutes = requiredMinutes;
                LastUIRewardId = rewardId;
            }
            public void LogBonusEvent(string eventType, int bonusId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogBonusId = bonusId;
                LastLogDetail = detailVi;
            }
            public void PlayBonusSFX(string action, int bonusId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXBonusId = bonusId;
            }
            public void SaveBonusState(int bonusId, int currentMinutes, int vipLevel)
            {
                SaveCalls++;
                LastSaveBonusId = bonusId;
                LastSaveCurrentMinutes = currentMinutes;
                LastSaveVipLevel = vipLevel;
            }
        }

        private static PcBonusOnlineRegistry MakeRegistry()
        {
            var reg = new PcBonusOnlineRegistry();
            reg.Register(new PcBonusOnlineEntry
            {
                bonusId = 1, requiredMinutes = 30, rewardType = 1, rewardId = 5001, rewardCount = 1, vipRequired = 0,
            });
            reg.Register(new PcBonusOnlineEntry
            {
                bonusId = 2, requiredMinutes = 60, rewardType = 1, rewardId = 5002, rewardCount = 5, vipRequired = 2,
            });
            return reg;
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new BonusOnlineService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new BonusOnlineService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── GetBonus dispatch ───────────────────────────────────────────────
        [Test]
        public void GetBonus_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var b = svc.GetBonus(1);
            Assert.IsNotNull(b);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedBonusId);
            Assert.AreEqual(30, host.LastResolvedRequiredMinutes);
            Assert.AreEqual(5001, host.LastResolvedRewardId);
            Assert.AreEqual(1, host.LastResolvedRewardCount);
            Assert.AreEqual(0, host.LastResolvedVipRequired);
        }

        [Test]
        public void GetBonus_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var b = svc.GetBonus(9999);
            Assert.IsNull(b);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetBonusForMinutes dispatch ─────────────────────────────────────
        [Test]
        public void GetBonusForMinutes_DispatchesHostCount()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            var list = svc.GetBonusForMinutes(30);
            Assert.GreaterOrEqual(list.Count, 1);
            Assert.AreEqual(1, host.ForMinutesQueriedCalls);
            Assert.AreEqual(30, host.LastForMinutes);
        }

        // ── GetBonusByVip dispatch ──────────────────────────────────────────
        [Test]
        public void GetBonusByVip_DispatchesHostCount()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            var list = svc.GetBonusByVip(2);
            Assert.GreaterOrEqual(list.Count, 1);
            Assert.AreEqual(1, host.ByVipQueriedCalls);
            Assert.AreEqual(2, host.LastByVip);
        }

        // ── GetAll dispatch ────────────────────────────────────────────────
        [Test]
        public void GetAll_DispatchesHostCount()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            int n = 0;
            foreach (var _ in svc.GetAll()) n++;
            Assert.AreEqual(2, n);
            Assert.AreEqual(1, host.AllQueriedCalls);
            Assert.AreEqual(2, host.LastAllResultCount);
        }

        // ── CanClaim dispatch ──────────────────────────────────────────────
        [Test]
        public void CanClaim_Eligible_DispatchesHostTrue()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            bool result = svc.CanClaim(1, 60, 0); // 60 minutes >= 30, vip 0 >= 0
            Assert.IsTrue(result);
            Assert.AreEqual(1, host.CanClaimEvaluatedCalls);
            Assert.IsTrue(host.LastCanClaimResult);
        }

        [Test]
        public void CanClaim_NotEnoughMinutes_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            bool result = svc.CanClaim(2, 30, 5); // 30 < 60
            Assert.IsFalse(result);
            Assert.IsFalse(host.LastCanClaimResult);
        }

        [Test]
        public void CanClaim_NotEnoughVip_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            bool result = svc.CanClaim(2, 100, 0); // vip 0 < 2
            Assert.IsFalse(result);
            Assert.IsFalse(host.LastCanClaimResult);
        }

        [Test]
        public void CanClaim_Missing_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            bool result = svc.CanClaim(9999, 100, 5);
            Assert.IsFalse(result);
            Assert.IsFalse(host.LastCanClaimResult);
        }

        // ── MarkClaimed dispatch ───────────────────────────────────────────
        [Test]
        public void MarkClaimed_Success_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            bool result = svc.MarkClaimed(1);
            Assert.IsTrue(result);
            Assert.AreEqual(1, host.ClaimDispatchedCalls);
            Assert.IsTrue(host.LastClaimSuccess);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("claim", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("claim", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
        }

        [Test]
        public void MarkClaimed_AlreadyClaimed_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            svc.MarkClaimed(1);
            int baseline = host.ClaimDispatchedCalls;
            bool result = svc.MarkClaimed(1);
            Assert.IsFalse(result);
            Assert.AreEqual(baseline + 1, host.ClaimDispatchedCalls);
            Assert.IsFalse(host.LastClaimSuccess);
        }

        [Test]
        public void MarkClaimed_Missing_DispatchesHostFalse()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            bool result = svc.MarkClaimed(9999);
            Assert.IsFalse(result);
            Assert.AreEqual(1, host.ClaimDispatchedCalls);
            Assert.IsFalse(host.LastClaimSuccess);
        }

        // ── ResetClaims dispatch ───────────────────────────────────────────
        [Test]
        public void ResetClaims_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.LogCalls;
            svc.MarkClaimed(1);
            svc.ResetClaims();
            Assert.AreEqual(baseline + 2, host.LogCalls); // claim + reset
            Assert.AreEqual("reset", host.LastLogEventType);
        }

        // ── Tick dispatch ──────────────────────────────────────────────────
        [Test]
        public void Tick_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BonusOnlineService(MakeRegistry());
            svc.AttachHost(host);
            svc.Tick(45, 1);
            Assert.AreEqual(1, host.TickCalls);
            Assert.AreEqual(45, host.LastTickCurrentMinutes);
            Assert.AreEqual(1, host.LastTickVipLevel);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new BonusOnlineService(MakeRegistry());
            Assert.DoesNotThrow(() => svc.GetBonus(1));
            Assert.DoesNotThrow(() => svc.GetBonusForMinutes(30));
            Assert.DoesNotThrow(() => svc.GetBonusByVip(2));
            foreach (var _ in svc.GetAll()) { }
            Assert.DoesNotThrow(() => svc.CanClaim(1, 100, 5));
            Assert.DoesNotThrow(() => svc.MarkClaimed(1));
            Assert.DoesNotThrow(() => svc.ResetClaims());
            Assert.DoesNotThrow(() => svc.Tick(0, 0));
        }
    }
}
