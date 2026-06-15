// -----------------------------------------------------------------------------
// VLTK Mobile — BattleScriptService host dispatch tests
// PC source: settings/battlescripts.txt — Kịch Bản Chiến Đấu.
// Verifies IBattleScriptServiceHost receives expected events for load / query /
// trigger operations (start, end, kill_boss, death).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class BattleScriptServiceHostServiceTests
    {
        private sealed class FakeHost : IBattleScriptServiceHost
        {
            public int RegistryAttachedCalls;
            public int ResolvedCalls;
            public int LastResolvedScriptId;
            public string LastResolvedName;
            public int LastResolvedMapId;
            public int LastResolvedTrigger;

            public int ForMapQueriedCalls;
            public int LastForMapId;
            public int LastForMapResultCount;

            public int ByTriggerQueriedCalls;
            public int LastByTriggerType;
            public int LastByTriggerResultCount;

            public int StartTriggeredCalls;
            public int EndTriggeredCalls;
            public int KillBossTriggeredCalls;
            public int DeathTriggeredCalls;
            public int LastTriggerScriptId;
            public int LastTriggerMapId;
            public int LastTriggerNpcId;
            public int LastEndRewardId;
            public int LastEndRewardCount;
            public int LastEndScoreReward;

            public int UIShowCalls;
            public int LastUIScriptId;
            public string LastUIScriptName;
            public int LastUITriggerType;

            public int LogCalls;
            public int LastLogScriptId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXScriptId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveScriptId;
            public int LastSaveProgress;
            public int LastSaveMapId;

            public void OnScriptRegistryAttached(int scriptCount) => RegistryAttachedCalls++;
            public void OnScriptResolved(int scriptId, string scriptName, int mapId, int triggerType)
            {
                ResolvedCalls++;
                LastResolvedScriptId = scriptId;
                LastResolvedName = scriptName;
                LastResolvedMapId = mapId;
                LastResolvedTrigger = triggerType;
            }
            public void OnScriptsForMapQueried(int mapId, int resultCount)
            {
                ForMapQueriedCalls++;
                LastForMapId = mapId;
                LastForMapResultCount = resultCount;
            }
            public void OnScriptsByTriggerQueried(int triggerType, int resultCount)
            {
                ByTriggerQueriedCalls++;
                LastByTriggerType = triggerType;
                LastByTriggerResultCount = resultCount;
            }
            public void OnScriptStartTriggered(int scriptId, int mapId, int npcId)
            {
                StartTriggeredCalls++;
                LastTriggerScriptId = scriptId;
                LastTriggerMapId = mapId;
                LastTriggerNpcId = npcId;
            }
            public void OnScriptEndTriggered(int scriptId, int mapId, int rewardId, int rewardCount, int scoreReward)
            {
                EndTriggeredCalls++;
                LastTriggerScriptId = scriptId;
                LastTriggerMapId = mapId;
                LastEndRewardId = rewardId;
                LastEndRewardCount = rewardCount;
                LastEndScoreReward = scoreReward;
            }
            public void OnScriptKillBossTriggered(int scriptId, int mapId, int npcId)
            {
                KillBossTriggeredCalls++;
                LastTriggerScriptId = scriptId;
                LastTriggerMapId = mapId;
                LastTriggerNpcId = npcId;
            }
            public void OnScriptDeathTriggered(int scriptId, int mapId, int npcId)
            {
                DeathTriggeredCalls++;
                LastTriggerScriptId = scriptId;
                LastTriggerMapId = mapId;
                LastTriggerNpcId = npcId;
            }
            public void ShowScriptUI(int scriptId, string scriptName, int triggerType)
            {
                UIShowCalls++;
                LastUIScriptId = scriptId;
                LastUIScriptName = scriptName;
                LastUITriggerType = triggerType;
            }
            public void LogScriptEvent(string eventType, int scriptId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogScriptId = scriptId;
                LastLogDetail = detailVi;
            }
            public void PlayScriptSFX(string action, int scriptId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXScriptId = scriptId;
            }
            public void SaveScriptState(int scriptId, int progressPercent, int mapId)
            {
                SaveCalls++;
                LastSaveScriptId = scriptId;
                LastSaveProgress = progressPercent;
                LastSaveMapId = mapId;
            }
        }

        private static (PcBattleScriptRegistry reg, PcBattleScriptEntry e1, PcBattleScriptEntry e2) MakeRegistry()
        {
            var reg = new PcBattleScriptRegistry();
            var e1 = new PcBattleScriptEntry
            {
                scriptId = 100, scriptName = "Tong Kim Start",
                triggerType = 0, mapId = 200, npcId = 1001,
                rewardId = 5001, rewardCount = 1, scoreReward = 100,
            };
            var e2 = new PcBattleScriptEntry
            {
                scriptId = 101, scriptName = "Boss Kill Reward",
                triggerType = 2, mapId = 201, npcId = 2001,
                rewardId = 5002, rewardCount = 3, scoreReward = 500,
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new BattleScriptService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new BattleScriptService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── AttachRegistry dispatch ────────────────────────────────────────
        [Test]
        public void AttachRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new BattleScriptService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            svc.AttachRegistry(reg);
            Assert.AreEqual(1, host.RegistryAttachedCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        // ── GetScript dispatch ─────────────────────────────────────────────
        [Test]
        public void GetScript_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var s = svc.GetScript(100);
            Assert.IsNotNull(s);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(100, host.LastResolvedScriptId);
            Assert.AreEqual("Tong Kim Start", host.LastResolvedName);
            Assert.AreEqual(0, host.LastResolvedTrigger);
        }

        [Test]
        public void GetScript_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var s = svc.GetScript(9999);
            Assert.IsNull(s);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetScriptsForMap dispatch ──────────────────────────────────────
        [Test]
        public void GetScriptsForMap_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            var list = new List<PcBattleScriptEntry>(svc.GetScriptsForMap(200));
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ForMapQueriedCalls);
            Assert.AreEqual(200, host.LastForMapId);
            Assert.AreEqual(1, host.LastForMapResultCount);
        }

        [Test]
        public void GetScriptsForMap_Empty_NoLog_ButStillDispatches()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            var list = new List<PcBattleScriptEntry>(svc.GetScriptsForMap(9999));
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(1, host.ForMapQueriedCalls);
            Assert.AreEqual(0, host.LastForMapResultCount);
        }

        [Test]
        public void GetScriptsForMap_NoRegistry_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new BattleScriptService();
            svc.AttachHost(host);
            var list = new List<PcBattleScriptEntry>(svc.GetScriptsForMap(200));
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, host.ForMapQueriedCalls);
        }

        // ── GetScriptsByTrigger dispatch ────────────────────────────────────
        [Test]
        public void GetScriptsByTrigger_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            var list = new List<PcBattleScriptEntry>(svc.GetScriptsByTrigger(2));
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByTriggerQueriedCalls);
            Assert.AreEqual(2, host.LastByTriggerType);
            Assert.AreEqual(1, host.LastByTriggerResultCount);
        }

        [Test]
        public void GetScriptsByTrigger_NoRegistry_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new BattleScriptService();
            svc.AttachHost(host);
            var list = new List<PcBattleScriptEntry>(svc.GetScriptsByTrigger(0));
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, host.ByTriggerQueriedCalls);
        }

        // ── Trigger dispatch ──────────────────────────────────────────────
        [Test]
        public void TriggerStart_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            svc.TriggerStart(100, 1001);
            Assert.AreEqual(1, host.StartTriggeredCalls);
            Assert.AreEqual(100, host.LastTriggerScriptId);
            Assert.AreEqual(200, host.LastTriggerMapId);
            Assert.AreEqual(1001, host.LastTriggerNpcId);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(0, host.LastUITriggerType);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("trigger_start", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("start", host.LastSFXAction);
            Assert.AreEqual(0, host.LastSaveProgress);
        }

        [Test]
        public void TriggerEnd_DispatchesHost_WithRewards()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            svc.TriggerEnd(100);
            Assert.AreEqual(1, host.EndTriggeredCalls);
            Assert.AreEqual(100, host.LastTriggerScriptId);
            Assert.AreEqual(5001, host.LastEndRewardId);
            Assert.AreEqual(1, host.LastEndRewardCount);
            Assert.AreEqual(100, host.LastEndScoreReward);
            Assert.AreEqual(1, host.LastUITriggerType);
            Assert.AreEqual(100, host.LastSaveProgress);
        }

        [Test]
        public void TriggerKillBoss_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            svc.TriggerKillBoss(100, 9999);
            Assert.AreEqual(1, host.KillBossTriggeredCalls);
            Assert.AreEqual(9999, host.LastTriggerNpcId);
            Assert.AreEqual(2, host.LastUITriggerType);
            Assert.AreEqual(80, host.LastSaveProgress);
        }

        [Test]
        public void TriggerDeath_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            svc.TriggerDeath(100, 8888);
            Assert.AreEqual(1, host.DeathTriggeredCalls);
            Assert.AreEqual(8888, host.LastTriggerNpcId);
            Assert.AreEqual(3, host.LastUITriggerType);
            Assert.AreEqual(100, host.LastSaveProgress);
        }

        [Test]
        public void TriggerStart_UnknownScript_NoDispatch()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new BattleScriptService(reg);
            svc.AttachHost(host);
            int baseline = host.StartTriggeredCalls;
            svc.TriggerStart(9999, 1);
            Assert.AreEqual(baseline, host.StartTriggeredCalls);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new BattleScriptService();
            Assert.DoesNotThrow(() => svc.AttachRegistry(null));
            Assert.DoesNotThrow(() => svc.GetScript(100));
            Assert.DoesNotThrow(() => svc.GetScriptsForMap(200));
            Assert.DoesNotThrow(() => svc.GetScriptsByTrigger(0));
            Assert.DoesNotThrow(() => svc.TriggerStart(100, 1));
            Assert.DoesNotThrow(() => svc.TriggerEnd(100));
            Assert.DoesNotThrow(() => svc.TriggerKillBoss(100, 1));
            Assert.DoesNotThrow(() => svc.TriggerDeath(100, 1));
        }
    }
}
