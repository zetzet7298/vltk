// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.7 Town Script Service
// Quản lý script thị trấn: NPC, nhiệm vụ, shop, service, event.
// ScriptType: 0=npc, 1=quest, 2=shop, 3=service, 4=event.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý town script (NPC, nhiệm vụ, shop trong thị trấn).</summary>
    public class TownScriptService
    {
        public const string LogTag = "TownScript";
        public const string DefaultStreamingDir = "Reference/PcTown";

        private PcTownScriptRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public TownScriptService() { }
        public TownScriptService(PcTownScriptRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcTownScriptRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Town script registry rỗng");
        }

        public static TownScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TownScriptService();
            if (Directory.Exists(dir))
            {
                var reg = PcTownScriptParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Town script directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcTownScriptEntry GetScript(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcTownScriptEntry> GetByTown(int townId)
            => _reg != null ? _reg.GetByTown(townId) : System.Array.Empty<PcTownScriptEntry>();
        public IReadOnlyList<PcTownScriptEntry> GetByType(int scriptType)
            => _reg != null ? _reg.GetByType(scriptType) : System.Array.Empty<PcTownScriptEntry>();
        public IReadOnlyList<PcTownScriptEntry> GetScriptsForTown(int townId) => GetByTown(townId);

        public string GetTownName(int townId)
        {
            var entries = GetByTown(townId);
            return entries.Count > 0 ? entries[0].townNameRaw : null;
        }

        public string GetScriptTypeName(int type)
        {
            return type switch
            {
                0 => "NPC",
                1 => "Nhiệm Vụ",
                2 => "Shop",
                3 => "Dịch Vụ",
                4 => "Sự Kiện",
                _ => $"Khác ({type})",
            };
        }
    }
}
