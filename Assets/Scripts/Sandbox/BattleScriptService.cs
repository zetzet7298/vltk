// -----------------------------------------------------------------------------
// VLTK Mobile — Battle Script Service (Kịch Bản Chiến Đấu runtime)
// Wraps PcBattleScriptRegistry. Lọc theo bản đồ, theo trigger type.
// Vietnamese: "Kịch Bản", "Chiến Đấu", "Bắt Đầu", "Kết Thúc", "Giết Boss", "Chết".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kịch bản chiến đấu (183 scripts). PC source:
    /// settings/battlescripts.txt — kịch bản cho Tống Kim, Công Thành Chiến,
    /// Võ Lâm Liên Đấu, Phong Hỏa Liên Thành, Bách Bảo Lâu, ...
    /// </summary>
    public class BattleScriptService
    {
        public const string LogTag = "BattleScript";

        private PcBattleScriptRegistry _registry;
        private IBattleScriptServiceHost _host;

        public int Count => _registry != null ? _registry.Count : 0;

        public BattleScriptService() { }
        public BattleScriptService(PcBattleScriptRegistry registry)
        {
            _registry = registry ?? new PcBattleScriptRegistry();
        }

        public void AttachHost(IBattleScriptServiceHost host) { _host = host; }

        public void AttachRegistry(PcBattleScriptRegistry registry)
        {
            _registry = registry ?? new PcBattleScriptRegistry();
            if (_host != null)
            {
                _host.OnScriptRegistryAttached(_registry.Count);
                _host.LogScriptEvent("load", 0, $"Loaded {_registry.Count} battle scripts");
                _host.PlayScriptSFX("load", 0);
                _host.SaveScriptState(0, 0, 0);
            }
        }

        public PcBattleScriptEntry GetScript(int scriptId)
        {
            var s = _registry != null ? _registry.Get(scriptId) : null;
            if (_host != null)
            {
                if (s != null)
                    _host.OnScriptResolved(s.scriptId, s.scriptName, s.mapId, s.triggerType);
                else
                    _host.LogScriptEvent("query_missing", scriptId, "Script not found in registry");
            }
            return s;
        }

        public IEnumerable<PcBattleScriptEntry> GetAllScripts()
            => _registry != null ? _registry.All : (IEnumerable<PcBattleScriptEntry>)System.Array.Empty<PcBattleScriptEntry>();

        public IEnumerable<PcBattleScriptEntry> GetScriptsForMap(int mapId)
        {
            if (_registry == null) yield break;
            int count = 0;
            foreach (var e in _registry.GetByMap(mapId))
            {
                count++;
                if (_host != null) _host.LogScriptEvent("map_query_hit", e.scriptId, $"{e.scriptName} for map {mapId}");
                yield return e;
            }
            if (_host != null) _host.OnScriptsForMapQueried(mapId, count);
        }

        public IEnumerable<PcBattleScriptEntry> GetScriptsByTrigger(int triggerType)
        {
            if (_registry == null) yield break;
            int count = 0;
            foreach (var e in _registry.GetByTriggerType(triggerType))
            {
                count++;
                if (_host != null) _host.LogScriptEvent("trigger_query_hit", e.scriptId, $"{e.scriptName} for trigger {triggerType}");
                yield return e;
            }
            if (_host != null) _host.OnScriptsByTriggerQueried(triggerType, count);
        }

        // ── Trigger dispatch (called by gameplay code) ────────────
        public void TriggerStart(int scriptId, int npcId)
        {
            var s = GetScript(scriptId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnScriptStartTriggered(s.scriptId, s.mapId, npcId);
                _host.ShowScriptUI(s.scriptId, s.scriptName, 0);
                _host.LogScriptEvent("trigger_start", s.scriptId, $"Script {s.scriptName} started");
                _host.PlayScriptSFX("start", s.scriptId);
                _host.SaveScriptState(s.scriptId, 0, s.mapId);
            }
        }

        public void TriggerEnd(int scriptId)
        {
            var s = GetScript(scriptId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnScriptEndTriggered(s.scriptId, s.mapId, s.rewardId, s.rewardCount, s.scoreReward);
                _host.ShowScriptUI(s.scriptId, s.scriptName, 1);
                _host.LogScriptEvent("trigger_end", s.scriptId, $"Script {s.scriptName} ended");
                _host.PlayScriptSFX("end", s.scriptId);
                _host.SaveScriptState(s.scriptId, 100, s.mapId);
            }
        }

        public void TriggerKillBoss(int scriptId, int npcId)
        {
            var s = GetScript(scriptId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnScriptKillBossTriggered(s.scriptId, s.mapId, npcId);
                _host.ShowScriptUI(s.scriptId, s.scriptName, 2);
                _host.LogScriptEvent("trigger_kill_boss", s.scriptId, $"Boss {npcId} killed in {s.scriptName}");
                _host.PlayScriptSFX("kill", s.scriptId);
                _host.SaveScriptState(s.scriptId, 80, s.mapId);
            }
        }

        public void TriggerDeath(int scriptId, int npcId)
        {
            var s = GetScript(scriptId);
            if (s == null) return;
            if (_host != null)
            {
                _host.OnScriptDeathTriggered(s.scriptId, s.mapId, npcId);
                _host.ShowScriptUI(s.scriptId, s.scriptName, 3);
                _host.LogScriptEvent("trigger_death", s.scriptId, $"NPC {npcId} died in {s.scriptName}");
                _host.PlayScriptSFX("death", s.scriptId);
                _host.SaveScriptState(s.scriptId, 100, s.mapId);
            }
        }

        public static BattleScriptService LoadFromStreamingAssets()
        {
            var svc = new BattleScriptService();
            string[] candidates = { "Reference/PcBattleScript", "Reference/PcEvent/Battle" };
            foreach (var sub in candidates)
            {
                string dir = Path.Combine(Application.streamingAssetsPath, sub);
                if (Directory.Exists(dir))
                {
                    var reg = PcBattleScriptParser.BuildRegistry(dir);
                    svc.AttachRegistry(reg);
                    SubsystemLog.Info(LogTag, $"BattleScriptService loaded {reg.Count} kịch bản từ {dir}");
                    return svc;
                }
            }
            SubsystemLog.Warn(LogTag, "BattleScriptService: không tìm thấy thư mục, khởi tạo registry rỗng");
            return svc;
        }
    }
}
