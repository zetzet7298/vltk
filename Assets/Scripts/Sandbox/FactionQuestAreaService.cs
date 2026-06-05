// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.3/9 Faction Quest Area Service
// Quản lý khu vực nhiệm vụ theo môn phái.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý khu vực nhiệm vụ môn phái (Thiên Vương, Võ Đang, ...).</summary>
    public class FactionQuestAreaService
    {
        public const string LogTag = "FactionQuestArea";
        public const string DefaultStreamingDir = "Reference/PcTong";

        private PcFactionQuestAreaRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public FactionQuestAreaService() { }
        public FactionQuestAreaService(PcFactionQuestAreaRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcFactionQuestAreaRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Faction quest area registry rỗng");
        }

        public static FactionQuestAreaService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new FactionQuestAreaService();
            if (Directory.Exists(dir))
            {
                var reg = PcFactionQuestAreaParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Faction quest area directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcFactionQuestAreaEntry GetQuestArea(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcFactionQuestAreaEntry> GetByFaction(int factionId)
            => _reg != null ? _reg.GetByFaction(factionId) : System.Array.Empty<PcFactionQuestAreaEntry>();
        public IReadOnlyList<PcFactionQuestAreaEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcFactionQuestAreaEntry>();
        public IReadOnlyList<PcFactionQuestAreaEntry> GetFactionQuestAreas()
            => _reg != null ? _reg.All : System.Array.Empty<PcFactionQuestAreaEntry>();
        public int GetTotalQuestsForFaction(int factionId)
            => _reg != null ? _reg.GetTotalQuestsForFaction(factionId) : 0;
    }
}
