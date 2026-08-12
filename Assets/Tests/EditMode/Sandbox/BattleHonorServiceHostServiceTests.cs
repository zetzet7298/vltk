// -----------------------------------------------------------------------------
// VLTK Mobile — BattleHonorService host dispatch tests
// PC source: battlehonor.txt — Vinh Danh Chiến Trường (battlefield honor).
// Verifies IBattleHonorServiceHost receives expected events for load / query /
// score-match / earn.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BattleHonorServiceHostServiceTests
    {
        private sealed class FakeHost : IBattleHonorServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryHonorCount;
            public int RegistryEmptyCalls;

            public int ResolvedCalls;
            public int LastResolvedHonorId;
            public int LastResolvedBattleType;
            public string LastResolvedName;
            public int LastResolvedRequiredScore;
            public string LastResolvedBonusTitle;

            public int ByBattleTypeQueriedCalls;
            public int LastByBattleType;
            public int LastByBattleTypeResultCount;

            public int ForScoreQueriedCalls;
            public int LastForScoreBattleType;
            public int LastForScoreScore;
            public int LastForScoreMatchedHonorId;
            public int LastForScoreMatchedScore;
            public bool LastForScoreFound;

            public int EarnedCalls;
            public int LastEarnedHonorId;
            public int LastEarnedBattleType;
            public int LastEarnedFinalScore;
            public string LastEarnedBonusTitle;

            public int UIShowCalls;
            public int LastUIHonorId;
            public string LastUIName;
            public int LastUIRequiredScore;
            public string LastUIBonusTitle;

            public int LogCalls;
            public int LastLogHonorId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXHonorId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveHonorId;
            public int LastSaveBattleType;
            public int LastSaveCurrentScore;

            public void OnBattleHonorRegistryAttached(int honorCount)
            {
                RegistryAttachedCalls++;
                LastRegistryHonorCount = honorCount;
            }
            public void OnBattleHonorRegistryEmpty() => RegistryEmptyCalls++;
            public void OnHonorResolved(int honorId, int battleType, string nameVi, int requiredScore, string bonusTitle)
            {
                ResolvedCalls++;
                LastResolvedHonorId = honorId;
                LastResolvedBattleType = battleType;
                LastResolvedName = nameVi;
                LastResolvedRequiredScore = requiredScore;
                LastResolvedBonusTitle = bonusTitle;
            }
            public void OnHonorsByBattleTypeQueried(int battleType, int resultCount)
            {
                ByBattleTypeQueriedCalls++;
                LastByBattleType = battleType;
                LastByBattleTypeResultCount = resultCount;
            }
            public void OnHonorForScoreQueried(int battleType, int score, int matchedHonorId, int matchedScore, bool found)
            {
                ForScoreQueriedCalls++;
                LastForScoreBattleType = battleType;
                LastForScoreScore = score;
                LastForScoreMatchedHonorId = matchedHonorId;
                LastForScoreMatchedScore = matchedScore;
                LastForScoreFound = found;
            }
            public void OnHonorEarned(int honorId, int battleType, int finalScore, string bonusTitle)
            {
                EarnedCalls++;
                LastEarnedHonorId = honorId;
                LastEarnedBattleType = battleType;
                LastEarnedFinalScore = finalScore;
                LastEarnedBonusTitle = bonusTitle;
            }
            public void ShowHonorUI(int honorId, string nameVi, int requiredScore, string bonusTitle)
            {
                UIShowCalls++;
                LastUIHonorId = honorId;
                LastUIName = nameVi;
                LastUIRequiredScore = requiredScore;
                LastUIBonusTitle = bonusTitle;
            }
            public void LogBattleHonorEvent(string eventType, int honorId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogHonorId = honorId;
                LastLogDetail = detailVi;
            }
            public void PlayBattleHonorSFX(string action, int honorId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXHonorId = honorId;
            }
            public void SaveBattleHonorState(int honorId, int battleType, int currentScore)
            {
                SaveCalls++;
                LastSaveHonorId = honorId;
                LastSaveBattleType = battleType;
                LastSaveCurrentScore = currentScore;
            }
        }

        private static (PcBattleHonorRegistry reg, PcBattleHonorEntry e1, PcBattleHonorEntry e2) MakeRegistry()
        {
            var reg = new PcBattleHonorRegistry();
            var e1 = new PcBattleHonorEntry
            {
                honorId = 1, battleType = 0, name = "Chiến Binh Tập Sự",
                requiredScore = 100, bonusTitle = "Tân Binh Chiến", bonusEffect = "+5% exp",
            };
            var e2 = new PcBattleHonorEntry
            {
                honorId = 2, battleType = 0, name = "Lão Làng Tối Thượng",
                requiredScore = 500, bonusTitle = "Võ Lâm Cao Thủ", bonusEffect = "+10% damage",
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new BattleHonorService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new BattleHonorService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── RegisterRegistry dispatch ──────────────────────────────────────
        [Test]
        public void RegisterRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BattleHonorService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.RegisterRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistryHonorCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        [Test]
        public void RegisterRegistry_Empty_DispatchesEmpty()
        {
            var host = new FakeHost();
            var svc = new BattleHonorService();
            svc.AttachHost(host);
            svc.RegisterRegistry(null);
            Assert.AreEqual(1, host.RegistryEmptyCalls);
            Assert.AreEqual(0, host.RegistryAttachedCalls);
        }

        // ── GetHonor dispatch ───────────────────────────────────────────────
        [Test]
        public void GetHonor_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var h = svc.GetHonor(1);
            Assert.IsNotNull(h);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedHonorId);
            Assert.AreEqual(0, host.LastResolvedBattleType);
            Assert.AreEqual("Chiến Binh Tập Sự", host.LastResolvedName);
            Assert.AreEqual(100, host.LastResolvedRequiredScore);
            Assert.AreEqual("Tân Binh Chiến", host.LastResolvedBonusTitle);
        }

        [Test]
        public void GetHonor_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var h = svc.GetHonor(9999);
            Assert.IsNull(h);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetByBattleType dispatch ───────────────────────────────────────
        [Test]
        public void GetByBattleType_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            var list = svc.GetByBattleType(0);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, host.ByBattleTypeQueriedCalls);
            Assert.AreEqual(0, host.LastByBattleType);
            Assert.AreEqual(2, host.LastByBattleTypeResultCount);
        }

        [Test]
        public void GetByBattleType_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new BattleHonorService();
            svc.AttachHost(host);
            var list = svc.GetByBattleType(0);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(1, host.ByBattleTypeQueriedCalls);
            Assert.AreEqual(0, host.LastByBattleTypeResultCount);
        }

        [Test]
        public void GetAvailableHonors_Alias_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            var list = svc.GetAvailableHonors(0);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, host.ByBattleTypeQueriedCalls);
        }

        // ── GetHonorForScore dispatch ──────────────────────────────────────
        [Test]
        public void GetHonorForScore_MatchBest_DispatchesFound()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            int baseline = host.ForScoreQueriedCalls;
            var h = svc.GetHonorForScore(0, 250);
            // Both 100 and 500 thresholds. Score 250 matches only the 100.
            Assert.IsNotNull(h);
            Assert.AreEqual(1, h.honorId);
            Assert.AreEqual(baseline + 1, host.ForScoreQueriedCalls);
            Assert.IsTrue(host.LastForScoreFound);
            Assert.AreEqual(1, host.LastForScoreMatchedHonorId); // matched
        }

        [Test]
        public void GetHonorForScore_Highest_DispatchesBest()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            var h = svc.GetHonorForScore(0, 600);
            Assert.IsNotNull(h);
            Assert.AreEqual(2, h.honorId);
            Assert.IsTrue(host.LastForScoreFound);
        }

        [Test]
        public void GetHonorForScore_Zero_DispatchesNotFound()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            var h = svc.GetHonorForScore(0, 0);
            Assert.IsNull(h);
            Assert.AreEqual(1, host.ForScoreQueriedCalls);
            Assert.IsFalse(host.LastForScoreFound);
        }

        [Test]
        public void GetHonorForScore_NoRegistry_DispatchesNotFound()
        {
            var host = new FakeHost();
            var svc = new BattleHonorService();
            svc.AttachHost(host);
            var h = svc.GetHonorForScore(0, 100);
            Assert.IsNull(h);
            Assert.IsFalse(host.LastForScoreFound);
        }

        // ── EarnHonor dispatch ─────────────────────────────────────────────
        [Test]
        public void EarnHonor_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            svc.EarnHonor(1, 150);
            Assert.AreEqual(1, host.EarnedCalls);
            Assert.AreEqual(1, host.LastEarnedHonorId);
            Assert.AreEqual(0, host.LastEarnedBattleType);
            Assert.AreEqual(150, host.LastEarnedFinalScore);
            Assert.AreEqual("Tân Binh Chiến", host.LastEarnedBonusTitle);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("earn", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("earn", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
        }

        [Test]
        public void EarnHonor_Unknown_NoDispatch()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleHonorService(reg);
            svc.AttachHost(host);
            int baseline = host.EarnedCalls;
            svc.EarnHonor(9999, 100);
            Assert.AreEqual(baseline, host.EarnedCalls);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new BattleHonorService();
            Assert.DoesNotThrow(() => svc.RegisterRegistry(null));
            Assert.DoesNotThrow(() => svc.GetHonor(1));
            Assert.DoesNotThrow(() => svc.GetByBattleType(0));
            Assert.DoesNotThrow(() => svc.GetAvailableHonors(0));
            Assert.DoesNotThrow(() => svc.GetHonorForScore(0, 100));
            Assert.DoesNotThrow(() => svc.EarnHonor(1, 100));
        }
    }
}
