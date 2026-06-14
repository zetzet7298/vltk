// -----------------------------------------------------------------------------
// VLTK Mobile — BattleAwardService EditMode tests.
// Kiểm tra phần thưởng chiến đấu: registry attach, GetAward/GetByBattleType/
// GetByRank, GrantAward (dispatch chain including top-rank broadcast), no
// reward variants.
// PC source: settings/battleaward.txt + lua battle_award_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BattleAwardGrantTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IBattleAwardHost
        {
            public int ReceivedCalls;
            public int SfxCalls;
            public int NoticeCalls;
            public int BroadcastCalls;
            public int SilverCalls;
            public int ExpCalls;
            public int ItemCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastAwardId;
            public int LastBattleType;
            public int LastRank;
            public int LastSilver;
            public int LastExp;
            public int LastItem;
            public int LastItemCount;

            public void OnAwardReceived(int playerId, int awardId, int battleType, int rank, int rewardSilver, int rewardExp, int rewardItem)
            {
                ReceivedCalls++;
                LastPlayerId = playerId;
                LastAwardId = awardId;
                LastBattleType = battleType;
                LastRank = rank;
                LastSilver = rewardSilver;
                LastExp = rewardExp;
                LastItem = rewardItem;
            }
            public void PlayAwardSFX(int playerId, int battleType, int rank) { SfxCalls++; }
            public void ShowAwardNotice(int playerId, int battleType, int rank, int rewardSilver, int rewardExp) { NoticeCalls++; }
            public void BroadcastTopRank(int playerId, int battleType, int rank) { BroadcastCalls++; }
            public void GrantSilver(int playerId, int silver) { SilverCalls++; LastSilver = silver; }
            public void GrantExp(int playerId, int exp) { ExpCalls++; LastExp = exp; }
            public void GrantItem(int playerId, int itemId, int count)
            {
                ItemCalls++;
                LastItem = itemId;
                LastItemCount = count;
            }
            public void SaveAwardHistory(int playerId, int awardId, int battleType, int rank, long timestamp) { SaveCalls++; }
        }

        private static PcBattleAwardRegistry BuildRegistry(params (int id, int type, int rank, int silver, int exp, int item)[] rows)
        {
            var reg = new PcBattleAwardRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcBattleAwardEntry
                {
                    awardId = r.id,
                    battleType = r.type,
                    rank = r.rank,
                    rewardSilver = r.silver,
                    rewardExp = r.exp,
                    rewardItem = r.item,
                });
            }
            return reg;
        }

        // ── Ctor / count ────────────────────────────────────────────────────

        [Test]
        public void Count_EmptyService_Zero()
        {
            var svc = new BattleAwardService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Count_AfterRegistry()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0), (2, 0, 2, 50, 25, 0));
            var svc = new BattleAwardService(reg);
            Assert.AreEqual(2, svc.Count);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            svc.AttachHost(host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, host.ReceivedCalls);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetAward_NotFound_ReturnsNull()
        {
            var svc = new BattleAwardService();
            Assert.IsNull(svc.GetAward(99));
        }

        [Test]
        public void GetAward_Exists_ReturnsEntry()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            var e = svc.GetAward(1);
            Assert.IsNotNull(e);
            Assert.AreEqual(100, e.rewardSilver);
        }

        [Test]
        public void GetByBattleType_Empty()
        {
            var svc = new BattleAwardService();
            Assert.AreEqual(0, svc.GetByBattleType(0).Count);
        }

        [Test]
        public void GetByBattleType_Filters()
        {
            var reg = BuildRegistry(
                (1, 0, 1, 100, 50, 0),
                (2, 1, 1, 200, 100, 0),
                (3, 0, 2, 50, 25, 0)
            );
            var svc = new BattleAwardService(reg);
            Assert.AreEqual(2, svc.GetByBattleType(0).Count);
            Assert.AreEqual(1, svc.GetByBattleType(1).Count);
        }

        [Test]
        public void GetByRank_Filters()
        {
            var reg = BuildRegistry(
                (1, 0, 1, 100, 50, 0),
                (2, 0, 2, 50, 25, 0),
                (3, 1, 1, 200, 100, 0)
            );
            var svc = new BattleAwardService(reg);
            Assert.AreEqual(2, svc.GetByRank(1).Count);
        }

        [Test]
        public void GetAllAwards_Empty()
        {
            var svc = new BattleAwardService();
            Assert.AreEqual(0, Count(svc.GetAllAwards()));
        }

        [Test]
        public void GetAllAwards_AfterRegistry()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0), (2, 0, 2, 50, 25, 0));
            var svc = new BattleAwardService(reg);
            Assert.AreEqual(2, Count(svc.GetAllAwards()));
        }

        // ── GrantAward ───────────────────────────────────────────────────────

        [Test]
        public void GrantAward_NotFound_ReturnsFalse()
        {
            var svc = new BattleAwardService();
            Assert.IsFalse(svc.GrantAward(1, 99));
        }

        [Test]
        public void GrantAward_Success_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            Assert.IsTrue(svc.GrantAward(1, 1));
        }

        [Test]
        public void GrantAward_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, host.ReceivedCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.NoticeCalls);
            Assert.AreEqual(1, host.SilverCalls);
            Assert.AreEqual(1, host.ExpCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void GrantAward_HostArgs()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, host.LastPlayerId);
            Assert.AreEqual(1, host.LastAwardId);
            Assert.AreEqual(0, host.LastBattleType);
            Assert.AreEqual(1, host.LastRank);
            Assert.AreEqual(100, host.LastSilver);
            Assert.AreEqual(50, host.LastExp);
        }

        [Test]
        public void GrantAward_NoSilver_NotGrantsSilver()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 0, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(0, host.SilverCalls);
        }

        [Test]
        public void GrantAward_NoExp_NotGrantsExp()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 0, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(0, host.ExpCalls);
        }

        [Test]
        public void GrantAward_HasItem_GrantsItem()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 500));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, host.ItemCalls);
            Assert.AreEqual(500, host.LastItem);
        }

        [Test]
        public void GrantAward_NoItem_NotGrantsItem()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(0, host.ItemCalls);
        }

        [Test]
        public void GrantAward_TopRank_Broadcasts()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, host.BroadcastCalls);
        }

        [Test]
        public void GrantAward_NonTopRank_NoBroadcast()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 2, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAward(1, 1);
            Assert.AreEqual(0, host.BroadcastCalls);
        }

        [Test]
        public void GrantAward_FiresOnAwardGrantedEvent()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            int fired = 0;
            int lastAward = 0;
            svc.OnAwardGranted += (pl, aw) => { fired++; lastAward = aw; };
            svc.GrantAward(1, 1);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastAward);
        }

        [Test]
        public void GrantAward_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            Assert.DoesNotThrow(() => svc.GrantAward(1, 1));
        }

        // ── GrantAwardByRank ─────────────────────────────────────────────────

        [Test]
        public void GrantAwardByRank_Exists()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0), (2, 0, 2, 50, 25, 0));
            var svc = new BattleAwardService(reg);
            Assert.IsTrue(svc.GrantAwardByRank(1, 0, 1));
        }

        [Test]
        public void GrantAwardByRank_NotFound()
        {
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg);
            Assert.IsFalse(svc.GrantAwardByRank(1, 0, 99));
        }

        [Test]
        public void GrantAwardByRank_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 0, 1, 100, 50, 0));
            var svc = new BattleAwardService(reg, host);
            svc.GrantAwardByRank(1, 0, 1);
            Assert.AreEqual(1, host.ReceivedCalls);
        }

        // ── RegisterRegistry ─────────────────────────────────────────────────

        [Test]
        public void RegisterRegistry_FiresCount()
        {
            var svc = new BattleAwardService();
            svc.RegisterRegistry(BuildRegistry((1, 0, 1, 100, 50, 0)));
            Assert.AreEqual(1, svc.Count);
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
