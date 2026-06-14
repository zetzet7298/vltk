// -----------------------------------------------------------------------------
// VLTK Mobile — AdventureService EditMode tests.
// Kiểm tra mạo hiểm lifecycle: registry attach, mark completed (UI pin +
// progress update + reward + 100% broadcast), Clear, query APIs.
// PC source: settings/adventure.txt + lua adventure_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AdventureCompleteTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IAdventureHost
        {
            public int PinCalls;
            public int CompletedCalls;
            public int RewardCalls;
            public int ProgressCalls;
            public int LogCalls;
            public int AllDoneCalls;
            public int SaveCalls;
            public int LastAdvId;
            public int LastMapId;
            public int LastPlayerId;
            public int LastCompletedCount;
            public int LastTotalCount;
            public float LastRatio;
            public int LastRewardItem;
            public int LastRewardCount;
            public int LastAllDoneCount;
            public bool LastPinCompleted;

            public void ShowMapPin(int advId, int mapId, bool isCompleted)
            {
                PinCalls++;
                LastAdvId = advId;
                LastMapId = mapId;
                LastPinCompleted = isCompleted;
            }
            public void OnAdventureCompleted(int playerId, int advId, string adventureName, int mapId)
            {
                CompletedCalls++;
                LastPlayerId = playerId;
            }
            public void GrantAdventureReward(int playerId, int advId, int rewardItem, int rewardCount)
            {
                RewardCalls++;
                LastRewardItem = rewardItem;
                LastRewardCount = rewardCount;
            }
            public void UpdateProgress(int playerId, int completed, int total, float ratio)
            {
                ProgressCalls++;
                LastCompletedCount = completed;
                LastTotalCount = total;
                LastRatio = ratio;
            }
            public void LogAdventureEvent(int playerId, int advId, string message) { LogCalls++; }
            public void OnAllAdventuresCompleted(int playerId, int totalCount)
            {
                AllDoneCalls++;
                LastAllDoneCount = totalCount;
            }
            public void SaveAdventureProgress(int playerId, int advId, bool completed) { SaveCalls++; }
        }

        private static PcAdventureRegistry BuildRegistry(params (int id, int mapId, string name, string extra0, string extra1)[] rows)
        {
            var reg = new PcAdventureRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcAdventureEntry
                {
                    id = r.id,
                    mapId = r.mapId,
                    nameRaw = r.name,
                    extra0 = r.extra0,
                    extra1 = r.extra1,
                });
            }
            return reg;
        }

        // ── Registry attach + count ────────────────────────────────────────

        [Test]
        public void Count_AfterRegistry_ReturnsEntryCount()
        {
            var reg = BuildRegistry((1, 1, "X", null, null), (2, 1, "Y", null, null));
            var svc = new AdventureService(reg);
            Assert.AreEqual(2, svc.Count);
        }

        [Test]
        public void Count_NullRegistry_Zero()
        {
            var svc = new AdventureService();
            Assert.AreEqual(0, svc.Count);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetAdventure_NotFound_ReturnsNull()
        {
            var svc = new AdventureService();
            Assert.IsNull(svc.GetAdventure(99));
        }

        [Test]
        public void GetAdventure_Exists_ReturnsEntry()
        {
            var reg = BuildRegistry((1, 1, "Foo", null, null));
            var svc = new AdventureService(reg);
            var adv = svc.GetAdventure(1);
            Assert.IsNotNull(adv);
            Assert.AreEqual("Foo", adv.nameRaw);
        }

        [Test]
        public void GetAllAdventures_Empty()
        {
            var svc = new AdventureService();
            Assert.AreEqual(0, Count(svc.GetAllAdventures()));
        }

        [Test]
        public void GetAdventuresForMap_FiltersByMapId()
        {
            var reg = BuildRegistry((1, 100, "A", null, null), (2, 200, "B", null, null), (3, 100, "C", null, null));
            var svc = new AdventureService(reg);
            Assert.AreEqual(2, Count(svc.GetAdventuresForMap(100)));
            Assert.AreEqual(1, Count(svc.GetAdventuresForMap(200)));
        }

        [Test]
        public void GetMapAdventureCount()
        {
            var reg = BuildRegistry((1, 100, "A", null, null), (2, 200, "B", null, null), (3, 100, "C", null, null));
            var svc = new AdventureService(reg);
            Assert.AreEqual(2, svc.GetMapAdventureCount(100));
            Assert.AreEqual(1, svc.GetMapAdventureCount(200));
            Assert.AreEqual(0, svc.GetMapAdventureCount(999));
        }

        // ── MarkCompleted ────────────────────────────────────────────────────

        [Test]
        public void MarkCompleted_FirstTime_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            Assert.IsTrue(svc.MarkCompleted(1));
            Assert.IsTrue(svc.IsCompleted(1));
        }

        [Test]
        public void MarkCompleted_Duplicate_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            svc.MarkCompleted(1);
            Assert.IsFalse(svc.MarkCompleted(1));
        }

        [Test]
        public void MarkCompleted_FiresOnAdventureCompletedEvent()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            int fired = 0;
            int lastId = 0;
            svc.OnAdventureCompleted += id => { fired++; lastId = id; };
            svc.MarkCompleted(1);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastId);
        }

        [Test]
        public void MarkCompleted_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 100, "Foo", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(1, host.PinCalls);
            Assert.AreEqual(1, host.CompletedCalls);
            Assert.AreEqual(1, host.ProgressCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void MarkCompleted_HostArgsCorrect()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((42, 7, "Adventure42", null, null));
            var svc = new AdventureService(reg, host) { PlayerId = 100 };
            svc.MarkCompleted(42);
            Assert.AreEqual(42, host.LastAdvId);
            Assert.AreEqual(7, host.LastMapId);
            Assert.IsTrue(host.LastPinCompleted);
            Assert.AreEqual(100, host.LastPlayerId);
        }

        [Test]
        public void MarkCompleted_GrantsRewardIfExtra()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", "500", "3")); // reward item 500 x3
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(1, host.RewardCalls);
            Assert.AreEqual(500, host.LastRewardItem);
            Assert.AreEqual(3, host.LastRewardCount);
        }

        [Test]
        public void MarkCompleted_NoExtra_NoReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(0, host.RewardCalls);
        }

        [Test]
        public void MarkCompleted_UpdatesProgress()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null), (2, 1, "Y", null, null), (3, 1, "Z", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(1, host.LastCompletedCount);
            Assert.AreEqual(3, host.LastTotalCount);
            Assert.AreEqual(1f / 3f, host.LastRatio, 0.01f);
        }

        [Test]
        public void MarkCompleted_AllDone_TriggersBroadcast()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(1, host.AllDoneCalls);
            Assert.AreEqual(1, host.LastAllDoneCount);
        }

        [Test]
        public void MarkCompleted_AllDone_FiresOnAllCompletedEvent()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            int fired = 0;
            svc.OnAllCompleted += () => fired++;
            svc.MarkCompleted(1);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void MarkCompleted_PartialCompletion_NoAllDone()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null), (2, 1, "Y", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompleted(1);
            Assert.AreEqual(0, host.AllDoneCalls);
        }

        [Test]
        public void MarkCompleted_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            Assert.DoesNotThrow(() => svc.MarkCompleted(1));
        }

        // ── MarkCompletedFor ────────────────────────────────────────────────

        [Test]
        public void MarkCompletedFor_TempPlayerId()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg, host);
            svc.MarkCompletedFor(42, 1);
            Assert.AreEqual(42, host.LastPlayerId);
            Assert.AreEqual(0, svc.PlayerId); // restored to 0
        }

        // ── IsCompleted / CompletedCount / CompletionRatio ─────────────────

        [Test]
        public void IsCompleted_AfterMark_ReturnsTrue()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            svc.MarkCompleted(1);
            Assert.IsTrue(svc.IsCompleted(1));
        }

        [Test]
        public void CompletedCount_Accumulates()
        {
            var reg = BuildRegistry((1, 1, "X", null, null), (2, 1, "Y", null, null));
            var svc = new AdventureService(reg);
            svc.MarkCompleted(1);
            svc.MarkCompleted(2);
            Assert.AreEqual(2, svc.CompletedCount);
        }

        [Test]
        public void CompletionRatio_EmptyRegistry_Zero()
        {
            var svc = new AdventureService();
            Assert.AreEqual(0f, svc.CompletionRatio);
        }

        [Test]
        public void CompletionRatio_HalfCompleted()
        {
            var reg = BuildRegistry((1, 1, "X", null, null), (2, 1, "Y", null, null));
            var svc = new AdventureService(reg);
            svc.MarkCompleted(1);
            Assert.AreEqual(0.5f, svc.CompletionRatio, 0.01f);
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_ResetsCompleted()
        {
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg);
            svc.MarkCompleted(1);
            svc.Clear();
            Assert.AreEqual(0, svc.CompletedCount);
            Assert.IsFalse(svc.IsCompleted(1));
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry((1, 1, "X", null, null));
            var svc = new AdventureService(reg, host1);
            svc.AttachHost(host2);
            svc.MarkCompleted(1);
            Assert.AreEqual(0, host1.PinCalls);
            Assert.AreEqual(1, host2.PinCalls);
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
