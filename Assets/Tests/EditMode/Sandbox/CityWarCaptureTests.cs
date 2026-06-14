// -----------------------------------------------------------------------------
// VLTK Mobile — CityWarService EditMode tests.
// Kiểm tra thành chiến lifecycle: registry attach, capture (dispatch chain
// including capture reward), AddDefender (host dispatch), ResetAll.
// PC source: settings/event/citywar.ini + lua citywar_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class CityWarCaptureTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : ICityWarHost
        {
            public int OwnerChangedCalls;
            public int DefenderNpcCalls;
            public int MarkerCalls;
            public int SfxCalls;
            public int RewardCalls;
            public int LogCalls;
            public int BoardCalls;
            public int ResetCalls;
            public int LastCityId;
            public int LastOldOwner;
            public int LastNewOwner;
            public int LastDefenderCount;
            public int LastRewardItem;
            public int LastRewardCount;
            public int LastTotalCities;
            public int LastNeutralCount;
            public long LastCaptureTimestamp;
            public string LastCityName;
            public string LastMessage;
            public System.Collections.Generic.Dictionary<int, int> RewardByCity = new();
            public System.Collections.Generic.Dictionary<int, int> RewardCountByCity = new();

            public void OnCityOwnerChanged(int cityId, int oldOwnerFaction, int newOwnerFaction, string cityName)
            {
                OwnerChangedCalls++;
                LastCityId = cityId;
                LastOldOwner = oldOwnerFaction;
                LastNewOwner = newOwnerFaction;
                LastCityName = cityName;
            }
            public void UpdateDefenderNpcs(int cityId, int factionId, int defenderCount)
            {
                DefenderNpcCalls++;
                LastDefenderCount = defenderCount;
            }
            public void ShowCityMarker(int cityId, int ownerFaction, string cityName) { MarkerCalls++; }
            public void PlayCaptureSFX(int cityId, int newOwnerFaction) { SfxCalls++; }
            public void GrantCaptureReward(int cityId, int factionId, int rewardItem, int rewardCount)
            {
                RewardCalls++;
                LastRewardItem = rewardItem;
                LastRewardCount = rewardCount;
                RewardByCity[cityId] = rewardItem;
                RewardCountByCity[cityId] = rewardCount;
            }
            public void LogCityWarEvent(int cityId, int oldOwner, int newOwner, string message)
            {
                LogCalls++;
                LastMessage = message;
            }
            public void UpdateLeaderboard(int cityId, int ownerFaction, int defenderCount, long captureTimestamp)
            {
                BoardCalls++;
                LastCaptureTimestamp = captureTimestamp;
            }
            public void OnCityWarReset(int totalCities, int neutralCount)
            {
                ResetCalls++;
                LastTotalCities = totalCities;
                LastNeutralCount = neutralCount;
            }
        }

        private static PcCityWarRegistry BuildRegistry(params (string key, string name, int[] mapIds)[] areas)
        {
            var reg = new PcCityWarRegistry();
            foreach (var a in areas)
            {
                reg.Register(new PcCityWarArea
                {
                    key = a.key,
                    name = a.name,
                    mapIds = new System.Collections.Generic.List<int>(a.mapIds),
                });
            }
            return reg;
        }

        // ── Constructor + count ────────────────────────────────────────────

        [Test]
        public void Count_EmptyService_Zero()
        {
            var svc = new CityWarService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Count_AfterRegistry_ReturnsAreaCount()
        {
            var reg = BuildRegistry(
                ("AreaName01", "Biện Kinh", new[] { 100, 101 }),
                ("AreaName02", "Tương Dương", new[] { 200 })
            );
            var svc = new CityWarService(reg);
            Assert.AreEqual(2, svc.Count);
        }

        [Test]
        public void Count_InvalidKey_NotIndexed()
        {
            var reg = BuildRegistry(("Foo", "Foo", new[] { 1 })); // doesn't start with AreaName
            var svc = new CityWarService(reg);
            Assert.AreEqual(0, svc.Count);
        }

        // ── GetCity / GetCityState ──────────────────────────────────────────

        [Test]
        public void GetCity_NotFound_ReturnsNull()
        {
            var svc = new CityWarService();
            Assert.IsNull(svc.GetCity(99));
        }

        [Test]
        public void GetCity_Exists_ReturnsArea()
        {
            var reg = BuildRegistry(("AreaName01", "Biện Kinh", new[] { 100 }));
            var svc = new CityWarService(reg);
            var c = svc.GetCity(1);
            Assert.IsNotNull(c);
            Assert.AreEqual("Biện Kinh", c.name);
        }

        [Test]
        public void GetCityState_Exists_ReturnsNeutral()
        {
            var reg = BuildRegistry(("AreaName01", "Biện Kinh", new[] { 100 }));
            var svc = new CityWarService(reg);
            var s = svc.GetCityState(1);
            Assert.IsNotNull(s);
            Assert.AreEqual(CityWarService.NeutralFaction, s.ownerFaction);
        }

        [Test]
        public void GetAllCityStates_AfterRegistry()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }), ("AreaName02", "Y", new[] { 2 }));
            var svc = new CityWarService(reg);
            Assert.AreEqual(2, Count(svc.GetAllCityStates()));
        }

        // ── IsOwnedBy / GetCitiesOwnedBy ────────────────────────────────────

        [Test]
        public void IsOwnedBy_NeutralAfterAttach()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            Assert.IsTrue(svc.IsOwnedBy(1, CityWarService.NeutralFaction));
            Assert.IsFalse(svc.IsOwnedBy(1, 1));
        }

        [Test]
        public void IsOwnedBy_NotFound_ReturnsFalse()
        {
            var svc = new CityWarService();
            Assert.IsFalse(svc.IsOwnedBy(99, 1));
        }

        [Test]
        public void GetCitiesOwnedBy_NeutralAfterAttach()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }), ("AreaName02", "Y", new[] { 2 }));
            var svc = new CityWarService(reg);
            int n = 0;
            foreach (var s in svc.GetCitiesOwnedBy(CityWarService.NeutralFaction)) n++;
            Assert.AreEqual(2, n);
        }

        // ── CaptureCity ─────────────────────────────────────────────────────

        [Test]
        public void CaptureCity_NotFound_ReturnsFalse()
        {
            var svc = new CityWarService();
            Assert.IsFalse(svc.CaptureCity(99, 1));
        }

        [Test]
        public void CaptureCity_Success_ReturnsTrue()
        {
            var reg = BuildRegistry(("AreaName01", "Biện Kinh", new[] { 100 }));
            var svc = new CityWarService(reg);
            Assert.IsTrue(svc.CaptureCity(1, 1));
            Assert.IsTrue(svc.IsOwnedBy(1, 1));
        }

        [Test]
        public void CaptureCity_SameOwner_ReturnsFalse()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            svc.CaptureCity(1, 1);
            Assert.IsFalse(svc.CaptureCity(1, 1));
        }

        [Test]
        public void CaptureCity_FiresOnCityCapturedEvent()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            int fired = 0;
            int lastCity = 0;
            svc.OnCityCaptured += (c, o, n) => { fired++; lastCity = c; };
            svc.CaptureCity(1, 5);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastCity);
        }

        [Test]
        public void CaptureCity_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "Biện Kinh", new[] { 100 }));
            var svc = new CityWarService(reg, host);
            svc.CaptureCity(1, 1);
            Assert.AreEqual(1, host.OwnerChangedCalls);
            Assert.AreEqual(1, host.MarkerCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.BoardCalls);
            Assert.AreEqual(0, host.RewardCalls); // no reward set
        }

        [Test]
        public void CaptureCity_HostArgsCorrect()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "Biện Kinh", new[] { 100 }));
            var svc = new CityWarService(reg, host);
            svc.CaptureCity(1, 1);
            Assert.AreEqual(1, host.LastCityId);
            Assert.AreEqual(0, host.LastOldOwner);
            Assert.AreEqual(1, host.LastNewOwner);
            Assert.AreEqual("Biện Kinh", host.LastCityName);
        }

        [Test]
        public void CaptureCity_GrantsRewardIfSet()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg, host);
            svc.SetCaptureReward(1, 500, 10);
            svc.CaptureCity(1, 1);
            Assert.AreEqual(1, host.RewardCalls);
            Assert.AreEqual(500, host.LastRewardItem);
            Assert.AreEqual(10, host.LastRewardCount);
        }

        [Test]
        public void CaptureCity_NoRewardSet_NoHostRewardCall()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg, host);
            svc.CaptureCity(1, 1);
            Assert.AreEqual(0, host.RewardCalls);
        }

        [Test]
        public void CaptureCity_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            Assert.DoesNotThrow(() => svc.CaptureCity(1, 1));
        }

        // ── AddDefender ─────────────────────────────────────────────────────

        [Test]
        public void AddDefender_NotFound_ReturnsFalse()
        {
            var svc = new CityWarService();
            Assert.IsFalse(svc.AddDefender(99));
        }

        [Test]
        public void AddDefender_IncrementsCount()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            svc.AddDefender(1, 5);
            Assert.AreEqual(5, svc.GetCityState(1).defenderCount);
        }

        [Test]
        public void AddDefender_FiresOnDefenderChangedEvent()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            int fired = 0;
            svc.OnDefenderChanged += (c, n) => fired++;
            svc.AddDefender(1, 5);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void AddDefender_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg, host);
            svc.AddDefender(1, 5);
            Assert.AreEqual(1, host.DefenderNpcCalls);
            Assert.AreEqual(1, host.BoardCalls);
            Assert.AreEqual(5, host.LastDefenderCount);
        }

        [Test]
        public void AddDefender_NegativeCount_BoundsAtZero()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            svc.AddDefender(1, -10);
            Assert.AreEqual(0, svc.GetCityState(1).defenderCount);
        }

        [Test]
        public void AddDefender_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg);
            Assert.DoesNotThrow(() => svc.AddDefender(1));
        }

        // ── ResetAll ────────────────────────────────────────────────────────

        [Test]
        public void ResetAll_ResetsToNeutral()
        {
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }), ("AreaName02", "Y", new[] { 2 }));
            var svc = new CityWarService(reg);
            svc.CaptureCity(1, 1);
            svc.CaptureCity(2, 5);
            svc.ResetAll();
            Assert.IsTrue(svc.IsOwnedBy(1, CityWarService.NeutralFaction));
            Assert.IsTrue(svc.IsOwnedBy(2, CityWarService.NeutralFaction));
        }

        [Test]
        public void ResetAll_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }), ("AreaName02", "Y", new[] { 2 }));
            var svc = new CityWarService(reg, host);
            svc.ResetAll();
            Assert.AreEqual(1, host.ResetCalls);
            Assert.AreEqual(2, host.LastTotalCities);
            Assert.AreEqual(2, host.LastNeutralCount);
        }

        [Test]
        public void ResetAll_EmptyService_DispatchesZeroes()
        {
            var host = new FakeHost();
            var svc = new CityWarService(null, host);
            svc.ResetAll();
            Assert.AreEqual(1, host.ResetCalls);
            Assert.AreEqual(0, host.LastTotalCities);
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_Replaces()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }));
            var svc = new CityWarService(reg, host1);
            svc.AttachHost(host2);
            svc.CaptureCity(1, 1);
            Assert.AreEqual(0, host1.OwnerChangedCalls);
            Assert.AreEqual(1, host2.OwnerChangedCalls);
        }

        // ── SetCaptureReward ────────────────────────────────────────────────

        [Test]
        public void SetCaptureReward_MultipleCities()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(("AreaName01", "X", new[] { 1 }), ("AreaName02", "Y", new[] { 2 }));
            var svc = new CityWarService(reg, host);
            svc.SetCaptureReward(1, 100, 5);
            svc.SetCaptureReward(2, 200, 10);
            svc.CaptureCity(1, 1);
            svc.CaptureCity(2, 2);
            Assert.AreEqual(100, host.RewardByCity[1]);
            Assert.AreEqual(5, host.RewardCountByCity[1]);
            Assert.AreEqual(200, host.RewardByCity[2]);
            Assert.AreEqual(10, host.RewardCountByCity[2]);
            Assert.AreEqual(2, host.RewardCalls);
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
