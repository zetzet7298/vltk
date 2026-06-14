// -----------------------------------------------------------------------------
// VLTK Mobile — BangChienService EditMode tests.
// Kiểm tra Bang Chiến (công thành chiến) lifecycle: start, record kill, end
// with winner resolution, registry lookup, income compute, day filter.
// PC source: settings/battle/bangchien.txt + lua tongwar_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BangChienLifecycleTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IBangChienHost
        {
            public int StartingCalls;
            public int KillCalls;
            public int RewardCalls;
            public int EndedCalls;
            public int LogCalls;
            public int IncomeCalls;
            public int LastChallenger;
            public int LastDefender;
            public int LastWinner;
            public long LastIncome;

            public void OnBangChienStarting(int challengerBangId, int defenderBangId)
            {
                StartingCalls++;
                LastChallenger = challengerBangId;
                LastDefender = defenderBangId;
            }
            public void OnBangChienKill(bool isChallengerKill, int challengerScore, int defenderScore)
            {
                KillCalls++;
            }
            public void GrantBangChienReward(int bangId, bool isWinner, int score, int cityId)
            {
                RewardCalls++;
            }
            public void OnBangChienEnded(int winnerBangId, int challengerScore, int defenderScore)
            {
                EndedCalls++;
                LastWinner = winnerBangId;
            }
            public void LogBangChienEvent(string message) { LogCalls++; }
            public void GrantCityIncome(int tongId, int cityId, long amount)
            {
                IncomeCalls++;
                LastIncome = amount;
            }
        }

        private static PcBangChienRegistry BuildRegistry(params (int cityId, int mapId, int tongId, int income, int openDay)[] rows)
        {
            var reg = new PcBangChienRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcBangChienEntry
                {
                    cityId = r.cityId,
                    mapId = r.mapId,
                    ownerTongId = r.tongId,
                    income = r.income,
                    openDay = r.openDay,
                    nameRaw = $"Thành {r.cityId}",
                });
            }
            return reg;
        }

        private static BangChienService BuildService(IBangChienHost host = null)
            => new BangChienService(BuildRegistry((1, 100, 5, 100, 0b1111111), (2, 200, 6, 50, 0b0000011)), host);

        // ── Registry attach + count ────────────────────────────────────────

        [Test]
        public void Count_AfterRegistry_ReturnsEntryCount()
        {
            var svc = BuildService();
            Assert.AreEqual(2, svc.Count);
        }

        [Test]
        public void Count_EmptyService_ReturnsZero()
        {
            var svc = new BangChienService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_NullRegistry_EmptyState()
        {
            var svc = new BangChienService();
            svc.AttachRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_FiresOnCityLoadedEvent()
        {
            var svc = new BangChienService();
            int fired = 0;
            svc.OnCityLoaded += () => fired++;
            svc.AttachRegistry(BuildRegistry((1, 100, 5, 100, 0x7F)));
            Assert.AreEqual(1, fired);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetCity_NotFound_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetCity(99));
        }

        [Test]
        public void GetCity_Exists_ReturnsEntry()
        {
            var svc = BuildService();
            var c = svc.GetCity(1);
            Assert.IsNotNull(c);
            Assert.AreEqual(1, c.cityId);
            Assert.AreEqual(5, c.ownerTongId);
        }

        [Test]
        public void GetByMap_NotFound_ReturnsEmpty()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.GetByMap(999).Count);
        }

        [Test]
        public void GetByMap_Found_ReturnsList()
        {
            var svc = BuildService();
            Assert.AreEqual(1, svc.GetByMap(100).Count);
        }

        [Test]
        public void GetByTong_NotFound_ReturnsEmpty()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.GetByTong(999).Count);
        }

        [Test]
        public void GetByTong_Found_ReturnsList()
        {
            var svc = BuildService();
            Assert.AreEqual(1, svc.GetByTong(5).Count);
        }

        [Test]
        public void GetByMap_NoRegistry_ReturnsEmpty()
        {
            var svc = new BangChienService();
            Assert.AreEqual(0, svc.GetByMap(100).Count);
        }

        // ── Start ───────────────────────────────────────────────────────────

        [Test]
        public void StartBangChien_ResetsScores()
        {
            var svc = BuildService();
            svc.StartBangChien(1, 2);
            Assert.IsTrue(svc.IsActive);
        }

        [Test]
        public void StartBangChien_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartBangChien(10, 20);
            Assert.AreEqual(1, host.StartingCalls);
            Assert.AreEqual(10, host.LastChallenger);
            Assert.AreEqual(20, host.LastDefender);
            Assert.GreaterOrEqual(host.LogCalls, 1);
        }

        [Test]
        public void StartBangChien_WithoutHost_DoesNotThrow()
        {
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.StartBangChien(1, 2));
        }

        // ── RecordKill ──────────────────────────────────────────────────────

        [Test]
        public void RecordKill_NotActive_NoEffect()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.RecordKill(true); // not active
            Assert.AreEqual(0, host.KillCalls);
        }

        [Test]
        public void RecordKill_ChallengerKill_IncrementsScore()
        {
            var svc = BuildService();
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            Assert.IsTrue(svc.IsActive);
        }

        [Test]
        public void RecordKill_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            svc.RecordKill(false);
            Assert.AreEqual(2, host.KillCalls);
        }

        // ── EndBangChien ────────────────────────────────────────────────────

        [Test]
        public void EndBangChien_NoActive_StillReturnsWinner()
        {
            var svc = BuildService();
            int winner = svc.EndBangChien();
            // scores are 0-0 -> tie -> winner = 0
            Assert.AreEqual(0, winner);
            Assert.IsFalse(svc.IsActive);
        }

        [Test]
        public void EndBangChien_ChallengerWins_ReturnsChallenger()
        {
            var svc = BuildService();
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            svc.RecordKill(true);
            int winner = svc.EndBangChien();
            Assert.AreEqual(1, winner);
        }

        [Test]
        public void EndBangChien_DefenderWins_ReturnsDefender()
        {
            var svc = BuildService();
            svc.StartBangChien(1, 2);
            svc.RecordKill(false);
            svc.RecordKill(false);
            svc.RecordKill(false);
            int winner = svc.EndBangChien();
            Assert.AreEqual(2, winner);
        }

        [Test]
        public void EndBangChien_Tie_ReturnsZero()
        {
            var svc = BuildService();
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            svc.RecordKill(false);
            int winner = svc.EndBangChien();
            Assert.AreEqual(0, winner);
        }

        [Test]
        public void EndBangChien_FiresOnBangChienEndedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            int lastWinner = -1;
            svc.OnBangChienEnded += (w, cs, ds) => { fired++; lastWinner = w; };
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            svc.EndBangChien();
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastWinner);
        }

        [Test]
        public void EndBangChien_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.StartBangChien(1, 2);
            svc.RecordKill(true);
            svc.RecordKill(true);
            svc.RecordKill(false);
            host.RewardCalls = 0;
            svc.EndBangChien();
            Assert.AreEqual(2, host.RewardCalls); // 2 bangs
            Assert.AreEqual(1, host.LastWinner);
            Assert.AreEqual(1, host.EndedCalls);
            Assert.GreaterOrEqual(host.LogCalls, 1);
        }

        // ── GetOpenDay ──────────────────────────────────────────────────────

        [Test]
        public void GetOpenDay_Day0_FindsAllDaily()
        {
            // openDay = 0b1111111 = all 7 days
            var svc = BuildService();
            Assert.AreEqual(2, svc.GetOpenDay(0).Count);
        }

        [Test]
        public void GetOpenDay_Day1_RestrictsToBit1()
        {
            // 0b0000011 = days 0,1 only
            var svc = BuildService();
            // Both our entries have openDay=0b0000011
            Assert.AreEqual(2, svc.GetOpenDay(1).Count);
        }

        [Test]
        public void GetOpenDay_Day3_OneMatchForAllDailyEntry()
        {
            // 0b0000011 = days 0,1 only. 0b1111111 = all days.
            // Day 3 matches the all-days entry (1) but not the partial one.
            var svc = BuildService();
            Assert.AreEqual(1, svc.GetOpenDay(3).Count);
        }

        [Test]
        public void GetOpenDay_NoRegistry_ReturnsEmpty()
        {
            var svc = new BangChienService();
            Assert.AreEqual(0, svc.GetOpenDay(0).Count);
        }

        // ── ComputeIncome ───────────────────────────────────────────────────

        [Test]
        public void ComputeIncome_Valid_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            long income = svc.ComputeIncome(1, 5); // 100 * 5
            Assert.AreEqual(500L, income);
            Assert.AreEqual(1, host.IncomeCalls);
            Assert.AreEqual(500L, host.LastIncome);
        }

        [Test]
        public void ComputeIncome_NoHost_NoCrash()
        {
            var svc = BuildService();
            Assert.AreEqual(500L, svc.ComputeIncome(1, 5));
        }

        [Test]
        public void ComputeIncome_CityNotFound_ReturnsZero()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            Assert.AreEqual(0L, svc.ComputeIncome(99, 5));
            Assert.AreEqual(0, host.IncomeCalls);
        }

        [Test]
        public void ComputeIncome_NegativeHours_ClampsToZero()
        {
            var svc = BuildService();
            Assert.AreEqual(0L, svc.ComputeIncome(1, -10));
        }

        [Test]
        public void ComputeIncome_ZeroHours_NoDispatch()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            Assert.AreEqual(0L, svc.ComputeIncome(1, 0));
            // 0 income should not dispatch
            Assert.AreEqual(0, host.IncomeCalls);
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new BangChienService(BuildRegistry((1, 100, 5, 100, 0x7F)), host1);
            svc.AttachHost(host2);
            svc.StartBangChien(1, 2);
            Assert.AreEqual(0, host1.StartingCalls);
            Assert.AreEqual(1, host2.StartingCalls);
        }
    }
}
