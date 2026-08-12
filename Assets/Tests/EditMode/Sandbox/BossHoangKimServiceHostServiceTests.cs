// -----------------------------------------------------------------------------
// VLTK Mobile — BossHoangKimService host dispatch tests
// PC source: settings/boss/bosshoangkim.txt
// Verifies IBossHoangKimHost receives expected events for spawn / kill /
// respawn / query operations. No Unity / MonoBehaviour dependencies.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BossHoangKimServiceHostServiceTests
    {
        private sealed class FakeHost : IBossHoangKimHost
        {
            public int RegistryAttachedCalls;
            public int ResolvedCalls;
            public int LastResolvedBossId;
            public int LastResolvedMapId;
            public int LastResolvedRespawnSec;
            public int LastResolvedLevel;

            public int SpawnedCalls;
            public int LastSpawnedBossId;
            public int LastSpawnedMapId;
            public int LastSpawnedX;
            public int LastSpawnedY;
            public int LastSpawnedLevel;

            public int KilledCalls;
            public int LastKilledBossId;
            public int LastKillerActorId;
            public int LastKillRespawnMin;

            public int RespawnTickedCalls;
            public int LastTickedBossId;
            public int LastTickedRemainingSec;

            public int RespawnedCalls;
            public int LastRespawnedBossId;
            public int LastRespawnedMapId;

            public int ActiveQueriedCalls;
            public int LastActiveCount;

            public int UIShowCalls;
            public int LastUIBossId;
            public string LastUINameVi;
            public int LastUIMapId;
            public int LastUIHpPercent;

            public int LogCalls;
            public int LastLogBossId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXBossId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveBossId;

            public void OnBossRegistryAttached(int bossCount) => RegistryAttachedCalls++;
            public void OnBossResolved(int bossId, int mapId, int respawnSec, int level)
            {
                ResolvedCalls++;
                LastResolvedBossId = bossId;
                LastResolvedMapId = mapId;
                LastResolvedRespawnSec = respawnSec;
                LastResolvedLevel = level;
            }
            public void OnBossSpawned(int bossId, int mapId, int spawnX, int spawnY, int level)
            {
                SpawnedCalls++;
                LastSpawnedBossId = bossId;
                LastSpawnedMapId = mapId;
                LastSpawnedX = spawnX;
                LastSpawnedY = spawnY;
                LastSpawnedLevel = level;
            }
            public void OnBossKilled(int bossId, int killerActorId, int respawnMinutes)
            {
                KilledCalls++;
                LastKilledBossId = bossId;
                LastKillerActorId = killerActorId;
                LastKillRespawnMin = respawnMinutes;
            }
            public void OnBossRespawnTicked(int bossId, int remainingSeconds)
            {
                RespawnTickedCalls++;
                LastTickedBossId = bossId;
                LastTickedRemainingSec = remainingSeconds;
            }
            public void OnBossRespawned(int bossId, int mapId)
            {
                RespawnedCalls++;
                LastRespawnedBossId = bossId;
                LastRespawnedMapId = mapId;
            }
            public void OnActiveBossesQueried(int aliveCount, DateTime now)
            {
                ActiveQueriedCalls++;
                LastActiveCount = aliveCount;
            }
            public void ShowBossUI(int bossId, string nameVi, int mapId, int hpPercent)
            {
                UIShowCalls++;
                LastUIBossId = bossId;
                LastUINameVi = nameVi;
                LastUIMapId = mapId;
                LastUIHpPercent = hpPercent;
            }
            public void LogBossEvent(string eventType, int bossId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogBossId = bossId;
                LastLogDetail = detailVi;
            }
            public void PlayBossSFX(string action, int bossId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXBossId = bossId;
            }
            public void SaveBossState(int bossId, DateTime killedAtUtc, int respawnSec)
            {
                SaveCalls++;
                LastSaveBossId = bossId;
            }
        }

        private static (PcBossHoangKimRegistry reg, PcBossHoangKimEntry e1, PcBossHoangKimEntry e2) MakeRegistry()
        {
            var reg = new PcBossHoangKimRegistry();
            var e1 = new PcBossHoangKimEntry
            {
                bossId = 600, nameRaw = "Bach Van Phi", mapId = 200, posX = 500, posY = 1000,
                level = 50, respawnSec = 3600, npcTemplateId = 1001, dropItemId = 5001, dropCount = 1,
            };
            var e2 = new PcBossHoangKimEntry
            {
                bossId = 601, nameRaw = "Xich Diem Ma Vuong", mapId = 203, posX = 300, posY = 800,
                level = 70, respawnSec = 7200, npcTemplateId = 1002, dropItemId = 5002, dropCount = 2,
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_DefaultBosses_Registered()
        {
            var svc = new BossHoangKimService();
            Assert.GreaterOrEqual(svc.RegisteredBosses.Count, 3);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new BossHoangKimService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── AttachRegistry dispatch ────────────────────────────────────────
        [Test]
        public void AttachRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            // ctor dispatches nothing; the test calls AttachRegistry once
            int baselineRegistryAttached = host.RegistryAttachedCalls;
            var (reg, _, _) = MakeRegistry();
            svc.AttachRegistry(reg);
            Assert.AreEqual(baselineRegistryAttached + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual("load", host.LastSFXAction);
            Assert.AreEqual("load", host.LastLogEventType);
        }

        // ── GetBoss dispatch ───────────────────────────────────────────────
        [Test]
        public void GetBoss_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new BossHoangKimService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var boss = svc.GetBoss(600);
            Assert.IsNotNull(boss);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(600, host.LastResolvedBossId);
            Assert.AreEqual(200, host.LastResolvedMapId);
            Assert.AreEqual(50, host.LastResolvedLevel);
        }

        [Test]
        public void GetBoss_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BossHoangKimService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var boss = svc.GetBoss(9999);
            Assert.IsNull(boss);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── OnBossDeath dispatch ───────────────────────────────────────────
        [Test]
        public void OnBossDeath_DispatchesHost_KillUI()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            int baselineKilled = host.KilledCalls;
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            svc.OnBossDeath(600, 42);
            Assert.AreEqual(baselineKilled + 1, host.KilledCalls);
            Assert.AreEqual(600, host.LastKilledBossId);
            Assert.AreEqual(42, host.LastKillerActorId);
            Assert.AreEqual(60, host.LastKillRespawnMin);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(0, host.LastUIHpPercent);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("kill", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("kill", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
        }

        [Test]
        public void OnBossDeath_Unknown_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            int baseline = host.KilledCalls;
            svc.OnBossDeath(9999, 1);
            Assert.AreEqual(baseline, host.KilledCalls);
        }

        // ── Tick respawn dispatch ──────────────────────────────────────────
        [Test]
        public void Tick_TriggersRespawnDispatch_AfterTimer()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            // Boss 600 has respawnMinutes=60 → 3600 sec
            svc.OnBossDeath(600, 42);
            int baselineSpawned = host.SpawnedCalls;
            int baselineRespawned = host.RespawnedCalls;
            int baselineTicked = host.RespawnTickedCalls;
            // Tick less than respawn time → Ticked fires, no respawn yet
            svc.Tick(100f);
            Assert.Greater(host.RespawnTickedCalls, baselineTicked);
            Assert.AreEqual(baselineSpawned, host.SpawnedCalls);
            Assert.AreEqual(baselineRespawned, host.RespawnedCalls);
        }

        [Test]
        public void Tick_CompletesRespawn_DispatchesAll()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            svc.OnBossDeath(600, 42);
            int baselineSpawned = host.SpawnedCalls;
            int baselineRespawned = host.RespawnedCalls;
            // Tick > 3600 → respawn
            svc.Tick(3700f);
            Assert.AreEqual(baselineSpawned + 1, host.SpawnedCalls);
            Assert.AreEqual(baselineRespawned + 1, host.RespawnedCalls);
            Assert.AreEqual(600, host.LastSpawnedBossId);
            Assert.AreEqual(200, host.LastSpawnedMapId);
            Assert.AreEqual(100, host.LastUIHpPercent);
        }

        // ── GetActiveBosses dispatch ───────────────────────────────────────
        [Test]
        public void GetActiveBosses_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BossHoangKimService(reg);
            svc.AttachHost(host);
            int baseline = host.ActiveQueriedCalls;
            var list = svc.GetActiveBosses(DateTime.UtcNow);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(baseline + 1, host.ActiveQueriedCalls);
            Assert.AreEqual(2, host.LastActiveCount);
        }

        [Test]
        public void GetActiveBosses_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new BossHoangKimService();
            svc.AttachHost(host);
            int baseline = host.ActiveQueriedCalls;
            var list = svc.GetActiveBosses(DateTime.UtcNow);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(baseline + 1, host.ActiveQueriedCalls);
            Assert.AreEqual(0, host.LastActiveCount);
        }

        [Test]
        public void GetActiveBosses_FiltersByLastDeath()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BossHoangKimService(reg);
            svc.AttachHost(host);
            var now = DateTime.UtcNow;
            var deaths = new Dictionary<int, DateTime>
            {
                [600] = now.AddSeconds(-1000), // still in respawn (3600 sec)
            };
            var list = svc.GetActiveBosses(now, deaths);
            Assert.AreEqual(1, list.Count); // 601 alive, 600 in cooldown
        }

        // ── IsBossAlive + ComputeRespawnTime ───────────────────────────────
        [Test]
        public void IsBossAlive_DefaultsTrue()
        {
            var svc = new BossHoangKimService();
            Assert.IsTrue(svc.IsBossAlive(600));
        }

        [Test]
        public void IsBossAlive_AfterDeathFalse()
        {
            var svc = new BossHoangKimService();
            svc.OnBossDeath(600, 1);
            Assert.IsFalse(svc.IsBossAlive(600));
        }

        [Test]
        public void ComputeRespawnTime_KnownBoss_ReturnsFuture()
        {
            var (reg, e1, _) = MakeRegistry();
            var svc = new BossHoangKimService(reg);
            var killedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var rt = svc.ComputeRespawnTime(600, killedAt);
            Assert.AreEqual(killedAt.AddSeconds(e1.respawnSec), rt);
        }

        [Test]
        public void ComputeRespawnTime_MissingBoss_ReturnsMinValue()
        {
            var svc = new BossHoangKimService();
            var rt = svc.ComputeRespawnTime(9999);
            Assert.AreEqual(DateTime.MinValue, rt);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new BossHoangKimService();
            Assert.DoesNotThrow(() => svc.AttachRegistry(null));
            Assert.DoesNotThrow(() => svc.GetBoss(600));
            Assert.DoesNotThrow(() => svc.OnBossDeath(600, 1));
            Assert.DoesNotThrow(() => svc.Tick(100f));
            Assert.DoesNotThrow(() => svc.GetActiveBosses(DateTime.UtcNow));
        }
    }
}
