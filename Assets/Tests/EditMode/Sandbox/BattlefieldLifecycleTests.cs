// -----------------------------------------------------------------------------
// VLTK Mobile — BattlefieldService EditMode tests.
// Kiểm tra battlefield Tống Kim lifecycle: registry attach, join (gating by
// level + capacity + map existence), state machine (start on first join,
// end with winning team), query APIs.
// PC source: settings/battle/battlefield.txt + lua battlefield_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BattlefieldLifecycleTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IBattlefieldHost
        {
            public int OpeningCalls;
            public int AssignCalls;
            public int JoinedCalls;
            public int LeftCalls;
            public int KillCalls;
            public int RewardCalls;
            public int EndedCalls;
            public int LogCalls;
            public int LastMapId;
            public int LastWinningTeam;
            public int LastReward;
            public int AssignedTeam = 1;

            public void OnBattlefieldOpening(int mapId, int minLevel, int maxLevel, long secondsUntilOpen)
            {
                OpeningCalls++;
                LastMapId = mapId;
            }
            public int AssignPlayerTeam(int mapId, int playerId, int playerFaction)
            {
                AssignCalls++;
                return AssignedTeam;
            }
            public void OnPlayerJoinedBattlefield(int mapId, int playerId, int team, int totalPlayers)
            {
                JoinedCalls++;
            }
            public void OnPlayerLeftBattlefield(int mapId, int playerId, int team, int remainingPlayers) { LeftCalls++; }
            public void OnBattlefieldKill(int mapId, int killerId, int killerTeam, int victimId, int victimTeam) { KillCalls++; }
            public void GrantBattlefieldReward(int playerId, int team, int winningTeam, int score)
            {
                RewardCalls++;
                LastReward = score;
            }
            public void OnBattlefieldEnded(int mapId, int winningTeam, int challengerScore, int defenderScore)
            {
                EndedCalls++;
                LastWinningTeam = winningTeam;
            }
            public void LogBattlefieldEvent(int mapId, string message) { LogCalls++; }
        }

        private static PcBattlefieldRegistry BuildRegistry(params (int mapId, int minLevel, int maxLevel, int maxPlayers)[] rows)
        {
            var reg = new PcBattlefieldRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcBattlefieldEntry
                {
                    mapId = r.mapId,
                    nameVi = $"Chiến Trường {r.mapId}",
                    minLevel = r.minLevel,
                    maxLevel = r.maxLevel,
                    maxPlayers = r.maxPlayers,
                    duration = 1800, // 30 minutes
                });
            }
            return reg;
        }

        private static BattlefieldService BuildService(IBattlefieldHost host = null)
            => new BattlefieldService(BuildRegistry((1, 50, 100, 100), (2, 60, 120, 50)), host);

        // ── BattlefieldJoinResult enum ──────────────────────────────────────

        [Test]
        public void BattlefieldJoinResult_HasFiveOutcomes()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Allowed", "LevelTooLow", "LevelTooHigh", "Full", "NotFound" },
                System.Enum.GetNames(typeof(BattlefieldJoinResult)));
        }

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
            var svc = new BattlefieldService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_EmptyRegistry_StillAttaches()
        {
            var svc = new BattlefieldService();
            svc.AttachRegistry(new PcBattlefieldRegistry());
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_NullRegistry_EmptyState()
        {
            var svc = new BattlefieldService();
            svc.AttachRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetBattlefield_NotFound_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetBattlefield(99));
        }

        [Test]
        public void GetBattlefield_Exists_ReturnsEntry()
        {
            var svc = BuildService();
            var e = svc.GetBattlefield(1);
            Assert.IsNotNull(e);
            Assert.AreEqual(1, e.mapId);
        }

        [Test]
        public void IsBattlefieldMap_True_ForRegistered()
        {
            var svc = BuildService();
            Assert.IsTrue(svc.IsBattlefieldMap(1));
            Assert.IsFalse(svc.IsBattlefieldMap(99));
        }

        [Test]
        public void GetAllBattlefields_ReturnsAll()
        {
            var svc = BuildService();
            Assert.AreEqual(2, Count(svc.GetAllBattlefields()));
        }

        [Test]
        public void GetAllBattlefields_NoRegistry_ReturnsEmpty()
        {
            var svc = new BattlefieldService();
            Assert.AreEqual(0, Count(svc.GetAllBattlefields()));
        }

        [Test]
        public void GetState_NotFound_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetState(99));
        }

        [Test]
        public void GetState_AfterAttach_ReturnsState()
        {
            var svc = BuildService();
            var s = svc.GetState(1);
            Assert.IsNotNull(s);
            Assert.AreEqual(1, s.mapId);
            Assert.IsFalse(s.isActive);
            Assert.AreEqual(0, s.currentPlayers);
        }

        [Test]
        public void GetAllStates_ReturnsAll()
        {
            var svc = BuildService();
            Assert.AreEqual(2, Count(svc.GetAllStates()));
        }

        // ── CanJoin ─────────────────────────────────────────────────────────

        [Test]
        public void CanJoin_NotFound_ReturnsNotFound()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.NotFound, svc.CanJoin(99, 50, 0));
        }

        [Test]
        public void CanJoin_TooLow_ReturnsLevelTooLow()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.LevelTooLow, svc.CanJoin(1, 30, 0));
        }

        [Test]
        public void CanJoin_TooHigh_ReturnsLevelTooHigh()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.LevelTooHigh, svc.CanJoin(1, 150, 0));
        }

        [Test]
        public void CanJoin_Full_ReturnsFull()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.Full, svc.CanJoin(1, 50, 100));
        }

        [Test]
        public void CanJoin_Valid_ReturnsAllowed()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.Allowed, svc.CanJoin(1, 50, 0));
        }

        [Test]
        public void CanJoin_BoundaryMinLevel_Allowed()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.Allowed, svc.CanJoin(1, 50, 0));
        }

        [Test]
        public void CanJoin_BoundaryMaxLevel_Allowed()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.Allowed, svc.CanJoin(1, 100, 0));
        }

        [Test]
        public void CanJoin_BoundaryMaxPlayers_Allowed()
        {
            var svc = BuildService();
            Assert.AreEqual(BattlefieldJoinResult.Allowed, svc.CanJoin(1, 50, 99));
        }

        // ── TryJoin ─────────────────────────────────────────────────────────

        [Test]
        public void TryJoin_ValidLevelAndSpace_Succeeds()
        {
            var svc = BuildService();
            Assert.IsTrue(svc.TryJoin(1, 50));
            Assert.AreEqual(1, svc.GetState(1).currentPlayers);
        }

        [Test]
        public void TryJoin_LevelTooLow_Fails()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.TryJoin(1, 30));
            Assert.AreEqual(0, svc.GetState(1).currentPlayers);
        }

        [Test]
        public void TryJoin_FirstPlayer_ActivatesBattle()
        {
            var svc = BuildService();
            svc.TryJoin(1, 50);
            var s = svc.GetState(1);
            Assert.IsTrue(s.isActive);
            Assert.Greater(s.startTimestamp, 0L);
        }

        [Test]
        public void TryJoin_SecondPlayer_KeepsState()
        {
            var svc = BuildService();
            svc.TryJoin(1, 50);
            long firstStart = svc.GetState(1).startTimestamp;
            svc.TryJoin(1, 75);
            // second join doesn't change startTimestamp
            Assert.AreEqual(firstStart, svc.GetState(1).startTimestamp);
        }

        [Test]
        public void TryJoin_FiresOnPlayerJoinedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            int lastCount = 0;
            svc.OnPlayerJoined += (m, c) => { fired++; lastCount = c; };
            svc.TryJoin(1, 50);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastCount);
        }

        [Test]
        public void TryJoin_HostDispatch_FirstJoinTriggersOpening()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.TryJoin(1, 50);
            Assert.AreEqual(1, host.OpeningCalls);
            Assert.AreEqual(1, host.AssignCalls);
            Assert.AreEqual(1, host.JoinedCalls);
            Assert.GreaterOrEqual(host.LogCalls, 1);
        }

        [Test]
        public void TryJoin_SecondJoinDoesNotReTriggerOpening()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.TryJoin(1, 50);
            svc.TryJoin(1, 60);
            Assert.AreEqual(1, host.OpeningCalls); // only first
            Assert.AreEqual(2, host.JoinedCalls);
        }

        [Test]
        public void TryJoin_NoHost_NoCrash()
        {
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.TryJoin(1, 50));
        }

        // ── EndBattle ───────────────────────────────────────────────────────

        [Test]
        public void EndBattle_Valid_DeactivatesAndDispatches()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.TryJoin(1, 50);
            Assert.IsTrue(svc.EndBattle(1, 1)); // Tống thắng
            var s = svc.GetState(1);
            Assert.IsFalse(s.isActive);
            Assert.AreEqual(1, s.winningTeam);
            Assert.AreEqual(0, s.currentPlayers);
            Assert.AreEqual(1, host.EndedCalls);
            Assert.AreEqual(1, host.LastWinningTeam);
        }

        [Test]
        public void EndBattle_NotFound_ReturnsFalse()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.EndBattle(99, 1));
        }

        [Test]
        public void EndBattle_FiresOnBattleEndedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            int lastTeam = 0;
            svc.OnBattleEnded += (m, t) => { fired++; lastTeam = t; };
            svc.TryJoin(1, 50);
            svc.EndBattle(1, 2);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(2, lastTeam);
        }

        [Test]
        public void EndBattle_RewardsWinningTeam()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.TryJoin(1, 50);
            svc.TryJoin(1, 60);
            svc.TryJoin(1, 70); // 3 players
            host.RewardCalls = 0; host.LastReward = 0;
            svc.EndBattle(1, 1); // team 1 wins
            Assert.AreEqual(2, host.RewardCalls); // 2 teams get reward calls
            Assert.Greater(host.LastReward, 0); // winning team got 3*100=300
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new BattlefieldService(BuildRegistry((1, 50, 100, 100)), host1);
            svc.AttachHost(host2);
            svc.TryJoin(1, 50);
            Assert.AreEqual(0, host1.OpeningCalls);
            Assert.AreEqual(1, host2.OpeningCalls);
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static int Count<T>(System.Collections.Generic.IEnumerable<T> e)
        {
            int n = 0;
            foreach (var _ in e) n++;
            return n;
        }
    }
}
