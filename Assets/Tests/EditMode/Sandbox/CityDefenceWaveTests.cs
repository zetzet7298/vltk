// -----------------------------------------------------------------------------
// VLTK Mobile — CityDefenceService EditMode tests.
// Kiểm tra thủ thành wave lifecycle: registry attach, trigger wave (spawn
// NPC + SFX + UI + log), complete wave (reward dispatch), wave active state.
// PC source: settings/maps/newcitydefence/*.txt + lua wave_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class CityDefenceWaveTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : ICityDefenceHost
        {
            public int SpawnCalls;
            public int StartedCalls;
            public int EffectCalls;
            public int BuffCalls;
            public int NoticeCalls;
            public int LogCalls;
            public int RewardCalls;
            public int LastMapId;
            public int LastWaveIndex;
            public int LastNpcCount;
            public int LastWaveInterval;
            public int LastMinLevel;
            public int LastSpawnedNpc;
            public int LastRewardId;
            public int LastRewardCount;
            public int NextSpawnedNpc = 9001;

            public int SpawnDefenderNpc(int mapId, int waveIndex, int npcId, int count)
            {
                SpawnCalls++;
                LastNpcCount = count;
                return NextSpawnedNpc;
            }
            public void OnWaveStarted(int mapId, int waveIndex, int npcCount, int waveIntervalSec)
            {
                StartedCalls++;
                LastWaveInterval = waveIntervalSec;
            }
            public void PlayWaveStartEffect(int mapId, int waveIndex) { EffectCalls++; }
            public void SetDefenderBuff(int npcId, int mapId, int waveIndex) { BuffCalls++; LastSpawnedNpc = npcId; }
            public void ShowDefenceNotice(int mapId, int waveIndex, int minLevel)
            {
                NoticeCalls++;
                LastMinLevel = minLevel;
            }
            public void LogDefenceEvent(int mapId, int waveIndex, string message) { LogCalls++; }
            public void GrantWaveReward(int playerId, int mapId, int waveIndex, int rewardId, int rewardCount)
            {
                RewardCalls++;
                LastRewardId = rewardId;
                LastRewardCount = rewardCount;
            }
        }

        private static PcCityDefenceRegistry BuildRegistry(params (int mapId, int waveIndex, int npcId, int count, int interval, int rewardId, int rewardCount, int minLevel)[] rows)
        {
            var reg = new PcCityDefenceRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcCityDefenceEntry
                {
                    mapId = r.mapId,
                    waveIndex = r.waveIndex,
                    defenderNpcId = r.npcId,
                    npcCount = r.count,
                    waveIntervalSec = r.interval,
                    rewardId = r.rewardId,
                    rewardCount = r.rewardCount,
                    minLevel = r.minLevel,
                });
            }
            return reg;
        }

        // ── Registry attach + count ────────────────────────────────────────

        [Test]
        public void Count_AfterRegistry_ReturnsEntryCount()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 200, 1, 50));
            var svc = new CityDefenceService(reg);
            Assert.AreEqual(1, svc.Count);
        }

        [Test]
        public void Count_EmptyService_ReturnsZero()
        {
            var svc = new CityDefenceService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_NullRegistry_EmptyState()
        {
            var svc = new CityDefenceService();
            svc.AttachRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_FiresOnDefenceLoadedEvent()
        {
            var svc = new CityDefenceService();
            int fired = 0;
            svc.OnDefenceLoaded += () => fired++;
            svc.AttachRegistry(BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0)));
            Assert.AreEqual(1, fired);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetDefence_NotFound_ReturnsEmpty()
        {
            var svc = new CityDefenceService();
            Assert.AreEqual(0, svc.GetDefence(99).Count);
        }

        [Test]
        public void GetDefence_Exists_ReturnsList()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            Assert.AreEqual(1, svc.GetDefence(1).Count);
        }

        [Test]
        public void GetAllDefences_Empty()
        {
            var svc = new CityDefenceService();
            Assert.AreEqual(0, Count(svc.GetAllDefences()));
        }

        [Test]
        public void GetAllDefences_AfterRegistry()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0), (1, 2, 200, 3, 60, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            Assert.AreEqual(2, Count(svc.GetAllDefences()));
        }

        // ── TriggerWave ─────────────────────────────────────────────────────

        [Test]
        public void TriggerWave_FiresOnWaveTriggeredEvent()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            int fired = 0;
            int lastWave = 0;
            svc.OnWaveTriggered += (m, w) => { fired++; lastWave = w; };
            svc.TriggerWave(1, 1);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastWave);
        }

        [Test]
        public void TriggerWave_SetsActiveState()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            svc.TriggerWave(1, 1);
            Assert.IsTrue(svc.IsWaveActive(1, 1));
            Assert.AreEqual(1, svc.ActiveWaveCount);
        }

        [Test]
        public void TriggerWave_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 200, 1, 50));
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 1);
            Assert.AreEqual(1, host.SpawnCalls);
            Assert.AreEqual(1, host.StartedCalls);
            Assert.AreEqual(1, host.EffectCalls);
            Assert.AreEqual(1, host.BuffCalls);
            Assert.AreEqual(1, host.NoticeCalls);
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void TriggerWave_PassesCorrectArgs()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 50));
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 1);
            Assert.AreEqual(5, host.LastNpcCount);
            Assert.AreEqual(30, host.LastWaveInterval);
            Assert.AreEqual(50, host.LastMinLevel);
        }

        [Test]
        public void TriggerWave_NoMatchingEntry_NoSpawn()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 99); // wave 99 doesn't exist
            Assert.AreEqual(0, host.SpawnCalls);
            Assert.AreEqual(1, host.StartedCalls); // still dispatches wave started
        }

        [Test]
        public void TriggerWave_ZeroNpcId_NoSpawn()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 0, 5, 30, 0, 0, 0)); // npcId=0
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 1);
            Assert.AreEqual(0, host.SpawnCalls);
        }

        [Test]
        public void TriggerWave_WithoutHost_NoCrash()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            Assert.DoesNotThrow(() => svc.TriggerWave(1, 1));
            Assert.IsTrue(svc.IsWaveActive(1, 1));
        }

        [Test]
        public void TriggerWave_MultipleWaves()
        {
            var reg = BuildRegistry(
                (1, 1, 100, 5, 30, 0, 0, 0),
                (1, 2, 200, 3, 60, 0, 0, 0)
            );
            var svc = new CityDefenceService(reg);
            svc.TriggerWave(1, 1);
            svc.TriggerWave(1, 2);
            Assert.IsTrue(svc.IsWaveActive(1, 1));
            Assert.IsTrue(svc.IsWaveActive(1, 2));
            Assert.AreEqual(2, svc.ActiveWaveCount);
        }

        // ── CompleteWave ────────────────────────────────────────────────────

        [Test]
        public void CompleteWave_RemovesActive()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            svc.TriggerWave(1, 1);
            svc.CompleteWave(1, 1, 100);
            Assert.IsFalse(svc.IsWaveActive(1, 1));
        }

        [Test]
        public void CompleteWave_FiresOnWaveCompletedEvent()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            int fired = 0;
            svc.OnWaveCompleted += (m, w) => fired++;
            svc.TriggerWave(1, 1);
            svc.CompleteWave(1, 1, 100);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void CompleteWave_GrantsReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 200, 3, 0));
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 1);
            svc.CompleteWave(1, 1, 100);
            Assert.AreEqual(1, host.RewardCalls);
            Assert.AreEqual(200, host.LastRewardId);
            Assert.AreEqual(3, host.LastRewardCount);
        }

        [Test]
        public void CompleteWave_NoReward_StillCompletes()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0)); // reward=0
            var svc = new CityDefenceService(reg, host);
            svc.TriggerWave(1, 1);
            svc.CompleteWave(1, 1, 100);
            Assert.AreEqual(0, host.RewardCalls);
        }

        [Test]
        public void CompleteWave_NotTriggered_NoEffect()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 200, 3, 0));
            var svc = new CityDefenceService(reg, host);
            svc.CompleteWave(1, 1, 100); // never triggered
            Assert.IsFalse(svc.IsWaveActive(1, 1)); // still inactive
            Assert.AreEqual(0, host.RewardCalls); // entry not found, no reward
        }

        [Test]
        public void CompleteWave_WithoutHost_NoCrash()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            svc.TriggerWave(1, 1);
            Assert.DoesNotThrow(() => svc.CompleteWave(1, 1, 100));
        }

        // ── IsWaveActive ────────────────────────────────────────────────────

        [Test]
        public void IsWaveActive_NoTrigger_ReturnsFalse()
        {
            var svc = new CityDefenceService();
            Assert.IsFalse(svc.IsWaveActive(1, 1));
        }

        [Test]
        public void IsWaveActive_OtherWave_ReturnsFalse()
        {
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg);
            svc.TriggerWave(1, 1);
            Assert.IsFalse(svc.IsWaveActive(1, 99));
        }

        [Test]
        public void ActiveWaveCount_EmptyService_Zero()
        {
            var svc = new CityDefenceService();
            Assert.AreEqual(0, svc.ActiveWaveCount);
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry((1, 1, 100, 5, 30, 0, 0, 0));
            var svc = new CityDefenceService(reg, host1);
            svc.AttachHost(host2);
            svc.TriggerWave(1, 1);
            Assert.AreEqual(0, host1.StartedCalls);
            Assert.AreEqual(1, host2.StartedCalls);
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
