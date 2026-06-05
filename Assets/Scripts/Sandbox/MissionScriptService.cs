// -----------------------------------------------------------------------------
// VLTK Mobile — ST-6.2 Mission Scripts runtime service
// Quản lý 985 metadata scripts cho nhiệm vụ.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class MissionScriptService
    {
        public const string LogTag = "MissionScript";
        public const string DefaultStreamingDir = "Reference/PcMission";

        private PcMissionScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public MissionScriptService() { }
        public MissionScriptService(PcMissionScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcMissionScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Mission script registry rỗng");
        }

        public static string GetScriptTypeName(int type)
        {
            return type switch
            {
                0 => "Giết NPC",
                2 => "Thu thập vật phẩm",
                3 => "Đến bản đồ",
                4 => "Nói chuyện NPC",
                5 => "Chờ thời gian",
                _ => $"Khác ({type})",
            };
        }

        public PcMissionScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcMissionScriptEntry> GetByMission(int missionId)
            => _registry != null ? _registry.GetByMission(missionId) : System.Array.Empty<PcMissionScriptEntry>();
        public IReadOnlyList<PcMissionScriptEntry> GetByType(int type)
            => _registry != null ? _registry.GetByType(type) : System.Array.Empty<PcMissionScriptEntry>();
        public IReadOnlyList<PcMissionScriptEntry> GetByTrigger(int trigger)
            => _registry != null ? _registry.GetByTrigger(trigger) : System.Array.Empty<PcMissionScriptEntry>();

        public bool CanExecute(int scriptId, int playerLevel)
        {
            var s = GetScript(scriptId);
            if (s == null) return false;
            if (s.count <= 0) return true;
            return playerLevel >= 1;
        }

        public int GetNextScript(int scriptId)
        {
            var s = GetScript(scriptId);
            return s?.nextScriptId ?? 0;
        }

        public float GetMissionProgress(int missionId, int completedScripts)
        {
            if (_registry == null) return 0f;
            int total = 0;
            foreach (var e in _registry.All) if (e.missionId == missionId) total++;
            if (total <= 0) return 0f;
            return System.Math.Min(1f, (float)completedScripts / total);
        }

        public static MissionScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new MissionScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcMissionScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
